using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using VampireCommandFramework;
using System;
using System.IO;
using Unity.Mathematics;
using VAuto.Core;
using VAuto.Services;

namespace VAuto;

/// <summary>
/// Main plugin class for VAuto.Arena, a BepInEx mod for VRising that provides arena functionality
/// including player lifecycle management, build loading, and game modifications.
/// </summary>
[BepInPlugin("gg.Automation.arena", "VAutomationEvents", "1.0.0")]
[BepInDependency("gg.deca.VampireCommandFramework")]
[BepInProcess("VRising.exe")]
[BepInProcess("VRisingServer.exe")]
public class Plugin : BasePlugin
{
    internal static BepInEx.Logging.ManualLogSource Log;
    internal static BepInEx.Configuration.ConfigFile Cfg;

    private static Harmony _harmony;
    public static Harmony Harmony => _harmony;
    public static BepInEx.Logging.ManualLogSource Logger => Log;
    public static Plugin Instance { get; private set; }
    public static string DataPath => Path.Combine(Paths.ConfigPath, "VAuto");
    public static string ConfigPath => Path.Combine(Paths.ConfigPath, "VAuto");

    // Configuration manager will be added back after build issues are resolved

    /// <summary>
    /// Plugin configuration settings with validation and proper defaults
    /// </summary>
    public static class Config
    {
        #region General Settings
        /// <summary>Enable or disable the entire plugin</summary>
        public static bool Enable { get; set; } = true;

        /// <summary>Logging level: Debug, Info, Warning, Error</summary>
        public static string LogLevel { get; set; } = "Info";

        /// <summary>Enable debug mode for additional logging and features</summary>
        public static bool DebugMode { get; set; } = false;
        #endregion

        #region Arena Settings
        /// <summary>Enable arena functionality</summary>
        public static bool ArenaEnable { get; set; } = true;

        /// <summary>Arena center coordinates (x, y, z)</summary>
        public static float3 ArenaCenter { get; set; } = new float3(-1000, -5, -500);

        /// <summary>Arena radius in meters</summary>
        public static float ArenaRadius { get; set; } = 50.0f;

        /// <summary>Radius for automatically entering arena zone</summary>
        public static float ArenaEnterRadius { get; set; } = 50.0f;

        /// <summary>Radius for automatically exiting arena zone (1.5x enter radius)</summary>
        public static float ArenaExitRadius { get; set; } = 75.0f;

        /// <summary>Interval in seconds for checking arena status</summary>
        public static float ArenaCheckInterval { get; set; } = 2.0f;
        #endregion



        #region Lifecycle Settings
        /// <summary>Enable player respawn system</summary>
        public static bool RespawnEnable { get; set; } = true;

        /// <summary>Respawn cooldown in seconds (minimum: 5, maximum: 300)</summary>
        public static int RespawnCooldown { get; set; } = 30;
        #endregion

        #region Database Settings
        /// <summary>Enable database functionality</summary>
        public static bool DatabaseEnable { get; set; } = true;

        /// <summary>Database type: SQLite, MySQL, PostgreSQL</summary>
        public static string DatabaseType { get; set; } = "SQLite";

        /// <summary>Database file path (for SQLite)</summary>
        public static string DatabasePath { get; set; } = "./VAuto_Data/database.db";
        #endregion

        #region Advanced Settings
        /// <summary>Maximum number of players allowed in arena (0 = unlimited)</summary>
        public static int MaxArenaPlayers { get; set; } = 0;

        /// <summary>Arena damage multiplier (1.0 = normal)</summary>
        public static float ArenaDamageMultiplier { get; set; } = 1.0f;

        /// <summary>Enable automatic blood type conversion in arena</summary>
        public static bool ArenaAutoBloodType { get; set; } = true;

        /// <summary>Practice mode duration in minutes (0 = unlimited)</summary>
        public static int PracticeModeDuration { get; set; } = 0;

        /// <summary>Enable VBlood unlock system in arena</summary>
        public static bool ArenaVBloodUnlock { get; set; } = true;
        #endregion
    }

