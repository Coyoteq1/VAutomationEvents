using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Stunlock.Core;
using VAuto.Automation;

namespace VAuto.Data
{
    /// <summary>
    /// Immutable automation context - single source of truth for all automation operations
    /// Contains all plan data, zones, bosses, rewards, respawns, and map effects
    /// </summary>
    public class AutomationContext
    {
        // Identity
        public string PlanId { get; }
        public string Description { get; }
        public DateTime CreatedAt { get; }
        public bool IsTestOnly { get; }

        // PvP State (read from current game state)
        public PvPState CurrentPvPState { get; set; }
        public bool IsInPvPZone { get; set; }
        public bool IsAdmin { get; set; }
        public ulong CharacterId { get; set; }

        // Zones (defined in tiles)
        public List<ZoneDefinition> Zones { get; }

        // New Zone System
        public List<Zone> NewZones { get; set; }

        // Bosses with rewards
        public List<BossDefinition> Bosses { get; }

        // Item registry
        public List<ItemDefinition> Items { get; }

        // Blood handling
        public BloodHandlingDefinition BloodHandling { get; }

        // Map effects
        public List<MapEffectDefinition> MapEffects { get; }

        // Respawn rules
        public RespawnRulesDefinition RespawnRules { get; }

        // Castle automation
        public CastleAutomationDefinition CastleAutomation { get; }

        // Logistics automation
        public LogisticsAutomationDefinition LogisticsAutomation { get; }

        // Constructor - creates immutable context
        public AutomationContext(
            string planId,
            string description,
            List<ZoneDefinition> zones,
            List<BossDefinition> bosses,
            List<ItemDefinition> items,
            BloodHandlingDefinition bloodHandling,
            List<MapEffectDefinition> mapEffects,
            RespawnRulesDefinition respawnRules,
            CastleAutomationDefinition castleAutomation,
            LogisticsAutomationDefinition logisticsAutomation,
            bool isTestOnly = false)
        {
            PlanId = planId ?? throw new ArgumentNullException(nameof(planId));
            Description = description ?? string.Empty;
            CreatedAt = DateTime.UtcNow;
            IsTestOnly = isTestOnly;

            // Deep copy collections to ensure immutability
            Zones = zones != null ? new List<ZoneDefinition>(zones) : new List<ZoneDefinition>();
            Bosses = bosses != null ? new List<BossDefinition>(bosses) : new List<BossDefinition>();
            Items = items != null ? new List<ItemDefinition>(items) : new List<ItemDefinition>();
            BloodHandling = bloodHandling ?? new BloodHandlingDefinition();
            MapEffects = mapEffects != null ? new List<MapEffectDefinition>(mapEffects) : new List<MapEffectDefinition>();
            RespawnRules = respawnRules ?? new RespawnRulesDefinition();
            CastleAutomation = castleAutomation ?? new CastleAutomationDefinition();
            LogisticsAutomation = logisticsAutomation ?? new LogisticsAutomationDefinition();
        }

        // Validation methods
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(PlanId) &&
                   Zones.Count > 0 &&
                   ValidateZoneReferences() &&
                   ValidateItemReferences() &&
                   ValidateBossRewards();
        }

        private bool ValidateZoneReferences()
        {
            var zoneNames = new HashSet<string>(Zones.Select(z => z.Name));
            foreach (var boss in Bosses)
            {
                if (!string.IsNullOrEmpty(boss.ZoneName) && !zoneNames.Contains(boss.ZoneName))
                    return false;
            }
            return true;
        }

        private bool ValidateItemReferences()
        {
            var itemGuids = new HashSet<int>(Items.Select(i => i.Guid.GuidHash));
            foreach (var boss in Bosses)
            {
                if (boss.Rewards != null)
                {
                    foreach (var reward in boss.Rewards)
                    {
                        if (!itemGuids.Contains(reward.ItemGuid.GuidHash))
                            return false;
                    }
                }
            }
            return true;
        }

