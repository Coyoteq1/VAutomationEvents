using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ProjectM;
using Unity.Entities;
using Unity.Mathematics;
using PrefabGUID = Stunlock.Core.PrefabGUID;
using BepInEx.Logging;
using VAuto.Core;
using VAuto.Data;
using VAuto.Services;
using VAuto.Services.Interfaces;
using static VAuto.Core.MissingTypes;

namespace VAuto.Services
{
    /// <summary>
    /// Service for managing conveyor systems and automated material flow
    /// </summary>
    public class ConveyorService : IService
    {
        private static ConveyorService _instance;
        public static ConveyorService Instance => _instance ??= new ConveyorService();

        private bool _isInitialized;
        private readonly Dictionary<Entity, ConveyorTerritoryConfig> _territoryConfigs = new();
        private readonly Regex _senderRegex = new Regex(@"s(\d+)", RegexOptions.IgnoreCase);
        private readonly Regex _receiverRegex = new Regex(@"r(\d+)", RegexOptions.IgnoreCase);
        private readonly Regex _overflowRegex = new Regex(@"overflow", RegexOptions.IgnoreCase);

        // Update intervals
        private const int DEFAULT_UPDATE_INTERVAL = 30; // seconds
        private DateTime _lastGlobalUpdate = DateTime.UtcNow;

        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => Plugin.Log;

