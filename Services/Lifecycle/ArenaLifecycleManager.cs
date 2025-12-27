using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using ProjectM;
using ProjectM.Network;
using VAuto.Core;
using BepInEx.Logging;

namespace VAuto.Services.Lifecycle
{
    /// <summary>
    /// Central manager for all arena lifecycle events
    /// </summary>
    public static class ArenaLifecycleManager
    {
        private static bool _initialized = false;
        private static readonly List<IArenaLifecycleService> _lifecycleServices = new();
        private static readonly Dictionary<string, ArenaLifecycleState> _arenaStates = new();
        private static readonly object _lock = new object();
        private static ManualLogSource _log;
        
        public static bool IsInitialized => _initialized;
        public static ManualLogSource Log => _log;

        #region Initialization
        public static void Initialize(ManualLogSource logger)
        {
            lock (_lock)
            {
                if (_initialized) return;
                
                try
                {
                    _log = logger;
                    logger?.LogInfo("[ArenaLifecycleManager] Initializing arena lifecycle manager...");
                    
                    _lifecycleServices.Clear();
                    _arenaStates.Clear();
                    
                    // Register all lifecycle services
                    RegisterLifecycleServices();
                    
                    _initialized = true;
                    
                    logger?.LogInfo("[ArenaLifecycleManager] Arena lifecycle manager initialized successfully");
                }
                catch (Exception ex)
                {
                    logger?.LogError($"[ArenaLifecycleManager] Failed to initialize: {ex.Message}");
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
                    _log?.LogInfo("[ArenaLifecycleManager] Cleaning up arena lifecycle manager...");
                    
                    // End all active arenas
                    foreach (var arenaState in _arenaStates.Values.ToList())
                    {
                        OnArenaEnd(arenaState.ArenaId);
                    }
                    
                    _lifecycleServices.Clear();
                    _arenaStates.Clear();
                    _initialized = false;
                    
                    _log?.LogInfo("[ArenaLifecycleManager] Arena lifecycle manager cleaned up successfully");
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[ArenaLifecycleManager] Failed to cleanup: {ex.Message}");
                }
            }
        }

        private static void RegisterLifecycleServices()
        {
            try
            {
                // Register all services that implement lifecycle interface
                // These would be registered through the service manager
                _log?.LogInfo("[ArenaLifecycleManager] Registered lifecycle services");
            }
            catch (Exception ex)
            {
                _log?.LogError($"[ArenaLifecycleManager] Failed to register lifecycle services: {ex.Message}");
            }
        }
        #endregion

