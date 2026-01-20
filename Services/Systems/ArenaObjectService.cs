using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM;
using ProjectM.Network;
using ProjectM.Gameplay;
using VAuto.Core;
using BepInEx.Logging;
using VAuto.Services.Interfaces;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Arena Object Service - Manages objects within arena zones
    /// </summary>
    public class ArenaObjectService : IService
    {
        private static ArenaObjectService _instance;
        public static ArenaObjectService Instance => _instance ??= new ArenaObjectService();

        private bool _initialized = false;
        private readonly Dictionary<Entity, ArenaObjectData> _arenaObjects = new();
        private readonly Dictionary<string, List<Entity>> _objectsByArena = new();
        private readonly Dictionary<string, List<Entity>> _objectsByType = new();
        private readonly object _lock = new object();
        
        public bool IsInitialized => _initialized;
        public ManualLogSource Log => Plugin.Logger;

        #region Initialization
        public void Initialize()
        {
            lock (_lock)
            {
                if (_initialized) return;
                
                try
                {
                    Log?.LogInfo("[ArenaObjectService] Initializing arena object service...");
                    
                    _arenaObjects.Clear();
                    _objectsByArena.Clear();
                    _objectsByType.Clear();
                    _initialized = true;
                    
                    Log?.LogInfo("[ArenaObjectService] Arena object service initialized successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[ArenaObjectService] Failed to initialize: {ex.Message}");
                    throw;
                }
            }
        }

        public void Cleanup()
        {
            lock (_lock)
            {
                if (!_initialized) return;
                
                try
                {
                    Log?.LogInfo("[ArenaObjectService] Cleaning up arena object service...");
                    
                    // Remove all objects from their arenas
                    RemoveAllObjects();
                    
                    _arenaObjects.Clear();
                    _objectsByArena.Clear();
                    _objectsByType.Clear();
                    _initialized = false;
                    
                    Log?.LogInfo("[ArenaObjectService] Arena object service cleaned up successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[ArenaObjectService] Failed to cleanup: {ex.Message}");
                }
            }
        }
        #endregion

        #region Object Management
        public bool AddObjectToArena(Entity entity, string arenaId, ArenaObjectType type, string name)
        {
            try
            {
                if (entity == Entity.Null || string.IsNullOrEmpty(arenaId) || string.IsNullOrEmpty(name))
                {
                    Log?.LogWarning("[ArenaObjectService] Cannot add object with invalid parameters");
                    return false;
                }

                var em = VAuto.Core.Core.EntityManager;
                if (!em.Exists(entity))
                {
                    Log?.LogWarning("[ArenaObjectService] Entity does not exist");
                    return false;
                }

                lock (_lock)
                {
                    // Check if object is already in an arena
                    if (_arenaObjects.ContainsKey(entity))
                    {
                        Log?.LogWarning($"[ArenaObjectService] Object is already in an arena");
                        return false;
                    }

                    var objectData = new ArenaObjectData
                    {
                        Entity = entity,
                        ArenaId = arenaId,
                        Type = type,
                        Name = name,
                        AddedAt = DateTime.UtcNow,
                        Position = GetEntityPosition(entity),
                        Rotation = GetEntityRotation(entity),
                        IsActive = true,
                        Properties = new Dictionary<string, object>()
                    };

                    _arenaObjects[entity] = objectData;

                    // Add to arena-specific list
                    if (!_objectsByArena.ContainsKey(arenaId))
                    {
                        _objectsByArena[arenaId] = new List<Entity>();
                    }
                    _objectsByArena[arenaId].Add(entity);

                    // Add to type-specific list
                    var typeName = type.ToString();
                    if (!_objectsByType.ContainsKey(typeName))
                    {
                        _objectsByType[typeName] = new List<Entity>();
                    }
                    _objectsByType[typeName].Add(entity);

                    Log?.LogInfo($"[ArenaObjectService] Added {type} '{name}' to arena '{arenaId}'");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaObjectService] Failed to add object to arena: {ex.Message}");
                return false;
            }
        }

        public bool RemoveObjectFromArena(Entity entity)
        {
            try
            {
                if (entity == Entity.Null)
                    return false;

                lock (_lock)
                {
                    if (!_arenaObjects.TryGetValue(entity, out var objectData))
                        return false;

                    // Remove from arena-specific list
                    if (_objectsByArena.ContainsKey(objectData.ArenaId))
                    {
                        _objectsByArena[objectData.ArenaId].Remove(entity);
                        if (_objectsByArena[objectData.ArenaId].Count == 0)
                        {
                            _objectsByArena.Remove(objectData.ArenaId);
                        }
                    }

                    // Remove from type-specific list
                    var typeName = objectData.Type.ToString();
                    if (_objectsByType.ContainsKey(typeName))
                    {
                        _objectsByType[typeName].Remove(entity);
                        if (_objectsByType[typeName].Count == 0)
                        {
                            _objectsByType.Remove(typeName);
                        }
                    }

                    _arenaObjects.Remove(entity);

                    Log?.LogInfo($"[ArenaObjectService] Removed {objectData.Type} '{objectData.Name}' from arena '{objectData.ArenaId}'");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaObjectService] Failed to remove object from arena: {ex.Message}");
                return false;
            }
        }

        public bool MoveObject(Entity entity, float3 newPosition, quaternion? newRotation = null)
        {
            try
            {
                if (entity == Entity.Null)
                    return false;

                lock (_lock)
                {
                    if (!_arenaObjects.TryGetValue(entity, out var objectData))
                        return false;

                    // Update entity position
                    var em = VAuto.Core.Core.EntityManager;
                    if (em.HasComponent<Translation>(entity))
                    {
                        em.SetComponentData(entity, new Translation { Value = newPosition });
                    }
                    else
                    {
                        em.AddComponentData(entity, new Translation { Value = newPosition });
                    }

                    // Update rotation if provided
                    if (newRotation.HasValue && em.HasComponent<Rotation>(entity))
                    {
                        em.SetComponentData(entity, new Rotation { Value = newRotation.Value });
                    }

                    // Update cached data
                    objectData.Position = newPosition;
                    if (newRotation.HasValue)
                    {
                        objectData.Rotation = newRotation.Value;
                    }
                    objectData.LastMoved = DateTime.UtcNow;

                    Log?.LogDebug($"[ArenaObjectService] Moved {objectData.Name} to {newPosition}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaObjectService] Failed to move object: {ex.Message}");
                return false;
            }
        }

        public bool SetObjectActive(Entity entity, bool isActive)
        {
            try
            {
                if (entity == Entity.Null)
                    return false;

                lock (_lock)
                {
                    if (!_arenaObjects.TryGetValue(entity, out var objectData))
                        return false;

                    objectData.IsActive = isActive;
                    objectData.LastModified = DateTime.UtcNow;

                    // TODO: Update entity active state if needed
                    // This would involve setting entity enabled/disabled state

                    Log?.LogDebug($"[ArenaObjectService] Set {objectData.Name} active state to {isActive}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaObjectService] Failed to set object active state: {ex.Message}");
                return false;
            }
        }

        public bool SetObjectProperty(Entity entity, string propertyName, object value)
        {
            try
            {
                if (entity == Entity.Null || string.IsNullOrEmpty(propertyName))
                    return false;

                lock (_lock)
                {
                    if (!_arenaObjects.TryGetValue(entity, out var objectData))
                        return false;

                    objectData.Properties[propertyName] = value;
                    objectData.LastModified = DateTime.UtcNow;

                    Log?.LogDebug($"[ArenaObjectService] Set property '{propertyName}' for {objectData.Name}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaObjectService] Failed to set object property: {ex.Message}");
                return false;
            }
        }

        public object GetObjectProperty(Entity entity, string propertyName)
        {
            try
            {
                if (entity == Entity.Null || string.IsNullOrEmpty(propertyName))
                    return null;

                lock (_lock)
                {
                    if (!_arenaObjects.TryGetValue(entity, out var objectData))
                        return null;

                    return objectData.Properties.TryGetValue(propertyName, out var value) ? value : null;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaObjectService] Failed to get object property: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region Bulk Operations
        public bool MoveAllObjectsFromArena(string arenaId, float3 offset)
        {
            try
            {
                if (string.IsNullOrEmpty(arenaId))
                    return false;

                lock (_lock)
                {
                    if (!_objectsByArena.TryGetValue(arenaId, out var arenaObjects))
                        return false;

                    var movedCount = 0;
                    foreach (var entity in arenaObjects.ToList())
                    {
                        if (_arenaObjects.TryGetValue(entity, out var objectData))
                        {
                            var newPosition = objectData.Position + offset;
                            if (MoveObject(entity, newPosition))
                            {
                                movedCount++;
                            }
                        }
                    }

                    Log?.LogInfo($"[ArenaObjectService] Moved {movedCount} objects from arena '{arenaId}' by offset {offset}");
                    return movedCount > 0;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaObjectService] Failed to move objects from arena: {ex.Message}");
                return false;
            }
        }

        public bool RemoveAllObjectsFromArena(string arenaId)
        {
            try
            {
                if (string.IsNullOrEmpty(arenaId))
                    return false;

                lock (_lock)
                {
                    if (!_objectsByArena.TryGetValue(arenaId, out var arenaObjects))
                        return false;

                    var removedCount = 0;
                    foreach (var entity in arenaObjects.ToList())
                    {
                        if (RemoveObjectFromArena(entity))
                        {
                            removedCount++;
                        }
                    }

                    Log?.LogInfo($"[ArenaObjectService] Removed {removedCount} objects from arena '{arenaId}'");
                    return removedCount > 0;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaObjectService] Failed to remove objects from arena: {ex.Message}");
                return false;
            }
        }

        public void RemoveAllObjects()
        {
            lock (_lock)
            {
                var entitiesToRemove = _arenaObjects.Keys.ToList();
                var removedCount = 0;

                foreach (var entity in entitiesToRemove)
                {
                    if (RemoveObjectFromArena(entity))
                    {
                        removedCount++;
                    }
                }

                Log?.LogInfo($"[ArenaObjectService] Removed all {removedCount} arena objects");
            }
        }
        #endregion

        #region Query Methods
        public List<ArenaObjectData> GetAllArenaObjects()
        {
            lock (_lock)
            {
                return _arenaObjects.Values.ToList();
            }
        }

        public List<ArenaObjectData> GetArenaObjects(string arenaId)
        {
            lock (_lock)
            {
                if (_objectsByArena.TryGetValue(arenaId, out var arenaObjects))
                {
                    return arenaObjects.Select(entity => _arenaObjects[entity]).ToList();
                }
                return new List<ArenaObjectData>();
            }
        }

        public List<ArenaObjectData> GetObjectsByType(ArenaObjectType type)
        {
            lock (_lock)
            {
                var typeName = type.ToString();
                if (_objectsByType.TryGetValue(typeName, out var typeObjects))
                {
                    return typeObjects.Select(entity => _arenaObjects[entity]).ToList();
                }
                return new List<ArenaObjectData>();
            }
        }

        public ArenaObjectData GetObjectData(Entity entity)
        {
            lock (_lock)
            {
                return _arenaObjects.TryGetValue(entity, out var objectData) ? objectData : null;
            }
        }

        public List<ArenaObjectData> GetObjectsInRange(float3 center, float radius)
        {
            lock (_lock)
            {
                return _arenaObjects.Values.Where(obj => 
                    math.distance(obj.Position, center) <= radius).ToList();
            }
        }

        public List<ArenaObjectData> GetActiveObjects()
        {
            lock (_lock)
            {
                return _arenaObjects.Values.Where(obj => obj.IsActive).ToList();
            }
        }

        public List<string> GetAllArenaIds()
        {
            lock (_lock)
            {
                return _objectsByArena.Keys.ToList();
            }
        }

        public List<string> GetAllObjectTypes()
        {
            lock (_lock)
            {
                return _objectsByType.Keys.ToList();
            }
        }

        public int GetTotalObjectCount()
        {
            lock (_lock)
            {
                return _arenaObjects.Count;
            }
        }

        public int GetObjectCountByArena(string arenaId)
        {
            lock (_lock)
            {
                return _objectsByArena.TryGetValue(arenaId, out var objects) ? objects.Count : 0;
            }
        }

        public int GetObjectCountByType(ArenaObjectType type)
        {
            lock (_lock)
            {
                var typeName = type.ToString();
                return _objectsByType.TryGetValue(typeName, out var objects) ? objects.Count : 0;
            }
        }

        private float3 GetEntityPosition(Entity entity)
        {
            try
            {
                var em = VAuto.Core.Core.EntityManager;
                if (em.HasComponent<Translation>(entity))
                {
                    return em.GetComponentData<Translation>(entity).Value;
                }
                return float3.zero;
            }
            catch
            {
                return float3.zero;
            }
        }

        private quaternion GetEntityRotation(Entity entity)
        {
            try
            {
                var em = VAuto.Core.Core.EntityManager;
                if (em.HasComponent<Rotation>(entity))
                {
                    return em.GetComponentData<Rotation>(entity).Value;
                }
                return quaternion.identity;
            }
            catch
            {
                return quaternion.identity;
            }
        }
        #endregion

        #region Data Structures
        public enum ArenaObjectType
        {
            Structure,
            Decoration,
            Light,
            Portal,
            Trap,
            Resource,
            NPC,
            Trigger,
            Effect,
            Utility
        }

        public class ArenaObjectData
        {
            public Entity Entity { get; set; }
            public string ArenaId { get; set; }
            public ArenaObjectType Type { get; set; }
            public string Name { get; set; }
            public float3 Position { get; set; }
            public quaternion Rotation { get; set; }
            public bool IsActive { get; set; }
            public DateTime AddedAt { get; set; }
            public DateTime LastModified { get; set; }
            public DateTime? LastMoved { get; set; }
            public Dictionary<string, object> Properties { get; set; } = new();
        }
        #endregion
    }
}