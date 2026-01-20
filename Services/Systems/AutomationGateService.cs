using System;
using System.Collections.Generic;
using Unity.Entities;
using VAuto.Services.Interfaces;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Automation Gate Service - enforces PvP-safe automation execution
    /// All automation requests must pass through this gate
    /// </summary>
    public class AutomationGateService : IService, IServiceHealthMonitor
    {
        private static AutomationGateService _instance;
        public static AutomationGateService Instance => _instance ??= new AutomationGateService();

        private bool _isInitialized;
        private ManualLogSource _log;

        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => _log;

        public void Initialize()
        {
            if (_isInitialized) return;
            _log = ServiceManager.Log;
            _isInitialized = true;
            _log?.LogInfo("[AutomationGateService] Initialized - enforcing PvP-safe automation");
        }

        public void Cleanup()
        {
            _isInitialized = false;
        }

        /// <summary>
        /// Validate and execute automation request
        /// </summary>
        public AutomationResult ExecuteAutomation(AutomationContext context, AutomationCapability capability, Func<AutomationResult> action)
        {
            // Validate context
            var validation = ValidateContext(context);
            if (!validation.IsValid)
            {
                LogAutomationAttempt(context, capability, false, validation.Reason);
                return new AutomationResult
                {
                    Success = false,
                    Error = validation.Reason,
                    Capability = capability
                };
            }

            // Check capability permissions
            if (!CanExecuteCapability(context, capability))
            {
                var reason = $"Capability {capability} not allowed for context";
                LogAutomationAttempt(context, capability, false, reason);
                return new AutomationResult
                {
                    Success = false,
                    Error = reason,
                    Capability = capability
                };
            }

            // Execute action
            try
            {
                var result = action();
                result.Capability = capability;
                LogAutomationAttempt(context, capability, result.Success, result.Success ? "Success" : result.Error);
                return result;
            }
            catch (Exception ex)
            {
                var error = $"Automation execution failed: {ex.Message}";
                LogAutomationAttempt(context, capability, false, error);
                return new AutomationResult
                {
                    Success = false,
                    Error = error,
                    Capability = capability
                };
            }
        }

        private ContextValidation ValidateContext(AutomationContext context)
        {
            // Context must be immutable and valid
            if (context.CharacterId == 0)
                return new ContextValidation { IsValid = false, Reason = "Invalid CharacterId" };

            if (string.IsNullOrEmpty(context.ScriptName.ToString()))
                return new ContextValidation { IsValid = false, Reason = "Missing script name" };

            // PvP state validation
            if (context.PvPState == PvPState.Snapshot || context.PvPState == PvPState.Restoring)
                return new ContextValidation { IsValid = false, Reason = "Automation blocked during snapshot/restore" };

            return new ContextValidation { IsValid = true };
        }

        private bool CanExecuteCapability(AutomationContext context, AutomationCapability capability)
        {
            // All automation requires PvP Active state
            if (context.PvPState != PvPState.Active)
                return false;

            // All automation requires being in PvP zone
            if (!context.InPvPZone)
                return false;

            // PvP Virtual Mutation requires admin
            if (capability == AutomationCapability.PvPVirtualMutation && !context.IsAdmin)
                return false;

            // Advisory capabilities are allowed for all in PvP
            if (capability == AutomationCapability.AdvisoryOnly ||
                capability == AutomationCapability.ReadOnlyAnalytics ||
                capability == AutomationCapability.Broadcast ||
                capability == AutomationCapability.Logging)
                return true;

            // Castle capabilities require admin approval
            if (capability == AutomationCapability.CastleBuilding ||
                capability == AutomationCapability.CastleManagement)
            {
                return context.IsAdmin;
            }

            // Logistics capabilities require admin approval and no snapshot in progress
            if (capability == AutomationCapability.LogisticsTransfer ||
                capability == AutomationCapability.LogisticsRefill ||
                capability == AutomationCapability.LogisticsRepair ||
                capability == AutomationCapability.LogisticsBalance)
            {
                return context.IsAdmin; // Additional validation for no snapshot could be added
            }

            return false;
        }

        private void LogAutomationAttempt(AutomationContext context, AutomationCapability capability, bool allowed, string reason)
        {
            var status = allowed ? "ALLOWED" : "DENIED";
            _log?.LogInfo($"[Automation] {context.ScriptName} | {capability} | {status} | {reason} | CharacterId: {context.CharacterId}");
        }

        #region Zone System Methods

        /// <summary>
        /// Check zone-specific automation gates
        /// </summary>
        public ZoneGateResult CheckZoneGates(VAuto.Automation.Zone zone, VAuto.Data.AutomationContext context)
        {
            var result = new ZoneGateResult { Passed = true, Messages = new List<string>() };

            // Check if zone requires dev approval
            if (zone.Permissions?.RequiresDevApproval == true)
            {
                if (!context.IsAdmin)
                {
                    result.Passed = false;
                    result.Messages.Add("Zone requires dev approval but user is not admin");
                    result.RequiresDevApproval = true;
                }
            }

            // Check if zone requires snapshot
            if (zone.Permissions?.RequiresSnapshot == true)
            {
                // Check if we're in a safe state for snapshots
                if (context.CurrentPvPState != VAuto.Data.PvPState.Outside &&
                    context.CurrentPvPState != VAuto.Data.PvPState.Active)
                {
                    result.Passed = false;
                    result.Messages.Add("Zone requires snapshot but PvP state is not safe for snapshots");
                    result.RequiresSnapshot = true;
                }
            }

            // Check if zone requires admin
            if (zone.Permissions?.RequiresAdmin == true && !context.IsAdmin)
            {
                result.Passed = false;
                result.Messages.Add("Zone requires admin privileges");
            }

            return result;
        }

        #endregion

        public ServiceHealthStatus GetHealthStatus()
        {
            return new ServiceHealthStatus
            {
                ServiceName = "AutomationGateService",
                IsHealthy = _isInitialized,
                Status = _isInitialized ? "Active" : "Inactive",
                LastCheck = DateTime.UtcNow
            };
        }

        public ServicePerformanceMetrics GetPerformanceMetrics()
        {
            return new ServicePerformanceMetrics
            {
                ServiceName = "AutomationGateService",
                ActiveOperations = 0,
                MeasuredAt = DateTime.UtcNow
            };
        }

        public int GetErrorCount() => 0;
        public string GetLastError() => null;
    }

    // Automation Enums and Structs

    public enum AutomationCapability
    {
        ReadOnlyAnalytics,
        AdvisoryOnly,
        PvPVirtualMutation,
        Broadcast,
        Logging,
        CastleBuilding,
        CastleManagement,
        LogisticsTransfer,
        LogisticsRefill,
        LogisticsRepair,
        LogisticsBalance
    }

    public enum PvPState
    {
        Outside,
        Snapshot,
        Active,
        Restoring
    }

    public struct AutomationContext
    {
        public ulong CharacterId;
        public PvPState PvPState;
        public bool InPvPZone;
        public bool IsAdmin;
        public FixedString64Bytes ScriptName;
    }

    public class AutomationResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public AutomationCapability Capability { get; set; }
        public object Data { get; set; }
    }

    internal class ContextValidation
    {
        public bool IsValid { get; set; }
        public string Reason { get; set; }
    }

    public class ZoneGateResult
    {
        public bool Passed { get; set; }
        public List<string> Messages { get; set; }
        public bool RequiresDevApproval { get; set; }
        public bool RequiresSnapshot { get; set; }
    }
}