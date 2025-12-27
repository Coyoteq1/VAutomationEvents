using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM;
using ProjectM.Network;
using ProjectM.Gameplay;
using VAuto.Core;
using BepInEx.Logging;

namespace VAuto.Services.Lifecycle
{
    /// <summary>
    /// Lifecycle-aware AutoEnter service with full Enter/Exit lifecycle management
    /// </summary>
    public class LifecycleAutoEnterService : IArenaLifecycleService
    {
        private static bool _initialized = false;
        private static readonly Dictionary<ulong, AutoEnterData> _autoEnterPlayers = new();
        private static readonly Dictionary<string, DateTime> _playerEnterTimes = new();
        private static readonly object _lock = new object();
        
        public static bool IsInitialized => _initialized;
        public static ManualLogSource Log => Plugin.Logger;

        #region Initialization
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized) return;
                
                try
                {
                    Log?.LogInfo("[LifecycleAutoEnterService] Initializing lifecycle auto-enter service...");
                    
                    _autoEnterPlayers.Clear();
                    _playerEnterTimes.Clear();
                    
                    // Register with lifecycle manager
                    ArenaLifecycleManager.RegisterLifecycleService(new LifecycleAutoEnterService());
                    
                    _initialized = true;
                    
                    Log?.LogInfo("[LifecycleAutoEnterService] Lifecycle auto-enter service initialized successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[LifecycleAutoEnterService] Failed to initialize: {ex.Message}");
                    throw;
                }
            }
        }

        public static void Cleanup()
        {
            lock (_lock)
            {
                if (!_initialized) return;
                
                try
                {
                    Log?.LogInfo("[LifecycleAutoEnterService] Cleaning up lifecycle auto-enter service...");
                    
                    // Unregister from lifecycle manager
                    ArenaLifecycleManager.UnregisterLifecycleService(new LifecycleAutoEnterService());
                    
                    _autoEnterPlayers.Clear();
                    _playerEnterTimes.Clear();
                    _initialized = false;
                    
                    Log?.LogInfo("[LifecycleAutoEnterService] Lifecycle auto-enter service cleaned up successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[LifecycleAutoEnterService] Failed to cleanup: {ex.Message}");
                }
            }
        }
        #endregion

        #region IArenaLifecycleService Implementation
        public bool OnPlayerEnter(Entity user, Entity character, string arenaId)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                if (!em.TryGetComponentData(user, out User userData))
                    return false;

                var platformId = userData.PlatformId;
                var characterName = userData.CharacterName.ToString();

                lock (_lock)
                {
                    // Record enter time
                    _playerEnterTimes[$"{platformId}_{arenaId}"] = DateTime.UtcNow;

                    // Update player data if they have auto-enter enabled
                    if (_autoEnterPlayers.TryGetValue(platformId, out var autoEnterData))
                    {
                        autoEnterData.LastArenaId = arenaId;
                        autoEnterData.EnterCount++;
                        autoEnterData.LastEnterTime = DateTime.UtcNow;
                        
                        Log?.LogInfo($"[LifecycleAutoEnterService] Player {characterName} entered arena {arenaId} (Auto-enter active)");
                    }
                    else
                    {
                        Log?.LogDebug($"[LifecycleAutoEnterService] Player {characterName} entered arena {arenaId}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleAutoEnterService] Error in OnPlayerEnter: {ex.Message}");
                return false;
            }
        }

        public bool OnPlayerExit(Entity user, Entity character, string arenaId)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                if (!em.TryGetComponentData(user, out User userData))
                    return false;

                var platformId = userData.PlatformId;
                var characterName = userData.CharacterName.ToString();

                lock (_lock)
                {
                    // Clear enter time
                    var enterKey = $"{platformId}_{arenaId}";
                    if (_playerEnterTimes.ContainsKey(enterKey))
                    {
                        _playerEnterTimes.Remove(enterKey);
                    }

                    // Update player data
                    if (_autoEnterPlayers.TryGetValue(platformId, out var autoEnterData))
                    {
                        autoEnterData.LastExitTime = DateTime.UtcNow;
                        autoEnterData.ExitCount++;
                        
                        Log?.LogInfo($"[LifecycleAutoEnterService] Player {characterName} exited arena {arenaId} (Auto-enter active)");
                    }
                    else
                    {
                        Log?.LogDebug($"[LifecycleAutoEnterService] Player {characterName} exited arena {arenaId}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleAutoEnterService] Error in OnPlayerExit: {ex.Message}");
                return false;
            }
        }

        public bool OnArenaStart(string arenaId)
        {
            try
            {
                Log?.LogInfo($"[LifecycleAutoEnterService] Arena {arenaId} started - enabling auto-enter for eligible players");
                
                // Enable auto-enter for all players who have it enabled
                lock (_lock)
                {
                    foreach (var playerData in _autoEnterPlayers.Values)
                    {
                        if (playerData.IsEnabled)
                        {
                            // Trigger auto-enter for players in range
                            TryAutoEnterToArena(playerData.PlatformId, arenaId);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleAutoEnterService] Error in OnArenaStart: {ex.Message}");
                return false;
            }
        }

        public bool OnArenaEnd(string arenaId)
        {
            try
            {
                Log?.LogInfo($"[LifecycleAutoEnterService] Arena {arenaId} ended - processing player exits");
                
                // Get all players who were in this arena
                var playersToExit = new List<ulong>();
                lock (_lock)
                {
                    foreach (var enterTime in _playerEnterTimes.Where(et => et.Key.EndsWith($"_{arenaId}")))
                    {
                        var platformId = ulong.Parse(enterTime.Key.Split('_')[0]);
                        playersToExit.Add(platformId);
                    }
                }

                // Force exit for all players in the ended arena
                foreach (var platformId in playersToExit)
                {
                    ForcePlayerExit(platformId, arenaId);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleAutoEnterService] Error in OnArenaEnd: {ex.Message}");
                return false;
            }
        }

        public bool OnBuildStart(Entity user, string structureName, string arenaId)
        {
            try
            {
                // Building start doesn't directly affect auto-enter, but we can log it
                Log?.LogDebug($"[LifecycleAutoEnterService] Build started: {structureName} in arena {arenaId}");
                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleAutoEnterService] Error in OnBuildStart: {ex.Message}");
                return false;
            }
        }

        public bool OnBuildComplete(Entity user, string structureName, string arenaId)
        {
            try
            {
                // Building completion might affect auto-enter if it's a portal or waygate
                if (structureName.ToLower().Contains("portal") || structureName.ToLower().Contains("waygate"))
                {
                    Log?.LogInfo($"[LifecycleAutoEnterService] Portal/Waygate built: {structureName} in arena {arenaId} - updating auto-enter paths");
                    
                    // Update auto-enter paths for this arena
                    UpdateAutoEnterPaths(arenaId);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleAutoEnterService] Error in OnBuildComplete: {ex.Message}");
                return false;
            }
        }

        public bool OnBuildDestroy(Entity user, string structureName, string arenaId)
        {
            try
            {
                // Building destruction might affect auto-enter paths
                if (structureName.ToLower().Contains("portal") || structureName.ToLower().Contains("waygate"))
                {
                    Log?.LogInfo($"[LifecycleAutoEnterService] Portal/Waygate destroyed: {structureName} in arena {arenaId} - updating auto-enter paths");
                    
                    // Update auto-enter paths for this arena
                    UpdateAutoEnterPaths(arenaId);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleAutoEnterService] Error in OnBuildDestroy: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Auto-Enter Management
        public static void EnableAutoEnter(ulong platformId)
        {
            lock (_lock)
            {
                if (!_autoEnterPlayers.ContainsKey(platformId))
                {
                    _autoEnterPlayers[platformId] = new AutoEnterData
                    {
                        PlatformId = platformId,
                        IsEnabled = true,
                        EnabledAt = DateTime.UtcNow,
                        LastAttempt = DateTime.MinValue
                    };
                }
                else
                {
                    _autoEnterPlayers[platformId].IsEnabled = true;
                    _autoEnterPlayers[platformId].EnabledAt = DateTime.UtcNow;
                }
                
                Log?.LogInfo($"[LifecycleAutoEnterService] Auto-enter enabled for player {platformId}");
            }
        }

        public static void DisableAutoEnter(ulong platformId)
        {
            lock (_lock)
            {
                if (_autoEnterPlayers.ContainsKey(platformId))
                {
                    _autoEnterPlayers[platformId].IsEnabled = false;
                    _autoEnterPlayers[platformId].DisabledAt = DateTime.UtcNow;
                }
                
                Log?.LogInfo($"[LifecycleAutoEnterService] Auto-enter disabled for player {platformId}");
            }
        }

        public static bool IsAutoEnterEnabled(ulong platformId)
        {
            lock (_lock)
            {
                return _autoEnterPlayers.TryGetValue(platformId, out var data) && data.IsEnabled;
            }
        }

        public static bool TryAutoEnterToArena(ulong platformId, string arenaId)
        {
            try
            {
                if (!IsAutoEnterEnabled(platformId))
                    return false;

                var em = VAutoCore.EntityManager;
                
                // Find player entity
                var userQuery = em.CreateEntityQuery(ComponentType.ReadOnly<User>());
                var userEntities = userQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                
                Entity targetUser = Entity.Null;
                Entity targetCharacter = Entity.Null;
                
                foreach (var userEntity in userEntities)
                {
                    if (em.TryGetComponentData(userEntity, out User user) && user.PlatformId == platformId)
                    {
                        targetUser = userEntity;
                        targetCharacter = user.LocalCharacter.Equals(default) ? Entity.Null : user.LocalCharacter.GetEntityOnServer();
                        break;
                    }
                }
                
                userEntities.Dispose();

                if (targetUser == Entity.Null || targetCharacter == Entity.Null)
                {
                    Log?.LogWarning($"[LifecycleAutoEnterService] Could not find player {platformId} for auto-enter");
                    return false;
                }

                // Check cooldown
                if (_autoEnterPlayers.TryGetValue(platformId, out var autoEnterData))
                {
                    if (DateTime.UtcNow - autoEnterData.LastAttempt < TimeSpan.FromSeconds(autoEnterData.CooldownSeconds))
                    {
                        Log?.LogDebug($"[LifecycleAutoEnterService] Player {platformId} on cooldown for auto-enter");
                        return false;
                    }

                    autoEnterData.LastAttempt = DateTime.UtcNow;
                    autoEnterData.AttemptsCount++;
                }

                // Trigger arena entry through lifecycle manager
                var success = ArenaLifecycleManager.OnPlayerEnter(targetUser, targetCharacter, arenaId);
                
                if (success)
                {
                    Log?.LogInfo($"[LifecycleAutoEnterService] Auto-entered player {platformId} to arena {arenaId}");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleAutoEnterService] Auto-enter failed for player {platformId}: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Helper Methods
        private static void ForcePlayerExit(ulong platformId, string arenaId)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                
                // Find player entity
                var userQuery = em.CreateEntityQuery(ComponentType.ReadOnly<User>());
                var userEntities = userQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                
                Entity targetUser = Entity.Null;
                Entity targetCharacter = Entity.Null;
                
                foreach (var userEntity in userEntities)
                {
                    if (em.TryGetComponentData(userEntity, out User user) && user.PlatformId == platformId)
                    {
                        targetUser = userEntity;
                        targetCharacter = user.LocalCharacter.Equals(default) ? Entity.Null : user.LocalCharacter.GetEntityOnServer();
                        break;
                    }
                }
                
                userEntities.Dispose();

                if (targetUser != Entity.Null && targetCharacter != Entity.Null)
                {
                    // Force exit through lifecycle manager
                    ArenaLifecycleManager.OnPlayerExit(targetUser, targetCharacter, arenaId);
                    
                    Log?.LogInfo($"[LifecycleAutoEnterService] Force exited player {platformId} from arena {arenaId}");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleAutoEnterService] Failed to force exit player {platformId}: {ex.Message}");
            }
        }

        private static void UpdateAutoEnterPaths(string arenaId)
        {
            try
            {
                // This would update auto-enter paths based on new portal/waygate locations
                Log?.LogDebug($"[LifecycleAutoEnterService] Updated auto-enter paths for arena {arenaId}");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleAutoEnterService] Failed to update auto-enter paths: {ex.Message}");
            }
        }
        #endregion

        #region Query Methods
        public static int GetAutoEnterEnabledCount()
        {
            lock (_lock)
            {
                return _autoEnterPlayers.Values.Count(p => p.IsEnabled);
            }
        }

        public static Dictionary<string, object> GetPlayerAutoEnterStats(ulong platformId)
        {
            lock (_lock)
            {
                if (!_autoEnterPlayers.TryGetValue(platformId, out var data))
                    return new Dictionary<string, object>();

                return new Dictionary<string, object>
                {
                    ["Enabled"] = data.IsEnabled,
                    ["EnabledAt"] = data.EnabledAt,
                    ["EnterCount"] = data.EnterCount,
                    ["ExitCount"] = data.ExitCount,
                    ["LastEnterTime"] = data.LastEnterTime,
                    ["LastExitTime"] = data.LastExitTime,
                    ["AttemptsCount"] = data.AttemptsCount,
                    ["LastArenaId"] = data.LastArenaId
                };
            }
        }

        public static List<AutoEnterData> GetAllAutoEnterPlayers()
        {
            lock (_lock)
            {
                return _autoEnterPlayers.Values.ToList();
            }
        }
        #endregion

        #region Data Structures
        public class AutoEnterData
        {
            public ulong PlatformId { get; set; }
            public bool IsEnabled { get; set; }
            public DateTime EnabledAt { get; set; }
            public DateTime DisabledAt { get; set; }
            public DateTime LastAttempt { get; set; }
            public int AttemptsCount { get; set; }
            public int EnterCount { get; set; }
            public int ExitCount { get; set; }
            public DateTime LastEnterTime { get; set; }
            public DateTime LastExitTime { get; set; }
            public string LastArenaId { get; set; }
            public float CooldownSeconds { get; set; } = 5.0f;
        }
        #endregion
    }
}