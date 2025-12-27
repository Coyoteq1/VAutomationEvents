using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Unity.Mathematics;
using BepInEx.Logging;

namespace VAuto.Services
{
    /// <summary>
    /// Configuration management for all services
    /// </summary>
    public static class ServiceConfiguration
    {
        private static bool _initialized = false;
        private static ServiceConfig _config;
        private static readonly object _lock = new object();
        private static ManualLogSource _log;
        
        public static bool IsInitialized => _initialized;
        public static ManualLogSource Log => _log;

        #region Configuration Structure
        public class ServiceConfig
        {
            public GlobalConfig Global { get; set; } = new();
            public AutoEnterConfig AutoEnter { get; set; } = new();
            public ArenaGlowConfig ArenaGlow { get; set; } = new();
            public ArenaBuildConfig ArenaBuild { get; set; } = new();
            public CastleObjectConfig CastleObject { get; set; } = new();
            public DatabaseConfig Database { get; set; } = new();
            public DataPersistenceConfig DataPersistence { get; set; } = new();
            public ArenaSnapshotConfig ArenaSnapshot { get; set; } = new();
            public ComponentSaverConfig ComponentSaver { get; set; } = new();
            public ArenaDataSaverConfig ArenaDataSaver { get; set; } = new();
            public ArenaObjectConfig ArenaObject { get; set; } = new();
        }

        public class GlobalConfig
        {
            public bool EnableAllServices { get; set; } = true;
            public bool EnableDebugLogging { get; set; } = false;
            public bool EnableHealthMonitoring { get; set; } = true;
            public int ServiceUpdateIntervalMs { get; set; } = 100;
            public bool EnableServiceDependencyManagement { get; set; } = true;
        }

        public class AutoEnterConfig
        {
            public bool Enabled { get; set; } = true;
            public float CooldownSeconds { get; set; } = 5.0f;
            public bool AutoEnableForNewPlayers { get; set; } = false;
            public float EnterRadius { get; set; } = 20.0f;
            public List<string> AllowedArenas { get; set; } = new();
        }

        public class ArenaGlowConfig
        {
            public bool Enabled { get; set; } = true;
            public bool EnableDynamicEffects { get; set; } = true;
            public float DefaultIntensity { get; set; } = 1.0f;
            public int MaxGlowsPerArena { get; set; } = 50;
            public bool EnablePerformanceOptimizations { get; set; } = true;
            public Dictionary<string, GlowPreset> Presets { get; set; } = new();
        }

        public class GlowPreset
        {
            public float3 Position { get; set; }
            public float Radius { get; set; }
            public float4 Color { get; set; }
            public string Type { get; set; } = "Point";
            public bool Active { get; set; } = true;
        }

        public class ArenaBuildConfig
        {
            public bool Enabled { get; set; } = true;
            public bool EnableStructureTypes { get; set; } = true;
            public int MaxStructuresPerPlayer { get; set; } = 20;
            public float BuildRange { get; set; } = 10.0f;
            public Dictionary<string, StructureConfig> AvailableStructures { get; set; } = new();
            public bool RequireBuildPermission { get; set; } = true;
        }

        public class StructureConfig
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public bool Enabled { get; set; } = true;
            public float Cost { get; set; } = 0;
            public List<string> RequiredPermissions { get; set; } = new();
        }

        public class CastleObjectConfig
        {
            public bool Enabled { get; set; } = true;
            public bool EnableArenaTransfer { get; set; } = true;
            public int MaxObjectsPerArena { get; set; } = 100;
            public bool EnableObjectPersistence { get; set; } = true;
            public List<string> AllowedObjectTypes { get; set; } = new();
        }

        public class DatabaseConfig
        {
            public bool Enabled { get; set; } = true;
            public string DatabasePath { get; set; } = "VAuto/ArenaDatabase.json";
            public bool EnableAutoSave { get; set; } = true;
            public int AutoSaveIntervalMinutes { get; set; } = 5;
            public bool EnableBackup { get; set; } = true;
            public int MaxBackups { get; set; } = 10;
            public bool EnableCompression { get; set; } = false;
        }

        public class DataPersistenceConfig
        {
            public bool Enabled { get; set; } = true;
            public bool EnableAutoSave { get; set; } = true;
            public int SaveIntervalSeconds { get; set; } = 300;
            public int MaxPendingSaves { get; set; } = 1000;
            public bool EnableBackup { get; set; } = true;
            public string BackupDirectory { get; set; } = "VAuto/Backups";
        }

        public class ArenaSnapshotConfig
        {
            public bool Enabled { get; set; } = true;
            public bool AutoCreateSnapshots { get; set; } = true;
            public bool AutoRestoreSnapshots { get; set; } = true;
            public int MaxSnapshotsPerPlayer { get; set; } = 5;
            public bool IncludeInventory { get; set; } = true;
            public bool IncludeEquipment { get; set; } = true;
            public bool IncludeAbilities { get; set; } = true;
            public bool IncludeProgression { get; set; } = true;
        }

