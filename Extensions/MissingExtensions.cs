using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM;
using ProjectM.Network;

namespace VAuto.Extensions
{
    /// <summary>
    /// Missing extension methods that are referenced but not implemented
    /// </summary>
    public static class MissingExtensions
    {
        /// <summary>
        /// Get user safely with EntityManager
        /// </summary>
        public static User GetUserSafely(this Entity entity, EntityManager entityManager)
        {
            try
            {
                // First validate that the entity exists in this EntityManager
                if (entity == Entity.Null || !entityManager.Exists(entity))
                {
                    Plugin.Logger?.LogDebug($"GetUserSafely: Entity {entity.Index} does not exist in EntityManager");
                    return default;
                }

                if (entityManager.HasComponent<User>(entity))
                {
                    return entityManager.GetComponentData<User>(entity);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"GetUserSafely failed for entity {entity.Index}: {ex.Message}");
                Plugin.Logger?.LogError($"GetUserSafely stack trace: {ex.StackTrace}");
            }
            return default;
        }

        /// <summary>
        /// Get translation safely with EntityManager
        /// </summary>
        public static Translation GetTranslationSafely(this Entity entity, EntityManager entityManager)
        {
            try
            {
                // First validate that the entity exists in this EntityManager
                if (entity == Entity.Null || !entityManager.Exists(entity))
                {
                    Plugin.Logger?.LogDebug($"GetTranslationSafely: Entity {entity.Index} does not exist in EntityManager");
                    return new Translation { Value = float3.zero };
                }

                if (entityManager.HasComponent<Translation>(entity))
                {
                    return entityManager.GetComponentData<Translation>(entity);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"GetTranslationSafely failed for entity {entity.Index}: {ex.Message}");
                Plugin.Logger?.LogError($"GetTranslationSafely stack trace: {ex.StackTrace}");
            }
            return new Translation { Value = float3.zero };
        }

        /// <summary>
        /// Get local character entity safely from User
        /// </summary>
        public static Entity GetLocalCharacterSafely(this User user)
        {
            try
            {
                if (user.LocalCharacter._Entity != Entity.Null)
                {
                    return user.LocalCharacter._Entity;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"GetLocalCharacterSafely failed for user {user.PlatformId}: {ex.Message}");
            }
            return Entity.Null;
        }

        /// <summary>
        /// Get player character safely from Entity
        /// </summary>
        public static Entity GetPlayerCharacterSafely(this Entity entity)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                if (em.Exists(entity) && em.HasComponent<User>(entity))
                {
                    var user = em.GetComponentData<User>(entity);
                    return user.GetLocalCharacterSafely();
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"GetPlayerCharacterSafely failed for entity {entity.Index}: {ex.Message}");
            }
            return Entity.Null;
        }

        /// <summary>
        /// Check if entity is valid
        /// </summary>
        public static bool IsValidEntity(this Entity entity)
        {
            try
            {
                return entity != Entity.Null && VAutoCore.EntityManager.Exists(entity);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if entity has component
        /// </summary>
        public static bool Has<T>(this Entity entity) where T : unmanaged
        {
            try
            {
                return VAutoCore.EntityManager.HasComponent<T>(entity);
            }
            catch
            {
                return false;
            }
        }
    }
}
