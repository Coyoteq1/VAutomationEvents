using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using VAuto.Services.Interfaces;
using VAuto.Core;
using VAuto.Data;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Castle Registry Service - Tracks all castles/structures inside PvP zones
    /// Provides fast lookup for automation validation and planning
    /// </summary>
    public class CastleRegistryService : IService
    {
        private static CastleRegistryService _instance;
        public static CastleRegistryService Instance => _instance ??= new CastleRegistryService();

        private bool _isInitialized;
        private readonly Dictionary<int, CastleData> _castles = new();
        private readonly Dictionary<Entity, int> _entityToCastleId = new();
        private readonly object _lock = new object();

        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => Plugin.Logger;

        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            Log?.LogInfo("[CastleRegistryService] Initialized");
        }

        public void Cleanup()
        {
            if (!_isInitialized) return;
            lock (_lock)
            {
                _castles.Clear();
                _entityToCastleId.Clear();
            }
            _isInitialized = false;
            Log?.LogInfo("[CastleRegistryService] Cleaned up");
        }

        /// <summary>
        /// Register a castle in the registry
        /// </summary>
        public bool RegisterCastle(int castleId, string name, float3 center, float radius)
        {
            lock (_lock)
            {
                try
                {
                    if (_castles.ContainsKey(castleId))
                    {
                        Log?.LogWarning($"[CastleRegistryService] Castle {castleId} already registered");
                        return false;
                    }

                    var castleData = new CastleData
                    {
                        Id = castleId,
                        Name = name,
                        Center = center,
                        Radius = radius,
                        Structures = new List<CastleStructure>(),
                        RegisteredAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    _castles[castleId] = castleData;
                    Log?.LogInfo($"[CastleRegistryService] Registered castle '{name}' (ID: {castleId})");
                    return true;
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[CastleRegistryService] Failed to register castle {castleId}: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Unregister a castle from the registry
        /// </summary>
        public bool UnregisterCastle(int castleId)
        {
            lock (_lock)
            {
                try
                {
                    if (!_castles.TryGetValue(castleId, out var castleData))
                        return false;

                    // Remove all entity mappings
                    foreach (var structure in castleData.Structures)
                    {
                        _entityToCastleId.Remove(structure.Entity);
                    }

                    _castles.Remove(castleId);
                    Log?.LogInfo($"[CastleRegistryService] Unregistered castle '{castleData.Name}' (ID: {castleId})");
                    return true;
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[CastleRegistryService] Failed to unregister castle {castleId}: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Add a structure to a castle
        /// </summary>
        public bool AddStructure(int castleId, Entity entity, string structureType, float3 position, PrefabGUID prefabGuid)
        {
            lock (_lock)
            {
                try
                {
                    if (!_castles.TryGetValue(castleId, out var castleData))
                        return false;

                    var structure = new CastleStructure
                    {
                        Entity = entity,
                        Type = structureType,
                        Position = position,
                        PrefabGuid = prefabGuid,
                        AddedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    castleData.Structures.Add(structure);
                    _entityToCastleId[entity] = castleId;

                    Log?.LogDebug($"[CastleRegistryService] Added {structureType} to castle {castleId} at {position}");
                    return true;
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[CastleRegistryService] Failed to add structure to castle {castleId}: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Remove a structure from a castle
        /// </summary>
        public bool RemoveStructure(Entity entity)
        {
            lock (_lock)
            {
                try
                {
                    if (!_entityToCastleId.TryGetValue(entity, out var castleId))
                        return false;

                    if (!_castles.TryGetValue(castleId, out var castleData))
                        return false;

                    var structure = castleData.Structures.FirstOrDefault(s => s.Entity == entity);
                    if (structure != null)
                    {
                        castleData.Structures.Remove(structure);
                        _entityToCastleId.Remove(entity);
                        Log?.LogDebug($"[CastleRegistryService] Removed structure from castle {castleId}");
                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[CastleRegistryService] Failed to remove structure: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Get castle data by ID
        /// </summary>
        public CastleData GetCastle(int castleId)
        {
            lock (_lock)
            {
                return _castles.TryGetValue(castleId, out var castle) ? castle : null;
            }
        }

        /// <summary>
        /// Get castle ID for an entity
        /// </summary>
        public int? GetCastleIdForEntity(Entity entity)
        {
            lock (_lock)
            {
                return _entityToCastleId.TryGetValue(entity, out var castleId) ? castleId : null;
            }
        }

        /// <summary>
        /// Get all castles
        /// </summary>
        public List<CastleData> GetAllCastles()
        {
            lock (_lock)
            {
                return _castles.Values.ToList();
            }
        }

        /// <summary>
        /// Get structures of a specific type in a castle
        /// </summary>
        public List<CastleStructure> GetStructuresByType(int castleId, string structureType)
        {
            lock (_lock)
            {
                if (!_castles.TryGetValue(castleId, out var castleData))
                    return new List<CastleStructure>();

                return castleData.Structures.Where(s => s.Type == structureType && s.IsActive).ToList();
            }
        }

        /// <summary>
        /// Check if a position is within castle bounds
        /// </summary>
        public bool IsPositionInCastle(int castleId, float3 position)
        {
            lock (_lock)
            {
                if (!_castles.TryGetValue(castleId, out var castleData))
                    return false;

                return math.distance(position, castleData.Center) <= castleData.Radius;
            }
        }

        /// <summary>
        /// Get total structure count for a castle
        /// </summary>
        public int GetStructureCount(int castleId)
        {
            lock (_lock)
            {
                return _castles.TryGetValue(castleId, out var castleData) ? castleData.Structures.Count : 0;
            }
        }

        /// <summary>
        /// Validate if a structure can be placed at the given position
        /// </summary>
        public bool CanPlaceStructure(int castleId, float3 position, float minDistance = 2f)
        {
            lock (_lock)
            {
                if (!_castles.TryGetValue(castleId, out var castleData))
                    return false;

                // Check if within castle bounds
                if (!IsPositionInCastle(castleId, position))
                    return false;

                // Check minimum distance from existing structures
                foreach (var structure in castleData.Structures)
                {
                    if (math.distance(position, structure.Position) < minDistance)
                        return false;
                }

                return true;
            }
        }
    }

    /// <summary>
    /// Castle data structure
    /// </summary>
    public class CastleData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public float3 Center { get; set; }
        public float Radius { get; set; }
        public List<CastleStructure> Structures { get; set; }
        public DateTime RegisteredAt { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Castle structure data
    /// </summary>
    public class CastleStructure
    {
        public Entity Entity { get; set; }
        public string Type { get; set; }
        public float3 Position { get; set; }
        public PrefabGUID PrefabGuid { get; set; }
        public DateTime AddedAt { get; set; }
        public bool IsActive { get; set; }
    }
}