using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using VAuto;

/// <summary>
/// Queue-based Configuration Manager for VAuto
/// Handles asynchronous configuration loading, parsing, and dynamic updates
/// </summary>
public class ConfigurationManager : IDisposable
{
    // Configuration queues for thread-safe processing
    private readonly ConcurrentQueue<ConfigUpdateRequest> _configUpdateQueue = new();
    private readonly ConcurrentQueue<ConfigParseRequest> _parseQueue = new();
    private readonly ConcurrentQueue<ConfigReloadRequest> _reloadQueue = new();

    // Processing threads
    private Thread _processingThread;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _isRunning = false;

    // Configuration state
    private readonly Dictionary<string, Dictionary<string, string>> _configSections = new();
    private readonly object _configLock = new object();

    // File watchers for dynamic updates
    private FileSystemWatcher _configFileWatcher;
    private FileSystemWatcher _arenaConfigFileWatcher;

    // Paths
    private readonly string _configPath;
    private readonly string _arenaConfigPath;

    // Events
    public event Action<string, string, string> OnConfigurationChanged;
    public event Action OnConfigurationReloaded;

    public ConfigurationManager(string configPath, string arenaConfigPath)
    {
        _configPath = configPath;
        _arenaConfigPath = arenaConfigPath;

        InitializeFileWatchers();
        StartProcessingThread();
    }

    public void Dispose()
    {
        StopProcessingThread();
        DisposeFileWatchers();
    }

    #region Queue-Based Processing

    public void QueueConfigUpdate(string section, string key, string value)
    {
        _configUpdateQueue.Enqueue(new ConfigUpdateRequest
        {
            Section = section,
            Key = key,
            Value = value,
            Timestamp = DateTime.UtcNow
        });
    }

    public void QueueConfigParse(string configFile, string[] configLines)
    {
        _parseQueue.Enqueue(new ConfigParseRequest
        {
            ConfigFile = configFile,
            ConfigLines = configLines,
            Timestamp = DateTime.UtcNow
        });
    }

    public void QueueConfigReload(string configFile)
    {
        _reloadQueue.Enqueue(new ConfigReloadRequest
        {
            ConfigFile = configFile,
            Timestamp = DateTime.UtcNow
        });
    }

    #endregion

    #region Configuration Access
    
