using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;

namespace VAuto.Extensions
{
    /// <summary>
    /// ECS Extension Methods for Entity Operations
    /// Provides comprehensive entity manipulation utilities following KindredSchematics patterns
    /// </summary>
    public static class ECSExtensions
    {
        /// <summary>
        /// Safe entity existence check with EntityManager
        /// </summary>
        public static bool Exists(this Entity entity, EntityManager entityManager)
        {
            try
            {
                return entityManager.Exists(entity);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Safe component check with EntityManager
        /// </summary>
        public static bool Has<T>(this Entity entity, EntityManager entityManager) where T : struct
        {
            try
            {
                return entityManager.HasComponent<T>(entity);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Safe component read with EntityManager
        /// </summary>
        public static T Read<T>(this Entity entity, EntityManager entityManager) where T : struct
        {
            try
            {
                if (entityManager.HasComponent<T>(entity))
                {
                    return entityManager.GetComponentData<T>(entity);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Read<{typeof(T).Name}> failed for entity {entity.Index}: {ex.Message}");
            }
            return default;
        }

        /// <summary>
        /// Safe component write with EntityManager
        /// </summary>
        public static void Write<T>(this Entity entity, EntityManager entityManager, T component) where T : struct
        {
            try
            {
                if (entityManager.Exists(entity))
                {
                    if (!entityManager.HasComponent<T>(entity))
                    {
                        entityManager.AddComponentData(entity, component);
                    }
                    else
                    {
                        entityManager.SetComponentData(entity, component);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Write<{typeof(T).Name}> failed for entity {entity.Index}: {ex.Message}");
            }
        }

        /// <summary>
        /// Safe buffer access with EntityManager
        /// </summary>
        public static DynamicBuffer<T> GetBuffer<T>(this Entity entity, EntityManager entityManager) where T : struct
        {
            try
            {
                if (entityManager.Exists(entity) && entityManager.HasBuffer<T>(entity))
                {
                    return entityManager.GetBuffer<T>(entity);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"GetBuffer<{typeof(T).Name}> failed for entity {entity.Index}: {ex.Message}");
            }
            return default;
        }

        /// <summary>
        /// Safe buffer read-only access with EntityManager
        /// </summary>
        public static DynamicBuffer<T> GetBufferReadOnly<T>(this Entity entity, EntityManager entityManager) where T : struct
        {
            try
            {
                if (entityManager.Exists(entity) && entityManager.HasBuffer<T>(entity))
                {
                    return entityManager.GetBufferReadOnly<T>(entity);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"GetBufferReadOnly<{typeof(T).Name}> failed for entity {entity.Index}: {ex.Message}");
            }
            return default;
        }

        /// <summary>
        /// Safe entity destruction with EntityManager
        /// </summary>
        public static void Destroy(this Entity entity, EntityManager entityManager)
        {
            try
            {
                if (entityManager.Exists(entity))
                {
                    entityManager.DestroyEntity(entity);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Destroy failed for entity {entity.Index}: {ex.Message}");
            }
        }

        /// <summary>
        /// Safe entity instantiation with EntityManager
        /// </summary>
        public static Entity Instantiate(this Entity entity, EntityManager entityManager)
        {
            try
            {
                return entityManager.Instantiate(entity);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Instantiate failed for entity {entity.Index}: {ex.Message}");
                return Entity.Null;
            }
        }

        /// <summary>
        /// Get entities by component type with query options
        /// </summary>
        public static NativeArray<Entity> GetEntitiesByComponentType<T>(EntityManager entityManager, bool includeAll = false, bool includeDisabled = false, bool includeSpawn = false, bool includePrefab = false, bool includeDestroyed = false) where T : struct
        {
            try
            {
                EntityQueryOptions options = EntityQueryOptions.Default;
                if (includeAll) options |= EntityQueryOptions.IncludeAll;
                if (includeDisabled) options |= EntityQueryOptions.IncludeDisabled;
                if (includeSpawn) options |= EntityQueryOptions.IncludeSpawnTag;
                if (includePrefab) options |= EntityQueryOptions.IncludePrefab;
                if (includeDestroyed) options |= EntityQueryOptions.IncludeDestroyTag;

                var entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
                    .AddAll(new ComponentType(Il2CppType.Of<T>(), ComponentType.AccessMode.ReadWrite))
                    .WithOptions(options);

                var query = entityManager.CreateEntityQuery(ref entityQueryBuilder);

                var entities = query.ToEntityArray(Allocator.Temp);
                return entities;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"GetEntitiesByComponentType<{typeof(T).Name}> failed: {ex.Message}");
                return new NativeArray<Entity>(0, Allocator.Temp);
            }
        }

        /// <summary>
        /// Get entities by multiple component types
        /// </summary>
        public static NativeArray<Entity> GetEntitiesByComponentTypes<T1, T2>(EntityManager entityManager, bool includeAll = false, bool includeDisabled = false, bool includeSpawn = false, bool includePrefab = false, bool includeDestroyed = false) where T1 : struct where T2 : struct
        {
            try
            {
                EntityQueryOptions options = EntityQueryOptions.Default;
                if (includeAll) options |= EntityQueryOptions.IncludeAll;
                if (includeDisabled) options |= EntityQueryOptions.IncludeDisabled;
                if (includeSpawn) options |= EntityQueryOptions.IncludeSpawnTag;
                if (includePrefab) options |= EntityQueryOptions.IncludePrefab;
                if (includeDestroyed) options |= EntityQueryOptions.IncludeDestroyTag;

                var entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
                    .AddAll(new ComponentType(Il2CppType.Of<T1>(), ComponentType.AccessMode.ReadWrite))
                    .AddAll(new ComponentType(Il2CppType.Of<T2>(), ComponentType.AccessMode.ReadWrite))
                    .WithOptions(options);

                var query = entityManager.CreateEntityQuery(ref entityQueryBuilder);

                var entities = query.ToEntityArray(Allocator.Temp);
                return entities;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"GetEntitiesByComponentTypes<{typeof(T1).Name},{typeof(T2).Name}> failed: {ex.Message}");
                return new NativeArray<Entity>(0, Allocator.Temp);
            }
        }

        /// <summary>
        /// Get translation safely with EntityManager
        /// </summary>
        public static float3 GetTranslation(this Entity entity, EntityManager entityManager)
        {
            try
            {
                if (entityManager.HasComponent<Translation>(entity))
                {
                    var translation = entityManager.GetComponentData<Translation>(entity);
                    return translation.Value;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"GetTranslation failed for entity {entity.Index}: {ex.Message}");
            }
            return float3.zero;
        }

        /// <summary>
        /// Set translation safely with EntityManager
        /// </summary>
        public static void SetTranslation(this Entity entity, EntityManager entityManager, float3 position)
        {
            try
            {
                if (entityManager.HasComponent<Translation>(entity))
                {
                    var translation = entityManager.GetComponentData<Translation>(entity);
                    translation.Value = position;
                    entityManager.SetComponentData(entity, translation);
                }
                else
                {
                    entityManager.AddComponentData(entity, new Translation { Value = position });
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"SetTranslation failed for entity {entity.Index}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get local transform safely with EntityManager
        /// </summary>
        public static LocalTransform GetLocalTransform(this Entity entity, EntityManager entityManager)
        {
            try
            {
                if (entityManager.HasComponent<LocalTransform>(entity))
                {
                    return entityManager.GetComponentData<LocalTransform>(entity);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"GetLocalTransform failed for entity {entity.Index}: {ex.Message}");
            }
            return LocalTransform.Identity;
        }

        /// <summary>
        /// Set local transform safely with EntityManager
        /// </summary>
        public static void SetLocalTransform(this Entity entity, EntityManager entityManager, LocalTransform transform)
        {
            try
            {
                if (entityManager.HasComponent<LocalTransform>(entity))
                {
                    entityManager.SetComponentData(entity, transform);
                }
                else
                {
                    entityManager.AddComponentData(entity, transform);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"SetLocalTransform failed for entity {entity.Index}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get distance between two entities
        /// </summary>
        public static float GetDistance(this Entity entity1, Entity entity2, EntityManager entityManager)
        {
            try
            {
                var pos1 = entity1.GetTranslation(entityManager);
                var pos2 = entity2.GetTranslation(entityManager);
                return math.distance(pos1, pos2);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"GetDistance failed between entities {entity1.Index} and {entity2.Index}: {ex.Message}");
                return float.MaxValue;
            }
        }

        /// <summary>
        /// Check if entity is within radius of position
        /// </summary>
        public static bool IsWithinRadius(this Entity entity, float3 center, float radius, EntityManager entityManager)
        {
            try
            {
                var entityPos = entity.GetTranslation(entityManager);
                return math.distance(entityPos, center) <= radius;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"IsWithinRadius failed for entity {entity.Index}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get all entities within radius of position
        /// </summary>
        public static List<Entity> GetEntitiesWithinRadius<T>(float3 center, float radius, EntityManager entityManager) where T : struct
        {
            var entities = new List<Entity>();
            try
            {
                var foundEntities = GetEntitiesByComponentType<T>(entityManager, includeSpawn: true, includeDisabled: true);
                foreach (var entity in foundEntities)
                {
                    if (entity.IsWithinRadius(center, radius, entityManager))
                    {
                        entities.Add(entity);
                    }
                }
                foundEntities.Dispose();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"GetEntitiesWithinRadius<{typeof(T).Name}> failed: {ex.Message}");
            }
            return entities;
        }

        /// <summary>
        /// Safe component addition
        /// </summary>
        public static void AddComponent<T>(this Entity entity, EntityManager entityManager) where T : struct
        {
            try
            {
                if (!entityManager.HasComponent<T>(entity))
                {
                    entityManager.AddComponent<T>(entity);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"AddComponent<{typeof(T).Name}> failed for entity {entity.Index}: {ex.Message}");
            }
        }

        /// <summary>
        /// Safe component removal
        /// </summary>
        public static void RemoveComponent<T>(this Entity entity, EntityManager entityManager) where T : struct
        {
            try
            {
                if (entityManager.HasComponent<T>(entity))
                {
                    entityManager.RemoveComponent<T>(entity);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"RemoveComponent<{typeof(T).Name}> failed for entity {entity.Index}: {ex.Message}");
            }
        }

        /// <summary>
        /// Create entity with components
        /// </summary>
        public static Entity CreateEntityWithComponents(EntityManager entityManager, params (ComponentType componentType, object componentData)[] components)
        {
            try
            {
                var entity = entityManager.CreateEntity();
                foreach (var (componentType, componentData) in components)
                {
                    entityManager.AddComponentData(entity, componentData);
                }
                return entity;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"CreateEntityWithComponents failed: {ex.Message}");
                return Entity.Null;
            }
        }

        /// <summary>
        /// Get entity debug info
        /// </summary>
        public static string GetDebugInfo(this Entity entity, EntityManager entityManager)
        {
            try
            {
                var info = $"Entity[{entity.Index}:{entity.Version}]";
                if (entityManager.Exists(entity))
                {
                    var componentCount = entityManager.GetComponentCount(entity);
                    info += $" Components:{componentCount}";
                    
                    if (entityManager.HasComponent<PrefabGUID>(entity))
                    {
                        var prefabGuid = entityManager.GetComponentData<PrefabGUID>(entity);
                        info += $" PrefabGUID:{prefabGuid}";
                    }
                }
                else
                {
                    info += " [DOES NOT EXIST]";
                }
                return info;
            }
            catch (Exception ex)
            {
                return $"Entity[{entity.Index}] [ERROR: {ex.Message}]";
            }
        }
    }

}












