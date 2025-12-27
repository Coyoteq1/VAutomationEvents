using System;
using System.Collections.Generic;
using System.Linq;
using VAuto.Services.Interfaces;
using VAuto.Services.Systems;
using BepInEx.Logging;

namespace VAuto.Services
{
    /// <summary>
    /// Central service manager that coordinates all services
    /// </summary>
    public static class ServiceManager
    {
        private static bool _initialized = false;
        private static readonly Dictionary<Type, IService> _services = new();
        private static readonly Dictionary<Type, List<Type>> _dependencies = new();
        private static readonly object _lock = new object();
        private static ManualLogSource _log;
        
        public static bool IsInitialized => _initialized;
        public static ManualLogSource Log => _log;

        #region Initialization
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized) return;
                
                try
                {
                    _log = Plugin.Logger;
                    Plugin.Logger?.LogInfo("[ServiceManager] Initializing service manager...");
                    
                    // Define service dependencies
                    InitializeDependencies();
                    
                    // Initialize services in dependency order
                    InitializeServices();
                    
                    _initialized = true;
                    
                    Plugin.Logger?.LogInfo("[ServiceManager] Service manager initialized successfully");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[ServiceManager] Failed to initialize: {ex.Message}");
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
                    Plugin.Logger?.LogInfo("[ServiceManager] Cleaning up service manager...");
                    
                    // Cleanup services in reverse dependency order
                    CleanupServices();
                    
                    _services.Clear();
                    _dependencies.Clear();
                    _initialized = false;
                    
                    Plugin.Logger?.LogInfo("[ServiceManager] Service manager cleaned up successfully");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[ServiceManager] Failed to cleanup: {ex.Message}");
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
                typeof(PlayerService)
            };

            _dependencies[typeof(ArenaGlowService)] = new List<Type>
            {
                typeof(ZoneService)
            };

            _dependencies[typeof(ArenaBuildService)] = new List<Type>
            {
                typeof(ZoneService),
                typeof(PlayerService)
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
                typeof(DatabaseService),
                typeof(PlayerService)
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

            _dependencies[typeof(GameSystems)] = new List<Type>();
            _dependencies[typeof(RespawnPreventionService)] = new List<Type>();
            _dependencies[typeof(NameTagService)] = new List<Type>();

            _dependencies[typeof(ConveyorService)] = new List<Type>();

            _dependencies[typeof(AchievementUnlockService)] = new List<Type>();

            _dependencies[typeof(AutoEnterService)] = new List<Type>
            {
                typeof(LifecycleService)
            };

            _log?.LogDebug("[ServiceManager] Service dependencies initialized");
        }

