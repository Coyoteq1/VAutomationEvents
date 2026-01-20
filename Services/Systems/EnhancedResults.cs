using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Enhanced execution result with additional context and metadata
    /// </summary>
    public class EnhancedExecutionResult : ExecutionResult
    {
        public string ContextId { get; set; }
        public ulong PlatformId { get; set; }
        public string OperationType { get; set; }
        public Dictionary<string, object> EnhancedMetadata { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public ExecutionStatus Status { get; set; }
    }

    /// <summary>
    /// Execution status enumeration
    /// </summary>
    public enum ExecutionStatus
    {
        Started,
        InProgress,
        Completed,
        Failed,
        Cancelled,
        Timeout
    }

    /// <summary>
    /// Enhanced automation context for tracking operations
    /// </summary>
    public class EnhancedAutomationContext
    {
        public string ContextId { get; set; }
        public ulong PlatformId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> ContextData { get; set; } = new();
        public List<string> ActiveOperations { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public string CurrentZone { get; set; }
        public Dictionary<string, Entity> TrackedEntities { get; set; } = new();
    }

    /// <summary>
    /// Execution log entry for tracking operations
    /// </summary>
    public class ExecutionLogEntry
    {
        public string EntryId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string ContextId { get; set; }
        public string OperationType { get; set; }
        public string OperationId { get; set; }
        public ExecutionStatus Status { get; set; }
        public string Message { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public Exception Exception { get; set; }
    }

    #region Zone Management Results

    public class ZoneExecutionResult : ExecutionResult
    {
        public string ZoneId { get; set; }
        public float3 Position { get; set; }
        public float Radius { get; set; }
        public int EntitiesAffected { get; set; }
        public ZoneType ZoneType { get; set; }
    }

    public enum ZoneType
    {
        Arena,
        SafeZone,
        CombatZone,
        ResourceZone,
        Custom
    }

    #endregion

    #region Spawn Management Results

    public class MobSpawnResult : ExecutionResult
    {
        public string MobPrefabId { get; set; }
        public float3 SpawnPosition { get; set; }
        public int Quantity { get; set; }
        public List<Entity> SpawnedEntities { get; set; } = new();
        public bool WasBoss { get; set; }
    }

    public class BossSpawnResult : MobSpawnResult
    {
        public string BossName { get; set; }
        public int BossLevel { get; set; }
        public List<string> Abilities { get; set; } = new();
        public Dictionary<string, object> BossMetadata { get; set; } = new();
    }

    #endregion

    #region Loot and Item Management Results

    public class LootConfigurationResult : ExecutionResult
    {
        public string ContainerId { get; set; }
        public int ItemsConfigured { get; set; }
        public List<string> ItemPrefabIds { get; set; } = new();
        public LootType LootType { get; set; }
    }

    public enum LootType
    {
        Standard,
        Boss,
        Event,
        Custom
    }

    #endregion

    #region Map and Effect Results

    public class MapEffectResult : ExecutionResult
    {
        public string EffectId { get; set; }
        public float3 EffectPosition { get; set; }
        public float Duration { get; set; }
        public float EffectRadius { get; set; }
        public List<Entity> AffectedEntities { get; set; } = new();
    }

    #endregion

    #region Castle Management Results

    public class CastleExecutionResult : ExecutionResult
    {
        public string CastleId { get; set; }
        public CastleOperation Operation { get; set; }
        public List<string> ModifiedStructures { get; set; } = new();
        public Dictionary<string, object> CastleData { get; set; } = new();
    }

    public enum CastleOperation
    {
        Build,
        Destroy,
        Modify,
        Repair,
        Upgrade
    }

    #endregion

    #region Logistics Results

    public class LogisticsExecutionResult : ExecutionResult
    {
        public string LogisticsId { get; set; }
        public LogisticsOperation Operation { get; set; }
        public int ItemsTransferred { get; set; }
        public float TransferTime { get; set; }
    }

    public class LogisticsTransferResult : LogisticsExecutionResult
    {
        public string SourceCastle { get; set; }
        public string DestinationCastle { get; set; }
        public string Item { get; set; }
        public int Amount { get; set; }
        public List<string> TransferredItemIds { get; set; } = new();
        public Dictionary<string, int> TransferQuantities { get; set; } = new();
    }

    public class LogisticsRefillResult : LogisticsExecutionResult
    {
        public string TargetContainerId { get; set; }
        public string Item { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
        public List<string> RefilledItems { get; set; } = new();
        public Dictionary<string, int> RefillQuantities { get; set; } = new();
        public bool WasFullyRefilled { get; set; }
    }

    public class LogisticsRepairResult : LogisticsExecutionResult
    {
        public string StructureId { get; set; }
        public string Castle { get; set; }
        public string EquipmentId { get; set; }
        public float RepairAmount { get; set; }
        public float CurrentDurability { get; set; }
        public float MaxDurability { get; set; }
        public bool WasFullyRepaired { get; set; }
    }

    public class LogisticsBalanceResult : LogisticsExecutionResult
    {
        public List<string> BalancedContainers { get; set; } = new();
        public Dictionary<string, int> BalancedQuantities { get; set; } = new();
        public int TotalItemsMoved { get; set; }
        
        // Additional properties expected by execution engine
        public string SourceCastle { get; set; }
        public string TargetCastle { get; set; }
        public string Resource { get; set; }
        public float Amount { get; set; }
    }

    public enum LogisticsOperation
    {
        Transfer,
        Refill,
        Repair,
        Balance,
        Sort
    }

    #endregion
}
