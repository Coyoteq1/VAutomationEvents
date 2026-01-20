using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using ProjectM;
using ProjectM.Network;
using VAuto.Core;
using VAuto.Data;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Arena Data Saver - Specialized service for saving arena-specific data
    /// </summary>
    public static class ArenaDataSaver
    {
        private static bool _initialized = false;
        private static readonly Dictionary<string, ArenaData> _arenaData = new();
        private static readonly Dictionary<string, DateTime> _lastSaveTimes = new();
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
                    Log?.LogInfo("[ArenaDataSaver] Initializing arena data saver...");
                    
                    _arenaData.Clear();
                    _lastSaveTimes.Clear();
                    _initialized = true;
                    
                    Log?.LogInfo("[ArenaDataSaver] Arena data saver initialized successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[ArenaDataSaver] Failed to initialize: {ex.Message}");
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
                    Log?.LogInfo("[ArenaDataSaver] Cleaning up arena data saver...");
                    
                    // Save all pending data before cleanup
                    SaveAllArenaData();
                    
                    _arenaData.Clear();
                    _lastSaveTimes.Clear();
                    _initialized = false;
                    
                    Log?.LogInfo("[ArenaDataSaver] Arena data saver cleaned up successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[ArenaDataSaver] Failed to cleanup: {ex.Message}");
                }
            }
        }
        #endregion

        #region Arena Data Management
        public static bool CreateArenaData(string arenaId, ArenaType type, float3 center, float radius)
        {
            try
            {
                if (string.IsNullOrEmpty(arenaId))
                {
                    Log?.LogWarning("[ArenaDataSaver] Cannot create arena with null or empty ID");
                    return false;
                }

                lock (_lock)
                {
                    if (_arenaData.ContainsKey(arenaId))
                    {
                        Log?.LogWarning($"[ArenaDataSaver] Arena data already exists for '{arenaId}'");
                        return false;
                    }

                    var arenaData = new ArenaData
                    {
                        ArenaId = arenaId,
                        Type = type,
                        Center = center,
                        Radius = radius,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        IsActive = true,
                        PlayerData = new Dictionary<ulong, ArenaPlayerData>(),
                        GameState = new ArenaGameState(),
                        EnvironmentData = new ArenaEnvironmentData()
                    };

                    _arenaData[arenaId] = arenaData;
                    _lastSaveTimes[arenaId] = DateTime.UtcNow;

                    Log?.LogInfo($"[ArenaDataSaver] Created arena data for '{arenaId}' at {center} with radius {radius}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaDataSaver] Failed to create arena data: {ex.Message}");
                return false;
            }
        }

        public static bool DeleteArenaData(string arenaId)
        {
            try
            {
                if (string.IsNullOrEmpty(arenaId))
                    return false;

                lock (_lock)
                {
                    if (_arenaData.Remove(arenaId))
                    {
                        _lastSaveTimes.Remove(arenaId);
                        Log?.LogInfo($"[ArenaDataSaver] Deleted arena data for '{arenaId}'");
                        return true;
                    }
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaDataSaver] Failed to delete arena data: {ex.Message}");
                return false;
            }
        }

        public static bool UpdateArenaData(string arenaId, Action<ArenaData> updateAction)
        {
            try
            {
                if (string.IsNullOrEmpty(arenaId) || updateAction == null)
                    return false;

                lock (_lock)
                {
                    if (!_arenaData.TryGetValue(arenaId, out var arenaData))
                    {
                        Log?.LogWarning($"[ArenaDataSaver] No arena data found for '{arenaId}'");
                        return false;
                    }

                    updateAction(arenaData);
                    arenaData.LastModified = DateTime.UtcNow;
                    _lastSaveTimes[arenaId] = DateTime.UtcNow;

                    Log?.LogDebug($"[ArenaDataSaver] Updated arena data for '{arenaId}'");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaDataSaver] Failed to update arena data: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Player Data Management
        public static bool AddPlayerToArena(string arenaId, Entity user, Entity character)
        {
            try
            {
                if (string.IsNullOrEmpty(arenaId) || user == Entity.Null || character == Entity.Null)
                    return false;

                var em = VAuto.Core.Core.EntityManager;
                if (!VAuto.Core.Core.TryRead<User>(user, out var userData))
                    return false;

                return UpdateArenaData(arenaId, arenaData =>
                {
                    if (!arenaData.PlayerData.ContainsKey(userData.PlatformId))
                    {
                        arenaData.PlayerData[userData.PlatformId] = new ArenaPlayerData
                        {
                            PlatformId = userData.PlatformId,
                            CharacterName = userData.CharacterName.ToString(),
                            UserEntity = user,
                            CharacterEntity = character,
                            EnteredAt = DateTime.UtcNow,
                            LastActiveAt = DateTime.UtcNow,
                            IsActive = true,
                            SessionStats = new PlayerSessionStats()
                        };
                        
                        Log?.LogInfo($"[ArenaDataSaver] Added player {userData.CharacterName} to arena '{arenaId}'");
                    }
                });
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaDataSaver] Failed to add player to arena: {ex.Message}");
                return false;
            }
        }

        public static bool RemovePlayerFromArena(string arenaId, ulong platformId)
        {
            try
            {
                if (string.IsNullOrEmpty(arenaId))
                    return false;

                return UpdateArenaData(arenaId, arenaData =>
                {
                    if (arenaData.PlayerData.TryGetValue(platformId, out var playerData))
                    {
                        playerData.LeftAt = DateTime.UtcNow;
                        playerData.IsActive = false;
                        
                        Log?.LogInfo($"[ArenaDataSaver] Removed player {playerData.CharacterName} from arena '{arenaId}'");
                    }
                });
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaDataSaver] Failed to remove player from arena: {ex.Message}");
                return false;
            }
        }

        public static bool UpdatePlayerStats(string arenaId, ulong platformId, Action<PlayerSessionStats> statsUpdate)
        {
            try
            {
                if (string.IsNullOrEmpty(arenaId) || statsUpdate == null)
                    return false;

                return UpdateArenaData(arenaId, arenaData =>
                {
                    if (arenaData.PlayerData.TryGetValue(platformId, out var playerData))
                    {
                        statsUpdate(playerData.SessionStats);
                        playerData.LastActiveAt = DateTime.UtcNow;
                    }
                });
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaDataSaver] Failed to update player stats: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Game State Management
        public static bool UpdateGameState(string arenaId, Action<ArenaGameState> stateUpdate)
        {
            try
            {
                if (string.IsNullOrEmpty(arenaId) || stateUpdate == null)
                    return false;

                return UpdateArenaData(arenaId, arenaData =>
                {
                    stateUpdate(arenaData.GameState);
                    arenaData.GameState.LastUpdate = DateTime.UtcNow;
                });
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaDataSaver] Failed to update game state: {ex.Message}");
                return false;
            }
        }

        public static bool SetArenaActive(string arenaId, bool isActive)
        {
            try
            {
                if (string.IsNullOrEmpty(arenaId))
                    return false;

                return UpdateArenaData(arenaId, arenaData =>
                {
                    arenaData.IsActive = isActive;
                    arenaData.LastModified = DateTime.UtcNow;
                    
                    Log?.LogInfo($"[ArenaDataSaver] Set arena '{arenaId}' {(isActive ? "active" : "inactive")}");
                });
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaDataSaver] Failed to set arena active state: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Data Persistence
        public static bool SaveArenaData(string arenaId)
        {
            try
            {
                if (string.IsNullOrEmpty(arenaId))
                    return false;

                lock (_lock)
                {
                    if (!_arenaData.TryGetValue(arenaId, out var arenaData))
                        return false;

                    // Use DatabaseService to persist the data
                    var success = DatabaseService.SaveArenaState(arenaId, ConvertToArenaState(arenaData));
                    
                    if (success)
                    {
                        _lastSaveTimes[arenaId] = DateTime.UtcNow;
                        Log?.LogDebug($"[ArenaDataSaver] Saved arena data for '{arenaId}'");
                    }
                    
                    return success;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaDataSaver] Failed to save arena data: {ex.Message}");
                return false;
            }
        }

        public static bool LoadArenaData(string arenaId)
        {
            try
            {
                if (string.IsNullOrEmpty(arenaId))
                    return false;

                // Use DatabaseService to load the data
                var arenaState = DatabaseService.LoadArenaState(arenaId);
                
                if (arenaState != null)
                {
                    lock (_lock)
                    {
                        _arenaData[arenaId] = ConvertFromArenaState(arenaState);
                        _lastSaveTimes[arenaId] = DateTime.UtcNow;
                        
                        Log?.LogInfo($"[ArenaDataSaver] Loaded arena data for '{arenaId}'");
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaDataSaver] Failed to load arena data: {ex.Message}");
                return false;
            }
        }

        public static void SaveAllArenaData()
        {
            try
            {
                lock (_lock)
                {
                    foreach (var arenaKvp in _arenaData)
                    {
                        SaveArenaData(arenaKvp.Key);
                    }
                    
                    Log?.LogInfo($"[ArenaDataSaver] Saved data for {_arenaData.Count} arenas");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaDataSaver] Failed to save all arena data: {ex.Message}");
            }
        }

        private static DatabaseService.ArenaState ConvertToArenaState(ArenaData arenaData)
        {
            return new DatabaseService.ArenaState
            {
                ArenaId = arenaData.ArenaId,
                Center = arenaData.Center,
                Radius = arenaData.Radius,
                Type = arenaData.Type,
                CreatedAt = arenaData.CreatedAt,
                LastModified = arenaData.LastModified,
                StateData = new Dictionary<string, object>
                {
                    ["IsActive"] = arenaData.IsActive,
                    ["PlayerCount"] = arenaData.PlayerData.Count,
                    ["GameState"] = System.Text.Json.JsonSerializer.Serialize(arenaData.GameState),
                    ["EnvironmentData"] = System.Text.Json.JsonSerializer.Serialize(arenaData.EnvironmentData)
                },
                ActivePlayers = arenaData.PlayerData.Values.Where(p => p.IsActive).Select(p => p.CharacterName).ToList(),
                IsActive = arenaData.IsActive
            };
        }

        private static ArenaData ConvertFromArenaState(DatabaseService.ArenaState arenaState)
        {
            var arenaData = new ArenaData
            {
                ArenaId = arenaState.ArenaId,
                Center = arenaState.Center,
                Radius = arenaState.Radius,
                Type = arenaState.Type,
                CreatedAt = arenaState.CreatedAt,
                LastModified = arenaState.LastModified,
                IsActive = arenaState.IsActive,
                PlayerData = new Dictionary<ulong, ArenaPlayerData>(),
                GameState = new ArenaGameState(),
                EnvironmentData = new ArenaEnvironmentData()
            };

            // Restore additional state data
            if (arenaState.StateData != null)
            {
                if (arenaState.StateData.TryGetValue("GameState", out var gameStateJson))
                {
                    try
                    {
                        arenaData.GameState = System.Text.Json.JsonSerializer.Deserialize<ArenaGameState>(gameStateJson.ToString());
                    }
                    catch { }
                }

                if (arenaState.StateData.TryGetValue("EnvironmentData", out var envDataJson))
                {
                    try
                    {
                        arenaData.EnvironmentData = System.Text.Json.JsonSerializer.Deserialize<ArenaEnvironmentData>(envDataJson.ToString());
                    }
                    catch { }
                }
            }

            return arenaData;
        }
        #endregion

        #region Query Methods
        public static List<string> GetAllArenaIds()
        {
            lock (_lock)
            {
                return _arenaData.Keys.ToList();
            }
        }

        public static ArenaData GetArenaData(string arenaId)
        {
            lock (_lock)
            {
                return _arenaData.TryGetValue(arenaId, out var arenaData) ? arenaData : null;
            }
        }

        public static List<ArenaData> GetActiveArenas()
        {
            lock (_lock)
            {
                return _arenaData.Values.Where(a => a.IsActive).ToList();
            }
        }

        public static List<ArenaPlayerData> GetArenaPlayers(string arenaId)
        {
            lock (_lock)
            {
                if (_arenaData.TryGetValue(arenaId, out var arenaData))
                {
                    return arenaData.PlayerData.Values.ToList();
                }
                return new List<ArenaPlayerData>();
            }
        }

        public static int GetArenaCount()
        {
            lock (_lock)
            {
                return _arenaData.Count;
            }
        }

        public static int GetActiveArenaCount()
        {
            lock (_lock)
            {
                return _arenaData.Values.Count(a => a.IsActive);
            }
        }

        public static DateTime GetLastSaveTime(string arenaId)
        {
            lock (_lock)
            {
                return _lastSaveTimes.TryGetValue(arenaId, out var saveTime) ? saveTime : DateTime.MinValue;
            }
        }
        #endregion

        #region Data Structures
        public class ArenaData
        {
            public string ArenaId { get; set; }
            public ArenaType Type { get; set; }
            public float3 Center { get; set; }
            public float Radius { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime LastModified { get; set; }
            public bool IsActive { get; set; }
            public Dictionary<ulong, ArenaPlayerData> PlayerData { get; set; } = new();
            public ArenaGameState GameState { get; set; } = new();
            public ArenaEnvironmentData EnvironmentData { get; set; } = new();
        }

        public class ArenaPlayerData
        {
            public ulong PlatformId { get; set; }
            public string CharacterName { get; set; }
            public Entity UserEntity { get; set; }
            public Entity CharacterEntity { get; set; }
            public DateTime EnteredAt { get; set; }
            public DateTime LastActiveAt { get; set; }
            public DateTime? LeftAt { get; set; }
            public bool IsActive { get; set; }
            public PlayerSessionStats SessionStats { get; set; } = new();
        }

        public class PlayerSessionStats
        {
            public int Kills { get; set; }
            public int Deaths { get; set; }
            public int Assists { get; set; }
            public float TotalDamageDealt { get; set; }
            public float TotalDamageReceived { get; set; }
            public int TimeAliveSeconds { get; set; }
            public int HighestKillStreak { get; set; }
            public Dictionary<string, int> AbilitiesUsed { get; set; } = new();
        }

        public class ArenaGameState
        {
            public string CurrentPhase { get; set; } = "Preparation";
            public int PhaseTimeRemaining { get; set; }
            public int ActivePlayers { get; set; }
            public bool IsMatchActive { get; set; }
            public DateTime LastUpdate { get; set; }
            public Dictionary<string, object> CustomState { get; set; } = new();
        }

        public class ArenaEnvironmentData
        {
            public float3 WeatherCenter { get; set; }
            public float WeatherIntensity { get; set; } = 0f;
            public string WeatherType { get; set; } = "Clear";
            public bool DynamicLighting { get; set; } = true;
            public Dictionary<string, object> EnvironmentSettings { get; set; } = new();
        }
        #endregion
    }
}