    public override void Load()
    {
        Instance = this;

        // Initialize static Log property using BasePlugin's Log
        Log = BepInEx.Logging.Logger.CreateLogSource("VAuto.Arena");
        Log.LogInfo("==========================================");
        Log.LogInfo("=== VAuto Plugin Loading Sequence Start ===");
        Log.LogInfo("==========================================");

        var startTime = DateTime.UtcNow;
        Log.LogInfo($"[VAuto] Load start timestamp: {startTime:yyyy-MM-dd HH:mm:ss.fff} UTC");

        Log.LogInfo($"[VAuto] Plugin Info:");
        Log.LogInfo($"  - GUID: {MyPluginInfo.PLUGIN_GUID}");
        Log.LogInfo($"  - Name: {MyPluginInfo.PLUGIN_NAME}");
        Log.LogInfo($"  - Version: {MyPluginInfo.PLUGIN_VERSION}");
        Log.LogInfo($"[VAuto] Paths:");
        Log.LogInfo($"  - Data Path: {DataPath}");
        Log.LogInfo($"  - Config Path: {ConfigPath}");
        Log.LogInfo($"  - Current Directory: {Directory.GetCurrentDirectory()}");

        try
        {
            // Step 1: Load basic configuration
            Log.LogInfo("==========================================");
            Log.LogInfo("[VAuto] STEP 1: Loading configuration...");
            var configStartTime = DateTime.UtcNow;
            LoadBasicConfiguration();
            var configDuration = DateTime.UtcNow - configStartTime;
            Log.LogInfo($"[VAuto] ✓ Configuration loaded successfully in {configDuration.TotalMilliseconds:F2}ms");
            Log.LogInfo($"[VAuto] Current configuration state:");
            Log.LogInfo($"  - Plugin Enabled: {Config.Enable}");
            Log.LogInfo($"  - Debug Mode: {Config.DebugMode}");
            Log.LogInfo($"  - Log Level: {Config.LogLevel}");

            // Step 2: Validate configuration
            Log.LogInfo("==========================================");
            Log.LogInfo("[VAuto] STEP 2: Validating configuration...");
            if (!Config.Enable)
            {
                Log.LogWarning("[VAuto] ⚠️ Plugin is DISABLED in configuration - stopping load sequence");
                Log.LogWarning("[VAuto] To enable the plugin, set 'Enable = true' in the config file");
                return;
            }
            Log.LogInfo("[VAuto] ✓ Configuration validation passed");

            // Step 3: Initialize core systems
            Log.LogInfo("==========================================");
            Log.LogInfo("[VAuto] STEP 3: Initializing core systems...");
            var coreStartTime = DateTime.UtcNow;
            InitializeCoreSystems();
            var coreDuration = DateTime.UtcNow - coreStartTime;
            Log.LogInfo($"[VAuto] ✓ Core systems initialized in {coreDuration.TotalMilliseconds:F2}ms");

            // Step 4: Initialize services
            Log.LogInfo("==========================================");
            Log.LogInfo("[VAuto] STEP 4: Initializing services...");
            var servicesStartTime = DateTime.UtcNow;
            try
            {
                InitializeServices();
                var servicesDuration = DateTime.UtcNow - servicesStartTime;
                Log.LogInfo($"[VAuto] ✓ All services initialized in {servicesDuration.TotalMilliseconds:F2}ms");
            }
            catch (Exception serviceEx)
            {
                Log.LogError($"[VAuto] ✗ SERVICE INITIALIZATION FAILED: {serviceEx.Message}");
                Log.LogError($"[VAuto] Service exception details:");
                Log.LogError($"  - Type: {serviceEx.GetType().Name}");
                Log.LogError($"  - Source: {serviceEx.Source}");
                Log.LogError($"  - Stack trace: {serviceEx.StackTrace}");
                Log.LogWarning("[VAuto] ⚠️ Continuing with plugin load despite service initialization failure");
                Log.LogWarning("[VAuto] Some features may not work correctly");
            }

            // Step 5: Harmony patching
            Log.LogInfo("==========================================");
            Log.LogInfo("[VAuto] STEP 5: Applying Harmony patches...");
            var harmonyStartTime = DateTime.UtcNow;
            _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            _harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            var harmonyDuration = DateTime.UtcNow - harmonyStartTime;
            Log.LogInfo($"[VAuto] ✓ Harmony patches applied in {harmonyDuration.TotalMilliseconds:F2}ms");

            // Step 6: Register commands
            Log.LogInfo("==========================================");
            Log.LogInfo("[VAuto] STEP 6: Registering commands...");
            var commandStartTime = DateTime.UtcNow;
            CommandRegistry.RegisterAll();
            var commandDuration = DateTime.UtcNow - commandStartTime;
            Log.LogInfo($"[VAuto] ✓ Commands registered in {commandDuration.TotalMilliseconds:F2}ms");

            // Step 7: Initialize game-specific systems
            Log.LogInfo("==========================================");
            Log.LogInfo("[VAuto] STEP 7: Initializing game systems...");
            var gameStartTime = DateTime.UtcNow;
            InitializeGameSystems();
            var gameDuration = DateTime.UtcNow - gameStartTime;
            Log.LogInfo($"[VAuto] ✓ Game systems initialized in {gameDuration.TotalMilliseconds:F2}ms");

            var totalDuration = DateTime.UtcNow - startTime;
            Log.LogInfo("==========================================");
            Log.LogInfo("=== VAuto Plugin Loaded Successfully ===");
            Log.LogInfo($"[VAuto] Total load time: {totalDuration.TotalSeconds:F2} seconds");
            Log.LogInfo($"[VAuto] System Status:");
            Log.LogInfo($"  - Arena System: {(Config.ArenaEnable ? "ENABLED" : "DISABLED")}");
            Log.LogInfo($"  - Database: {(Config.DatabaseEnable ? "ENABLED" : "DISABLED")}");
            Log.LogInfo($"  - Respawn System: {(Config.RespawnEnable ? "ENABLED" : "DISABLED")}");
            Log.LogInfo($"[VAuto] Arena Configuration:");
            Log.LogInfo($"  - Center: {Config.ArenaCenter}");
            Log.LogInfo($"  - Radius: {Config.ArenaRadius:F1} meters");
            Log.LogInfo($"  - Damage Multiplier: {Config.ArenaDamageMultiplier:F2}x");
            Log.LogInfo("==========================================");

        }
        catch (Exception ex)
        {
            Log.LogError("==========================================");
            Log.LogError("=== VAuto CRITICAL LOAD FAILURE ===");
            Log.LogError($"[VAuto] Exception: {ex.Message}");
            Log.LogError($"[VAuto] Exception Type: {ex.GetType().Name}");
            Log.LogError($"[VAuto] Source: {ex.Source}");
            Log.LogError($"[VAuto] Target Site: {ex.TargetSite}");
            Log.LogError($"[VAuto] Stack Trace:");
            Log.LogError(ex.StackTrace);
            Log.LogError("==========================================");
            throw;
        }
    }

