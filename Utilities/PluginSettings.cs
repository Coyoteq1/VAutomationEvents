using System;
using System.IO;
using System.Text.Json;
using Unity.Mathematics;
using BepInEx;

namespace VAuto
{
    /// <summary>
    /// Minimal settings loader for VAuto plugin
    /// </summary>
    public static class PluginSettings
    {
        private static readonly string SETTINGS_PATH = Path.Combine(Paths.ConfigPath, "VAuto", "Settings.json");
        private static VAutoSettings _settings;

        public static void LoadSettings()
        {
            try
            {
                if (!File.Exists(SETTINGS_PATH))
                {
                    Console.WriteLine($"[PluginSettings] Settings file not found: {SETTINGS_PATH}");
                    _settings = GetDefaultSettings();
                    return;
                }

                var json = File.ReadAllText(SETTINGS_PATH);
                _settings = JsonSerializer.Deserialize<VAutoSettings>(json) ?? GetDefaultSettings();
                
                Console.WriteLine($"[PluginSettings] Loaded settings from {SETTINGS_PATH}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PluginSettings] Failed to load settings: {ex.Message}");
                _settings = GetDefaultSettings();
            }
        }

        public static VAutoSettings GetSettings() => _settings ?? GetDefaultSettings();

        private static VAutoSettings GetDefaultSettings()
        {
            return new VAutoSettings
            {
                Core = new CoreSettings
                {
                    Enable = true,
                    DebugMode = true,
                    LogLevel = "Debug"
                },
                Arena = new ArenaSettings
                {
                    Enable = true,
                    Center = new float3(-1000, 5, -500),
                    Radius = 50,
                    EnterRadius = 50,
                    ExitRadius = 75,
                    CheckInterval = 3.0f
                },
                Services = new ServicesSettings
                {
                    EnableAutomation = true,
                    AutomationTickRate = 1.0f,
                    EnableDatabase = true,
                    DatabasePath = "./VAuto_Data/database.db"
                },
                Respawn = new RespawnSettings
                {
                    IsEnabled = false,
                    DefaultCooldownSeconds = 30
                }
            };
        }
    }

    public class VAutoSettings
    {
        public CoreSettings Core { get; set; }
        public ArenaSettings Arena { get; set; }
        public ServicesSettings Services { get; set; }
        public RespawnSettings Respawn { get; set; }
    }

    public class CoreSettings
    {
        public bool Enable { get; set; }
        public bool DebugMode { get; set; }
        public string LogLevel { get; set; }
    }

    public class ArenaSettings
    {
        public bool Enable { get; set; }
        public float3 Center { get; set; }
        public float Radius { get; set; }
        public float EnterRadius { get; set; }
        public float ExitRadius { get; set; }
        public float CheckInterval { get; set; }
    }

    public class ServicesSettings
    {
        public bool EnableAutomation { get; set; }
        public float AutomationTickRate { get; set; }
        public bool EnableDatabase { get; set; }
        public string DatabasePath { get; set; }
    }

    public class RespawnSettings
    {
        public bool IsEnabled { get; set; }
        public int DefaultCooldownSeconds { get; set; }
    }
}