        public class ComponentSaverConfig
        {
            public bool Enabled { get; set; } = true;
            public bool AutoSaveOnChange { get; set; } = true;
            public int MaxSavedComponents { get; set; } = 10000;
            public List<string> IncludeComponentTypes { get; set; } = new();
            public List<string> ExcludeComponentTypes { get; set; } = new();
        }

        public class ArenaDataSaverConfig
        {
            public bool Enabled { get; set; } = true;
            public bool AutoSaveOnChange { get; set; } = true;
            public bool EnablePlayerTracking { get; set; } = true;
            public bool EnableGameStateTracking { get; set; } = true;
            public bool EnableEnvironmentTracking { get; set; } = true;
            public int MaxSavedArenaStates { get; set; } = 100;
        }

        public class ArenaObjectConfig
        {
            public bool Enabled { get; set; } = true;
            public int MaxObjectsPerArena { get; set; } = 1000;
            public bool EnableObjectTracking { get; set; } = true;
            public bool EnableBulkOperations { get; set; } = true;
            public Dictionary<string, ObjectTypeConfig> ObjectTypes { get; set; } = new();
        }

        public class ObjectTypeConfig
        {
            public string Name { get; set; }
            public bool Enabled { get; set; } = true;
            public int MaxInstances { get; set; } = -1; // -1 = unlimited
            public List<string> RequiredPermissions { get; set; } = new();
        }
        #endregion

        #region Initialization
        public static void Initialize(ManualLogSource logger, string configPath = null)
        {
            lock (_lock)
            {
                if (_initialized) return;
                
                try
                {
                    _log = logger;
                    
                    var path = configPath ?? GetDefaultConfigPath();
                    LoadConfiguration(path);
                    
                    _initialized = true;
                    
                    logger?.LogInfo("[ServiceConfiguration] Service configuration initialized successfully");
                }
                catch (Exception ex)
                {
                    logger?.LogError($"[ServiceConfiguration] Failed to initialize: {ex.Message}");
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
                    _log?.LogInfo("[ServiceConfiguration] Cleaning up service configuration...");
                    
                    // Save configuration before cleanup
                    SaveConfiguration();
                    
                    _config = null;
                    _initialized = false;
                    
                    _log?.LogInfo("[ServiceConfiguration] Service configuration cleaned up successfully");
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[ServiceConfiguration] Failed to cleanup: {ex.Message}");
                }
            }
        }
        #endregion

