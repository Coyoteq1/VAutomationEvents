using System;
using System.Collections.Generic;
using System.Linq;
using Stunlock.Core;
using VAuto.Services.Interfaces;
using VAuto.Core;
using VAuto.Data;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Service for resolving prefab GUIDs from names and validating prefab existence
    /// Supports VBlood bosses, items, and other game prefabs
    /// </summary>
    public class PrefabResolverService : IService, IServiceHealthMonitor
    {
        private static PrefabResolverService _instance;
        public static PrefabResolverService Instance => _instance ??= new PrefabResolverService();

        private bool _isInitialized;
        private ManualLogSource _log;

        // Cached lookups for performance
        private readonly Dictionary<string, PrefabGUID> _nameToGuidCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, string> _guidToNameCache = new();

        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => _log;

        public void Initialize()
        {
            if (_isInitialized) return;
            _log = ServiceManager.Log;

            // Build caches from known prefabs
            BuildCaches();

            _isInitialized = true;
            _log?.LogInfo("[PrefabResolverService] Initialized with caches built");
        }

        public void Cleanup()
        {
            _nameToGuidCache.Clear();
            _guidToNameCache.Clear();
            _isInitialized = false;
        }

        private void BuildCaches()
        {
            // Add VBlood bosses
            foreach (var boss in VBloodMapper.GetAllVBloodBosses())
            {
                // VBlood GUIDs are stored as int hashes, convert to PrefabGUID
                var prefabGuid = new PrefabGUID(boss.GuidHash);
                _nameToGuidCache[boss.Name] = prefabGuid;
                _guidToNameCache[boss.GuidHash] = boss.Name;
            }

            // Add weapon mappings from Prefabs.cs
            foreach (var kvp in Prefabs.Weapons)
            {
                _nameToGuidCache[kvp.Key] = kvp.Value;
                _guidToNameCache[kvp.Value.GuidHash] = kvp.Key;
            }

            // Add item mappings (weapons are already included above)
            // You can extend this to include other item types from Prefabs.cs

            _log?.LogInfo($"[PrefabResolverService] Built caches with {_nameToGuidCache.Count} name->GUID mappings");
        }

        /// <summary>
        /// Resolve a prefab GUID from a name (case-insensitive)
        /// </summary>
        public PrefabGUID? ResolvePrefab(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (_nameToGuidCache.TryGetValue(name, out var guid))
            {
                return guid;
            }

            // Try partial matching for common variations
            var matches = _nameToGuidCache.Keys
                .Where(k => k.Contains(name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 1)
            {
                _log?.LogDebug($"[PrefabResolverService] Resolved '{name}' to '{matches[0]}'");
                return _nameToGuidCache[matches[0]];
            }
            else if (matches.Count > 1)
            {
                _log?.LogWarning($"[PrefabResolverService] Ambiguous name '{name}', matches: {string.Join(", ", matches)}");
                return null;
            }

            _log?.LogWarning($"[PrefabResolverService] Could not resolve prefab name: {name}");
            return null;
        }

        /// <summary>
        /// Get the name for a prefab GUID
        /// </summary>
        public string GetPrefabName(PrefabGUID guid)
        {
            return _guidToNameCache.TryGetValue(guid.GuidHash, out var name) ? name : $"Unknown_{guid.GuidHash}";
        }

        /// <summary>
        /// Get the name for a GUID hash
        /// </summary>
        public string GetPrefabName(int guidHash)
        {
            return _guidToNameCache.TryGetValue(guidHash, out var name) ? name : $"Unknown_{guidHash}";
        }

        /// <summary>
        /// Validate if a prefab GUID exists
        /// </summary>
        public bool IsValidPrefab(PrefabGUID guid)
        {
            return _guidToNameCache.ContainsKey(guid.GuidHash);
        }

        /// <summary>
        /// Validate if a GUID hash exists
        /// </summary>
        public bool IsValidPrefab(int guidHash)
        {
            return _guidToNameCache.ContainsKey(guidHash);
        }

        /// <summary>
        /// Get all known prefab names
        /// </summary>
        public IEnumerable<string> GetAllPrefabNames()
        {
            return _nameToGuidCache.Keys;
        }

        /// <summary>
        /// Get all known prefab GUIDs
        /// </summary>
        public IEnumerable<PrefabGUID> GetAllPrefabGuids()
        {
            return _nameToGuidCache.Values;
        }

        /// <summary>
        /// Resolve multiple prefab names at once
        /// </summary>
        public Dictionary<string, PrefabGUID?> ResolvePrefabs(IEnumerable<string> names)
        {
            var results = new Dictionary<string, PrefabGUID?>();
            foreach (var name in names)
            {
                results[name] = ResolvePrefab(name);
            }
            return results;
        }

        /// <summary>
        /// Validate multiple GUIDs at once
        /// </summary>
        public Dictionary<int, bool> ValidateGuids(IEnumerable<int> guidHashes)
        {
            var results = new Dictionary<int, bool>();
            foreach (var hash in guidHashes)
            {
                results[hash] = IsValidPrefab(hash);
            }
            return results;
        }

        /// <summary>
        /// Add a custom prefab mapping (for dynamic prefabs)
        /// </summary>
        public bool AddCustomMapping(string name, PrefabGUID guid)
        {
            if (string.IsNullOrEmpty(name) || guid.GuidHash == 0) return false;

            _nameToGuidCache[name] = guid;
            _guidToNameCache[guid.GuidHash] = name;

            _log?.LogInfo($"[PrefabResolverService] Added custom mapping: {name} -> {guid.GuidHash}");
            return true;
        }

        /// <summary>
        /// Remove a custom prefab mapping
        /// </summary>
        public bool RemoveCustomMapping(string name)
        {
            if (!_nameToGuidCache.TryGetValue(name, out var guid)) return false;

            _nameToGuidCache.Remove(name);
            _guidToNameCache.Remove(guid.GuidHash);

            _log?.LogInfo($"[PrefabResolverService] Removed custom mapping: {name}");
            return true;
        }

        #region Zone System Methods

        /// <summary>
        /// Resolve a mob name for zone validation
        /// </summary>
        public bool ResolveMobName(string mobName)
        {
            if (string.IsNullOrEmpty(mobName)) return false;

            // Check if mob exists in our cache (simplified - would integrate with actual mob database)
            // For now, we'll assume all mob names are valid if they're not empty
            // In a real implementation, you would check against a mob database or prefab list
            return !string.IsNullOrEmpty(mobName);
        }

        /// <summary>
        /// Resolve a boss name for zone validation
        /// </summary>
        public bool ResolveBossName(string bossName)
        {
            if (string.IsNullOrEmpty(bossName)) return false;

            // Check if boss exists in our VBlood cache
            return _nameToGuidCache.ContainsKey(bossName);
        }

        /// <summary>
        /// Resolve an item name for zone validation
        /// </summary>
        public bool ResolveItemName(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return false;

            // Check if item exists in our cache
            return _nameToGuidCache.ContainsKey(itemName);
        }

        /// <summary>
        /// Resolve a blood type for zone validation
        /// </summary>
        public bool ResolveBloodType(string bloodType)
        {
            if (string.IsNullOrEmpty(bloodType)) return false;

            // Check if blood type exists in our VBlood cache
            // This would integrate with your blood type system
            // For now, we'll assume common blood types are valid
            var validBloodTypes = new[] { "Worker", "Creature", "Rogue", "Scholar", "Warrior", "Brute" };
            return validBloodTypes.Contains(bloodType, StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        public ServiceHealthStatus GetHealthStatus()
        {
            return new ServiceHealthStatus
            {
                ServiceName = "PrefabResolverService",
                IsHealthy = _isInitialized,
                Status = _isInitialized ? "Active" : "Inactive",
                LastCheck = DateTime.UtcNow
            };
        }

        public ServicePerformanceMetrics GetPerformanceMetrics()
        {
            return new ServicePerformanceMetrics
            {
                ServiceName = "PrefabResolverService",
                ActiveOperations = _nameToGuidCache.Count,
                MeasuredAt = DateTime.UtcNow
            };
        }

        public int GetErrorCount() => 0;
        public string GetLastError() => null;
    }
}