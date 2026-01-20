using System;
using System.Collections.Generic;

namespace VAuto.Services.Systems
{
    #region Settings Classes

    public class ZoneSettings
    {
        public string ZoneId { get; set; }
        public string ZoneName { get; set; }
        public bool Enabled { get; set; } = true;
        public float Radius { get; set; } = 50f;
        public float EnterRadius { get; set; } = 45f;
        public float ExitRadius { get; set; } = 55f;
        public List<string> AllowedUsers { get; set; } = new();
        public List<string> BlockedUsers { get; set; } = new();
        public Dictionary<string, object> ZoneProperties { get; set; } = new();
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }

    public class AutomationSettings
    {
        public bool Enabled { get; set; } = true;
        public int UpdateIntervalMs { get; set; } = 1000;
        public bool EnableDebugLogging { get; set; } = false;
        public bool EnablePerformanceMonitoring { get; set; } = true;
        public int MaxConcurrentOperations { get; set; } = 10;
        public Dictionary<string, object> AutomationProperties { get; set; } = new();
        public List<string> EnabledModules { get; set; } = new();
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }

    public class DatabaseSettings
    {
        public bool Enabled { get; set; } = true;
        public string ConnectionString { get; set; } = "";
        public int ConnectionTimeoutSeconds { get; set; } = 30;
        public int CommandTimeoutSeconds { get; set; } = 60;
        public bool EnableConnectionPooling { get; set; } = true;
        public int MaxPoolSize { get; set; } = 100;
        public int MinPoolSize { get; set; } = 5;
        public bool EnableAutoSave { get; set; } = true;
        public int AutoSaveIntervalMinutes { get; set; } = 5;
        public Dictionary<string, object> DatabaseProperties { get; set; } = new();
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }

    public class UISettings
    {
        public bool Enabled { get; set; } = true;
        public string Theme { get; set; } = "Default";
        public float FontSize { get; set; } = 14f;
        public bool ShowDebugInfo { get; set; } = false;
        public bool EnableNotifications { get; set; } = true;
        public List<string> EnabledPanels { get; set; } = new();
        public Dictionary<string, object> UIProperties { get; set; } = new();
        public Dictionary<string, string> KeyBindings { get; set; } = new();
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }

    public class PerformanceSettings
    {
        public bool Enabled { get; set; } = true;
        public bool EnableProfiling { get; set; } = false;
        public int ProfilingIntervalMs { get; set; } = 1000;
        public long MaxMemoryUsageMB { get; set; } = 1024;
        public double MaxCPUUsagePercent { get; set; } = 80.0;
        public bool EnableGarbageCollectionMonitoring { get; set; } = true;
        public bool EnablePerformanceLogging { get; set; } = true;
        public Dictionary<string, object> PerformanceProperties { get; set; } = new();
        public List<string> MonitoredMetrics { get; set; } = new();
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }

    public class SecuritySettings
    {
        public bool Enabled { get; set; } = true;
        public bool EnableAuthentication { get; set; } = true;
        public bool EnableAuthorization { get; set; } = true;
        public int SessionTimeoutMinutes { get; set; } = 30;
        public int MaxLoginAttempts { get; set; } = 5;
        public bool EnableIPWhitelist { get; set; } = false;
        public List<string> WhitelistedIPs { get; set; } = new();
        public bool EnableRateLimiting { get; set; } = true;
        public int MaxRequestsPerMinute { get; set; } = 100;
        public Dictionary<string, object> SecurityProperties { get; set; } = new();
        public List<string> AllowedRoles { get; set; } = new();
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }

    public class UserPreferences
    {
        public ulong PlatformId { get; set; }
        public string Username { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Preferences { get; set; } = new();
        public List<string> FavoriteCommands { get; set; } = new();
        public List<string> RecentCommands { get; set; } = new();
        public string PreferredTheme { get; set; } = "Default";
        public bool EnableNotifications { get; set; } = true;
        public bool EnableSounds { get; set; } = true;
        public Dictionary<string, string> CustomKeyBindings { get; set; } = new();
        public bool IsPremiumUser { get; set; } = false;
        public List<string> Permissions { get; set; } = new();
    }

    #endregion
}
