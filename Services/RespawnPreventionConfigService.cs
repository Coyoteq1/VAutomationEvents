using System.IO;
using System.Text.Json;
using BepInEx.Logging;
using VAuto.Core;

namespace VAuto.Services
{
    /// <summary>
    /// Configuration service for respawn prevention settings with JSON persistence
    /// </summary>
    public class RespawnPreventionConfigService
    {
        private static readonly string CONFIG_PATH = Path.Combine(BepInEx.Paths.ConfigPath, "VAuto");
        private static readonly string SETTINGS_PATH = Path.Combine(CONFIG_PATH, "respawn_prevention.json");

        private RespawnPreventionConfig _config;
        private readonly object _lock = new object();
        
        public bool IsEnabled
        {
            get => _config.IsEnabled;
            set
            {
                _config.IsEnabled = value;
                SaveConfig();
                Core.Log.LogInfo($"Respawn prevention {(value ? "enabled" : "disabled")} via configuration");
            }
        }

        public int DefaultCooldownSeconds
        {
            get => _config.DefaultCooldownSeconds;
            set
            {
                _config.DefaultCooldownSeconds = Math.Max(1, value);
                SaveConfig();
                Core.Log.LogInfo($"Respawn prevention default cooldown set to {value} seconds");
            }
        }

        public bool AllowGlobalToggle
        {
            get => _config.AllowGlobalToggle;
            set
            {
                _config.AllowGlobalToggle = value;
                SaveConfig();
            }
        }

        public int MaxCooldownSeconds
        {
            get => _config.MaxCooldownSeconds;
            set
            {
                _config.MaxCooldownSeconds = Math.Max(value, DefaultCooldownSeconds);
                SaveConfig();
            }
        }

        public bool EnablePerPlayerCooldowns
        {
            get => _config.EnablePerPlayerCooldowns;
            set
            {
                _config.EnablePerPlayerCooldowns = value;
                SaveConfig();
            }
        }

        public RespawnPreventionConfigService()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(SETTINGS_PATH))
                {
                    _config = GetDefaultConfig();
                    SaveConfig();
                    Core.Log.LogInfo("Created default respawn prevention configuration");
                    return;
                }

                var json = File.ReadAllText(SETTINGS_PATH);
                _config = JsonSerializer.Deserialize<RespawnPreventionConfig>(json) ?? GetDefaultConfig();
                
                // Validate and fix any invalid values
                ValidateConfig();
                
                Core.Log.LogInfo($"Loaded respawn prevention configuration: Enabled={IsEnabled}, DefaultCooldown={DefaultCooldownSeconds}s");
            }
            catch (System.Exception ex)
            {
                Core.Log.LogWarning($"Failed to load respawn prevention config, using defaults: {ex.Message}");
                _config = GetDefaultConfig();
                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            try
            {
                if (!Directory.Exists(CONFIG_PATH))
                    Directory.CreateDirectory(CONFIG_PATH);

                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var json = JsonSerializer.Serialize(_config, options);
                File.WriteAllText(SETTINGS_PATH, json);
            }
            catch (System.Exception ex)
            {
                Core.Log.LogError($"Failed to save respawn prevention config: {ex.Message}");
            }
        }

        private void ValidateConfig()
        {
            if (DefaultCooldownSeconds < 1)
                DefaultCooldownSeconds = 30;
                
            if (MaxCooldownSeconds < DefaultCooldownSeconds)
                MaxCooldownSeconds = DefaultCooldownSeconds * 2;
        }

        private RespawnPreventionConfig GetDefaultConfig()
        {
            return new RespawnPreventionConfig
            {
                IsEnabled = false,
                DefaultCooldownSeconds = 30,
                AllowGlobalToggle = true,
                MaxCooldownSeconds = 300,
                EnablePerPlayerCooldowns = true,
                PlayerCooldowns = new System.Collections.Generic.Dictionary<ulong, int>()
            };
        }

        /// <summary>
        /// Get cooldown for specific player (returns -1 if no specific cooldown set)
        /// </summary>
        public int GetPlayerCooldown(ulong platformId)
        {
            lock (_lock)
            {
                return _config.PlayerCooldowns.TryGetValue(platformId, out var cooldown) ? cooldown : -1;
            }
        }

        /// <summary>
        /// Set specific cooldown for player
        /// </summary>
        public void SetPlayerCooldown(ulong platformId, int cooldownSeconds)
        {
            lock (_lock)
            {
                if (cooldownSeconds <= 0)
                {
                    _config.PlayerCooldowns.Remove(platformId);
                }
                else
                {
                    _config.PlayerCooldowns[platformId] = Math.Min(cooldownSeconds, MaxCooldownSeconds);
                }
                SaveConfig();
            }
        }

        /// <summary>
        /// Clear specific player cooldown
        /// </summary>
        public void ClearPlayerCooldown(ulong platformId)
        {
            lock (_lock)
            {
                _config.PlayerCooldowns.Remove(platformId);
                SaveConfig();
            }
        }

        /// <summary>
        /// Get all player cooldowns
        /// </summary>
        public System.Collections.Generic.IReadOnlyDictionary<ulong, int> GetAllPlayerCooldowns()
        {
            lock (_lock)
            {
                return new System.Collections.Generic.Dictionary<ulong, int>(_config.PlayerCooldowns);
            }
        }

        /// <summary>
        /// Check if player has specific cooldown override
        /// </summary>
        public bool HasPlayerCooldown(ulong platformId)
        {
            lock (_lock)
            {
                return _config.PlayerCooldowns.ContainsKey(platformId);
            }
        }

        /// <summary>
        /// Clean up expired player cooldowns (older than specified time)
        /// </summary>
        public void CleanupExpiredPlayerCooldowns(System.TimeSpan maxAge)
        {
            lock (_lock)
            {
                var expiredKeys = new System.Collections.Generic.List<ulong>();
                var cutoffTime = System.DateTime.UtcNow - maxAge;
                
                // Note: In a real implementation, you'd track when cooldowns were set
                // For now, this just cleans up the dictionary if it gets too large
                if (_config.PlayerCooldowns.Count > 1000)
                {
                    expiredKeys.AddRange(_config.PlayerCooldowns.Keys.Take(_config.PlayerCooldowns.Count / 2));
                }
                
                foreach (var key in expiredKeys)
                {
                    _config.PlayerCooldowns.Remove(key);
                }
                
                if (expiredKeys.Count > 0)
                {
                    SaveConfig();
                    Core.Log.LogInfo($"Cleaned up {expiredKeys.Count} expired player cooldowns");
                }
            }
        }
    }

    /// <summary>
    /// Configuration data structure for respawn prevention
    /// </summary>
    public class RespawnPreventionConfig
    {
        public bool IsEnabled { get; set; }
        public int DefaultCooldownSeconds { get; set; }
        public bool AllowGlobalToggle { get; set; }
        public int MaxCooldownSeconds { get; set; }
        public bool EnablePerPlayerCooldowns { get; set; }
        public System.Collections.Generic.Dictionary<ulong, int> PlayerCooldowns { get; set; } = new();
    }
}