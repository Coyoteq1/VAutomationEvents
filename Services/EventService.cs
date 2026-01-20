using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using VAuto.Services.Interfaces;
using BepInEx.Logging;

namespace VAuto.Services
{
    /// <summary>
    /// Centralized event handling service inspired by VAMP API.
    /// Allows decoupled communication between services.
    /// </summary>
    public class EventService : IService, IServiceHealthMonitor
    {
        private static EventService _instance;
        public static EventService Instance => _instance ??= new EventService();

        private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
        private bool _isInitialized;
        private ManualLogSource _log;

        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => _log;

        public void Initialize()
        {
            if (_isInitialized) return;
            _log = ServiceManager.Log;
            _isInitialized = true;
            _log?.LogInfo("[EventService] Initialized");
        }

        public void Cleanup()
        {
            _subscribers.Clear();
            _isInitialized = false;
        }

        /// <summary>
        /// Subscribe to a specific event type
        /// </summary>
        public void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type))
            {
                _subscribers[type] = new List<Delegate>();
            }
            _subscribers[type].Add(handler);
        }

        /// <summary>
        /// Unsubscribe from a specific event type
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var handlers))
            {
                handlers.Remove(handler);
            }
        }

        /// <summary>
        /// Trigger an event and notify all subscribers
        /// </summary>
        public void Trigger<T>(T eventData)
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        ((Action<T>)handler)(eventData);
                    }
                    catch (Exception ex)
                    {
                        _log?.LogError($"[EventService] Error in event handler for {type.Name}: {ex.Message}");
                    }
                }
            }
        }

        public ServiceHealthStatus GetHealthStatus()
        {
            return new ServiceHealthStatus
            {
                ServiceName = "EventService",
                IsHealthy = _isInitialized,
                Status = _isInitialized ? "Active" : "Inactive",
                LastCheck = DateTime.UtcNow
            };
        }

        public ServicePerformanceMetrics GetPerformanceMetrics()
        {
            return new ServicePerformanceMetrics
            {
                ServiceName = "EventService",
                ActiveOperations = _subscribers.Count,
                MeasuredAt = DateTime.UtcNow
            };
        }

        public int GetErrorCount() => 0;
        public string GetLastError() => null;
    }

    // Event Types
    public class PlayerJoinEvent { public ulong PlatformId; public string Name; }
    public class ArenaCreatedEvent { public string ArenaId; }
    public class PlayerArenaEnterEvent { public ulong PlatformId; }
    public class PlayerArenaExitEvent { public ulong PlatformId; }
    public class ServerStartupEvent { }
}