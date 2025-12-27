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

namespace VAuto.Services.Lifecycle
{
    /// <summary>
    /// Lifecycle-aware Building service with immediate response to lifecycle changes
    /// </summary>
    public class LifecycleBuildingService : IArenaLifecycleService
    {
        private static bool _initialized = false;
        private static readonly Dictionary<string, ArenaBuildingState> _buildingStates = new();
        private static readonly Dictionary<Entity, BuildingData> _activeBuildings = new();
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
                    Log?.LogInfo("[LifecycleBuildingService] Initializing lifecycle building service...");
                    
                    _buildingStates.Clear();
                    _activeBuildings.Clear();
                    
                    // Register with lifecycle manager
                    ArenaLifecycleManager.RegisterLifecycleService(new LifecycleBuildingService());
                    
                    _initialized = true;
                    
                    Log?.LogInfo("[LifecycleBuildingService] Lifecycle building service initialized successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[LifecycleBuildingService] Failed to initialize: {ex.Message}");
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
                    Log?.LogInfo("[LifecycleBuildingService] Cleaning up lifecycle building service...");
                    
                    // Unregister from lifecycle manager
                    ArenaLifecycleManager.UnregisterLifecycleService(new LifecycleBuildingService());
                    
                    // Clean up all buildings
                    CleanupAllBuildings();
                    
                    _buildingStates.Clear();
                    _activeBuildings.Clear();
                    _initialized = false;
                    
