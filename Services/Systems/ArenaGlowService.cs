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

namespace VAuto.Services.Systems
{
    /// <summary>
    /// PURE ZONE VISUALIZATION - Spawns and manages arena border glow entities.
    /// MUST NOT: Know about players, react to .arena enter, store PlatformId, track characters.
    /// Glow = pure zone visualization.
    /// </summary>
    public sealed class ArenaGlowService : VAuto.Services.Interfaces.IService
    {
        private static readonly Lazy<ArenaGlowService> _instance = new(() => new ArenaGlowService());
        public static ArenaGlowService Instance => _instance.Value;

        // ─────────────────────────────────────────────
        // CONFIG
        // ─────────────────────────────────────────────

        private const float DEFAULT_SPACING = 3.0f;
        private const float DEFAULT_HEIGHT_OFFSET = 0.25f;

        // ─────────────────────────────────────────────
        // STATE
        // ─────────────────────────────────────────────

        private bool _isInitialized;
        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => Plugin.Logger;

        /// <summary>
        /// Tracks all glow entities spawned per arena.
        /// </summary>
        private readonly Dictionary<int, List<Entity>> _arenaGlowEntities = new();
        private readonly Dictionary<int, float> _glowTimers = new();
        private readonly Dictionary<int, ArenaZone> _activeGlowZones = new();
        private readonly Dictionary<string, GlowData> _glows = new();

        // ─────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────

        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            Log?.LogInfo("[ArenaGlowService] Initialized");
        }

        public void Cleanup()
        {
            if (!_isInitialized) return;
            ClearAll();
            _isInitialized = false;
            Log?.LogInfo("[ArenaGlowService] Cleaned up");
        }

        /// <summary>
        /// Builds (or rebuilds) border glows for an arena zone.
        /// Safe to call multiple times.
        /// </summary>
        public void BuildBorderGlows(int arenaId, ArenaZone zone)
        {
            if (!zone.IsValid)
            {
                Plugin.Logger?.LogWarning($"[ArenaGlowService] Invalid zone for arena {arenaId}");
                return;
            }

            ClearArenaGlows(arenaId);

            var positions = ComputeBorderPositions(zone, DEFAULT_SPACING);
            if (positions.Count == 0)
            {
                Plugin.Logger?.LogWarning($"[ArenaGlowService] No border positions computed for arena {arenaId}");
                return;
            }

            // Use Item_Consumable_Potion_HealingPotion as placeholder glow (will be replaced with actual glow prefab)
            var glowPrefab = VAuto.Data.Prefabs.Item_Consumable_HealingPotion_T01;
            var spawned = SpawnGlowPrefabs(positions, glowPrefab);

            _arenaGlowEntities[arenaId] = spawned;

            if (spawned.Count > 0)
            {
                Plugin.Logger?.LogInfo(
                    $"[ArenaGlowService] Spawned {spawned.Count} border glows for arena {arenaId}"
                );
            }
            else
            {
                Plugin.Logger?.LogWarning(
                    $"[ArenaGlowService] Failed to spawn any glows for arena {arenaId}"
                );
            }
        }

        /// <summary>
        /// Removes all glow entities associated with an arena.
        /// </summary>
        public void ClearArenaGlows(int arenaId)
        {
            if (!_arenaGlowEntities.TryGetValue(arenaId, out var entities))
                return;

            foreach (var entity in entities)
            {
                if (VAuto.Core.Core.Exists(entity))
                {
                    VAuto.Core.Core.EntityManager.DestroyEntity(entity);
                }
            }

            entities.Clear();
            _arenaGlowEntities.Remove(arenaId);
        }

        /// <summary>
        /// Clears all arena glow entities (server shutdown / reload).
        /// </summary>
        public void ClearAll()
        {
            foreach (var arenaId in _arenaGlowEntities.Keys)
            {
                ClearArenaGlows(arenaId);
            }

            _arenaGlowEntities.Clear();
        }

        public void SpawnZoneGlow(ArenaZone zone, PrefabGUID prefab)
        {
            // Use a temporary ID for manual command glows
            int tempId = zone.Name.GetHashCode();
            var positions = ComputeBorderPositions(zone, zone.GlowSpacing > 0 ? zone.GlowSpacing : DEFAULT_SPACING);
            var spawned = SpawnGlowPrefabs(positions, prefab);
            _arenaGlowEntities[tempId] = spawned;
            _activeGlowZones[tempId] = zone;

            if (zone.TimerSeconds > 0)
            {
                _glowTimers[tempId] = zone.TimerSeconds;
            }
        }

        public void ClearAllGlows()
        {
            ClearAll();
        }

        /// <summary>
        /// Updates timers for glow zones. Should be called from a system update.
        /// </summary>
        public void OnUpdate(float deltaTime)
        {
            var expired = new List<int>();
            var keys = new List<int>(_glowTimers.Keys);

            foreach (var id in keys)
            {
                // Skip endless timers (-1)
                if (_glowTimers[id] < 0)
                    continue;

                _glowTimers[id] -= deltaTime;
                if (_glowTimers[id] <= 0)
                {
                    expired.Add(id);
                }
            }

            foreach (var id in expired)
            {
                if (_activeGlowZones.TryGetValue(id, out var zone) && zone.IsRepeating)
                {
                    // For repeating zones, set timer to -1 (endless)
                    _glowTimers[id] = -1f;
                    // Keep the glow entities active indefinitely
                }
                else
                {
                    ClearArenaGlows(id);
                    _glowTimers.Remove(id);
                    _activeGlowZones.Remove(id);
                }
            }
        }

