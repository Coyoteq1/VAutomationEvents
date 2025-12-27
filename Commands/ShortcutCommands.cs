using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System;
using Unity.Entities;
using VampireCommandFramework;
using VAuto.Services;

namespace VAuto.Commands
{
    /// <summary>
    /// Command Shortcuts - Quick access to commonly used commands
    /// </summary>
    public static class ShortcutCommands
    {
        #region Character Shortcuts
        [Command("char", "char <action> [args]", "Character commands shortcut", adminOnly: false)]
        public static void CharShortcut(ChatCommandContext ctx, string action = "help", string args = "")
        {
            CharacterCommands.CharacterCommand(ctx, action, args);
        }

        [Command("c", "c <action> [args]", "Character commands shortcut", adminOnly: false)]
        public static void CShortcut(ChatCommandContext ctx, string action = "help", string args = "")
        {
            CharacterCommands.CharacterCommand(ctx, action, args);
        }

        [Command("player", "player <action> [args]", "Player commands shortcut", adminOnly: false)]
        public static void PlayerShortcut(ChatCommandContext ctx, string action = "help", string args = "")
        {
            CharacterCommands.CharacterCommand(ctx, action, args);
        }

        [Command("p", "p <action> [args]", "Player commands shortcut", adminOnly: false)]
        public static void PShortcut(ChatCommandContext ctx, string action = "help", string args = "")
        {
            CharacterCommands.CharacterCommand(ctx, action, args);
        }
        #endregion

        #region Service Shortcuts
        [Command("svc", "svc <action> [args]", "Service commands shortcut", adminOnly: true)]
        public static void ServiceShortcut(ChatCommandContext ctx, string action = "", string args = "")
        {
            ServiceCommands.ServiceCommand(ctx, "status", action);
        }

        [Command("sys", "sys <service>", "System status shortcut", adminOnly: true)]
        public static void SystemShortcut(ChatCommandContext ctx, string service = "")
        {
            ServiceCommands.SystemStatusCommand(ctx, service);
        }

        [Command("map", "map <action> [args]", "Map icon commands shortcut", adminOnly: true)]
        public static void MapShortcut(ChatCommandContext ctx, string action = "", string args = "")
        {
            ServiceCommands.MapIconCommand(ctx, action, args);
        }

        [Command("resp", "resp <action> [args]", "Respawn commands shortcut", adminOnly: true)]
        public static void RespawnShortcut(ChatCommandContext ctx, string action = "", string args = "")
        {
            ServiceCommands.RespawnCommand(ctx, action, "", 30);
        }

        [Command("gs", "gs <action> [args]", "Game system commands shortcut", adminOnly: true)]
        public static void GameSystemShortcut(ChatCommandContext ctx, string action = "", string args = "")
        {
            ServiceCommands.GameSystemCommand(ctx, action, args);
        }

        [Command("r", "r <target> [options]", "Quick reload command", adminOnly: true)]
        public static void QuickReload(ChatCommandContext ctx, string target = "help", string options = "")
        {
            ctx.Reply("Reload commands have been removed. Use service commands instead.");
        }

        [Command("reload", "reload <target> [options]", "Reload commands shortcut", adminOnly: true)]
        public static void ReloadFullShortcut(ChatCommandContext ctx, string target = "help", string options = "")
        {
            ctx.Reply("Reload commands have been removed. Use service commands instead.");
        }
        #endregion

        #region Arena Shortcuts
        [Command("arena", "arena <action> [args]", "Arena commands shortcut", adminOnly: false)]
        public static void ArenaShortcut(ChatCommandContext ctx, string action = "help", string args = "")
        {
            ctx.Reply("Arena commands are available. Use .arena help for more information.");
        }

        [Command("pvp", "pvp <action> [args]", "PvP arena commands shortcut", adminOnly: false)]
        public static void PvPShortcut(ChatCommandContext ctx, string action = "help", string args = "")
        {
            ctx.Reply("PvP arena commands are available. Use .arena help for more information.");
        }
        #endregion

        #region Quick Action Shortcuts
        [Command("tp", "tp <target>", "Quick teleport", adminOnly: false)]
        public static void TeleportShortcut(ChatCommandContext ctx, string target = "")
        {
            CharacterCommands.CharacterCommand(ctx, "teleport", target);
        }

        [Command("pos", "pos [target]", "Quick position check", adminOnly: false)]
        public static void PositionShortcut(ChatCommandContext ctx, string target = "")
        {
            CharacterCommands.CharacterCommand(ctx, "position", target);
        }

        [Command("loc", "loc [target]", "Quick location check", adminOnly: false)]
        public static void LocationShortcut(ChatCommandContext ctx, string target = "")
        {
            CharacterCommands.CharacterCommand(ctx, "position", target);
        }

