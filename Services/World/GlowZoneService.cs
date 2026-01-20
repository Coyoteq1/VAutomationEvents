using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using ProjectM;
using Stunlock.Core;
using VAuto.Core;
using VAuto.Data;
using BepInEx.Logging;

namespace VAuto.Services.World
{
    /// <summary>
    /// PURE VISUAL Glow Zone Service - world-based, no player logic.
    /// Spawns glow zones based on shapes, no enter/exit tracking.
    /// </summary>
    public static class GlowZoneService
    {
        private static readonly Dictionary<string, List<Entity>> _glowZones = new();
        private static readonly object _lock = new object();
        private static ManualLogSource _log;

        static GlowZoneService()
        {
            _log = Plugin.Logger;
        }

        #region Zone Management
        /// <summary>
        /// Build a circular glow zone.
        /// </summary>
        public static void BuildCircleZone(string zoneName, float3 center, float radius, float spacing = 3.0f, PrefabGUID? glowPrefab = null)
        {
            lock (_lock)
            {
                try
                {
                    ClearZone(zoneName);

                    var positions = ComputeCirclePositions(center, radius, spacing);
                    var prefab = glowPrefab ?? Prefabs.ArenaGlowPrefabs.Default;
                    var spawned = SpawnGlowPrefabs(positions, prefab);

                    _glowZones[zoneName] = spawned;
                    _log?.LogInfo($"[GlowZoneService] Built circular zone '{zoneName}' with {spawned.Count} glows");
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[GlowZoneService] Failed to build circle zone '{zoneName}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Build a box glow zone.
        /// </summary>
        public static void BuildBoxZone(string zoneName, float3 center, float2 dimensions, float spacing = 3.0f, PrefabGUID? glowPrefab = null)
        {
            lock (_lock)
            {
                try
                {
                    ClearZone(zoneName);

                    var positions = ComputeBoxPositions(center, dimensions, spacing);
                    var prefab = glowPrefab ?? Prefabs.ArenaGlowPrefabs.Default;
                    var spawned = SpawnGlowPrefabs(positions, prefab);

                    _glowZones[zoneName] = spawned;
                    _log?.LogInfo($"[GlowZoneService] Built box zone '{zoneName}' with {spawned.Count} glows");
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[GlowZoneService] Failed to build box zone '{zoneName}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Clear a specific glow zone.
        /// </summary>
        public static void ClearZone(string zoneName)
        {
            lock (_lock)
            {
                if (!_glowZones.TryGetValue(zoneName, out var entities))
                    return;

                foreach (var entity in entities)
                {
                    if (VRCore.EntityManager.Exists(entity))
                    {
                        VRCore.EntityManager.DestroyEntity(entity);
                    }
                }

                entities.Clear();
                _glowZones.Remove(zoneName);
                _log?.LogInfo($"[GlowZoneService] Cleared zone '{zoneName}'");
            }
        }

        /// <summary>
        /// Clear all glow zones.
        /// </summary>
        public static void ClearAll()
        {
            lock (_lock)
            {
                var zoneNames = _glowZones.Keys.ToList();
                foreach (var zoneName in zoneNames)
                {
                    ClearZone(zoneName);
                }
                _glowZones.Clear();
                _log?.LogInfo("[GlowZoneService] Cleared all glow zones");
            }
        }
        #endregion

        #region Query Methods
        /// <summary>
        /// Get all active zone names.
        /// </summary>
        public static List<string> GetActiveZones()
        {
            lock (_lock)
            {
                return _glowZones.Keys.ToList();
            }
        }

        /// <summary>
        /// Get glow entities for a zone.
        /// </summary>
        public static List<Entity> GetZoneEntities(string zoneName)
        {
            lock (_lock)
            {
                return _glowZones.TryGetValue(zoneName, out var entities) ? entities : new List<Entity>();
            }
        }

        /// <summary>
        /// Check if zone exists.
        /// </summary>
        public static bool ZoneExists(string zoneName)
        {
            lock (_lock)
            {
                return _glowZones.ContainsKey(zoneName);
            }
        }
        #endregion

        #region Position Computation
        private static List<float3> ComputeCirclePositions(float3 center, float radius, float spacing)
        {
            var positions = new List<float3>();
            var circumference = 2f * math.PI * radius;
            var count = math.max(6, (int)(circumference / spacing));

            for (var i = 0; i < count; i++)
            {
                var angle = (i / (float)count) * math.PI * 2f;
                var pos = new float3(
                    center.x + math.cos(angle) * radius,
                    center.y + 0.25f, // Height offset
                    center.z + math.sin(angle) * radius
                );
                positions.Add(pos);
            }

            return positions;
        }

        private static List<float3> ComputeBoxPositions(float3 center, float2 dimensions, float spacing)
        {
            var positions = new List<float3>();
            var halfWidth = dimensions.x / 2f;
            var halfLength = dimensions.y / 2f;

            // Top & Bottom edges
            for (float x = -halfWidth; x <= halfWidth; x += spacing)
            {
                positions.Add(center + new float3(x, 0.25f, halfLength));
                positions.Add(center + new float3(x, 0.25f, -halfLength));
            }

            // Left & Right edges (skip corners)
            for (float z = -halfLength + spacing; z < halfLength; z += spacing)
            {
                positions.Add(center + new float3(halfWidth, 0.25f, z));
                positions.Add(center + new float3(-halfWidth, 0.25f, z));
            }

            return positions;
        }
        #endregion

        #region Spawning
        private static List<Entity> SpawnGlowPrefabs(List<float3> positions, PrefabGUID prefabGuid)
        {
            var entities = new List<Entity>(positions.Count);

            var prefabCollectionSystem = VRCore.ServerWorld?.GetExistingSystemManaged<ProjectM.PrefabCollectionSystem>();
            if (prefabCollectionSystem == null || !prefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(prefabGuid, out var prefabEntity))
            {
                _log?.LogError($"[GlowZoneService] Failed to find prefab entity for GUID {prefabGuid.GuidHash}");
                return entities;
            }

            foreach (var position in positions)
            {
                var entity = VRCore.EntityManager.Instantiate(prefabEntity);
                if (entity != Entity.Null)
                {
                    VRCore.EntityManager.SetComponentData(entity, new Translation { Value = position });
                    entities.Add(entity);
                }
            }

            return entities;
        }
        #endregion
    }
}
