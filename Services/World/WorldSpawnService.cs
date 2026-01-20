using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM;
using Stunlock.Core;
using VAuto.Core;
using VAuto.Data;
using VAuto.Services.Systems;
using BepInEx.Logging;

namespace VAuto.Services.World
{
    /// <summary>
    /// World Spawn Service - spawns tiles, structures, doors.
    /// Pure world spawning - no player ownership or lifecycle.
    /// </summary>
    public static class WorldSpawnService
    {
        private static readonly Dictionary<string, PrefabGUID> _prefabCache = new();
        private static readonly object _lock = new object();
        private static ManualLogSource _log;

        static WorldSpawnService()
        {
            _log = Plugin.Logger;
            InitializePrefabCache();
        }

        #region Prefab Management
        private static void InitializePrefabCache()
        {
            lock (_lock)
            {
                // Common tiles
                _prefabCache["floor_basic"] = Prefabs.Item_Weapon_Sword_Iron; // Placeholder
                _prefabCache["floor_stone"] = Prefabs.Item_Weapon_Axe_Iron; // Placeholder
                _prefabCache["floor_ritual"] = Prefabs.Item_Weapon_Mace_Iron; // Placeholder

                // Common structures
                _prefabCache["wall_basic"] = Prefabs.Item_Shield_Basic_Wood;
                _prefabCache["pillar_basic"] = Prefabs.Item_Weapon_Spear_Iron; // Placeholder
                _prefabCache["decoration_torch"] = Prefabs.Item_Consumable_HealingPotion_T01; // Placeholder

                // Doors (placeholders)
                _prefabCache["door_stone"] = Prefabs.Item_Weapon_Crossbow_Bone;
                _prefabCache["door_wood"] = Prefabs.Item_Weapon_Bow_Bone;
                _prefabCache["door_iron"] = Prefabs.Item_Weapon_Dagger_Iron;

                // Triggers
                _prefabCache["trigger_plate"] = Prefabs.Item_Weapon_Staff_Wood;
                _prefabCache["trigger_proximity"] = Prefabs.Item_Weapon_Polearm_Iron;
                _prefabCache["trigger_timer"] = Prefabs.Item_Weapon_Greatsword_Iron;

                // Castle objects (placeholders)
                _prefabCache["castle_workbench"] = default; // TODO: Define TM_Castle_WorkBench_Standard prefab
                _prefabCache["castle_forge"] = default; // TODO: Define TM_Castle_Forge_Standard prefab
                _prefabCache["castle_alchemy"] = default; // TODO: Define TM_Castle_AlchemyTable_Standard prefab
                _prefabCache["castle_throne"] = Prefabs.Item_Weapon_Greatsword_Iron;
                _prefabCache["castle_storage"] = Prefabs.Item_Weapon_Crossbow_Bone;
                _prefabCache["castle_decoration"] = Prefabs.Item_Consumable_HealingPotion_T01;
                _prefabCache["castle_trap"] = Prefabs.Item_Weapon_Dagger_Iron;
                _prefabCache["castle_utility"] = Prefabs.Item_Weapon_Mace_Iron;
                _prefabCache["castle_defense"] = Prefabs.Item_Shield_Basic_Wood;

                _log?.LogInfo($"[WorldSpawnService] Initialized {_prefabCache.Count} prefabs");
            }
        }

        /// <summary>
        /// Add custom prefab to cache.
        /// </summary>
        public static void AddPrefab(string name, PrefabGUID guid)
        {
            lock (_lock)
            {
                _prefabCache[name.ToLower()] = guid;
                _log?.LogInfo($"[WorldSpawnService] Added prefab: {name}");
            }
        }

        /// <summary>
        /// Get prefab GUID by name.
        /// </summary>
        public static PrefabGUID GetPrefab(string name)
        {
            lock (_lock)
            {
                return _prefabCache.TryGetValue(name.ToLower(), out var guid) ? guid : default;
            }
        }

        /// <summary>
        /// List all available prefabs.
        /// </summary>
        public static List<string> ListPrefabs()
        {
            lock (_lock)
            {
                return _prefabCache.Keys.ToList();
            }
        }
        #endregion

        #region Spawn Methods
        /// <summary>
        /// Spawn a tile at position.
        /// </summary>
        public static Entity SpawnTile(string prefabName, float3 position, quaternion? rotation = null)
        {
            var prefab = GetPrefab(prefabName);
            if (prefab.GuidHash == 0)
            {
                _log?.LogWarning($"[WorldSpawnService] Unknown tile prefab: {prefabName}");
                return Entity.Null;
            }

            var entity = SpawnPrefab(prefab, position, rotation);
            if (entity != Entity.Null)
            {
                var record = new WorldObjectRecord
                {
                    Entity = entity,
                    Type = WorldObjectType.Tile,
                    PrefabName = prefabName,
                    Position = position,
                    Rotation = rotation ?? quaternion.identity
                };
                WorldObjectService.Register(record);
            }

            return entity;
        }

        /// <summary>
        /// Spawn a structure at position.
        /// </summary>
        public static Entity SpawnStructure(string prefabName, float3 position, quaternion? rotation = null)
        {
            var prefab = GetPrefab(prefabName);
            if (prefab.GuidHash == 0)
            {
                _log?.LogWarning($"[WorldSpawnService] Unknown structure prefab: {prefabName}");
                return Entity.Null;
            }

            var entity = SpawnPrefab(prefab, position, rotation);
            if (entity != Entity.Null)
            {
                var record = new WorldObjectRecord
                {
                    Entity = entity,
                    Type = WorldObjectType.Structure,
                    PrefabName = prefabName,
                    Position = position,
                    Rotation = rotation ?? quaternion.identity
                };
                WorldObjectService.Register(record);
            }

            return entity;
        }

