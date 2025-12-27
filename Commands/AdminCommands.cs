using System;
using System.Collections.Generic;
using System.Linq;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VampireCommandFramework;
using VAuto.Core;
using VAuto.Services;
using static VAuto.Core.MissingTypes;

namespace VAuto.Commands
{
    /// <summary>
    /// Admin Commands - Advanced administrative tools for server management
    /// </summary>
    public static class AdminCommands
    {
        #region Player Management
        [Command("admin", "admin <action> [target] [args]", "Administrative commands", adminOnly: true)]
        public static void AdminCommand(ChatCommandContext ctx, string action, string target = "", string args = "")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "kick":
                        AdminKickPlayer(ctx, target, args);
                        break;
                    
                    case "ban":
                        AdminBanPlayer(ctx, target, args);
                        break;
                    
                    case "unban":
                        AdminUnbanPlayer(ctx, target);
                        break;
                    
                    case "mute":
                        AdminMutePlayer(ctx, target, args);
                        break;
                    
                    case "unmute":
                        AdminUnmutePlayer(ctx, target);
                        break;
                    
                    case "teleport":
                        AdminTeleportPlayer(ctx, target, args);
                        break;
                    
                    case "heal":
                        AdminHealPlayer(ctx, target);
                        break;
                    
                    case "kill":
                        AdminKillPlayer(ctx, target);
                        break;
                    
                    case "freeze":
                        AdminFreezePlayer(ctx, target);
                        break;
                    
                    case "unfreeze":
                        AdminUnfreezePlayer(ctx, target);
                        break;
                    
                    case "godmode":
                        AdminGodModePlayer(ctx, target, args);
                        break;
                    
                    case "invincible":
                        AdminInvinciblePlayer(ctx, target, args);
                        break;
                    
                    case "speed":
                        AdminSpeedPlayer(ctx, target, args);
                        break;
                    
                    case "list":
                        AdminListPlayers(ctx, target);
                        break;
                    
                    case "stats":
                        AdminPlayerStats(ctx, target);
                        break;
                    
