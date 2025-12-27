using System;
using System.Collections.Generic;
using Unity.Entities;
using ProjectM;
using ProjectM.Network;
using VAuto.Core;
using Unity.Collections;

namespace VAuto.Services
{
    /// <summary>
    /// Service for managing auto-enter functionality in arena mode
    /// Handles automatic re-entry and seamless arena transitions
    /// </summary>
    public static class AutoEnterService
    {
        private static readonly HashSet<ulong> _autoEnterEnabledPlayers = new();
        private static readonly Dictionary<ulong, AutoEnterState> _playerStates = new();

        /// <summary>
        /// Initialize the auto-enter service
        /// </summary>
        public static void Initialize()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Plugin.Logger?.LogInfo($"[{timestamp}] AUTO_ENTER_SERVICE_INIT - Initializing auto-enter service");

            try
            {
                _autoEnterEnabledPlayers.Clear();
                _playerStates.Clear();

                Plugin.Logger?.LogInfo($"[{timestamp}] AUTO_ENTER_SERVICE_COMPLETE - Auto-enter service initialized");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[{timestamp}] AUTO_ENTER_SERVICE_ERROR - Failed to initialize: {ex.Message}");
                Plugin.Logger?.LogError($"[{timestamp}] AUTO_ENTER_SERVICE_STACK - {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Cleanup the auto-enter service
        /// </summary>
        public static void Cleanup()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Plugin.Logger?.LogInfo($"[{timestamp}] AUTO_ENTER_SERVICE_CLEANUP - Cleaning up auto-enter service");

            _autoEnterEnabledPlayers.Clear();
            _playerStates.Clear();

            Plugin.Logger?.LogInfo($"[{timestamp}] AUTO_ENTER_SERVICE_CLEANUP_COMPLETE - Auto-enter service cleaned up");
        }

        /// <summary>
        /// Enable auto-enter for a player
        /// </summary>
        public static bool EnableAutoEnter(Entity userEntity)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

            try
            {
                // Get platform ID from user entity
                if (!VRCore.EM.TryGetComponentData(userEntity, out User userComponent))
                {
                    Plugin.Logger?.LogError($"[{timestamp}] AUTO_ENTER_ENABLE_ERROR - Could not get User component for entity {userEntity}");
                    return false;
                }

                var platformId = userComponent.PlatformId;
                Plugin.Logger?.LogInfo($"[{timestamp}] AUTO_ENTER_ENABLE - Enabling auto-enter for player {platformId}");

                // Check if already enabled
                if (_autoEnterEnabledPlayers.Contains(platformId))
                {
                    Plugin.Logger?.LogWarning($"[{timestamp}] AUTO_ENTER_ENABLE_WARNING - Auto-enter already enabled for {platformId}");
                    return true;
                }

                // Enable auto-enter
                _autoEnterEnabledPlayers.Add(platformId);

                // Create or update state
                var state = new AutoEnterState
                {
                    PlatformId = platformId,
                    UserEntity = userEntity,
                    EnabledAt = DateTime.UtcNow,
                    IsEnabled = true,
                    LastAutoEnter = null
                };
                _playerStates[platformId] = state;

                Plugin.Logger?.LogInfo($"[{timestamp}] AUTO_ENTER_ENABLE_SUCCESS - Auto-enter enabled for player {platformId}");
                return true;

            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[{timestamp}] AUTO_ENTER_ENABLE_EXCEPTION - Failed to enable auto-enter: {ex.Message}");
                Plugin.Logger?.LogError($"[{timestamp}] AUTO_ENTER_ENABLE_STACK - {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Disable auto-enter for a player
        /// </summary>
        public static bool DisableAutoEnter(Entity userEntity)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

            try
            {
                // Get platform ID from user entity
                if (!VRCore.EM.TryGetComponentData(userEntity, out User userComponent))
                {
                    Plugin.Logger?.LogError($"[{timestamp}] AUTO_ENTER_DISABLE_ERROR - Could not get User component for entity {userEntity}");
                    return false;
                }

                var platformId = userComponent.PlatformId;
                Plugin.Logger?.LogInfo($"[{timestamp}] AUTO_ENTER_DISABLE - Disabling auto-enter for player {platformId}");

                // Check if enabled
                if (!_autoEnterEnabledPlayers.Contains(platformId))
                {
                    Plugin.Logger?.LogWarning($"[{timestamp}] AUTO_ENTER_DISABLE_WARNING - Auto-enter not enabled for {platformId}");
                    return true;
                }

                // Disable auto-enter
                _autoEnterEnabledPlayers.Remove(platformId);

                // Update state
                if (_playerStates.TryGetValue(platformId, out var state))
                {
                    state.IsEnabled = false;
                    state.DisabledAt = DateTime.UtcNow;
                }

                Plugin.Logger?.LogInfo($"[{timestamp}] AUTO_ENTER_DISABLE_SUCCESS - Auto-enter disabled for player {platformId}");
                return true;

            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[{timestamp}] AUTO_ENTER_DISABLE_EXCEPTION - Failed to disable auto-enter: {ex.Message}");
                Plugin.Logger?.LogError($"[{timestamp}] AUTO_ENTER_DISABLE_STACK - {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Check if auto-enter is enabled for a player
        /// </summary>
        public static bool IsAutoEnterEnabled(ulong platformId)
        {
            var enabled = _autoEnterEnabledPlayers.Contains(platformId);
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Plugin.Logger?.LogDebug($"[{timestamp}] AUTO_ENTER_CHECK - PlatformId: {platformId}, Enabled: {enabled}");
            return enabled;
        }

        /// <summary>
        /// Get auto-enter state for a player
        /// </summary>
        public static AutoEnterState GetAutoEnterState(ulong platformId)
        {
            return _playerStates.TryGetValue(platformId, out var state) ? state : null;
        }

        /// <summary>
        /// Attempt auto-enter for a player if enabled
        /// </summary>
        public static bool TryAutoEnter(ulong platformId, Entity userEntity, Entity character)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

            try
            {
                // Check if auto-enter is enabled
                if (!IsAutoEnterEnabled(platformId))
                {
                    Plugin.Logger?.LogDebug($"[{timestamp}] AUTO_ENTER_SKIP - Auto-enter not enabled for {platformId}");
                    return false;
                }

                // Check if already in arena
                if (MissingServices.LifecycleService.IsPlayerInArena(platformId))
                {
                    Plugin.Logger?.LogDebug($"[{timestamp}] AUTO_ENTER_SKIP - Player {platformId} already in arena");
                    return false;
                }

                Plugin.Logger?.LogInfo($"[{timestamp}] AUTO_ENTER_ATTEMPT - Attempting auto-enter for player {platformId}");

                // Perform arena entry
                if (MissingServices.LifecycleService.EnterArena(userEntity, character))
                {
                    // Update last auto-enter time
                    if (_playerStates.TryGetValue(platformId, out var state))
                    {
                        state.LastAutoEnter = DateTime.UtcNow;
                        state.AutoEnterCount++;
                    }

                    Plugin.Logger?.LogInfo($"[{timestamp}] AUTO_ENTER_SUCCESS - Auto-enter successful for player {platformId}");
                    return true;
                }
                else
                {
                    Plugin.Logger?.LogWarning($"[{timestamp}] AUTO_ENTER_FAILED - LifecycleService.EnterArena failed for {platformId}");
                    return false;
                }

            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[{timestamp}] AUTO_ENTER_EXCEPTION - Auto-enter failed for {platformId}: {ex.Message}");
                Plugin.Logger?.LogError($"[{timestamp}] AUTO_ENTER_STACK - {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Check if auto-enter should be triggered for a player
        /// </summary>
        public static bool ShouldTriggerAutoEnter(ulong platformId, float3 playerPosition)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

            try
            {
                // Check if auto-enter is enabled
                if (!IsAutoEnterEnabled(platformId))
                {
                    return false;
                }

                // Check distance to arena center
                var distanceToArena = math.distance(playerPosition, Plugin.Config.ArenaCenter);

                // Check enter radius
                var shouldTrigger = distanceToArena <= Plugin.Config.ArenaEnterRadius;

                Plugin.Logger?.LogDebug($"[{timestamp}] AUTO_ENTER_TRIGGER_CHECK - PlatformId: {platformId}, Distance: {distanceToArena:F1}, EnterRadius: {Plugin.Config.ArenaEnterRadius:F1}, ShouldTrigger: {shouldTrigger}");

                return shouldTrigger;

            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[{timestamp}] AUTO_ENTER_TRIGGER_EXCEPTION - Failed to check trigger for {platformId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get auto-enter statistics
        /// </summary>
        public static Dictionary<string, int> GetAutoEnterStatistics()
        {
            return new Dictionary<string, int>
            {
                ["Players_With_AutoEnter"] = _autoEnterEnabledPlayers.Count,
                ["Total_AutoEnter_Events"] = _playerStates.Values.Sum(s => s.AutoEnterCount)
            };
        }

        /// <summary>
        /// Reset auto-enter state for all players
        /// </summary>
        public static void ResetAllAutoEnterStates()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Plugin.Logger?.LogInfo($"[{timestamp}] AUTO_ENTER_RESET_ALL - Resetting auto-enter states for all players");

            _autoEnterEnabledPlayers.Clear();

            foreach (var state in _playerStates.Values)
            {
                state.IsEnabled = false;
                state.DisabledAt = DateTime.UtcNow;
            }

            Plugin.Logger?.LogInfo($"[{timestamp}] AUTO_ENTER_RESET_ALL_COMPLETE - Reset {(_playerStates.Count)} player states");
        }

        /// <summary>
        /// Process auto-enter for all eligible players (called periodically)
        /// </summary>
        public static void ProcessAutoEnterForAllPlayers()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

            try
            {
                var processed = 0;
                var triggered = 0;

                foreach (var platformId in _autoEnterEnabledPlayers.ToList())
                {
                    try
                    {
                        // Find user and character entities
                        var userEntity = FindUserEntity(platformId);
                        if (userEntity == Entity.Null)
                        {
                            Plugin.Logger?.LogWarning($"[{timestamp}] AUTO_ENTER_PROCESS_SKIP - Could not find user entity for {platformId}");
                            continue;
                        }

                        // Get character entity
                        var characterEntity = FindCharacterEntity(userEntity);
                        if (characterEntity == Entity.Null)
                        {
                            Plugin.Logger?.LogWarning($"[{timestamp}] AUTO_ENTER_PROCESS_SKIP - Could not find character entity for {platformId}");
                            continue;
                        }

                        // Get current position
                        if (!VRCore.EM.TryGetComponentData(characterEntity, out Translation translation))
                        {
                            Plugin.Logger?.LogWarning($"[{timestamp}] AUTO_ENTER_PROCESS_SKIP - Could not get position for {platformId}");
                            continue;
                        }

                        var position = translation.Value;
                        processed++;

                        // Check if should trigger
                        if (ShouldTriggerAutoEnter(platformId, position))
                        {
                            if (TryAutoEnter(platformId, userEntity, characterEntity))
                            {
                                triggered++;
                                Plugin.Logger?.LogInfo($"[{timestamp}] AUTO_ENTER_PROCESS_TRIGGERED - Auto-enter triggered for {platformId} at position {position}");
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger?.LogError($"[{timestamp}] AUTO_ENTER_PROCESS_ERROR - Error processing auto-enter for {platformId}: {ex.Message}");
                    }
                }

                if (processed > 0)
                {
                    Plugin.Logger?.LogDebug($"[{timestamp}] AUTO_ENTER_PROCESS_COMPLETE - Processed {processed} players, triggered {triggered} auto-enters");
                }

            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[{timestamp}] AUTO_ENTER_PROCESS_EXCEPTION - Critical error in auto-enter processing: {ex.Message}");
                Plugin.Logger?.LogError($"[{timestamp}] AUTO_ENTER_PROCESS_STACK - {ex.StackTrace}");
            }
        }

        #region Private Methods

        private static Entity FindUserEntity(ulong platformId)
        {
            try
            {
                var em = VRCore.EM;

                // Query for User components
                var userQuery = em.CreateEntityQuery(ComponentType.ReadOnly<User>());
                var userEntities = userQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

                foreach (var userEntity in userEntities)
                {
                    if (em.TryGetComponentData(userEntity, out User user) && user.PlatformId == platformId)
                    {
                        userEntities.Dispose();
                        return userEntity;
                    }
                }

                userEntities.Dispose();
                return Entity.Null;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"FindUserEntity error for {platformId}: {ex.Message}");
                return Entity.Null;
            }
        }

        private static Entity FindCharacterEntity(Entity userEntity)
        {
            try
            {
                var em = VRCore.EM;

                // Try to get character from user entity
                if (em.TryGetComponentData(userEntity, out User user))
                {
                    return user.LocalCharacter._Entity;
                }

                return Entity.Null;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"FindCharacterEntity error for user {userEntity}: {ex.Message}");
                return Entity.Null;
            }
        }

        #endregion

        #region Data Structures

        public class AutoEnterState
        {
            public ulong PlatformId { get; set; }
            public Entity UserEntity { get; set; }
            public DateTime EnabledAt { get; set; }
            public DateTime? DisabledAt { get; set; }
            public DateTime? LastAutoEnter { get; set; }
            public bool IsEnabled { get; set; }
            public int AutoEnterCount { get; set; }
        }

        #endregion
    }
}
