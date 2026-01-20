using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using ProjectM;
using ProjectM.Network;
using VAuto.Core;
using VAuto.Services.Systems;
using BepInEx.Logging;

namespace VAuto.Services.ECS
{
    /// <summary>
    /// ECS System for managing arena building functionality
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial struct ArenaBuildSystem : ISystem
    {
        private EntityQuery _buildRequestQuery;

        public void OnCreate(ref SystemState state)
        {
            _buildRequestQuery = state.GetEntityQuery(ComponentType.ReadOnly<BuildRequestComponent>());
            
            // Initialize build service
            ArenaBuildService.Instance.Initialize();
        }

        public void OnUpdate(ref SystemState state)
        {
            try
            {
                if (!ArenaBuildService.Instance.IsInitialized)
                    return;

                var deltaTime = SystemAPI.Time.DeltaTime;

                // Process build requests
                ProcessBuildRequests(ref state);
                
                // Validate build permissions
                ValidateBuildPermissions(ref state);
                
                // Update build validation
                UpdateBuildValidation(ref state);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaBuildSystem] Error in OnUpdate: {ex.Message}");
            }
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        private static void ProcessBuildRequests(ref SystemState state)
        {
            try
            {
                var ecb = new EntityCommandBuffer(Allocator.Temp);

                foreach (var (buildRequest, entity) in SystemAPI.Query<RefRO<BuildRequestComponent>>().WithEntityAccess())
                {
                    var request = buildRequest.ValueRO;
                    
                    // Validate build request
                    if (!ValidateBuildRequest(request))
                    {
                        // Mark as failed and continue
                        ecb.SetComponent(entity, new BuildResultComponent
                        {
                            Success = false,
                            ErrorMessage = "Invalid build request",
                            Timestamp = SystemAPI.Time.ElapsedTime
                        });
                        continue;
                    }

                    // Attempt to build
                    var success = ArenaBuildService.Instance.BuildStructure(
                        request.UserEntity,
                        request.StructureName,
                        request.Position,
                        request.Rotation
                    );

                    // Update result
                    ecb.SetComponent(entity, new BuildResultComponent
                    {
                        Success = success,
                        ErrorMessage = success ? null : "Build failed",
                        Timestamp = SystemAPI.Time.ElapsedTime,
                        ArenaId = request.ArenaId
                    });

                    if (success)
                    {
                        // Create built structure entity
                        CreateStructureEntity(ref ecb, request);
                        
                        Plugin.Logger?.LogInfo($"[ArenaBuildSystem] Successfully built {request.StructureName} for player");
                    }
                    else
                    {
                        Plugin.Logger?.LogWarning($"[ArenaBuildSystem] Failed to build {request.StructureName} for player");
                    }
                }

                ecb.Playback(state.EntityManager);
                ecb.Dispose();
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaBuildSystem] Error processing build requests: {ex.Message}");
            }
        }

        private static void ValidateBuildPermissions(ref SystemState state)
        {
            try
            {
                // Check player build permissions
                foreach (var (user, buildPermission) in SystemAPI.Query<RefRO<User>, RefRO<BuildPermissionComponent>>())
                {
                    var platformId = user.ValueRO.PlatformId;
                    var canBuild = buildPermission.ValueRO.CanBuild;

                    // Update build service with permission
                    ArenaBuildService.Instance.SetPlayerBuildPermission(platformId, canBuild);
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaBuildSystem] Error validating build permissions: {ex.Message}");
            }
        }

        private static void UpdateBuildValidation(ref SystemState state)
        {
            try
            {
                // Update validation for existing structures
                var structures = ArenaBuildService.Instance.GetAllArenaObjects();
                
                foreach (var structure in structures)
                {
                    // Validate structure is still in valid build area
                    if (!ZoneService.IsInArena(structure.Position))
                    {
                        // Structure is outside arena, may need to be removed
                        Plugin.Logger?.LogWarning($"[ArenaBuildSystem] Structure {structure.Name} is outside arena bounds");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaBuildSystem] Error updating build validation: {ex.Message}");
            }
        }

        private static bool ValidateBuildRequest(BuildRequestComponent request)
        {
            try
            {
                // Check if entities are valid
                if (!VAuto.Core.Core.Exists(request.UserEntity) || !VAuto.Core.Core.Exists(request.StructureEntity))
                    return false;

                // Check if structure name is valid
                var availableStructures = ArenaBuildService.Instance.GetAvailableStructures();
                if (!availableStructures.Contains(request.StructureName))
                    return false;

                // Check if position is valid for building
                if (!ArenaBuildService.Instance.CanBuildAt(request.UserEntity, request.Position))
                    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaBuildSystem] Error validating build request: {ex.Message}");
                return false;
            }
        }

