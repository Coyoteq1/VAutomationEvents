using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace VAuto.Data
{
    /// <summary>
    /// Represents a conveyor inventory (chest or station)
    /// </summary>
    public class ConveyorInventory
    {
        public Entity Entity { get; set; }
        public string Name { get; set; }
        public InventoryType Type { get; set; }
        public int Group { get; set; }
        public bool IsSender { get; set; }
        public bool IsReceiver { get; set; }
        public float3 Position { get; set; }
        public Dictionary<PrefabGUID, int> Items { get; set; } = new();
        public DateTime LastUpdated { get; set; }

        public ConveyorInventory()
        {
            LastUpdated = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Represents a buffer for recipe inputs
    /// </summary>
    public class ConveyorBuffer
    {
        public PrefabGUID ItemGuid { get; set; }
        public string ItemName { get; set; }
        public int TargetAmount { get; set; }
        public int CurrentAmount { get; set; }
        public bool HasFloor { get; set; } // Reduces target to 75%

        public int EffectiveTarget => HasFloor ? (int)(TargetAmount * 0.75f) : TargetAmount;
        public int NeededAmount => Math.Max(0, EffectiveTarget - CurrentAmount);
        public bool IsSatisfied => CurrentAmount >= EffectiveTarget;
    }

    /// <summary>
    /// Represents a conveyor link between sender and receiver
    /// </summary>
    public class ConveyorLink
    {
        public int Group { get; set; }
        public ConveyorInventory Sender { get; set; }
        public ConveyorInventory Receiver { get; set; }
        public List<ConveyorBuffer> Buffers { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime LastTransfer { get; set; }

        public string SenderName => Sender?.Name ?? "Unknown";
        public string ReceiverName => Receiver?.Name ?? "Unknown";

        public ConveyorLink()
        {
            LastTransfer = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Territory-specific conveyor configuration
    /// </summary>
    public class ConveyorTerritoryConfig
    {
        public Entity TerritoryOwner { get; set; }
        public bool IsEnabled { get; set; }
        public List<ConveyorInventory> Inventories { get; set; } = new();
        public List<ConveyorLink> Links { get; set; } = new();
        public List<Entity> OverflowChests { get; set; } = new();
        public DateTime LastUpdate { get; set; }
        public int UpdateIntervalSeconds { get; set; } = 30; // Default 30 seconds

        public ConveyorTerritoryConfig()
        {
            LastUpdate = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Transfer operation result
    /// </summary>
    public class TransferResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int ItemsTransferred { get; set; }
        public PrefabGUID ItemGuid { get; set; }
        public string ItemName { get; set; }

        public TransferResult(bool success, string message = "", int itemsTransferred = 0, PrefabGUID itemGuid = default, string itemName = "")
        {
            Success = success;
            Message = message;
            ItemsTransferred = itemsTransferred;
            ItemGuid = itemGuid;
            ItemName = itemName;
        }
    }

    /// <summary>
    /// Types of inventories that can participate in conveyor systems
    /// </summary>
    public enum InventoryType
    {
        Chest,
        Station,
        Unknown
    }

    /// <summary>
    /// Transfer operation types
    /// </summary>
    public enum TransferType
    {
        OverflowToReceiver,
        StationSenderToReceiver,
        ChestSenderToStation
    }
}