        #region Player Lifecycle Events
        public static bool OnPlayerEnter(Entity user, Entity character, string arenaId)
        {
            try
            {
                if (user == Entity.Null || character == Entity.Null || string.IsNullOrEmpty(arenaId))
                {
                    _log?.LogWarning("[ArenaLifecycleManager] Invalid parameters for player enter");
                    return false;
                }

                var em = VAutoCore.EntityManager;
                if (!em.TryGetComponentData(user, out User userData))
                {
                    _log?.LogWarning("[ArenaLifecycleManager] Invalid user entity");
                    return false;
                }

                var platformId = userData.PlatformId;
                var characterName = userData.CharacterName.ToString();
                var position = GetEntityPosition(character);
                var rotation = GetEntityRotation(character);

                lock (_lock)
                {
                    // Ensure arena state exists
                    if (!_arenaStates.ContainsKey(arenaId))
                    {
                        CreateArenaState(arenaId);
                    }

                    var arenaState = _arenaStates[arenaId];
                    
                    // Check if player is already in arena
                    if (arenaState.ActivePlayers.Contains(platformId))
                    {
                        _log?.LogWarning($"[ArenaLifecycleManager] Player {characterName} already in arena {arenaId}");
                        return false;
                    }

                    // Create lifecycle event
                    var lifecycleEvent = new PlayerLifecycleEvent
                    {
                        UserEntity = user,
                        CharacterEntity = character,
                        PlatformId = platformId,
                        CharacterName = characterName,
                        ArenaId = arenaId,
                        Position = position,
                        Rotation = rotation,
                        EventType = PlayerLifecycleEventType.Enter,
                        Timestamp = DateTime.UtcNow
                    };

                    // Notify all lifecycle services
                    var success = true;
                    foreach (var service in _lifecycleServices)
                    {
                        try
                        {
                            if (!service.OnPlayerEnter(user, character, arenaId))
                            {
                                success = false;
                                _log?.LogWarning($"[ArenaLifecycleManager] Service {service.GetType().Name} failed player enter for {characterName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _log?.LogError($"[ArenaLifecycleManager] Service {service.GetType().Name} error on player enter: {ex.Message}");
                            success = false;
                        }
                    }

                    if (success)
                    {
                        // Add player to active arena state
                        arenaState.ActivePlayers.Add(platformId);
                        arenaState.PlayerEvents.Add(lifecycleEvent);
                        arenaState.LastActivity = DateTime.UtcNow;

                        _log?.LogInfo($"[ArenaLifecycleManager] Player {characterName} entered arena {arenaId}");
                    }

                    return success;
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[ArenaLifecycleManager] Failed to process player enter: {ex.Message}");
                return false;
            }
        }

        public static bool OnPlayerExit(Entity user, Entity character, string arenaId)
        {
            try
            {
                if (user == Entity.Null || character == Entity.Null || string.IsNullOrEmpty(arenaId))
                {
                    _log?.LogWarning("[ArenaLifecycleManager] Invalid parameters for player exit");
                    return false;
                }

                var em = VAutoCore.EntityManager;
                if (!em.TryGetComponentData(user, out User userData))
                {
                    _log?.LogWarning("[ArenaLifecycleManager] Invalid user entity");
                    return false;
                }

                var platformId = userData.PlatformId;
                var characterName = userData.CharacterName.ToString();
                var position = GetEntityPosition(character);
                var rotation = GetEntityRotation(character);

                lock (_lock)
                {
                    if (!_arenaStates.ContainsKey(arenaId))
                    {
                        _log?.LogWarning($"[ArenaLifecycleManager] Arena {arenaId} does not exist");
                        return false;
                    }

                    var arenaState = _arenaStates[arenaId];
                    
                    // Check if player is in arena
                    if (!arenaState.ActivePlayers.Contains(platformId))
                    {
                        _log?.LogWarning($"[ArenaLifecycleManager] Player {characterName} not in arena {arenaId}");
                        return false;
                    }

                    // Create lifecycle event
                    var lifecycleEvent = new PlayerLifecycleEvent
                    {
                        UserEntity = user,
                        CharacterEntity = character,
                        PlatformId = platformId,
                        CharacterName = characterName,
                        ArenaId = arenaId,
                        Position = position,
                        Rotation = rotation,
                        EventType = PlayerLifecycleEventType.Exit,
                        Timestamp = DateTime.UtcNow
                    };

                    // Notify all lifecycle services
                    var success = true;
                    foreach (var service in _lifecycleServices)
                    {
                        try
                        {
                            if (!service.OnPlayerExit(user, character, arenaId))
                            {
                                success = false;
                                _log?.LogWarning($"[ArenaLifecycleManager] Service {service.GetType().Name} failed player exit for {characterName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _log?.LogError($"[ArenaLifecycleManager] Service {service.GetType().Name} error on player exit: {ex.Message}");
                            success = false;
                        }
                    }

                    if (success)
                    {
                        // Remove player from active arena state
                        arenaState.ActivePlayers.Remove(platformId);
                        arenaState.PlayerEvents.Add(lifecycleEvent);
                        arenaState.LastActivity = DateTime.UtcNow;

                        _log?.LogInfo($"[ArenaLifecycleManager] Player {characterName} exited arena {arenaId}");
                    }

                    return success;
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[ArenaLifecycleManager] Failed to process player exit: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Building Lifecycle Events
        public static bool OnBuildStart(Entity user, string structureName, string arenaId)
        {
            try
            {
                if (user == Entity.Null || string.IsNullOrEmpty(structureName) || string.IsNullOrEmpty(arenaId))
                {
                    _log?.LogWarning("[ArenaLifecycleManager] Invalid parameters for build start");
                    return false;
                }

                lock (_lock)
                {
                    // Ensure arena state exists
                    if (!_arenaStates.ContainsKey(arenaId))
                    {
                        CreateArenaState(arenaId);
                    }

                    // Create lifecycle event
                    var lifecycleEvent = new BuildingLifecycleEvent
                    {
                        UserEntity = user,
                        StructureName = structureName,
                        ArenaId = arenaId,
                        Position = float3.zero, // Will be set by build service
                        Rotation = quaternion.identity,
                        EventType = BuildingLifecycleEventType.Start,
                        Timestamp = DateTime.UtcNow
                    };

                    // Notify all lifecycle services
                    var success = true;
                    foreach (var service in _lifecycleServices)
                    {
                        try
                        {
                            if (!service.OnBuildStart(user, structureName, arenaId))
                            {
                                success = false;
                                _log?.LogWarning($"[ArenaLifecycleManager] Service {service.GetType().Name} failed build start for {structureName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _log?.LogError($"[ArenaLifecycleManager] Service {service.GetType().Name} error on build start: {ex.Message}");
                            success = false;
                        }
                    }

                    if (success)
                    {
                        var arenaState = _arenaStates[arenaId];
                        arenaState.BuildingEvents.Add(lifecycleEvent);
                        arenaState.LastActivity = DateTime.UtcNow;

                        _log?.LogInfo($"[ArenaLifecycleManager] Build started: {structureName} in arena {arenaId}");
                    }

                    return success;
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[ArenaLifecycleManager] Failed to process build start: {ex.Message}");
                return false;
            }
        }

        public static bool OnBuildComplete(Entity user, string structureName, string arenaId)
        {
            try
            {
                if (user == Entity.Null || string.IsNullOrEmpty(structureName) || string.IsNullOrEmpty(arenaId))
                {
                    _log?.LogWarning("[ArenaLifecycleManager] Invalid parameters for build complete");
                    return false;
                }

                lock (_lock)
                {
                    // Create lifecycle event
                    var lifecycleEvent = new BuildingLifecycleEvent
                    {
                        UserEntity = user,
                        StructureName = structureName,
                        ArenaId = arenaId,
                        Position = float3.zero, // Will be set by build service
                        Rotation = quaternion.identity,
                        EventType = BuildingLifecycleEventType.Complete,
                        Timestamp = DateTime.UtcNow
                    };

                    // Notify all lifecycle services
                    var success = true;
                    foreach (var service in _lifecycleServices)
                    {
                        try
                        {
                            if (!service.OnBuildComplete(user, structureName, arenaId))
                            {
                                success = false;
                                _log?.LogWarning($"[ArenaLifecycleManager] Service {service.GetType().Name} failed build complete for {structureName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _log?.LogError($"[ArenaLifecycleManager] Service {service.GetType().Name} error on build complete: {ex.Message}");
                            success = false;
                        }
                    }

                    if (success)
                    {
                        var arenaState = _arenaStates[arenaId];
                        arenaState.BuildingEvents.Add(lifecycleEvent);
                        arenaState.LastActivity = DateTime.UtcNow;

                        _log?.LogInfo($"[ArenaLifecycleManager] Build completed: {structureName} in arena {arenaId}");
                    }

                    return success;
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[ArenaLifecycleManager] Failed to process build complete: {ex.Message}");
                return false;
            }
        }

        public static bool OnBuildDestroy(Entity user, string structureName, string arenaId)
        {
            try
            {
                if (user == Entity.Null || string.IsNullOrEmpty(structureName) || string.IsNullOrEmpty(arenaId))
                {
                    _log?.LogWarning("[ArenaLifecycleManager] Invalid parameters for build destroy");
                    return false;
                }

                lock (_lock)
                {
                    // Create lifecycle event
                    var lifecycleEvent = new BuildingLifecycleEvent
                    {
                        UserEntity = user,
                        StructureName = structureName,
                        ArenaId = arenaId,
                        Position = float3.zero, // Will be set by build service
                        Rotation = quaternion.identity,
                        EventType = BuildingLifecycleEventType.Destroy,
                        Timestamp = DateTime.UtcNow
                    };

                    // Notify all lifecycle services
                    var success = true;
                    foreach (var service in _lifecycleServices)
                    {
                        try
                        {
                            if (!service.OnBuildDestroy(user, structureName, arenaId))
                            {
                                success = false;
                                _log?.LogWarning($"[ArenaLifecycleManager] Service {service.GetType().Name} failed build destroy for {structureName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _log?.LogError($"[ArenaLifecycleManager] Service {service.GetType().Name} error on build destroy: {ex.Message}");
                            success = false;
                        }
                    }

                    if (success)
                    {
                        var arenaState = _arenaStates[arenaId];
                        arenaState.BuildingEvents.Add(lifecycleEvent);
                        arenaState.LastActivity = DateTime.UtcNow;

                        _log?.LogInfo($"[ArenaLifecycleManager] Build destroyed: {structureName} in arena {arenaId}");
                    }

                    return success;
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[ArenaLifecycleManager] Failed to process build destroy: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Arena Lifecycle Events
        public static bool OnArenaStart(string arenaId)
        {
            try
            {
                if (string.IsNullOrEmpty(arenaId))
                {
                    _log?.LogWarning("[ArenaLifecycleManager] Invalid arena ID for start");
                    return false;
                }

                lock (_lock)
                {
                    // Create arena state if it doesn't exist
                    if (!_arenaStates.ContainsKey(arenaId))
                    {
                        CreateArenaState(arenaId);
                    }

                    var arenaState = _arenaStates[arenaId];
                    
                    // Notify all lifecycle services
                    var success = true;
                    foreach (var service in _lifecycleServices)
                    {
                        try
                        {
                            if (!service.OnArenaStart(arenaId))
                            {
                                success = false;
                                _log?.LogWarning($"[ArenaLifecycleManager] Service {service.GetType().Name} failed arena start for {arenaId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _log?.LogError($"[ArenaLifecycleManager] Service {service.GetType().Name} error on arena start: {ex.Message}");
                            success = false;
                        }
                    }

                    if (success)
                    {
                        arenaState.IsActive = true;
                        arenaState.StartedAt = DateTime.UtcNow;
                        arenaState.LastActivity = DateTime.UtcNow;

                        _log?.LogInfo($"[ArenaLifecycleManager] Arena started: {arenaId}");
                    }

                    return success;
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[ArenaLifecycleManager] Failed to process arena start: {ex.Message}");
                return false;
            }
        }

        public static bool OnArenaEnd(string arenaId)
        {
            try
            {
                if (string.IsNullOrEmpty(arenaId))
                {
                    _log?.LogWarning("[ArenaLifecycleManager] Invalid arena ID for end");
                    return false;
                }

                lock (_lock)
                {
                    if (!_arenaStates.ContainsKey(arenaId))
                    {
                        _log?.LogWarning($"[ArenaLifecycleManager] Arena {arenaId} does not exist");
                        return false;
                    }

                    var arenaState = _arenaStates[arenaId];
                    
                    // Notify all lifecycle services
                    var success = true;
                    foreach (var service in _lifecycleServices)
                    {
                        try
                        {
                            if (!service.OnArenaEnd(arenaId))
                            {
                                success = false;
                                _log?.LogWarning($"[ArenaLifecycleManager] Service {service.GetType().Name} failed arena end for {arenaId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _log?.LogError($"[ArenaLifecycleManager] Service {service.GetType().Name} error on arena end: {ex.Message}");
                            success = false;
                        }
                    }

                    if (success)
                    {
                        arenaState.IsActive = false;
                        arenaState.EndedAt = DateTime.UtcNow;
                        arenaState.LastActivity = DateTime.UtcNow;

                        _log?.LogInfo($"[ArenaLifecycleManager] Arena ended: {arenaId}");
                    }

                    return success;
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[ArenaLifecycleManager] Failed to process arena end: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Service Management
        public static void RegisterLifecycleService(IArenaLifecycleService service)
        {
            lock (_lock)
            {
                if (!_lifecycleServices.Contains(service))
                {
                    _lifecycleServices.Add(service);
                    _log?.LogInfo($"[ArenaLifecycleManager] Registered lifecycle service: {service.GetType().Name}");
                }
            }
        }

        public static void UnregisterLifecycleService(IArenaLifecycleService service)
        {
            lock (_lock)
            {
                if (_lifecycleServices.Remove(service))
                {
                    _log?.LogInfo($"[ArenaLifecycleManager] Unregistered lifecycle service: {service.GetType().Name}");
                }
            }
        }
        #endregion

        #region Query Methods
        public static List<string> GetActiveArenaIds()
        {
            lock (_lock)
            {
                return _arenaStates.Values.Where(s => s.IsActive).Select(s => s.ArenaId).ToList();
            }
        }

        public static ArenaLifecycleState GetArenaState(string arenaId)
        {
            lock (_lock)
            {
                return _arenaStates.TryGetValue(arenaId, out var state) ? state : null;
            }
        }

        public static List<PlayerLifecycleEvent> GetPlayerEvents(string arenaId, DateTime? since = null)
        {
            lock (_lock)
            {
                if (!_arenaStates.TryGetValue(arenaId, out var state))
                    return new List<PlayerLifecycleEvent>();

                var events = state.PlayerEvents;
                if (since.HasValue)
                {
                    events = events.Where(e => e.Timestamp >= since.Value).ToList();
                }
                
                return events;
            }
        }

        public static List<BuildingLifecycleEvent> GetBuildingEvents(string arenaId, DateTime? since = null)
        {
            lock (_lock)
            {
                if (!_arenaStates.TryGetValue(arenaId, out var state))
                    return new List<BuildingLifecycleEvent>();

                var events = state.BuildingEvents;
                if (since.HasValue)
                {
                    events = events.Where(e => e.Timestamp >= since.Value).ToList();
                }
                
                return events;
            }
        }

        public static int GetActivePlayerCount(string arenaId)
        {
            lock (_lock)
            {
                return _arenaStates.TryGetValue(arenaId, out var state) ? state.ActivePlayers.Count : 0;
            }
        }
        #endregion

        #region Helper Methods
        private static void CreateArenaState(string arenaId)
        {
            _arenaStates[arenaId] = new ArenaLifecycleState
            {
                ArenaId = arenaId,
                IsActive = false,
                ActivePlayers = new List<ulong>(),
                PlayerEvents = new List<PlayerLifecycleEvent>(),
                BuildingEvents = new List<BuildingLifecycleEvent>(),
                CreatedAt = DateTime.UtcNow
            };
        }

        private static float3 GetEntityPosition(Entity entity)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                if (em.HasComponent<Unity.Transforms.Translation>(entity))
                {
                    return em.GetComponentData<Unity.Transforms.Translation>(entity).Value;
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
                if (em.HasComponent<Unity.Transforms.Rotation>(entity))
                {
                    return em.GetComponentData<Unity.Transforms.Rotation>(entity).Value;
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
        public class ArenaLifecycleState
        {
            public string ArenaId { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? StartedAt { get; set; }
            public DateTime? EndedAt { get; set; }
            public DateTime LastActivity { get; set; }
            public List<ulong> ActivePlayers { get; set; } = new();
            public List<PlayerLifecycleEvent> PlayerEvents { get; set; } = new();
            public List<BuildingLifecycleEvent> BuildingEvents { get; set; } = new();
        }
        #endregion
    }
}