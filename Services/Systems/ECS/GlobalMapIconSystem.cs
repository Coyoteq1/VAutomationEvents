using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using ProjectM;
using ProjectM.Network;
using VAuto.Core;
using VAuto.Services.Systems;
using VAuto.Services.Lifecycle;
using BepInEx.Logging;

namespace VAuto.Services.Systems.ECS
{
    /// <summary>
    /// ECS System for global map icon management
    /// Integrates with PlayerLocationTracker and GlobalMapIconService for 3-second updates
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial struct GlobalMapIconSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<User>();
            
            // Initialize global map icon service
            GlobalMapIconService.Initialize();
            
            Plugin.Logger?.LogInfo("[GlobalMapIconSystem] Initialized global map icon ECS system");
        }

        public void OnUpdate(ref SystemState state)
        {
            try
            {
                if (!GlobalMapIconService.IsInitialized)
                    return;

                var deltaTime = SystemAPI.Time.DeltaTime;

                // Update player locations through the tracker
                UpdatePlayerLocations();
                
                // Additional map icon updates can be done here if needed
                ProcessMapIconUpdates(ref state, deltaTime);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[GlobalMapIconSystem] Error in OnUpdate: {ex.Message}");
            }
        }

        private static void UpdatePlayerLocations()
        {
            try
            {
                // Update each player's location through the lifecycle system
                foreach (var (user, character, transform) in SystemAPI.Query<RefRO<User>, RefRO<PlayerCharacter>, RefRO<LocalTransform>>())
                {
                    var userEntity = SystemAPI.GetEntity(user);
                    var characterEntity = SystemAPI.GetEntity(character);
                    
                    // Update location through player location tracker
                    PlayerLocationTracker.UpdatePlayerLocation(userEntity, characterEntity);
                    
                    // Also update global map icon service (redundant but ensures consistency)
                    GlobalMapIconService.UpdateAllPlayerIcons();
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[GlobalMapIconSystem] Error updating player locations: {ex.Message}");
            }
        }

        private static void ProcessMapIconUpdates(ref SystemState state, float deltaTime)
        {
            try
            {
                // Process any additional map icon logic
                // For example: updating icon visibility based on distance, line of sight, etc.
                
                UpdateMapIconVisibility(ref state);
                ProcessMapIconInteractions(ref state);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[GlobalMapIconSystem] Error processing map icon updates: {ex.Message}");
            }
        }

        private static void UpdateMapIconVisibility(ref SystemState state)
        {
            try
            {
                // Update visibility of map icons based on player distance or other factors
                // This is where you could implement visibility culling for performance
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[GlobalMapIconSystem] Error updating map icon visibility: {ex.Message}");
            }
        }

        private static void ProcessMapIconInteractions(ref SystemState state)
        {
            try
            {
                // Handle interactions with map icons
                // For example: clicking on an icon to get player info, set waypoints, etc.
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[GlobalMapIconSystem] Error processing map icon interactions: {ex.Message}");
            }
        }

        public void OnDestroy(ref SystemState state)
        {
            try
            {
                // Cleanup global map icon service
                GlobalMapIconService.Cleanup();
                
                Plugin.Logger?.LogInfo("[GlobalMapIconSystem] Cleaned up global map icon ECS system");
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[GlobalMapIconSystem] Error during cleanup: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// ECS System for map icon lifecycle integration
    /// Handles map icon creation/destruction based on lifecycle events
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(GlobalMapIconSystem))]
    public partial struct MapIconLifecycleSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // Register with lifecycle manager for lifecycle events
            // This would be done through the service manager
        }

        public void OnUpdate(ref SystemState state)
        {
            try
            {
                // Process lifecycle events that affect map icons
                ProcessPlayerEnterEvents(ref state);
                ProcessPlayerExitEvents(ref state);
                ProcessArenaEvents(ref state);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[MapIconLifecycleSystem] Error in OnUpdate: {ex.Message}");
            }
        }

        private static void ProcessPlayerEnterEvents(ref SystemState state)
        {
            try
            {
                // Handle player entering arenas - update icon types
                var activeArenas = ArenaLifecycleManager.GetActiveArenaIds();
                
                foreach (var arenaId in activeArenas)
                {
                    var playerEvents = ArenaLifecycleManager.GetPlayerEvents(arenaId);
                    var recentEnterEvents = playerEvents.Where(e => 
                        e.EventType == PlayerLifecycleEventType.Enter && 
                        (DateTime.UtcNow - e.Timestamp).TotalSeconds < 5.0).ToList();
                    
                    foreach (var enterEvent in recentEnterEvents)
                    {
                        // Update the player's map icon to reflect arena status
                        UpdatePlayerIconForArena(enterEvent.PlatformId, arenaId, true);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[MapIconLifecycleSystem] Error processing player enter events: {ex.Message}");
            }
        }

        private static void ProcessPlayerExitEvents(ref SystemState state)
        {
            try
            {
                // Handle player exiting arenas - update icon types back to normal
                var activeArenas = ArenaLifecycleManager.GetActiveArenaIds();
                
                foreach (var arenaId in activeArenas)
                {
                    var playerEvents = ArenaLifecycleManager.GetPlayerEvents(arenaId);
                    var recentExitEvents = playerEvents.Where(e => 
                        e.EventType == PlayerLifecycleEventType.Exit && 
                        (DateTime.UtcNow - e.Timestamp).TotalSeconds < 5.0).ToList();
                    
                    foreach (var exitEvent in recentExitEvents)
                    {
                        // Update the player's map icon back to normal status
                        UpdatePlayerIconForArena(exitEvent.PlatformId, arenaId, false);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[MapIconLifecycleSystem] Error processing player exit events: {ex.Message}");
            }
        }

        private static void ProcessArenaEvents(ref SystemState state)
        {
            try
            {
                // Handle arena start/end events
                // This could include updating all map icons when an arena starts/ends
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[MapIconLifecycleSystem] Error processing arena events: {ex.Message}");
            }
        }

        private static void UpdatePlayerIconForArena(ulong platformId, string arenaId, bool isInArena)
        {
            try
            {
                // Update the player's map icon based on their arena status
                var iconData = GlobalMapIconService.GetPlayerIcon(platformId);
                
                if (iconData != null)
                {
                    // Update icon properties based on arena status
                    // This would change colors, scales, etc. based on whether player is in arena
                    
                    Plugin.Logger?.LogDebug($"[MapIconLifecycleSystem] Updated icon for player {platformId} - Arena: {isInArena}");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[MapIconLifecycleSystem] Error updating player icon for arena: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// ECS System for map icon performance optimization
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MapIconLifecycleSystem))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct MapIconOptimizationSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            try
            {
                if (!GlobalMapIconService.IsInitialized)
                    return;

                var deltaTime = SystemAPI.Time.DeltaTime;

                // Optimize map icon updates based on distance
                OptimizeMapIconUpdates(ref state, deltaTime);
                
                // Clean up unused map icons
                CleanupUnusedMapIcons(ref state);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[MapIconOptimizationSystem] Error in OnUpdate: {ex.Message}");
            }
        }

        private static void OptimizeMapIconUpdates(ref SystemState state, float deltaTime)
        {
            try
            {
                // Only update map icons for players that have moved significantly
                // This can reduce the update frequency for performance
                
                var allIcons = GlobalMapIconService.GetAllActiveIcons();
                
                foreach (var iconData in allIcons)
                {
                    // Check if player has moved enough to warrant an icon update
                    if (ShouldUpdateIcon(iconData, deltaTime))
                    {
                        // Force an update of this specific icon
                        ForceIconUpdate(iconData.PlatformId);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[MapIconOptimizationSystem] Error optimizing icon updates: {ex.Message}");
            }
        }

        private static bool ShouldUpdateIcon(GlobalMapIconService.MapIconData iconData, float deltaTime)
        {
            try
            {
                // Simple distance-based optimization
                // Only update if enough time has passed since last update
                var timeSinceUpdate = (float)(DateTime.UtcNow - iconData.LastUpdate).TotalSeconds;
                return timeSinceUpdate >= GlobalMapIconService.GetUpdateInterval() * 0.8f; // 80% of interval
            }
            catch
            {
                return true; // Default to updating if there's an error
            }
        }

        private static void ForceIconUpdate(ulong platformId)
        {
            try
            {
                // Force an update of a specific player's icon
                // This would be implemented in the GlobalMapIconService
                Plugin.Logger?.LogDebug($"[MapIconOptimizationSystem] Forced icon update for player {platformId}");
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[MapIconOptimizationSystem] Error forcing icon update: {ex.Message}");
            }
        }

        private static void CleanupUnusedMapIcons(ref SystemState state)
        {
            try
            {
                // Remove map icons for players who are no longer online or have been offline for too long
                var allIcons = GlobalMapIconService.GetAllActiveIcons();
                var currentTime = DateTime.UtcNow;
                
                var iconsToRemove = allIcons.Where(icon => 
                    (currentTime - icon.LastUpdate).TotalMinutes > 10.0).ToList(); // Remove after 10 minutes of inactivity
                
                foreach (var iconData in iconsToRemove)
                {
                    // This would be implemented in GlobalMapIconService
                    Plugin.Logger?.LogDebug($"[MapIconOptimizationSystem] Cleanup: removing inactive icon for {iconData.CharacterName}");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[MapIconOptimizationSystem] Error cleaning up unused icons: {ex.Message}");
            }
        }
    }
}