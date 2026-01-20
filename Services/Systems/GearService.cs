using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using VAuto.Data;
using VAuto.Services.Interfaces;

namespace VAuto.Services.Systems
{
    public class GearService : IGearService
    {
        private readonly IPrefabResolverService _prefabs;
        private readonly IPlayerInventoryService _inventory;
        private readonly ILogger<GearService> _logger;

        // per-player toggle for auto-equip on enter
        private readonly ConcurrentDictionary<string, bool> _playerAutoEquip = new();

        // per-player saved gear snapshots (stacked)
        private readonly ConcurrentDictionary<string, List<EquippedItemSnapshot>> _savedGear = new();

        // per-player active swap sessions: playerId -> sessionId -> snapshot
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, List<EquippedItemSnapshot>>> _swapSessions
            = new();

        public GearService(
            IPrefabResolverService prefabs,
            IPlayerInventoryService inventory,
            ILogger<GearService> logger)
        {
            _prefabs = prefabs;
            _inventory = inventory;
            _logger = logger;
        }

        public bool IsAutoEquipEnabledForPlayer(string playerId)
        {
            return _playerAutoEquip.TryGetValue(playerId, out var enabled) && enabled;
        }

        public void SetAutoEquipForPlayer(string playerId, bool enabled)
        {
            _playerAutoEquip[playerId] = enabled;
            _logger.LogInformation("AutoEquip for player {Player} set to {Enabled}", playerId, enabled);
        }

        public bool EquipGearForPlayer(string playerId, IEnumerable<GearEntry> gearList)
        {
            if (gearList == null) return false;
            var toEquip = gearList.Where(g => !string.IsNullOrWhiteSpace(g.ItemName) && g.Count > 0).ToList();
            if (!toEquip.Any()) return false;

            SaveCurrentGearSnapshot(playerId);

            var equippedAny = false;
            foreach (var gear in toEquip)
            {
                if (!_prefabs.ResolveItemName(gear.ItemName))
                {
                    _logger.LogWarning("Cannot equip unknown item {Item} for player {Player}", gear.ItemName, playerId);
                    continue;
                }

                var success = _inventory.EquipItem(playerId, gear.ItemName, gear.Count);
                if (success)
                {
                    equippedAny = true;
                    _logger.LogInformation("Equipped {Count}x {Item} for player {Player}", gear.Count, gear.ItemName, playerId);
                }
                else
                {
                    _logger.LogWarning("Failed to equip {Item} for player {Player}", gear.ItemName, playerId);
                }
            }

            return equippedAny;
        }

        // --- Swap session API (separate flow) ---
        public bool StartSwapSession(string playerId, string sessionId, IEnumerable<GearEntry> swapGear)
        {
            if (string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(sessionId) || swapGear == null)
                return false;

            // Save snapshot for this session
            var snapshot = GetCurrentEquippedSnapshot(playerId);
            var playerSessions = _swapSessions.GetOrAdd(playerId, _ => new ConcurrentDictionary<string, List<EquippedItemSnapshot>>());
            playerSessions[sessionId] = snapshot;

            // Equip swap gear
            var swapped = EquipGearForPlayer(playerId, swapGear);
            if (swapped)
                _logger.LogInformation("Started swap session {Session} for player {Player}", sessionId, playerId);
            else
                _logger.LogWarning("Swap session {Session} for player {Player} equipped no items", sessionId, playerId);

            return swapped;
        }

        public bool EndSwapSession(string playerId, string sessionId)
        {
            if (!_swapSessions.TryGetValue(playerId, out var sessions)) return false;
            if (!sessions.TryRemove(sessionId, out var snapshot)) return false;

            // Revert to snapshot saved for this session
            _inventory.ClearEquippedItems(playerId);
            foreach (var item in snapshot)
                _inventory.EquipItem(playerId, item.ItemName, item.Count);

            _logger.LogInformation("Ended swap session {Session} for player {Player}", sessionId, playerId);
            return true;
        }

        public IEnumerable<string> GetActiveSwapSessions(string playerId)
        {
            if (!_swapSessions.TryGetValue(playerId, out var sessions)) return Enumerable.Empty<string>();
            return sessions.Keys.ToList();
        }

        public void RevertPlayerGear(string playerId)
        {
            // If there are active swap sessions, revert the most recent session snapshot first
            if (_swapSessions.TryGetValue(playerId, out var sessions) && sessions.Any())
            {
                // revert all sessions in LIFO order
                var keys = sessions.Keys.ToList();
                foreach (var key in keys)
                {
                    EndSwapSession(playerId, key);
                }
                return;
            }

            // Otherwise revert the last saved global snapshot
            if (!_savedGear.TryRemove(playerId, out var snapshot))
            {
                _logger.LogDebug("No saved gear to revert for player {Player}", playerId);
                return;
            }

            _inventory.ClearEquippedItems(playerId);
            foreach (var item in snapshot)
                _inventory.EquipItem(playerId, item.ItemName, item.Count);

            _logger.LogInformation("Reverted gear for player {Player}", playerId);
        }

        // --- Helpers ---
        private void SaveCurrentGearSnapshot(string playerId)
        {
            try
            {
                var current = GetCurrentEquippedSnapshot(playerId);
                _savedGear[playerId] = current;
                _logger.LogDebug("Saved gear snapshot for player {Player} ({Count} items)", playerId, current.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save gear snapshot for player {Player}", playerId);
            }
        }

        private List<EquippedItemSnapshot> GetCurrentEquippedSnapshot(string playerId)
        {
            try
            {
                return _inventory.GetEquippedItems(playerId)
                    .Select(i => new EquippedItemSnapshot { ItemName = i.ItemName, Count = i.Count })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read equipped items for player {Player}", playerId);
                return new List<EquippedItemSnapshot>();
            }
        }

        private class EquippedItemSnapshot
        {
            public string ItemName { get; set; }
            public int Count { get; set; }
        }
    }
}