        private bool ValidateBossRewards()
        {
            foreach (var boss in Bosses)
            {
                if (boss.Rewards != null)
                {
                    foreach (var reward in boss.Rewards)
                    {
                        if (reward.DropChance < 0 || reward.DropChance > 1)
                            return false;
                        if (reward.Quantity <= 0)
                            return false;
                    }
                }
            }
            return true;
        }

        // Utility methods
        public ZoneDefinition GetZone(string name)
        {
            return Zones.FirstOrDefault(z => z.Name == name);
        }

        public BossDefinition GetBoss(string name)
        {
            return Bosses.FirstOrDefault(b => b.Name == name);
        }

        public ItemDefinition GetItem(PrefabGUID guid)
        {
            return Items.FirstOrDefault(i => i.Guid == guid);
        }

        public IEnumerable<BossDefinition> GetBossesInZone(string zoneName)
        {
            return Bosses.Where(b => b.ZoneName == zoneName);
        }
    }

    // Enums
    public enum PvPState
    {
        Outside,
        Snapshot,
        Active,
        Restoring
    }

    // Data structures
    public class ZoneDefinition
    {
        public string Name { get; set; }
        public int2 CenterTile { get; set; } // Tile coordinates
        public int RadiusTiles { get; set; } // Radius in tiles
        public string Type { get; set; } // "pvp", "safe", etc.
        public bool AutoEnter { get; set; }
    }

    public class BossDefinition
    {
        public string Name { get; set; }
        public PrefabGUID PrefabGuid { get; set; }
        public int2 SpawnTile { get; set; }
        public string ZoneName { get; set; }
        public float Health { get; set; }
        public List<RewardDefinition> Rewards { get; set; }
        public DateTime? LastSpawn { get; set; }
        public int SpawnCount { get; set; }
    }

    public class ItemDefinition
    {
        public PrefabGUID Guid { get; set; }
        public string Name { get; set; }
        public int MaxStack { get; set; }
    }

    public class RewardDefinition
    {
        public PrefabGUID ItemGuid { get; set; }
        public int Quantity { get; set; }
        public float DropChance { get; set; }
    }

    public class BloodHandlingDefinition
    {
        public bool BossBloodOverride { get; set; }
        public PrefabGUID BossBloodType { get; set; }
        public bool PlayerBloodOverride { get; set; }
        public PrefabGUID PlayerBloodType { get; set; }
    }

    public class MapEffectDefinition
    {
        public string Type { get; set; }
        public int2 LocationTile { get; set; }
        public int RadiusTiles { get; set; }
        public float Duration { get; set; }
        public string VisualEffect { get; set; }
    }

    public class RespawnRulesDefinition
    {
        public float RespawnIntervalSeconds { get; set; } // 0 = no auto respawn
        public bool DateBasedRespawn { get; set; }
        public List<DateTime> RespawnDates { get; set; }
        public int MaxRespawns { get; set; } // 0 = unlimited
        public bool ResetOnPlanRestart { get; set; }
    }

    public class CastleAutomationDefinition
    {
        public string TargetCastle { get; set; }
        public List<CastleBuildDefinition> Build { get; set; } = new();
    }

    public class CastleBuildDefinition
    {
        public string Prefab { get; set; }
        public float3 Position { get; set; }
        public float Rotation { get; set; }
    }

    public class LogisticsAutomationDefinition
    {
        public List<LogisticsTransferDefinition> Transfer { get; set; } = new();
        public List<LogisticsRefillDefinition> AutoRefill { get; set; } = new();
        public List<LogisticsBalanceDefinition> Balance { get; set; } = new();
    }

    public class LogisticsTransferDefinition
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Item { get; set; }
        public int Amount { get; set; }
    }

    public class LogisticsRefillDefinition
    {
        public string Castle { get; set; }
        public string Item { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
    }

    public class LogisticsBalanceDefinition
    {
        public string SourceCastle { get; set; }
        public string TargetCastle { get; set; }
        public string Item { get; set; }
        public float BalanceRatio { get; set; } = 0.1f;
    }
}