        [Command("hp", "hp [target]", "Quick health check", adminOnly: false)]
        public static void HealthShortcut(ChatCommandContext ctx, string target = "")
        {
            CharacterCommands.CharacterCommand(ctx, "health", target);
        }

        [Command("stats", "stats [target]", "Quick stats check", adminOnly: false)]
        public static void StatsShortcut(ChatCommandContext ctx, string target = "")
        {
            CharacterCommands.CharacterCommand(ctx, "stats", target);
        }

        [Command("inv", "inv [target]", "Quick inventory check", adminOnly: false)]
        public static void InventoryShortcut(ChatCommandContext ctx, string target = "")
        {
            CharacterCommands.CharacterCommand(ctx, "inventory", target);
        }

        [Command("list", "list [filter]", "Quick player list", adminOnly: false)]
        public static void ListShortcut(ChatCommandContext ctx, string filter = "")
        {
            CharacterCommands.CharacterCommand(ctx, "list", filter);
        }

        [Command("online", "online [target]", "Quick online check", adminOnly: false)]
        public static void OnlineShortcut(ChatCommandContext ctx, string target = "")
        {
            CharacterCommands.CharacterCommand(ctx, "online", target);
        }
        #endregion

        #region Utility Shortcuts
        [Command("help", "help [topic]", "Comprehensive help system", adminOnly: false)]
        public static void HelpCommand(ChatCommandContext ctx, string topic = "")
        {
            try
            {
                switch (topic.ToLower())
                {
                    case "character":
                    case "char":
                    case "c":
                    case "player":
                    case "p":
                        CharacterCommands.CharacterCommand(ctx, "help", "");
                        break;
                    case "arena":
                    case "pvp":
                        ctx.Reply("Arena commands are available. Use .arena help for more information.");
                        break;
                    case "service":
                    case "svc":
                    case "sys":
                        ServiceCommands.ServiceCommand(ctx, "help", "");
                        break;
                    case "shortcuts":
                    case "alias":
                        ShortcutHelp(ctx);
                        break;
                    default:
                        GeneralHelp(ctx);
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in help command: {ex.Message}");
                ctx.Reply($"Error: {ex.Message}");
            }
        }

        private static void ShortcutHelp(ChatCommandContext ctx)
        {
            ctx.Reply("=== Command Shortcuts ===");
            ctx.Reply("");
            ctx.Reply("Character Commands:");
            ctx.Reply("  char, c, player, p → character commands");
            ctx.Reply("");
            ctx.Reply("Quick Actions:");
            ctx.Reply("  tp → teleport");
            ctx.Reply("  pos, loc → position");
            ctx.Reply("  hp → health");
            ctx.Reply("  stats → statistics");
            ctx.Reply("  inv → inventory");
            ctx.Reply("  list → player list");
            ctx.Reply("  online → online status");
            ctx.Reply("");
            ctx.Reply("Service Commands:");
            ctx.Reply("  svc → service management");
            ctx.Reply("  sys → system status");
            ctx.Reply("  map → map icons");
            ctx.Reply("  resp → respawn management");
            ctx.Reply("  gs → game systems");
            ctx.Reply("");
            ctx.Reply("Arena Commands:");
            ctx.Reply("  arena, pvp → arena commands");
            ctx.Reply("");
            ctx.Reply("Examples:");
            ctx.Reply("  char i PlayerName   → character info");
            ctx.Reply("  tp 1000 50 2000 → teleport to coordinates");
            ctx.Reply("  svc status map    → map icon service status");
            ctx.Reply("  arena enter PlayerName → enter arena");
        }

        private static void GeneralHelp(ChatCommandContext ctx)
        {
            ctx.Reply("=== VAuto Command System ===");
            ctx.Reply("");
            ctx.Reply("Main Command Categories:");
            ctx.Reply("  • Character Management (character, char, c, player, p)");
            ctx.Reply("  • Arena System (arena, pvp)");
            ctx.Reply("  • Service Management (service, svc, sys)");
            ctx.Reply("  • Quick Actions (tp, pos, hp, stats, inv, list, online)");
            ctx.Reply("");
            ctx.Reply("Type 'help <topic>' for specific help:");
            ctx.Reply("  help character - Character command help");
            ctx.Reply("  help arena - Arena command help");
            ctx.Reply("  help shortcuts - Show all shortcuts");
            ctx.Reply("  help - This general help");
            ctx.Reply("");
            ctx.Reply("Quick Start Examples:");
            ctx.Reply("  char i MyFriend - Check friend's info");
            ctx.Reply("  tp 1000 50 2000 - Teleport to coordinates");
            ctx.Reply("  arena list - Show arena commands");
        }
        #endregion
    }
}


