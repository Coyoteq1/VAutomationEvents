using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VAuto.Services.Systems;
using VAuto.Services.Lifecycle;
using VAuto.Services.Interfaces;
using VAuto.Services;
using BepInEx.Logging;

namespace VAuto.Services
{
    /// <summary>
    /// Central service manager that coordinates all services
    /// </summary>
    public static class ServiceManager
    {
        private static bool _initialized = false;
        private static readonly Dictionary<Type, VAuto.Services.Interfaces.IService> _services = new();
        private static readonly Dictionary<Type, List<Type>> _dependencies = new();
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
                    logger?.LogInfo("[ServiceManager] Initializing service manager...");
                    
                    // Define service dependencies
                    InitializeDependencies();
                    
                    // Initialize services in dependency order
                    InitializeServices();
                    
                    _initialized = true;
                    
                    logger?.LogInfo("[ServiceManager] Service manager initialized successfully");
                }
                catch (Exception ex)
                {
                    logger?.LogError($"[ServiceManager] Failed to initialize: {ex.Message}");
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
                    _log?.LogInfo("[ServiceManager] Cleaning up service manager...");
                    
                    // Cleanup services in reverse dependency order
                    CleanupServices();
                    
                    _services.Clear();
                    _dependencies.Clear();
                    _initialized = false;
                    
                    _log?.LogInfo("[ServiceManager] Service manager cleaned up successfully");
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[ServiceManager] Failed to cleanup: {ex.Message}");
                }
            }
        }

        private static void InitializeDependencies()
        {
            // Define the dependency graph
            _dependencies[typeof(AutoEnterService)] = new List<Type>
            {
                typeof(LifecycleService),
                typeof(ZoneService),
                typeof(PlayerService),
                typeof(AbilityOverrideService),
                typeof(ArenaBuildingService)
            };

            _dependencies[typeof(ArenaGlowService)] = new List<Type>
            {
                typeof(ZoneService)
            };

            _dependencies[typeof(ArenaBuildService)] = new List<Type>
            {
                typeof(ZoneService)
            };

            _dependencies[typeof(CastleObjectIntegrationService)] = new List<Type>
            {
                typeof(ZoneService)
            };

            _dependencies[typeof(DatabaseService)] = new List<Type>();

            _dependencies[typeof(EnhancedDataPersistenceService)] = new List<Type>
            {
                typeof(DatabaseService)
            };

            _dependencies[typeof(EnhancedArenaSnapshotService)] = new List<Type>
            {
                typeof(DatabaseService)
            };

            _dependencies[typeof(AutoComponentSaver)] = new List<Type>();

            _dependencies[typeof(ArenaDataSaver)] = new List<Type>
            {
                typeof(DatabaseService),
                typeof(ZoneService)
            };

            _dependencies[typeof(ArenaObjectService)] = new List<Type>
            {
                typeof(ZoneService)
            };

            // Core services have no dependencies
            _dependencies[typeof(PlayerService)] = new List<Type>();
            _dependencies[typeof(MapIconService)] = new List<Type>();
            _dependencies[typeof(GameSystems)] = new List<Type>();
            _dependencies[typeof(RespawnPreventionService)] = new List<Type>();
            _dependencies[typeof(NameTagService)] = new List<Type>();

            // _dependencies[typeof(ConveyorService)] = new List<Type>();

            _dependencies[typeof(EventService)] = new List<Type>();

            _dependencies[typeof(GlobalMapIconService)] = new List<Type>();

            // Gear services
            _dependencies[typeof(GearService)] = new List<Type>
            {
                typeof(PrefabResolverService),
                typeof(PlayerInventoryService) // Assuming we create a wrapper for IPlayerInventoryService
            };

            _dependencies[typeof(ZoneEntrySystem)] = new List<Type>
            {
                typeof(GearService)
            };

            _dependencies[typeof(GearSwapSystem)] = new List<Type>
            {
                typeof(GearService)
            };

            _dependencies[typeof(AutomationAPI)] = new List<Type>
            {
                typeof(LifecycleService),
                typeof(ZoneService),
                typeof(ArenaBuildService)
            };

            _log?.LogDebug("[ServiceManager] Service dependencies initialized");
        }

        private static void InitializeServices()
        {
            var initializationOrder = GetInitializationOrder();
            
            _log?.LogInfo($"[ServiceManager] Initializing {initializationOrder.Count} services in dependency order");
            
            foreach (var serviceType in initializationOrder)
            {
                try
                {
                    var service = CreateService(serviceType);
                    if (service != null)
                    {
                        var startTime = DateTime.UtcNow;
                        service.Initialize();
                        var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                        
                        _services[serviceType] = service;
                        _log?.LogInfo($"[ServiceManager] ✓ Initialized service: {serviceType.Name} ({duration:F2}ms)");
                    }
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[ServiceManager] ✗ Failed to initialize service {serviceType.Name}: {ex.Message}");
                    throw;
                }
            }
            
            _log?.LogInfo($"[ServiceManager] Successfully initialized all {initializationOrder.Count} services");
        }

        /// <summary>
        /// Register a service manually (VAMP pattern)
        /// </summary>
        public static void RegisterService<T>(T service) where T : VAuto.Services.Interfaces.IService
        {
            lock (_lock)
            {
                var type = typeof(T);
                if (_services.ContainsKey(type))
                {
                    _log?.LogWarning($"[ServiceManager] Service {type.Name} already registered. Overwriting.");
                }
                _services[type] = service;
                _log?.LogInfo($"[ServiceManager] Manually registered service: {type.Name}");
            }
        }

        private static List<Type> GetInitializationOrder()
        {
            var ordered = new List<Type>();
            var visited = new HashSet<Type>();
            var visiting = new HashSet<Type>();

            foreach (var serviceType in _dependencies.Keys)
            {
                if (!visited.Contains(serviceType))
                {
                    VisitService(serviceType, visited, visiting, ordered);
                }
            }

            return ordered;
        }

        private static void VisitService(Type serviceType, HashSet<Type> visited, HashSet<Type> visiting, List<Type> ordered)
        {
            if (visited.Contains(serviceType)) return;
            if (visiting.Contains(serviceType))
            {
                throw new InvalidOperationException($"Circular dependency detected involving {serviceType.Name}");
            }

            visiting.Add(serviceType);

            if (_dependencies.TryGetValue(serviceType, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    if (!visited.Contains(dependency))
                    {
                        VisitService(dependency, visited, visiting, ordered);
                    }
                }
            }

            visiting.Remove(serviceType);
            visited.Add(serviceType);
            ordered.Add(serviceType);
        }

        private static void CleanupServices()
        {
            var initializationOrder = GetInitializationOrder();
            for (var i = initializationOrder.Count - 1; i >= 0; i--)
            {
                var serviceType = initializationOrder[i];
                try
                {
                    if (_services.TryGetValue(serviceType, out var service))
                    {
                        service.Cleanup();
                        _log?.LogInfo($"[ServiceManager] Cleaned up service: {serviceType.Name}");
                    }
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[ServiceManager] Failed to cleanup service {serviceType.Name}: {ex.Message}");
                }
            }
        }

        private static VAuto.Services.Interfaces.IService CreateService(Type serviceType)
        {
            // Create service instances based on type
            if (serviceType == typeof(AutoEnterService))
                return new AutoEnterServiceWrapper();
            else if (serviceType == typeof(ArenaGlowService))
                return new ArenaGlowServiceWrapper();
            else if (serviceType == typeof(ArenaBuildService))
                return new ArenaBuildServiceWrapper();
            else if (serviceType == typeof(CastleObjectIntegrationService))
                return new CastleObjectIntegrationServiceWrapper();
            else if (serviceType == typeof(DatabaseService))
                return new DatabaseServiceWrapper();
            else if (serviceType == typeof(EnhancedDataPersistenceService))
                return new EnhancedDataPersistenceServiceWrapper();
            else if (serviceType == typeof(EnhancedArenaSnapshotService))
                return new EnhancedArenaSnapshotServiceWrapper();
            else if (serviceType == typeof(AutoComponentSaver))
                return new AutoComponentSaverWrapper();
            else if (serviceType == typeof(ArenaDataSaver))
                return new ArenaDataSaverWrapper();
            else if (serviceType == typeof(ArenaObjectService))
                return new ArenaObjectServiceWrapper();
            else if (serviceType == typeof(PlayerService))
                return new PlayerServiceWrapper();
            else if (serviceType == typeof(MapIconService))
                return new MapIconServiceWrapper();
            else if (serviceType == typeof(LocalizationService))
                return new LocalizationServiceWrapper();
            else if (serviceType == typeof(GameSystems))
                return new GameSystemsWrapper();
            else if (serviceType == typeof(RespawnPreventionService))
                return new RespawnPreventionServiceWrapper();
            else if (serviceType == typeof(NameTagService))
                return new NameTagServiceWrapper();
            else if (serviceType == typeof(LifecycleService))
                return new LifecycleService();
            else if (serviceType == typeof(AbilityOverrideService))
                return new AbilityOverrideServiceWrapper();
            else if (serviceType == typeof(ArenaBuildingService))
                return new ArenaBuildingServiceWrapper();
            else if (serviceType == typeof(ZoneService))
                return new ZoneServiceWrapper();
            else if (serviceType == typeof(EventService))
                return new EventServiceWrapper();
            else if (serviceType == typeof(GlobalMapIconService))
                return new GlobalMapIconServiceWrapper();
            else if (serviceType == typeof(AutomationAPI))
                return AutomationAPI.Instance;
            else if (serviceType == typeof(GearService))
                return new GearServiceWrapper();
            else if (serviceType == typeof(ZoneEntrySystem))
                return new ZoneEntrySystemWrapper();
            else if (serviceType == typeof(GearSwapSystem))
                return new GearSwapSystemWrapper();
            else if (serviceType == typeof(PlayerInventoryService))
                return new PlayerInventoryServiceWrapper();

            Plugin.Log?.LogInfo($"[ServiceManager] Creating service: {serviceType.Name} - {serviceType.FullName}");
            throw new NotSupportedException($"Service type {serviceType.Name} is not supported");
        }
        #endregion

        #region Service Access
        public static T GetService<T>() where T : VAuto.Services.Interfaces.IService
        {
            lock (_lock)
            {
                var serviceType = typeof(T);
                if (_services.TryGetValue(serviceType, out var service))
                {
                    return (T)service;
                }
                
                _log?.LogWarning($"[ServiceManager] Service {serviceType.Name} not found");
                return default;
            }
        }

        public static VAuto.Services.Interfaces.IService GetService(Type serviceType)
        {
            lock (_lock)
            {
                if (_services.TryGetValue(serviceType, out var service))
                {
                    return service;
                }
                
                _log?.LogWarning($"[ServiceManager] Service {serviceType.Name} not found");
                return null;
            }
        }

        public static bool HasService(Type serviceType)
        {
            lock (_lock)
            {
                return _services.ContainsKey(serviceType);
            }
        }

        public static List<Type> GetAllServiceTypes()
        {
            lock (_lock)
            {
                return _services.Keys.ToList();
            }
        }

        /// <summary>
        /// VAMP-style service health check
        /// </summary>
        public static Dictionary<string, object> GetAllServiceHealth()
        {
            var healthStatus = new Dictionary<string, object>();
            
            lock (_lock)
            {
                foreach (var kvp in _services)
                {
                    var serviceName = kvp.Key.Name;
                    var service = kvp.Value;
                    
                    if (service is VAuto.Services.Interfaces.IServiceHealthMonitor healthMonitor)
                    {
                        var health = healthMonitor.GetHealthStatus();
                        var metrics = healthMonitor.GetPerformanceMetrics();
                        
                        healthStatus[serviceName] = new
                        {
                            Status = health.Status,
                            IsHealthy = health.IsHealthy,
                            LastCheck = health.LastCheck,
                            ErrorCount = healthMonitor.GetErrorCount(),
                            LastError = healthMonitor.GetLastError(),
                            Performance = new
                            {
                                ActiveOperations = metrics.ActiveOperations,
                                MeasuredAt = metrics.MeasuredAt
                            }
                        };
                    }
                    else
                    {
                        healthStatus[serviceName] = new
                        {
                            Status = service.IsInitialized ? "Running" : "Stopped",
                            IsHealthy = service.IsInitialized,
                            HasHealthMonitoring = false
                        };
                    }
                }
            }
            
            return healthStatus;
        }

        /// <summary>
        /// VAMP-style service warmup phase
        /// </summary>
        public static async Task WarmupServicesAsync(CancellationToken cancellationToken = default)
        {
            _log?.LogInfo("[ServiceManager] Starting VAMP-style service warmup phase...");
            
            var warmupTasks = new List<Task>();
            
            lock (_lock)
            {
                foreach (var service in _services.Values)
                {
                    // Note: IAsyncWarmupService interface not available in this build; skipping warmup
                }
            }
            
            if (warmupTasks.Count > 0)
            {
                await Task.WhenAll(warmupTasks);
                _log?.LogInfo($"[ServiceManager] Completed warmup for {warmupTasks.Count} services");
            }
        }

        public static int GetServiceCount()
        {
            lock (_lock)
            {
                return _services.Count;
            }
        }
        #endregion

        #region Health Monitoring
        public static Dictionary<string, VAuto.Services.Interfaces.IServiceHealthMonitor> GetServiceHealth()
        {
            var health = new Dictionary<string, VAuto.Services.Interfaces.IServiceHealthMonitor>();
            
            lock (_lock)
            {
                foreach (var kvp in _services)
                {
                    if (kvp.Value is VAuto.Services.Interfaces.IServiceHealthMonitor healthMonitor)
                    {
                        health[kvp.Key.Name] = healthMonitor;
                    }
                }
            }
            
            return health;
        }

        public static void LogServiceStatus()
        {
            _log?.LogInfo("=== Service Manager Status ===");
            
            lock (_lock)
            {
                foreach (var service in _services)
                {
                    var status = service.Value.IsInitialized ? "Initialized" : "Not Initialized";
                    _log?.LogInfo($"• {service.Key.Name}: {status}");
                }
            }
        }
        #endregion

        #region Service Wrappers
        // Wrapper classes to implement VAuto.Services.Interfaces.IService interface for existing services
        
        private class AutoEnterServiceWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => AutoEnterService.Instance.IsInitialized;
            public ManualLogSource Log => AutoEnterService.Instance.Log;
            
            public void Initialize() => AutoEnterService.Instance.Initialize();
            public void Cleanup() => AutoEnterService.Instance.Cleanup();
            
            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "AutoEnterService",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }
            
            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "AutoEnterService",
                    ActiveOperations = AutoEnterService.Instance.GetAutoEnterEnabledCount(),
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class ArenaGlowServiceWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => ArenaGlowService.Instance.IsInitialized;
            public ManualLogSource Log => ArenaGlowService.Instance.Log;
            
            public void Initialize() => ArenaGlowService.Instance.Initialize();
            public void Cleanup() => ArenaGlowService.Instance.Cleanup();
            
            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "ArenaGlowService",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }
            
            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "ArenaGlowService",
                    ActiveOperations = ArenaGlowService.GetActiveGlowCount(),
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class ArenaBuildServiceWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => ArenaBuildService.Instance.IsInitialized;
            public ManualLogSource Log => ArenaBuildService.Instance.Log;
            
            public void Initialize() => ArenaBuildService.Instance.Initialize();
            public void Cleanup() => ArenaBuildService.Instance.Cleanup();
            
            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "ArenaBuildService",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }
            
            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "ArenaBuildService",
                    ActiveOperations = ArenaBuildService.Instance.GetTotalBuiltStructures(),
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class CastleObjectIntegrationServiceWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => CastleObjectIntegrationService.IsInitialized;
            public ManualLogSource Log => CastleObjectIntegrationService.Log;
            
            public void Initialize() => CastleObjectIntegrationService.Initialize();
            public void Cleanup() => CastleObjectIntegrationService.Cleanup();
            
            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "CastleObjectIntegrationService",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }
            
            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "CastleObjectIntegrationService",
                    ActiveOperations = CastleObjectIntegrationService.GetTotalCastleObjects(),
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class DatabaseServiceWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => DatabaseService.IsInitialized;
            public ManualLogSource Log => DatabaseService.Log;
            
            public void Initialize() => DatabaseService.Initialize();
            public void Cleanup() => DatabaseService.Cleanup();
            
            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "DatabaseService",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }
            
            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "DatabaseService",
                    ActiveOperations = DatabaseService.GetArenaCount() + DatabaseService.GetPlayerCount(),
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class EnhancedDataPersistenceServiceWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => EnhancedDataPersistenceService.IsInitialized;
            public ManualLogSource Log => EnhancedDataPersistenceService.Log;
            
            public void Initialize() => EnhancedDataPersistenceService.Initialize();
            public void Cleanup() => EnhancedDataPersistenceService.Cleanup();
            
            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "EnhancedDataPersistenceService",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }
            
            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "EnhancedDataPersistenceService",
                    ActiveOperations = EnhancedDataPersistenceService.GetPendingSaves().Count,
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class EnhancedArenaSnapshotServiceWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => EnhancedArenaSnapshotService.IsInitialized;
            public ManualLogSource Log => EnhancedArenaSnapshotService.Log;
            
            public void Initialize() => EnhancedArenaSnapshotService.Initialize();
            public void Cleanup() => EnhancedArenaSnapshotService.Cleanup();
            
            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "EnhancedArenaSnapshotService",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }
            
            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "EnhancedArenaSnapshotService",
                    ActiveOperations = EnhancedArenaSnapshotService.GetSnapshotCount(),
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class AutoComponentSaverWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => AutoComponentSaver.IsInitialized;
            public ManualLogSource Log => AutoComponentSaver.Log;
            
            public void Initialize() => AutoComponentSaver.Initialize();
            public void Cleanup() => AutoComponentSaver.Cleanup();
            
            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "AutoComponentSaver",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }
            
            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "AutoComponentSaver",
                    ActiveOperations = AutoComponentSaver.GetSavedComponentCount(),
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class ArenaDataSaverWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => ArenaDataSaver.IsInitialized;
            public ManualLogSource Log => ArenaDataSaver.Log;
            
            public void Initialize() => ArenaDataSaver.Initialize();
            public void Cleanup() => ArenaDataSaver.Cleanup();
            
            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "ArenaDataSaver",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }
            
            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "ArenaDataSaver",
                    ActiveOperations = ArenaDataSaver.GetArenaCount(),
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class ArenaObjectServiceWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => ArenaObjectService.Instance.IsInitialized;
            public ManualLogSource Log => ArenaObjectService.Instance.Log;
            
            public void Initialize() => ArenaObjectService.Instance.Initialize();
            public void Cleanup() => ArenaObjectService.Instance.Cleanup();
            
            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "ArenaObjectService",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }
            
            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "ArenaObjectService",
                    ActiveOperations = ArenaObjectService.Instance.GetTotalObjectCount(),
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class LifecycleServiceWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            private LifecycleService _service;
            public bool IsInitialized => _service?.IsInitialized ?? false;
            public ManualLogSource Log => _service?.Log;

            public void Initialize()
            {
                _service = new LifecycleService();
                _service.Initialize();
            }

            public void Cleanup() => _service?.Cleanup();

            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "LifecycleService",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }

            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "LifecycleService",
                    MeasuredAt = DateTime.UtcNow
                };
            }

            public int GetErrorCount() => 0;
            public string GetLastError() => null;

            public LifecycleService Service => _service;
        }

        // Wrapper classes for existing services from UnifiedServices.cs and MissingServices.cs
        private class PlayerServiceWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => PlayerService.IsInitialized;
            public ManualLogSource Log => PlayerService.Log;
            
            public void Initialize() => PlayerService.Initialize();
            public void Cleanup() => PlayerService.Cleanup();
            
            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "PlayerService",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }
            
            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "PlayerService",
                    ActiveOperations = 0, // PlayerService.GetOnlinePlayerCount(),
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class MapIconServiceWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => MapIconService.IsInitialized;
            public ManualLogSource Log => MapIconService.Log;

            public void Initialize() => MapIconService.Initialize();
            public void Cleanup() => MapIconService.Cleanup();

            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "MapIconService",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }

            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "MapIconService",
                    ActiveOperations = MapIconService.GetActiveIconCount(),
                    MeasuredAt = DateTime.UtcNow
                };
            }

            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class LocalizationServiceWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => LocalizationService.IsInitialized;
            public ManualLogSource Log => LocalizationService.Log;

            public void Initialize() => LocalizationService.Initialize();
            public void Cleanup() => LocalizationService.Cleanup();

            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "LocalizationService",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }

            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "LocalizationService",
                    ActiveOperations = LocalizationService.GetLocalizationCount(),
                    MeasuredAt = DateTime.UtcNow
                };
            }

            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class GameSystemsWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => GameSystems.IsInitialized;
            public ManualLogSource Log => GameSystems.Log;
            
            public void Initialize() => GameSystems.Initialize();
            public void Cleanup() => GameSystems.Cleanup();
            
            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "GameSystems",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }
            
            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "GameSystems",
                    ActiveOperations = GameSystems.GetActiveHookedPlayers().Count,
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class RespawnPreventionServiceWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => RespawnPreventionService.IsInitialized;
            public ManualLogSource Log => RespawnPreventionService.Log;
            
            public void Initialize() => RespawnPreventionService.Initialize();
            public void Cleanup() => RespawnPreventionService.Cleanup();
            
            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "RespawnPreventionService",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }
            
            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "RespawnPreventionService",
                    ActiveOperations = 0, // RespawnPreventionService.GetActiveCooldownCount(),
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class NameTagServiceWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => NameTagService.IsInitialized;
            public ManualLogSource Log => NameTagService.Log;
            
            public void Initialize() => NameTagService.Initialize();
            public void Cleanup() => NameTagService.Cleanup();
            
            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "NameTagService",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }
            
            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "NameTagService",
                    ActiveOperations = NameTagService.GetActiveTagCount(),
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }


        private class AbilityOverrideServiceWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => AbilityOverrideService.Instance.IsInitialized;
            public ManualLogSource Log => AbilityOverrideService.Instance.Log;
            
            public void Initialize() => AbilityOverrideService.Instance.Initialize();
            public void Cleanup() => AbilityOverrideService.Instance.Cleanup();
            
            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "AbilityOverrideService",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }
            
            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "AbilityOverrideService",
                    ActiveOperations = 0,
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class ArenaBuildingServiceWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            private ArenaBuildingService _service;
            
            public bool IsInitialized => _service?.IsInitialized ?? false;
            public ManualLogSource Log => _service?.Log;
            
            public void Initialize()
            {
                _service = new ArenaBuildingService();
                _service.Initialize();
            }
            
            public void Cleanup() => _service?.Cleanup();
            
            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "ArenaBuildingService",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }
            
            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "ArenaBuildingService",
                    ActiveOperations = 0,
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class ZoneServiceWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => true; // Always considered initialized

            public ManualLogSource Log => Plugin.Logger;

            public void Initialize() { }
            public void Cleanup() { }

            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "ZoneService",
                    IsHealthy = true,
                    Status = "Running",
                    LastCheck = DateTime.UtcNow
                };
            }

            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "ZoneService",
                    ActiveOperations = 0,
                    MeasuredAt = DateTime.UtcNow
                };
            }

            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        /*
        private class ConveyorServiceWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => ConveyorService.Instance.IsInitialized;
            public ManualLogSource Log => ConveyorService.Instance.Log;

            public void Initialize() => ConveyorService.Instance.Initialize();
            public void Cleanup() => ConveyorService.Instance.Cleanup();

            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "ConveyorService",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }

            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "ConveyorService",
                    ActiveOperations = 0, // ConveyorService metrics not available in this build
                    MeasuredAt = DateTime.UtcNow
                };
            }

            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }
        */



        private class EventServiceWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => EventService.Instance.IsInitialized;
            public ManualLogSource Log => EventService.Instance.Log;
            public void Initialize() => EventService.Instance.Initialize();
            public void Cleanup() => EventService.Instance.Cleanup();
            public ServiceHealthStatus GetHealthStatus() => EventService.Instance.GetHealthStatus();
            public ServicePerformanceMetrics GetPerformanceMetrics() => EventService.Instance.GetPerformanceMetrics();
            public int GetErrorCount() => EventService.Instance.GetErrorCount();
            public string GetLastError() => EventService.Instance.GetLastError();
        }

        private class GlobalMapIconServiceWrapper : VAuto.Services.Interfaces.IService, VAuto.Services.Interfaces.IServiceHealthMonitor
        {
            public bool IsInitialized => GlobalMapIconService.IsInitialized;
            public ManualLogSource Log => GlobalMapIconService.Log;

            public void Initialize() => GlobalMapIconService.Initialize();
            public void Cleanup() => GlobalMapIconService.Cleanup();

            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "GlobalMapIconService",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }

            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "GlobalMapIconService",
                    ActiveOperations = GlobalMapIconService.GetActiveIconCount(),
                    MeasuredAt = DateTime.UtcNow
                };
            }

            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class PlayerInventoryServiceWrapper : VAuto.Services.Interfaces.IService, IPlayerInventoryService
        {
            private PlayerInventoryService _service;
            public bool IsInitialized => _service != null;
            public ManualLogSource Log => Plugin.Logger;

            public void Initialize()
            {
                // Simple logger using Plugin.Logger
                _service = new PlayerInventoryService(null); // TODO: proper logger
            }

            public void Cleanup() => _service = null;

            // IPlayerInventoryService implementation
            public bool EquipItem(string playerId, string itemName, int count) => _service?.EquipItem(playerId, itemName, count) ?? false;
            public IEnumerable<EquippedItem> GetEquippedItems(string playerId) => _service?.GetEquippedItems(playerId) ?? new List<EquippedItem>();
            public void ClearEquippedItems(string playerId) => _service?.ClearEquippedItems(playerId);
        }

        private class GearServiceWrapper : VAuto.Services.Interfaces.IService, IGearService
        {
            private GearService _service;
            public bool IsInitialized => _service != null;
            public ManualLogSource Log => Plugin.Logger;

            public void Initialize()
            {
                var prefabResolver = ServiceManager.GetService<PrefabResolverService>() as IPrefabResolverService;
                var inventoryService = ServiceManager.GetService<PlayerInventoryService>() as IPlayerInventoryService;
                _service = new GearService(prefabResolver, inventoryService, null); // TODO: proper logger
            }

            public void Cleanup() => _service = null;

            // IGearService implementation
            public bool IsAutoEquipEnabledForPlayer(string playerId) => _service?.IsAutoEquipEnabledForPlayer(playerId) ?? false;
            public void SetAutoEquipForPlayer(string playerId, bool enabled) => _service?.SetAutoEquipForPlayer(playerId, enabled);
            public bool EquipGearForPlayer(string playerId, IEnumerable<VAuto.Data.GearEntry> gearList) => _service?.EquipGearForPlayer(playerId, gearList) ?? false;
            public bool StartSwapSession(string playerId, string sessionId, IEnumerable<VAuto.Data.GearEntry> swapGear) => _service?.StartSwapSession(playerId, sessionId, swapGear) ?? false;
            public bool EndSwapSession(string playerId, string sessionId) => _service?.EndSwapSession(playerId, sessionId) ?? false;
            public void RevertPlayerGear(string playerId) => _service?.RevertPlayerGear(playerId);
            public IEnumerable<string> GetActiveSwapSessions(string playerId) => _service?.GetActiveSwapSessions(playerId) ?? Enumerable.Empty<string>();
        }

        private class ZoneEntrySystemWrapper : VAuto.Services.Interfaces.IService
        {
            private ZoneEntrySystem _service;
            public bool IsInitialized => _service != null;
            public ManualLogSource Log => Plugin.Logger;

            public void Initialize()
            {
                var gearService = ServiceManager.GetService<GearService>() as IGearService;
                _service = new ZoneEntrySystem(gearService, null); // TODO: proper logger
            }

            public void Cleanup() => _service = null;
        }

        private class GearSwapSystemWrapper : VAuto.Services.Interfaces.IService
        {
            private GearSwapSystem _service;
            public bool IsInitialized => _service != null;
            public ManualLogSource Log => Plugin.Logger;

            public void Initialize()
            {
                var gearService = ServiceManager.GetService<GearService>() as IGearService;
                _service = new GearSwapSystem(gearService, null); // TODO: proper logger
            }

            public void Cleanup() => _service = null;
        }

        #endregion
    }
}