    public override bool Unload()
    {
        Log.LogInfo("[VAuto] Unloading plugin...");

        try
        {
            // Cleanup services
            UnifiedServiceManager.Cleanup();

            // Unregister commands
            CommandRegistry.UnregisterAssembly();

            // Unpatch Harmony
            _harmony?.UnpatchSelf();

            Log.LogInfo("[VAuto] Plugin unloaded successfully");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogError($"[VAuto] Error during plugin unload: {ex.Message}");
            return false;
        }
    }

    private void LoadBasicConfiguration()
    {
        try
        {
            // Initialize BepInEx ConfigFile
            string configPath = Path.Combine(Paths.ConfigPath, "gg.Automation.arena.cfg");
            Log.LogInfo($"[VAuto] Loading config from: {configPath}");

            if (!File.Exists(configPath))
            {
                Log.LogWarning($"[VAuto] Config file not found at {configPath}, creating with defaults");
                
                // Create default config file
                CreateDefaultConfigFile(configPath);
            }

            Cfg = new BepInEx.Configuration.ConfigFile(configPath, true);
            Log.LogInfo($"[VAuto] Config file loaded successfully");

            // Load configuration values using Config.Bind with validation
            Config.Enable = Cfg.Bind("General", "Enable", true, "Enable or disable the entire plugin").Value;
            Config.LogLevel = Cfg.Bind("General", "LogLevel", "Info", "Logging level (Debug, Info, Warning, Error)").Value;
            Config.DebugMode = Cfg.Bind("General", "DebugMode", false, "Enable debug mode for additional logging and features").Value;

            // Arena settings with validation
            Config.ArenaEnable = Cfg.Bind("Arena", "ArenaEnable", true, "Enable arena functionality").Value;
            string centerStr = Cfg.Bind("Arena", "Center", "-1000, 5, -500", "Arena center coordinates as 'x, y, z'").Value;
            Log.LogInfo($"[VAuto] Raw center string from config: '{centerStr}'");
            Config.ArenaCenter = ParseFloat3(centerStr);
            
            // Validate arena center
            if (Config.ArenaCenter.Equals(float3.zero))
            {
                Log.LogWarning("[VAuto] Arena center is (0,0,0), using default");
                Config.ArenaCenter = new float3(-1000, -5, -500);
            }
            
            Config.ArenaRadius = Math.Max(1.0f, Cfg.Bind("Arena", "Radius", 50.0f, "Arena radius in meters (minimum: 10)").Value);
            Config.ArenaEnterRadius = Math.Max(1.0f, Cfg.Bind("Arena", "ArenaEnterRadius", 50.0f, "Radius for entering arena zone").Value);
            Config.ArenaExitRadius = Math.Max(1.0f, Cfg.Bind("Arena", "ArenaExitRadius", 75.0f, "Radius for exiting arena zone (1.5x enter radius)").Value);
            Config.ArenaCheckInterval = Math.Max(0.5f, Cfg.Bind("Arena", "ArenaCheckInterval", 2.0f, "Interval for checking arena status in seconds").Value);

            // Respawn settings with validation
            Config.RespawnEnable = Cfg.Bind("LifecycleSystem", "Enabled", true, "Enable player respawn system").Value;
            Config.RespawnCooldown = Math.Clamp(Cfg.Bind("LifecycleSystem", "RespawnCooldown", 30, "Respawn cooldown in seconds").Value, 5, 300);

            // Database settings
            Config.DatabaseEnable = Cfg.Bind("Data", "DatabaseEnable", true, "Enable database functionality").Value;
            Config.DatabaseType = Cfg.Bind("Data", "DatabaseType", "SQLite", "Database type (SQLite, MySQL, PostgreSQL)").Value;
            Config.DatabasePath = Cfg.Bind("Data", "DatabasePath", "./VAuto_Data/database.db", "Database file path").Value;

            // Advanced settings
            Config.MaxArenaPlayers = Cfg.Bind("Arena", "MaxPlayers", 0, "Maximum players allowed in arena (0 = unlimited)").Value;
            Config.ArenaDamageMultiplier = Math.Max(0.1f, Cfg.Bind("Arena", "DamageMultiplier", 1.0f, "Arena damage multiplier (0.1 = 10% damage)").Value);
            Config.ArenaAutoBloodType = Cfg.Bind("Arena", "AutoBloodType", true, "Automatically set blood type in arena").Value;
            Config.PracticeModeDuration = Cfg.Bind("Arena", "PracticeModeDuration", 0, "Practice mode duration in minutes (0 = unlimited)").Value;
            Config.ArenaVBloodUnlock = Cfg.Bind("Arena", "VBloodUnlock", true, "Enable VBlood unlock system in arena").Value;

            Log.LogInfo("[VAuto] Configuration loaded and validated successfully");
        }
        catch (Exception ex)
        {
            Log.LogError($"[VAuto] CRITICAL ERROR loading configuration: {ex.Message}");
            Log.LogError($"[VAuto] Stack trace: {ex.StackTrace}");
            
            // Continue with defaults on critical error
            Log.LogWarning("[VAuto] Continuing with default configuration due to config loading failure");
        }
    }

