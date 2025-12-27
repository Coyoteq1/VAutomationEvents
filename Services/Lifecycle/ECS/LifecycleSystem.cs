using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using ProjectM;
using ProjectM.Network;
using VAuto.Core;
using BepInEx.Logging;

namespace VAuto.Services.Lifecycle.ECS
{
    /// <summary>
    /// ECS System for lifecycle management and player location tracking
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct LifecycleSystem : ISystem
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
                if (!ArenaLifecycleManager.IsInitialized)
                    return;

                var deltaTime = SystemAPI.Time.DeltaTime;

                // Update player locations and trigger lifecycle events
                UpdatePlayerLocations(ref state);
                
                // Process lifecycle events
                ProcessLifecycleEvents(ref state);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[LifecycleSystem] Error in OnUpdate: {ex.Message}");
            }
        }

        private static void UpdatePlayerLocations(ref SystemState state)
        {
            try
            {
                // Update each player's location
                foreach (var (user, character, transform) in SystemAPI.Query<RefRO<User>, RefRO<PlayerCharacter>, RefRO<LocalTransform>>())
                {
                    var userEntity = SystemAPI.GetEntity(user);
                    var characterEntity = SystemAPI.GetEntity(character);
                    
                    // Update player location in the tracker
                    PlayerLocationTracker.UpdatePlayerLocation(userEntity, characterEntity);
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[LifecycleSystem] Error updating player locations: {ex.Message}");
            }
        }

        private static void ProcessLifecycleEvents(ref SystemState state)
        {
            try
            {
                // Process any pending lifecycle events
                // This could include processing build completions, arena state changes, etc.
                
                var time = SystemAPI.Time.ElapsedTime;
                
                // Example: Check for timed build completions
                CheckBuildCompletions(time);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[LifecycleSystem] Error processing lifecycle events: {ex.Message}");
            }
        }

        private static void CheckBuildCompletions(double currentTime)
        {
            // This would check for any build completions that are due
            // and trigger the appropriate lifecycle events
        }
    }

    /// <summary>
    /// ECS System for building lifecycle management
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(LifecycleSystem))]
    public partial struct BuildingLifecycleSystem : ISystem
    {
        private EntityQuery _buildRequestQuery;

        public void OnCreate(ref SystemState state)
        {
            _buildRequestQuery = state.GetEntityQuery(ComponentType.ReadOnly<BuildRequestComponent>());
        }

        public void OnUpdate(ref SystemState state)
        {
            try
            {
                if (!LifecycleBuildingService.IsInitialized)
                    return;

                var deltaTime = SystemAPI.Time.DeltaTime;

                // Process build requests
                ProcessBuildRequests(ref state);
                
                // Update building states
                UpdateBuildingStates(ref state, deltaTime);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[BuildingLifecycleSystem] Error in OnUpdate: {ex.Message}");
            }
        }

        private static void ProcessBuildRequests(ref SystemState state)
        {
            try
            {
                // Process any build requests that need lifecycle handling
                foreach (var (buildRequest, entity) in SystemAPI.Query<RefRO<BuildRequestComponent>>().WithEntityAccess())
                {
                    var request = buildRequest.ValueRO;
                    
                    // Start build through lifecycle service
                    var success = LifecycleBuildingService.StartBuild(
                        request.UserEntity,
                        request.StructureName,
                        request.ArenaId,
                        request.Position,
                        request.Rotation
                    );

                    if (success)
                    {
                        // Remove the build request entity as it's been processed
                        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
                        ecb.DestroyEntity(entity);
                        ecb.Playback(state.EntityManager);
                        ecb.Dispose();
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[BuildingLifecycleSystem] Error processing build requests: {ex.Message}");
            }
        }

        private static void UpdateBuildingStates(ref SystemState state, float deltaTime)
        {
            try
            {
                // Update all building entities
                foreach (var (building, transform, entity) in SystemAPI.Query<RefRO<BuildingComponent>, RefRO<LocalTransform>>().WithEntityAccess())
                {
                    var buildingData = building.ValueRO;
                    
                    // Update building health
                    UpdateBuildingHealth(ref state, entity, buildingData, deltaTime);
                    
                    // Update building effects
                    UpdateBuildingEffects(ref state, entity, buildingData, deltaTime);
                    
                    // Check if building should be destroyed
                    if (ShouldDestroyBuilding(buildingData))
                    {
                        DestroyBuilding(ref state, entity, buildingData);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[BuildingLifecycleSystem] Error updating building states: {ex.Message}");
            }
        }

        private static void UpdateBuildingHealth(ref SystemState state, Entity entity, BuildingComponent building, float deltaTime)
        {
            try
            {
                if (building.Health <= 0)
                    return;

                // Natural degradation
                building.Health = math.max(0, building.Health - deltaTime * building.DecayRate);
                
                // Update the component
                SystemAPI.SetComponent(entity, building);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[BuildingLifecycleSystem] Error updating building health: {ex.Message}");
            }
        }

        private static void UpdateBuildingEffects(ref SystemState state, Entity entity, BuildingComponent building, float deltaTime)
        {
            try
            {
                // Update visual effects for different building types
                switch (building.Type.ToLower())
                {
                    case "portal":
                        UpdatePortalEffects(entity, building, deltaTime);
                        break;
                    case "glow":
                        UpdateGlowEffects(entity, building, deltaTime);
                        break;
                    case "waygate":
                        UpdateWaygateEffects(entity, building, deltaTime);
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[BuildingLifecycleSystem] Error updating building effects: {ex.Message}");
            }
        }

        private static void UpdatePortalEffects(Entity entity, BuildingComponent building, float deltaTime)
        {
            // Update portal visual effects
            // This would handle particle systems, light effects, etc.
        }

        private static void UpdateGlowEffects(Entity entity, BuildingComponent building, float deltaTime)
        {
            // Update glow light effects
            var time = SystemAPI.Time.ElapsedTime;
            var flicker = (float)math.sin(time * 8.0) * 0.1f + 1.0f;
            
            if (SystemAPI.HasComponent<LightComponent>(entity))
            {
                var light = SystemAPI.GetComponent<LightComponent>(entity);
                light.Intensity = building.Intensity * flicker;
                SystemAPI.SetComponent(entity, light);
            }
        }

        private static void UpdateWaygateEffects(Entity entity, BuildingComponent building, float deltaTime)
        {
            // Update waygate visual effects
        }

        private static bool ShouldDestroyBuilding(BuildingComponent building)
        {
            return building.Health <= 0 || building.TimeRemaining <= 0;
        }

        private static void DestroyBuilding(ref SystemState state, Entity entity, BuildingComponent building)
        {
            try
            {
                // Trigger build destruction through lifecycle manager
                LifecycleBuildingService.DestroyBuilding(
                    building.OwnerEntity,
                    building.Type,
                    building.ArenaId
                );

                // Destroy the entity
                var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
                ecb.DestroyEntity(entity);
                ecb.Playback(state.EntityManager);
                ecb.Dispose();

                Plugin.Logger?.LogInfo($"[BuildingLifecycleSystem] Destroyed building: {building.Type} in arena {building.ArenaId}");
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[BuildingLifecycleSystem] Error destroying building: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// ECS System for zone detection and auto-enter
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(LifecycleSystem))]
    public partial struct ZoneDetectionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<User>();
        }

        public void OnUpdate(ref SystemState state)
        {
            try
            {
                if (!PlayerLocationTracker.IsInitialized)
                    return;

                var deltaTime = SystemAPI.Time.DeltaTime;

                // Check for zone transitions
                CheckZoneTransitions(ref state);
                
                // Process auto-enter triggers
                ProcessAutoEnterTriggers(ref state);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ZoneDetectionSystem] Error in OnUpdate: {ex.Message}");
            }
        }

        private static void CheckZoneTransitions(ref SystemState state)
        {
            try
            {
                // Player locations are updated in LifecycleSystem
                // Zone detection and transitions are handled by PlayerLocationTracker
                // This system can add additional zone-specific logic if needed
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ZoneDetectionSystem] Error checking zone transitions: {ex.Message}");
            }
        }

        private static void ProcessAutoEnterTriggers(ref SystemState state)
        {
            try
            {
                // Process any auto-enter triggers
                // This could include checking if players should be automatically entered into arenas
                // based on their location and configuration
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ZoneDetectionSystem] Error processing auto-enter triggers: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// ECS System for arena state management
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ZoneDetectionSystem))]
    public partial struct ArenaStateSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            try
            {
                if (!ArenaLifecycleManager.IsInitialized)
                    return;

                var deltaTime = SystemAPI.Time.DeltaTime;

                // Update arena states
                UpdateArenaStates(ref state, deltaTime);
                
                // Process arena events
                ProcessArenaEvents(ref state);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaStateSystem] Error in OnUpdate: {ex.Message}");
            }
        }

        private static void UpdateArenaStates(ref SystemState state, float deltaTime)
        {
            try
            {
                // Update all arena states
                var activeArenas = ArenaLifecycleManager.GetActiveArenaIds();
                
                foreach (var arenaId in activeArenas)
                {
                    var arenaState = ArenaLifecycleManager.GetArenaState(arenaId);
                    if (arenaState != null)
                    {
                        // Update arena-specific logic
                        UpdateArenaLogic(arenaId, arenaState, deltaTime);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaStateSystem] Error updating arena states: {ex.Message}");
            }
        }

        private static void UpdateArenaLogic(string arenaId, ArenaLifecycleManager.ArenaLifecycleState arenaState, float deltaTime)
        {
            try
            {
                // Update arena-specific logic based on current players and activities
                var playerCount = ArenaLifecycleManager.GetActivePlayerCount(arenaId);
                
                // Example: Auto-start arena when enough players are present
                if (playerCount >= 2 && !arenaState.IsActive)
                {
                    ArenaLifecycleManager.OnArenaStart(arenaId);
                }
                
                // Example: Auto-end arena when no players are present
                if (playerCount == 0 && arenaState.IsActive)
                {
                    ArenaLifecycleManager.OnArenaEnd(arenaId);
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaStateSystem] Error updating arena logic: {ex.Message}");
            }
        }

        private static void ProcessArenaEvents(ref SystemState state)
        {
            try
            {
                // Process any arena-specific events
                // This could include tournament start/end, special events, etc.
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaStateSystem] Error processing arena events: {ex.Message}");
            }
        }
    }

    #region Component Definitions

    /// <summary>
    /// Component for build requests
    /// </summary>
    public struct BuildRequestComponent : IComponentData
    {
        public Entity UserEntity;
        public string StructureName;
        public string ArenaId;
        public float3 Position;
        public quaternion? Rotation;
    }

    /// <summary>
    /// Component for building data
    /// </summary>
    public struct BuildingComponent : IComponentData
    {
        public string Type;
        public string ArenaId;
        public Entity OwnerEntity;
        public ulong OwnerPlatformId;
        public float Health;
        public float MaxHealth;
        public float DecayRate;
        public float Intensity;
        public float TimeRemaining;
        public bool IsActive;
    }

    #endregion
}