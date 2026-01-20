using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM;
using Stunlock.Core;
using VAuto.Core;
using VAuto.Data;
using VAuto.Services;
using VAuto.Services.Systems;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// ZONE-ONLY STRUCTURE SERVICE - Manages physical arena structures.
    /// MUST NOT: Track players, apply gameplay logic, modify lifecycle.
    /// Buildings = pure zone structures.
    /// </summary>
    public sealed class ArenaBuildingService : VAuto.Services.Interfaces.IService
    {
        private readonly Dictionary<int, List<Entity>> _arenaBuildingEntities = new();
        private readonly object _lock = new object();
        private ManualLogSource _log;
        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => _log;

        public ArenaBuildingService()
        {
            _log = Plugin.Logger;
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            _log?.LogInfo("[ArenaBuildingService] Initialized");
        }

        public void Cleanup()
        {
            if (!_isInitialized) return;
            ClearAll();
            _isInitialized = false;
            _log?.LogInfo("[ArenaBuildingService] Cleaned up");
        }

        #region Zone Structure Management
        /// <summary>
        /// Spawn arena buildings for zone (called by ArenaZoneService only).
        /// </summary>
        public List<Entity> SpawnArenaBuildings(int arenaId, ArenaZone zone)
        {
            if (!zone.IsValid)
            {
                _log?.LogWarning($"[ArenaBuildingService] Invalid zone for arena {arenaId}");
                return new List<Entity>();
            }

            lock (_lock)
            {
                try
                {
                    // Clear existing buildings first
                    DespawnArenaBuildings(arenaId);

                    var spawnedEntities = new List<Entity>();

                    // Spawn arena structures based on zone configuration
                    spawnedEntities.AddRange(SpawnArenaBoundaries(arenaId, zone));
                    spawnedEntities.AddRange(SpawnArenaPlatforms(arenaId, zone));
                    spawnedEntities.AddRange(SpawnArenaDecorations(arenaId, zone));

                    _arenaBuildingEntities[arenaId] = spawnedEntities;

                    _log?.LogInfo($"[ArenaBuildingService] Spawned {spawnedEntities.Count} arena structures for arena {arenaId}");
                    return spawnedEntities;
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[ArenaBuildingService] Failed to spawn arena buildings for {arenaId}: {ex.Message}");
                    return new List<Entity>();
                }
            }
        }

        /// <summary>
        /// Despawn arena buildings for zone (called by ArenaZoneService only).
        /// </summary>
        public void DespawnArenaBuildings(int arenaId)
        {
            lock (_lock)
            {
                try
                {
                    if (!_arenaBuildingEntities.TryGetValue(arenaId, out var entities))
                        return;

                    foreach (var entity in entities)
                    {
                        if (VAuto.Core.Core.Exists(entity))
                        {
                            VAuto.Core.Core.EntityManager.DestroyEntity(entity);
                        }
                    }

                    entities.Clear();
                    _arenaBuildingEntities.Remove(arenaId);

                    _log?.LogInfo($"[ArenaBuildingService] Despawned arena buildings for arena {arenaId}");
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[ArenaBuildingService] Failed to despawn arena buildings for {arenaId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Clear all arena buildings (server shutdown).
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                try
                {
                    var arenaIds = _arenaBuildingEntities.Keys.ToList();
                    foreach (var arenaId in arenaIds)
                    {
                        DespawnArenaBuildings(arenaId);
                    }

                    _arenaBuildingEntities.Clear();
                    _log?.LogInfo("[ArenaBuildingService] Cleared all arena buildings");
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[ArenaBuildingService] Failed to clear all arena buildings: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get building entities for arena (used by ArenaZoneService).
        /// </summary>
        public List<Entity> GetBuildingEntities(int arenaId)
        {
            lock (_lock)
            {
                return _arenaBuildingEntities.TryGetValue(arenaId, out var entities) ? entities : new List<Entity>();
            }
        }
        #endregion

        #region Structure Spawning
        private List<Entity> SpawnArenaBoundaries(int arenaId, ArenaZone zone)
        {
            var entities = new List<Entity>();

            try
            {
                // Spawn boundary markers around the arena perimeter
                var positions = ComputeBoundaryPositions(zone, 10f); // 10m spacing
                
                foreach (var position in positions)
                {
                    var entity = SpawnBoundaryMarker(position);
                    if (entity != Entity.Null)
                    {
                        entities.Add(entity);
                    }
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[ArenaBuildingService] Failed to spawn boundaries for arena {arenaId}: {ex.Message}");
            }

            return entities;
        }

        private List<Entity> SpawnArenaPlatforms(int arenaId, ArenaZone zone)
        {
            var entities = new List<Entity>();

            try
            {
                // Spawn central platform
                var centerPlatform = SpawnPlatform(zone.Center, quaternion.identity);
                if (centerPlatform != Entity.Null)
                {
                    entities.Add(centerPlatform);
                }

                // Spawn corner platforms for larger arenas
                if (zone.Radius > 30f)
                {
                    var cornerPositions = ComputeCornerPositions(zone);
                    foreach (var position in cornerPositions)
                    {
                        var platform = SpawnPlatform(position, quaternion.identity);
                        if (platform != Entity.Null)
                        {
                            entities.Add(platform);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[ArenaBuildingService] Failed to spawn platforms for arena {arenaId}: {ex.Message}");
            }

            return entities;
        }

        private List<Entity> SpawnArenaDecorations(int arenaId, ArenaZone zone)
        {
            var entities = new List<Entity>();

            try
            {
                // Spawn decorative elements around the arena
                var decorationPositions = ComputeDecorationPositions(zone);
                
                foreach (var position in decorationPositions)
                {
                    var entity = SpawnDecoration(position);
                    if (entity != Entity.Null)
                    {
                        entities.Add(entity);
                    }
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[ArenaBuildingService] Failed to spawn decorations for arena {arenaId}: {ex.Message}");
            }

            return entities;
        }
        #endregion

        #region Entity Spawning
        private Entity SpawnBoundaryMarker(float3 position)
        {
            try
            {
                // Use a simple placeholder prefab for boundary markers
                var prefabGuid = VAuto.Data.Prefabs.Item_Weapon_Sword_Iron; // Placeholder
                return SpawnPrefab(prefabGuid, position);
            }
            catch (Exception ex)
            {
                _log?.LogError($"[ArenaBuildingService] Failed to spawn boundary marker at {position}: {ex.Message}");
                return Entity.Null;
            }
        }

        private Entity SpawnPlatform(float3 position, quaternion rotation)
        {
            try
            {
                // Use a placeholder prefab for platforms
                var prefabGuid = VAuto.Data.Prefabs.Item_Shield_Basic_Wood; // Placeholder
                return SpawnPrefab(prefabGuid, position, rotation);
            }
            catch (Exception ex)
            {
                _log?.LogError($"[ArenaBuildingService] Failed to spawn platform at {position}: {ex.Message}");
                return Entity.Null;
            }
        }

        private Entity SpawnDecoration(float3 position)
        {
            try
            {
                // Use a placeholder prefab for decorations
                var prefabGuid = VAuto.Data.Prefabs.Item_Consumable_HealingPotion_T01; // Placeholder
                return SpawnPrefab(prefabGuid, position);
            }
            catch (Exception ex)
            {
                _log?.LogError($"[ArenaBuildingService] Failed to spawn decoration at {position}: {ex.Message}");
                return Entity.Null;
            }
        }

        private Entity SpawnPrefab(PrefabGUID prefabGuid, float3 position, quaternion? rotation = null)
        {
            try
            {
                if (prefabGuid.GuidHash == 0)
                {
                    _log?.LogWarning($"[ArenaBuildingService] Cannot spawn prefab with zero GUID at {position}");
                    return Entity.Null;
                }

                    var world = VRCore.ServerWorld;
                if (world == null)
                {
                    return Entity.Null;
                }

                var prefabCollectionSystem = world.GetExistingSystemManaged<ProjectM.PrefabCollectionSystem>();
                if (prefabCollectionSystem == null)
                {
                    return Entity.Null;
                }

                if (!prefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(prefabGuid, out var prefabEntity))
                {
                    return Entity.Null;
                }

                if (!VAuto.Core.Core.EntityManager.Exists(prefabEntity))
                {
                    return Entity.Null;
                }

                var entity = VAuto.Core.Core.EntityManager.Instantiate(prefabEntity);
                if (entity != Entity.Null)
                {
                    VAuto.Core.Core.EntityManager.SetComponentData(entity, new Translation { Value = position });
                    if (rotation.HasValue)
                    {
                        VAuto.Core.Core.EntityManager.SetComponentData(entity, new Rotation { Value = rotation.Value });
                    }
                }

                return entity;
            }
            catch (Exception ex)
            {
                _log?.LogError($"[ArenaBuildingService] Failed to spawn prefab {prefabGuid.GuidHash}: {ex.Message}\n{ex.StackTrace}");
                return Entity.Null;
            }
        }
        #endregion

        #region Position Computation
        private static List<float3> ComputeBoundaryPositions(ArenaZone zone, float spacing)
        {
            var positions = new List<float3>();

            if (zone.Shape == ArenaZoneShape.Circle)
            {
                var circumference = 2f * math.PI * zone.Radius;
                var count = math.max(8, (int)(circumference / spacing));

                for (var i = 0; i < count; i++)
                {
                    var angle = (i / (float)count) * math.PI * 2f;
                    var pos = new float3(
                        zone.Center.x + math.cos(angle) * zone.Radius,
                        zone.Center.y,
                        zone.Center.z + math.sin(angle) * zone.Radius
                    );
                    positions.Add(pos);
                }
            }
            else if (zone.Shape == ArenaZoneShape.Box)
            {
                var halfWidth = zone.Dimensions.x / 2f;
                var halfLength = zone.Dimensions.y / 2f;

                // Perimeter positions
                for (float x = -halfWidth; x <= halfWidth; x += spacing)
                {
                    positions.Add(zone.Center + new float3(x, 0, halfLength));
                    positions.Add(zone.Center + new float3(x, 0, -halfLength));
                }

                for (float z = -halfLength + spacing; z < halfLength; z += spacing)
                {
                    positions.Add(zone.Center + new float3(halfWidth, 0, z));
                    positions.Add(zone.Center + new float3(-halfWidth, 0, z));
                }
            }

            return positions;
        }

        private static List<float3> ComputeCornerPositions(ArenaZone zone)
        {
            var positions = new List<float3>();

            if (zone.Shape == ArenaZoneShape.Box)
            {
                var halfWidth = zone.Dimensions.x / 2f;
                var halfLength = zone.Dimensions.y / 2f;

                positions.Add(zone.Center + new float3(halfWidth * 0.7f, 0, halfLength * 0.7f));
                positions.Add(zone.Center + new float3(-halfWidth * 0.7f, 0, halfLength * 0.7f));
                positions.Add(zone.Center + new float3(halfWidth * 0.7f, 0, -halfLength * 0.7f));
                positions.Add(zone.Center + new float3(-halfWidth * 0.7f, 0, -halfLength * 0.7f));
            }
            else if (zone.Shape == ArenaZoneShape.Circle)
            {
                // Cardinal directions for circular arenas
                positions.Add(zone.Center + new float3(zone.Radius * 0.7f, 0, 0));
                positions.Add(zone.Center + new float3(-zone.Radius * 0.7f, 0, 0));
                positions.Add(zone.Center + new float3(0, 0, zone.Radius * 0.7f));
                positions.Add(zone.Center + new float3(0, 0, -zone.Radius * 0.7f));
            }

            return positions;
        }

        private static List<float3> ComputeDecorationPositions(ArenaZone zone)
        {
            var positions = new List<float3>();

            // Add some decorative positions around the arena
            var boundaryPositions = ComputeBoundaryPositions(zone, 20f); // Wider spacing for decorations
            
            // Take every 3rd boundary position for decoration
            for (var i = 0; i < boundaryPositions.Count; i += 3)
            {
                var pos = boundaryPositions[i];
                // Offset decorations slightly inward
                var dir = zone.Center - pos;
                var offset = (math.lengthsq(dir) > 0.001f ? math.normalize(dir) : float3.zero) * 2f;
                positions.Add(pos + offset);
            }

            return positions;
        }
        #endregion
    }
}
