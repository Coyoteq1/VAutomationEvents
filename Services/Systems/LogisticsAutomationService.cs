using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using VAuto.Services.Interfaces;
using VAuto.Core;
using VAuto.Data;
using VAuto.Services.Systems;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Logistics Automation Service - Handles automated item transfers, refills, and repairs
    /// Provides automation hooks for logistics operations in PvP zones
    /// </summary>
    public class LogisticsAutomationService : IService
    {
        private static LogisticsAutomationService _instance;
        public static LogisticsAutomationService Instance => _instance ??= new LogisticsAutomationService();

        private bool _isInitialized;
        private readonly object _lock = new object();

        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => Plugin.Logger;

        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            Log?.LogInfo("[LogisticsAutomationService] Initialized");
        }

        public void Cleanup()
        {
            if (!_isInitialized) return;
            _isInitialized = false;
            Log?.LogInfo("[LogisticsAutomationService] Cleaned up");
        }

        /// <summary>
        /// Execute item transfer operation
        /// </summary>
        public LogisticsResult TransferItems(LogisticsTransferCommand command)
        {
            lock (_lock)
            {
                try
                {
                    Log?.LogInfo($"[LogisticsAutomationService] Transferring {command.Amount}x {command.ItemName} from {command.SourceCastleId} to {command.TargetCastleId}");

                    // Validate PvP state and admin approval
                    if (!ValidateAutomationContext())
                    {
                        return new LogisticsResult
                        {
                            Success = false,
                            ErrorMessage = "Automation not allowed - PvP state inactive or admin approval required"
                        };
                    }

                    // Get source and target castles
                    var sourceCastle = CastleRegistryService.Instance.GetCastle(command.SourceCastleId);
                    var targetCastle = CastleRegistryService.Instance.GetCastle(command.TargetCastleId);

                    if (sourceCastle == null || targetCastle == null)
                    {
                        return new LogisticsResult
                        {
                            Success = false,
                            ErrorMessage = "Source or target castle not found"
                        };
                    }

                    // Find suitable inventories for transfer
                    var sourceInventories = FindInventoriesWithItem(sourceCastle, command.ItemGuid, command.Amount);
                    var targetInventories = FindAvailableInventories(targetCastle, command.ItemGuid);

                    if (!sourceInventories.Any() || !targetInventories.Any())
                    {
                        return new LogisticsResult
                        {
                            Success = false,
                            ErrorMessage = "No suitable source or target inventories found"
                        };
                    }

                    // Execute transfer
                    var totalTransferred = 0;
                    foreach (var sourceInv in sourceInventories)
                    {
                        if (totalTransferred >= command.Amount) break;

                        foreach (var targetInv in targetInventories)
                        {
                            var remaining = command.Amount - totalTransferred;
                            var result = TransferBetweenInventories(sourceInv, targetInv, command.ItemGuid, remaining);

                            if (result.Success)
                            {
                                totalTransferred += result.ItemsTransferred;
                                Log?.LogInfo($"[LogisticsAutomationService] Transferred {result.ItemsTransferred}x {command.ItemName}");
                            }

                            if (totalTransferred >= command.Amount) break;
                        }
                    }

                    return new LogisticsResult
                    {
                        Success = totalTransferred > 0,
                        ItemsTransferred = totalTransferred,
                        ErrorMessage = totalTransferred == 0 ? "No items could be transferred" : null
                    };
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[LogisticsAutomationService] Transfer failed: {ex.Message}");
                    return new LogisticsResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                }
            }
        }

        /// <summary>
        /// Execute auto-refill operation
        /// </summary>
        public LogisticsResult AutoRefill(LogisticsRefillCommand command)
        {
            lock (_lock)
            {
                try
                {
                    Log?.LogInfo($"[LogisticsAutomationService] Auto-refilling {command.ItemName} in castle {command.CastleId}");

                    if (!ValidateAutomationContext())
                    {
                        return new LogisticsResult
                        {
                            Success = false,
                            ErrorMessage = "Automation not allowed"
                        };
                    }

                    var castle = CastleRegistryService.Instance.GetCastle(command.CastleId);
                    if (castle == null)
                    {
                        return new LogisticsResult
                        {
                            Success = false,
                            ErrorMessage = "Castle not found"
                        };
                    }

                    // Find inventories that need refilling
                    var inventoriesToRefill = FindInventoriesNeedingItem(castle, command.ItemGuid, command.MinAmount);
                    var sourceInventories = FindInventoriesWithItem(castle, command.ItemGuid, 1); // At least 1 item

                    if (!inventoriesToRefill.Any() || !sourceInventories.Any())
                    {
                        return new LogisticsResult
                        {
                            Success = false,
                            ErrorMessage = "No inventories need refilling or no source items available"
                        };
                    }

                    var totalRefilled = 0;
                    foreach (var targetInv in inventoriesToRefill)
                    {
                        var currentAmount = GetItemAmount(targetInv, command.ItemGuid);
                        var needed = Math.Min(command.MaxAmount - currentAmount, command.RefillAmount);

                        if (needed <= 0) continue;

                        foreach (var sourceInv in sourceInventories)
                        {
                            if (sourceInv == targetInv) continue; // Don't transfer to self

                            var result = TransferBetweenInventories(sourceInv, targetInv, command.ItemGuid, needed);
                            if (result.Success)
                            {
                                totalRefilled += result.ItemsTransferred;
                                needed -= result.ItemsTransferred;

                                if (needed <= 0) break;
                            }
                        }
                    }

                    return new LogisticsResult
                    {
                        Success = totalRefilled > 0,
                        ItemsTransferred = totalRefilled
                    };
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[LogisticsAutomationService] Auto-refill failed: {ex.Message}");
                    return new LogisticsResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                }
            }
        }

        /// <summary>
        /// Execute auto-repair operation
        /// </summary>
        public LogisticsResult AutoRepair(LogisticsRepairCommand command)
        {
            lock (_lock)
            {
                try
                {
                    Log?.LogInfo($"[LogisticsAutomationService] Auto-repairing equipment in castle {command.CastleId}");

                    if (!ValidateAutomationContext())
                    {
                        return new LogisticsResult
                        {
                            Success = false,
                            ErrorMessage = "Automation not allowed"
                        };
                    }

                    var castle = CastleRegistryService.Instance.GetCastle(command.CastleId);
                    if (castle == null)
                    {
                        return new LogisticsResult
                        {
                            Success = false,
                            ErrorMessage = "Castle not found"
                        };
                    }

                    // Find damaged equipment in castle inventories
                    var damagedItems = FindDamagedEquipment(castle, command.RepairThreshold);

                    if (!damagedItems.Any())
                    {
                        return new LogisticsResult
                        {
                            Success = false,
                            ErrorMessage = "No damaged equipment found"
                        };
                    }

                    // Find repair materials
                    var repairMaterials = FindRepairMaterials(castle);

                    var totalRepaired = 0;
                    foreach (var damagedItem in damagedItems)
                    {
                        if (RepairEquipment(damagedItem, repairMaterials))
                        {
                            totalRepaired++;
                            Log?.LogInfo($"[LogisticsAutomationService] Repaired equipment");
                        }
                    }

                    return new LogisticsResult
                    {
                        Success = totalRepaired > 0,
                        ItemsTransferred = totalRepaired
                    };
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[LogisticsAutomationService] Auto-repair failed: {ex.Message}");
                    return new LogisticsResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                }
            }
        }

        /// <summary>
        /// Balance resources between castles
        /// </summary>
        public LogisticsResult BalanceResources(LogisticsBalanceCommand command)
        {
            lock (_lock)
            {
                try
                {
                    Log?.LogInfo($"[LogisticsAutomationService] Balancing {command.ItemName} between castles");

                    if (!ValidateAutomationContext())
                    {
                        return new LogisticsResult
                        {
                            Success = false,
                            ErrorMessage = "Automation not allowed"
                        };
                    }

                    var sourceCastle = CastleRegistryService.Instance.GetCastle(command.SourceCastleId);
                    var targetCastle = CastleRegistryService.Instance.GetCastle(command.TargetCastleId);

                    if (sourceCastle == null || targetCastle == null)
                    {
                        return new LogisticsResult
                        {
                            Success = false,
                            ErrorMessage = "Source or target castle not found"
                        };
                    }

                    // Calculate amounts to balance
                    var sourceAmount = GetTotalItemAmount(sourceCastle, command.ItemGuid);
                    var targetAmount = GetTotalItemAmount(targetCastle, command.ItemGuid);

                    var difference = sourceAmount - targetAmount;
                    var transferAmount = (int)(difference * command.BalanceRatio);

                    if (Math.Abs(transferAmount) < command.MinTransferAmount)
                    {
                        return new LogisticsResult
                        {
                            Success = false,
                            ErrorMessage = "Transfer amount below minimum threshold"
                        };
                    }

                    if (transferAmount > 0)
                    {
                        // Transfer from source to target
                        var transferCommand = new LogisticsTransferCommand
                        {
                            SourceCastleId = command.SourceCastleId,
                            TargetCastleId = command.TargetCastleId,
                            ItemGuid = command.ItemGuid,
                            ItemName = command.ItemName,
                            Amount = transferAmount
                        };

                        return TransferItems(transferCommand);
                    }
                    else
                    {
                        // Transfer from target to source
                        var transferCommand = new LogisticsTransferCommand
                        {
                            SourceCastleId = command.TargetCastleId,
                            TargetCastleId = command.SourceCastleId,
                            ItemGuid = command.ItemGuid,
                            ItemName = command.ItemName,
                            Amount = -transferAmount
                        };

                        return TransferItems(transferCommand);
                    }
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[LogisticsAutomationService] Balance failed: {ex.Message}");
                    return new LogisticsResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                }
            }
        }

        #region Helper Methods

        private bool ValidateAutomationContext()
        {
            // TODO: Check PvP state is active, admin approval, no snapshot in progress
            // For now, return true
            return true;
        }

        private List<Entity> FindInventoriesWithItem(CastleData castle, PrefabGUID itemGuid, int minAmount)
        {
            // TODO: Implement inventory scanning logic
            return new List<Entity>();
        }

        private List<Entity> FindAvailableInventories(CastleData castle, PrefabGUID itemGuid)
        {
            // TODO: Implement available inventory finding logic
            return new List<Entity>();
        }

        private List<Entity> FindInventoriesNeedingItem(CastleData castle, PrefabGUID itemGuid, int minAmount)
        {
            // TODO: Implement inventory scanning for items below threshold
            return new List<Entity>();
        }

        private int GetItemAmount(Entity inventory, PrefabGUID itemGuid)
        {
            // TODO: Implement item amount retrieval
            return 0;
        }

        private int GetTotalItemAmount(CastleData castle, PrefabGUID itemGuid)
        {
            // TODO: Implement total item amount calculation across castle
            return 0;
        }

        private LogisticsResult TransferBetweenInventories(Entity source, Entity target, PrefabGUID itemGuid, int amount)
        {
            // TODO: Implement actual item transfer logic
            return new LogisticsResult { Success = true, ItemsTransferred = amount };
        }

        private List<Entity> FindDamagedEquipment(CastleData castle, int repairThreshold)
        {
            // TODO: Implement damaged equipment scanning
            return new List<Entity>();
        }

        private Dictionary<PrefabGUID, int> FindRepairMaterials(CastleData castle)
        {
            // TODO: Implement repair material finding
            return new Dictionary<PrefabGUID, int>();
        }

        private bool RepairEquipment(Entity equipment, Dictionary<PrefabGUID, int> materials)
        {
            // TODO: Implement equipment repair logic
            return true;
        }

        #endregion
    }

    #region Command Structures

    public class LogisticsTransferCommand
    {
        public int SourceCastleId { get; set; }
        public int TargetCastleId { get; set; }
        public PrefabGUID ItemGuid { get; set; }
        public string ItemName { get; set; }
        public int Amount { get; set; }
    }

    public class LogisticsRefillCommand
    {
        public int CastleId { get; set; }
        public PrefabGUID ItemGuid { get; set; }
        public string ItemName { get; set; }
        public int MinAmount { get; set; }
        public int MaxAmount { get; set; }
        public int RefillAmount { get; set; }
    }

    public class LogisticsRepairCommand
    {
        public int CastleId { get; set; }
        public int RepairThreshold { get; set; } = 50; // Repair items below 50% durability
    }

    public class LogisticsBalanceCommand
    {
        public int SourceCastleId { get; set; }
        public int TargetCastleId { get; set; }
        public PrefabGUID ItemGuid { get; set; }
        public string ItemName { get; set; }
        public float BalanceRatio { get; set; } = 0.1f; // Transfer 10% of difference
        public int MinTransferAmount { get; set; } = 10;
    }

    public class LogisticsResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int ItemsTransferred { get; set; }
    }

    #endregion
}