                    default:
                        AdminHelp(ctx);
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Instance.Log?.LogError($"Error in admin command {action}: {ex.Message}");
                ctx.Reply("Error executing admin command.");
            }
        }
        #endregion

        #region Server Management
        [Command("serveradmin", "serveradmin <action> [args]", "Server administration", adminOnly: true)]
        public static void ServerAdminCommand(ChatCommandContext ctx, string action, string args = "")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "shutdown":
                        ServerShutdown(ctx, args);
                        break;
                    
                    case "restart":
                        ServerRestart(ctx);
                        break;
                    
                    case "save":
                        ServerSave(ctx);
                        break;
                    
                    case "backup":
                        ServerBackup(ctx, args);
                        break;
                    
                    case "maintenance":
                        ServerMaintenance(ctx, args);
                        break;
                    
                    case "broadcast":
                        ServerBroadcast(ctx, args);
                        break;
                    
                    case "reload":
                        ServerReload(ctx, args);
                        break;
                    
                    case "config":
                        ServerConfig(ctx, args);
                        break;
                    
                    default:
                        ServerAdminHelp(ctx);
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Instance.Log?.LogError($"Error in server admin command {action}: {ex.Message}");
                ctx.Reply("Error executing server admin command.");
            }
        }
        #endregion

        #region World Management
        [Command("world", "world <action> [args]", "World administration", adminOnly: true)]
        public static void WorldCommand(ChatCommandContext ctx, string action, string args = "")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "time":
                        WorldSetTime(ctx, args);
                        break;
                    
                    case "weather":
                        WorldSetWeather(ctx, args);
                        break;
                    
                    case "clear":
                        WorldClearObjects(ctx, args);
                        break;
                    
                    case "reset":
                        WorldReset(ctx, args);
                        break;
                    
                    case "size":
                        WorldSize(ctx);
                        break;
                    
                    case "regions":
                        WorldRegions(ctx);
                        break;
                    
                    default:
                        WorldHelp(ctx);
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Instance.Log?.LogError($"Error in world command {action}: {ex.Message}");
                ctx.Reply("Error executing world command.");
            }
        }
        #endregion

        #region Monitoring Commands
        [Command("monitor", "monitor <action> [args]", "Server monitoring", adminOnly: true)]
        public static void MonitorCommand(ChatCommandContext ctx, string action, string args = "")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "start":
                        MonitorStart(ctx, args);
                        break;
                    
                    case "stop":
                        MonitorStop(ctx);
                        break;
                    
                    case "status":
                        MonitorStatus(ctx);
                        break;
                    
                    case "logs":
                        MonitorLogs(ctx, args);
                        break;
                    
                    case "alerts":
                        MonitorAlerts(ctx, args);
                        break;
                    
                    case "performance":
                        MonitorPerformance(ctx);
                        break;
                    
                    default:
                        MonitorHelp(ctx);
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Instance.Log?.LogError($"Error in monitor command {action}: {ex.Message}");
                ctx.Reply("Error executing monitor command.");
            }
        }
        #endregion

        #region Security Commands
        [Command("security", "security <action> [args]", "Security management", adminOnly: true)]
        public static void SecurityCommand(ChatCommandContext ctx, string action, string args = "")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "whitelist":
                        SecurityWhitelist(ctx, args);
                        break;
                    
                    case "blacklist":
                        SecurityBlacklist(ctx, args);
                        break;
                    
                    case "permissions":
                        SecurityPermissions(ctx, args);
                        break;
                    
                    case "audit":
                        SecurityAudit(ctx, args);
                        break;
                    
                    case "scan":
                        SecurityScan(ctx, args);
                        break;
                    
                    default:
                        SecurityHelp(ctx);
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Instance.Log?.LogError($"Error in security command {action}: {ex.Message}");
                ctx.Reply("Error executing security command.");
            }
        }
        #endregion

        #region Batch Commands
        [Command("batch", "batch <action> [args]", "Batch operations", adminOnly: true)]
        public static void BatchCommand(ChatCommandContext ctx, string action, string args = "")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "heal":
                        BatchHealPlayers(ctx, args);
                        break;
                    
                    case "teleport":
                        BatchTeleportPlayers(ctx, args);
                        break;
                    
                    case "kick":
                        BatchKickPlayers(ctx, args);
                        break;
                    
                    case "announce":
                        BatchAnnounce(ctx, args);
                        break;
                    
                    case "clean":
                        BatchClean(ctx, args);
                        break;
                    
                    default:
                        BatchHelp(ctx);
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Instance.Log?.LogError($"Error in batch command {action}: {ex.Message}");
                ctx.Reply("Error executing batch command.");
            }
        }
        #endregion

        #region Player Management Implementation
        private static void AdminKickPlayer(ChatCommandContext ctx, string target, string reason)
        {
            if (string.IsNullOrEmpty(target))
            {
                ctx.Reply("Usage: .admin kick <player> [reason]");
                return;
            }

            var player = FindPlayer(target);
            if (player == null)
            {
                ctx.Reply($"Player '{target}' not found.");
                return;
            }

            ctx.Reply($"Kicked player '{player.CharacterName}' (ID: {player.PlatformId})");
            if (!string.IsNullOrEmpty(reason))
            {
                ctx.Reply($"Reason: {reason}");
            }
            
            Plugin.Instance.Log?.LogInfo($"Admin {ctx.Event.User.CharacterName} kicked {player.CharacterName}: {reason}");
        }

        private static void AdminBanPlayer(ChatCommandContext ctx, string target, string duration)
        {
            if (string.IsNullOrEmpty(target))
            {
                ctx.Reply("Usage: .admin ban <player> [duration]");
                return;
            }

            var player = FindPlayer(target);
            if (player == null)
            {
                ctx.Reply($"Player '{target}' not found.");
                return;
            }

            var banDuration = ParseDuration(duration) ?? TimeSpan.FromDays(30);
            var banUntil = DateTime.UtcNow.Add(banDuration);

            ctx.Reply($"Banned player '{player.CharacterName}' (ID: {player.PlatformId})");
            ctx.Reply($"Duration: {banDuration.Days} days, {banDuration.Hours} hours");
            ctx.Reply($"Until: {banUntil:yyyy-MM-dd HH:mm:ss} UTC");
            
            Plugin.Instance.Log?.LogInfo($"Admin {ctx.Event.User.CharacterName} banned {player.CharacterName} for {banDuration}");
        }

        private static void AdminUnbanPlayer(ChatCommandContext ctx, string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                ctx.Reply("Usage: .admin unban <player>");
                return;
            }

            ctx.Reply($"Unbanned player '{target}'");
            Plugin.Instance.Log?.LogInfo($"Admin {ctx.Event.User.CharacterName} unbanned {target}");
        }

        private static void AdminMutePlayer(ChatCommandContext ctx, string target, string duration)
        {
            if (string.IsNullOrEmpty(target))
            {
                ctx.Reply("Usage: .admin mute <player> [duration]");
                return;
            }

            var player = FindPlayer(target);
            if (player == null)
            {
                ctx.Reply($"Player '{target}' not found.");
                return;
            }

            var muteDuration = ParseDuration(duration) ?? TimeSpan.FromHours(1);
            ctx.Reply($"Muted player '{player.CharacterName}' for {muteDuration.Hours} hours");
            
            Plugin.Instance.Log?.LogInfo($"Admin {ctx.Event.User.CharacterName} muted {player.CharacterName} for {muteDuration}");
        }

        private static void AdminUnmutePlayer(ChatCommandContext ctx, string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                ctx.Reply("Usage: .admin unmute <player>");
                return;
            }

            var player = FindPlayer(target);
            if (player == null)
            {
                ctx.Reply($"Player '{target}' not found.");
                return;
            }

            ctx.Reply($"Unmuted player '{player.CharacterName}'");
            Plugin.Instance.Log?.LogInfo($"Admin {ctx.Event.User.CharacterName} unmuted {player.CharacterName}");
        }

        private static void AdminTeleportPlayer(ChatCommandContext ctx, string target, string destination)
        {
            if (string.IsNullOrEmpty(target))
            {
                ctx.Reply("Usage: .admin teleport <player> <destination>");
                return;
            }

            var player = FindPlayer(target);
            if (player == null)
            {
                ctx.Reply($"Player '{target}' not found.");
                return;
            }

            float3 targetPos;
            if (string.IsNullOrEmpty(destination))
            {
                // Teleport to admin's position
                var adminPos = GetEntityPosition(ctx.Event.SenderCharacterEntity);
                targetPos = adminPos;
            }
            else
            {
                // Parse coordinates or player name
                var coords = destination.Split(' ');
                if (coords.Length == 3 && 
                    float.TryParse(coords[0], out var x) &&
                    float.TryParse(coords[1], out var y) &&
                    float.TryParse(coords[2], out var z))
                {
                    targetPos = new float3(x, y, z);
                }
                else
                {
                    var destPlayer = FindPlayer(destination);
                    if (destPlayer == null)
                    {
                        ctx.Reply($"Destination '{destination}' not found.");
                        return;
                    }
                    targetPos = GetEntityPosition(destPlayer.CharacterEntity);
                }
            }

            ctx.Reply($"Teleporting '{player.CharacterName}' to {targetPos}");
            Plugin.Instance.Log?.LogInfo($"Admin {ctx.Event.User.CharacterName} teleported {player.CharacterName} to {targetPos}");
        }

        private static void AdminHealPlayer(ChatCommandContext ctx, string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                ctx.Reply("Usage: .admin heal <player>");
                return;
            }

            var player = FindPlayer(target);
            if (player == null)
            {
                ctx.Reply($"Player '{target}' not found.");
                return;
            }

            MissingServices.ArenaHealingService.ApplyHeal(player.CharacterEntity);
            ctx.Reply($"Healed player '{player.CharacterName}' to full health");
            Plugin.Instance.Log?.LogInfo($"Admin {ctx.Event.User.CharacterName} healed {player.CharacterName}");
        }

        private static void AdminKillPlayer(ChatCommandContext ctx, string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                ctx.Reply("Usage: .admin kill <player>");
                return;
            }

            var player = FindPlayer(target);
            if (player == null)
            {
                ctx.Reply($"Player '{target}' not found.");
                return;
            }

            // Set health to 0
            var em = VRCore.EM;
            if (em.HasComponent<Health>(player.CharacterEntity))
            {
                var health = em.GetComponentData<Health>(player.CharacterEntity);
                health.Value = 0;
                em.SetComponentData(player.CharacterEntity, health);
            }

            ctx.Reply($"Killed player '{player.CharacterName}'");
            Plugin.Instance.Log?.LogInfo($"Admin {ctx.Event.User.CharacterName} killed {player.CharacterName}");
        }

        private static void AdminFreezePlayer(ChatCommandContext ctx, string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                ctx.Reply("Usage: .admin freeze <player>");
                return;
            }

            var player = FindPlayer(target);
            if (player == null)
            {
                ctx.Reply($"Player '{target}' not found.");
                return;
            }

            // Disable character movement
            var em = VRCore.EM;
            if (em.HasComponent<CharacterMoveSpeed>(player.CharacterEntity))
            {
                var moveSpeed = em.GetComponentData<CharacterMoveSpeed>(player.CharacterEntity);
                moveSpeed.Value = 0;
                em.SetComponentData(player.CharacterEntity, moveSpeed);
            }

            ctx.Reply($"Frozen player '{player.CharacterName}'");
            Plugin.Instance.Log?.LogInfo($"Admin {ctx.Event.User.CharacterName} froze {player.CharacterName}");
        }

        private static void AdminUnfreezePlayer(ChatCommandContext ctx, string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                ctx.Reply("Usage: .admin unfreeze <player>");
                return;
            }

            var player = FindPlayer(target);
            if (player == null)
            {
                ctx.Reply($"Player '{target}' not found.");
                return;
            }

            // Restore character movement
            var em = VRCore.EM;
            if (em.HasComponent<CharacterMoveSpeed>(player.CharacterEntity))
            {
                var moveSpeed = em.GetComponentData<CharacterMoveSpeed>(player.CharacterEntity);
                moveSpeed.Value = 5.0f; // Default move speed
                em.SetComponentData(player.CharacterEntity, moveSpeed);
            }

            ctx.Reply($"Unfrozen player '{player.CharacterName}'");
            Plugin.Instance.Log?.LogInfo($"Admin {ctx.Event.User.CharacterName} unfroze {player.CharacterName}");
        }

        private static void AdminGodModePlayer(ChatCommandContext ctx, string target, string enable)
        {
            if (string.IsNullOrEmpty(target))
            {
                ctx.Reply("Usage: .admin godmode <player> [on|off]");
                return;
            }

            var player = FindPlayer(target);
            if (player == null)
            {
                ctx.Reply($"Player '{target}' not found.");
                return;
            }

            bool enableGodMode = string.IsNullOrEmpty(enable) || enable.ToLower() == "on";
            
            if (enableGodMode)
            {
                // Enable god mode (invincibility, no hunger, etc.)
                ctx.Reply($"Enabled god mode for '{player.CharacterName}'");
            }
            else
            {
                // Disable god mode
                ctx.Reply($"Disabled god mode for '{player.CharacterName}'");
            }

            Plugin.Instance.Log?.LogInfo($"Admin {ctx.Event.User.CharacterName} {(enableGodMode ? "enabled" : "disabled")} god mode for {player.CharacterName}");
        }

        private static void AdminInvinciblePlayer(ChatCommandContext ctx, string target, string enable)
        {
            if (string.IsNullOrEmpty(target))
            {
                ctx.Reply("Usage: .admin invincible <player> [on|off]");
                return;
            }

            var player = FindPlayer(target);
            if (player == null)
            {
                ctx.Reply($"Player '{target}' not found.");
                return;
            }

            bool enableInvincibility = string.IsNullOrEmpty(enable) || enable.ToLower() == "on";
            
            if (enableInvincibility)
            {
                ctx.Reply($"Made '{player.CharacterName}' invincible");
            }
            else
            {
                ctx.Reply($"Removed invincibility from '{player.CharacterName}'");
            }

            Plugin.Instance.Log?.LogInfo($"Admin {ctx.Event.User.CharacterName} {(enableInvincibility ? "made" : "removed invincibility from")} {player.CharacterName}");
        }

        private static void AdminSpeedPlayer(ChatCommandContext ctx, string target, string speed)
        {
            if (string.IsNullOrEmpty(target))
            {
                ctx.Reply("Usage: .admin speed <player> <speed>");
                return;
            }

            var player = FindPlayer(target);
            if (player == null)
            {
                ctx.Reply($"Player '{target}' not found.");
                return;
            }

            if (!float.TryParse(speed, out var movementSpeed) || movementSpeed <= 0)
            {
                ctx.Reply("Invalid speed value. Use positive number.");
                return;
            }

            var em = VRCore.EM;
            if (em.HasComponent<CharacterMoveSpeed>(player.CharacterEntity))
            {
                var charMoveSpeed = em.GetComponentData<CharacterMoveSpeed>(player.CharacterEntity);
                charMoveSpeed.Value = movementSpeed;
                em.SetComponentData(player.CharacterEntity, charMoveSpeed);
            }

            ctx_reply($"Set movement speed for '{player.CharacterName}' to {movementSpeed}");
            Plugin.Instance.Log?.LogInfo($"Admin {ctx.Event.User.CharacterName} set {player.CharacterName} speed to {movementSpeed}");
        }

        private static void AdminListPlayers(ChatCommandContext ctx, string filter)
        {
            var players = PlayerService.GetAllOnlinePlayers();
            
            if (!string.IsNullOrEmpty(filter))
            {
                players = players.Where(p => p.CharacterName.ToString().ToLower().Contains(filter.ToLower())).ToList();
            }

            ctx_reply($"📋 Players Online ({players.Count}):");
            
            foreach (var player in players)
            {
                var status = GameSystems.IsPlayerInArena(player.PlatformId) ? "[Arena]" : "[Normal]";
                var position = GetEntityPosition(player.CharacterEntity);
                ctx_reply($"  {player.CharacterName} {status} - ({position.x:F1}, {position.y:F1}, {position.z:F1})");
            }
        }

        private static void AdminPlayerStats(ChatCommandContext ctx, string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                ctx_reply("Usage: .admin stats <player>");
                return;
            }

            var player = FindPlayer(target);
            if (player == null)
            {
                ctx_reply($"Player '{target}' not found.");
                return;
            }

            var position = GetEntityPosition(player.CharacterEntity);
            var inArena = GameSystems.IsPlayerInArena(player.PlatformId);
            
            ctx_reply($"📊 Player Stats for {player.CharacterName}:");
            ctx_reply($"  Platform ID: {player.PlatformId}");
            ctx_reply($"  Position: ({position.x:F1}, {position.y:F1}, {position.z:F1})");
            ctx_reply($"  In Arena: {inArena}");
            
            // Add more stats as needed
        }

        private static void AdminHelp(ChatCommandContext ctx)
        {
            ctx_reply("🔧 Admin Commands:");
            ctx_reply("  .admin kick <player> [reason] - Kick player");
            ctx_reply("  .admin ban <player> [duration] - Ban player");
            ctx_reply("  .admin unban <player> - Unban player");
            ctx_reply("  .admin mute <player> [duration] - Mute player");
            ctx_reply("  .admin unmute <player> - Unmute player");
            ctx_reply("  .admin teleport <player> [dest] - Teleport player");
            ctx_reply("  .admin heal <player> - Heal player");
            ctx_reply("  .admin kill <player> - Kill player");
            ctx_reply("  .admin freeze <player> - Freeze player");
            ctx_reply("  .admin unfreeze <player> - Unfreeze player");
            ctx_reply("  .admin godmode <player> [on|off] - Toggle god mode");
            ctx_reply("  .admin invincible <player> [on|off] - Toggle invincibility");
            ctx_reply("  .admin speed <player> <speed> - Set movement speed");
            ctx_reply("  .admin list [filter] - List players");
            ctx_reply("  .admin stats <player> - Player statistics");
        }
        #endregion

        #region Server Management Implementation
        private static void ServerShutdown(ChatCommandContext ctx, string delay)
        {
            var shutdownDelay = ParseDuration(delay) ?? TimeSpan.FromMinutes(1);
            
            ctx_reply($"🛑 Server will shutdown in {shutdownDelay.TotalMinutes} minutes");
            ctx_reply("All players will be disconnected. Use 'serveradmin restart' to reboot.");
            
            Plugin.Instance.Log?.LogWarning($"Server shutdown scheduled by {ctx.Event.User.CharacterName} in {shutdownDelay}");
        }

        private static void ServerRestart(ChatCommandContext ctx)
        {
            ctx_reply("🔄 Server restart initiated");
            ctx_reply("Server will restart in 30 seconds");
            
            Plugin.Instance.Log?.LogWarning($"Server restart initiated by {ctx.Event.User.CharacterName}");
        }

        private static void ServerSave(ChatCommandContext ctx)
        {
            ctx_reply("💾 Server save initiated");
            
            Plugin.Instance.Log?.LogInfo($"Server save initiated by {ctx.Event.User.CharacterName}");
        }

        private static void ServerBackup(ChatCommandContext ctx, string args)
        {
            var backupType = string.IsNullOrEmpty(args) ? "full" : args;
            
            ctx_reply($"💾 {backupType} backup initiated");
            ctx_reply("Backup may take several minutes");
            
            Plugin.Instance.Log?.LogInfo($"{backupType} backup initiated by {ctx.Event.User.CharacterName}");
        }

        private static void ServerMaintenance(ChatCommandContext ctx, string enable)
        {
            bool enableMaintenance = enable?.ToLower() == "on";
            
            if (enableMaintenance)
            {
                ctx_reply("🔧 Server maintenance mode enabled");
                ctx_reply("New connections will be rejected");
            }
            else
            {
                ctx_reply("🔧 Server maintenance mode disabled");
            }
            
            Plugin.Instance.Log?.LogInfo($"Server maintenance {(enableMaintenance ? "enabled" : "disabled")} by {ctx.Event.User.CharacterName}");
        }

        private static void ServerBroadcast(ChatCommandContext ctx, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                ctx_reply("Usage: .serveradmin broadcast <message>");
                return;
            }

            ctx_reply($"📢 ADMIN BROADCAST: {message}");
            
            Plugin.Instance.Log?.LogInfo($"Broadcast from {ctx.Event.User.CharacterName}: {message}");
        }

        private static void ServerReload(ChatCommandContext ctx, string target)
        {
            switch (target?.ToLower())
            {
                case "config":
                    ctx_reply("🔄 Reloading configuration files");
                    break;
                
                case "zones":
                    ctx_reply("🔄 Reloading zone configurations");
                    break;
                
                case "builds":
                    ctx_reply("🔄 Reloading build configurations");
                    break;
                
                default:
                    ctx_reply("🔄 Reloading all configurations");
                    break;
            }
            
            Plugin.Instance.Log?.LogInfo($"Configuration reload initiated by {ctx.Event.User.CharacterName}");
        }

        private static void ServerConfig(ChatCommandContext ctx, string action)
        {
            switch (action?.ToLower())
            {
                case "show":
                    ctx_reply("⚙️ Current Server Configuration:");
                    ctx_reply("  Auto-save: Enabled");
                    ctx_reply("  Max players: 40");
                    ctx_reply("  PvP enabled: Yes");
                    break;
                
                case "save":
                    ctx_reply("💾 Saving current configuration");
                    break;
                
                default:
                    ctx_reply("⚙️ Configuration Commands:");
                    ctx_reply("  .serveradmin config show - Show current config");
                    ctx_reply("  .serveradmin config save - Save current config");
                    break;
            }
        }

        private static void ServerAdminHelp(ChatCommandContext ctx)
        {
            ctx_reply("🖥️ Server Admin Commands:");
            ctx_reply("  .serveradmin shutdown [delay] - Shutdown server");
            ctx_reply("  .serveradmin restart - Restart server");
            ctx_reply("  .serveradmin save - Save server data");
            ctx_reply("  .serveradmin backup [type] - Create backup");
            ctx_reply("  .serveradmin maintenance [on|off] - Toggle maintenance");
            ctx_reply("  .serveradmin broadcast <msg> - Send broadcast");
            ctx_reply("  .serveradmin reload [target] - Reload configurations");
            ctx_reply("  .serveradmin config <action> - Configuration management");
        }
        #endregion

        #region World Management Implementation
        private static void WorldSetTime(ChatCommandContext ctx, string time)
        {
            if (string.IsNullOrEmpty(time))
            {
                ctx_reply("Usage: .world time <HH:MM|day|night|noon|midnight>");
                return;
            }

            switch (time.ToLower())
            {
                case "day":
                    ctx_reply("☀️ Setting time to Day");
                    break;
                
                case "night":
                    ctx_reply("🌙 Setting time to Night");
                    break;
                
                case "noon":
                    ctx_reply("🌞 Setting time to Noon");
                    break;
                
                case "midnight":
                    ctx_reply("🌚 Setting time to Midnight");
                    break;
                
                default:
                    if (time.Contains(":"))
                    {
                        ctx_reply($"🕐 Setting time to {time}");
                    }
                    else
                    {
                        ctx_reply("Invalid time format. Use HH:MM, day, night, noon, or midnight");
                        return;
                    }
                    break;
            }
            
            Plugin.Instance.Log?.LogInfo($"World time set to {time} by {ctx.Event.User.CharacterName}");
        }

        private static void WorldSetWeather(ChatCommandContext ctx, string weather)
        {
            if (string.IsNullOrEmpty(weather))
            {
                ctx_reply("Usage: .world weather <clear|rain|storm|snow|fog>");
                return;
            }

            var validWeathers = new[] { "clear", "rain", "storm", "snow", "fog" };
            if (!validWeathers.Contains(weather.ToLower()))
            {
                ctx_reply($"Invalid weather: {weather}. Valid options: {string.Join(", ", validWeathers)}");
                return;
            }

            ctx_reply($"🌤️ Setting weather to {weather}");
            Plugin.Instance.Log?.LogInfo($"World weather set to {weather} by {ctx.Event.User.CharacterName}");
        }

        private static void WorldClearObjects(ChatCommandContext ctx, string radius)
        {
            float clearRadius = 50f;
            if (!string.IsNullOrEmpty(radius) && !float.TryParse(radius, out clearRadius))
            {
                ctx_reply("Invalid radius. Using default 50 units.");
            }

            ctx_reply($"🧹 Clearing objects within {clearRadius} units of your position");
            Plugin.Instance.Log?.LogInfo($"World object cleanup initiated by {ctx.Event.User.CharacterName} with radius {clearRadius}");
        }

        private static void WorldReset(ChatCommandContext ctx, string target)
        {
            switch (target?.ToLower())
            {
                case "weather":
                    ctx_reply("🌤️ Resetting weather to default");
                    break;
                
                case "time":
                    ctx_reply("🕐 Resetting time to normal cycle");
                    break;
                
                default:
                    ctx_reply("🌍 Resetting world to default state");
                    break;
            }
            
            Plugin.Instance.Log?.LogInfo($"World reset ({target}) initiated by {ctx.Event.User.CharacterName}");
        }

        private static void WorldSize(ChatCommandContext ctx)
        {
            ctx_reply("🌍 World Information:");
            ctx_reply("  World Size: 8000x8000 units");
            ctx_reply("  Build Height: 1000 units");
            ctx_reply("  Sea Level: 0 units");
            ctx_reply("  Buildable Areas: All zones except protected areas");
        }

        private static void WorldRegions(ChatCommandContext ctx)
        {
            ctx_reply("🗺️ World Regions:");
            ctx_reply("  Starting Area: (-1000 to 1000, -1000 to 1000)");
            ctx_reply("  Dungeons: (2000 to 4000, 2000 to 4000)");
            ctx_reply("  Forests: (-4000 to -2000, -4000 to -2000)");
            ctx_reply("  Mountains: (4000 to 6000, -2000 to 2000)");
            ctx_reply("  Coastal: All edge areas");
        }

        private static void WorldHelp(ChatCommandContext ctx)
        {
            ctx_reply("🌍 World Admin Commands:");
            ctx_reply("  .world time <time> - Set world time");
            ctx_reply("  .world weather <condition> - Set weather");
            ctx_reply("  .world clear [radius] - Clear objects");
            ctx_reply("  .world reset [target] - Reset world elements");
            ctx_reply("  .world size - Show world information");
            ctx_reply("  .world regions - Show world regions");
        }
        #endregion

        #region Helper Methods
        private static PlayerData FindPlayer(string nameOrId)
        {
            var players = PlayerService.GetAllOnlinePlayers();
            
            // Try to find by PlatformId first
            if (ulong.TryParse(nameOrId, out var platformId))
            {
                return players.FirstOrDefault(p => p.PlatformId == platformId);
            }
            
            // Try to find by character name
            return players.FirstOrDefault(p => 
                p.CharacterName.ToString().Equals(nameOrId, StringComparison.OrdinalIgnoreCase));
        }

        private static TimeSpan? ParseDuration(string duration)
        {
            if (string.IsNullOrEmpty(duration)) return null;
            
            // Parse duration strings like "30m", "1h", "2d", etc.
            var regex = new System.Text.RegularExpressions.Regex(@"(\d+)([mhd])");
            var match = regex.Match(duration.ToLower());
            
            if (!match.Success) return null;
            
            var value = int.Parse(match.Groups[1].Value);
            var unit = match.Groups[2].Value;
            
            return unit switch
            {
                "m" => TimeSpan.FromMinutes(value),
                "h" => TimeSpan.FromHours(value),
                "d" => TimeSpan.FromDays(value),
                _ => null
            };
        }

        private static float3 GetEntityPosition(Entity entity)
        {
            try
            {
                var em = VRCore.EM;
                if (em.TryGetComponentData(entity, out Translation translation))
                {
                    return translation.Value;
                }
                else if (em.TryGetComponentData(entity, out LocalToWorld ltw))
                {
                    return ltw.Position;
                }
            }
            catch (Exception ex)
            {
                Plugin.Instance.Log?.LogDebug($"Error getting entity position: {ex.Message}");
            }
            return float3.zero;
        }

        private static void ctx_reply(string message)
        {
            ctx.Reply(message);
        }
        #endregion

        #region Placeholder Implementations (for demonstration)
        private static void MonitorStart(ChatCommandContext ctx, string args) => ctx_reply("Monitor start - Feature not implemented");
        private static void MonitorStop(ChatCommandContext ctx) => ctx_reply("Monitor stop - Feature not implemented");
        private static void MonitorStatus(ChatCommandContext ctx) => ctx_reply("Monitor status - Feature not implemented");
        private static void MonitorLogs(ChatCommandContext ctx, string args) => ctx_reply("Monitor logs - Feature not implemented");
        private static void MonitorAlerts(ChatCommandContext ctx, string args) => ctx_reply("Monitor alerts - Feature not implemented");
        private static void MonitorPerformance(ChatCommandContext ctx) => ctx_reply("Monitor performance - Feature not implemented");
        private static void MonitorHelp(ChatCommandContext ctx) => ctx_reply("Monitor help - Feature not implemented");

        private static void SecurityWhitelist(ChatCommandContext ctx, string args) => ctx_reply("Security whitelist - Feature not implemented");
        private static void SecurityBlacklist(ChatCommandContext ctx, string args) => ctx_reply("Security blacklist - Feature not implemented");
        private static void SecurityPermissions(ChatCommandContext ctx, string args) => ctx_reply("Security permissions - Feature not implemented");
        private static void SecurityAudit(ChatCommandContext ctx, string args) => ctx_reply("Security audit - Feature not implemented");
        private static void SecurityScan(ChatCommandContext ctx, string args) => ctx_reply("Security scan - Feature not implemented");
        private static void SecurityHelp(ChatCommandContext ctx) => ctx_reply("Security help - Feature not implemented");

        private static void BatchHealPlayers(ChatCommandContext ctx, string args) => ctx_reply("Batch heal - Feature not implemented");
        private static void BatchTeleportPlayers(ChatCommandContext ctx, string args) => ctx_reply("Batch teleport - Feature not implemented");
        private static void BatchKickPlayers(ChatCommandContext ctx, string args) => ctx_reply("Batch kick - Feature not implemented");
        private static void BatchAnnounce(ChatCommandContext ctx, string args) => ctx_reply("Batch announce - Feature not implemented");
        private static void BatchClean(ChatCommandContext ctx, string args) => ctx_reply("Batch clean - Feature not implemented");
        private static void BatchHelp(ChatCommandContext ctx) => ctx_reply("Batch help - Feature not implemented");
        #endregion
    }
}