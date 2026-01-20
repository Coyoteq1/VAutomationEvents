using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using VAuto.Services.Interfaces;
using VAuto.Data;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Automation Scheduler Service - handles respawn timing and scheduled events
    /// Only operates during Active PvP state, fully lifecycle-compliant
    /// </summary>
    public class AutomationSchedulerService : IService, IServiceHealthMonitor
    {
        private static AutomationSchedulerService _instance;
        public static AutomationSchedulerService Instance => _instance ??= new AutomationSchedulerService();

        private bool _isInitialized;
        private ManualLogSource _log;

        // Active schedules
        private readonly Dictionary<string, ScheduledEvent> _activeSchedules = new();
        private readonly List<RespawnSchedule> _respawnSchedules = new();

        // Dependencies
        private AutomationExecutionEngine _executionEngine;

        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => _log;

        public void Initialize()
        {
            if (_isInitialized) return;
            _log = ServiceManager.Log;

            _executionEngine = AutomationExecutionEngine.Instance;

            _isInitialized = true;
            _log?.LogInfo("[AutomationSchedulerService] Initialized");
        }

        public void Cleanup()
        {
            _activeSchedules.Clear();
            _respawnSchedules.Clear();
            _isInitialized = false;
        }

        /// <summary>
        /// Schedule a plan for execution
        /// </summary>
        public bool SchedulePlanExecution(string planId, DateTime executeAt, ulong scheduledBy)
        {
            try
            {
                var schedule = new ScheduledEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    PlanId = planId,
                    ExecuteAt = executeAt,
                    ScheduledBy = scheduledBy,
                    EventType = ScheduledEventType.PlanExecution,
                    IsActive = true
                };

                _activeSchedules[schedule.EventId] = schedule;
                _log?.LogInfo($"[Scheduler] Scheduled plan '{planId}' for {executeAt:yyyy-MM-dd HH:mm:ss} by {scheduledBy}");

                return true;
            }
            catch (Exception ex)
            {
                _log?.LogError($"[Scheduler] Error scheduling plan '{planId}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Set up respawn schedules for a plan
        /// </summary>
        public bool SetupRespawnSchedules(AutomationContext context)
        {
            try
            {
                // Clear existing schedules for this plan
                _respawnSchedules.RemoveAll(s => s.PlanId == context.PlanId);

                if (context.RespawnRules.RespawnIntervalSeconds > 0)
                {
                    // Interval-based respawning
                    var schedule = new RespawnSchedule
                    {
                        PlanId = context.PlanId,
                        IntervalSeconds = context.RespawnRules.RespawnIntervalSeconds,
                        MaxRespawns = context.RespawnRules.MaxRespawns,
                        NextRespawn = DateTime.UtcNow.AddSeconds(context.RespawnRules.RespawnIntervalSeconds),
                        RespawnCount = 0,
                        IsActive = true
                    };

                    _respawnSchedules.Add(schedule);
                    _log?.LogInfo($"[Scheduler] Set up interval respawn for plan '{context.PlanId}' every {schedule.IntervalSeconds}s");
                }

                if (context.RespawnRules.DateBasedRespawn && context.RespawnRules.RespawnDates != null)
                {
                    // Date-based respawning
                    foreach (var respawnDate in context.RespawnRules.RespawnDates.Where(d => d > DateTime.UtcNow))
                    {
                        var schedule = new RespawnSchedule
                        {
                            PlanId = context.PlanId,
                            RespawnDate = respawnDate,
                            IsActive = true
                        };

                        _respawnSchedules.Add(schedule);
                        _log?.LogInfo($"[Scheduler] Set up date respawn for plan '{context.PlanId}' at {respawnDate:yyyy-MM-dd HH:mm:ss}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _log?.LogError($"[Scheduler] Error setting up respawn schedules for '{context.PlanId}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Process scheduled events (call this periodically)
        /// </summary>
        public void ProcessScheduledEvents()
        {
            var now = DateTime.UtcNow;

            // Process plan executions
            var dueExecutions = _activeSchedules.Values
                .Where(s => s.IsActive && s.ExecuteAt <= now)
                .ToList();

            foreach (var schedule in dueExecutions)
            {
                ExecuteScheduledPlan(schedule);
                schedule.IsActive = false; // One-time execution
            }

            // Process respawns
            var dueRespawns = _respawnSchedules
                .Where(s => s.IsActive && ShouldRespawn(s, now))
                .ToList();

            foreach (var schedule in dueRespawns)
            {
                ExecuteRespawn(schedule);
                UpdateRespawnSchedule(schedule, now);
            }

            // Clean up completed schedules
            _activeSchedules.Values.RemoveAll(s => !s.IsActive);
            _respawnSchedules.RemoveAll(s => !s.IsActive);
        }

        private void ExecuteScheduledPlan(ScheduledEvent schedule)
        {
            try
            {
                _log?.LogInfo($"[Scheduler] Executing scheduled plan '{schedule.PlanId}'");

                // Execute the plan (assuming admin context for scheduled executions)
                var result = _executionEngine.RunPlan(schedule.PlanId, schedule.ScheduledBy);

                if (result.Success)
                {
                    _log?.LogInfo($"[Scheduler] Scheduled plan '{schedule.PlanId}' executed successfully");
                }
                else
                {
                    _log?.LogError($"[Scheduler] Scheduled plan '{schedule.PlanId}' failed: {result.Error}");
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[Scheduler] Error executing scheduled plan '{schedule.PlanId}': {ex.Message}");
            }
        }

        private bool ShouldRespawn(RespawnSchedule schedule, DateTime now)
        {
            if (!schedule.IsActive) return false;

            if (schedule.RespawnDate.HasValue)
            {
                return schedule.RespawnDate.Value <= now;
            }
            else if (schedule.NextRespawn.HasValue)
            {
                return schedule.NextRespawn.Value <= now;
            }

            return false;
        }

        private void ExecuteRespawn(RespawnSchedule schedule)
        {
            try
            {
                var context = _executionEngine.GetPlan(schedule.PlanId);
                if (context == null)
                {
                    _log?.LogWarning($"[Scheduler] Cannot respawn - plan '{schedule.PlanId}' not found");
                    schedule.IsActive = false;
                    return;
                }

                // Check if we're in a valid PvP state for respawning
                if (context.CurrentPvPState != PvPState.Active)
                {
                    _log?.LogInfo($"[Scheduler] Skipping respawn for '{schedule.PlanId}' - not in Active PvP state");
                    return;
                }

                _log?.LogInfo($"[Scheduler] Executing respawn for plan '{schedule.PlanId}'");

                // For respawns, we do a targeted execution (just bosses)
                // TODO: Implement targeted boss respawn logic

                schedule.RespawnCount++;

            }
            catch (Exception ex)
            {
                _log?.LogError($"[Scheduler] Error executing respawn for '{schedule.PlanId}': {ex.Message}");
            }
        }

        private void UpdateRespawnSchedule(RespawnSchedule schedule, DateTime now)
        {
            if (!schedule.IsActive) return;

            if (schedule.RespawnDate.HasValue)
            {
                // Date-based: mark as completed
                schedule.IsActive = false;
                _log?.LogInfo($"[Scheduler] Completed date-based respawn for '{schedule.PlanId}'");
            }
            else if (schedule.NextRespawn.HasValue)
            {
                // Interval-based: schedule next
                if (schedule.MaxRespawns == 0 || schedule.RespawnCount < schedule.MaxRespawns)
                {
                    schedule.NextRespawn = now.AddSeconds(schedule.IntervalSeconds);
                    _log?.LogInfo($"[Scheduler] Scheduled next respawn for '{schedule.PlanId}' at {schedule.NextRespawn:yyyy-MM-dd HH:mm:ss}");
                }
                else
                {
                    schedule.IsActive = false;
                    _log?.LogInfo($"[Scheduler] Completed max respawns ({schedule.MaxRespawns}) for '{schedule.PlanId}'");
                }
            }
        }

        /// <summary>
        /// Cancel a scheduled event
        /// </summary>
        public bool CancelScheduledEvent(string eventId)
        {
            if (_activeSchedules.TryGetValue(eventId, out var schedule))
            {
                schedule.IsActive = false;
                _log?.LogInfo($"[Scheduler] Cancelled scheduled event '{eventId}'");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get active schedules for a plan
        /// </summary>
        public IEnumerable<ScheduledEvent> GetActiveSchedules(string planId)
        {
            return _activeSchedules.Values.Where(s => s.PlanId == planId && s.IsActive);
        }

        /// <summary>
        /// Get active respawn schedules for a plan
        /// </summary>
        public IEnumerable<RespawnSchedule> GetActiveRespawnSchedules(string planId)
        {
            return _respawnSchedules.Where(s => s.PlanId == planId && s.IsActive);
        }

        /// <summary>
        /// Reset respawn schedules for a plan (useful when restarting a plan)
        /// </summary>
        public bool ResetRespawnSchedules(string planId)
        {
            try
            {
                var schedules = _respawnSchedules.Where(s => s.PlanId == planId).ToList();
                foreach (var schedule in schedules)
                {
                    if (schedule.NextRespawn.HasValue)
                    {
                        schedule.NextRespawn = DateTime.UtcNow.AddSeconds(schedule.IntervalSeconds);
                        schedule.RespawnCount = 0;
                    }
                }
                _log?.LogInfo($"[Scheduler] Reset respawn schedules for plan '{planId}'");
                return true;
            }
            catch (Exception ex)
            {
                _log?.LogError($"[Scheduler] Error resetting respawn schedules for '{planId}': {ex.Message}");
                return false;
            }
        }

        public ServiceHealthStatus GetHealthStatus()
        {
            return new ServiceHealthStatus
            {
                ServiceName = "AutomationSchedulerService",
                IsHealthy = _isInitialized,
                Status = _isInitialized ? "Active" : "Inactive",
                LastCheck = DateTime.UtcNow
            };
        }

        public ServicePerformanceMetrics GetPerformanceMetrics()
        {
            return new ServicePerformanceMetrics
            {
                ServiceName = "AutomationSchedulerService",
                ActiveOperations = _activeSchedules.Count + _respawnSchedules.Count(s => s.IsActive),
                MeasuredAt = DateTime.UtcNow
            };
        }

        public int GetErrorCount() => 0;
        public string GetLastError() => null;
    }

    // Schedule data structures
    public class ScheduledEvent
    {
        public string EventId { get; set; }
        public string PlanId { get; set; }
        public DateTime ExecuteAt { get; set; }
        public ulong ScheduledBy { get; set; }
        public ScheduledEventType EventType { get; set; }
        public bool IsActive { get; set; }
    }

    public enum ScheduledEventType
    {
        PlanExecution,
        Respawn
    }

    public class RespawnSchedule
    {
        public string PlanId { get; set; }
        public float IntervalSeconds { get; set; }
        public DateTime? RespawnDate { get; set; }
        public DateTime? NextRespawn { get; set; }
        public int MaxRespawns { get; set; }
        public int RespawnCount { get; set; }
        public bool IsActive { get; set; }
    }
}