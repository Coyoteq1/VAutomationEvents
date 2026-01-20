using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using VAuto.Services.Interfaces;
using VAuto.Data;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Automation Execution Engine - orchestrates all automation operations
    /// Handles .testplan and .runplan commands with full lifecycle compliance
    /// </summary>
    public class AutomationExecutionEngine : IService, IServiceHealthMonitor
    {
        private static AutomationExecutionEngine _instance;
        public static AutomationExecutionEngine Instance => _instance ??= new AutomationExecutionEngine();

        private bool _isInitialized;
        private ManualLogSource _log;

        // Dependencies
        private AutomationGateService _gateService;
        private PlanValidatorService _validatorService;
        private PrefabResolverService _prefabResolver;
        private BossRewardBindingService _rewardService;

        // Active contexts
        private readonly Dictionary<string, AutomationContext> _activeContexts = new();

        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => _log;

        public void Initialize()
        {
            if (_isInitialized) return;
            _log = ServiceManager.Log;

            // Initialize dependencies
            _gateService = AutomationGateService.Instance;
            _validatorService = PlanValidatorService.Instance;
            _prefabResolver = PrefabResolverService.Instance;
            _rewardService = BossRewardBindingService.Instance;

            _gateService.Initialize();
            _validatorService.Initialize();
            _prefabResolver.Initialize();
            _rewardService.Initialize();

            _isInitialized = true;
            _log?.LogInfo("[AutomationExecutionEngine] Initialized");
        }

        public void Cleanup()
        {
            _activeContexts.Clear();
            _isInitialized = false;
        }

        /// <summary>
        /// Load and validate an automation plan
        /// </summary>
        public ExecutionResult LoadPlan(AutomationContext context)
        {
            var result = new ExecutionResult { PlanId = context.PlanId };

            try
            {
                // Validate context
                if (!context.IsValid())
                {
                    result.Success = false;
                    result.Error = "Invalid automation context";
                    return result;
                }

                // Validate with PlanValidatorService
                var validation = _validatorService.ValidatePlan(context);
                if (!validation.IsValid)
                {
                    result.Success = false;
                    result.Error = $"Plan validation failed: {string.Join(", ", validation.Errors)}";
                    return result;
                }

                // Store context
                _activeContexts[context.PlanId] = context;

                result.Success = true;
                result.Message = $"Plan '{context.PlanId}' loaded and validated successfully";
                _log?.LogInfo($"[ExecutionEngine] Plan '{context.PlanId}' loaded");

            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"Failed to load plan: {ex.Message}";
                _log?.LogError($"[ExecutionEngine] Error loading plan '{context.PlanId}': {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Execute test plan (dry run with visualization)
        /// </summary>
        public ExecutionResult TestPlan(string planId, ulong characterId, bool showVisualization = true)
        {
            var result = new ExecutionResult { PlanId = planId, IsTestRun = true };

            try
            {
                if (!_activeContexts.TryGetValue(planId, out var context))
                {
                    result.Success = false;
                    result.Error = $"Plan '{planId}' not found";
                    return result;
                }

                // Update context with current state
                UpdateContextWithCurrentState(context, characterId);

                // Execute through gate
                var gateResult = _gateService.ExecuteAutomation(
                    new AutomationContextStruct
                    {
                        CharacterId = characterId,
                        PvPState = context.CurrentPvPState,
                        InPvPZone = context.IsInPvPZone,
                        IsAdmin = context.IsAdmin,
                        ScriptName = $"testplan_{planId}"
                    },
                    AutomationCapability.ReadOnlyAnalytics,
                    () => PerformTestExecution(context, showVisualization)
                );

                result.Success = gateResult.Success;
                result.Error = gateResult.Error;
                result.Data = gateResult.Data;

                if (result.Success)
                {
                    result.Message = $"Test execution completed for plan '{planId}'";
                }

            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"Test execution failed: {ex.Message}";
                _log?.LogError($"[ExecutionEngine] Test execution error for '{planId}': {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Execute plan (full execution with mutations)
        /// </summary>
        public ExecutionResult RunPlan(string planId, ulong characterId)
        {
            var result = new ExecutionResult { PlanId = planId, IsTestRun = false };

            try
            {
                if (!_activeContexts.TryGetValue(planId, out var context))
                {
                    result.Success = false;
                    result.Error = $"Plan '{planId}' not found";
                    return result;
                }

                // Update context with current state
                UpdateContextWithCurrentState(context, characterId);

                // Execute through gate with appropriate capability
                var capability = context.IsTestOnly ? AutomationCapability.ReadOnlyAnalytics : AutomationCapability.PvPVirtualMutation;

                var gateResult = _gateService.ExecuteAutomation(
                    new AutomationContextStruct
                    {
                        CharacterId = characterId,
                        PvPState = context.CurrentPvPState,
                        InPvPZone = context.IsInPvPZone,
                        IsAdmin = context.IsAdmin,
                        ScriptName = $"runplan_{planId}"
                    },
                    capability,
                    () => PerformFullExecution(context)
                );

                result.Success = gateResult.Success;
                result.Error = gateResult.Error;
                result.Data = gateResult.Data;

                if (result.Success)
                {
                    result.Message = $"Plan '{planId}' executed successfully";
                }

            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"Execution failed: {ex.Message}";
                _log?.LogError($"[ExecutionEngine] Execution error for '{planId}': {ex.Message}");
            }

            return result;
        }

        private void UpdateContextWithCurrentState(AutomationContext context, ulong characterId)
        {
            // TODO: Query actual PvP state from LifecycleService
            // For now, assume Active state for testing
            context.CurrentPvPState = PvPState.Active;
            context.IsInPvPZone = true;
            context.IsAdmin = true; // TODO: Check admin status
            context.CharacterId = characterId;
        }

        private AutomationResult PerformTestExecution(AutomationContext context, bool showVisualization)
        {
            var result = new AutomationResult { Success = true };

            try
            {
                // Simulate zone creation
                foreach (var zone in context.Zones)
                {
                    _log?.LogInfo($"[Test] Would create zone '{zone.Name}' at tile {zone.CenterTile} with radius {zone.RadiusTiles}");
                }

                // Simulate boss spawning
                foreach (var boss in context.Bosses)
                {
                    _log?.LogInfo($"[Test] Would spawn boss '{boss.Name}' at tile {boss.SpawnTile} in zone '{boss.ZoneName}'");

                    // Calculate potential rewards
                    var rewards = _rewardService.CalculateRewards(boss.PrefabGuid.GuidHash);
                    foreach (var reward in rewards)
                    {
                        var itemName = _prefabResolver.GetPrefabName(reward.ItemGuid.GuidHash);
                        _log?.LogInfo($"[Test] Boss '{boss.Name}' would drop {reward.Quantity}x {itemName}");
                    }
                }

                // Simulate map effects
                foreach (var effect in context.MapEffects)
                {
                    _log?.LogInfo($"[Test] Would apply {effect.Type} effect at tile {effect.LocationTile} for {effect.Duration}s");
                }

                // Simulate respawn scheduling
                if (context.RespawnRules.RespawnIntervalSeconds > 0)
                {
                    _log?.LogInfo($"[Test] Would schedule respawns every {context.RespawnRules.RespawnIntervalSeconds}s");
                }

                if (showVisualization)
                {
                    result.Data = GenerateVisualizationData(context);
                }

                result.Message = "Test execution completed successfully";

            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"Test execution error: {ex.Message}";
            }

            return result;
        }

        private AutomationResult PerformFullExecution(AutomationContext context)
        {
            var result = new AutomationResult { Success = true };

            try
            {
                // TODO: Implement actual execution
                // This would involve:
                // 1. Creating zones via ZoneService
                // 2. Spawning bosses via VBloodMapper
                // 3. Applying map effects
                // 4. Setting up respawn schedules

                _log?.LogWarning("[ExecutionEngine] Full execution not yet implemented - would apply changes to game world");

                result.Message = "Full execution simulation completed (actual implementation pending)";

            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"Full execution error: {ex.Message}";
            }

            return result;
        }

        private object GenerateVisualizationData(AutomationContext context)
        {
            // TODO: Generate map visualization data
            return new
            {
                Zones = context.Zones.Select(z => new
                {
                    z.Name,
                    Center = z.CenterTile,
                    z.RadiusTiles,
                    z.Type
                }),
                Bosses = context.Bosses.Select(b => new
                {
                    b.Name,
                    Location = b.SpawnTile,
                    b.ZoneName
                }),
                Effects = context.MapEffects.Select(e => new
                {
                    e.Type,
                    Location = e.LocationTile,
                    e.RadiusTiles
                })
            };
        }

        /// <summary>
        /// Get loaded plan
        /// </summary>
        public AutomationContext GetPlan(string planId)
        {
            return _activeContexts.TryGetValue(planId, out var context) ? context : null;
        }

        /// <summary>
        /// List all loaded plans
        /// </summary>
        public IEnumerable<string> GetLoadedPlans()
        {
            return _activeContexts.Keys;
        }

        /// <summary>
        /// Unload a plan
        /// </summary>
        public bool UnloadPlan(string planId)
        {
            if (_activeContexts.Remove(planId))
            {
                _log?.LogInfo($"[ExecutionEngine] Plan '{planId}' unloaded");
                return true;
            }
            return false;
        }

        public ServiceHealthStatus GetHealthStatus()
        {
            return new ServiceHealthStatus
            {
                ServiceName = "AutomationExecutionEngine",
                IsHealthy = _isInitialized,
                Status = _isInitialized ? "Active" : "Inactive",
                LastCheck = DateTime.UtcNow
            };
        }

        public ServicePerformanceMetrics GetPerformanceMetrics()
        {
            return new ServicePerformanceMetrics
            {
                ServiceName = "AutomationExecutionEngine",
                ActiveOperations = _activeContexts.Count,
                MeasuredAt = DateTime.UtcNow
            };
        }

        public int GetErrorCount() => 0;
        public string GetLastError() => null;
    }

    // Internal struct for gate service compatibility
    internal struct AutomationContextStruct
    {
        public ulong CharacterId;
        public PvPState PvPState;
        public bool InPvPZone;
        public bool IsAdmin;
        public FixedString64Bytes ScriptName;
    }

    public class ExecutionResult
    {
        public string PlanId { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public bool IsTestRun { get; set; }
    }
}