                    Log?.LogInfo("[LifecycleBuildingService] Lifecycle building service cleaned up successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[LifecycleBuildingService] Failed to cleanup: {ex.Message}");
                }
            }
        }
        #endregion

        #region IArenaLifecycleService Implementation
        public bool OnPlayerEnter(Entity user, Entity character, string arenaId)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                if (!em.TryGetComponentData(user, out User userData))
                    return false;

                var platformId = userData.PlatformId;
                var characterName = userData.CharacterName.ToString();

                lock (_lock)
                {
                    // Ensure arena building state exists
                    if (!_buildingStates.ContainsKey(arenaId))
                    {
                        CreateArenaBuildingState(arenaId);
                    }

                    var buildingState = _buildingStates[arenaId];
                    
                    // Create player building data
                    var playerBuildingData = new PlayerBuildingData
                    {
                        PlatformId = platformId,
                        CharacterName = characterName,
                        UserEntity = user,
                        CharacterEntity = character,
                        EnteredAt = DateTime.UtcNow,
                        AvailableStructures = GetAvailableStructuresForPlayer(platformId),
                        BuildPermissions = GetBuildPermissions(platformId)
                    };

                    buildingState.PlayerData[platformId] = playerBuildingData;

                    // Apply arena-specific building rules
                    ApplyArenaBuildingRules(arenaId, platformId);

                    Log?.LogInfo($"[LifecycleBuildingService] Player {characterName} entered arena {arenaId} - building enabled");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleBuildingService] Error in OnPlayerEnter: {ex.Message}");
                return false;
            }
        }

        public bool OnPlayerExit(Entity user, Entity character, string arenaId)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                if (!em.TryGetComponentData(user, out User userData))
                    return false;

                var platformId = userData.PlatformId;
                var characterName = userData.CharacterName.ToString();

                lock (_lock)
                {
                    if (!_buildingStates.TryGetValue(arenaId, out var buildingState))
                        return false;

                    if (!buildingState.PlayerData.TryGetValue(platformId, out var playerBuildingData))
                        return false;

                    // CRITICAL: Destroy all buildings owned by this player
                    DestroyPlayerBuildings(platformId, arenaId);

                    // Remove player data
                    buildingState.PlayerData.Remove(platformId);

                    Log?.LogInfo($"[LifecycleBuildingService] Player {characterName} exited arena {arenaId} - all buildings destroyed and state restored");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleBuildingService] Error in OnPlayerExit: {ex.Message}");
                return false;
            }
        }

        public bool OnArenaStart(string arenaId)
        {
            try
            {
                lock (_lock)
                {
                    // Ensure arena building state exists
                    if (!_buildingStates.ContainsKey(arenaId))
                    {
                        CreateArenaBuildingState(arenaId);
                    }

                    var buildingState = _buildingStates[arenaId];
                    buildingState.IsActive = true;
                    buildingState.StartedAt = DateTime.UtcNow;

                    // Initialize arena-specific building configurations
                    InitializeArenaBuildingConfig(arenaId);

                    Log?.LogInfo($"[LifecycleBuildingService] Arena {arenaId} started - building system activated");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleBuildingService] Error in OnArenaStart: {ex.Message}");
                return false;
            }
        }

        public bool OnArenaEnd(string arenaId)
        {
            try
            {
                lock (_lock)
                {
                    if (!_buildingStates.TryGetValue(arenaId, out var buildingState))
                        return false;

                    // Destroy all buildings in the arena
                    DestroyAllArenaBuildings(arenaId);

                    buildingState.IsActive = false;
                    buildingState.EndedAt = DateTime.UtcNow;

                    Log?.LogInfo($"[LifecycleBuildingService] Arena {arenaId} ended - all buildings destroyed");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleBuildingService] Error in OnArenaEnd: {ex.Message}");
                return false;
            }
        }

        public bool OnBuildStart(Entity user, string structureName, string arenaId)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                if (!em.TryGetComponentData(user, out User userData))
                    return false;

                var platformId = userData.PlatformId;
                var characterName = userData.CharacterName.ToString();

                lock (_lock)
                {
                    if (!_buildingStates.TryGetValue(arenaId, out var buildingState))
                        return false;

                    if (!buildingState.PlayerData.TryGetValue(platformId, out var playerBuildingData))
                        return false;

                    // Validate build permissions
                    if (!ValidateBuildPermission(playerBuildingData, structureName))
                    {
                        Log?.LogWarning($"[LifecycleBuildingService] Player {characterName} lacks permission to build {structureName}");
                        return false;
                    }

                    // Create building record
                    var buildingData = new BuildingData
                    {
                        BuildingId = GenerateBuildingId(),
                        StructureName = structureName,
                        ArenaId = arenaId,
                        OwnerPlatformId = platformId,
                        OwnerCharacterName = characterName,
                        UserEntity = user,
                        Status = BuildingStatus.Building,
                        StartedAt = DateTime.UtcNow,
                        Position = float3.zero, // Will be set when build completes
                        Rotation = quaternion.identity
                    };

                    _activeBuildings[user] = buildingData;

                    Log?.LogInfo($"[LifecycleBuildingService] Build started: {structureName} by {characterName} in arena {arenaId}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleBuildingService] Error in OnBuildStart: {ex.Message}");
                return false;
            }
        }

        public bool OnBuildComplete(Entity user, string structureName, string arenaId)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                if (!em.TryGetComponentData(user, out User userData))
                    return false;

                var platformId = userData.PlatformId;
                var characterName = userData.CharacterName.ToString();

                lock (_lock)
                {
                    if (!_activeBuildings.TryGetValue(user, out var buildingData))
                        return false;

                    // Update building status
                    buildingData.Status = BuildingStatus.Completed;
                    buildingData.CompletedAt = DateTime.UtcNow;

                    // Create the actual building entity
                    var buildingEntity = CreateBuildingEntity(buildingData);

                    Log?.LogInfo($"[LifecycleBuildingService] Build completed: {structureName} by {characterName} in arena {arenaId}");

                    // Trigger visual effects for completion
                    TriggerBuildCompletionEffects(buildingData);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleBuildingService] Error in OnBuildComplete: {ex.Message}");
                return false;
            }
        }

        public bool OnBuildDestroy(Entity user, string structureName, string arenaId)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                if (!em.TryGetComponentData(user, out User userData))
                    return false;

                var platformId = userData.PlatformId;
                var characterName = userData.CharacterName.ToString();

                lock (_lock)
                {
                    // Find and destroy the building
                    var buildingToDestroy = _activeBuildings.Values
                        .FirstOrDefault(b => b.OwnerPlatformId == platformId && 
                                            b.StructureName == structureName && 
                                            b.ArenaId == arenaId);

                    if (buildingToDestroy != null)
                    {
                        // Destroy the building entity
                        if (buildingToDestroy.BuildingEntity != Entity.Null)
                        {
                            em.DestroyEntity(buildingToDestroy.BuildingEntity);
                        }

                        // Remove from active buildings
                        _activeBuildings.Remove(user);

                        Log?.LogInfo($"[LifecycleBuildingService] Build destroyed: {structureName} by {characterName} in arena {arenaId}");

                        // Trigger destruction effects
                        TriggerBuildDestructionEffects(buildingToDestroy);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleBuildingService] Error in OnBuildDestroy: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Building Management
        public static bool StartBuild(Entity user, string structureName, string arenaId, float3 position, quaternion? rotation = null)
        {
            try
            {
                // Trigger build start through lifecycle manager
                var startSuccess = ArenaLifecycleManager.OnBuildStart(user, structureName, arenaId);
                
                if (!startSuccess)
                    return false;

                // Set position for the building
                if (_activeBuildings.TryGetValue(user, out var buildingData))
                {
                    buildingData.Position = position;
                    buildingData.Rotation = rotation ?? quaternion.identity;
                }

                // Simulate build time (in real implementation, this would be based on structure type)
                var buildTime = GetBuildTimeForStructure(structureName);
                
                // Complete the build after the build time
                System.Threading.Tasks.Task.Delay((int)(buildTime * 1000)).ContinueWith(_ =>
                {
                    ArenaLifecycleManager.OnBuildComplete(user, structureName, arenaId);
                });

                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleBuildingService] Failed to start build: {ex.Message}");
                return false;
            }
        }

        public static bool DestroyBuilding(Entity user, string structureName, string arenaId)
        {
            try
            {
                // Trigger build destruction through lifecycle manager
                return ArenaLifecycleManager.OnBuildDestroy(user, structureName, arenaId);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleBuildingService] Failed to destroy building: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Helper Methods
        private static void CreateArenaBuildingState(string arenaId)
        {
            _buildingStates[arenaId] = new ArenaBuildingState
            {
                ArenaId = arenaId,
                IsActive = false,
                PlayerData = new Dictionary<ulong, PlayerBuildingData>(),
                BuildingConfigs = GetDefaultBuildingConfigs(),
                CreatedAt = DateTime.UtcNow
            };
        }

        private static void ApplyArenaBuildingRules(string arenaId, ulong platformId)
        {
            try
            {
                var buildingState = _buildingStates[arenaId];
                var playerData = buildingState.PlayerData[platformId];

                // Apply arena-specific rules
                switch (arenaId.ToLower())
                {
                    case "main_arena":
                        // Main arena rules - all structures allowed
                        playerData.MaxStructures = 20;
                        playerData.BuildRange = 15f;
                        break;
                        
                    case "pvp_arena":
                        // PvP arena rules - limited structures
                        playerData.MaxStructures = 10;
                        playerData.BuildRange = 10f;
                        playerData.AvailableStructures.RemoveAll(s => s.Contains("portal") || s.Contains("waygate"));
                        break;
                }

                Log?.LogDebug($"[LifecycleBuildingService] Applied arena building rules for {arenaId}");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleBuildingService] Error applying arena building rules: {ex.Message}");
            }
        }

        private static void InitializeArenaBuildingConfig(string arenaId)
        {
            // Initialize arena-specific building configurations
            Log?.LogDebug($"[LifecycleBuildingService] Initialized building config for arena {arenaId}");
        }

        private static List<string> GetAvailableStructuresForPlayer(ulong platformId)
        {
            // Return available structures based on player permissions
            return new List<string> { "wall", "floor", "portal", "waygate", "glow" };
        }

        private static List<string> GetBuildPermissions(ulong platformId)
        {
            // Return build permissions based on player role/level
            return new List<string> { "build", "destroy", "modify" };
        }

        private static bool ValidateBuildPermission(PlayerBuildingData playerData, string structureName)
        {
            // Validate if player can build this structure
            return playerData.AvailableStructures.Contains(structureName) && 
                   playerData.BuildPermissions.Contains("build");
        }

        private static string GenerateBuildingId()
        {
            return $"building_{Guid.NewGuid():N}";
        }

        private static Entity CreateBuildingEntity(BuildingData buildingData)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                var buildingEntity = em.CreateEntity();

                // Add transform components
                em.AddComponentData(buildingEntity, new Translation { Value = buildingData.Position });
                em.AddComponentData(buildingEntity, new Rotation { Value = buildingData.Rotation });

                // Add building-specific components based on structure type
                AddBuildingComponents(buildingEntity, buildingData.StructureName);

                // Update building data
                buildingData.BuildingEntity = buildingEntity;

                return buildingEntity;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleBuildingService] Failed to create building entity: {ex.Message}");
                return Entity.Null;
            }
        }

        private static void AddBuildingComponents(Entity buildingEntity, string structureName)
        {
            var em = VAutoCore.EntityManager;

            switch (structureName.ToLower())
            {
                case "wall":
                    em.AddComponentData(buildingEntity, new BuildingHealthComponent { Health = 100f, MaxHealth = 100f });
                    break;
                case "portal":
                    em.AddComponentData(buildingEntity, new PortalComponent { IsActive = true, Destination = float3.zero });
                    break;
                case "glow":
                    em.AddComponentData(buildingEntity, new LightComponent { Intensity = 1f, Range = 10f });
                    break;
                case "waygate":
                    em.AddComponentData(buildingEntity, new WaygateComponent { IsActive = true, Destination = float3.zero });
                    break;
            }
        }

        private static void DestroyPlayerBuildings(ulong platformId, string arenaId)
        {
            try
            {
                var buildingsToDestroy = _activeBuildings.Values
                    .Where(b => b.OwnerPlatformId == platformId && b.ArenaId == arenaId)
                    .ToList();

                foreach (var building in buildingsToDestroy)
                {
                    if (building.BuildingEntity != Entity.Null)
                    {
                        var em = VAutoCore.EntityManager;
                        em.DestroyEntity(building.BuildingEntity);
                    }
                    
                    // Remove from active buildings
                    _activeBuildings.Remove(building.UserEntity);
                }

                Log?.LogInfo($"[LifecycleBuildingService] Destroyed {buildingsToDestroy.Count} buildings for player {platformId} in arena {arenaId}");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleBuildingService] Error destroying player buildings: {ex.Message}");
            }
        }

        private static void DestroyAllArenaBuildings(string arenaId)
        {
            try
            {
                var buildingsToDestroy = _activeBuildings.Values
                    .Where(b => b.ArenaId == arenaId)
                    .ToList();

                foreach (var building in buildingsToDestroy)
                {
                    if (building.BuildingEntity != Entity.Null)
                    {
                        var em = VAutoCore.EntityManager;
                        em.DestroyEntity(building.BuildingEntity);
                    }
                    
                    _activeBuildings.Remove(building.UserEntity);
                }

                Log?.LogInfo($"[LifecycleBuildingService] Destroyed {buildingsToDestroy.Count} buildings in arena {arenaId}");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleBuildingService] Error destroying arena buildings: {ex.Message}");
            }
        }

        private static void CleanupAllBuildings()
        {
            try
            {
                foreach (var building in _activeBuildings.Values)
                {
                    if (building.BuildingEntity != Entity.Null)
                    {
                        var em = VAutoCore.EntityManager;
                        em.DestroyEntity(building.BuildingEntity);
                    }
                }
                
                _activeBuildings.Clear();
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LifecycleBuildingService] Error cleaning up buildings: {ex.Message}");
            }
        }

        private static float GetBuildTimeForStructure(string structureName)
        {
            return structureName.ToLower() switch
            {
                "wall" => 2.0f,
                "floor" => 1.0f,
                "portal" => 5.0f,
                "waygate" => 8.0f,
                "glow" => 1.5f,
                _ => 3.0f
            };
        }

        private static Dictionary<string, BuildingConfig> GetDefaultBuildingConfigs()
        {
            return new Dictionary<string, BuildingConfig>
            {
                ["wall"] = new BuildingConfig { Name = "Wall", BuildTime = 2.0f, MaxHealth = 100f },
                ["floor"] = new BuildingConfig { Name = "Floor", BuildTime = 1.0f, MaxHealth = 50f },
                ["portal"] = new BuildingConfig { Name = "Portal", BuildTime = 5.0f, MaxHealth = 200f },
                ["waygate"] = new BuildingConfig { Name = "Waygate", BuildTime = 8.0f, MaxHealth = 300f },
                ["glow"] = new BuildingConfig { Name = "Glow", BuildTime = 1.5f, MaxHealth = 25f }
            };
        }

        private static void TriggerBuildCompletionEffects(BuildingData buildingData)
        {
            // Trigger visual effects when build completes
            Log?.LogDebug($"[LifecycleBuildingService] Triggering completion effects for {buildingData.StructureName}");
        }

        private static void TriggerBuildDestructionEffects(BuildingData buildingData)
        {
            // Trigger visual effects when building is destroyed
            Log?.LogDebug($"[LifecycleBuildingService] Triggering destruction effects for {buildingData.StructureName}");
        }
        #endregion

        #region Query Methods
        public static List<BuildingData> GetPlayerBuildings(ulong platformId, string arenaId)
        {
            lock (_lock)
            {
                return _activeBuildings.Values.Where(b => b.OwnerPlatformId == platformId && b.ArenaId == arenaId).ToList();
            }
        }

        public static List<BuildingData> GetArenaBuildings(string arenaId)
        {
            lock (_lock)
            {
                return _activeBuildings.Values.Where(b => b.ArenaId == arenaId).ToList();
            }
        }

        public static int GetPlayerBuildingCount(ulong platformId, string arenaId)
        {
            lock (_lock)
            {
                return _activeBuildings.Values.Count(b => b.OwnerPlatformId == platformId && b.ArenaId == arenaId);
            }
        }

        public static bool CanPlayerBuild(ulong platformId, string arenaId, string structureName)
        {
            lock (_lock)
            {
                if (!_buildingStates.TryGetValue(arenaId, out var buildingState))
                    return false;

                if (!buildingState.PlayerData.TryGetValue(platformId, out var playerData))
                    return false;

                var currentCount = GetPlayerBuildingCount(platformId, arenaId);
                
                return currentCount < playerData.MaxStructures && 
                       playerData.AvailableStructures.Contains(structureName) &&
                       playerData.BuildPermissions.Contains("build");
            }
        }
        #endregion

        #region Data Structures
        public enum BuildingStatus
        {
            Building,
            Completed,
            Destroyed
        }

        public class ArenaBuildingState
        {
            public string ArenaId { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? StartedAt { get; set; }
            public DateTime? EndedAt { get; set; }
            public Dictionary<ulong, PlayerBuildingData> PlayerData { get; set; } = new();
            public Dictionary<string, BuildingConfig> BuildingConfigs { get; set; } = new();
        }

        public class PlayerBuildingData
        {
            public ulong PlatformId { get; set; }
            public string CharacterName { get; set; }
            public Entity UserEntity { get; set; }
            public Entity CharacterEntity { get; set; }
            public DateTime EnteredAt { get; set; }
            public List<string> AvailableStructures { get; set; } = new();
            public List<string> BuildPermissions { get; set; } = new();
            public int MaxStructures { get; set; } = 10;
            public float BuildRange { get; set; } = 10f;
        }

        public class BuildingData
        {
            public string BuildingId { get; set; }
            public string StructureName { get; set; }
            public string ArenaId { get; set; }
            public ulong OwnerPlatformId { get; set; }
            public string OwnerCharacterName { get; set; }
            public Entity UserEntity { get; set; }
            public Entity BuildingEntity { get; set; }
            public BuildingStatus Status { get; set; }
            public float3 Position { get; set; }
            public quaternion Rotation { get; set; }
            public DateTime StartedAt { get; set; }
            public DateTime? CompletedAt { get; set; }
        }

        public class BuildingConfig
        {
            public string Name { get; set; }
            public float BuildTime { get; set; }
            public float MaxHealth { get; set; }
            public List<string> RequiredPermissions { get; set; } = new();
        }

        // Component structs for buildings
        public struct BuildingHealthComponent : IComponentData
        {
            public float Health;
            public float MaxHealth;
        }

        public struct PortalComponent : IComponentData
        {
            public bool IsActive;
            public float3 Destination;
        }

        public struct LightComponent : IComponentData
        {
            public float Intensity;
            public float Range;
        }

        public struct WaygateComponent : IComponentData
        {
            public bool IsActive;
            public float3 Destination;
            public int RequiredLevel;
        }
        #endregion
    }
}