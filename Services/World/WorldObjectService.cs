using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM;
using Stunlock.Core;
using VAuto.Core;
using BepInEx.Logging;

namespace VAuto.Services.World
{
    /// <summary>
    /// World Object Service - Foundation for world automation.
    /// Tracks spawned objects only - no player logic.
    /// </summary>
    public static class WorldObjectService
    {
        private static readonly List<WorldObjectRecord> _objects = new();
        private static readonly object _lock = new object();
        private static ManualLogSource _log;

        static WorldObjectService()
        {
            _log = Plugin.Logger;
        }

        #region Core Registry
        /// <summary>
        /// Register a spawned world object.
        /// </summary>
        public static void Register(WorldObjectRecord record)
        {
            lock (_lock)
            {
                _objects.Add(record);
                _log?.LogInfo($"[WorldObjectService] Registered {record.Type}: {record.PrefabName} at {record.Position}");
            }
        }

        /// <summary>
        /// Remove a world object and destroy its entity.
        /// </summary>
        public static void Remove(Entity entity)
        {
            lock (_lock)
            {
                var record = _objects.FirstOrDefault(o => o.Entity == entity);
                if (record != null)
                {
                    _objects.Remove(record);
                    _log?.LogInfo($"[WorldObjectService] Removed {record.Type}: {record.PrefabName}");
                }

                if (VRCore.EntityManager.Exists(entity))
                {
                    VRCore.EntityManager.DestroyEntity(entity);
                }
            }
        }

        /// <summary>
        /// Remove world objects by type.
        /// </summary>
        public static void RemoveByType(WorldObjectType type)
        {
            lock (_lock)
            {
                var toRemove = _objects.Where(o => o.Type == type).ToList();
                foreach (var record in toRemove)
                {
                    Remove(record.Entity);
                }
            }
        }

        /// <summary>
        /// Remove world objects within radius.
        /// </summary>
        public static void RemoveInRadius(float3 center, float radius)
        {
            lock (_lock)
            {
                var toRemove = _objects.Where(o => math.distance(o.Position, center) <= radius).ToList();
                foreach (var record in toRemove)
                {
                    Remove(record.Entity);
                }
            }
        }

        /// <summary>
        /// Clear all world objects.
        /// </summary>
        public static void ClearAll()
        {
            lock (_lock)
            {
                foreach (var record in _objects.ToList())
                {
                    Remove(record.Entity);
                }
                _objects.Clear();
                _log?.LogInfo("[WorldObjectService] Cleared all world objects");
            }
        }
        #endregion

        #region Query Methods
        /// <summary>
        /// Get all world objects of specific type.
        /// </summary>
        public static IEnumerable<WorldObjectRecord> GetByType(WorldObjectType type)
        {
            lock (_lock)
            {
                return _objects.Where(o => o.Type == type).ToList();
            }
        }

        /// <summary>
        /// Get world objects within radius.
        /// </summary>
        public static IEnumerable<WorldObjectRecord> GetInRadius(float3 center, float radius)
        {
            lock (_lock)
            {
                return _objects.Where(o => math.distance(o.Position, center) <= radius).ToList();
            }
        }

        /// <summary>
        /// Get world objects by prefab name.
        /// </summary>
        public static IEnumerable<WorldObjectRecord> GetByPrefab(string prefabName)
        {
            lock (_lock)
            {
                return _objects.Where(o => o.PrefabName.Equals(prefabName, StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }

        /// <summary>
        /// Get all world objects.
        /// </summary>
        public static IEnumerable<WorldObjectRecord> GetAll()
        {
            lock (_lock)
            {
                return _objects.ToList();
            }
        }

        /// <summary>
        /// Get count of objects by type.
        /// </summary>
        public static int GetCount(WorldObjectType type)
        {
            lock (_lock)
            {
                return _objects.Count(o => o.Type == type);
            }
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Find nearest object of type.
        /// </summary>
        public static WorldObjectRecord FindNearest(float3 position, WorldObjectType type)
        {
            lock (_lock)
            {
                return _objects
                    .Where(o => o.Type == type)
                    .OrderBy(o => math.distance(o.Position, position))
                    .FirstOrDefault();
            }
        }

        /// <summary>
        /// Check if entity exists in registry.
        /// </summary>
        public static bool Contains(Entity entity)
        {
            lock (_lock)
            {
                return _objects.Any(o => o.Entity == entity);
            }
        }
        #endregion
    }

    /// <summary>
    /// World object types for pure world automation.
    /// </summary>
    public enum WorldObjectType
    {
        Tile,
        Structure,
        Door,
        Glow,
        Trigger,
        Automation
    }

    /// <summary>
    /// World object record - pure data, no player references.
    /// </summary>
    public sealed class WorldObjectRecord
    {
        public Entity Entity { get; set; }
        public WorldObjectType Type { get; set; }
        public string PrefabName { get; set; }
        public float3 Position { get; set; }
        public quaternion Rotation { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
