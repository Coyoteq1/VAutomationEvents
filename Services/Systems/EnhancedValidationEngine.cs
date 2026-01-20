using System;
using System.Collections.Generic;
using System.Linq;
using VAuto.Data;
using VAuto.Automation;
using VAuto.Services.Systems;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Enhanced Validation Engine for the Vauto automation system
    /// Handles comprehensive validation for zones, schematics, logistics, and castle automation
    /// </summary>
    public class EnhancedValidationEngine : IService, IServiceHealthMonitor
    {
        private static EnhancedValidationEngine _instance;
        public static EnhancedValidationEngine Instance => _instance ??= new EnhancedValidationEngine();

        private bool _isInitialized;
        private ManualLogSource _log;

        // Service dependencies
        private ZoneValidatorService _zoneValidator;
        private PrefabResolverService _prefabResolver;
        private AutomationGateService _gateService;

        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => _log;

        public void Initialize()
        {
            if (_isInitialized) return;
            _log = ServiceManager.Log;

            // Initialize dependencies
            _zoneValidator = ZoneValidatorService.Instance;
            _prefabResolver = PrefabResolverService.Instance;
            _gateService = AutomationGateService.Instance;

            _isInitialized = true;
            _log?.LogInfo("[EnhancedValidationEngine] Initialized");
        }

        public void Cleanup()
        {
            _isInitialized = false;
        }

        /// <summary>
        /// Validate an entire automation context including zones, schematics, logistics, and castles
        /// </summary>
        public ComprehensiveValidationResult ValidateAutomationContext(AutomationContext context)
        {
            var result = new ComprehensiveValidationResult
            {
                IsValid = true,
                Messages = new List<string>(),
                ZoneValidations = new List<ZoneValidationResult>(),
                SchematicValidations = new List<SchematicValidationResult>(),
                LogisticsValidations = new List<LogisticsValidationResult>(),
                CastleValidations = new List<CastleValidationResult>()
            };

            try
            {
                // Validate zones
                if (context.NewZones != null && context.NewZones.Count > 0)
                {
                    foreach (var zone in context.NewZones)
                    {
                        var zoneValidation = ValidateZoneInContext(zone, context);
                        result.ZoneValidations.Add(zoneValidation);

                        if (!zoneValidation.IsValid)
                        {
                            result.IsValid = false;
                            result.Messages.AddRange(zoneValidation.Messages);
                        }
                    }
                }

                // Validate schematics (if any exist in context)
                // Note: Schematic validation would be more comprehensive if schematics were stored in context
                // For now, we'll validate zone schematics
                if (context.NewZones != null)
                {
                    foreach (var zone in context.NewZones.Where(z => z.Schematic != null))
                    {
                        var schematicValidation = ValidateZoneSchematic(zone);
                        result.SchematicValidations.Add(schematicValidation);

                        if (!schematicValidation.IsValid)
                        {
                            result.IsValid = false;
                            result.Messages.AddRange(schematicValidation.Messages);
                        }
                    }
                }

                // Validate logistics automation
                if (context.LogisticsAutomation != null)
                {
                    var logisticsValidation = ValidateLogisticsAutomation(context.LogisticsAutomation);
                    result.LogisticsValidations.Add(logisticsValidation);

                    if (!logisticsValidation.IsValid)
                    {
                        result.IsValid = false;
                        result.Messages.AddRange(logisticsValidation.Messages);
                    }
                }

                // Validate castle automation
                if (context.CastleAutomation != null)
                {
                    var castleValidation = ValidateCastleAutomation(context.CastleAutomation);
                    result.CastleValidations.Add(castleValidation);

                    if (!castleValidation.IsValid)
                    {
                        result.IsValid = false;
                        result.Messages.AddRange(castleValidation.Messages);
                    }
                }

                // Check cross-system dependencies
                var dependencyResult = ValidateCrossSystemDependencies(context);
                if (!dependencyResult.IsValid)
                {
                    result.IsValid = false;
                    result.Messages.AddRange(dependencyResult.Messages);
                }

                // Check automation gates
                var gateResult = CheckAutomationGates(context);
                if (!gateResult.Passed)
                {
                    result.IsValid = false;
                    result.RequiresDevApproval = result.RequiresDevApproval || gateResult.RequiresDevApproval;
                    result.RequiresSnapshot = result.RequiresSnapshot || gateResult.RequiresSnapshot;
                    result.Messages.AddRange(gateResult.Messages);
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Messages.Add($"Validation failed: {ex.Message}");
                _log?.LogError($"[EnhancedValidationEngine] Validation error: {ex.Message}");
            }

            return result;
        }

        #region Zone Validation

        private ZoneValidationResult ValidateZoneInContext(Zone zone, AutomationContext context)
        {
            var result = new ZoneValidationResult
            {
                ZoneId = zone.ZoneId,
                ZoneName = zone.Name,
                IsValid = true,
                Messages = new List<string>()
            };

            // Use the existing zone validator
            var validationResult = _zoneValidator.ValidateZone(zone, context);

            result.IsValid = validationResult.IsValid;
            result.RequiresDevApproval = validationResult.RequiresDevApproval;
            result.RequiresSnapshot = validationResult.RequiresSnapshot;

            if (validationResult.Messages != null)
            {
                result.Messages.AddRange(validationResult.Messages);
            }

            // Additional zone-specific validations
            if (zone.Boss != null)
            {
                var bossValidation = ValidateZoneBoss(zone.Boss, context);
                if (!bossValidation.IsValid)
                {
                    result.IsValid = false;
                    result.Messages.AddRange(bossValidation.Messages);
                }
            }

            if (zone.Loot != null)
            {
                var lootValidation = ValidateZoneLoot(zone.Loot, context);
                if (!lootValidation.IsValid)
                {
                    result.IsValid = false;
                    result.Messages.AddRange(lootValidation.Messages);
                }
            }

            return result;
        }

        private BossValidationResult ValidateZoneBoss(Boss boss, AutomationContext context)
        {
            var result = new BossValidationResult
            {
                BossName = boss.BossName,
                IsValid = true,
                Messages = new List<string>()
            };

            // Validate boss name
            if (string.IsNullOrEmpty(boss.BossName))
            {
                result.IsValid = false;
                result.Messages.Add("Boss name cannot be null or empty");
            }
            else if (!_prefabResolver.ResolveBossName(boss.BossName))
            {
                result.IsValid = false;
                result.Messages.Add($"Boss '{boss.BossName}' does not exist in prefabs");
            }

            // Validate boss level
            if (boss.BossLevel <= 0)
            {
                result.IsValid = false;
                result.Messages.Add("Boss level must be greater than 0");
            }

            // Validate respawn configuration
            if (boss.Respawn != null)
            {
                if (string.IsNullOrEmpty(boss.Respawn.Type))
                {
                    result.IsValid = false;
                    result.Messages.Add("Boss respawn type cannot be null or empty");
                }
                else if (boss.Respawn.Type != "Interval" && boss.Respawn.Type != "DateTime")
                {
                    result.IsValid = false;
                    result.Messages.Add($"Invalid respawn type: {boss.Respawn.Type}. Must be 'Interval' or 'DateTime'");
                }

                if (string.IsNullOrEmpty(boss.Respawn.Value))
                {
                    result.IsValid = false;
                    result.Messages.Add("Boss respawn value cannot be null or empty");
                }
                else if (boss.Respawn.Type == "Interval")
                {
                    if (!int.TryParse(boss.Respawn.Value, out var interval) || interval <= 0)
                    {
                        result.IsValid = false;
                        result.Messages.Add("Interval respawn value must be a positive integer");
                    }
                }
                else if (boss.Respawn.Type == "DateTime")
                {
                    if (!DateTime.TryParse(boss.Respawn.Value, out _))
                    {
                        result.IsValid = false;
                        result.Messages.Add("DateTime respawn value must be a valid date/time");
                    }
                }
            }

            // Validate rewards
            if (boss.Reward != null && boss.Reward.Count > 0)
            {
                foreach (var reward in boss.Reward)
                {
                    if (string.IsNullOrEmpty(reward.ItemName))
                    {
                        result.IsValid = false;
                        result.Messages.Add("Boss reward item name cannot be null or empty");
                    }
                    else if (!_prefabResolver.ResolveItemName(reward.ItemName))
                    {
                        result.IsValid = false;
                        result.Messages.Add($"Boss reward item '{reward.ItemName}' does not exist in prefabs");
                    }

                    if (reward.Count <= 0)
                    {
                        result.IsValid = false;
                        result.Messages.Add($"Boss reward count must be greater than 0 for item {reward.ItemName}");
                    }

                    if (reward.Chance < 0 || reward.Chance > 1)
                    {
                        result.IsValid = false;
                        result.Messages.Add($"Boss reward chance must be between 0 and 1 for item {reward.ItemName}");
                    }
                }
            }

            return result;
        }

        private LootValidationResult ValidateZoneLoot(Loot loot, AutomationContext context)
        {
            var result = new LootValidationResult
            {
                IsValid = true,
                Messages = new List<string>()
            };

            // Validate chests
            if (loot.Chests != null)
            {
                foreach (var chest in loot.Chests)
                {
                    if (chest.Position == null)
                    {
                        result.IsValid = false;
                        result.Messages.Add("Chest position cannot be null");
                        continue;
                    }

                    if (chest.LootTable == null || chest.LootTable.Count == 0)
                    {
                        result.IsValid = false;
                        result.Messages.Add($"Chest at ({chest.Position.X}, {chest.Position.Y}, {chest.Position.Z}) has no loot table");
                        continue;
                    }

                    foreach (var lootItem in chest.LootTable)
                    {
                        if (string.IsNullOrEmpty(lootItem.ItemName))
                        {
                            result.IsValid = false;
                            result.Messages.Add("Loot item name cannot be null or empty");
                        }
                        else if (!_prefabResolver.ResolveItemName(lootItem.ItemName))
                        {
                            result.IsValid = false;
                            result.Messages.Add($"Loot item '{lootItem.ItemName}' does not exist in prefabs");
                        }

                        if (lootItem.Count <= 0)
                        {
                            result.IsValid = false;
                            result.Messages.Add($"Loot item count must be greater than 0 for {lootItem.ItemName}");
                        }

                        if (lootItem.Chance < 0 || lootItem.Chance > 1)
                        {
                            result.IsValid = false;
                            result.Messages.Add($"Loot item chance must be between 0 and 1 for {lootItem.ItemName}");
                        }
                    }
                }
            }

            // Validate drop tables
            if (loot.DropTables != null)
            {
                foreach (var dropTable in loot.DropTables)
                {
                    if (string.IsNullOrEmpty(dropTable.TableName))
                    {
                        result.IsValid = false;
                        result.Messages.Add("Drop table name cannot be null or empty");
                        continue;
                    }

                    if (dropTable.Items == null || dropTable.Items.Count == 0)
                    {
                        result.IsValid = false;
                        result.Messages.Add($"Drop table '{dropTable.TableName}' has no items");
                        continue;
                    }

                    foreach (var item in dropTable.Items)
                    {
                        if (string.IsNullOrEmpty(item.ItemName))
                        {
                            result.IsValid = false;
                            result.Messages.Add($"Drop table item name cannot be null or empty in table {dropTable.TableName}");
                        }
                        else if (!_prefabResolver.ResolveItemName(item.ItemName))
                        {
                            result.IsValid = false;
                            result.Messages.Add($"Drop table item '{item.ItemName}' does not exist in prefabs");
                        }

                        if (item.Count <= 0)
                        {
                            result.IsValid = false;
                            result.Messages.Add($"Drop table item count must be greater than 0 for {item.ItemName} in table {dropTable.TableName}");
                        }

                        if (item.Chance < 0 || item.Chance > 1)
                        {
                            result.IsValid = false;
                            result.Messages.Add($"Drop table item chance must be between 0 and 1 for {item.ItemName} in table {dropTable.TableName}");
                        }
                    }
                }
            }

            return result;
        }

        private SchematicValidationResult ValidateZoneSchematic(Zone zone)
        {
            var result = new SchematicValidationResult
            {
                ZoneId = zone.ZoneId,
                SchematicId = zone.Schematic?.SchematicId,
                IsValid = true,
                Messages = new List<string>()
            };

            if (zone.Schematic == null)
            {
                result.IsValid = false;
                result.Messages.Add("Zone schematic configuration is null");
                return result;
            }

            if (zone.Schematic.Enabled && string.IsNullOrEmpty(zone.Schematic.SchematicId))
            {
                result.IsValid = false;
                result.Messages.Add("Schematic is enabled but SchematicId is null or empty");
            }

            // Additional schematic validation would go here
            // For example: checking if schematic exists in database, validating format, etc.

            return result;
        }

        #endregion

        #region Logistics Validation

        private LogisticsValidationResult ValidateLogisticsAutomation(LogisticsAutomationDefinition logistics)
        {
            var result = new LogisticsValidationResult
            {
                IsValid = true,
                Messages = new List<string>(),
                TransferValidations = new List<TransferValidationResult>(),
                RefillValidations = new List<RefillValidationResult>(),
                RepairValidations = new List<RepairValidationResult>(),
                BalanceValidations = new List<BalanceValidationResult>()
            };

            // Validate transfers
            if (logistics.Transfer != null && logistics.Transfer.Count > 0)
            {
                foreach (var transfer in logistics.Transfer)
                {
                    var transferValidation = ValidateLogisticsTransfer(transfer);
                    result.TransferValidations.Add(transferValidation);

                    if (!transferValidation.IsValid)
                    {
                        result.IsValid = false;
                        result.Messages.AddRange(transferValidation.Messages);
                    }
                }
            }

            // Validate refills
            if (logistics.AutoRefill != null && logistics.AutoRefill.Count > 0)
            {
                foreach (var refill in logistics.AutoRefill)
                {
                    var refillValidation = ValidateLogisticsRefill(refill);
                    result.RefillValidations.Add(refillValidation);

                    if (!refillValidation.IsValid)
                    {
                        result.IsValid = false;
                        result.Messages.AddRange(refillValidation.Messages);
                    }
                }
            }

            // Validate repairs
            if (logistics.Repair != null && logistics.Repair.Count > 0)
            {
                foreach (var repair in logistics.Repair)
                {
                    var repairValidation = ValidateLogisticsRepair(repair);
                    result.RepairValidations.Add(repairValidation);

                    if (!repairValidation.IsValid)
                    {
                        result.IsValid = false;
                        result.Messages.AddRange(repairValidation.Messages);
                    }
                }
            }

            // Validate balances
            if (logistics.Balance != null && logistics.Balance.Count > 0)
            {
                foreach (var balance in logistics.Balance)
                {
                    var balanceValidation = ValidateLogisticsBalance(balance);
                    result.BalanceValidations.Add(balanceValidation);

                    if (!balanceValidation.IsValid)
                    {
                        result.IsValid = false;
                        result.Messages.AddRange(balanceValidation.Messages);
                    }
                }
            }

            return result;
        }

        private TransferValidationResult ValidateLogisticsTransfer(LogisticsTransferDefinition transfer)
        {
            var result = new TransferValidationResult
            {
                Source = transfer.From,
                Destination = transfer.To,
                Item = transfer.Item,
                Amount = transfer.Amount,
                IsValid = true,
                Messages = new List<string>()
            };

            if (string.IsNullOrEmpty(transfer.From))
            {
                result.IsValid = false;
                result.Messages.Add("Transfer source cannot be null or empty");
            }

            if (string.IsNullOrEmpty(transfer.To))
            {
                result.IsValid = false;
                result.Messages.Add("Transfer destination cannot be null or empty");
            }

            if (string.IsNullOrEmpty(transfer.Item))
            {
                result.IsValid = false;
                result.Messages.Add("Transfer item cannot be null or empty");
            }
            else if (!_prefabResolver.ResolveItemName(transfer.Item))
            {
                result.IsValid = false;
                result.Messages.Add($"Transfer item '{transfer.Item}' does not exist in prefabs");
            }

            if (transfer.Amount <= 0)
            {
                result.IsValid = false;
                result.Messages.Add("Transfer amount must be greater than 0");
            }

            // Check for circular transfers (source == destination)
            if (transfer.From.Equals(transfer.To, StringComparison.OrdinalIgnoreCase))
            {
                result.IsValid = false;
                result.Messages.Add("Cannot transfer items to the same location");
            }

            return result;
        }

        private RefillValidationResult ValidateLogisticsRefill(LogisticsRefillDefinition refill)
        {
            var result = new RefillValidationResult
            {
                Castle = refill.Castle,
                Item = refill.Item,
                MinAmount = refill.Min,
                MaxAmount = refill.Max,
                IsValid = true,
                Messages = new List<string>()
            };

            if (string.IsNullOrEmpty(refill.Castle))
            {
                result.IsValid = false;
                result.Messages.Add("Refill castle cannot be null or empty");
            }

            if (string.IsNullOrEmpty(refill.Item))
            {
                result.IsValid = false;
                result.Messages.Add("Refill item cannot be null or empty");
            }
            else if (!_prefabResolver.ResolveItemName(refill.Item))
            {
                result.IsValid = false;
                result.Messages.Add($"Refill item '{refill.Item}' does not exist in prefabs");
            }

            if (refill.Min < 0)
            {
                result.IsValid = false;
                result.Messages.Add("Refill minimum amount cannot be negative");
            }

            if (refill.Max <= 0)
            {
                result.IsValid = false;
                result.Messages.Add("Refill maximum amount must be greater than 0");
            }

            if (refill.Min > refill.Max)
            {
                result.IsValid = false;
                result.Messages.Add("Refill minimum amount cannot be greater than maximum amount");
            }

            return result;
        }

        private RepairValidationResult ValidateLogisticsRepair(LogisticsRepairDefinition repair)
        {
            var result = new RepairValidationResult
            {
                Castle = repair.Castle,
                EquipmentId = repair.EquipmentId,
                IsValid = true,
                Messages = new List<string>()
            };

            if (string.IsNullOrEmpty(repair.Castle))
            {
                result.IsValid = false;
                result.Messages.Add("Repair castle cannot be null or empty");
            }

            if (string.IsNullOrEmpty(repair.EquipmentId))
            {
                result.IsValid = false;
                result.Messages.Add("Repair equipment ID cannot be null or empty");
            }

            // Additional validation could check if equipment exists in castle inventory

            return result;
        }

        private BalanceValidationResult ValidateLogisticsBalance(LogisticsBalanceDefinition balance)
        {
            var result = new BalanceValidationResult
            {
                SourceCastle = balance.SourceCastle,
                TargetCastle = balance.TargetCastle,
                Resource = balance.Item,
                Amount = balance.BalanceRatio,
                IsValid = true,
                Messages = new List<string>()
            };

            if (string.IsNullOrEmpty(balance.SourceCastle))
            {
                result.IsValid = false;
                result.Messages.Add("Balance source castle cannot be null or empty");
            }

            if (string.IsNullOrEmpty(balance.TargetCastle))
            {
                result.IsValid = false;
                result.Messages.Add("Balance target castle cannot be null or empty");
            }

            if (string.IsNullOrEmpty(balance.Item))
            {
                result.IsValid = false;
                result.Messages.Add("Balance resource cannot be null or empty");
            }
            else if (!_prefabResolver.ResolveItemName(balance.Item))
            {
                result.IsValid = false;
                result.Messages.Add($"Balance resource '{balance.Item}' does not exist in prefabs");
            }

            if (balance.BalanceRatio <= 0 || balance.BalanceRatio > 1)
            {
                result.IsValid = false;
                result.Messages.Add("Balance ratio must be between 0 and 1");
            }

            // Check for circular balancing (same source and target)
            if (balance.SourceCastle.Equals(balance.TargetCastle, StringComparison.OrdinalIgnoreCase))
            {
                result.IsValid = false;
                result.Messages.Add("Cannot balance resources between the same castle");
            }

            return result;
        }

        #endregion

        #region Castle Validation

        private CastleValidationResult ValidateCastleAutomation(CastleAutomationDefinition castleAutomation)
        {
            var result = new CastleValidationResult
            {
                TargetCastle = castleAutomation.TargetCastle,
                IsValid = true,
                Messages = new List<string>(),
                BuildValidations = new List<CastleBuildValidationResult>()
            };

            if (string.IsNullOrEmpty(castleAutomation.TargetCastle))
            {
                result.IsValid = false;
                result.Messages.Add("Castle automation target castle cannot be null or empty");
            }

            // Validate build operations
            if (castleAutomation.Build != null && castleAutomation.Build.Count > 0)
            {
                foreach (var build in castleAutomation.Build)
                {
                    var buildValidation = ValidateCastleBuild(build);
                    result.BuildValidations.Add(buildValidation);

                    if (!buildValidation.IsValid)
                    {
                        result.IsValid = false;
                        result.Messages.AddRange(buildValidation.Messages);
                    }
                }
            }

            return result;
        }

        private CastleBuildValidationResult ValidateCastleBuild(CastleBuildDefinition build)
        {
            var result = new CastleBuildValidationResult
            {
                SchematicId = build.Prefab,
                IsValid = true,
                Messages = new List<string>()
            };

            if (string.IsNullOrEmpty(build.Prefab))
            {
                result.IsValid = false;
                result.Messages.Add("Castle build prefab cannot be null or empty");
            }
            else if (!_prefabResolver.ResolvePrefab(build.Prefab).HasValue)
            {
                result.IsValid = false;
                result.Messages.Add($"Castle build prefab '{build.Prefab}' does not exist");
            }

            // Validate position (basic checks)
            if (build.Position.x < -10000 || build.Position.x > 10000 ||
                build.Position.y < -10000 || build.Position.y > 10000 ||
                build.Position.z < -10000 || build.Position.z > 10000)
            {
                result.IsValid = false;
                result.Messages.Add("Castle build position coordinates are out of reasonable bounds");
            }

            // Validate rotation
            if (build.Rotation < 0 || build.Rotation > 360)
            {
                result.IsValid = false;
                result.Messages.Add("Castle build rotation must be between 0 and 360 degrees");
            }

            return result;
        }

        #endregion

        #region Cross-System Validation

        private CrossSystemValidationResult ValidateCrossSystemDependencies(AutomationContext context)
        {
            var result = new CrossSystemValidationResult
            {
                IsValid = true,
                Messages = new List<string>()
            };

            // Check if zones reference castles that don't exist
            if (context.NewZones != null && context.CastleAutomation != null)
            {
                var castleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(context.CastleAutomation.TargetCastle))
                {
                    castleNames.Add(context.CastleAutomation.TargetCastle);
                }

                foreach (var zone in context.NewZones)
                {
                    // Check if zone schematic references a castle that doesn't exist
                    if (zone.Schematic != null && !string.IsNullOrEmpty(zone.Schematic.SchematicId))
                    {
                        // This would be more comprehensive if we had a schematic database
                        // For now, we'll just check basic format
                        if (zone.Schematic.SchematicId.Length < 5)
                        {
                            result.IsValid = false;
                            result.Messages.Add($"Zone '{zone.Name}' has an invalid schematic ID format: {zone.Schematic.SchematicId}");
                        }
                    }
                }
            }

            // Check if logistics operations reference valid locations
            if (context.LogisticsAutomation != null)
            {
                var validLocations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Add zones as valid locations
                if (context.NewZones != null)
                {
                    foreach (var zone in context.NewZones)
                    {
                        validLocations.Add(zone.ZoneId);
                        validLocations.Add(zone.Name);
                    }
                }

                // Add castles as valid locations
                if (context.CastleAutomation != null && !string.IsNullOrEmpty(context.CastleAutomation.TargetCastle))
                {
                    validLocations.Add(context.CastleAutomation.TargetCastle);
                }

                // Check logistics transfers
                if (context.LogisticsAutomation.Transfer != null)
                {
                    foreach (var transfer in context.LogisticsAutomation.Transfer)
                    {
                        if (!validLocations.Contains(transfer.From))
                        {
                            result.IsValid = false;
                            result.Messages.Add($"Logistics transfer source '{transfer.From}' is not a valid location");
                        }

                        if (!validLocations.Contains(transfer.To))
                        {
                            result.IsValid = false;
                            result.Messages.Add($"Logistics transfer destination '{transfer.To}' is not a valid location");
                        }
                    }
                }

                // Check logistics refills
                if (context.LogisticsAutomation.AutoRefill != null)
                {
                    foreach (var refill in context.LogisticsAutomation.AutoRefill)
                    {
                        if (!validLocations.Contains(refill.Castle))
                        {
                            result.IsValid = false;
                            result.Messages.Add($"Logistics refill castle '{refill.Castle}' is not a valid location");
                        }
                    }
                }
            }

            return result;
        }

        #endregion

        #region Automation Gate Validation

        private ZoneGateResult CheckAutomationGates(AutomationContext context)
        {
            var result = new ZoneGateResult
            {
                Passed = true,
                Messages = new List<string>(),
                RequiresDevApproval = false,
                RequiresSnapshot = false
            };

            // Check zone gates for all zones
            if (context.NewZones != null)
            {
                foreach (var zone in context.NewZones)
                {
                    var zoneGateResult = _gateService.CheckZoneGates(zone, context);
                    if (!zoneGateResult.Passed)
                    {
                        result.Passed = false;
                        result.Messages.AddRange(zoneGateResult.Messages);
                        result.RequiresDevApproval = result.RequiresDevApproval || zoneGateResult.RequiresDevApproval;
                        result.RequiresSnapshot = result.RequiresSnapshot || zoneGateResult.RequiresSnapshot;
                    }
                }
            }

            return result;
        }

        #endregion

        public ServiceHealthStatus GetHealthStatus()
        {
            return new ServiceHealthStatus
            {
                ServiceName = "EnhancedValidationEngine",
                IsHealthy = _isInitialized,
                Status = _isInitialized ? "Active" : "Inactive",
                LastCheck = DateTime.UtcNow
            };
        }

        public ServicePerformanceMetrics GetPerformanceMetrics()
        {
            return new ServicePerformanceMetrics
            {
                ServiceName = "EnhancedValidationEngine",
                ActiveOperations = 0, // Would track active validations in production
                MeasuredAt = DateTime.UtcNow
            };
        }

        public int GetErrorCount() => 0;
        public string GetLastError() => null;
    }

    #region Validation Result Classes

    /// <summary>
    /// Comprehensive validation result containing all validation outcomes
    /// </summary>
    public class ComprehensiveValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Messages { get; set; }
        public List<ZoneValidationResult> ZoneValidations { get; set; }
        public List<SchematicValidationResult> SchematicValidations { get; set; }
        public List<LogisticsValidationResult> LogisticsValidations { get; set; }
        public List<CastleValidationResult> CastleValidations { get; set; }
        public bool RequiresDevApproval { get; set; }
        public bool RequiresSnapshot { get; set; }
    }

    /// <summary>
    /// Zone validation result
    /// </summary>
    public class ZoneValidationResult
    {
        public string ZoneId { get; set; }
        public string ZoneName { get; set; }
        public bool IsValid { get; set; }
        public List<string> Messages { get; set; }
        public bool RequiresDevApproval { get; set; }
        public bool RequiresSnapshot { get; set; }
    }

    /// <summary>
    /// Schematic validation result
    /// </summary>
    public class SchematicValidationResult
    {
        public string ZoneId { get; set; }
        public string SchematicId { get; set; }
        public bool IsValid { get; set; }
        public List<string> Messages { get; set; }
    }

    /// <summary>
    /// Logistics validation result
    /// </summary>
    public class LogisticsValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Messages { get; set; }
        public List<TransferValidationResult> TransferValidations { get; set; }
        public List<RefillValidationResult> RefillValidations { get; set; }
        public List<RepairValidationResult> RepairValidations { get; set; }
        public List<BalanceValidationResult> BalanceValidations { get; set; }
    }

    /// <summary>
    /// Castle validation result
    /// </summary>
    public class CastleValidationResult
    {
        public string TargetCastle { get; set; }
        public bool IsValid { get; set; }
        public List<string> Messages { get; set; }
        public List<CastleBuildValidationResult> BuildValidations { get; set; }
    }

    /// <summary>
    /// Cross-system validation result
    /// </summary>
    public class CrossSystemValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Messages { get; set; }
    }

    /// <summary>
    /// Boss validation result
    /// </summary>
    public class BossValidationResult
    {
        public string BossName { get; set; }
        public bool IsValid { get; set; }
        public List<string> Messages { get; set; }
    }

    /// <summary>
    /// Loot validation result
    /// </summary>
    public class LootValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Messages { get; set; }
    }

    /// <summary>
    /// Transfer validation result
    /// </summary>
    public class TransferValidationResult
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public string Item { get; set; }
        public int Amount { get; set; }
        public bool IsValid { get; set; }
        public List<string> Messages { get; set; }
    }

    /// <summary>
    /// Refill validation result
    /// </summary>
    public class RefillValidationResult
    {
        public string Castle { get; set; }
        public string Item { get; set; }
        public int MinAmount { get; set; }
        public int MaxAmount { get; set; }
        public bool IsValid { get; set; }
        public List<string> Messages { get; set; }
    }

    /// <summary>
    /// Repair validation result
    /// </summary>
    public class RepairValidationResult
    {
        public string Castle { get; set; }
        public string EquipmentId { get; set; }
        public bool IsValid { get; set; }
        public List<string> Messages { get; set; }
    }

    /// <summary>
    /// Balance validation result
    /// </summary>
    public class BalanceValidationResult
    {
        public string SourceCastle { get; set; }
        public string TargetCastle { get; set; }
        public string Resource { get; set; }
        public float Amount { get; set; }
        public bool IsValid { get; set; }
        public List<string> Messages { get; set; }
    }

    /// <summary>
    /// Castle build validation result
    /// </summary>
    public class CastleBuildValidationResult
    {
        public string SchematicId { get; set; }
        public bool IsValid { get; set; }
        public List<string> Messages { get; set; }
    }

    #endregion
}