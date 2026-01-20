using BepInEx.Logging;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Network;
using ProjectM.Physics;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using VAuto.Services;
using VAuto.Data;

namespace VAuto.Core
{
    /// <summary>
    /// Core utilities for accessing V Rising game systems
    /// </summary>
    public static class Core
    {
        private static World _serverWorld;
        private static World Server
        {
            get
            {
                if (_serverWorld == null || !_serverWorld.IsCreated)
                {
                    _serverWorld = GetWorld("Server");
                }
                return _serverWorld;
            }
        }

        public static World ServerWorld => Server;
        public static EntityManager EntityManager => Server?.EntityManager ?? throw new System.Exception("Server world not available");
        public static double ServerTime => ServerGameManager.ServerTime;
        public static ProjectM.Scripting.ServerGameManager ServerGameManager => Server?.GetExistingSystemManaged<ServerScriptMapper>()._ServerGameManager ?? throw new System.Exception("Server world not available");
        public static ProjectM.PrefabCollectionSystem PrefabCollection => Server?.GetExistingSystemManaged<ProjectM.PrefabCollectionSystem>() ?? throw new System.Exception("Server world not available");

        private static World GetWorld(string name)
        {
            foreach (var world in World.s_AllWorlds)
            {
                if (world.Name == name)
                {
                    return world;
                }
            }

            return null;
        }
        
        public static ManualLogSource Log { get; } = Plugin.Logger;

        static MonoBehaviour monoBehaviour;

        /// <summary>
        /// Log exception with caller information (KindredSchematics pattern)
        /// </summary>
        public static void LogException(System.Exception e, [CallerMemberName] string caller = null)
        {
            Plugin.Log.LogError($"Failure in {caller}\nMessage: {e.Message} Inner:{e.InnerException?.Message}\n\nStack: {e.StackTrace}\nInner Stack: {e.InnerException?.StackTrace}");
        }

        /// <summary>
        /// Initialize core systems after plugin load (KindredSchematics pattern)
        /// </summary>
        internal static void InitializeAfterLoaded()
        {
            if (_hasInitialized) return;
            _hasInitialized = true;

            var initTimestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Log.LogInfo($"[{initTimestamp}] CORE_INITIALIZATION_START - Starting VAuto Core systems initialization");

            // Initialize arena configuration and positions
            var arenaPosStart = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Log.LogInfo($"[{arenaPosStart}] ARENA_POSITIONS_INIT - Initializing arena positions");
            ArenaPositions.InitializePositions();
            Log.LogInfo($"[{arenaPosStart}] ARENA_POSITIONS_COMPLETE - Arena positions initialized");
            
            // Initialize tile system for building prefabs
            var tileStart = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Log.LogInfo($"[{tileStart}] TILE_SYSTEM_INIT - Initializing tile system");
            Tile.Populate();
            Log.LogInfo($"[{tileStart}] TILE_SYSTEM_COMPLETE - Tile system initialized");

            // Initialize complete arena lifecycle system
            var lifecycleStart = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Log.LogInfo($"[{lifecycleStart}] ARENA_LIFECYCLE_INIT - Initializing arena lifecycle system");
            // Arena lifecycle systems are initialized through ServiceManager
            Log.LogInfo($"[{lifecycleStart}] ARENA_LIFECYCLE_COMPLETE - Arena lifecycle system initialized");

            // Initialize arena database service
            var dbStart = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Log.LogInfo($"[{dbStart}] ARENA_DATABASE_INIT - Initializing arena database service");
            // Arena database systems are initialized through ServiceManager
            Log.LogInfo($"[{dbStart}] ARENA_DATABASE_COMPLETE - Arena database service initialized");

            // Initialize KindredExtract service
            var kindredStart = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Log.LogInfo($"[{kindredStart}] KINDEDEXTRACT_INIT - Initializing KindredExtract service");
            // TODO: KindredExtractService.Initialize(); - Service initialization disabled
            Log.LogInfo($"[{kindredStart}] KINDEDEXTRACT_COMPLETE - KindredExtract service initialized");

            var completeTimestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Log.LogInfo($"[{completeTimestamp}] CORE_INITIALIZATION_COMPLETE - VAuto Core fully initialized and ready");
        }

        private static bool _hasInitialized = false;

        /// <summary>
        /// Start coroutine (KindredSchematics pattern)
        /// </summary>
        public static Coroutine StartCoroutine(IEnumerator routine)
        {
            if (monoBehaviour == null)
            {
                var go = new GameObject("VAuto-Core");
                monoBehaviour = go.AddComponent<MonoBehaviour>();
                UnityEngine.Object.DontDestroyOnLoad(go);
            }
            return monoBehaviour.StartCoroutine(routine);
        }

        /// <summary>
        /// Stop coroutine (KindredSchematics pattern)
        /// </summary>
        public static void StopCoroutine(Coroutine coroutine)
        {
            if (monoBehaviour == null) return;
            monoBehaviour.StopCoroutine(coroutine);
        }

