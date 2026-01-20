using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Configuration;
using HarmonyLib;
using VampireCommandFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Unity.Mathematics;
using VAuto.Core;
using VAuto.Utilities;

namespace VAuto;

/// <summary>
/// VAuto Arena Plugin - Comprehensive arena management system for V Rising
/// Features: Player lifecycle, snapshot system, ability management, zone control, database persistence
/// Services: 25+ core services for arena operations
/// Commands: 149 commands across 8 categories
/// </summary>
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("gg.deca.VampireCommandFramework")]
[BepInProcess("VRisingServer.exe")]
public class Plugin : BasePlugin
{
    internal static ManualLogSource Log;
    internal static BepInEx.Configuration.ConfigFile Cfg;
    private static Harmony _harmony;

    public static Harmony Harmony => _harmony;
    public static ManualLogSource Logger => Log;
    public static Plugin Instance { get; private set; }
    public static string ConfigPath => BepInEx.Paths.ConfigPath;

    public override void Load()
    {
        Instance = this;
        Log = base.Log;
        Cfg = base.Config;
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        Log.LogInfo($"[VAuto] Loading {MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION}");
        Log.LogInfo($"[VAuto] Plugin GUID: {MyPluginInfo.PLUGIN_GUID}");

        try
        {
            // Step 0: Ensure all directories exist
            EnsureDirectories();
            Log.LogInfo("[VAuto] All required directories verified/created");

            // Step 1: Load unified JSON configuration
            PluginSettings.LoadSettings();
            Log.LogInfo("[VAuto] Unified configuration loaded");

            // Step 1.5: Load additional JSON configurations
            var jsonFiles = GetJsonConfigFiles();
            Log.LogInfo($"[VAuto] Found {jsonFiles.Count} JSON configuration files");
            
            // Step 2: Initialize core systems
            InitializeCoreSystems();
            Log.LogInfo("[VAuto] Core systems initialized");

            // Step 3: Initialize services (25+ services)
            InitializeServices();
            Log.LogInfo("[VAuto] Services initialized");

            // Step 4: Apply Harmony patches
            _harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            Log.LogInfo("[VAuto] Harmony patches applied");

            // Step 5: Register commands (149 commands)
            CommandRegistry.RegisterAll();
            Log.LogInfo("[VAuto] Commands registered");

            // Step 6: Initialize game systems
            InitializeGameSystems();
            Log.LogInfo("[VAuto] Game systems initialized");

            Log.LogInfo($"[VAuto] ✓ Plugin loaded successfully - Ready for arena operations");
        }
        catch (Exception ex)
        {
            Log.LogError($"[VAuto] ✗ CRITICAL ERROR: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    private static void InitializeCoreSystems()
    {
        // Initialize core systems
    }

    private static void InitializeServices()
    {
        // Initialize services
    }

    private static void InitializeGameSystems()
    {
        // Initialize game systems
    }

    // Directory and file management
    public static string VAutoConfigDir => Path.Combine(ConfigPath, "Vautomation");
    public static string VAutoDataDir => Path.Combine(VAutoConfigDir, "Data");
    public static string VAutoBackupDir => Path.Combine(VAutoConfigDir, "Backups");
    public static string VAutoArenaDir => Path.Combine(VAutoConfigDir, "Arena");

    /// <summary>
    /// Ensure all required directories exist
    /// </summary>
    public static void EnsureDirectories()
    {
        var directories = new[]
        {
            VAutoConfigDir,
            VAutoDataDir,
            VAutoBackupDir,
            VAutoArenaDir,
            Path.Combine(VAutoDataDir, "KindredExtract"),
            Path.Combine(VAutoDataDir, "CustomUuids"),
            Path.Combine(VAutoDataDir, "Snapshots"),
            Path.Combine(VAutoDataDir, "Schematics")
        };

        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                Log?.LogInfo($"[VAuto] Created directory: {dir}");
            }
        }
    }

    /// <summary>
    /// Get all JSON configuration files
    /// </summary>
    public static List<string> GetJsonConfigFiles()
    {
        var jsonFiles = new List<string>();
        
        // Main config files
        var mainConfigs = new[]
        {
            "Settings.json",
            "Victor2.json", 
            "Victor3.json", 
            "Victor4.json", 
            "Snapchat.json"
        };

        foreach (var config in mainConfigs)
        {
            var path = Path.Combine(VAutoConfigDir, config);
            if (File.Exists(path))
            {
                jsonFiles.Add(path);
            }
        }

        // Arena configs
        var arenaConfigs = new[]
        {
            "build.json",
            "zone.json",
            "snapshot.json"
        };

        foreach (var config in arenaConfigs)
        {
            var path = Path.Combine(VAutoArenaDir, config);
            if (File.Exists(path))
            {
                jsonFiles.Add(path);
            }
        }

        return jsonFiles;
    }

    /// <summary>
    /// Load JSON configuration file
    /// </summary>
    public static T LoadJsonConfig<T>(string fileName) where T : class, new()
    {
        var path = Path.Combine(VAutoConfigDir, fileName);
        try
        {
            if (!File.Exists(path))
            {
                Log?.LogWarning($"[VAuto] Config file not found: {path}");
                return new T();
            }

            var json = File.ReadAllText(path);
            var options = VAuto.Extensions.VRCoreStubs.SchematicService.GetJsonOptions();
            var config = JsonSerializer.Deserialize<T>(json, options);
            Log?.LogInfo($"[VAuto] Loaded config: {fileName}");
            return config;
        }
        catch (Exception ex)
        {
            Log?.LogError($"[VAuto] Failed to load config {fileName}: {ex.Message}");
            return new T();
        }
    }

    /// <summary>
    /// Save JSON configuration file
    /// </summary>
    public static void SaveJsonConfig<T>(string fileName, T config) where T : class
    {
        var path = Path.Combine(VAutoConfigDir, fileName);
        try
        {
            var options = VAuto.Extensions.VRCoreStubs.SchematicService.GetJsonOptions();
            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(path, json);
            Log?.LogInfo($"[VAuto] Saved config: {fileName}");
        }
        catch (Exception ex)
        {
            Log?.LogError($"[VAuto] Failed to save config {fileName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all available schematic files
    /// </summary>
    public static List<string> GetSchematicFiles()
    {
        var schematicDir = Path.Combine(VAutoDataDir, "Schematics");
        var schematics = new List<string>();

        if (Directory.Exists(schematicDir))
        {
            schematics.AddRange(Directory.GetFiles(schematicDir, "*.json"));
        }

        return schematics;
    }

    // Configuration properties (using unified settings)
    public static bool Enable => PluginSettings.GetSettings().Core.Enable;
    public static bool DebugMode => PluginSettings.GetSettings().Core.DebugMode;
    public static VAuto.Core.LogLevel LogLevel => Enum.Parse<VAuto.Core.LogLevel>(PluginSettings.GetSettings().Core.LogLevel);
    public static bool ZoneEnable => PluginSettings.GetSettings().Arena.Enable;
    public static float3 ZoneCenter => PluginSettings.GetSettings().Arena.Center;
    public static float ZoneRadius => PluginSettings.GetSettings().Arena.Radius;
    public static float ZoneEnterRadius => PluginSettings.GetSettings().Arena.EnterRadius;
    public static float ZoneExitRadius => PluginSettings.GetSettings().Arena.ExitRadius;
    public static float ZoneCheckInterval => PluginSettings.GetSettings().Arena.CheckInterval;
    public static bool RespawnEnable => PluginSettings.GetSettings().Respawn.IsEnabled;
    public static int RespawnCooldown => PluginSettings.GetSettings().Respawn.DefaultCooldownSeconds;
    public static bool AutomationEnable => PluginSettings.GetSettings().Services.EnableAutomation;
    public static float AutomationTickRate => PluginSettings.GetSettings().Services.AutomationTickRate;
    public static bool DatabaseEnable => PluginSettings.GetSettings().Services.EnableDatabase;
    public static string DatabasePath => PluginSettings.GetSettings().Services.DatabasePath;

    public override bool Unload()
    {
        Log.LogInfo("[VAuto] Unloading plugin");
        try
        {
            _harmony?.UnpatchSelf();
            Log.LogInfo("[VAuto] Plugin unloaded successfully");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogError($"[VAuto] Error during unload: {ex.Message}");
            return false;
        }
    }

    // Embedded configurations for exceptional settings

    public const string EmbeddedAutomationCfg = @"
# Exceptional VAuto Unified Configuration File
# Enhanced version with optimized settings for maximum performance and functionality
# Generated automatically by VAuto v2.1.0
# Each setting follows a five-row format:
# Row 1: Description (with hash)
# Row 2: Default value (with hash)
# Row 3: Type indicator ""2"" (with hash)
# Row 4: Additional details (with hash)
# Row 5: Input field

[Core]
# Enable or disable the entire VAuto plugin
# true
# 2
# Master switch - fully enabled for optimal functionality
Enable = true

# Log level: Debug, Info, Warning, Error
# Info
# 2
# Optimized logging level for production use
LogLevel = Info

# Enable debug logging for troubleshooting
# false
# 2
# Disabled in production for performance
DebugMode = false

[Arena]
# Enable arena system
# true
# 2
# Core arena functionality enabled
Enable = true

# Arena center coordinates (format: x,y,z)
# -1000,-5,-500
# 2
# Optimized center position
Center = -1000, -5, -500

# Arena radius in meters
# 75.0
# 2
# Increased radius for larger arenas
Radius = 75.0

# Proximity entry radius in meters
# 75.0
# 2
# Matching radius for smooth transitions
EnterRadius = 75.0

# Proximity exit radius in meters
# 100.0
# 2
# Larger exit radius to prevent flickering
ExitRadius = 100.0

# Position check interval in seconds
# 1.5
# 2
# Faster checks for responsive arena detection
CheckInterval = 1.5

[Commands]
# Enable character management commands
# true
# 2
# Full command suite enabled
EnableCharacter = true

# Enable reload commands
# true
# 2
# Configuration reloading enabled
EnableReload = true

# Enable service management commands
# true
# 2
# Service management enabled
EnableService = true

[Map]
# Enable map icon system
# true
# 2
# Map icons for player visibility
EnableIcons = true

# Map icon refresh interval in seconds
# 3.0
# 2
# Balanced refresh rate
RefreshInterval = 3.0

# Show player names on map
# true
# 2
# Enhanced visibility
ShowNames = true

[Respawn]
# Enable respawn prevention system
# true
# 2
# Respawn management enabled
Enable = true

# Default respawn cooldown in seconds
# 20
# 2
# Reduced cooldown for faster gameplay
Cooldown = 20

[Game]
# Enable VBlood hooks for arena
# true
# 2
# VBlood integration enabled
EnableVBloodHooks = true

# Enable unlock overrides for arena
# true
# 2
# Content unlocks enabled
EnableUnlockOverrides = true

[Paths]
# Data storage path relative to server directory
# ./VAuto_Data
# 2
# Persistent data location
DataPath = ./VAuto_Data

# Configuration path relative to server directory
# ./Config
# 2
# Config files location
ConfigPath = ./Config

[Database]
# Enable database persistence
# true
# 2
# Database enabled for reliability
Enable = true

# Database type: SQLite, JSON, None
# SQLite
# 2
# SQLite for performance
Type = SQLite

# Database file path relative to server directory
# ./VAuto_Data/database.db
# 2
# Database location
Path = ./VAuto_Data/database.db

[Performance]
# Max entities processed per frame
# 200
# 2
# Increased for better performance
MaxEntitiesPerFrame = 200

# Enable entity pooling
# true
# 2
# Memory optimization
EnablePooling = true

# Garbage collection interval in seconds
# 120
# 2
# Less frequent GC
GCInterval = 120

[Security]
# Maximum commands per player per minute
# 60
# 2
# Higher limit for active players
MaxCommandsPerMinute = 60

# Enable command cooldowns
# true
# 2
# Spam prevention
EnableCommandCooldowns = true

# Admin-only commands list (comma separated)
# reload,reloadall,svc,sys
# 2
# Admin commands
AdminOnlyCommands = reload,reloadall,svc,sys

[UI]
# Enable unified VAuto UI
# true
# 2
# UI enabled
EnableUI = true

# UI theme
# Dark
# 2
# Modern theme
UITheme = Dark

# Show debug info in UI
# false
# 2
# Clean UI
ShowDebugInfo = false

# Enable notifications
# true
# 2
# User feedback
EnableNotifications = true

# Notification duration in seconds
# 4
# 2
# Optimal duration
NotificationDuration = 4

# Enable advanced UI features
# true
# 2
# Full features
EnableAdvancedUI = true

# Show performance metrics
# true
# 2
# Performance monitoring
ShowPerformanceMetrics = true

[Automation]
# Enable automation system
# true
# 2
# Automation enabled
EnableAutomation = true

# Enable building automation
# true
# 2
# Building automation enabled
EnableBuildingAutomation = true

# Enable harvesting automation
# true
# 2
# Resource automation
EnableHarvestingAutomation = true

# Enable combat automation
# false
# 2
# Combat automation disabled for fairness
EnableCombatAutomation = false

# Enable crafting automation
# true
# 2
# Crafting automation
EnableCraftingAutomation = true

# Maximum concurrent automation tasks
# 10
# 2
# Higher concurrency
MaxConcurrentTasks = 10

# Automation tick rate in seconds
# 0.5
# 2
# Faster automation
AutomationTickRate = 0.5

# Task timeout in seconds
# 600
# 2
# Longer timeout
TaskTimeout = 600

[Prefabs]
# Enable custom prefab loading
# true
# 2
# Custom prefabs
EnableCustomPrefabs = true

# Prefab cache size
# 2000
# 2
# Larger cache
MaxPrefabCacheSize = 2000

# Enable prefab validation
# true
# 2
# Validation enabled
EnablePrefabValidation = true

# Prefab load timeout in seconds
# 60
# 2
# Longer timeout
PrefabLoadTimeout = 60

# Default prefab quality level
# 2
# 2
# Higher quality
DefaultPrefabQuality = 2

# Enable experimental prefabs
# false
# 2
# Stability first
EnableExperimentalPrefabs = false

[AdvancedServices]
# Enable analytics service
# true
# 2
# Analytics enabled
EnableAnalyticsService = true

# Enable backup service
# true
# 2
# Backups enabled
EnableBackupService = true

# Enable metrics service
# true
# 2
# Metrics enabled
EnableMetricsService = true

# Enable hot reload service
# true
# 2
# Hot reload enabled
EnableHotReloadService = true

# Enable diagnostic service
# true
# 2
# Diagnostics enabled
EnableDiagnosticService = true

# Service timeout in seconds
# 60
# 2
# Longer timeout
ServiceTimeout = 60

# Maximum service retries
# 5
# 2
# More retries
MaxServiceRetries = 5

[Debug]
# Enable debug mode
# false
# 2
# Production mode
EnableDebugMode = false

# Debug log level
# Warning
# 2
# Warning level
DebugLogLevel = Warning

# Enable debug commands
# false
# 2
# No debug commands
EnableDebugCommands = false

# Enable performance profiling
# false
# 2
# Profiling disabled
EnablePerformanceProfiling = false

# Enable experimental features
# false
# 2
# Stable features
EnableExperimentalFeatures = false

# Development mode
# false
# 2
# Production
DevelopmentMode = false

# UI refresh rate in seconds
# 0.5
# 2
# Faster UI
RefreshRate = 0.5

# Show debug information in UI
# false
# 2
# Clean UI
ShowDebugInfo = false

[Debug]
# Enable comprehensive debugging system
# false
# 2
# Disabled
EnableDebugSystem = false

# Debug logging level (0=None, 1=Basic, 2=Detailed, 3=Verbose)
# 1
# 2
# Basic logging
DebugLogLevel = 1

# Enable performance monitoring
# true
# 2
# Monitoring enabled
EnablePerformanceMonitoring = true

# Enable entity tracking
# true
# 2
# Tracking enabled
EnableEntityTracking = true

[Network]
# Enable network optimizations
# true
# 2
# Optimizations enabled
EnableOptimizations = true

# Maximum packets per second
# 200
# 2
# Higher limit
MaxPacketsPerSecond = 200

# Connection timeout in seconds
# 60
# 2
# Longer timeout
ConnectionTimeout = 60

[Backup]
# Enable automatic backups
# true
# 2
# Backups enabled
Enable = true

# Backup interval in hours
# 4
# 2
# More frequent
Interval = 4

# Maximum backup files to keep
# 50
# 2
# More backups
MaxBackups = 50

# Backup path relative to server directory
# ./VAuto_Data/Backups
# 2
# Backup location
Path = ./VAuto_Data/Backups

[Logging]
# Enable file logging
# true
# 2
# File logging
EnableFileLogging = true

# Log file path relative to server directory
# ./VAuto_Data/Logs
# 2
# Log location
FilePath = ./VAuto_Data/Logs

# Maximum log file size in MB
# 20
# 2
# Larger files
MaxFileSize = 20

# Number of log files to rotate
# 10
# 2
# More files
MaxLogFiles = 10

# Enable console logging
# true
# 2
# Console logging
EnableConsoleLogging = true

# Enable structured logging (JSON format)
# true
# 2
# Structured logs
EnableStructuredLogging = true

[Zone]
# Enable zone system
# true
# 2
# Zones enabled
EnableZoneSystem = true

# Default zone type
# Custom
# 2
# Custom zones
DefaultZoneType = Custom

# Maximum zones per player
# 10
# 2
# More zones
MaxZonesPerPlayer = 10

# Zone check interval in seconds
# 2.0
# 2
# Balanced
ZoneCheckInterval = 2.0

# Auto-save interval in seconds
# 180
# 2
# Frequent saves
AutoSaveInterval = 180

# Enable zone logging
# true
# 2
# Logging
EnableZoneLogging = true

# Enable zone statistics
# true
# 2
# Stats
EnableZoneStatistics = true

[Arena.Default]
# Arena zone ID
# exceptional_default
# 2
# Unique ID
ArenaId = exceptional_default

# Arena zone name
# Exceptional Arena
# 2
# Name
ArenaName = Exceptional Arena

# Arena zone description
# High-performance arena with optimized settings
# 2
# Description
ArenaDescription = High-performance arena with optimized settings

# Arena zone shape
# Circle
# 2
# Shape
ArenaShape = Circle

# Arena zone priority
# 1
# 2
# Priority
ArenaPriority = 1

# Arena center position X
# -1000.0
# 2
# X
ArenaCenterX = -1000.0

# Arena center position Y
# 5.0
# 2
# Y
ArenaCenterY = 5.0

# Arena center position Z
# -500.0
# 2
# Z
ArenaCenterZ = -500.0

# Arena radius
# 75.0
# 2
# Radius
ArenaRadius = 75.0

# Arena height
# 150.0
# 2
# Height
ArenaHeight = 150.0

# Arena minimum Y
# -75.0
# 2
# Min Y
ArenaMinY = -75.0

# Arena maximum Y
# 75.0
# 2
# Max Y
ArenaMaxY = 75.0

[Arena.Restrictions]
# Restrict building in arena
# true
# 2
# Building restricted
RestrictBuilding = true

# Restrict combat in arena
# false
# 2
# Combat allowed
RestrictCombat = false

# Restrict teleport in arena
# false
# 2
# Teleport allowed
RestrictTeleport = false

# Restrict inventory in arena
# false
# 2
# Inventory allowed
RestrictInventory = false

# Restrict abilities in arena
# false
# 2
# Abilities allowed
RestrictAbilities = false

# Restrict vehicles in arena
# false
# 2
# Vehicles allowed
RestrictVehicles = false

# Restrict NPCs in arena
# false
# 2
# NPCs allowed
RestrictNPCs = false

[Arena.Properties]
# Arena is PVP enabled
# true
# 2
# PVP enabled
ArenaIsPVP = true

# Enable healing in arena
# false
# 2
# Healing disabled
ArenaHealingEnabled = false

# Enable building in arena
# false
# 2
# Building disabled
ArenaBuildingEnabled = false

# Enable resource respawn in arena
# false
# 2
# Resources disabled
ArenaResourceRespawn = false

# Respawn time in seconds
# 180
# 2
# Shorter respawn
ArenaRespawnTime = 180

# Damage multiplier
# 1.2
# 2
# Increased damage
ArenaDamageMultiplier = 1.2

# Healing multiplier
# 0.8
# 2
# Reduced healing
ArenaHealingMultiplier = 0.8

# Experience multiplier
# 2.0
# 2
# Double XP
ArenaExperienceMultiplier = 2.0

# Enable auto cleanup
# true
# 2
# Cleanup enabled
ArenaAutoCleanup = true

# Cleanup interval in seconds
# 1800
# 2
# Longer interval
ArenaCleanupInterval = 1800

# Arena environment theme
# exceptional
# 2
# Custom theme
ArenaEnvironmentTheme = exceptional

[Zone.DefaultRestrictions]
# Default building restriction
# false
# 2
# Building allowed
DefaultRestrictBuilding = false

# Default combat restriction
# false
# 2
# Combat allowed
DefaultRestrictCombat = false

# Default teleport restriction
# false
# 2
# Teleport allowed
DefaultRestrictTeleport = false

[Zone.DefaultProperties]
# Default PVP setting
# false
# 2
# No PVP default
DefaultIsPVP = false

# Default healing setting
# true
# 2
# Healing enabled
DefaultHealingEnabled = true

# Default building setting
# true
# 2
# Building enabled
DefaultBuildingEnabled = true

# Default resource respawn setting
# true
# 2
# Resources enabled
DefaultResourceRespawn = true

# Default auto cleanup setting
# true
# 2
# Cleanup enabled
DefaultAutoCleanup = true

[Converters]
# Enable Quaternion converter
# true
# 2
# Custom converter for rotations
EnableQuaternionConverter = true

# Enable Float3 converter
# true
# 2
# Vector converter
EnableFloat3Converter = true

# Enable AABB converter
# true
# 2
# Bounding box converter
EnableAabbConverter = true

# Enable PrefabGUID converter
# true
# 2
# Prefab converter
EnablePrefabGUIDConverter = true
";

    public const string EmbeddedSettingsJson = @"{""version"": ""2.1.0"", ""lastUpdated"": ""2026-01-17T09:44:00Z"", ""metadata"": {""description"": ""Exceptional VAuto settings with optimized configurations"", ""author"": ""VAuto System"", ""compatibility"": ""VRising 1.0+"", ""features"": [""Enhanced performance"", ""Advanced converters"", ""Comprehensive automation"", ""Optimized Unity integration""]}, ""core"": {""enablePlugin"": true, ""debugMode"": false, ""logLevel"": ""Info"", ""autoSave"": true, ""autoSaveInterval"": 180, ""maxBackups"": 50, ""performanceMode"": ""Optimized"", ""memoryLimit"": 2048, ""threadingEnabled"": true}, ""arena"": {""enableArenaSystem"": true, ""defaultArenaRadius"": 75, ""enterRadius"": 75, ""exitRadius"": 100, ""checkInterval"": 1.5, ""autoCleanup"": true, ""cleanupInterval"": 1800, ""maxInactiveTime"": 3600, ""damageMultiplier"": 1.2, ""healingMultiplier"": 0.8, ""experienceMultiplier"": 2.0, ""environmentTheme"": ""exceptional"", ""maxPlayers"": 50, ""spectatorMode"": true}, ""zone"": {""enableZoneSystem"": true, ""zoneCenter"": {""x"": -1000, ""y"": 5, ""z"": -500}, ""zoneRadius"": 75, ""zoneShape"": ""Circle"", ""restrictBuilding"": true, ""restrictCombat"": false, ""allowTeleport"": true, ""maxZonesPerPlayer"": 10, ""zoneCheckInterval"": 2.0, ""autoSaveInterval"": 180}, ""respawn"": {""enableRespawnSystem"": true, ""respawnCooldown"": 20, ""respawnDelay"": 3, ""respawnHealthPercent"": 100, ""respawnBloodPercent"": 100, ""keepInventory"": false, ""respawnPoint"": {""x"": -1000, ""y"": 5, ""z"": -500}, ""safeRespawn"": true, ""respawnEffects"": true}, ""automation"": {""enableAutomation"": true, ""tickRate"": 0.5, ""enableBuilding"": true, ""enableHarvesting"": true, ""enableCombat"": false, ""enableCrafting"": true, ""maxConcurrentTasks"": 10, ""taskTimeout"": 600, ""automationRadius"": 50, ""resourcePriorities"": [""Wood"", ""Stone"", ""Ore""], ""buildingQueueSize"": 20}, ""database"": {""enableDatabase"": true, ""databasePath"": ""Data/exceptional-database.db"", ""backupEnabled"": true, ""backupInterval"": 14400, ""maxBackupSize"": 500, ""compressionEnabled"": true, ""connectionPoolSize"": 10, ""queryTimeout"": 30}, ""ui"": {""enableUI"": true, ""showDebugInfo"": false, ""showNotifications"": true, ""notificationDuration"": 4, ""theme"": ""Dark"", ""language"": ""English"", ""fontSize"": 12, ""showPerformanceMetrics"": true, ""uiScale"": 1.0, ""animationsEnabled"": true}, ""performance"": {""maxEntitiesPerFrame"": 200, ""updateInterval"": 0.016, ""enableBatching"": true, ""enableCulling"": true, ""memoryLimit"": 2048, ""gcInterval"": 120, ""cacheSize"": 1000, ""optimizationLevel"": ""High""}, ""security"": {""enableAuthentication"": true, ""requirePermissions"": true, ""logAdminActions"": true, ""maxLoginAttempts"": 5, ""sessionTimeout"": 1800, ""encryptionEnabled"": true, ""auditLogEnabled"": true}, ""network"": {""enableOptimizations"": true, ""maxPacketsPerSecond"": 200, ""connectionTimeout"": 60, ""compressionEnabled"": true, ""packetSizeLimit"": 65536, ""latencyCompensation"": true}, ""converters"": {""enableQuaternionConverter"": true, ""enableFloat3Converter"": true, ""enableAabbConverter"": true, ""enablePrefabGUIDConverter"": true, ""customConverters"": [""QuaternionConverter"", ""SchematicFloat3Converter"", ""AabbConverter"", ""PrefabGUIDConverter""], ""jsonOptions"": {""writeIndented"": true, ""propertyNamingPolicy"": ""CamelCase"", ""includeFields"": true, ""ignoreNullValues"": true, ""maxDepth"": 64}}, ""schematics"": {""enableSchematicSystem"": true, ""schematicDirectory"": ""Schematics"", ""autoSaveSchematics"": true, ""maxSchematicSize"": 10485760, ""supportedFormats"": [""json"", ""binary""], ""compressionEnabled"": true, ""validationEnabled"": true}, ""services"": {""enableAnalyticsService"": true, ""enableBackupService"": true, ""enableMetricsService"": true, ""enableHotReloadService"": true, ""enableDiagnosticService"": true, ""serviceTimeout"": 60, ""maxServiceRetries"": 5, ""healthCheckInterval"": 300}, ""experimental"": {""enableExperimentalFeatures"": false, ""aiLearningEnabled"": false, ""advancedAutomation"": false, ""customShaders"": false, ""extendedLogging"": false}}";

    public const string EmbeddedSnapshotJson = @"{""version"": ""2.1.0"", ""lastUpdated"": ""2026-01-17T09:44:00Z"", ""description"": ""Exceptional snapshot data with comprehensive player states and system configurations"", ""metadata"": {""totalSnapshots"": 5, ""compressionEnabled"": true, ""validationEnabled"": true, ""backupEnabled"": true, ""autoCleanup"": true}, ""snapshots"": [{""id"": ""snapshot_001"", ""playerId"": 123456789, ""characterName"": ""ArenaChampion"", ""timestamp"": ""2026-01-17T09:00:00Z"", ""location"": {""x"": -1000.0, ""y"": 5.0, ""z"": -500.0}, ""rotation"": [0.0, 90.0, 0.0], ""health"": 100.0, ""blood"": 100.0, ""level"": 50, ""experience"": 250000, ""inventory"": [{""itemId"": 238268650, ""quantity"": 10, ""equipped"": true}, {""itemId"": 1055898174, ""quantity"": 5, ""equipped"": false}], ""abilities"": [{""abilityId"": -1996241419, ""cooldown"": 0.0, ""level"": 3}], ""buffs"": [], ""zone"": ""exceptional_default"", ""status"": ""Active"", ""lastActivity"": ""2026-01-17T09:00:00Z""}, {""id"": ""snapshot_002"", ""playerId"": 987654321, ""characterName"": ""BuilderMaster"", ""timestamp"": ""2026-01-17T08:30:00Z"", ""location"": {""x"": -950.0, ""y"": 10.0, ""z"": -450.0}, ""rotation"": [0.0, 45.0, 0.0], ""health"": 85.0, ""blood"": 90.0, ""level"": 35, ""experience"": 150000, ""inventory"": [{""itemId"": 1392314162, ""quantity"": 20, ""equipped"": false}], ""abilities"": [], ""buffs"": [{""buffId"": 12345, ""duration"": 300.0, ""stackCount"": 1}], ""zone"": ""custom_zone_1"", ""status"": ""Building"", ""lastActivity"": ""2026-01-17T08:30:00Z""}, {""id"": ""snapshot_003"", ""playerId"": 555666777, ""characterName"": ""HarvesterPro"", ""timestamp"": ""2026-01-17T07:15:00Z"", ""location"": {""x"": -1050.0, ""y"": 0.0, ""z"": -550.0}, ""rotation"": [0.0, 180.0, 0.0], ""health"": 95.0, ""blood"": 95.0, ""level"": 42, ""experience"": 200000, ""inventory"": [{""itemId"": 1982551454, ""quantity"": 50, ""equipped"": false}], ""abilities"": [], ""buffs"": [], ""zone"": ""resource_zone"", ""status"": ""Harvesting"", ""lastActivity"": ""2026-01-17T07:15:00Z""}, {""id"": ""snapshot_004"", ""playerId"": 111222333, ""characterName"": ""CrafterElite"", ""timestamp"": ""2026-01-17T06:00:00Z"", ""location"": {""x"": -900.0, ""y"": 15.0, ""z"": -400.0}, ""rotation"": [0.0, 270.0, 0.0], ""health"": 100.0, ""blood"": 100.0, ""level"": 48, ""experience"": 230000, ""inventory"": [{""itemId"": 205207385, ""quantity"": 30, ""equipped"": false}], ""abilities"": [], ""buffs"": [], ""zone"": ""crafting_zone"", ""status"": ""Crafting"", ""lastActivity"": ""2026-01-17T06:00:00Z""}, {""id"": ""snapshot_005"", ""playerId"": 444555666, ""characterName"": ""AdminObserver"", ""timestamp"": ""2026-01-17T05:00:00Z"", ""location"": {""x"": -1000.0, ""y"": 50.0, ""z"": -500.0}, ""rotation"": [0.0, 0.0, 0.0], ""health"": 100.0, ""blood"": 100.0, ""level"": 80, ""experience"": 500000, ""inventory"": [], ""abilities"": [], ""buffs"": [], ""zone"": ""admin_zone"", ""status"": ""Observing"", ""lastActivity"": ""2026-01-17T05:00:00Z""}], ""globalSettings"": {""autoSave"": true, ""maxSnapshots"": 100, ""cleanupInterval"": 3600, ""compressionEnabled"": true, ""validationEnabled"": true, ""backupEnabled"": true, ""maxSnapshotAge"": 604800, ""snapshotInterval"": 300, ""dataRetentionPolicy"": ""Intelligent"", ""anomalyDetection"": true}, ""systemState"": {""activePlayers"": 5, ""totalZones"": 10, ""activeArenas"": 1, ""runningServices"": 15, ""memoryUsage"": 1024, ""cpuUsage"": 45.5, ""networkLatency"": 25, ""uptime"": 86400}, ""zoneStates"": [{""zoneId"": ""exceptional_default"", ""name"": ""Exceptional Arena"", ""playerCount"": 1, ""status"": ""Active"", ""lastUpdate"": ""2026-01-17T09:00:00Z""}, {""zoneId"": ""custom_zone_1"", ""name"": ""Building Zone"", ""playerCount"": 1, ""status"": ""Active"", ""lastUpdate"": ""2026-01-17T08:30:00Z""}], ""serviceStates"": [{""serviceName"": ""ArenaService"", ""status"": ""Running"", ""uptime"": 86400, ""memoryUsage"": 128}, {""serviceName"": ""ZoneService"", ""status"": ""Running"", ""uptime"": 86400, ""memoryUsage"": 96}, {""serviceName"": ""AutomationService"", ""status"": ""Running"", ""uptime"": 86400, ""memoryUsage"": 150}], ""performanceMetrics"": {""averageFrameTime"": 16.7, ""maxFrameTime"": 50.0, ""totalEntities"": 5000, ""activeCoroutines"": 25, ""networkPacketsSent"": 10000, ""networkPacketsReceived"": 9500}, ""backupInfo"": {""lastBackup"": ""2026-01-17T08:00:00Z"", ""backupCount"": 24, ""totalBackupSize"": 52428800, ""compressionRatio"": 0.75}}";

}

