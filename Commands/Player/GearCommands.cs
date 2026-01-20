using System;
using System.Collections.Generic;
using VampireCommandFramework;
using VAuto.Services.Interfaces;
using VAuto.Services;
using VAuto.Data;
using VAuto.EventAdapters;

namespace VAuto.Commands.Player
{
    /// <summary>
    /// Gear management commands for players
    /// </summary>
    public static class GearCommands
    {
        private static IGearService _gear => ServiceManager.GetService<IGearService>();

        [Command("autoequip toggle", "Toggle auto-equip on zone entry", adminOnly: false)]
        public static void ToggleAutoEquip(ChatCommandContext ctx, string playerId = null)
        {
            try
            {
                var id = playerId ?? ctx.Event.User.PlatformId.ToString();
                var enabled = !_gear.IsAutoEquipEnabledForPlayer(id);
                _gear.SetAutoEquipForPlayer(id, enabled);
                ctx.Reply($"AutoEquip for {id} is now {(enabled ? "enabled" : "disabled")}.");
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error toggling auto-equip: {ex.Message}");
            }
        }

        [Command("swap start", "swap start <sessionId> <item1>:<count>,<item2>:<count>", adminOnly: false)]
        public static void StartSwap(ChatCommandContext ctx, string sessionId, string itemsCsv)
        {
            try
            {
                var playerId = ctx.Event.User.PlatformId.ToString();
                if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(itemsCsv))
                {
                    ctx.Reply("Usage: swap start <sessionId> <item1>:<count>,<item2>:<count>");
                    return;
                }

                var gear = ParseGearCsv(itemsCsv);
                var started = _gear.StartSwapSession(playerId, sessionId, gear);
                ctx.Reply(started ? $"Swap session {sessionId} started." : "Swap failed (check prefabs or items).");
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error starting swap: {ex.Message}");
            }
        }

        [Command("swap end", "swap end <sessionId>", adminOnly: false)]
        public static void EndSwap(ChatCommandContext ctx, string sessionId)
        {
            try
            {
                var playerId = ctx.Event.User.PlatformId.ToString();
                var ended = _gear.EndSwapSession(playerId, sessionId);
                ctx.Reply(ended ? $"Swap session {sessionId} ended and gear reverted." : $"No active swap session {sessionId} found.");
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error ending swap: {ex.Message}");
            }
        }

        [Command("swap revert", "Revert all gear (swap sessions and snapshots)", adminOnly: false)]
        public static void RevertSwap(ChatCommandContext ctx)
        {
            try
            {
                var playerId = ctx.Event.User.PlatformId.ToString();
                _gear.RevertPlayerGear(playerId);
                ctx.Reply("Reverted gear (swap sessions and snapshots cleared).");
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error reverting gear: {ex.Message}");
            }
        }

        private static IEnumerable<GearEntry> ParseGearCsv(string csv)
        {
            var list = new List<GearEntry>();
            var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var kv = p.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (kv.Length == 2 && int.TryParse(kv[1], out var count))
                {
                    list.Add(new GearEntry { ItemName = kv[0].Trim(), Count = count });
                }
            }
            return list;
        }
    }
}