        /// <summary>
        /// Safe entity add component (VAMP/Kindred pattern)
        /// </summary>
        public static void AddComponent<T>(Entity entity) where T : struct
        {
            try
            {
                if (EntityManager.Exists(entity) && !EntityManager.HasComponent<T>(entity))
                {
                    EntityManager.AddComponent<T>(entity);
                }
            }
            catch (System.Exception ex)
            {
                LogException(ex);
            }
        }

        /// <summary>
        /// Safe entity write component (VAMP/Kindred pattern)
        /// </summary>
        public static void Write<T>(Entity entity, T component) where T : struct
        {
            try
            {
                if (!EntityManager.Exists(entity)) return;

                if (EntityManager.HasComponent<T>(entity))
                {
                    EntityManager.SetComponentData(entity, component);
                }
                else
                {
                    EntityManager.AddComponentData(entity, component);
                }
            }
            catch (System.Exception ex)
            {
                LogException(ex);
            }
        }

        /// <summary>
        /// Safe entity read component (KindredSchematics pattern)
        /// </summary>
        public static T Read<T>(Entity entity) where T : struct
        {
            try
            {
                if (EntityManager.HasComponent<T>(entity))
                {
                    return EntityManager.GetComponentData<T>(entity);
                }
            }
            catch (System.Exception ex)
            {
                LogException(ex);
            }
            return default;
        }

        /// <summary>
        /// VAMP-style safe component access with Try pattern
        /// </summary>
        public static bool TryGetComponent<T>(Entity entity, out T component) where T : struct
        {
            component = default;
            try
            {
                if (EntityManager.HasComponent<T>(entity))
                {
                    component = EntityManager.GetComponentData<T>(entity);
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                LogException(ex);
            }
            return false;
        }

        public static bool Exists(Entity entity)
        {
            try
            {
                return EntityManager.Exists(entity);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// VAMP-style safe component check (enhanced version)
        /// </summary>
        public static bool HasComponentSafely<T>(Entity entity) where T : struct
        {
            try
            {
                return EntityManager.Exists(entity) && EntityManager.HasComponent<T>(entity);
            }
            catch (System.Exception ex)
            {
                LogException(ex);
                return false;
            }
        }

        /// <summary>
        /// VAMP-style safe component write with Try pattern
        /// </summary>
        public static bool TrySetComponent<T>(Entity entity, T component) where T : struct
        {
            try
            {
                if (EntityManager.HasComponent<T>(entity))
                {
                    EntityManager.SetComponentData(entity, component);
                    return true;
                }
                else
                {
                    EntityManager.AddComponentData(entity, component);
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                LogException(ex);
                return false;
            }
        }

        /// <summary>
        /// Try to get component data from an entity (VAMP pattern)
        /// </summary>
        public static bool TryRead<T>(Entity entity, out T component) where T : struct
        {
            component = default;
            if (EntityManager.Exists(entity) && EntityManager.HasComponent<T>(entity))
            {
                component = EntityManager.GetComponentData<T>(entity);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if entity has component (VAMP pattern)
        /// </summary>
        public static bool Has<T>(Entity entity) where T : struct
        {
            return EntityManager.Exists(entity) && EntityManager.HasComponent<T>(entity);
        }

        /// <summary>
        /// Create entity query (KindredSchematics pattern)
        /// </summary>
        public static EntityQuery CreateQuery(params ComponentType[] componentTypes)
        {
            try
            {
                var queryBuilder = new EntityQueryBuilder(Allocator.Temp);
                foreach (var componentType in componentTypes)
                {
                    queryBuilder.AddAll(componentType);
                }
                return EntityManager.CreateEntityQuery(ref queryBuilder);
            }
            catch (System.Exception ex)
            {
                LogException(ex);
                return default(EntityQuery);
            }
        }

        /// <summary>
        /// Get entities by component types (KindredSchematics pattern)
        /// </summary>
        public static NativeArray<Entity> GetEntitiesByComponentTypes(params ComponentType[] componentTypes)
        {
            try
            {
                var query = CreateQuery(componentTypes);
                if (query == null) return new NativeArray<Entity>(0, Allocator.Temp);
                
                var entities = query.ToEntityArray(Allocator.Temp);
                query.Dispose();
                return entities;
            }
            catch (System.Exception ex)
            {
                LogException(ex);
                return new NativeArray<Entity>(0, Allocator.Temp);
            }
        }

        /// <summary>
        /// Send system message to client (KindredSchematics pattern)
        /// </summary>
        public static void SendSystemMessageToClient(Entity userEntity, string message)
        {
            try
            {
                var fixedString = new FixedString512Bytes(message);
                // TODO: Replace with proper ServerChatUtils implementation
            }
            catch (System.Exception ex)
            {
                LogException(ex);
            }
        }
    }
}