        /// <summary>
        /// Spawn a door at position.
        /// </summary>
        public static Entity SpawnDoor(string prefabName, float3 position, quaternion? rotation = null)
        {
            var prefab = GetPrefab(prefabName);
            if (prefab.GuidHash == 0)
            {
                _log?.LogWarning($"[WorldSpawnService] Unknown door prefab: {prefabName}");
                return Entity.Null;
            }

            var entity = SpawnPrefab(prefab, position, rotation);
            if (entity != Entity.Null)
            {
                var record = new WorldObjectRecord
                {
                    Entity = entity,
                    Type = WorldObjectType.Door,
                    PrefabName = prefabName,
                    Position = position,
                    Rotation = rotation ?? quaternion.identity,
                    Properties = new Dictionary<string, object>
                    {
                        ["state"] = "closed",
                        ["locked"] = false
                    }
                };
                WorldObjectService.Register(record);
            }

            return entity;
        }

        /// <summary>
        /// Spawn a trigger at position.
        /// </summary>
        public static Entity SpawnTrigger(string prefabName, float3 position, quaternion? rotation = null)
        {
            var prefab = GetPrefab(prefabName);
            if (prefab.GuidHash == 0)
            {
                _log?.LogWarning($"[WorldSpawnService] Unknown trigger prefab: {prefabName}");
                return Entity.Null;
            }

            var entity = SpawnPrefab(prefab, position, rotation);
            if (entity != Entity.Null)
            {
                var record = new WorldObjectRecord
                {
                    Entity = entity,
                    Type = WorldObjectType.Trigger,
                    PrefabName = prefabName,
                    Position = position,
                    Rotation = rotation ?? quaternion.identity,
                    Properties = new Dictionary<string, object>
                    {
                        ["trigger_radius"] = 5.0f,
                        ["enabled"] = true
                    }
                };
                WorldObjectService.Register(record);
            }

            return entity;
        }

        /// <summary>
        /// Spawn multiple objects in a grid pattern.
        /// </summary>
        public static List<Entity> SpawnGrid(string prefabName, int rows, int cols, float3 center, float spacing, WorldObjectType type = WorldObjectType.Structure)
        {
            var entities = new List<Entity>();
            var halfRows = (rows - 1) / 2f;
            var halfCols = (cols - 1) / 2f;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    var position = center + new float3(
                        (col - halfCols) * spacing,
                        0,
                        (row - halfRows) * spacing
                    );

                    Entity entity;
                    switch (type)
                    {
                        case WorldObjectType.Tile:
                            entity = SpawnTile(prefabName, position);
                            break;
                        case WorldObjectType.Door:
                            entity = SpawnDoor(prefabName, position);
                            break;
                        case WorldObjectType.Trigger:
                            entity = SpawnTrigger(prefabName, position);
                            break;
                        default:
                            entity = SpawnStructure(prefabName, position);
                            break;
                    }

                    if (entity != Entity.Null)
                    {
                        entities.Add(entity);
                    }
                }
            }

            _log?.LogInfo($"[WorldSpawnService] Spawned {entities.Count} {type} objects in {rows}x{cols} grid");
            return entities;
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Remove nearest object of type.
        /// </summary>
        public static bool RemoveNearest(float3 position, WorldObjectType type)
        {
            var nearest = WorldObjectService.FindNearest(position, type);
            if (nearest != null)
            {
                WorldObjectService.Remove(nearest.Entity);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove objects in radius.
        /// </summary>
        public static int RemoveInRadius(float3 center, float radius, WorldObjectType? type = null)
        {
            var objects = type.HasValue 
                ? WorldObjectService.GetInRadius(center, radius).Where(o => o.Type == type.Value)
                : WorldObjectService.GetInRadius(center, radius);

            var count = objects.Count();
            foreach (var obj in objects)
            {
                WorldObjectService.Remove(obj.Entity);
            }

            return count;
        }
        #endregion

        #region Private Spawning
        private static Entity SpawnPrefab(PrefabGUID prefabGuid, float3 position, quaternion? rotation = null)
        {
            try
            {
                var prefabCollectionSystem = VRCore.ServerWorld?.GetExistingSystemManaged<ProjectM.PrefabCollectionSystem>();
                if (prefabCollectionSystem == null || !prefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(prefabGuid, out var prefabEntity))
                {
                    _log?.LogError($"[WorldSpawnService] Failed to find prefab entity for GUID {prefabGuid.GuidHash}");
                    return Entity.Null;
                }

                var entity = VRCore.EntityManager.Instantiate(prefabEntity);
                if (entity != Entity.Null)
                {
                    VRCore.EntityManager.SetComponentData(entity, new Translation { Value = position });
                    if (rotation.HasValue)
                    {
                        VRCore.EntityManager.SetComponentData(entity, new Rotation { Value = rotation.Value });
                    }
                }

                return entity;
            }
            catch (Exception ex)
            {
                _log?.LogError($"[WorldSpawnService] Failed to spawn prefab {prefabGuid.GuidHash}: {ex.Message}");
                return Entity.Null;
            }
        }
        #endregion
    }
}
