using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM;
using ProjectM.Network;
using ProjectM.Gameplay;
using ProjectM.CastleBuilding;
using VAuto.Core;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Castle Object Integration Service - Manages integration with castle building objects
    /// </summary>
    public static class CastleObjectIntegrationService
    {
        private static bool _initialized = false;
        private static readonly Dictionary<Entity, CastleObjectData> _castleObjects = new();
        private static readonly Dictionary<string, List<Entity>> _objectsByType = new();
        private static readonly object _lock = new object();
        
        public static bool IsInitialized => _initialized;
        public static ManualLogSource Log => Plugin.Logger;

        #region Initialization
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized) return;
                
                try
                {
                    Log?.LogInfo("[CastleObjectIntegrationService] Initializing castle object integration service...");
                    
                    _castleObjects.Clear();
                    _objectsByType.Clear();
                    _initialized = true;
                    
                    Log?.LogInfo("[CastleObjectIntegrationService] Castle object integration service initialized successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[CastleObjectIntegrationService] Failed to initialize: {ex.Message}");
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
                    Log?.LogInfo("[CastleObjectIntegrationService] Cleaning up castle object integration service...");
                    
                    _castleObjects.Clear();
                    _objectsByType.Clear();
                    _initialized = false;
                    
                    Log?.LogInfo("[CastleObjectIntegrationService] Castle object integration service cleaned up successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[CastleObjectIntegrationService] Failed to cleanup: {ex.Message}");
                }
            }
        }
        #endregion

        #region Object Registration
        public static bool RegisterCastleObject(Entity entity, CastleObjectType type, string name)
        {
            lock (_lock)
            {
                try
                {
                    if (entity == Entity.Null)
                    {
                        Log?.LogWarning("[CastleObjectIntegrationService] Cannot register null entity");
                        return false;
                    }

                    var em = VAutoCore.EntityManager;
                    if (!em.Exists(entity))
                    {
                        Log?.LogWarning("[CastleObjectIntegrationService] Entity does not exist");
                        return false;
                    }

                    // Check if already registered
                    if (_castleObjects.ContainsKey(entity))
                    {
                        Log?.LogDebug($"[CastleObjectIntegrationService] Entity already registered");
                        return false;
                    }

                    var castleObjectData = new CastleObjectData
                    {
                        Entity = entity,
                        Type = type,
                        Name = name,
                        RegisteredAt = DateTime.UtcNow,
                        IsActive = true,
                        Position = GetEntityPosition(entity),
                        Rotation = GetEntityRotation(entity)
                    };

                    _castleObjects[entity] = castleObjectData;

                    // Add to type-specific list
                    var typeName = type.ToString();
                    if (!_objectsByType.ContainsKey(typeName))
                    {
                        _objectsByType[typeName] = new List<Entity>();
                    }
                    _objectsByType[typeName].Add(entity);

                    Log?.LogInfo($"[CastleObjectIntegrationService] Registered {type} '{name}' at {castleObjectData.Position}");
                    return true;
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[CastleObjectIntegrationService] Failed to register castle object: {ex.Message}");
                    return false;
                }
            }
        }

        public static bool UnregisterCastleObject(Entity entity)
        {
            lock (_lock)
            {
                try
                {
                    if (!_castleObjects.TryGetValue(entity, out var castleObjectData))
                        return false;

                    // Remove from type-specific list
                    var typeName = castleObjectData.Type.ToString();
                    if (_objectsByType.ContainsKey(typeName))
                    {
                        _objectsByType[typeName].Remove(entity);
                        if (_objectsByType[typeName].Count == 0)
                        {
                            _objectsByType.Remove(typeName);
                        }
                    }

                    _castleObjects.Remove(entity);
                    
                    Log?.LogInfo($"[CastleObjectIntegrationService] Unregistered {castleObjectData.Type} '{castleObjectData.Name}'");
                    return true;
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[CastleObjectIntegrationService] Failed to unregister castle object: {ex.Message}");
                    return false;
                }
            }
        }
        #endregion

        #region Object Management
        public static bool UpdateCastleObjectPosition(Entity entity, float3 newPosition)
        {
            lock (_lock)
            {
                try
                {
                    if (!_castleObjects.TryGetValue(entity, out var castleObjectData))
                        return false;

                    // Update entity position
                    var em = VAutoCore.EntityManager;
                    if (em.HasComponent<Translation>(entity))
                    {
                        em.SetComponentData(entity, new Translation { Value = newPosition });
                    }
                    else
                    {
                        em.AddComponentData(entity, new Translation { Value = newPosition });
                    }

                    // Update cached data
                    castleObjectData.Position = newPosition;
                    castleObjectData.LastUpdated = DateTime.UtcNow;

                    Log?.LogDebug($"[CastleObjectIntegrationService] Updated position of {castleObjectData.Name} to {newPosition}");
                    return true;
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[CastleObjectIntegrationService] Failed to update position: {ex.Message}");
                    return false;
                }
            }
        }

        public static bool SetCastleObjectActive(Entity entity, bool isActive)
        {
            lock (_lock)
            {
                try
                {
                    if (!_castleObjects.TryGetValue(entity, out var castleObjectData))
                        return false;

                    castleObjectData.IsActive = isActive;
                    castleObjectData.LastUpdated = DateTime.UtcNow;

                    // Update entity active state if needed
                    var em = VAutoCore.EntityManager;
                    if (em.HasComponent<Prefab>(entity))
                    {
                        // Handle active state changes based on object type
                        HandleActiveStateChange(entity, castleObjectData.Type, isActive);
                    }

                    Log?.LogDebug($"[CastleObjectIntegrationService] Set {castleObjectData.Name} active state to {isActive}");
                    return true;
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[CastleObjectIntegrationService] Failed to set active state: {ex.Message}");
                    return false;
                }
            }
        }

        private static void HandleActiveStateChange(Entity entity, CastleObjectType type, bool isActive)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                
                switch (type)
                {
                    case CastleObjectType.Workbench:
                        if (isActive)
                            EnableWorkbench(entity, em);
                        else
                            DisableWorkbench(entity, em);
                        break;
                        
                    case CastleObjectType.Forge:
                        if (isActive)
                            EnableForge(entity, em);
                        else
                            DisableForge(entity, em);
                        break;
                        
                    case CastleObjectType.AlchemyTable:
                        if (isActive)
                            EnableAlchemyTable(entity, em);
                        else
                            DisableAlchemyTable(entity, em);
                        break;
                        
                    case CastleObjectType.Throne:
                        if (isActive)
                            EnableThrone(entity, em);
                        else
                            DisableThrone(entity, em);
                        break;
                        
                    // Add more cases for other castle object types
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[CastleObjectIntegrationService] Failed to handle active state change: {ex.Message}");
            }
        }

        private static void EnableWorkbench(Entity entity, EntityManager em)
        {
            // Enable workbench functionality
        }

        private static void DisableWorkbench(Entity entity, EntityManager em)
        {
            // Disable workbench functionality
        }

        private static void EnableForge(Entity entity, EntityManager em)
        {
            // Enable forge functionality
        }

        private static void DisableForge(Entity entity, EntityManager em)
        {
            // Disable forge functionality
        }

        private static void EnableAlchemyTable(Entity entity, EntityManager em)
        {
            // Enable alchemy table functionality
        }

        private static void DisableAlchemyTable(Entity entity, EntityManager em)
        {
            // Disable alchemy table functionality
        }

        private static void EnableThrone(Entity entity, EntityManager em)
        {
            // Enable throne functionality
        }

        private static void DisableThrone(Entity entity, EntityManager em)
        {
            // Disable throne functionality
        }
        #endregion

        #region Integration with Arena Systems
        public static bool TransferObjectToArena(Entity entity, float3 arenaPosition)
        {
            lock (_lock)
            {
                try
                {
                    if (!_castleObjects.TryGetValue(entity, out var castleObjectData))
                        return false;

                    // Update position to arena
                    UpdateCastleObjectPosition(entity, arenaPosition);
                    
                    // Mark as arena object
                    castleObjectData.IsInArena = true;
                    castleObjectData.ArenaTransferAt = DateTime.UtcNow;

                    Log?.LogInfo($"[CastleObjectIntegrationService] Transferred {castleObjectData.Name} to arena at {arenaPosition}");
                    return true;
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[CastleObjectIntegrationService] Failed to transfer object to arena: {ex.Message}");
                    return false;
                }
            }
        }

        public static bool ReturnObjectToCastle(Entity entity)
        {
            lock (_lock)
            {
                try
                {
                    if (!_castleObjects.TryGetValue(entity, out var castleObjectData))
                        return false;

                    // Mark as no longer in arena
                    castleObjectData.IsInArena = false;
                    castleObjectData.CastleReturnAt = DateTime.UtcNow;

                    Log?.LogInfo($"[CastleObjectIntegrationService] Returned {castleObjectData.Name} to castle");
                    return true;
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[CastleObjectIntegrationService] Failed to return object to castle: {ex.Message}");
                    return false;
                }
            }
        }

        public static List<Entity> GetArenaCastleObjects()
        {
            lock (_lock)
            {
                return _castleObjects.Values.Where(c => c.IsInArena).Select(c => c.Entity).ToList();
            }
        }
        #endregion

        #region Query Methods
        public static List<CastleObjectData> GetAllCastleObjects()
        {
            lock (_lock)
            {
                return _castleObjects.Values.ToList();
            }
        }

        public static List<CastleObjectData> GetCastleObjectsByType(CastleObjectType type)
        {
            lock (_lock)
            {
                return _castleObjects.Values.Where(c => c.Type == type).ToList();
            }
        }

        public static CastleObjectData GetCastleObject(Entity entity)
        {
            lock (_lock)
            {
                return _castleObjects.TryGetValue(entity, out var castleObject) ? castleObject : null;
            }
        }

        public static List<CastleObjectData> GetActiveCastleObjects()
        {
            lock (_lock)
            {
                return _castleObjects.Values.Where(c => c.IsActive).ToList();
            }
        }

        public static List<CastleObjectData> GetCastleObjectsInRange(float3 center, float radius)
        {
            lock (_lock)
            {
                return _castleObjects.Values.Where(c => 
                    math.distance(c.Position, center) <= radius).ToList();
            }
        }

        public static int GetTotalCastleObjects()
        {
            lock (_lock)
            {
                return _castleObjects.Count;
            }
        }

        private static float3 GetEntityPosition(Entity entity)
        {
            try
            {
                var em = VAutoCore.EntityManager;
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

        private static quaternion GetEntityRotation(Entity entity)
        {
            try
            {
                var em = VAutoCore.EntityManager;
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
        public enum CastleObjectType
        {
            Workbench,
            Forge,
            AlchemyTable,
            Throne,
            Storage,
            Decoration,
            Trap,
            Utility,
            Defense
        }

        public class CastleObjectData
        {
            public Entity Entity { get; set; }
            public CastleObjectType Type { get; set; }
            public string Name { get; set; }
            public float3 Position { get; set; }
            public quaternion Rotation { get; set; }
            public bool IsActive { get; set; }
            public bool IsInArena { get; set; }
            public DateTime RegisteredAt { get; set; }
            public DateTime LastUpdated { get; set; }
            public DateTime? ArenaTransferAt { get; set; }
            public DateTime? CastleReturnAt { get; set; }
        }
        #endregion
    }
}