        #region Configuration Management
        public static void LoadConfiguration(string configPath)
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var jsonContent = File.ReadAllText(configPath);
                    _config = JsonSerializer.Deserialize<ServiceConfig>(jsonContent) ?? new ServiceConfig();
                    _log?.LogInfo($"[ServiceConfiguration] Loaded configuration from {configPath}");
                }
                else
                {
                    // Create default configuration
                    _config = CreateDefaultConfiguration();
                    SaveConfiguration(configPath);
                    _log?.LogInfo($"[ServiceConfiguration] Created default configuration at {configPath}");
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[ServiceConfiguration] Failed to load configuration: {ex.Message}");
                _config = CreateDefaultConfiguration();
            }
        }

        public static void SaveConfiguration(string configPath = null)
        {
            try
            {
                var path = configPath ?? GetDefaultConfigPath();
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var jsonContent = JsonSerializer.Serialize(_config, options);
                File.WriteAllText(path, jsonContent);
                
                _log?.LogDebug($"[ServiceConfiguration] Saved configuration to {path}");
            }
            catch (Exception ex)
            {
                _log?.LogError($"[ServiceConfiguration] Failed to save configuration: {ex.Message}");
            }
        }

        public static ServiceConfig GetConfiguration()
        {
            lock (_lock)
            {
                return _config;
            }
        }

        public static T GetConfigSection<T>() where T : class
        {
            lock (_lock)
            {
                return _config as T;
            }
        }

        public static void UpdateConfiguration(Action<ServiceConfig> updateAction)
        {
            lock (_lock)
            {
                updateAction(_config);
                _log?.LogInfo("[ServiceConfiguration] Configuration updated");
            }
        }

        private static ServiceConfig CreateDefaultConfiguration()
        {
            var config = new ServiceConfig();
            
            // Set up default glow presets
            config.ArenaGlow.Presets["center"] = new GlowPreset
            {
                Position = new float3(0, 10, 0),
                Radius = 50,
                Color = new float4(0.2f, 0.8f, 1.0f, 0.3f),
                Type = "Circular",
                Active = true
            };
            
            config.ArenaGlow.Presets["boundary"] = new GlowPreset
            {
                Position = new float3(0, 2, 0),
                Radius = 60,
                Color = new float4(1.0f, 0.5f, 0.0f, 0.2f),
                Type = "Boundary",
                Active = false
            };

            // Set up default structures
            config.ArenaBuild.AvailableStructures["wall"] = new StructureConfig
            {
                Name = "Wall",
                Type = "Wall",
                Enabled = true,
                Cost = 0
            };
            
            config.ArenaBuild.AvailableStructures["floor"] = new StructureConfig
            {
                Name = "Floor",
                Type = "Floor",
                Enabled = true,
                Cost = 0
            };
            
            config.ArenaBuild.AvailableStructures["portal"] = new StructureConfig
            {
                Name = "Portal",
                Type = "Portal",
                Enabled = true,
                Cost = 0
            };

            return config;
        }

        private static string GetDefaultConfigPath()
        {
            return Path.Combine(BepInEx.Paths.BepInExRootPath, "VAuto", "ServiceConfig.json");
        }
        #endregion

        #region Configuration Validation
        public static bool ValidateConfiguration(out List<string> errors)
        {
            errors = new List<string>();
            
            try
            {
                // Validate global config
                if (_config.Global.ServiceUpdateIntervalMs < 10)
                    errors.Add("Service update interval must be at least 10ms");

                // Validate auto-enter config
                if (_config.AutoEnter.CooldownSeconds < 1)
                    errors.Add("Auto-enter cooldown must be at least 1 second");

                // Validate arena glow config
                if (_config.ArenaGlow.DefaultIntensity < 0 || _config.ArenaGlow.DefaultIntensity > 10)
                    errors.Add("Arena glow intensity must be between 0 and 10");

                // Validate arena build config
                if (_config.ArenaBuild.MaxStructuresPerPlayer < 1)
                    errors.Add("Max structures per player must be at least 1");

                // Validate database config
                if (_config.Database.AutoSaveIntervalMinutes < 1)
                    errors.Add("Database auto-save interval must be at least 1 minute");

                // Validate data persistence config
                if (_config.DataPersistence.SaveIntervalSeconds < 10)
                    errors.Add("Data persistence save interval must be at least 10 seconds");

                return errors.Count == 0;
            }
            catch (Exception ex)
            {
                errors.Add($"Configuration validation error: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Runtime Configuration
        public static bool IsServiceEnabled(string serviceName)
        {
            lock (_lock)
            {
                return serviceName.ToLower() switch
                {
                    "autoenter" => _config.AutoEnter.Enabled,
                    "arenaglow" => _config.ArenaGlow.Enabled,
                    "arenabuild" => _config.ArenaBuild.Enabled,
                    "castleobject" => _config.CastleObject.Enabled,
                    "database" => _config.Database.Enabled,
                    "datapersistence" => _config.DataPersistence.Enabled,
                    "arenasnapshot" => _config.ArenaSnapshot.Enabled,
                    "componentsaver" => _config.ComponentSaver.Enabled,
                    "arenadatasaver" => _config.ArenaDataSaver.Enabled,
                    "arenaobject" => _config.ArenaObject.Enabled,
                    _ => true // Default to enabled for unknown services
                };
            }
        }

        public static void SetServiceEnabled(string serviceName, bool enabled)
        {
            lock (_lock)
            {
                switch (serviceName.ToLower())
                {
                    case "autoenter":
                        _config.AutoEnter.Enabled = enabled;
                        break;
                    case "arenaglow":
                        _config.ArenaGlow.Enabled = enabled;
                        break;
                    case "arenabuild":
                        _config.ArenaBuild.Enabled = enabled;
                        break;
                    case "castleobject":
                        _config.CastleObject.Enabled = enabled;
                        break;
                    case "database":
                        _config.Database.Enabled = enabled;
                        break;
                    case "datapersistence":
                        _config.DataPersistence.Enabled = enabled;
                        break;
                    case "arenasnapshot":
                        _config.ArenaSnapshot.Enabled = enabled;
                        break;
                    case "componentsaver":
                        _config.ComponentSaver.Enabled = enabled;
                        break;
                    case "arenadatasaver":
                        _config.ArenaDataSaver.Enabled = enabled;
                        break;
                    case "arenaobject":
                        _config.ArenaObject.Enabled = enabled;
                        break;
                }
                
                _log?.LogInfo($"[ServiceConfiguration] Service '{serviceName}' {(enabled ? "enabled" : "disabled")}");
            }
        }

        public static T GetServiceConfig<T>(string serviceName) where T : class
        {
            lock (_lock)
            {
                return serviceName.ToLower() switch
                {
                    "autoenter" => _config.AutoEnter as T,
                    "arenaglow" => _config.ArenaGlow as T,
                    "arenabuild" => _config.ArenaBuild as T,
                    "castleobject" => _config.CastleObject as T,
                    "database" => _config.Database as T,
                    "datapersistence" => _config.DataPersistence as T,
                    "arenasnapshot" => _config.ArenaSnapshot as T,
                    "componentsaver" => _config.ComponentSaver as T,
                    "arenadatasaver" => _config.ArenaDataSaver as T,
                    "arenaobject" => _config.ArenaObject as T,
                    _ => null
                };
            }
        }
        #endregion
    }
}