        private static void CreateStructureEntity(ref EntityCommandBuffer ecb, BuildRequestComponent request)
        {
            try
            {
                var structureEntity = ecb.CreateEntity();
                
                // Add transform components
                ecb.AddComponent(structureEntity, LocalTransform.FromPositionRotationScale(
                    request.Position, request.Rotation ?? quaternion.identity, 1.0f));

                // Add structure component
                ecb.AddComponent(structureEntity, new StructureComponent
                {
                    StructureName = request.StructureName,
                    ArenaId = request.ArenaId,
                    BuiltBy = request.UserEntity,
                    BuiltAt = SystemAPI.Time.ElapsedTime,
                    StructureType = GetStructureType(request.StructureName)
                });

                // Add arena object component
                ecb.AddComponent(structureEntity, new ArenaObjectComponent
                {
                    ArenaId = request.ArenaId,
                    ObjectType = ArenaObjectService.ArenaObjectType.Structure,
                    Name = request.StructureName,
                    IsActive = true
                });

                // Add structure-specific components based on type
                AddStructureSpecificComponents(ref ecb, structureEntity, request.StructureName);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaBuildSystem] Error creating structure entity: {ex.Message}");
            }
        }

        private static ArenaBuildService.StructureType GetStructureType(string structureName)
        {
            return structureName.ToLower() switch
            {
                "wall" => ArenaBuildService.StructureType.Wall,
                "floor" => ArenaBuildService.StructureType.Floor,
                "portal" => ArenaBuildService.StructureType.Portal,
                "glow" => ArenaBuildService.StructureType.Light,
                "waygate" => ArenaBuildService.StructureType.Waygate,
                _ => ArenaBuildService.StructureType.Structure
            };
        }

        private static void AddStructureSpecificComponents(ref EntityCommandBuffer ecb, Entity entity, string structureName)
        {
            try
            {
                switch (structureName.ToLower())
                {
                    case "wall":
                        // Add wall-specific components
                        ecb.AddComponent(entity, new WallComponent
                        {
                            Health = 100.0f,
                            MaxHealth = 100.0f,
                            MaterialType = "stone"
                        });
                        break;

                    case "portal":
                        // Add portal-specific components
                        ecb.AddComponent(entity, new PortalComponent
                        {
                            IsActive = true,
                            Destination = float3.zero,
                            TeleportCooldown = 0.0f
                        });
                        break;

                    case "glow":
                        // Add light/glow components
                        ecb.AddComponent(entity, new LightComponent
                        {
                            Intensity = 1.0f,
                            Range = 10.0f,
                            LightType = "point"
                        });
                        break;

                    case "waygate":
                        // Add waygate components
                        ecb.AddComponent(entity, new WaygateComponent
                        {
                            IsActive = true,
                            Destination = float3.zero,
                            RequiredLevel = 1
                        });
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaBuildSystem] Error adding structure-specific components: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// ECS System for monitoring and updating built structures
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ArenaBuildSystem))]
    public partial struct StructureUpdateSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            try
            {
                if (!ArenaBuildService.Instance.IsInitialized)
                    return;

                var deltaTime = SystemAPI.Time.DeltaTime;

                // Update structure states
                UpdateStructureStates(ref state, deltaTime);
                
                // Process structure interactions
                ProcessStructureInteractions(ref state);
                
                // Clean up destroyed structures
                CleanupDestroyedStructures(ref state);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[StructureUpdateSystem] Error in OnUpdate: {ex.Message}");
            }
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        private static void UpdateStructureStates(ref SystemState state, float deltaTime)
        {
            try
            {
                foreach (var (structure, transform, entity) in SystemAPI.Query<RefRO<StructureComponent>, RefRO<LocalTransform>>().WithEntityAccess())
                {
                    var structureData = structure.ValueRO;
                    
                    // Update durability for damageable structures
                    if (structureData.StructureType == ArenaBuildService.StructureType.Wall)
                    {
                        UpdateWallStructure(ref state, entity, deltaTime);
                    }
                    
                    // Update portal structures
                    if (structureData.StructureType == ArenaBuildService.StructureType.Portal)
                    {
                        UpdatePortalStructure(ref state, entity, deltaTime);
                    }
                    
                    // Update light structures
                    if (structureData.StructureType == ArenaBuildService.StructureType.Light)
                    {
                        UpdateLightStructure(ref state, entity, deltaTime);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[StructureUpdateSystem] Error updating structure states: {ex.Message}");
            }
        }

        private static void UpdateWallStructure(ref SystemState state, Entity entity, float deltaTime)
        {
            try
            {
                if (SystemAPI.HasComponent<WallComponent>(entity))
                {
                    var wall = SystemAPI.GetComponent<WallComponent>(entity);
                    
                    // Natural degradation over time
                    if (wall.Health > 0)
                    {
                        wall.Health = math.max(0, wall.Health - deltaTime * 0.1f);
                        SystemAPI.SetComponent(entity, wall);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[StructureUpdateSystem] Error updating wall structure: {ex.Message}");
            }
        }

        private static void UpdatePortalStructure(ref SystemState state, Entity entity, float deltaTime)
        {
            try
            {
                if (SystemAPI.HasComponent<PortalComponent>(entity))
                {
                    var portal = SystemAPI.GetComponent<PortalComponent>(entity);
                    
                    // Update cooldown
                    if (portal.TeleportCooldown > 0)
                    {
                        portal.TeleportCooldown = math.max(0, portal.TeleportCooldown - deltaTime);
                        SystemAPI.SetComponent(entity, portal);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[StructureUpdateSystem] Error updating portal structure: {ex.Message}");
            }
        }

        private static void UpdateLightStructure(ref SystemState state, Entity entity, float deltaTime)
        {
            try
            {
                if (SystemAPI.HasComponent<LightComponent>(entity))
                {
                    var light = SystemAPI.GetComponent<LightComponent>(entity);
                    
                    // Flickering effect
                    var flicker = (float)math.sin(SystemAPI.Time.ElapsedTime * 8.0) * 0.1f + 1.0f;
                    light.Intensity = math.clamp(flicker, 0.5f, 1.5f);
                    SystemAPI.SetComponent(entity, light);
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[StructureUpdateSystem] Error updating light structure: {ex.Message}");
            }
        }

        private static void ProcessStructureInteractions(ref SystemState state)
        {
            // Process player interactions with structures
            // This would handle things like using portals, triggering traps, etc.
        }

        private static void CleanupDestroyedStructures(ref SystemState state)
        {
            try
            {
                var ecb = new EntityCommandBuffer(Allocator.Temp);

                foreach (var (structure, entity) in SystemAPI.Query<RefRO<StructureComponent>>().WithEntityAccess())
                {
                    // Check if structure should be destroyed
                    if (ShouldDestroyStructure(structure.ValueRO))
                    {
                        // Remove from arena object service
                        ArenaObjectService.RemoveObjectFromArena(entity);
                        
                        // Destroy entity
                        ecb.DestroyEntity(entity);
                        
                        Plugin.Logger?.LogInfo($"[StructureUpdateSystem] Cleaned up destroyed structure {structure.ValueRO.StructureName}");
                    }
                }

                ecb.Playback(state.EntityManager);
                ecb.Dispose();
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[StructureUpdateSystem] Error cleaning up destroyed structures: {ex.Message}");
            }
        }

        private static bool ShouldDestroyStructure(StructureComponent structure)
        {
            // Determine if structure should be destroyed
            // For walls, if health is 0
            // For other structures, based on age or other conditions
            
            return false; // Placeholder
        }
    }

    #region Component Definitions

    /// <summary>
    /// Component for build requests
    /// </summary>
    public struct BuildRequestComponent : IComponentData
    {
        public Entity UserEntity;
        public Entity StructureEntity;
        public string StructureName;
        public string ArenaId;
        public float3 Position;
        public quaternion? Rotation;
    }

    /// <summary>
    /// Component for build results
    /// </summary>
    public struct BuildResultComponent : IComponentData
    {
        public bool Success;
        public string ErrorMessage;
        public double Timestamp;
        public string ArenaId;
    }

    /// <summary>
    /// Component for build permissions
    /// </summary>
    public struct BuildPermissionComponent : IComponentData
    {
        public bool CanBuild;
        public Entity PlayerEntity;
    }

    /// <summary>
    /// Component for built structures
    /// </summary>
    public struct StructureComponent : IComponentData
    {
        public string StructureName;
        public string ArenaId;
        public Entity BuiltBy;
        public double BuiltAt;
        public ArenaBuildService.StructureType StructureType;
    }

    /// <summary>
    /// Component for arena objects
    /// </summary>
    public struct ArenaObjectComponent : IComponentData
    {
        public string ArenaId;
        public ArenaObjectService.ArenaObjectType ObjectType;
        public string Name;
        public bool IsActive;
    }

    /// <summary>
    /// Component for wall structures
    /// </summary>
    public struct WallComponent : IComponentData
    {
        public float Health;
        public float MaxHealth;
        public string MaterialType;
    }

    /// <summary>
    /// Component for portal structures
    /// </summary>
    public struct PortalComponent : IComponentData
    {
        public bool IsActive;
        public float3 Destination;
        public float TeleportCooldown;
    }

    /// <summary>
    /// Component for light structures
    /// </summary>
    public struct LightComponent : IComponentData
    {
        public float Intensity;
        public float Range;
        public string LightType;
    }

    /// <summary>
    /// Component for waygate structures
    /// </summary>
    public struct WaygateComponent : IComponentData
    {
        public bool IsActive;
        public float3 Destination;
        public int RequiredLevel;
    }

    #endregion
}