    private void CreateDefaultConfigFile(string configPath)
    {
        try
        {
            string defaultConfig = "# VAuto.Arena Configuration File\n" +
                                   "# Generated automatically on first run\n\n" +
                                   "[General]\n" +
                                   "# Enable or disable the entire plugin\n" +
                                   "Enable = true\n\n" +
                                   "# Logging level: Debug, Info, Warning, Error\n" +
                                   "LogLevel = Info\n\n" +
                                   "# Enable debug mode for additional logging and features\n" +
                                   "DebugMode = false\n\n" +
                                   "[Arena]\n" +
                                   "# Enable arena functionality\n" +
                                   "ArenaEnable = true\n\n" +
                                   "# Arena center coordinates (x, y, z)\n" +
                                   "Center = -1000, 5, -500\n\n" +
                                   "# Arena radius in meters\n" +
                                   "Radius = 50\n\n" +
                                   "# Radius for entering arena zone\n" +
                                   "ArenaEnterRadius = 50\n\n" +
                                   "# Radius for exiting arena zone (1.5x enter radius)\n" +
                                   "ArenaExitRadius = 75\n\n" +
                                   "# Interval for checking arena status in seconds\n" +
                                   "ArenaCheckInterval = 2\n\n" +
                                   "[GlobalMapIconService]\n" +
                                   "# Enable global map icon service\n" +
                                   "Enabled = true\n\n" +
                                   "# Map refresh interval in seconds\n" +
                                   "UpdateInterval = 5\n\n" +
                                   "# Show player names on global map\n" +
                                   "ShowNormalPlayers = true\n\n" +
                                   "[LifecycleSystem]\n" +
                                   "# Enable player respawn system\n" +
                                   "Enabled = true\n\n" +
                                   "# Respawn cooldown in seconds\n" +
                                   "RespawnCooldown = 30\n\n" +
                                   "[Data]\n" +
                                   "# Enable database functionality\n" +
                                   "DatabaseEnable = true\n\n" +
                                   "# Database type (SQLite, MySQL, PostgreSQL)\n" +
                                   "DatabaseType = SQLite\n\n" +
                                   "# Database file path\n" +
                                   "DatabasePath = ./VAuto_Data/database.db\n";

            File.WriteAllText(configPath, defaultConfig);
            Log.LogInfo($"[VAuto] Created default config file: {configPath}");
        }
        catch (Exception ex)
        {
            Log.LogError($"[VAuto] Failed to create default config file: {ex.Message}");
        }
    }

