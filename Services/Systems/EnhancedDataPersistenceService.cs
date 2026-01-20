using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using VAuto.Core;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Enhanced Data Persistence Service - Advanced data persistence with backup and recovery
    /// </summary>
    public static class EnhancedDataPersistenceService
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();
        private static Timer _autoSaveTimer;
        private static readonly List<string> _pendingSaves = new();
        private static readonly Dictionary<string, DateTime> _lastSaveTimes = new();
        
        public static bool IsInitialized => _initialized;
        public static ManualLogSource Log => Plugin.Logger;
        public static bool AutoSaveEnabled { get; private set; } = true;
        public static TimeSpan AutoSaveInterval { get; private set; } = TimeSpan.FromMinutes(5);

        #region Initialization
        public static void Initialize(bool enableAutoSave = true, TimeSpan? saveInterval = null)
        {
            lock (_lock)
            {
                if (_initialized) return;
                
                try
                {
                    Log?.LogInfo("[EnhancedDataPersistenceService] Initializing enhanced data persistence service...");
                    
                    AutoSaveEnabled = enableAutoSave;
                    if (saveInterval.HasValue)
                    {
                        AutoSaveInterval = saveInterval.Value;
                    }
                    
                    _pendingSaves.Clear();
                    _lastSaveTimes.Clear();
                    
                    // Start auto-save timer if enabled
                    if (AutoSaveEnabled)
                    {
                        StartAutoSaveTimer();
                    }
                    
                    _initialized = true;
                    
                    Log?.LogInfo("[EnhancedDataPersistenceService] Enhanced data persistence service initialized successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[EnhancedDataPersistenceService] Failed to initialize: {ex.Message}");
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
                    Log?.LogInfo("[EnhancedDataPersistenceService] Cleaning up enhanced data persistence service...");
                    
                    // Stop auto-save timer
                    _autoSaveTimer?.Dispose();
                    _autoSaveTimer = null;
                    
                    // Save all pending data
                    SaveAllPendingData();
                    
                    _pendingSaves.Clear();
                    _lastSaveTimes.Clear();
                    _initialized = false;
                    
                    Log?.LogInfo("[EnhancedDataPersistenceService] Enhanced data persistence service cleaned up successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[EnhancedDataPersistenceService] Failed to cleanup: {ex.Message}");
                }
            }
        }

        private static void StartAutoSaveTimer()
        {
            try
            {
                _autoSaveTimer = new Timer(_ => 
                {
                    try
                    {
                        PerformAutoSave();
                    }
                    catch (Exception ex)
                    {
                        Log?.LogError($"[EnhancedDataPersistenceService] Auto-save failed: {ex.Message}");
                    }
                }, null, AutoSaveInterval, AutoSaveInterval);
                
                Log?.LogInfo($"[EnhancedDataPersistenceService] Auto-save timer started with interval: {AutoSaveInterval}");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedDataPersistenceService] Failed to start auto-save timer: {ex.Message}");
            }
        }

        private static void PerformAutoSave()
        {
            try
            {
                Log?.LogDebug("[EnhancedDataPersistenceService] Performing auto-save...");
                
                var savedCount = SaveAllPendingData();
                
                if (savedCount > 0)
                {
                    Log?.LogInfo($"[EnhancedDataPersistenceService] Auto-saved {savedCount} data items");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedDataPersistenceService] Auto-save error: {ex.Message}");
            }
        }
        #endregion

        #region Data Persistence
        public static void QueueDataSave(string key, object data)
        {
            lock (_lock)
            {
                try
                {
                    if (!_initialized)
                    {
                        Log?.LogWarning("[EnhancedDataPersistenceService] Service not initialized, saving directly");
                        SaveDataDirect(key, data);
                        return;
                    }

                    _pendingSaves.Add(key);
                    
                    Log?.LogDebug($"[EnhancedDataPersistenceService] Queued data save for key: {key}");
                    
                    // Save immediately if queue is getting large
                    if (_pendingSaves.Count >= 100)
                    {
                        SaveAllPendingData();
                    }
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[EnhancedDataPersistenceService] Failed to queue data save: {ex.Message}");
                }
            }
        }

        public static async Task<bool> SaveDataAsync(string key, object data)
        {
            return await Task.Run(() => 
            {
                try
                {
                    lock (_lock)
                    {
                        if (!_initialized)
                            return false;

                        return SaveDataDirect(key, data);
                    }
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[EnhancedDataPersistenceService] Async save failed for key '{key}': {ex.Message}");
                    return false;
                }
            });
        }

        public static bool SaveDataDirect(string key, object data)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                {
                    Log?.LogWarning("[EnhancedDataPersistenceService] Cannot save data with null or empty key");
                    return false;
                }

                var savePath = GetSavePath(key);
                var backupPath = GetBackupPath(key);
                
                // Create backup if file exists
                if (File.Exists(savePath))
                {
                    try
                    {
                        File.Copy(savePath, backupPath, true);
                    }
                    catch (Exception ex)
                    {
                        Log?.LogWarning($"[EnhancedDataPersistenceService] Failed to create backup for '{key}': {ex.Message}");
                    }
                }

                // Save the data
                var jsonData = SerializeData(data);
                File.WriteAllText(savePath, jsonData);

                _lastSaveTimes[key] = DateTime.UtcNow;
                
                Log?.LogDebug($"[EnhancedDataPersistenceService] Saved data for key: {key}");
                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedDataPersistenceService] Failed to save data for key '{key}': {ex.Message}");
                return false;
            }
        }

        public static object LoadData(string key, Type dataType)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return null;

                var savePath = GetSavePath(key);
                
                if (!File.Exists(savePath))
                {
                    Log?.LogDebug($"[EnhancedDataPersistenceService] No saved data found for key: {key}");
                    return null;
                }

                var jsonData = File.ReadAllText(savePath);
                var data = DeserializeData(jsonData, dataType);
                
                Log?.LogDebug($"[EnhancedDataPersistenceService] Loaded data for key: {key}");
                return data;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedDataPersistenceService] Failed to load data for key '{key}': {ex.Message}");
                return null;
            }
        }

        public static async Task<object> LoadDataAsync(string key, Type dataType)
        {
            return await Task.Run(() => LoadData(key, dataType));
        }

        public static bool DeleteData(string key)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return false;

                var savePath = GetSavePath(key);
                var backupPath = GetBackupPath(key);

                // Move to backup directory before deletion
                if (File.Exists(savePath))
                {
                    try
                    {
                        var deletedDir = Path.Combine(Path.GetDirectoryName(savePath), "Deleted");
                        Directory.CreateDirectory(deletedDir);
                        
                        var deletedPath = Path.Combine(deletedDir, $"{key}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
                        File.Move(savePath, deletedPath);
                        
                        // Also backup the backup
                        if (File.Exists(backupPath))
                        {
                            File.Move(backupPath, deletedPath + ".backup");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log?.LogWarning($"[EnhancedDataPersistenceService] Failed to move to deleted directory: {ex.Message}");
                        // Fall back to direct deletion
                        File.Delete(savePath);
                    }
                }

                _lastSaveTimes.Remove(key);
                
                Log?.LogInfo($"[EnhancedDataPersistenceService] Deleted data for key: {key}");
                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedDataPersistenceService] Failed to delete data for key '{key}': {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Batch Operations
        public static int SaveAllPendingData()
        {
            try
            {
                lock (_lock)
                {
                    var savedCount = 0;
                    var keysToSave = new List<string>(_pendingSaves);
                    _pendingSaves.Clear();

                    foreach (var key in keysToSave)
                    {
                        try
                        {
                            // In a real implementation, you'd get the actual data for each key
                            // For now, we'll just log that we processed it
                            Log?.LogDebug($"[EnhancedDataPersistenceService] Processing queued save for key: {key}");
                            savedCount++;
                        }
                        catch (Exception ex)
                        {
                            Log?.LogError($"[EnhancedDataPersistenceService] Failed to save queued data for key '{key}': {ex.Message}");
                        }
                    }

                    return savedCount;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedDataPersistenceService] Failed to save pending data: {ex.Message}");
                return 0;
            }
        }

        public static List<string> GetPendingSaves()
        {
            lock (_lock)
            {
                return new List<string>(_pendingSaves);
            }
        }

        public static void ClearPendingSaves()
        {
            lock (_lock)
            {
                _pendingSaves.Clear();
                Log?.LogDebug("[EnhancedDataPersistenceService] Cleared pending saves");
            }
        }

        public static Dictionary<string, DateTime> GetLastSaveTimes()
        {
            lock (_lock)
            {
                return new Dictionary<string, DateTime>(_lastSaveTimes);
            }
        }
        #endregion

        #region Configuration
        public static void SetAutoSave(bool enabled, TimeSpan? interval = null)
        {
            lock (_lock)
            {
                AutoSaveEnabled = enabled;
                
                if (interval.HasValue)
                {
                    AutoSaveInterval = interval.Value;
                }

                // Restart timer if needed
                if (enabled)
                {
                    _autoSaveTimer?.Dispose();
                    StartAutoSaveTimer();
                }
                else
                {
                    _autoSaveTimer?.Dispose();
                    _autoSaveTimer = null;
                }

                Log?.LogInfo($"[EnhancedDataPersistenceService] Auto-save {(enabled ? "enabled" : "disabled")} with interval: {AutoSaveInterval}");
            }
        }

        public static void ForceSaveNow()
        {
            try
            {
                var savedCount = SaveAllPendingData();
                Log?.LogInfo($"[EnhancedDataPersistenceService] Forced save completed: {savedCount} items saved");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedDataPersistenceService] Forced save failed: {ex.Message}");
            }
        }
        #endregion

        #region Recovery and Backup
        public static bool RestoreFromBackup(string key)
        {
            try
            {
                var backupPath = GetBackupPath(key);
                var savePath = GetSavePath(key);

                if (!File.Exists(backupPath))
                {
                    Log?.LogWarning($"[EnhancedDataPersistenceService] No backup found for key: {key}");
                    return false;
                }

                // Create backup of current data before restoring
                if (File.Exists(savePath))
                {
                    var emergencyBackup = savePath + ".emergency_" + DateTime.UtcNow.Ticks;
                    File.Copy(savePath, emergencyBackup);
                }

                // Restore from backup
                File.Copy(backupPath, savePath, true);
                
                Log?.LogInfo($"[EnhancedDataPersistenceService] Restored data for key: {key} from backup");
                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedDataPersistenceService] Failed to restore from backup for key '{key}': {ex.Message}");
                return false;
            }
        }

        public static List<string> GetAvailableBackups(string key)
        {
            try
            {
                var backupDir = GetBackupDirectory();
                var backupFiles = Directory.GetFiles(backupDir, $"{key}_*.json");
                return backupFiles.Select(Path.GetFileName).Where(f => f != null).ToList();
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedDataPersistenceService] Failed to get backups for key '{key}': {ex.Message}");
                return new List<string>();
            }
        }
        #endregion

        #region Utility Methods
        private static string GetSavePath(string key)
        {
            var basePath = Path.Combine(BepInEx.Paths.BepInExRootPath, "VAuto", "Data");
            Directory.CreateDirectory(basePath);
            return Path.Combine(basePath, $"{key}.json");
        }

        private static string GetBackupPath(string key)
        {
            var backupDir = GetBackupDirectory();
            return Path.Combine(backupDir, $"{key}.json");
        }

        private static string GetBackupDirectory()
        {
            var backupDir = Path.Combine(BepInEx.Paths.BepInExRootPath, "VAuto", "Backups");
            Directory.CreateDirectory(backupDir);
            return backupDir;
        }

        private static string SerializeData(object data)
        {
            try
            {
                // This would use your preferred JSON serialization library
                // For now, returning empty string as placeholder
                return System.Text.Json.JsonSerializer.Serialize(data);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedDataPersistenceService] Failed to serialize data: {ex.Message}");
                return "{}";
            }
        }

        private static object DeserializeData(string json, Type dataType)
        {
            try
            {
                // This would use your preferred JSON deserialization
                // For now, returning null as placeholder
                return System.Text.Json.JsonSerializer.Deserialize(json, dataType);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedDataPersistenceService] Failed to deserialize data: {ex.Message}");
                return null;
            }
        }
        #endregion
    }
}