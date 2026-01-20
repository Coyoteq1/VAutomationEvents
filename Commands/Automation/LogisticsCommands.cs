using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;
using VAuto.Core;
using VAuto.Services;
using static VAuto.Core.MissingTypes;

namespace VAuto.Commands.Automation
{
    /// <summary>
    /// Logistics Commands - Automated material flow and conveyor systems
    /// </summary>
    public static class LogisticsCommands
    {
        #region Conveyor System
        [Command("logistics", "logistics <subcommand> [args]", "Logistics and automation systems management", adminOnly: false)]
        public static void LogisticsCommand(ChatCommandContext ctx, string subcommand, string args = "")
        {
            try
            {
                switch (subcommand.ToLower())
                {
                    case "conveyor":
                        ConveyorCommand(ctx, args);
                        break;
                    case "co":
                        ConveyorCommand(ctx, args);
                        break;
                    default:
                        LogisticsHelp(ctx);
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in logistics command: {ex.Message}");
                ctx.Reply("Error executing logistics command.");
            }
        }

        [Command("l", "l <subcommand> [args]", "Logistics shortcut command", adminOnly: false)]
        public static void LogisticsShortcutCommand(ChatCommandContext ctx, string subcommand, string args = "")
        {
            LogisticsCommand(ctx, subcommand, args);
        }
        #endregion

        #region Implementation Methods
        private static void ConveyorCommand(ChatCommandContext ctx, string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                ShowConveyorStatus(ctx);
                return;
            }

            var parts = args.Split(' ');
            var action = parts[0].ToLower();

            switch (action)
            {
                case "enable":
                    EnableConveyor(ctx);
                    break;
                case "disable":
                    DisableConveyor(ctx);
                    break;
                case "status":
                    ShowConveyorStatus(ctx);
                    break;
                case "list":
                    ListConveyorLinks(ctx);
                    break;
                case "debug":
                    DebugConveyor(ctx);
                    break;
                default:
                    ctx.Reply("Usage: .logistics conveyor [enable|disable|status|list|debug]");
                    break;
            }
        }

        private static void EnableConveyor(ChatCommandContext ctx)
        {
            // Check if player owns the territory
            var playerEntity = ctx.Event.SenderCharacterEntity;
            var territoryOwner = GetTerritoryOwner(playerEntity);

            if (territoryOwner != ctx.Event.SenderUserEntity)
            {
                ctx.Reply("❌ You can only enable conveyor systems on territories you personally own.");
                return;
            }

            // Enable conveyor system for this territory
            ConveyorService.EnableForTerritory(territoryOwner);
            ctx.Reply("✅ Conveyor system enabled for your territory.");
        }

        private static void DisableConveyor(ChatCommandContext ctx)
        {
            var playerEntity = ctx.Event.SenderCharacterEntity;
            var territoryOwner = GetTerritoryOwner(playerEntity);

            ConveyorService.DisableForTerritory(territoryOwner);
            ctx.Reply("❌ Conveyor system disabled for your territory.");
        }

        private static void ShowConveyorStatus(ChatCommandContext ctx)
        {
            var playerEntity = ctx.Event.SenderCharacterEntity;
            var territoryOwner = GetTerritoryOwner(playerEntity);

            var enabled = ConveyorService.IsEnabledForTerritory(territoryOwner);
            var status = enabled ? "✅ ENABLED" : "❌ DISABLED";

            ctx.Reply($"Conveyor System Status: {status}");

            if (enabled)
            {
                var links = ConveyorService.GetActiveLinks(territoryOwner);
                ctx.Reply($"Active conveyor links: {links.Count}");

                // Show some basic stats
                var groups = links.GroupBy(l => l.Group).Select(g => g.Key).ToList();
                ctx.Reply($"Active groups: {string.Join(", ", groups)}");
            }
        }

        private static void ListConveyorLinks(ChatCommandContext ctx)
        {
            var playerEntity = ctx.Event.SenderCharacterEntity;
            var territoryOwner = GetTerritoryOwner(playerEntity);

            var links = ConveyorService.GetActiveLinks(territoryOwner);

            if (links.Count == 0)
            {
                ctx.Reply("No active conveyor links found.");
                return;
            }

            ctx.Reply("=== Active Conveyor Links ===");
            foreach (var link in links)
            {
                ctx.Reply($"Group {link.Group}: {link.SenderName} → {link.ReceiverName}");
            }
        }

        private static void DebugConveyor(ChatCommandContext ctx)
        {
            // Admin only debug command
            if (!ctx.Event.SenderUserEntity.Has<AdminAuth>()) // Assuming AdminAuth component exists
            {
                ctx.Reply("❌ Debug command requires admin privileges.");
                return;
            }

            var playerEntity = ctx.Event.SenderCharacterEntity;
            var territoryOwner = GetTerritoryOwner(playerEntity);

            var debugInfo = ConveyorService.GetDebugInfo(territoryOwner);
            ctx.Reply("=== Conveyor Debug Info ===");
            foreach (var info in debugInfo)
            {
                ctx.Reply(info);
            }
        }

        private static void LogisticsHelp(ChatCommandContext ctx)
        {
            ctx.Reply("🚀 Logistics Commands:");
            ctx.Reply("  .logistics conveyor - Conveyor system management");
            ctx.Reply("  .logistics conveyor enable - Enable conveyor system");
            ctx.Reply("  .logistics conveyor disable - Disable conveyor system");
            ctx.Reply("  .logistics conveyor status - Show conveyor status");
            ctx.Reply("  .logistics conveyor list - List active conveyor links");
            ctx.Reply("  .logistics conveyor debug - Debug information (admin only)");
            ctx.Reply("");
            ctx.Reply("Shortcut: .l co [args]");
        }

        #endregion

        #region Helper Methods
        private static Entity GetTerritoryOwner(Entity playerEntity)
        {
            // Get the territory owner for the player's current location
            // This would need to be implemented based on V Rising's territory system
            try
            {
                var em = VRCore.EM;

                // Get player's position
                if (!em.HasComponent<Translation>(playerEntity))
                    return Entity.Null;

                var position = em.GetComponentData<Translation>(playerEntity).Value;

                // Find castle heart in the area (simplified logic)
                var castleQuery = em.CreateEntityQuery(
                    ComponentType.ReadOnly<CastleHeart>(),
                    ComponentType.ReadOnly<Translation>(),
                    ComponentType.ReadOnly<UserOwner>()
                );

                var closestCastle = Entity.Null;
                var closestDistance = float.MaxValue;

                foreach (var castleEntity in castleQuery.ToEntityArray(Unity.Collections.Allocator.Temp))
                {
                    var castlePos = em.GetComponentData<Translation>(castleEntity).Value;
                    var distance = Unity.Mathematics.math.distance(position, castlePos);

                    if (distance < closestDistance && distance < 100f) // Within territory range
                    {
                        closestDistance = distance;
                        closestCastle = castleEntity;
                    }
                }

                if (closestCastle != Entity.Null && em.HasComponent<UserOwner>(closestCastle))
                {
                    return em.GetComponentData<UserOwner>(closestCastle).Owner;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error getting territory owner: {ex.Message}");
            }

            return Entity.Null;
        }
        #endregion
    }
}
