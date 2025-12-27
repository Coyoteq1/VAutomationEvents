using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM;
using ProjectM.Network;
using ProjectM.Gameplay;
using VAuto.Core;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Arena Build Service - Manages building functionality within arena
    /// </summary>
    public static class ArenaBuildService
    {
        private static bool _initialized = false;
        private static readonly Dictionary<string, BuildData> _structures = new();
        private static readonly Dictionary<ulong, BuildPermissions> _playerPermissions = new();
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
                    Log?.LogInfo("[ArenaBuildService] Initializing arena build service...");
                    
                    _structures.Clear();
                    _playerPermissions.Clear();
                    InitializeDefaultStructures();
                    _initialized = true;
                    
                    Log?.LogInfo("[ArenaBuildService] Arena build service initialized successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[ArenaBuildService] Failed to initialize: {ex.Message}");
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
                    Log?.LogInfo("[ArenaBuildService] Cleaning up arena build service...");
                    
                    _structures.Clear();
                    _playerPermissions.Clear();
                    _initialized = false;
                    
                    Log?.LogInfo("[ArenaBuildService] Arena build service cleaned up successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[ArenaBuildService] Failed to cleanup: {ex.Message}");
                }
            }
        }

        private static void InitializeDefaultStructures()
        {
            // Register default buildable structures
            RegisterStructure("wall", "Basic wall structure", StructureType.Wall, true);
            RegisterStructure("floor", "Basic floor structure", StructureType.Floor, true);
            RegisterStructure("portal", "Portal structure", StructureType.Portal, true);
            RegisterStructure("glow", "Light structure", StructureType.Light, true);
            RegisterStructure("waygate", "Waygate structure", StructureType.Waygate, true);
        }
        #endregion

        #region Structure Management
        public static bool RegisterStructure(string name, string description, StructureType type, bool isBuildable)
        {
            lock (_lock)
            {
                try
                {
                    if (_structures.ContainsKey(name))
                    {
                        Log?.LogWarning($"[ArenaBuildService] Structure '{name}' already registered");
                        return false;
                    }

                    var structureData = new BuildData
                    {
                        Name = name,
                        Description = description,
                        Type = type,
                        IsBuildable = isBuildable,
                        CreatedAt = DateTime.UtcNow,
                        Position = float3.zero,
                        Rotation = quaternion.identity,
                        BuiltBy = 0
                    };

                    _structures[name] = structureData;
                    Log?.LogInfo($"[ArenaBuildService] Registered structure '{name}'");
                    return true;
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[ArenaBuildService] Failed to register structure '{name}': {ex.Message}");
                    return false;
                }
            }
        }

        public static bool BuildStructure(Entity user, string structureName, float3 position, quaternion? rotation = null)
        {
            lock (_lock)
            {
                try
                {
                    if (!_structures.TryGetValue(structureName, out var structureData))
                    {
                        Log?.LogWarning($"[ArenaBuildService] Unknown structure '{structureName}'");
                        return false;
                    }

                    if (!structureData.IsBuildable)
                    {
                        Log?.LogWarning($"[ArenaBuildService] Structure '{structureName}' is not buildable");
                        return false;
                    }

                    var em = VAutoCore.EntityManager;
                    if (!em.TryGetComponentData(user, out User userData))
                    {
                        Log?.LogWarning("[ArenaBuildService] Invalid user entity");
                        return false;
                    }

                    // Check build permissions
                    if (!CanBuildAt(user, position))
                    {
                        Log?.LogWarning($"[ArenaBuildService] Player {userData.PlatformId} cannot build at position {position}");
                        return false;
                    }

                    // Create the structure entity
                    var structureEntity = CreateStructureEntity(structureData, position, rotation ?? quaternion.identity, userData.PlatformId);
                    
                    if (structureEntity == Entity.Null)
                    {
                        Log?.LogError($"[ArenaBuildService] Failed to create structure entity for '{structureName}'");
                        return false;
                    }

                    // Update structure data
                    structureData.Position = position;
                    structureData.Rotation = rotation ?? quaternion.identity;
                    structureData.BuiltBy = userData.PlatformId;
                    structureData.BuiltAt = DateTime.UtcNow;
                    structureData.Entity = structureEntity;
                    structureData.IsBuilt = true;

                    Log?.LogInfo($"[ArenaBuildService] Player {userData.CharacterName} built '{structureName}' at {position}");
                    return true;
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[ArenaBuildService] Failed to build structure '{structureName}': {ex.Message}");
                    return false;
                }
            }
        }

        public static bool RemoveStructure(string structureName)
        {
            lock (_lock)
            {
                try
                {
                    if (!_structures.TryGetValue(structureName, out var structureData))
                        return false;

                    // Destroy the entity
                    var em = VAutoCore.EntityManager;
                    if (structureData.Entity != Entity.Null && em.Exists(structureData.Entity))
                    {
                        em.DestroyEntity(structureData.Entity);
                    }

                    // Reset structure data
                    structureData.IsBuilt = false;
                    structureData.Entity = Entity.Null;
                    structureData.BuiltBy = 0;
                    structureData.BuiltAt = DateTime.MinValue;

                    Log?.LogInfo($"[ArenaBuildService] Removed structure '{structureName}'");
                    return true;
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[ArenaBuildService] Failed to remove structure '{structureName}': {ex.Message}");
                    return false;
                }
            }
        }

        private static Entity CreateStructureEntity(BuildData structureData, float3 position, quaternion rotation, ulong builtBy)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                
                // Create new entity
                var entity = em.CreateEntity();
                
                // Add basic transform components
                em.AddComponentData(entity, new Translation { Value = position });
                em.AddComponentData(entity, new Rotation { Value = rotation });
                
                // Add structure-specific components based on type
                switch (structureData.Type)
                {
                    case StructureType.Wall:
                        AddWallComponents(entity, em);
                        break;
                    case StructureType.Floor:
                        AddFloorComponents(entity, em);
                        break;
                    case StructureType.Portal:
                        AddPortalComponents(entity, em, builtBy);
                        break;
                    case StructureType.Light:
                        AddLightComponents(entity, em);
                        break;
                    case StructureType.Waygate:
                        AddWaygateComponents(entity, em, builtBy);
                        break;
                }

                return entity;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaBuildService] Failed to create structure entity: {ex.Message}");
                return Entity.Null;
            }
        }

        private static void AddWallComponents(Entity entity, EntityManager em)
        {
            // Add wall-specific components
            // This would include collision, visual, and structural components
        }

        private static void AddFloorComponents(Entity entity, EntityManager em)
        {
            // Add floor-specific components
        }

        private static void AddPortalComponents(Entity entity, EntityManager em, ulong owner)
        {
            // Add portal-specific components with owner information
        }

        private static void AddLightComponents(Entity entity, EntityManager em)
        {
            // Add light components
        }

        private static void AddWaygateComponents(Entity entity, EntityManager em, ulong owner)
        {
            // Add waygate-specific components
        }
        #endregion

        #region Build Permissions
        public static void SetPlayerBuildPermission(ulong platformId, bool canBuild)
        {
            lock (_lock)
            {
                _playerPermissions[platformId] = new BuildPermissions
                {
                    PlatformId = platformId,
                    CanBuild = canBuild,
                    GrantedAt = DateTime.UtcNow
                };
            }
        }

        public static bool CanBuildAt(Entity user, float3 position)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                if (!em.TryGetComponentData(user, out User userData))
                    return false;

                // Check if player has build permission
                if (!_playerPermissions.TryGetValue(userData.PlatformId, out var permissions) || !permissions.CanBuild)
                    return false;

                // Check if position is within buildable area
                if (!ZoneService.IsInArena(position))
                    return false;

                // Check if position is too close to existing structures
                if (IsPositionOccupied(position, 2f))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaBuildService] Build permission check failed: {ex.Message}");
                return false;
            }
        }

        private static bool IsPositionOccupied(float3 position, float minDistance)
        {
            return _structures.Values.Any(s => 
                s.IsBuilt && math.distance(s.Position, position) < minDistance);
        }
        #endregion

        #region Query Methods
        public static List<string> GetAvailableStructures()
        {
            lock (_lock)
            {
                return _structures.Values.Where(s => s.IsBuildable).Select(s => s.Name).ToList();
            }
        }

        public static BuildData GetStructure(string name)
        {
            lock (_lock)
            {
                return _structures.TryGetValue(name, out var structure) ? structure : null;
            }
        }

        public static List<BuildData> GetPlayerStructures(ulong platformId)
        {
            lock (_lock)
            {
                return _structures.Values.Where(s => s.BuiltBy == platformId && s.IsBuilt).ToList();
            }
        }

        public static int GetTotalBuiltStructures()
        {
            lock (_lock)
            {
                return _structures.Values.Count(s => s.IsBuilt);
            }
        }

        public static List<BuildData> GetStructuresInRange(float3 center, float radius)
        {
            lock (_lock)
            {
                return _structures.Values.Where(s => 
                    s.IsBuilt && math.distance(s.Position, center) <= radius).ToList();
            }
        }
        #endregion

        #region Data Structures
        public enum StructureType
        {
            Wall,
            Floor,
            Portal,
            Light,
            Waygate,
            Decoration,
            Functional
        }

        public class BuildData
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public StructureType Type { get; set; }
            public bool IsBuildable { get; set; }
            public bool IsBuilt { get; set; }
            public Entity Entity { get; set; }
            public float3 Position { get; set; }
            public quaternion Rotation { get; set; }
            public ulong BuiltBy { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime BuiltAt { get; set; }
        }

        public class BuildPermissions
        {
            public ulong PlatformId { get; set; }
            public bool CanBuild { get; set; }
            public DateTime GrantedAt { get; set; }
        }
        #endregion
    }
}