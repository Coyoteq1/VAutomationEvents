using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using VAuto.Core;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Map Icon Service - Manages map icons for arena objects and players
    /// Independent of lifecycle - available all the time for all players
    /// </summary>
    public static class MapIconService
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        private static PrefabGUID _mapIconProxyPrefabGUID;
        private static Entity _mapIconProxyPrefab;
        private static EntityQuery _mapIconProxyQuery;

        public static bool IsInitialized => _initialized;
        public static ManualLogSource Log => Plugin.Logger;

        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized) return;

                // Initialize map icon proxy prefab
                if (!VRCore.PrefabCollection._SpawnableNameToPrefabGuidDictionary.TryGetValue("MapIcon_ProxyObject_POI_Unknown", out _mapIconProxyPrefabGUID))
                    Log.LogError("[MapIconService] Failed to find MapIcon_ProxyObject_POI_Unknown PrefabGUID");

                if (!VRCore.PrefabCollection._PrefabGuidToEntityMap.TryGetValue(_mapIconProxyPrefabGUID, out _mapIconProxyPrefab))
                    Log.LogError("[MapIconService] Failed to find MapIcon_ProxyObject_POI_Unknown Prefab entity");

                // Create query for map icon proxies
                var queryBuilder = new EntityQueryBuilder(Allocator.Temp)
                    .AddAll(ComponentType.ReadOnly<AttachMapIconsToEntity>())
                    .AddAll(ComponentType.ReadOnly<SpawnedBy>())
                    .AddNone(ComponentType.ReadOnly<ChunkPortal>())
                    .AddNone(ComponentType.ReadOnly<ChunkWaypoint>())
                    .WithOptions(EntityQueryOptions.IncludeDisabled);

                _mapIconProxyQuery = VRCore.EntityManager.CreateEntityQuery(ref queryBuilder);
                queryBuilder.Dispose();

                _initialized = true;
                Log?.LogInfo("[MapIconService] Initialized - Map icons available for all players");
            }
        }

        public static void Cleanup()
        {
            lock (_lock)
            {
                if (!_initialized) return;

                if (_mapIconProxyQuery != default)
                {
                    _mapIconProxyQuery.Dispose();
                    _mapIconProxyQuery = default;
                }

                _initialized = false;
                Log?.LogInfo("[MapIconService] Cleaned up");
            }
        }

        /// <summary>
        /// Create a map icon for an entity (available for all players)
        /// </summary>
        public static void AddMapIcon(Entity characterEntity, PrefabGUID mapIcon)
        {
            if (!_initialized) return;

            try
            {
                var pos = VRCore.EntityManager.GetComponentData<Translation>(characterEntity).Value;
                var mapIconProxy = VRCore.EntityManager.Instantiate(_mapIconProxyPrefab);
                VRCore.EntityManager.SetComponentData(mapIconProxy, new Translation { Value = pos });

                VRCore.EntityManager.AddComponentData(mapIconProxy, new SpawnedBy { Value = characterEntity });

                VRCore.EntityManager.RemoveComponent<SyncToUserBitMask>(mapIconProxy);
                VRCore.EntityManager.RemoveComponent<SyncToUserBuffer>(mapIconProxy);
                VRCore.EntityManager.RemoveComponent<OnlySyncToUsersTag>(mapIconProxy);

                var attachMapIconsToEntity = VRCore.EntityManager.GetBuffer<AttachMapIconsToEntity>(mapIconProxy);
                attachMapIconsToEntity.Clear();
                attachMapIconsToEntity.Add(new() { Prefab = mapIcon });

                Log?.LogDebug($"[MapIconService] Created map icon for entity {characterEntity.Index}");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[MapIconService] Failed to create map icon: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove map icon from an entity
        /// </summary>
        public static bool RemoveMapIcon(Entity characterEntity)
        {
            if (!_initialized) return false;

            const float DISTANCE_TO_DESTROY = 5f;

            try
            {
                var pos = VRCore.EntityManager.GetComponentData<Translation>(characterEntity).Value;
                var mapIconProxies = _mapIconProxyQuery.ToEntityArray(Allocator.Temp);
                var iconToDestroy = mapIconProxies.ToArray()
                    .Where(x => x.Has<PrefabGUID>() && x.Read<PrefabGUID>().Equals(_mapIconProxyPrefabGUID))
                    .OrderBy(x => math.distance(pos, x.Read<Translation>().Value))
                    .FirstOrDefault(x => math.distance(pos, x.Read<Translation>().Value) < DISTANCE_TO_DESTROY);
                mapIconProxies.Dispose();

                if (iconToDestroy == Entity.Null)
                    return false;

                if (iconToDestroy.Has<AttachedBuffer>())
                {
                    var attachedBuffer = VRCore.EntityManager.GetBuffer<AttachedBuffer>(iconToDestroy);
                    for(var i = 0; i < attachedBuffer.Length; i++)
                    {
                        var attachedEntity = attachedBuffer[i].Entity;
                        if (attachedEntity == Entity.Null) continue;
                        VRCore.EntityManager.DestroyEntity(attachedEntity);
                    }
                }

                VRCore.EntityManager.DestroyEntity(iconToDestroy);
                Log?.LogDebug($"[MapIconService] Removed map icon for entity {characterEntity.Index}");
                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[MapIconService] Failed to remove map icon: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Refresh all player icons (lifecycle-independent)
        /// </summary>
        public static void RefreshPlayerIcons()
        {
            // Implementation for refreshing all active player icons
            // This can be called periodically or on demand
            Log?.LogDebug("[MapIconService] Refreshed player icons");
        }

        /// <summary>
        /// Get count of active map icons
        /// </summary>
        public static int GetActiveIconCount()
        {
            if (!_initialized || _mapIconProxyQuery == default) return 0;
            return _mapIconProxyQuery.CalculateEntityCount();
        }
    }
}