        /// <summary>
        /// Get glow entities for arena (used by ArenaZoneService).
        /// </summary>
        public List<Entity> GetGlowEntities(int arenaId)
        {
            return _arenaGlowEntities.TryGetValue(arenaId, out var entities) ? entities : new List<Entity>();
        }

        public static List<string> GetAllGlowNames() => Instance._glows.Keys.ToList();
        public static GlowData GetGlow(string name) => Instance._glows.TryGetValue(name, out var data) ? data : null;
        public static void UpdateGlow(string name, float? intensity = null, float4? color = null, float3? position = null, float? radius = null)
        {
            if (Instance._glows.TryGetValue(name, out var data))
            {
                if (intensity.HasValue) data.Intensity = intensity.Value;
                if (color.HasValue) data.Color = color.Value;
                if (position.HasValue) data.Position = position.Value;
                if (radius.HasValue) data.Radius = radius.Value;
            }
        }

        public static int GetActiveGlowCount() => Instance._glows.Count;

        // ─────────────────────────────────────────────
        // BORDER COMPUTATION
        // ─────────────────────────────────────────────

        private static List<float3> ComputeBorderPositions(ArenaZone zone, float spacing)
        {
            return zone.Shape switch
            {
                ArenaZoneShape.Circle => ComputeCircleBorder(zone.Center, zone.Radius, spacing),
                ArenaZoneShape.Box => ComputeBoxBorder(zone.Center, zone.Dimensions, spacing),
                _ => new List<float3>()
            };
        }

        private static List<float3> ComputeCircleBorder(
            float3 center,
            float radius,
            float spacing
        )
        {
            var positions = new List<float3>();

            var circumference = 2f * math.PI * radius;
            var count = math.max(6, (int)(circumference / spacing));

            for (var i = 0; i < count; i++)
            {
                var angle = (i / (float)count) * math.PI * 2f;

                var pos = new float3(
                    center.x + math.cos(angle) * radius,
                    center.y + DEFAULT_HEIGHT_OFFSET,
                    center.z + math.sin(angle) * radius
                );

                positions.Add(pos);
            }

            return positions;
        }

        private static List<float3> ComputeBoxBorder(
            float3 center,
            float2 dimensions,
            float spacing
        )
        {
            var positions = new List<float3>();
            var halfWidth = dimensions.x / 2f;
            var halfHeight = dimensions.y / 2f;

            // Top & Bottom edges
            for (float x = -halfWidth; x <= halfWidth; x += spacing)
            {
                positions.Add(center + new float3(x, DEFAULT_HEIGHT_OFFSET, halfHeight));
                positions.Add(center + new float3(x, DEFAULT_HEIGHT_OFFSET, -halfHeight));
            }

            // Left & Right edges (skip corners already added)
            for (float z = -halfHeight + spacing; z < halfHeight; z += spacing)
            {
                positions.Add(center + new float3(halfWidth, DEFAULT_HEIGHT_OFFSET, z));
                positions.Add(center + new float3(-halfWidth, DEFAULT_HEIGHT_OFFSET, z));
            }

            return positions;
        }

        // ─────────────────────────────────────────────
        // SPAWNING
        // ─────────────────────────────────────────────

        private static List<Entity> SpawnGlowPrefabs(
            List<float3> positions,
            PrefabGUID prefabGuid
        )
        {
            var entities = new List<Entity>(positions.Count);

            // Skip if GUID is invalid (0 or negative)
            if (prefabGuid.GuidHash == 0 || prefabGuid.GuidHash < 0)
            {
                Plugin.Logger?.LogDebug(
                    $"[ArenaGlowService] Skipping glow spawn - invalid prefab GUID {prefabGuid.GuidHash}"
                );
                return entities;
            }

            // Get prefab entity from PrefabGUID
            var prefabCollectionSystem = VAuto.Core.VRisingCore.GetSystem<ProjectM.PrefabCollectionSystem>();
            if (prefabCollectionSystem == null)
            {
                Plugin.Logger?.LogWarning("[ArenaGlowService] PrefabCollectionSystem not available");
                return entities;
            }

            if (!prefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(prefabGuid, out var prefabEntity))
            {
                Plugin.Logger?.LogWarning(
                    $"[ArenaGlowService] Prefab GUID {prefabGuid.GuidHash} not found in collection"
                );
                return entities;
            }

            foreach (var position in positions)
            {
                try
                {
                    var entity = VAuto.Core.Core.EntityManager.Instantiate(prefabEntity);
                    
                    if (entity != Entity.Null)
                    {
                        VAuto.Core.Core.Write(entity, new Translation { Value = position });
                        entities.Add(entity);
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogWarning(
                        $"[ArenaGlowService] Failed to instantiate prefab: {ex.Message}"
                    );
                }
            }

            return entities;
        }
    }

    public enum GlowType
    {
        Circular,
        Boundary,
        Point,
        Linear,
        Area
    }

    public class GlowData
    {
        public string Name { get; set; }
        public GlowType Type { get; set; }
        public float Intensity { get; set; }
        public float4 Color { get; set; }
        public float3 Position { get; set; }
        public quaternion Rotation { get; set; }
        public float Radius { get; set; }
        public bool IsActive { get; set; }
    }
}