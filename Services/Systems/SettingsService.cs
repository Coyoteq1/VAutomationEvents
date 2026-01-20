using System;
using System.Collections.Generic;
using System.IO;
using Unity.Entities;
using VAuto.Core;
using VAuto.Utilities;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Settings management service for VAuto plugin
    /// Handles loading, saving, and managing all plugin settings and user preferences
    /// </summary>
    public class SettingsService
    {
        private static SettingsService _instance;
        private VAutoSettings _currentSettings;
        private readonly Dictionary<string, UserPreferences> _userPreferences;
        private readonly string _settingsPath;
        private readonly string _userPreferencesPath;
        private readonly object _lockObject = new object();

        public static SettingsService Instance => _instance ??= new SettingsService();

        private SettingsService()
        {
            _userPreferences = new Dictionary<string, UserPreferences>();
            _settingsPath = Path.Combine(Plugin.ConfigPath, "settings.json");
            _userPreferencesPath = Path.Combine(Plugin.ConfigPath, "UserPreferences");
            
            InitializeDirectories();
            LoadSettings();
        }

        #region Properties

        /// <summary>
        /// Current plugin settings
        /// </summary>
        public VAutoSettings Settings
        {
            get
            {
                lock (_lockObject)
                {
                    return _currentSettings;
                }
            }
        }

        /// <summary>
        /// Core settings shortcut
        /// </summary>
        public CoreSettings Core => Settings?.Core;

        /// <summary>
        /// Arena settings shortcut
        /// </summary>
        public ArenaSettings Arena => Settings?.Arena;

        /// <summary>
        /// Zone settings shortcut
        /// </summary>
        public ZoneSettings Zone => Settings?.Zone;

        /// <summary>
        /// Respawn settings shortcut
        /// </summary>
        public RespawnSettings Respawn => Settings?.Respawn;

        /// <summary>
        /// Automation settings shortcut
        /// </summary>
        public AutomationSettings Automation => Settings?.Automation;

        /// <summary>
        /// Database settings shortcut
        /// </summary>
        public DatabaseSettings Database => Settings?.Database;

        /// <summary>
        /// UI settings shortcut
        /// </summary>
        public UISettings UI => Settings?.UI;

        /// <summary>
        /// Performance settings shortcut
        /// </summary>
        public PerformanceSettings Performance => Settings?.Performance;

        /// <summary>
        /// Security settings shortcut
        /// </summary>
        public SecuritySettings Security => Settings?.Security;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize required directories
        /// </summary>
        private void InitializeDirectories()
        {
            try
            {
                if (!Directory.Exists(_userPreferencesPath))
                {
                    Directory.CreateDirectory(_userPreferencesPath);
                    Plugin.Logger?.LogInfo($"[SettingsService] Created user preferences directory: {_userPreferencesPath}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[SettingsService] Error initializing directories: {ex.Message}");
            }
        }

        /// <summary>
        /// Load settings from file
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                lock (_lockObject)
                {
                    _currentSettings = SettingsJson.LoadSettings(_settingsPath);
                    Plugin.Logger?.LogInfo($"[SettingsService] Loaded settings version {_currentSettings?.Version}");
                }

                // Load user preferences
                LoadUserPreferences();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[SettingsService] Error loading settings: {ex.Message}");
                lock (_lockObject)
                {
                    _currentSettings = SettingsJson.CreateDefaultSettings();
                }
            }
        }

        /// <summary>
        /// Load all user preferences
        /// </summary>
        private void LoadUserPreferences()
        {
            try
            {
                if (!Directory.Exists(_userPreferencesPath))
                    return;

                var preferenceFiles = Directory.GetFiles(_userPreferencesPath, "*.json");
                foreach (var file in preferenceFiles)
                {
                    try
                    {
                        var preferences = SettingsJson.DeserializeUserPreferences(File.ReadAllText(file));
                        if (preferences != null && !string.IsNullOrEmpty(preferences.UserId))
                        {
                            _userPreferences[preferences.UserId] = preferences;
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger?.LogWarning($"[SettingsService] Error loading user preferences from {file}: {ex.Message}");
                    }
                }

                Plugin.Logger?.LogInfo($"[SettingsService] Loaded {_userPreferences.Count} user preferences");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[SettingsService] Error loading user preferences: {ex.Message}");
            }
        }

        #endregion

        #region Settings Management

        /// <summary>
        /// Update settings
        /// </summary>
        public bool UpdateSettings(VAutoSettings newSettings)
        {
            try
            {
                if (newSettings == null)
                    return false;

                lock (_lockObject)
                {
                    // Validate settings
                    var issues = SettingsJson.ValidateSettings(newSettings);
                    if (issues.Count > 0)
                    {
                        Plugin.Logger?.LogWarning($"[SettingsService] Settings validation failed: {string.Join(", ", issues)}");
                        return false;
                    }

                    // Merge with current settings
                    _currentSettings = SettingsJson.MergeSettings(_currentSettings, newSettings);
                    _currentSettings.LastUpdated = DateTime.UtcNow;

                    // Save to file
                    return SaveSettings();
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[SettingsService] Error updating settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Update specific settings section
        /// </summary>
        public bool UpdateCoreSettings(CoreSettings coreSettings)
        {
            if (coreSettings == null)
                return false;

            var newSettings = new VAutoSettings { Core = coreSettings };
            return UpdateSettings(newSettings);
        }

        /// <summary>
        /// Update arena settings
        /// </summary>
        public bool UpdateArenaSettings(ArenaSettings arenaSettings)
        {
            if (arenaSettings == null)
                return false;

            var newSettings = new VAutoSettings { Arena = arenaSettings };
            return UpdateSettings(newSettings);
        }

        /// <summary>
        /// Update zone settings
        /// </summary>
        public bool UpdateZoneSettings(ZoneSettings zoneSettings)
        {
            if (zoneSettings == null)
                return false;

            var newSettings = new VAutoSettings { Zone = zoneSettings };
            return UpdateSettings(newSettings);
        }

        /// <summary>
        /// Update respawn settings
        /// </summary>
        public bool UpdateRespawnSettings(RespawnSettings respawnSettings)
        {
            if (respawnSettings == null)
                return false;

            var newSettings = new VAutoSettings { Respawn = respawnSettings };
            return UpdateSettings(newSettings);
        }

        /// <summary>
        /// Update automation settings
        /// </summary>
        public bool UpdateAutomationSettings(AutomationSettings automationSettings)
        {
            if (automationSettings == null)
                return false;

            var newSettings = new VAutoSettings { Automation = automationSettings };
            return UpdateSettings(newSettings);
        }

        /// <summary>
        /// Update database settings
        /// </summary>
        public bool UpdateDatabaseSettings(DatabaseSettings databaseSettings)
        {
            if (databaseSettings == null)
                return false;

            var newSettings = new VAutoSettings { Database = databaseSettings };
            return UpdateSettings(newSettings);
        }

        /// <summary>
        /// Update UI settings
        /// </summary>
        public bool UpdateUISettings(UISettings uiSettings)
        {
            if (uiSettings == null)
                return false;

            var newSettings = new VAutoSettings { UI = uiSettings };
            return UpdateSettings(newSettings);
        }

        /// <summary>
        /// Update performance settings
        /// </summary>
        public bool UpdatePerformanceSettings(PerformanceSettings performanceSettings)
        {
            if (performanceSettings == null)
                return false;

            var newSettings = new VAutoSettings { Performance = performanceSettings };
            return UpdateSettings(newSettings);
        }

        /// <summary>
        /// Update security settings
        /// </summary>
        public bool UpdateSecuritySettings(SecuritySettings securitySettings)
        {
            if (securitySettings == null)
                return false;

            var newSettings = new VAutoSettings { Security = securitySettings };
            return UpdateSettings(newSettings);
        }

        /// <summary>
        /// Reset settings to defaults
        /// </summary>
        public bool ResetToDefaults()
        {
            try
            {
                lock (_lockObject)
                {
                    _currentSettings = SettingsJson.CreateDefaultSettings();
                    return SaveSettings();
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[SettingsService] Error resetting settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Save settings to file
        /// </summary>
        public bool SaveSettings()
        {
            try
            {
                lock (_lockObject)
                {
                    return SettingsJson.SaveSettings(_currentSettings, _settingsPath);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[SettingsService] Error saving settings: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region User Preferences Management

        /// <summary>
        /// Get user preferences
        /// </summary>
        public UserPreferences GetUserPreferences(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return null;

            lock (_lockObject)
            {
                return _userPreferences.TryGetValue(userId, out var preferences) ? preferences : null;
            }
        }

        /// <summary>
        /// Update user preferences
        /// </summary>
        public bool UpdateUserPreferences(string userId, UserPreferences preferences)
        {
            if (string.IsNullOrEmpty(userId) || preferences == null)
                return false;

            try
            {
                lock (_lockObject)
                {
                    preferences.UserId = userId;
                    preferences.LastUpdated = DateTime.UtcNow;
                    _userPreferences[userId] = preferences;

                    // Save to file
                    var filePath = Path.Combine(_userPreferencesPath, $"{userId}.json");
                    var json = SettingsJson.SerializeUserPreferences(preferences);
                    File.WriteAllText(filePath, json);

                    return true;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[SettingsService] Error updating user preferences for {userId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create default user preferences
        /// </summary>
        public UserPreferences CreateUserPreferences(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return null;

            var preferences = SettingsJson.CreateDefaultUserPreferences();
            preferences.UserId = userId;
            
            if (UpdateUserPreferences(userId, preferences))
                return preferences;
            
            return null;
        }

        /// <summary>
        /// Delete user preferences
        /// </summary>
        public bool DeleteUserPreferences(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            try
            {
                lock (_lockObject)
                {
                    if (_userPreferences.Remove(userId))
                    {
                        // Delete file
                        var filePath = Path.Combine(_userPreferencesPath, $"{userId}.json");
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[SettingsService] Error deleting user preferences for {userId}: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Get all user preferences
        /// </summary>
        public List<UserPreferences> GetAllUserPreferences()
        {
            lock (_lockObject)
            {
                return new List<UserPreferences>(_userPreferences.Values);
            }
        }

        #endregion

        #region Settings Validation and Diagnostics

        /// <summary>
        /// Validate current settings
        /// </summary>
        public List<string> ValidateCurrentSettings()
        {
            lock (_lockObject)
            {
                return SettingsJson.ValidateSettings(_currentSettings);
            }
        }

        /// <summary>
        /// Get settings summary
        /// </summary>
        public string GetSettingsSummary()
        {
            lock (_lockObject)
            {
                if (_currentSettings == null)
                    return "No settings loaded";

                var summary = $"VAuto Settings v{_currentSettings.Version}\n";
                summary += $"Last Updated: {_currentSettings.LastUpdated:yyyy-MM-dd HH:mm:ss}\n\n";

                summary += "Core Settings:\n";
                summary += $"  Plugin Enabled: {_currentSettings.Core?.EnablePlugin}\n";
                summary += $"  Debug Mode: {_currentSettings.Core?.DebugMode}\n";
                summary += $"  Log Level: {_currentSettings.Core?.LogLevel}\n\n";

                summary += "Arena Settings:\n";
                summary += $"  Arena System: {_currentSettings.Arena?.EnableArenaSystem}\n";
                summary += $"  Default Radius: {_currentSettings.Arena?.DefaultArenaRadius}m\n";
                summary += $"  Check Interval: {_currentSettings.Arena?.CheckInterval}s\n\n";

                summary += "Zone Settings:\n";
                summary += $"  Zone System: {_currentSettings.Zone?.EnableZoneSystem}\n";
                summary += $"  Zone Radius: {_currentSettings.Zone?.ZoneRadius}m\n";
                summary += $"  Zone Shape: {_currentSettings.Zone?.ZoneShape}\n\n";

                summary += "Performance Settings:\n";
                summary += $"  Max Entities/Frame: {_currentSettings.Performance?.MaxEntitiesPerFrame}\n";
                summary += $"  Update Interval: {_currentSettings.Performance?.UpdateInterval}s\n";
                summary += $"  Memory Limit: {_currentSettings.Performance?.MemoryLimit}MB\n";

                return summary;
            }
        }

        /// <summary>
        /// Export settings to file
        /// </summary>
        public bool ExportSettings(string filePath)
        {
            try
            {
                lock (_lockObject)
                {
                    var json = SettingsJson.SerializeSettings(_currentSettings);
                    File.WriteAllText(filePath, json);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[SettingsService] Error exporting settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Import settings from file
        /// </summary>
        public bool ImportSettings(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var json = File.ReadAllText(filePath);
                var settings = SettingsJson.DeserializeSettings(json);
                
                return UpdateSettings(settings);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[SettingsService] Error importing settings: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Auto-Save

        /// <summary>
        /// Start auto-save timer
        /// </summary>
        public void StartAutoSave()
        {
            if (Core?.AutoSave != true)
                return;

            // This would be implemented with a timer in a real scenario
            Plugin.Logger?.LogInfo($"[SettingsService] Auto-save enabled (interval: {Core?.AutoSaveInterval}s)");
        }

        /// <summary>
        /// Stop auto-save timer
        /// </summary>
        public void StopAutoSave()
        {
            Plugin.Logger?.LogInfo("[SettingsService] Auto-save stopped");
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleanup old user preferences
        /// </summary>
        public void CleanupOldPreferences(int maxAgeDays = 90)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-maxAgeDays);
                var toRemove = new List<string>();

                lock (_lockObject)
                {
                    foreach (var kvp in _userPreferences)
                    {
                        if (kvp.Value.LastUpdated < cutoffDate)
                        {
                            toRemove.Add(kvp.Key);
                        }
                    }

                    foreach (var userId in toRemove)
                    {
                        DeleteUserPreferences(userId);
                    }
                }

                if (toRemove.Count > 0)
                {
                    Plugin.Logger?.LogInfo($"[SettingsService] Cleaned up {toRemove.Count} old user preferences");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[SettingsService] Error during cleanup: {ex.Message}");
            }
        }

        #endregion
    }
}
