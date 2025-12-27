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
[BepInPlugin("gg.Coyote.vautomation.arena", "VAuto.Arena", "0.1.0")]
[BepInDependency("gg.deca.VampireCommandFramework")]
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

    // Configuration settings loaded from VAuto.cfg
    public static class Config
    {
        public static bool Enable = true;
        public static string LogLevel = "Info";
        public static bool DebugMode = false;

        // Arena settings
        public static bool ArenaEnable = true;
        public static float3 ArenaCenter = new float3(-1000, -5, -500);
        public static float ArenaRadius = 50.0f;
        public static float ArenaEnterRadius = 50.0f;
        public static float ArenaExitRadius = 75.0f;
        public static float ArenaCheckInterval = 2.0f;

        // Map settings
        public static bool MapEnableIcons = true;
        public static float MapRefreshInterval = 5.0f;
        public static bool MapShowNames = true;

        // Respawn settings
        public static bool RespawnEnable = true;
        public static int RespawnCooldown = 30;

        // Database settings
        public static bool DatabaseEnable = true;
        public static string DatabaseType = "SQLite";
        public static string DatabasePath = "./VAuto_Data/database.db";
    }

    public override void Load()
    {
        Instance = this;
        Plugin.Log = Log;

        Log.LogInfo($"=== VAuto Plugin Loading ===");
        Log.LogInfo($"Plugin: {MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION}");
        Log.LogInfo($"Data Path: {DataPath}");
        Log.LogInfo($"Config Path: {ConfigPath}");

        try
        {
            // Step 1: Load basic configuration
            Log.LogInfo("[VAuto] Loading configuration...");
            LoadBasicConfiguration();
            Log.LogInfo("[VAuto] Configuration loaded successfully");

            // Step 3: Validate configuration
            if (!Config.Enable)
            {
                Log.LogWarning("[VAuto] Plugin is disabled in configuration");
                return;
            }

            // Step 4: Initialize core systems
            Log.LogInfo("[VAuto] Initializing core systems...");
            InitializeCoreSystems();

            // Step 5: Initialize services
            Log.LogInfo("[VAuto] Initializing services...");
            try
            {
                InitializeServices();
            }
            catch (Exception serviceEx)
            {
                Log.LogError($"[VAuto] Service initialization failed: {serviceEx.Message}");
                Log.LogError($"[VAuto] Stack trace: {serviceEx.StackTrace}");
                Log.LogWarning("[VAuto] Continuing with plugin load despite service initialization failure");
                // Don't re-throw - allow plugin to continue loading
            }

            // Step 6: Harmony patching
            Log.LogInfo("[VAuto] Applying Harmony patches...");
            _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            _harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());

            // Step 7: Register commands
            Log.LogInfo("[VAuto] Registering commands...");
            CommandRegistry.RegisterAll();

            // Step 8: Initialize game-specific systems
            Log.LogInfo("[VAuto] Initializing game systems...");
            InitializeGameSystems();

            Log.LogInfo($"=== VAuto Plugin Loaded Successfully ===");
            Log.LogInfo($"[VAuto] Arena System: {(Config.ArenaEnable ? "ENABLED" : "DISABLED")}");
            Log.LogInfo($"[VAuto] Map Icons: {(Config.MapEnableIcons ? "ENABLED" : "DISABLED")}");
            Log.LogInfo($"[VAuto] Database: {(Config.DatabaseEnable ? "ENABLED" : "DISABLED")}");
        }
        catch (Exception ex)
        {
            Log.LogError($"[VAuto] CRITICAL ERROR during plugin loading: {ex.Message}");
            Log.LogError($"[VAuto] Stack trace: {ex.StackTrace}");
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
        // For now, use default configuration values
        // TODO: Implement proper configuration loading from VAuto.cfg
        Log.LogInfo("[VAuto] Using default configuration (basic mode)");
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

            // Initialize map services
            if (Config.MapEnableIcons)
            {
                // Map icon service initialization will be handled by UnifiedServiceManager
                Log.LogInfo("[VAuto] Map services initialized");
            }

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