    public BepInEx.Configuration.ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string description = "")
    {
        return new BepInEx.Configuration.ConfigEntry<T>(key, defaultValue, description);
    }

    public string GetConfigValue(string section, string key, string defaultValue = "")
    {
        lock (_configLock)
        {
            if (_configSections.TryGetValue(section.ToLower(), out var sectionDict))
            {
                if (sectionDict.TryGetValue(key.ToLower(), out var value))
                {
                    return value;
                }
            }
            return defaultValue;
        }
    }

    public bool GetConfigBool(string section, string key, bool defaultValue = false)
    {
        var value = GetConfigValue(section, key, defaultValue.ToString());
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    public int GetConfigInt(string section, string key, int defaultValue = 0)
    {
        var value = GetConfigValue(section, key, defaultValue.ToString());
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    public float GetConfigFloat(string section, string key, float defaultValue = 0f)
    {
        var value = GetConfigValue(section, key, defaultValue.ToString());
        return float.TryParse(value, out var result) ? result : defaultValue;
    }

    public float3 GetConfigFloat3(string section, string key, float3 defaultValue)
    {
        var value = GetConfigValue(section, key, $"{defaultValue.x},{defaultValue.y},{defaultValue.z}");
        var parts = value.Split(',');
        if (parts.Length == 3 &&
            float.TryParse(parts[0], out var x) &&
            float.TryParse(parts[1], out var y) &&
            float.TryParse(parts[2], out var z))
        {
            return new float3(x, y, z);
        }
        return defaultValue;
    }

    public string[] GetConfigArray(string section, string key, char separator = ',')
    {
        var value = GetConfigValue(section, key, "");
        return string.IsNullOrEmpty(value) ? Array.Empty<string>() : value.Split(separator);
    }

    #endregion

    #region Processing Thread

    private void StartProcessingThread()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _processingThread = new Thread(ProcessingLoop)
        {
            Name = "VAuto-Config-Processor",
            IsBackground = true
        };
        _isRunning = true;
        _processingThread.Start();
        Plugin.Logger?.LogInfo("[ConfigManager] Processing thread started");
    }

    private void StopProcessingThread()
    {
        _isRunning = false;
        _cancellationTokenSource?.Cancel();

        if (_processingThread != null && _processingThread.IsAlive)
        {
            _processingThread.Join(5000); // Wait up to 5 seconds
        }

        Plugin.Logger?.LogInfo("[ConfigManager] Processing thread stopped");
    }

    private void ProcessingLoop()
    {
        while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                // Process config updates
                if (_configUpdateQueue.TryDequeue(out var updateRequest))
                {
                    ProcessConfigUpdate(updateRequest);
                }

                // Process config parsing
                if (_parseQueue.TryDequeue(out var parseRequest))
                {
                    ProcessConfigParse(parseRequest);
                }

                // Process config reloads
                if (_reloadQueue.TryDequeue(out var reloadRequest))
                {
                    ProcessConfigReload(reloadRequest);
                }

                // Small delay to prevent tight loop
                Thread.Sleep(10);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[ConfigManager] Error in processing loop: {ex.Message}");
                Thread.Sleep(1000); // Longer delay on error
            }
        }
    }

    #endregion

    #region Request Processing

    private void ProcessConfigUpdate(ConfigUpdateRequest request)
    {
        try
        {
            lock (_configLock)
            {
                var sectionKey = request.Section.ToLower();
                if (!_configSections.ContainsKey(sectionKey))
                {
                    _configSections[sectionKey] = new Dictionary<string, string>();
                }

                var oldValue = _configSections[sectionKey].ContainsKey(request.Key.ToLower()) ? _configSections[sectionKey][request.Key.ToLower()] : "";
                _configSections[sectionKey][request.Key.ToLower()] = request.Value;

                Plugin.Logger?.LogDebug($"[ConfigManager] Updated [{request.Section}]{request.Key}: '{oldValue}' -> '{request.Value}'");
            }

            // Notify listeners
            OnConfigurationChanged?.Invoke(request.Section, request.Key, request.Value);
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"[ConfigManager] Error processing config update: {ex.Message}");
        }
    }

    private void ProcessConfigParse(ConfigParseRequest request)
    {
        try
        {
            var parsedSections = ParseConfigLines(request.ConfigLines);
            var sectionCount = 0;
            var keyCount = 0;

            lock (_configLock)
            {
                foreach (var section in parsedSections)
                {
                    var sectionKey = section.Key.ToLower();
                    if (!_configSections.ContainsKey(sectionKey))
                    {
                        _configSections[sectionKey] = new Dictionary<string, string>();
                    }

                    foreach (var kvp in section.Value)
                    {
                        _configSections[sectionKey][kvp.Key.ToLower()] = kvp.Value;
                        keyCount++;
                    }
                    sectionCount++;
                }
            }

            Plugin.Logger?.LogInfo($"[ConfigManager] Parsed {sectionCount} sections with {keyCount} keys from {Path.GetFileName(request.ConfigFile)}");
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"[ConfigManager] Error processing config parse: {ex.Message}");
        }
    }

    private void ProcessConfigReload(ConfigReloadRequest request)
    {
        try
        {
            if (File.Exists(request.ConfigFile))
            {
                var lines = File.ReadAllLines(request.ConfigFile);
                QueueConfigParse(request.ConfigFile, lines);
                Plugin.Logger?.LogInfo($"[ConfigManager] Reloaded configuration from {Path.GetFileName(request.ConfigFile)}");
            }
            else
            {
                Plugin.Logger?.LogWarning($"[ConfigManager] Configuration file not found for reload: {request.ConfigFile}");
            }

            // Notify listeners
            OnConfigurationReloaded?.Invoke();
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"[ConfigManager] Error processing config reload: {ex.Message}");
        }
    }

    #endregion

    #region File Watching

    private void InitializeFileWatchers()
    {
        try
        {
            // Watch VAuto.cfg
            if (File.Exists(_configPath))
            {
                _configFileWatcher = new FileSystemWatcher
                {
                    Path = Path.GetDirectoryName(_configPath),
                    Filter = Path.GetFileName(_configPath),
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                };
                _configFileWatcher.Changed += OnConfigFileChanged;
                _configFileWatcher.EnableRaisingEvents = true;
            }

            // Watch gg.vautomation.arena.cfg
            if (File.Exists(_arenaConfigPath))
            {
                _arenaConfigFileWatcher = new FileSystemWatcher
                {
                    Path = Path.GetDirectoryName(_arenaConfigPath),
                    Filter = Path.GetFileName(_arenaConfigPath),
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                };
                _arenaConfigFileWatcher.Changed += OnArenaConfigFileChanged;
                _arenaConfigFileWatcher.EnableRaisingEvents = true;
            }

            Plugin.Logger?.LogInfo("[ConfigManager] File watchers initialized");
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"[ConfigManager] Error initializing file watchers: {ex.Message}");
        }
    }

    private void DisposeFileWatchers()
    {
        _configFileWatcher?.Dispose();
        _arenaConfigFileWatcher?.Dispose();
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce file changes (wait 100ms to avoid multiple triggers)
        Task.Delay(100).ContinueWith(_ =>
        {
            QueueConfigReload(_configPath);
        });
    }

    private void OnArenaConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce file changes (wait 100ms to avoid multiple triggers)
        Task.Delay(100).ContinueWith(_ =>
        {
            QueueConfigReload(_arenaConfigPath);
        });
    }

    #endregion

    #region Configuration Parsing

    private Dictionary<string, Dictionary<string, string>> ParseConfigLines(string[] lines)
    {
        var sections = new Dictionary<string, Dictionary<string, string>>();
        var currentSection = "";

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip comments and empty lines
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                continue;

            // Section headers
            if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
            {
                currentSection = trimmedLine.Trim('[', ']');
                if (!sections.ContainsKey(currentSection))
                {
                    sections[currentSection] = new Dictionary<string, string>();
                }
                continue;
            }

            // Parse key-value pairs
            if (trimmedLine.Contains("=") && !string.IsNullOrEmpty(currentSection))
            {
                var parts = trimmedLine.Split(new char[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    sections[currentSection][key] = value;
                }
            }
        }

        return sections;
    }

    #endregion

    #region Data Structures

    private class ConfigUpdateRequest
    {
        public string Section { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public DateTime Timestamp { get; set; }
    }

    private class ConfigParseRequest
    {
        public string ConfigFile { get; set; }
        public string[] ConfigLines { get; set; }
        public DateTime Timestamp { get; set; }
    }

    private class ConfigReloadRequest
    {
        public string ConfigFile { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}
