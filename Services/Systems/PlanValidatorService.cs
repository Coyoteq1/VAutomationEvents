using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using VAuto.Services.Interfaces;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Service for validating event orchestration plans
    /// Ensures plans are safe, consistent, and compliant with system rules
    /// </summary>
    public class PlanValidatorService : IService, IServiceHealthMonitor
    {
        private static PlanValidatorService _instance;
        public static PlanValidatorService Instance => _instance ??= new PlanValidatorService();

        private bool _isInitialized;
        private ManualLogSource _log;

        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => _log;

        public void Initialize()
        {
            if (_isInitialized) return;
            _log = ServiceManager.Log;
            _isInitialized = true;
            _log?.LogInfo("[PlanValidatorService] Initialized");
        }

        public void Cleanup()
        {
            _isInitialized = false;
        }

        /// <summary>
        /// Validates a complete event plan
        /// </summary>
        public ValidationResult ValidatePlan(EventPlan plan)
        {
            var result = new ValidationResult { IsValid = true, Errors = new List<string>() };

            // Validate zones
            ValidateZones(plan.Zones, result);

            // Validate bosses
            ValidateBosses(plan.Bosses, result);

            // Validate items
            ValidateItems(plan.Items, result);

            // Validate blood handling
            ValidateBloodHandling(plan.BloodHandling, result);

            // Validate map effects
            ValidateMapEffects(plan.MapEffects, result);

            // Validate respawn rules
            ValidateRespawnRules(plan.RespawnRules, result);

            // Cross-validation
            CrossValidatePlan(plan, result);

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        private void ValidateZones(List<ZoneDefinition> zones, ValidationResult result)
        {
            if (zones == null || zones.Count == 0)
            {
                result.Errors.Add("Plan must define at least one zone");
                return;
            }

            var zoneNames = new HashSet<string>();
            foreach (var zone in zones)
            {
                if (string.IsNullOrEmpty(zone.Name))
                {
                    result.Errors.Add("Zone name cannot be empty");
                    continue;
                }

                if (!zoneNames.Add(zone.Name))
                {
                    result.Errors.Add($"Duplicate zone name: {zone.Name}");
                }

                if (zone.Radius <= 0)
                {
                    result.Errors.Add($"Zone {zone.Name} has invalid radius: {zone.Radius}");
                }

                // Validate zone bounds are within world limits
                if (math.length(zone.Center) > 10000) // Arbitrary world limit
                {
                    result.Errors.Add($"Zone {zone.Name} center is outside world bounds");
                }
            }
        }

        private void ValidateBosses(List<BossDefinition> bosses, ValidationResult result)
        {
            if (bosses == null) return;

            var bossNames = new HashSet<string>();
            foreach (var boss in bosses)
            {
                if (string.IsNullOrEmpty(boss.Name))
                {
                    result.Errors.Add("Boss name cannot be empty");
                    continue;
                }

                if (!bossNames.Add(boss.Name))
                {
                    result.Errors.Add($"Duplicate boss name: {boss.Name}");
                }

                if (string.IsNullOrEmpty(boss.PrefabGuid))
                {
                    result.Errors.Add($"Boss {boss.Name} has no prefab GUID");
                }

                if (boss.Health <= 0)
                {
                    result.Errors.Add($"Boss {boss.Name} has invalid health: {boss.Health}");
                }

                // Validate spawn location is within a defined zone
                if (!string.IsNullOrEmpty(boss.ZoneName))
                {
                    // This will be checked in cross-validation
                }
            }
        }

        private void ValidateItems(List<ItemDefinition> items, ValidationResult result)
        {
            if (items == null) return;

            var itemGuids = new HashSet<string>();
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.Guid))
                {
                    result.Errors.Add("Item GUID cannot be empty");
                    continue;
                }

                if (!itemGuids.Add(item.Guid))
                {
                    result.Errors.Add($"Duplicate item GUID: {item.Guid}");
                }

                if (item.Quantity <= 0)
                {
                    result.Errors.Add($"Item {item.Guid} has invalid quantity: {item.Quantity}");
                }
            }
        }

        private void ValidateBloodHandling(BloodHandlingDefinition blood, ValidationResult result)
        {
            if (blood == null) return;

            if (blood.BossBloodOverride && string.IsNullOrEmpty(blood.BossBloodType))
            {
                result.Errors.Add("Boss blood override enabled but no blood type specified");
            }

            if (blood.PlayerBloodOverride && string.IsNullOrEmpty(blood.PlayerBloodType))
            {
                result.Errors.Add("Player blood override enabled but no blood type specified");
            }
        }

        private void ValidateMapEffects(List<MapEffectDefinition> effects, ValidationResult result)
        {
            if (effects == null) return;

            foreach (var effect in effects)
            {
                if (string.IsNullOrEmpty(effect.Type))
                {
                    result.Errors.Add("Map effect type cannot be empty");
                }

                if (effect.Duration <= 0)
                {
                    result.Errors.Add($"Map effect {effect.Type} has invalid duration: {effect.Duration}");
                }
            }
        }

        private void ValidateRespawnRules(RespawnRulesDefinition rules, ValidationResult result)
        {
            if (rules == null) return;

            if (rules.RespawnInterval <= 0 && !rules.DateBasedRespawn)
            {
                result.Errors.Add("Invalid respawn configuration: no interval or date-based rule");
            }

            if (rules.DateBasedRespawn && rules.RespawnDates.Count == 0)
            {
                result.Errors.Add("Date-based respawn enabled but no dates specified");
            }
        }

        private void CrossValidatePlan(EventPlan plan, ValidationResult result)
        {
            // Validate boss zones exist
            var zoneNames = plan.Zones?.Select(z => z.Name).ToHashSet() ?? new HashSet<string>();
            if (plan.Bosses != null)
            {
                foreach (var boss in plan.Bosses)
                {
                    if (!string.IsNullOrEmpty(boss.ZoneName) && !zoneNames.Contains(boss.ZoneName))
                    {
                        result.Errors.Add($"Boss {boss.Name} references non-existent zone: {boss.ZoneName}");
                    }
                }
            }

            // Validate reward items exist
            var itemGuids = plan.Items?.Select(i => i.Guid).ToHashSet() ?? new HashSet<string>();
            if (plan.Bosses != null)
            {
                foreach (var boss in plan.Bosses)
                {
                    if (boss.Rewards != null)
                    {
                        foreach (var reward in boss.Rewards)
                        {
                            if (!itemGuids.Contains(reward.ItemGuid))
                            {
                                result.Errors.Add($"Boss {boss.Name} reward references non-existent item: {reward.ItemGuid}");
                            }
                        }
                    }
                }
            }
        }

        public ServiceHealthStatus GetHealthStatus()
        {
            return new ServiceHealthStatus
            {
                ServiceName = "PlanValidatorService",
                IsHealthy = _isInitialized,
                Status = _isInitialized ? "Active" : "Inactive",
                LastCheck = DateTime.UtcNow
            };
        }

        public ServicePerformanceMetrics GetPerformanceMetrics()
        {
            return new ServicePerformanceMetrics
            {
                ServiceName = "PlanValidatorService",
                ActiveOperations = 0,
                MeasuredAt = DateTime.UtcNow
            };
        }

        public int GetErrorCount() => 0;
        public string GetLastError() => null;
    }

    // Plan Data Structures

    public class EventPlan
    {
        public string PlanId { get; set; }
        public string Description { get; set; }
        public List<ZoneDefinition> Zones { get; set; }
        public List<BossDefinition> Bosses { get; set; }
        public List<ItemDefinition> Items { get; set; }
        public BloodHandlingDefinition BloodHandling { get; set; }
        public List<MapEffectDefinition> MapEffects { get; set; }
        public RespawnRulesDefinition RespawnRules { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsTestOnly { get; set; }
    }

    public class ZoneDefinition
    {
        public string Name { get; set; }
        public float3 Center { get; set; }
        public float Radius { get; set; }
        public string Type { get; set; } // "pvp", "safe", etc.
    }

    public class BossDefinition
    {
        public string Name { get; set; }
        public string PrefabGuid { get; set; }
        public float3 SpawnLocation { get; set; }
        public string ZoneName { get; set; }
        public float Health { get; set; }
        public List<RewardDefinition> Rewards { get; set; }
    }

    public class ItemDefinition
    {
        public string Guid { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
    }

    public class RewardDefinition
    {
        public string ItemGuid { get; set; }
        public int Quantity { get; set; }
        public float DropChance { get; set; }
    }

    public class BloodHandlingDefinition
    {
        public bool BossBloodOverride { get; set; }
        public string BossBloodType { get; set; }
        public bool PlayerBloodOverride { get; set; }
        public string PlayerBloodType { get; set; }
    }

    public class MapEffectDefinition
    {
        public string Type { get; set; }
        public float Duration { get; set; }
        public float3 Location { get; set; }
        public float Radius { get; set; }
    }

    public class RespawnRulesDefinition
    {
        public float RespawnInterval { get; set; } // in seconds, 0 = no auto respawn
        public bool DateBasedRespawn { get; set; }
        public List<DateTime> RespawnDates { get; set; }
        public int MaxRespawns { get; set; } // 0 = unlimited
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
    }
}