using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VAuto.Data;
using VAuto.Services.Systems;

namespace VAuto.Services.Systems
{
    public class ZoneValidatorService
    {
        private readonly PrefabResolverService _prefabResolver;
        private readonly AutomationGateService _automationGate;
        private readonly PlanValidatorService _planValidator;

        public ZoneValidatorService(PrefabResolverService prefabResolver, AutomationGateService automationGate, PlanValidatorService planValidator)
        {
            _prefabResolver = prefabResolver;
            _automationGate = automationGate;
            _planValidator = planValidator;
        }

        public ValidationResult ValidateZone(Zone zone, AutomationContext context)
        {
            var result = new ValidationResult { IsValid = true, Messages = new List<string>() };

            // Validate basic zone properties
            if (string.IsNullOrEmpty(zone.ZoneId))
            {
                result.IsValid = false;
                result.Messages.Add("Zone ID cannot be null or empty");
            }

            if (string.IsNullOrEmpty(zone.Name))
            {
                result.IsValid = false;
                result.Messages.Add("Zone name cannot be null or empty");
            }

            // Validate location
            if (zone.Location == null)
            {
                result.IsValid = false;
                result.Messages.Add("Zone location cannot be null");
            }
            else
            {
                if (zone.Location.Center == null)
                {
                    result.IsValid = false;
                    result.Messages.Add("Zone center location cannot be null");
                }

                if (zone.Location.Radius <= 0)
                {
                    result.IsValid = false;
                    result.Messages.Add("Zone radius must be greater than 0");
                }
            }

            // Validate mobs
            if (zone.Mobs != null)
            {
                foreach (var mob in zone.Mobs)
                {
                    if (string.IsNullOrEmpty(mob.MobName))
                    {
                        result.IsValid = false;
                        result.Messages.Add($"Mob name cannot be null or empty in zone {zone.Name}");
                    }

                    if (mob.Count <= 0)
                    {
                        result.IsValid = false;
                        result.Messages.Add($"Mob count must be greater than 0 for {mob.MobName} in zone {zone.Name}");
                    }

                    // Validate mob exists in prefabs
                    if (!_prefabResolver.ResolveMobName(mob.MobName))
                    {
                        result.IsValid = false;
                        result.Messages.Add($"Mob {mob.MobName} does not exist in prefabs for zone {zone.Name}");
                    }
                }
            }

            // Validate boss
            if (zone.Boss != null)
            {
                if (string.IsNullOrEmpty(zone.Boss.BossName))
                {
                    result.IsValid = false;
                    result.Messages.Add($"Boss name cannot be null or empty in zone {zone.Name}");
                }

                // Validate boss exists
                if (!_prefabResolver.ResolveBossName(zone.Boss.BossName))
                {
                    result.IsValid = false;
                    result.Messages.Add($"Boss {zone.Boss.BossName} does not exist in prefabs for zone {zone.Name}");
                }

                // Validate boss rewards
                if (zone.Boss.Reward != null)
                {
                    foreach (var reward in zone.Boss.Reward)
                    {
                        if (string.IsNullOrEmpty(reward.ItemName))
                        {
                            result.IsValid = false;
                            result.Messages.Add($"Boss reward item name cannot be null or empty for boss {zone.Boss.BossName}");
                        }

                        if (reward.Count <= 0)
                        {
                            result.IsValid = false;
                            result.Messages.Add($"Boss reward count must be greater than 0 for item {reward.ItemName}");
                        }

                        // Validate item exists
                        if (!_prefabResolver.ResolveItemName(reward.ItemName))
                        {
                            result.IsValid = false;
                            result.Messages.Add($"Boss reward item {reward.ItemName} does not exist in prefabs");
                        }
                    }
                }
            }

            // Validate gear items
            if (zone.Entry?.OnEnter?.GearList != null)
            {
                foreach (var gear in zone.Entry.OnEnter.GearList)
                {
                    if (string.IsNullOrEmpty(gear.ItemName))
                    {
                        result.IsValid = false;
                        result.Messages.Add($"Gear item name cannot be null or empty in zone {zone.Name}");
                    }

                    if (gear.Count <= 0)
                    {
                        result.IsValid = false;
                        result.Messages.Add($"Gear item count must be greater than 0 for {gear.ItemName} in zone {zone.Name}");
                    }

                    // Validate item exists
                    if (!_prefabResolver.ResolveItemName(gear.ItemName))
                    {
                        result.IsValid = false;
                        result.Messages.Add($"Gear item {gear.ItemName} does not exist in prefabs for zone {zone.Name}");
                    }
                }
            }

            // Validate blood type
            if (zone.Entry?.OnEnter?.OverrideBlood == true && !string.IsNullOrEmpty(zone.Entry.OnEnter.BloodType))
            {
                if (!_prefabResolver.ResolveBloodType(zone.Entry.OnEnter.BloodType))
                {
                    result.IsValid = false;
                    result.Messages.Add($"Blood type {zone.Entry.OnEnter.BloodType} does not exist in prefabs for zone {zone.Name}");
                }
            }

            // Validate permissions
            if (zone.Permissions != null)
            {
                if (zone.Permissions.RequiresDevApproval)
                {
                    result.RequiresDevApproval = true;
                }

                if (zone.Permissions.RequiresSnapshot)
                {
                    result.RequiresSnapshot = true;
                }
            }

            // Check for zone overlaps with existing zones
            if (context.Zones != null)
            {
                foreach (var existingZone in context.Zones)
                {
                    if (existingZone.ZoneId != zone.ZoneId && ZoneOverlaps(zone, existingZone))
                    {
                        result.IsValid = false;
                        result.Messages.Add($"Zone {zone.Name} overlaps with existing zone {existingZone.Name}");
                    }
                }
            }

            return result;
        }

        private bool ZoneOverlaps(Zone zone1, Zone zone2)
        {
            // Simple distance-based overlap check
            var dx = zone1.Location.Center.X - zone2.Location.Center.X;
            var dy = zone1.Location.Center.Y - zone2.Location.Center.Y;
            var dz = zone1.Location.Center.Z - zone2.Location.Center.Z;
            var distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);

            return distance < (zone1.Location.Radius + zone2.Location.Radius);
        }

        public ValidationResult ValidateZonePlan(AutomationContext context)
        {
            var result = new ValidationResult { IsValid = true, Messages = new List<string>() };

            if (context.Zones == null || context.Zones.Count == 0)
            {
                result.IsValid = false;
                result.Messages.Add("No zones defined in automation context");
                return result;
            }

            foreach (var zone in context.Zones)
            {
                var zoneResult = ValidateZone(zone, context);
                if (!zoneResult.IsValid)
                {
                    result.IsValid = false;
                    result.Messages.AddRange(zoneResult.Messages);
                    result.RequiresDevApproval = result.RequiresDevApproval || zoneResult.RequiresDevApproval;
                    result.RequiresSnapshot = result.RequiresSnapshot || zoneResult.RequiresSnapshot;
                }
            }

            return result;
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Messages { get; set; }
        public bool RequiresDevApproval { get; set; }
        public bool RequiresSnapshot { get; set; }
    }
}