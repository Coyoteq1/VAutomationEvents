using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using VAuto.Core;
using VAuto.Data;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Database Service - Enhanced database operations for arena data persistence
    /// </summary>
    public static class DatabaseService
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();
        private static string _databasePath;
        
        public static bool IsInitialized => _initialized;
        public static ManualLogSource Log => Plugin.Logger;

        #region Initialization
        public static void Initialize(string databasePath = null)
        {
            lock (_lock)
            {
                if (_initialized) return;
                
                try
                {
                    Log?.LogInfo("[DatabaseService] Initializing database service...");
                    
                    _databasePath = databasePath ?? Path.Combine(Paths.BepInExRootPath, "VAuto", "ArenaDatabase.json");
                    
                    // Ensure directory exists
                    var directory = Path.GetDirectoryName(_databasePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    // Initialize database structure
                    InitializeDatabaseStructure();
                    
                    _initialized = true;
                    
                    Log?.LogInfo($"[DatabaseService] Database service initialized at {_databasePath}");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[DatabaseService] Failed to initialize: {ex.Message}");
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
                    Log?.LogInfo("[DatabaseService] Cleaning up database service...");
                    
                    // Save any pending data
                    SaveAllData();
                    
                    _initialized = false;
                    
                    Log?.LogInfo("[DatabaseService] Database service cleaned up successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[DatabaseService] Failed to cleanup: {ex.Message}");
                }
            }
        }

        private static void InitializeDatabaseStructure()
        {
            try
            {
                if (!File.Exists(_databasePath))
                {
                    var initialData = new ArenaDatabase
                    {
                        Version = "1.0.0",
                        CreatedAt = DateTime.UtcNow,
                        LastSaved = DateTime.UtcNow,
                        ArenaStates = new Dictionary<string, ArenaState>(),
                        PlayerData = new Dictionary<ulong, PlayerDatabaseData>(),
                        GlobalSettings = new GlobalDatabaseSettings()
                    };
                    
                    SaveDatabase(initialData);
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[DatabaseService] Failed to initialize database structure: {ex.Message}");
            }
        }
        #endregion

        #region Data Persistence
        public static bool SaveArenaState(string arenaId, ArenaState arenaState)
        {
            try
            {
                if (!_initialized)
                {
                    Log?.LogWarning("[DatabaseService] Database service not initialized");
                    return false;
                }

                lock (_lock)
                {
                    var database = LoadDatabase();
                    
                    arenaState.LastModified = DateTime.UtcNow;
                    database.ArenaStates[arenaId] = arenaState;
                    database.LastSaved = DateTime.UtcNow;
                    
                    SaveDatabase(database);
                    
                    Log?.LogDebug($"[DatabaseService] Saved arena state for '{arenaId}'");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[DatabaseService] Failed to save arena state: {ex.Message}");
                return false;
            }
        }

        public static ArenaState LoadArenaState(string arenaId)
        {
            try
            {
                if (!_initialized)
                    return null;

                lock (_lock)
                {
                    var database = LoadDatabase();
                    
                    if (database.ArenaStates.TryGetValue(arenaId, out var arenaState))
                    {
                        Log?.LogDebug($"[DatabaseService] Loaded arena state for '{arenaId}'");
                        return arenaState;
                    }
                    
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[DatabaseService] Failed to load arena state: {ex.Message}");
                return null;
            }
        }

        public static bool SavePlayerData(ulong platformId, PlayerDatabaseData playerData)
        {
            try
            {
                if (!_initialized)
                    return false;

                lock (_lock)
                {
                    var database = LoadDatabase();
                    
                    playerData.LastUpdated = DateTime.UtcNow;
                    database.PlayerData[platformId] = playerData;
                    database.LastSaved = DateTime.UtcNow;
                    
                    SaveDatabase(database);
                    
                    Log?.LogDebug($"[DatabaseService] Saved player data for {platformId}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[DatabaseService] Failed to save player data: {ex.Message}");
                return false;
            }
        }

        public static PlayerDatabaseData LoadPlayerData(ulong platformId)
        {
            try
            {
                if (!_initialized)
                    return null;

                lock (_lock)
                {
                    var database = LoadDatabase();
                    
                    if (database.PlayerData.TryGetValue(platformId, out var playerData))
                    {
                        Log?.LogDebug($"[DatabaseService] Loaded player data for {platformId}");
                        return playerData;
                    }
                    
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[DatabaseService] Failed to load player data: {ex.Message}");
                return null;
            }
        }

        public static bool SaveGlobalSettings(GlobalDatabaseSettings settings)
        {
            try
            {
                if (!_initialized)
                    return false;

                lock (_lock)
                {
                    var database = LoadDatabase();
                    
                    database.GlobalSettings = settings;
                    database.LastSaved = DateTime.UtcNow;
                    
                    SaveDatabase(database);
                    
                    Log?.LogDebug("[DatabaseService] Saved global settings");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[DatabaseService] Failed to save global settings: {ex.Message}");
                return false;
            }
        }

        public static GlobalDatabaseSettings LoadGlobalSettings()
        {
            try
            {
                if (!_initialized)
                    return new GlobalDatabaseSettings();

                lock (_lock)
                {
                    var database = LoadDatabase();
                    return database.GlobalSettings ?? new GlobalDatabaseSettings();
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[DatabaseService] Failed to load global settings: {ex.Message}");
                return new GlobalDatabaseSettings();
            }
        }
        #endregion

        #region Data Management
        public static bool DeleteArenaState(string arenaId)
        {
            try
            {
                if (!_initialized)
                    return false;

                lock (_lock)
                {
                    var database = LoadDatabase();
                    
                    if (database.ArenaStates.Remove(arenaId))
                    {
                        SaveDatabase(database);
                        Log?.LogInfo($"[DatabaseService] Deleted arena state for '{arenaId}'");
                        return true;
                    }
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[DatabaseService] Failed to delete arena state: {ex.Message}");
                return false;
            }
        }

        public static bool DeletePlayerData(ulong platformId)
        {
            try
            {
                if (!_initialized)
                    return false;

                lock (_lock)
                {
                    var database = LoadDatabase();
                    
                    if (database.PlayerData.Remove(platformId))
                    {
                        SaveDatabase(database);
                        Log?.LogInfo($"[DatabaseService] Deleted player data for {platformId}");
                        return true;
                    }
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[DatabaseService] Failed to delete player data: {ex.Message}");
                return false;
            }
        }
            public static void SaveAllData()
        {
            try
            {
                if (!_initialized)
                    return;

                lock (_lock)
                {
                    var database = LoadDatabase();
                    database.LastSaved = DateTime.UtcNow;
                    SaveDatabase(database);
                    
                    Log?.LogDebug("[DatabaseService] Saved all data");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[DatabaseService] Failed to save all data: {ex.Message}");
            }
        }

        public static void ClearAllData()
        {
            try
            {
                if (!_initialized)
                    return;

                lock (_lock)
                {
                    var database = new ArenaDatabase
                    {
                        Version = "1.0.0",
                        CreatedAt = DateTime.UtcNow,
                        LastSaved = DateTime.UtcNow,
                        ArenaStates = new Dictionary<string, ArenaState>(),
                        PlayerData = new Dictionary<ulong, PlayerDatabaseData>(),
                        GlobalSettings = new GlobalDatabaseSettings()
                    };
                    
                    SaveDatabase(database);
                    
                    Log?.LogInfo("[DatabaseService] Cleared all data");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[DatabaseService] Failed to clear all data: {ex.Message}");
            }
        }
        #endregion

        #region Query Methods
        public static List<string> GetAllArenaIds()
        {
            try
            {
                if (!_initialized)
                    return new List<string>();

                lock (_lock)
                {
                    var database = LoadDatabase();
                    return database.ArenaStates.Keys.ToList();
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[DatabaseService] Failed to get arena IDs: {ex.Message}");
                return new List<string>();
            }
        }

        public static List<ulong> GetAllPlayerIds()
        {
            try
            {
                if (!_initialized)
                    return new List<ulong>();

                lock (_lock)
                {
                    var database = LoadDatabase();
                    return database.PlayerData.Keys.ToList();
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[DatabaseService] Failed to get player IDs: {ex.Message}");
                return new List<ulong>();
            }
        }

        public static int GetArenaCount()
        {
            try
            {
                if (!_initialized)
                    return 0;

                lock (_lock)
                {
                    var database = LoadDatabase();
                    return database.ArenaStates.Count;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[DatabaseService] Failed to get arena count: {ex.Message}");
                return 0;
            }
        }

        public static int GetPlayerCount()
        {
            try
            {
                if (!_initialized)
                    return 0;

                lock (_lock)
                {
                    var database = LoadDatabase();
                    return database.PlayerData.Count;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[DatabaseService] Failed to get player count: {ex.Message}");
                return 0;
            }
        }

        public static long GetDatabaseSize()
        {
            try
            {
                if (!File.Exists(_databasePath))
                    return 0;

                var fileInfo = new FileInfo(_databasePath);
                return fileInfo.Length;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[DatabaseService] Failed to get database size: {ex.Message}");
                return 0;
            }
        }

        public static DateTime GetLastSavedTime()
        {
            try
            {
                if (!_initialized)
                    return DateTime.MinValue;

                lock (_lock)
                {
                    var database = LoadDatabase();
                    return database.LastSaved;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[DatabaseService] Failed to get last saved time: {ex.Message}");
                return DateTime.MinValue;
            }
        }
        #endregion

        #region File Operations
        private static ArenaDatabase LoadDatabase()
        {
            try
            {
                if (!File.Exists(_databasePath))
                {
                    InitializeDatabaseStructure();
                }

                var jsonContent = File.ReadAllText(_databasePath);
                return JsonSerializer.Deserialize<ArenaDatabase>(jsonContent) ?? new ArenaDatabase();
            }
            catch (Exception ex)
            {
                Log?.LogError($"[DatabaseService] Failed to load database: {ex.Message}");
                return new ArenaDatabase();
            }
        }

        private static void SaveDatabase(ArenaDatabase database)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var jsonContent = JsonSerializer.Serialize(database, options);
                File.WriteAllText(_databasePath, jsonContent);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[DatabaseService] Failed to save database: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region Data Structures
        public class ArenaDatabase
        {
            public string Version { get; set; } = "1.0.0";
            public DateTime CreatedAt { get; set; }
            public DateTime LastSaved { get; set; }
            public Dictionary<string, ArenaState> ArenaStates { get; set; } = new();
            public Dictionary<ulong, PlayerDatabaseData> PlayerData { get; set; } = new();
            public GlobalDatabaseSettings GlobalSettings { get; set; } = new();
        }

        public class ArenaState
        {
            public string ArenaId { get; set; }
            public float3 Center { get; set; }
            public float Radius { get; set; }
            public ArenaType Type { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime LastModified { get; set; }
            public Dictionary<string, object> StateData { get; set; } = new();
            public List<string> ActivePlayers { get; set; } = new();
            public bool IsActive { get; set; }
        }

        public class PlayerDatabaseData
        {
            public ulong PlatformId { get; set; }
            public string CharacterName { get; set; }
            public DateTime FirstSeen { get; set; }
            public DateTime LastSeen { get; set; }
            public DateTime LastUpdated { get; set; }
            public Dictionary<string, object> PlayerStats { get; set; } = new();
            public List<string> ArenaVisits { get; set; } = new();
            public Dictionary<string, object> Preferences { get; set; } = new();
        }

        public class GlobalDatabaseSettings
        {
            public bool AutoSave { get; set; } = true;
            public int SaveIntervalMinutes { get; set; } = 5;
            public bool BackupEnabled { get; set; } = true;
            public int MaxBackups { get; set; } = 10;
            public bool CompressionEnabled { get; set; } = false;
            public Dictionary<string, object> CustomSettings { get; set; } = new();
        }
        #endregion
    }
}