        private static void InitializeServices()
        {
            var initializationOrder = GetInitializationOrder();
            
            foreach (var serviceType in initializationOrder)
            {
                try
                {
                    var service = CreateService(serviceType);
                    if (service != null)
                    {
                        service.Initialize();
                        _services[serviceType] = service;
                        _log?.LogInfo($"[ServiceManager] Initialized service: {serviceType.Name}");
                    }
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[ServiceManager] Failed to initialize service {serviceType.Name}: {ex.Message}");
                    throw;
                }
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
            var cleanupOrder = GetInitializationOrder().Reverse();
            
            foreach (var serviceType in cleanupOrder)
            {
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

        private static IService CreateService(Type serviceType)
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

            else if (serviceType == typeof(GameSystems))
                return new GameSystemsWrapper();
            else if (serviceType == typeof(RespawnPreventionService))
                return new RespawnPreventionServiceWrapper();
            else if (serviceType == typeof(NameTagService))
                return new NameTagServiceWrapper();
            else if (serviceType == typeof(LifecycleService))
                return new LifecycleServiceWrapper();
            else if (serviceType == typeof(ZoneService))
                return new ZoneServiceWrapper();
            else if (serviceType == typeof(ConveyorService))
                return new ConveyorServiceWrapper();
            else if (serviceType == typeof(AchievementUnlockService))
                return new AchievementUnlockServiceWrapper();

            throw new NotSupportedException($"Service type {serviceType.Name} is not supported");
        }
        #endregion

        #region Service Access
        public static T GetService<T>() where T : IService
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

        public static IService GetService(Type serviceType)
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

        public static int GetServiceCount()
        {
            lock (_lock)
            {
                return _services.Count;
            }
        }
        #endregion

        #region Health Monitoring
        public static Dictionary<string, IServiceHealthMonitor> GetServiceHealth()
        {
            var health = new Dictionary<string, IServiceHealthMonitor>();
            
            lock (_lock)
            {
                foreach (var kvp in _services)
                {
                    if (kvp.Value is IServiceHealthMonitor healthMonitor)
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
        // Wrapper classes to implement IService interface for existing services
        
        private class AutoEnterServiceWrapper : IService, IServiceHealthMonitor
        {
            public bool IsInitialized => AutoEnterService.IsInitialized;
            public ManualLogSource Log => AutoEnterService.Log;
            
            public void Initialize() => AutoEnterService.Initialize();
            public void Cleanup() => AutoEnterService.Cleanup();
            
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
                    ActiveOperations = AutoEnterService.GetAutoEnterEnabledCount(),
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class ArenaGlowServiceWrapper : IService, IServiceHealthMonitor
        {
            public bool IsInitialized => ArenaGlowService.IsInitialized;
            public ManualLogSource Log => ArenaGlowService.Log;
            
            public void Initialize() => ArenaGlowService.Initialize();
            public void Cleanup() => ArenaGlowService.Cleanup();
            
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

        private class ArenaBuildServiceWrapper : IService, IServiceHealthMonitor
        {
            public bool IsInitialized => ArenaBuildService.IsInitialized;
            public ManualLogSource Log => ArenaBuildService.Log;
            
            public void Initialize() => ArenaBuildService.Initialize();
            public void Cleanup() => ArenaBuildService.Cleanup();
            
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
                    ActiveOperations = ArenaBuildService.GetTotalBuiltStructures(),
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class CastleObjectIntegrationServiceWrapper : IService, IServiceHealthMonitor
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

        private class DatabaseServiceWrapper : IService, IServiceHealthMonitor
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

        private class EnhancedDataPersistenceServiceWrapper : IService, IServiceHealthMonitor
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

        private class EnhancedArenaSnapshotServiceWrapper : IService, IServiceHealthMonitor
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

        private class AutoComponentSaverWrapper : IService, IServiceHealthMonitor
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

        private class ArenaDataSaverWrapper : IService, IServiceHealthMonitor
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

        private class ArenaObjectServiceWrapper : IService, IServiceHealthMonitor
        {
            public bool IsInitialized => ArenaObjectService.IsInitialized;
            public ManualLogSource Log => ArenaObjectService.Log;
            
            public void Initialize() => ArenaObjectService.Initialize();
            public void Cleanup() => ArenaObjectService.Cleanup();
            
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
                    ActiveOperations = ArenaObjectService.GetTotalObjectCount(),
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        // Wrapper classes for existing services from UnifiedServices.cs and MissingServices.cs
        private class PlayerServiceWrapper : IService, IServiceHealthMonitor
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
                    ActiveOperations = PlayerService.GetOnlinePlayerCount(),
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }



        private class GameSystemsWrapper : IService, IServiceHealthMonitor
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

        private class RespawnPreventionServiceWrapper : IService, IServiceHealthMonitor
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
                    ActiveOperations = RespawnPreventionService.GetActiveCooldownCount(),
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class NameTagServiceWrapper : IService, IServiceHealthMonitor
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

        private class LifecycleServiceWrapper : IService, IServiceHealthMonitor
        {
            public bool IsInitialized => true; // Always considered initialized
            
            public ManualLogSource Log => Plugin.Logger;
            
            public void Initialize() { }
            public void Cleanup() { }
            
            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "LifecycleService",
                    IsHealthy = true,
                    Status = "Running",
                    LastCheck = DateTime.UtcNow
                };
            }
            
            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                return new ServicePerformanceMetrics
                {
                    ServiceName = "LifecycleService",
                    ActiveOperations = 0,
                    MeasuredAt = DateTime.UtcNow
                };
            }
            
            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class ZoneServiceWrapper : IService, IServiceHealthMonitor
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

        private class ConveyorServiceWrapper : IService, IServiceHealthMonitor
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
                    ActiveOperations = ConveyorService.Instance._territoryConfigs.Count(c => c.Value.IsEnabled),
                    MeasuredAt = DateTime.UtcNow
                };
            }

            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }

        private class AchievementUnlockServiceWrapper : IService, IServiceHealthMonitor
        {
            public bool IsInitialized => AchievementUnlockService._isInitialized;
            public ManualLogSource Log => Plugin.Logger;

            public void Initialize() => AchievementUnlockService.Initialize();
            public void Cleanup() { }

            public ServiceHealthStatus GetHealthStatus()
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "AchievementUnlockService",
                    IsHealthy = IsInitialized,
                    Status = IsInitialized ? "Running" : "Stopped",
                    LastCheck = DateTime.UtcNow
                };
            }

            public ServicePerformanceMetrics GetPerformanceMetrics()
            {
                var stats = AchievementUnlockService.GetAchievementStatistics();
                return new ServicePerformanceMetrics
                {
                    ServiceName = "AchievementUnlockService",
                    ActiveOperations = (int)stats["Players_Currently_Unlocked"],
                    MeasuredAt = DateTime.UtcNow
                };
            }

            public int GetErrorCount() => 0;
            public string GetLastError() => null;
        }
        #endregion
    }
}
