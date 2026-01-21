using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using VAuto.Data.Zones;
using VAuto.Utilities;
using BepInEx.Logging;
using VampireCommandFramework;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Zone service for JSON-based zone management
    /// For zone system requirements
    /// </summary>
    public static class ZoneService
    {
        private static ZoneConfig _zoneConfig;
        private static readonly string _zonesPath = Path.Combine(Plugin.VAutoDataDir, "zones.json");
        public static ManualLogSource Log => Plugin.Logger;

        /// <summary>
        /// Initialize zone service
        /// </summary>
        public static void Initialize()
        {
            try
            {
                _zoneConfig = MinimalJsonUtil.DeserializeFromFile<ZoneConfig>(_zonesPath);
                if (_zoneConfig == null)
                {
                    _zoneConfig = new ZoneConfig();
                    SaveZones();
                }
                Log?.LogInfo($"[ZoneService] Loaded {_zoneConfig.Zones.Count} zones");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ZoneService] Failed to initialize: {ex.Message}");
                _zoneConfig = new ZoneConfig();
            }
        }

        /// <summary>
        /// Get all zones
        /// </summary>
        public static List<ZoneData> GetZones()
        {
            return _zoneConfig?.Zones ?? new List<ZoneData>();
        }

        /// <summary>
        /// Get zone by ID
        /// </summary>
        public static ZoneData? GetZone(string id)
        {
            return _zoneConfig?.Zones?.Find(z => z.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Add or update zone
        /// </summary>
        public static bool SaveZone(ZoneData zone)
        {
            try
            {
                if (_zoneConfig == null) return false;

                var existingIndex = _zoneConfig.Zones.FindIndex(z => z.Id.Equals(zone.Id, StringComparison.OrdinalIgnoreCase));
                if (existingIndex >= 0)
                {
                    _zoneConfig.Zones[existingIndex] = zone;
                }
                else
                {
                    _zoneConfig.Zones.Add(zone);
                }

                _zoneConfig.LastUpdated = DateTime.UtcNow;
                return SaveZones();
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ZoneService] Failed to save zone {zone.Id}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Remove zone
        /// </summary>
        public static bool RemoveZone(string id)
        {
            try
            {
                if (_zoneConfig == null) return false;

                var zone = _zoneConfig.Zones.Find(z => z.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
                if (zone != null)
                {
                    _zoneConfig.Zones.Remove(zone);
                    _zoneConfig.LastUpdated = DateTime.UtcNow;
                    return SaveZones();
                }
                return false;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ZoneService] Failed to remove zone {id}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if position is within any zone
        /// </summary>
        public static ZoneData? GetZoneAtPosition(float3 position)
        {
            var zones = GetZones();
            foreach (var zone in zones)
            {
                if (!zone.Enabled) continue;
                
                var distance = math.distance(position, zone.Center);
                if (distance <= zone.Radius)
                {
                    return zone;
                }
            }
            return null;
        }

        /// <summary>
        /// Save zones to file
        /// </summary>
        private static bool SaveZones()
        {
            try
            {
                return JsonUtil.SerializeToFile(_zoneConfig, _zonesPath);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ZoneService] Failed to save zones: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Snapshot service for JSON-based snapshot management with hot reload
    /// For snapshot system requirements with database folder structure
    /// </summary>
    public static class SnapshotService
    {
        private static SnapshotConfig _snapshotConfig;
        private static readonly string _databasePath = Path.Combine(Plugin.VAutoDataDir, "Database");
        private static readonly string _snapshotsPath = Path.Combine(_databasePath, "snapshots.json");
        private static readonly FileSystemWatcher _fileWatcher;
        private static readonly object _lock = new object();
        public static ManualLogSource Log => Plugin.Logger;
        public static bool HotReloadEnabled { get; private set; } = true;

        static SnapshotService()
        {
            // Initialize file watcher for hot reload
            _fileWatcher = new FileSystemWatcher(_databasePath, "snapshots.json")
            {
                EnableRaisingEvents = false
            };
            _fileWatcher.Changed += OnSnapshotFileChanged;
            _fileWatcher.Created += OnSnapshotFileChanged;
        }

        /// <summary>
        /// Initialize snapshot service with database folder structure
        /// </summary>
        public static void Initialize()
        {
            try
            {
                // Ensure database directory exists
                if (!Directory.Exists(_databasePath))
                {
                    Directory.CreateDirectory(_databasePath);
                    Log?.LogInfo($"[SnapshotService] Created database directory: {_databasePath}");
                }

                LoadSnapshots();
                
                // Enable hot reload if configured
                if (HotReloadEnabled)
                {
                    _fileWatcher.EnableRaisingEvents = true;
                    Log?.LogInfo("[SnapshotService] Hot reload enabled for snapshots");
                }

                Log?.LogInfo($"[SnapshotService] Loaded {_snapshotConfig.Snapshots.Count} snapshots from database");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SnapshotService] Failed to initialize: {ex.Message}");
                _snapshotConfig = new SnapshotConfig();
            }
        }

        /// <summary>
        /// Load snapshots from file (for hot reload)
        /// </summary>
        private static void LoadSnapshots()
        {
            try
            {
                var newConfig = JsonUtil.DeserializeFromFile<SnapshotConfig>(_snapshotsPath);
                if (newConfig != null)
                {
                    lock (_lock)
                    {
                        _snapshotConfig = newConfig;
                    }
                    Log?.LogInfo($"[SnapshotService] Reloaded {_snapshotConfig.Snapshots.Count} snapshots");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SnapshotService] Failed to load snapshots: {ex.Message}");
                lock (_lock)
                {
                    _snapshotConfig ??= new SnapshotConfig();
                }
            }
        }

        /// <summary>
        /// Hot reload event handler
        /// </summary>
        private static void OnSnapshotFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Debounce file system events
                System.Threading.Thread.Sleep(100);
                
                Log?.LogInfo("[SnapshotService] Snapshot file changed, reloading...");
                LoadSnapshots();
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SnapshotService] Hot reload failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all snapshots (thread-safe)
        /// </summary>
        public static List<PlayerSnapshot> GetSnapshots()
        {
            lock (_lock)
            {
                return _snapshotConfig?.Snapshots?.ToList() ?? new List<PlayerSnapshot>();
            }
        }

        /// <summary>
        /// Get snapshot by player ID (thread-safe)
        /// </summary>
        public static PlayerSnapshot? GetSnapshot(string playerId)
        {
            lock (_lock)
            {
                return _snapshotConfig?.Snapshots?.Find(s => s.PlayerId.Equals(playerId, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Save player snapshot (thread-safe)
        /// </summary>
        public static bool SaveSnapshot(PlayerSnapshot snapshot)
        {
            try
            {
                if (_snapshotConfig == null) return false;

                lock (_lock)
                {
                    var existingIndex = _snapshotConfig.Snapshots.FindIndex(s => s.PlayerId.Equals(snapshot.PlayerId, StringComparison.OrdinalIgnoreCase));
                    if (existingIndex >= 0)
                    {
                        _snapshotConfig.Snapshots[existingIndex] = snapshot;
                    }
                    else
                    {
                        _snapshotConfig.Snapshots.Add(snapshot);
                    }

                    _snapshotConfig.LastUpdated = DateTime.UtcNow;
                    
                    // Auto cleanup if enabled
                    if (_snapshotConfig.AutoCleanup && _snapshotConfig.Snapshots.Count > _snapshotConfig.MaxSnapshots)
                    {
                        CleanupOldSnapshots();
                    }

                    return SaveSnapshotsToFile();
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SnapshotService] Failed to save snapshot for {snapshot.PlayerId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Remove player snapshot (thread-safe)
        /// </summary>
        public static bool RemoveSnapshot(string playerId)
        {
            try
            {
                lock (_lock)
                {
                    if (_snapshotConfig == null) return false;

                    var snapshot = _snapshotConfig.Snapshots.Find(s => s.PlayerId.Equals(playerId, StringComparison.OrdinalIgnoreCase));
                    if (snapshot != null)
                    {
                        _snapshotConfig.Snapshots.Remove(snapshot);
                        _snapshotConfig.LastUpdated = DateTime.UtcNow;
                        return SaveSnapshotsToFile();
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SnapshotService] Failed to remove snapshot for {playerId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Cleanup old snapshots (thread-safe)
        /// </summary>
        private static void CleanupOldSnapshots()
        {
            if (_snapshotConfig?.Snapshots == null) return;

            _snapshotConfig.Snapshots.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
            
            while (_snapshotConfig.Snapshots.Count > _snapshotConfig.MaxSnapshots)
            {
                _snapshotConfig.Snapshots.RemoveAt(_snapshotConfig.Snapshots.Count - 1);
            }

            Log?.LogInfo($"[SnapshotService] Cleaned up old snapshots, now have {_snapshotConfig.Snapshots.Count}");
        }

        /// <summary>
        /// Save snapshots to file (thread-safe)
        /// </summary>
        private static bool SaveSnapshotsToFile()
        {
            try
            {
                return JsonUtil.SerializeToFile(_snapshotConfig, _snapshotsPath);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SnapshotService] Failed to save snapshots: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Enable/disable hot reload
        /// </summary>
        public static void SetHotReloadEnabled(bool enabled)
        {
            HotReloadEnabled = enabled;
            if (enabled && _fileWatcher != null)
            {
                _fileWatcher.EnableRaisingEvents = true;
                Log?.LogInfo("[SnapshotService] Hot reload enabled");
            }
            else if (_fileWatcher != null)
            {
                _fileWatcher.EnableRaisingEvents = false;
                Log?.LogInfo("[SnapshotService] Hot reload disabled");
            }
        }

        /// <summary>
        /// Get database path
        /// </summary>
        public static string DatabasePath => _databasePath;

        /// <summary>
        /// Force reload snapshots
        /// </summary>
        public static void ReloadSnapshots()
        {
            Log?.LogInfo("[SnapshotService] Force reloading snapshots...");
            LoadSnapshots();
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public static void Cleanup()
        {
            try
            {
                if (_fileWatcher != null)
                {
                    _fileWatcher.EnableRaisingEvents = false;
                    _fileWatcher.Dispose();
                }
                Log?.LogInfo("[SnapshotService] Cleanup completed");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SnapshotService] Cleanup failed: {ex.Message}");
            }
        }
    }
}
