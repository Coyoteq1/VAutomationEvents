using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using ProjectM;
using ProjectM.Network;
using VAuto.Core;
using VAuto.Services.Systems;
using BepInEx.Logging;

namespace VAuto.Services.ECS
{
    /// <summary>
    /// ECS System for auto-enter functionality
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct AutoEnterSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<User>();
            state.RequireForUpdate<PlayerCharacter>();
        }

        public void OnUpdate(ref SystemState state)
        {
            try
            {
                if (!AutoEnterService.IsInitialized)
                    return;

                var time = SystemAPI.Time.ElapsedTime;
                var deltaTime = SystemAPI.Time.DeltaTime;

                // Check each player for auto-enter conditions
                foreach (var (user, character, transform) in SystemAPI.Query<RefRO<User>, RefRO<PlayerCharacter>, RefRO<LocalTransform>>())
                {
                    var userEntity = SystemAPI.GetEntity(user);
                    var characterEntity = SystemAPI.GetEntity(character);
                    var position = transform.ValueRO.Position;
                    var platformId = user.ValueRO.PlatformId;

                    // Check if player is in range for auto-enter
                    if (ShouldAutoEnter(platformId, position))
                    {
                        // Attempt auto-enter
                        AutoEnterService.TryAutoEnter(userEntity, characterEntity, position);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[AutoEnterSystem] Error in OnUpdate: {ex.Message}");
            }
        }

        private static bool ShouldAutoEnter(ulong platformId, float3 position)
        {
            try
            {
                // Check if auto-enter is enabled for this player
                if (!AutoEnterService.IsAutoEnterEnabled(platformId))
                    return false;

                // Check if player is not already in an arena
                if (ZoneService.IsInArena(position))
                    return false;

                // Check if player is in a valid location for auto-enter
                // This could include proximity to arena entrance points
                // For now, we'll return true if other conditions are met
                
                return true;
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[AutoEnterSystem] Error checking auto-enter conditions: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// ECS System for monitoring player positions and triggering auto-enter
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AutoEnterSystem))]
    public partial struct PlayerPositionMonitorSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<User>();
        }

        public void OnUpdate(ref SystemState state)
        {
            try
            {
                if (!AutoEnterService.IsInitialized)
                    return;

                var deltaTime = SystemAPI.Time.DeltaTime;

                // Monitor player positions and update related services
                foreach (var (user, transform) in SystemAPI.Query<RefRO<User>, RefRO<LocalTransform>>())
                {
                    var platformId = user.ValueRO.PlatformId;
                    var position = transform.ValueRO.Position;

                    // Update player position in various services
                    MapIconService.RefreshPlayerIcons();
                    
                    // Check for zone transitions
                    if (ZoneService.IsInTransitionZone(position))
                    {
                        HandleZoneTransition(platformId, position);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[PlayerPositionMonitorSystem] Error in OnUpdate: {ex.Message}");
            }
        }

        private static void HandleZoneTransition(ulong platformId, float3 position)
        {
            try
            {
                if (ZoneService.IsInArena(position))
                {
                    // Player entered arena
                    Plugin.Logger?.LogDebug($"[PlayerPositionMonitorSystem] Player {platformId} entered arena zone");
                    
                    // Mark player as in arena
                    GameSystems.MarkPlayerEnteredArena(platformId);
                    
                    // Trigger arena entry events
                    LifecycleService.EnterArena(SystemAPI.GetSingleton<User>().UserEntity, SystemAPI.GetSingleton<User>().LocalCharacter.GetEntityOnServer());
                }
                else
                {
                    // Player exited arena
                    Plugin.Logger?.LogDebug($"[PlayerPositionMonitorSystem] Player {platformId} exited arena zone");
                    
                    // Mark player as left arena
                    GameSystems.MarkPlayerExitedArena(platformId);
                    
                    // Trigger arena exit events
                    LifecycleService.ExitArena(SystemAPI.GetSingleton<User>().UserEntity, SystemAPI.GetSingleton<User>().LocalCharacter.GetEntityOnServer());
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[PlayerPositionMonitorSystem] Error handling zone transition: {ex.Message}");
            }
        }
    }
}