    private float3 ParseFloat3(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            Log.LogWarning($"[VAuto] ParseFloat3: Input string is null or empty, using default");
            return new float3(-1000, -5, -500);
        }

        try
        {
            Log.LogDebug($"[VAuto] ParseFloat3: Parsing '{input}'");

            // Handle different possible formats
            string[] parts = input.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 3)
            {
                if (float.TryParse(parts[0].Trim(), out float x) &&
                    float.TryParse(parts[1].Trim(), out float y) &&
                    float.TryParse(parts[2].Trim(), out float z))
                {
                    var result = new float3(x, y, z);
                    Log.LogDebug($"[VAuto] ParseFloat3: Successfully parsed {result}");
                    return result;
                }
                else
                {
                    Log.LogWarning($"[VAuto] ParseFloat3: Failed to parse float values from parts: {string.Join(", ", parts)}");
                }
            }
            else
            {
                Log.LogWarning($"[VAuto] ParseFloat3: Expected 3 parts but got {parts.Length}: {string.Join(", ", parts)}");
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"[VAuto] ParseFloat3: Exception parsing '{input}': {ex.Message}");
        }

        // Default fallback
        Log.LogWarning($"[VAuto] ParseFloat3: Using default fallback (-1000, -5, -500)");
        return new float3(-1000, -5, -500);
    }

    private void InitializeCoreSystems()
    {
        try
        {
            // Initialize VR Core
            VRCore.Initialize();
            Log.LogInfo("[VAuto] VR Core initialized");

            // Initialize Type Aliases
            // (Add any type alias initialization here if needed)

            Log.LogInfo("[VAuto] Core systems initialized");
        }
        catch (Exception ex)
        {
            Log.LogError($"[VAuto] Error initializing core systems: {ex.Message}");
            throw;
        }
    }

    private void InitializeServices()
    {
        try
        {
            // Initialize Unified Service Manager
            UnifiedServiceManager.Initialize();
            Log.LogInfo("[VAuto] Unified Service Manager initialized");

            // Initialize arena-specific services
            if (Config.ArenaEnable)
            {
                MissingServices.LifecycleService.Initialize();
                MissingServices.ZoneService.Initialize();
                Log.LogInfo("[VAuto] Arena services initialized");
            }

            // Map services removed - keeping only UI overwrite functionality

            // Initialize database services
            if (Config.DatabaseEnable)
            {
                // Database initialization will be handled by UnifiedServiceManager
                Log.LogInfo("[VAuto] Database services initialized");
            }

            Log.LogInfo("[VAuto] All services initialized successfully");
        }
        catch (Exception ex)
        {
            Log.LogError($"[VAuto] Error initializing services: {ex.Message}");
            throw;
        }
    }

    private void InitializeGameSystems()
    {
        try
        {
            // Initialize arena system if enabled
            if (Config.ArenaEnable)
            {
                // Set up arena configuration from loaded config
                MissingServices.ZoneService.SetArenaZone(Config.ArenaCenter, Config.ArenaRadius);
                MissingServices.ZoneService.SetSpawn(Config.ArenaCenter);

                Log.LogInfo($"[VAuto] Arena configured: Center={Config.ArenaCenter}, Radius={Config.ArenaRadius}");
            }

            // Initialize respawn system if enabled
            if (Config.RespawnEnable)
            {
                // Respawn system initialization is handled by UnifiedServiceManager
                Log.LogInfo($"[VAuto] Respawn system configured: Cooldown={Config.RespawnCooldown}s");
            }

            Log.LogInfo("[VAuto] Game systems initialized");
        }
        catch (Exception ex)
        {
            Log.LogError($"[VAuto] Error initializing game systems: {ex.Message}");
            throw;
        }
    }
}
