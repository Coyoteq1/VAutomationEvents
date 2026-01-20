using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using VAuto.Services.Interfaces;
using VAuto.Data;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Plan Visualization Service - provides PlanDiff viewer and dry-run map visualization
    /// Optional service for visual plan inspection and comparison
    /// </summary>
    public class PlanVisualizationService : IService, IServiceHealthMonitor
    {
        private static PlanVisualizationService _instance;
        public static PlanVisualizationService Instance => _instance ??= new PlanVisualizationService();

        private bool _isInitialized;
        private ManualLogSource _log;

        // Dependencies
        private PrefabResolverService _prefabResolver;

        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => _log;

        public void Initialize()
        {
            if (_isInitialized) return;
            _log = ServiceManager.Log;

            _prefabResolver = PrefabResolverService.Instance;
            _prefabResolver.Initialize();

            _isInitialized = true;
            _log?.LogInfo("[PlanVisualizationService] Initialized");
        }

        public void Cleanup()
        {
            _isInitialized = false;
        }

        /// <summary>
        /// Generate dry-run visualization data
        /// </summary>
        public VisualizationData GenerateVisualization(AutomationContext context)
        {
            var data = new VisualizationData
            {
                PlanId = context.PlanId,
                GeneratedAt = DateTime.UtcNow
            };

            try
            {
                // Generate zone visualizations
                data.Zones = context.Zones.Select(z => new ZoneVisualization
                {
                    Name = z.Name,
                    CenterTile = z.CenterTile,
                    RadiusTiles = z.RadiusTiles,
                    Type = z.Type,
                    AutoEnter = z.AutoEnter,
                    TileCount = CalculateTileCount(z.RadiusTiles)
                }).ToList();

                // Generate boss visualizations
                data.Bosses = context.Bosses.Select(b => new BossVisualization
                {
                    Name = b.Name,
                    PrefabName = _prefabResolver.GetPrefabName(b.PrefabGuid.GuidHash),
                    SpawnTile = b.SpawnTile,
                    ZoneName = b.ZoneName,
                    Health = b.Health,
                    RewardCount = b.Rewards?.Count ?? 0,
                    LastSpawn = b.LastSpawn
                }).ToList();

                // Generate effect visualizations
                data.Effects = context.MapEffects.Select(e => new EffectVisualization
                {
                    Type = e.Type,
                    LocationTile = e.LocationTile,
                    RadiusTiles = e.RadiusTiles,
                    Duration = e.Duration,
                    VisualEffect = e.VisualEffect,
                    AffectedTiles = CalculateTileCount(e.RadiusTiles)
                }).ToList();

                // Generate summary
                data.Summary = new PlanSummary
                {
                    TotalZones = data.Zones.Count,
                    TotalBosses = data.Bosses.Count,
                    TotalEffects = data.Effects.Count,
                    TotalItems = context.Items.Count,
                    EstimatedTileCoverage = data.Zones.Sum(z => z.TileCount) + data.Effects.Sum(e => e.AffectedTiles),
                    HasRespawnRules = context.RespawnRules.RespawnIntervalSeconds > 0 || context.RespawnRules.DateBasedRespawn
                };

                data.Success = true;

            }
            catch (Exception ex)
            {
                data.Success = false;
                data.Error = $"Visualization generation failed: {ex.Message}";
                _log?.LogError($"[Visualization] Error generating visualization for '{context.PlanId}': {ex.Message}");
            }

            return data;
        }

        /// <summary>
        /// Generate PlanDiff between two plans
        /// </summary>
        public PlanDiff GeneratePlanDiff(AutomationContext oldPlan, AutomationContext newPlan)
        {
            var diff = new PlanDiff
            {
                OldPlanId = oldPlan?.PlanId,
                NewPlanId = newPlan?.PlanId,
                GeneratedAt = DateTime.UtcNow
            };

            try
            {
                // Compare zones
                diff.ZoneChanges = CompareZones(oldPlan?.Zones, newPlan?.Zones);

                // Compare bosses
                diff.BossChanges = CompareBosses(oldPlan?.Bosses, newPlan?.Bosses);

                // Compare items
                diff.ItemChanges = CompareItems(oldPlan?.Items, newPlan?.Items);

                // Compare effects
                diff.EffectChanges = CompareEffects(oldPlan?.MapEffects, newPlan?.MapEffects);

                // Compare respawn rules
                diff.RespawnRuleChanges = CompareRespawnRules(oldPlan?.RespawnRules, newPlan?.RespawnRules);

                // Compare blood handling
                diff.BloodHandlingChanges = CompareBloodHandling(oldPlan?.BloodHandling, newPlan?.BloodHandling);

                diff.HasChanges = diff.ZoneChanges.Any() || diff.BossChanges.Any() || diff.ItemChanges.Any() ||
                                diff.EffectChanges.Any() || diff.RespawnRuleChanges.Any() || diff.BloodHandlingChanges.Any();

                diff.Success = true;

            }
            catch (Exception ex)
            {
                diff.Success = false;
                diff.Error = $"PlanDiff generation failed: {ex.Message}";
                _log?.LogError($"[Visualization] Error generating PlanDiff: {ex.Message}");
            }

            return diff;
        }

        /// <summary>
        /// Generate map overlay data for visualization
        /// </summary>
        public MapOverlayData GenerateMapOverlay(AutomationContext context)
        {
            var overlay = new MapOverlayData
            {
                PlanId = context.PlanId,
                GeneratedAt = DateTime.UtcNow
            };

            try
            {
                // Create tile-based overlay
                var tileMap = new Dictionary<int2, TileData>();

                // Add zones to overlay
                foreach (var zone in context.Zones)
                {
                    var tiles = GetTilesInRadius(zone.CenterTile, zone.RadiusTiles);
                    foreach (var tile in tiles)
                    {
                        if (!tileMap.ContainsKey(tile))
                        {
                            tileMap[tile] = new TileData { Position = tile };
                        }
                        tileMap[tile].ZoneName = zone.Name;
                        tileMap[tile].ZoneType = zone.Type;
                    }
                }

                // Add effects to overlay
                foreach (var effect in context.MapEffects)
                {
                    var tiles = GetTilesInRadius(effect.LocationTile, effect.RadiusTiles);
                    foreach (var tile in tiles)
                    {
                        if (!tileMap.ContainsKey(tile))
                        {
                            tileMap[tile] = new TileData { Position = tile };
                        }
                        tileMap[tile].EffectType = effect.Type;
                        tileMap[tile].EffectDuration = effect.Duration;
                    }
                }

                // Add bosses to overlay
                foreach (var boss in context.Bosses)
                {
                    if (!tileMap.ContainsKey(boss.SpawnTile))
                    {
                        tileMap[boss.SpawnTile] = new TileData { Position = boss.SpawnTile };
                    }
                    tileMap[boss.SpawnTile].BossName = boss.Name;
                    tileMap[boss.SpawnTile].BossHealth = boss.Health;
                }

                overlay.Tiles = tileMap.Values.ToList();
                overlay.Bounds = CalculateBounds(overlay.Tiles);
                overlay.Success = true;

            }
            catch (Exception ex)
            {
                overlay.Success = false;
                overlay.Error = $"Map overlay generation failed: {ex.Message}";
                _log?.LogError($"[Visualization] Error generating map overlay for '{context.PlanId}': {ex.Message}");
            }

            return overlay;
        }

        private List<ZoneChange> CompareZones(List<ZoneDefinition> oldZones, List<ZoneDefinition> newZones)
        {
            var changes = new List<ZoneChange>();

            oldZones = oldZones ?? new List<ZoneDefinition>();
            newZones = newZones ?? new List<ZoneDefinition>();

            var oldDict = oldZones.ToDictionary(z => z.Name);
            var newDict = newZones.ToDictionary(z => z.Name);

            // Added zones
            foreach (var zone in newZones.Where(z => !oldDict.ContainsKey(z.Name)))
            {
                changes.Add(new ZoneChange
                {
                    ChangeType = ChangeType.Added,
                    ZoneName = zone.Name,
                    NewValue = zone
                });
            }

            // Removed zones
            foreach (var zone in oldZones.Where(z => !newDict.ContainsKey(z.Name)))
            {
                changes.Add(new ZoneChange
                {
                    ChangeType = ChangeType.Removed,
                    ZoneName = zone.Name,
                    OldValue = zone
                });
            }

            // Modified zones
            foreach (var newZone in newZones.Where(z => oldDict.ContainsKey(z.Name)))
            {
                var oldZone = oldDict[newZone.Name];
                if (!ZonesEqual(oldZone, newZone))
                {
                    changes.Add(new ZoneChange
                    {
                        ChangeType = ChangeType.Modified,
                        ZoneName = newZone.Name,
                        OldValue = oldZone,
                        NewValue = newZone
                    });
                }
            }

            return changes;
        }

        private List<BossChange> CompareBosses(List<BossDefinition> oldBosses, List<BossDefinition> newBosses)
        {
            var changes = new List<BossChange>();

            oldBosses = oldBosses ?? new List<BossDefinition>();
            newBosses = newBosses ?? new List<BossDefinition>();

            var oldDict = oldBosses.ToDictionary(b => b.Name);
            var newDict = newBosses.ToDictionary(b => b.Name);

            // Added bosses
            foreach (var boss in newBosses.Where(b => !oldDict.ContainsKey(b.Name)))
            {
                changes.Add(new BossChange
                {
                    ChangeType = ChangeType.Added,
                    BossName = boss.Name,
                    NewValue = boss
                });
            }

            // Removed bosses
            foreach (var boss in oldBosses.Where(b => !newDict.ContainsKey(b.Name)))
            {
                changes.Add(new BossChange
                {
                    ChangeType = ChangeType.Removed,
                    BossName = boss.Name,
                    OldValue = boss
                });
            }

            // Modified bosses
            foreach (var newBoss in newBosses.Where(b => oldDict.ContainsKey(b.Name)))
            {
                var oldBoss = oldDict[newBoss.Name];
                if (!BossesEqual(oldBoss, newBoss))
                {
                    changes.Add(new BossChange
                    {
                        ChangeType = ChangeType.Modified,
                        BossName = newBoss.Name,
                        OldValue = oldBoss,
                        NewValue = newBoss
                    });
                }
            }

            return changes;
        }

        private List<ItemChange> CompareItems(List<ItemDefinition> oldItems, List<ItemDefinition> newItems)
        {
            var changes = new List<ItemChange>();

            oldItems = oldItems ?? new List<ItemDefinition>();
            newItems = newItems ?? new List<ItemDefinition>();

            var oldDict = oldItems.ToDictionary(i => i.Guid.GuidHash);
            var newDict = newItems.ToDictionary(i => i.Guid.GuidHash);

            // Added items
            foreach (var item in newItems.Where(i => !oldDict.ContainsKey(i.Guid.GuidHash)))
            {
                changes.Add(new ItemChange
                {
                    ChangeType = ChangeType.Added,
                    ItemGuid = item.Guid,
                    ItemName = item.Name,
                    NewValue = item
                });
            }

            // Removed items
            foreach (var item in oldItems.Where(i => !newDict.ContainsKey(i.Guid.GuidHash)))
            {
                changes.Add(new ItemChange
                {
                    ChangeType = ChangeType.Removed,
                    ItemGuid = item.Guid,
                    ItemName = item.Name,
                    OldValue = item
                });
            }

            return changes;
        }

        private List<EffectChange> CompareEffects(List<MapEffectDefinition> oldEffects, List<MapEffectDefinition> newEffects)
        {
            var changes = new List<EffectChange>();

            oldEffects = oldEffects ?? new List<MapEffectDefinition>();
            newEffects = newEffects ?? new List<MapEffectDefinition>();

            // Simple comparison - effects are considered different if counts differ
            if (oldEffects.Count != newEffects.Count)
            {
                changes.Add(new EffectChange
                {
                    ChangeType = ChangeType.Modified,
                    Description = $"Effect count changed from {oldEffects.Count} to {newEffects.Count}"
                });
            }

            return changes;
        }

        private List<RespawnRuleChange> CompareRespawnRules(RespawnRulesDefinition oldRules, RespawnRulesDefinition newRules)
        {
            var changes = new List<RespawnRuleChange>();

            if (oldRules == null && newRules == null) return changes;
            if (oldRules == null || newRules == null)
            {
                changes.Add(new RespawnRuleChange
                {
                    ChangeType = oldRules == null ? ChangeType.Added : ChangeType.Removed,
                    Description = "Respawn rules added/removed"
                });
                return changes;
            }

            if (oldRules.RespawnIntervalSeconds != newRules.RespawnIntervalSeconds)
            {
                changes.Add(new RespawnRuleChange
                {
                    ChangeType = ChangeType.Modified,
                    Description = $"Respawn interval changed from {oldRules.RespawnIntervalSeconds} to {newRules.RespawnIntervalSeconds}"
                });
            }

            return changes;
        }

        private List<BloodHandlingChange> CompareBloodHandling(BloodHandlingDefinition oldBlood, BloodHandlingDefinition newBlood)
        {
            var changes = new List<BloodHandlingChange>();

            if (oldBlood == null && newBlood == null) return changes;
            if (oldBlood == null || newBlood == null)
            {
                changes.Add(new BloodHandlingChange
                {
                    ChangeType = oldBlood == null ? ChangeType.Added : ChangeType.Removed,
                    Description = "Blood handling rules added/removed"
                });
                return changes;
            }

            if (oldBlood.BossBloodOverride != newBlood.BossBloodOverride)
            {
                changes.Add(new BloodHandlingChange
                {
                    ChangeType = ChangeType.Modified,
                    Description = $"Boss blood override changed from {oldBlood.BossBloodOverride} to {newBlood.BossBloodOverride}"
                });
            }

            return changes;
        }

        // Helper methods
        private int CalculateTileCount(int radius)
        {
            // Approximate circle area
            return (int)(Math.PI * radius * radius);
        }

        private List<int2> GetTilesInRadius(int2 center, int radius)
        {
            var tiles = new List<int2>();
            for (int x = center.x - radius; x <= center.x + radius; x++)
            {
                for (int y = center.y - radius; y <= center.y + radius; y++)
                {
                    var pos = new int2(x, y);
                    if (math.distance(pos, center) <= radius)
                    {
                        tiles.Add(pos);
                    }
                }
            }
            return tiles;
        }

        private MapBounds CalculateBounds(List<TileData> tiles)
        {
            if (tiles.Count == 0) return new MapBounds();

            var minX = tiles.Min(t => t.Position.x);
            var maxX = tiles.Max(t => t.Position.x);
            var minY = tiles.Min(t => t.Position.y);
            var maxY = tiles.Max(t => t.Position.y);

            return new MapBounds
            {
                MinTile = new int2(minX, minY),
                MaxTile = new int2(maxX, maxY),
                Width = maxX - minX + 1,
                Height = maxY - minY + 1
            };
        }

        private bool ZonesEqual(ZoneDefinition a, ZoneDefinition b)
        {
            return a.Name == b.Name &&
                   a.CenterTile.Equals(b.CenterTile) &&
                   a.RadiusTiles == b.RadiusTiles &&
                   a.Type == b.Type &&
                   a.AutoEnter == b.AutoEnter;
        }

        private bool BossesEqual(BossDefinition a, BossDefinition b)
        {
            return a.Name == b.Name &&
                   a.PrefabGuid.Equals(b.PrefabGuid) &&
                   a.SpawnTile.Equals(b.SpawnTile) &&
                   a.ZoneName == b.ZoneName &&
                   a.Health == b.Health;
        }

        public ServiceHealthStatus GetHealthStatus()
        {
            return new ServiceHealthStatus
            {
                ServiceName = "PlanVisualizationService",
                IsHealthy = _isInitialized,
                Status = _isInitialized ? "Active" : "Inactive",
                LastCheck = DateTime.UtcNow
            };
        }

        public ServicePerformanceMetrics GetPerformanceMetrics()
        {
            return new ServicePerformanceMetrics
            {
                ServiceName = "PlanVisualizationService",
                ActiveOperations = 0,
                MeasuredAt = DateTime.UtcNow
            };
        }

        public int GetErrorCount() => 0;
        public string GetLastError() => null;
    }

    // Visualization data structures
    public class VisualizationData
    {
        public string PlanId { get; set; }
        public DateTime GeneratedAt { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public List<ZoneVisualization> Zones { get; set; } = new();
        public List<BossVisualization> Bosses { get; set; } = new();
        public List<EffectVisualization> Effects { get; set; } = new();
        public PlanSummary Summary { get; set; }
    }

    public class ZoneVisualization
    {
        public string Name { get; set; }
        public int2 CenterTile { get; set; }
        public int RadiusTiles { get; set; }
        public string Type { get; set; }
        public bool AutoEnter { get; set; }
        public int TileCount { get; set; }
    }

    public class BossVisualization
    {
        public string Name { get; set; }
        public string PrefabName { get; set; }
        public int2 SpawnTile { get; set; }
        public string ZoneName { get; set; }
        public float Health { get; set; }
        public int RewardCount { get; set; }
        public DateTime? LastSpawn { get; set; }
    }

    public class EffectVisualization
    {
        public string Type { get; set; }
        public int2 LocationTile { get; set; }
        public int RadiusTiles { get; set; }
        public float Duration { get; set; }
        public string VisualEffect { get; set; }
        public int AffectedTiles { get; set; }
    }

    public class PlanSummary
    {
        public int TotalZones { get; set; }
        public int TotalBosses { get; set; }
        public int TotalEffects { get; set; }
        public int TotalItems { get; set; }
        public int EstimatedTileCoverage { get; set; }
        public bool HasRespawnRules { get; set; }
    }

    public class PlanDiff
    {
        public string OldPlanId { get; set; }
        public string NewPlanId { get; set; }
        public DateTime GeneratedAt { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public bool HasChanges { get; set; }
        public List<ZoneChange> ZoneChanges { get; set; } = new();
        public List<BossChange> BossChanges { get; set; } = new();
        public List<ItemChange> ItemChanges { get; set; } = new();
        public List<EffectChange> EffectChanges { get; set; } = new();
        public List<RespawnRuleChange> RespawnRuleChanges { get; set; } = new();
        public List<BloodHandlingChange> BloodHandlingChanges { get; set; } = new();
    }

    public enum ChangeType { Added, Removed, Modified }

    public class ZoneChange
    {
        public ChangeType ChangeType { get; set; }
        public string ZoneName { get; set; }
        public ZoneDefinition OldValue { get; set; }
        public ZoneDefinition NewValue { get; set; }
    }

    public class BossChange
    {
        public ChangeType ChangeType { get; set; }
        public string BossName { get; set; }
        public BossDefinition OldValue { get; set; }
        public BossDefinition NewValue { get; set; }
    }

    public class ItemChange
    {
        public ChangeType ChangeType { get; set; }
        public PrefabGUID ItemGuid { get; set; }
        public string ItemName { get; set; }
        public ItemDefinition OldValue { get; set; }
        public ItemDefinition NewValue { get; set; }
    }

    public class EffectChange
    {
        public ChangeType ChangeType { get; set; }
        public string Description { get; set; }
    }

    public class RespawnRuleChange
    {
        public ChangeType ChangeType { get; set; }
        public string Description { get; set; }
    }

    public class BloodHandlingChange
    {
        public ChangeType ChangeType { get; set; }
        public string Description { get; set; }
    }

    public class MapOverlayData
    {
        public string PlanId { get; set; }
        public DateTime GeneratedAt { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public List<TileData> Tiles { get; set; } = new();
        public MapBounds Bounds { get; set; }
    }

    public class TileData
    {
        public int2 Position { get; set; }
        public string ZoneName { get; set; }
        public string ZoneType { get; set; }
        public string BossName { get; set; }
        public float BossHealth { get; set; }
        public string EffectType { get; set; }
        public float EffectDuration { get; set; }
    }

    public class MapBounds
    {
        public int2 MinTile { get; set; }
        public int2 MaxTile { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}