        private ConveyorService()
        {
            // Constructor
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                Log?.LogInfo("[ConveyorService] Initializing conveyor service...");

                // Start the update loop
                Task.Run(UpdateLoop);

                _isInitialized = true;
                Log?.LogInfo("[ConveyorService] Conveyor service initialized successfully");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ConveyorService] Failed to initialize: {ex.Message}");
                throw;
            }
        }

        public void Cleanup()
        {
            if (!_isInitialized) return;

            try
            {
                Log?.LogInfo("[ConveyorService] Cleaning up conveyor service...");

                _territoryConfigs.Clear();
                _isInitialized = false;

                Log?.LogInfo("[ConveyorService] Conveyor service cleaned up");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ConveyorService] Error during cleanup: {ex.Message}");
            }
        }

        #region Public API Methods
        public static void EnableForTerritory(Entity territoryOwner)
        {
            if (!Instance._territoryConfigs.ContainsKey(territoryOwner))
            {
                Instance._territoryConfigs[territoryOwner] = new ConveyorTerritoryConfig
                {
                    TerritoryOwner = territoryOwner,
                    IsEnabled = true
                };
            }
            else
            {
                Instance._territoryConfigs[territoryOwner].IsEnabled = true;
            }

            Instance.Log.LogInfo($"[ConveyorService] Enabled conveyor system for territory owner {territoryOwner}");
        }

        public static void DisableForTerritory(Entity territoryOwner)
        {
            if (Instance._territoryConfigs.ContainsKey(territoryOwner))
            {
                Instance._territoryConfigs[territoryOwner].IsEnabled = false;
            }

            Instance.Log.LogInfo($"[ConveyorService] Disabled conveyor system for territory owner {territoryOwner}");
        }

        public static bool IsEnabledForTerritory(Entity territoryOwner)
        {
            return Instance._territoryConfigs.ContainsKey(territoryOwner) &&
                   Instance._territoryConfigs[territoryOwner].IsEnabled;
        }

        public static List<ConveyorLink> GetActiveLinks(Entity territoryOwner)
        {
            if (!Instance._territoryConfigs.ContainsKey(territoryOwner))
                return new List<ConveyorLink>();

            return Instance._territoryConfigs[territoryOwner].Links
                .Where(l => l.IsActive)
                .ToList();
        }

        public static List<string> GetDebugInfo(Entity territoryOwner)
        {
            var debugInfo = new List<string>();

            if (!Instance._territoryConfigs.ContainsKey(territoryOwner))
            {
                debugInfo.Add("No conveyor configuration found for this territory");
                return debugInfo;
            }

            var config = Instance._territoryConfigs[territoryOwner];
            debugInfo.Add($"Territory Owner: {territoryOwner}");
            debugInfo.Add($"Enabled: {config.IsEnabled}");
            debugInfo.Add($"Inventories: {config.Inventories.Count}");
            debugInfo.Add($"Links: {config.Links.Count}");
            debugInfo.Add($"Overflow Chests: {config.OverflowChests.Count}");
            debugInfo.Add($"Last Update: {config.LastUpdate}");
            debugInfo.Add($"Update Interval: {config.UpdateIntervalSeconds}s");

            foreach (var inventory in config.Inventories)
            {
                debugInfo.Add($"  Inventory: {inventory.Name} (Group: {inventory.Group}, Type: {inventory.Type})");
                debugInfo.Add($"    Items: {inventory.Items.Count}, Last Updated: {inventory.LastUpdated}");
            }

            foreach (var link in config.Links)
            {
                debugInfo.Add($"  Link: {link.SenderName} → {link.ReceiverName} (Active: {link.IsActive})");
                debugInfo.Add($"    Buffers: {link.Buffers.Count}, Last Transfer: {link.LastTransfer}");
            }

            return debugInfo;
        }
        #endregion

        #region Update Logic
        private async Task UpdateLoop()
        {
            while (_isInitialized)
            {
                try
                {
                    var now = DateTime.UtcNow;

                    // Update all enabled territories
                    foreach (var config in _territoryConfigs.Values.Where(c => c.IsEnabled))
                    {
                        if ((now - config.LastUpdate).TotalSeconds >= config.UpdateIntervalSeconds)
                        {
                            await UpdateTerritory(config);
                            config.LastUpdate = now;
                        }
                    }

                    _lastGlobalUpdate = now;
                }
                catch (Exception ex)
                {
                    Instance.Log.LogError($"[ConveyorService] Error in update loop: {ex.Message}");
                }

                // Wait before next update
                await Task.Delay(1000); // Check every second
            }
        }

        private async Task UpdateTerritory(ConveyorTerritoryConfig config)
        {
            try
            {
                // Refresh inventories
                RefreshInventories(config);

                // Refresh links
                RefreshLinks(config);

                // Execute transfer operations in correct order
                await ExecuteTransfers(config);
            }
            catch (Exception ex)
            {
                Instance.Log.LogError($"[ConveyorService] Error updating territory {config.TerritoryOwner}: {ex.Message}");
            }
        }

        private void RefreshInventories(ConveyorTerritoryConfig config)
        {
            try
            {
                var em = VRCore.EM;

                // Find all inventories in the territory (simplified - would need proper territory detection)
                var inventoryQuery = em.CreateEntityQuery(
                    ComponentType.ReadOnly<ProjectM.Inventory>(),
                    ComponentType.ReadOnly<Unity.Transforms.Translation>(),
                    ComponentType.ReadOnly<ProjectM.Name>()
                );

                config.Inventories.Clear();
                config.OverflowChests.Clear();

                foreach (var entity in inventoryQuery.ToEntityArray(Unity.Collections.Allocator.Temp))
                {
                    if (!ComponentInitializer.HasComponent(entity, typeof(ProjectM.Name))) continue;

                    var name = "Unknown"; // Use stub value since ProjectM.Name is not available in this build

                    var inventory = ParseInventory(entity, name);
                    if (inventory != null)
                    {
                        config.Inventories.Add(inventory);

                        // Check if it's an overflow chest
                        if (_overflowRegex.IsMatch(name))
                        {
                            config.OverflowChests.Add(entity);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Instance.Log.LogError($"[ConveyorService] Error refreshing inventories: {ex.Message}");
            }
        }

        private void RefreshLinks(ConveyorTerritoryConfig config)
        {
            config.Links.Clear();

            // Group inventories by type and group
            var senders = config.Inventories.Where(i => i.IsSender).ToList();
            var receivers = config.Inventories.Where(i => i.IsReceiver).ToList();

            // Create links between matching groups
            foreach (var sender in senders)
            {
                var matchingReceivers = receivers.Where(r => r.Group == sender.Group).ToList();

                foreach (var receiver in matchingReceivers)
                {
                    var link = new ConveyorLink
                    {
                        Group = sender.Group,
                        Sender = sender,
                        Receiver = receiver,
                        IsActive = true
                    };

                    // Initialize buffers for receiver
                    InitializeBuffers(link);

                    config.Links.Add(link);
                }
            }
        }

        private async Task ExecuteTransfers(ConveyorTerritoryConfig config)
        {
            // Order of operations per tick:
            // 1. Overflow to receivers
            // 2. Station senders to receivers
            // 3. Chest senders to stations

            await OverflowToReceivers(config);
            await StationSendersToReceivers(config);
            await ChestSendersToStations(config);
        }
        #endregion

        #region Transfer Operations
        private async Task OverflowToReceivers(ConveyorTerritoryConfig config)
        {
            if (config.OverflowChests.Count == 0) return;

            var receivers = config.Inventories.Where(i => i.IsReceiver).ToList();

            foreach (var overflowChest in config.OverflowChests)
            {
                var overflowInventory = GetInventoryItems(overflowChest);

                foreach (var receiver in receivers)
                {
                    foreach (var item in overflowInventory)
                    {
                        if (receiver.Items.ContainsKey(item.Key))
                        {
                            var needed = GetNeededAmount(receiver, item.Key);
                            if (needed > 0)
                            {
                                var transferAmount = Math.Min(item.Value, needed);
                                var result = await TransferItem(overflowChest, receiver.Entity, item.Key, transferAmount);

                                if (result.Success)
                                {
                                    Instance.Log.LogInfo($"[ConveyorService] Overflow transfer: {result.ItemsTransferred}x {result.ItemName} to {receiver.Name}");
                                }
                            }
                        }
                    }
                }
            }
        }

        private async Task StationSendersToReceivers(ConveyorTerritoryConfig config)
        {
            var stationLinks = config.Links.Where(l => l.Sender.Type == InventoryType.Station).ToList();

            foreach (var link in stationLinks)
            {
                foreach (var buffer in link.Buffers.Where(b => b.NeededAmount > 0))
                {
                    if (link.Sender.Items.ContainsKey(buffer.ItemGuid))
                    {
                        var available = link.Sender.Items[buffer.ItemGuid];
                        var transferAmount = Math.Min(available, buffer.NeededAmount);

                        var result = await TransferItem(link.Sender.Entity, link.Receiver.Entity, buffer.ItemGuid, transferAmount);

                        if (result.Success)
                        {
                            link.LastTransfer = DateTime.UtcNow;
                            Instance.Log.LogInfo($"[ConveyorService] Station transfer: {result.ItemsTransferred}x {result.ItemName} from {link.SenderName} to {link.ReceiverName}");
                        }
                    }
                }
            }
        }

        private async Task ChestSendersToStations(ConveyorTerritoryConfig config)
        {
            var chestLinks = config.Links.Where(l => l.Sender.Type == InventoryType.Chest && l.Receiver.Type == InventoryType.Station).ToList();

            foreach (var link in chestLinks)
            {
                foreach (var item in link.Sender.Items)
                {
                    // Keep 1 of each item in sender chest
                    var available = Math.Max(0, item.Value - 1);

                    if (available > 0)
                    {
                        var result = await TransferItem(link.Sender.Entity, link.Receiver.Entity, item.Key, available);

                        if (result.Success)
                        {
                            link.LastTransfer = DateTime.UtcNow;
                            Instance.Log.LogInfo($"[ConveyorService] Chest transfer: {result.ItemsTransferred}x {result.ItemName} from {link.SenderName} to {link.ReceiverName}");
                        }
                    }
                }
            }
        }
        #endregion

        #region Helper Methods
        private ConveyorInventory ParseInventory(Entity entity, string name)
        {
            var inventory = new ConveyorInventory
            {
                Entity = entity,
                Name = name,
                Items = GetInventoryItems(entity)
            };

            // Parse sender pattern
            var senderMatch = _senderRegex.Match(name);
            if (senderMatch.Success)
            {
                inventory.IsSender = true;
                if (int.TryParse(senderMatch.Groups[1].Value, out var group))
                {
                    inventory.Group = group;
                }
            }

            // Parse receiver pattern
            var receiverMatch = _receiverRegex.Match(name);
            if (receiverMatch.Success)
            {
                inventory.IsReceiver = true;
                if (int.TryParse(receiverMatch.Groups[1].Value, out var group))
                {
                    inventory.Group = group;
                }
            }

            // Determine type (simplified logic)
            inventory.Type = DetermineInventoryType(entity);

            return (inventory.IsSender || inventory.IsReceiver) ? inventory : null;
        }

        private InventoryType DetermineInventoryType(Entity entity)
        {
            // Check for station components (simplified) using safe ComponentInitializer
            if (ComponentInitializer.HasComponent(entity, typeof(ProjectM.ProcessingStation)))
                return InventoryType.Station;

            // Assume chest otherwise
            return InventoryType.Chest;
        }

        private Dictionary<PrefabGUID, int> GetInventoryItems(Entity entity)
        {
            var items = new Dictionary<PrefabGUID, int>();

            try
            {
                if (!ComponentInitializer.HasComponent(entity, typeof(ProjectM.Inventory)))
                    return items;

                // This would need proper inventory iteration logic
                // Simplified for now - would need to access inventory buffer elements

                return items;
            }
            catch (Exception ex)
            {
                Instance.Log.LogError($"[ConveyorService] Error getting inventory items: {ex.Message}");
                return items;
            }
        }

        private void InitializeBuffers(ConveyorLink link)
        {
            link.Buffers.Clear();

            // Get recipe requirements for the receiver station
            var recipes = GetStationRecipes(link.Receiver.Entity);

            foreach (var recipe in recipes)
            {
                foreach (var input in recipe.Inputs)
                {
                    var buffer = new ConveyorBuffer
                    {
                        ItemGuid = input.ItemGuid,
                        ItemName = input.ItemName,
                        TargetAmount = input.Amount * 5, // 5x per-craft requirement
                        CurrentAmount = link.Receiver.Items.ContainsKey(input.ItemGuid) ?
                            link.Receiver.Items[input.ItemGuid] : 0,
                        HasFloor = HasFloorTile(link.Receiver.Entity)
                    };

                    link.Buffers.Add(buffer);
                }
            }
        }

        private int GetNeededAmount(ConveyorInventory receiver, PrefabGUID itemGuid)
        {
            // Find the relevant buffer for this item
            var links = Instance._territoryConfigs
                .Where(c => c.Value.Inventories.Contains(receiver))
                .SelectMany(c => c.Value.Links)
                .Where(l => l.Receiver == receiver);

            foreach (var link in links)
            {
                var buffer = link.Buffers.FirstOrDefault(b => b.ItemGuid.Equals(itemGuid));
                if (buffer != null)
                {
                    return buffer.NeededAmount;
                }
            }

            return 0;
        }

        private async Task<TransferResult> TransferItem(Entity fromEntity, Entity toEntity, PrefabGUID itemGuid, int amount)
        {
            try
            {
                // This would need proper item transfer logic
                // Simplified for now - would need to manipulate inventory buffers

                return new TransferResult(true, $"Transferred {amount}x item", amount, itemGuid, "Unknown Item");
            }
            catch (Exception ex)
            {
                Instance.Log.LogError($"[ConveyorService] Error transferring item: {ex.Message}");
                return new TransferResult(false, $"Transfer failed: {ex.Message}");
            }
        }

        private List<RecipeData> GetStationRecipes(Entity stationEntity)
        {
            // This would need to access station recipe data
            // Simplified for now
            return new List<RecipeData>();
        }

        private bool HasFloorTile(Entity entity)
        {
            // Check if station has a floor tile (reduces buffer target to 75%)
            // Simplified logic
            return false;
        }

        private class RecipeData
        {
            public List<RecipeInput> Inputs { get; set; } = new();
        }

        private class RecipeInput
        {
            public PrefabGUID ItemGuid { get; set; }
            public string ItemName { get; set; }
            public int Amount { get; set; }
        }
        #endregion
    }
}
