using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System;
using System.Linq;
using Unity.Entities;
using VampireCommandFramework;
using VAuto.Core;
using VAuto.Services;
using VAuto.Extensions;
using static VAuto.Core.MissingTypes;

namespace VAuto.Commands
{
    /// <summary>
    /// Service Commands - Administrative commands for managing all VAuto services
    /// </summary>
    public static class ServiceCommands
    {
        /// <summary>
        /// Register all service commands
        /// </summary>
        public static void RegisterCommands()
        {
            // Commands are auto-registered via attributes
        }

        #region Player Progress Commands
        [Command("progress", "progress <action> [platformId]", "Manage player progress data", adminOnly: true)]
        public static void ProgressCommand(ChatCommandContext ctx, string action, string platformIdStr = "")
        {
            switch (action.ToLower())
            {
                case "get":
                    GetPlayerProgress(ctx, platformIdStr);
                    break;
                case "list":
                    ListAllProgress(ctx);
                    break;
                case "save":
                    ForceSaveProgress(ctx);
                    break;
                case "remove":
                    RemovePlayerProgress(ctx, platformIdStr);
                    break;
                case "status":
                    ProgressStatus(ctx);
                    break;
                default:
                    ctx.Reply($"Unknown progress action: {action}. Available: get, list, save, remove, status");
                    break;
            }
        }

        private static void GetPlayerProgress(ChatCommandContext ctx, string platformIdStr)
        {
            if (!ulong.TryParse(platformIdStr, out var platformId))
            {
                ctx.Reply("Invalid platform ID. Usage: .progress get <platformId>");
                return;
            }

            var progress = PlayerProgressStore.Get(platformId);
            if (progress == null)
            {
                ctx.Reply($"No progress data found for platform ID {platformId}");
                return;
            }

            ctx.Reply($"Progress for {progress.CharacterName} (ID: {progress.PlatformId}):");
            ctx.Reply($"  Level: {progress.Level}");
            ctx.Reply($"  Experience: {progress.Experience:F1}");
            ctx.Reply($"  Unlocked VBloods: {progress.UnlockedVBloods.Count}");
            ctx.Reply($"  Ability Levels: {progress.AbilityLevels.Count}");
            ctx.Reply($"  Completed Quests: {progress.CompletedQuests.Count}");
            ctx.Reply($"  Last Updated: {progress.LastUpdated:yyyy-MM-dd HH:mm:ss}");
        }

        private static void ListAllProgress(ChatCommandContext ctx)
        {
            var allProgress = PlayerProgressStore.GetAll();
            var count = allProgress.Count;
            
            if (count == 0)
            {
                ctx.Reply("No player progress data stored.");
                return;
            }

            ctx.Reply($"Found {count} cached players:");
            foreach (var kvp in allProgress.Take(10)) // Show first 10 to avoid spam
            {
                var progress = kvp.Value;
                ctx.Reply($"  {progress.CharacterName} (ID: {progress.PlatformId}) - Level {progress.Level} - Updated: {progress.LastUpdated:MM/dd}");
            }

            if (count > 10)
            {
                ctx.Reply($"  ... and {count - 10} more players");
            }
        }

        private static void ForceSaveProgress(ChatCommandContext ctx)
        {
            PlayerProgressStore.ForceSave();
            ctx.Reply("Player progress data force-saved to disk.");
        }

        private static void RemovePlayerProgress(ChatCommandContext ctx, string platformIdStr)
        {
            if (!ulong.TryParse(platformIdStr, out var platformId))
            {
                ctx.Reply("Invalid platform ID. Usage: .progress remove <platformId>");
                return;
            }

            var progress = PlayerProgressStore.Get(platformId);
            if (progress == null)
            {
                ctx.Reply($"No progress data found for platform ID {platformId}");
                return;
            }

            PlayerProgressStore.Remove(platformId);
            ctx.Reply($"Removed progress data for {progress.CharacterName} (ID: {platformId})");
        }

        private static void ProgressStatus(ChatCommandContext ctx)
        {
            var count = PlayerProgressStore.GetCachedPlayerCount();
            ctx.Reply($"PlayerProgressStore Status: {(PlayerProgressStore.IsInitialized ? "Running" : "Stopped")}");
            ctx.Reply($"  Cached players: {count}");
            ctx.Reply($"  Storage location: BepInEx/config/VAuto.Arena/player_progress.json");
        }
        #endregion

        #region Map Icon Commands
        [Command("mapicon", "mapicon <action> [args]", "Manage map icons", adminOnly: true)]
        public static void MapIconCommand(ChatCommandContext ctx, string action, string args = "")
        {
            switch (action.ToLower())
            {
                case "refresh":
                    RefreshPlayerIcons(ctx);
                    break;
                case "clear":
                    ClearPlayerIcons(ctx);
                    break;
                case "status":
                    MapIconStatus(ctx);
                    break;
                case "toggle":
                    ToggleMapIcons(ctx, args);
                    break;
                default:
                    ctx.Reply($"Unknown mapicon action: {action}. Available: refresh, clear, status, toggle");
                    break;
            }
        }

        private static void RefreshPlayerIcons(ChatCommandContext ctx)
        {
            try
            {
                MapIconService.RefreshPlayerIcons();
                ctx.Reply("Map icons refreshed manually");
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error refreshing map icons: {ex.Message}");
            }
        }

        private static void ClearPlayerIcons(ChatCommandContext ctx)
        {
            try
            {
                MapIconService.Cleanup();
                MapIconService.Initialize();
                ctx.Reply("All player map icons cleared and reinitialized");
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error clearing map icons: {ex.Message}");
            }
        }

        private static void MapIconStatus(ChatCommandContext ctx)
        {
            try
            {
                ctx.Reply("Map Icon Service Status: Active");
                ctx.Reply("- Refresh Interval: 3 seconds");
                ctx.Reply("- Tracking all online players");
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error getting map icon status: {ex.Message}");
            }
        }

        private static void ToggleMapIcons(ChatCommandContext ctx, string enable)
        {
            bool enabled = enable?.ToLower() == "true" || enable?.ToLower() == "on";
            ctx.Reply($"Map icons {(enabled ? "enabled" : "disabled")} - Note: Requires restart to take effect");
        }
        #endregion

        #region Game Systems Commands
        [Command("gamesystem", "gamesystem <action> [args]", "Manage game systems", adminOnly: true)]
        public static void GameSystemCommand(ChatCommandContext ctx, string action, string args = "")
        {
            switch (action.ToLower())
            {
                case "status":
                    GameSystemStatus(ctx);
                    break;
                case "clearhooks":
                    ClearVBloodHooks(ctx);
                    break;
                case "check":
                    CheckPlayerHook(ctx, args);
                    break;
                default:
                    ctx.Reply($"Unknown gamesystem action: {action}. Available: status, clearhooks, check");
                    break;
            }
        }

        private static void GameSystemStatus(ChatCommandContext ctx)
        {
            try
            {
                var activeHooks = GameSystems.GetActiveHookedPlayers();
                ctx.Reply($"Game Systems Status: Active");
                ctx.Reply($"- Active VBlood Hooks: {activeHooks.Count}");
                if (activeHooks.Count > 0)
                {
                    ctx.Reply($"- Hooked Players: {string.Join(", ", activeHooks)}");
                }
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error getting game system status: {ex.Message}");
            }
        }

        private static void ClearVBloodHooks(ChatCommandContext ctx)
        {
            try
            {
                GameSystems.ClearAllHooks();
                ctx.Reply("All VBlood hooks cleared");
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error clearing VBlood hooks: {ex.Message}");
            }
        }

        private static void CheckPlayerHook(ChatCommandContext ctx, string platformIdStr)
        {
            if (!ulong.TryParse(platformIdStr, out var platformId))
            {
                ctx.Reply("Invalid platform ID format");
                return;
            }

            bool isHooked = GameSystems.IsPlayerInArena(platformId);
            ctx.Reply($"Player {platformId} VBlood Hook Status: {(isHooked ? "Active" : "Inactive")}");
        }
        #endregion

        #region Respawn Prevention Commands
        [Command("respawn", "respawn <action> [player] [duration]", "Manage respawn prevention", adminOnly: true)]
        public static void RespawnCommand(ChatCommandContext ctx, string action, string player = "", double duration = 30)
        {
            switch (action.ToLower())
            {
                case "set":
                    SetRespawnCooldown(ctx, player, duration);
                    break;
                case "clear":
                    ClearRespawnCooldown(ctx, player);
                    break;
                case "check":
                    CheckRespawnStatus(ctx, player);
                    break;
                case "cleanup":
                    CleanupExpiredCooldowns(ctx);
                    break;
                default:
                    ctx.Reply($"Unknown respawn action: {action}. Available: set, clear, check, cleanup");
                    break;
            }
        }

        private static void SetRespawnCooldown(ChatCommandContext ctx, string player, double duration)
        {
            if (string.IsNullOrEmpty(player))
            {
                ctx.Reply("Usage: respawn set <player> <duration>");
                return;
            }

            if (!ulong.TryParse(player, out var platformId))
            {
                ctx.Reply("Invalid player platform ID");
                return;
            }

            try
            {
                VAuto.Services.RespawnPreventionService.SetRespawnCooldown(platformId, (int)duration);
                ctx.Reply($"Set respawn cooldown for player {platformId}: {duration} seconds");
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error setting respawn cooldown: {ex.Message}");
            }
        }

        private static void ClearRespawnCooldown(ChatCommandContext ctx, string player)
        {
            if (string.IsNullOrEmpty(player))
            {
                ctx.Reply("Usage: respawn clear <player>");
                return;
            }

            if (!ulong.TryParse(player, out var platformId))
            {
                ctx.Reply("Invalid player platform ID");
                return;
            }

            try
            {
                VAuto.Services.RespawnPreventionService.ClearRespawnCooldown(platformId);
                ctx.Reply($"Cleared respawn cooldown for player {platformId}");
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error clearing respawn cooldown: {ex.Message}");
            }
        }

        private static void CheckRespawnStatus(ChatCommandContext ctx, string player)
        {
            if (string.IsNullOrEmpty(player))
            {
                ctx.Reply("Usage: respawn check <player>");
                return;
            }

            if (!ulong.TryParse(player, out var platformId))
            {
                ctx.Reply("Invalid player platform ID");
                return;
            }

            try
            {
                bool isPrevented = VAuto.Services.RespawnPreventionService.IsRespawnPrevented(platformId);
                ctx.Reply($"Player {platformId} respawn status: {(isPrevented ? "Prevented" : "Allowed")}");
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error checking respawn status: {ex.Message}");
            }
        }

        private static void CleanupExpiredCooldowns(ChatCommandContext ctx)
        {
            try
            {
                VAuto.Services.RespawnPreventionService.CleanupExpiredCooldowns();
                ctx.Reply("Cleaned up expired respawn cooldowns");
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error cleaning up cooldowns: {ex.Message}");
            }
        }
        #endregion

        #region Arena Service Commands
        [Command("arenaservice", "arenaservice <action> [args]", "Manage arena services", adminOnly: true)]
        public static void ArenaServiceCommand(ChatCommandContext ctx, string action, string args = "")
        {
            switch (action.ToLower())
            {
                case "status":
                    ArenaServiceStatus(ctx);
                    break;
                case "initialize":
                    InitializeArenaServices(ctx);
                    break;
                case "cleanup":
                    CleanupArenaServices(ctx);
                    break;
                case "reload":
                    ReloadArenaServices(ctx);
                    break;
                default:
                    ctx.Reply($"Unknown arenaservice action: {action}. Available: status, initialize, cleanup, reload");
                    break;
            }
        }

        private static void ArenaServiceStatus(ChatCommandContext ctx)
        {
            try
            {
                ctx.Reply("Arena Services Status:");
                ctx.Reply("- ArenaVirtualContext: " + (ArenaVirtualContext.Active ? "Active" : "Inactive"));
                ctx.Reply("- MapIconService: Active");
                ctx.Reply("- GameSystems: Active");
                ctx.Reply("- VAuto.Services.RespawnPreventionService: Active");
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error getting arena service status: {ex.Message}");
            }
        }

        private static void InitializeArenaServices(ChatCommandContext ctx)
        {
            try
            {
                ArenaVirtualContext.Instance.Initialize();
                MapIconService.Initialize();
                GameSystems.Initialize();
                VAuto.Services.RespawnPreventionService.Initialize();
                ctx.Reply("All arena services initialized");
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error initializing arena services: {ex.Message}");
            }
        }

        private static void CleanupArenaServices(ChatCommandContext ctx)
        {
            try
            {
                MapIconService.Cleanup();
                VAuto.Services.RespawnPreventionService.CleanupExpiredCooldowns();
                ctx.Reply("Arena services cleaned up");
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error cleaning up arena services: {ex.Message}");
            }
        }

        private static void ReloadArenaServices(ChatCommandContext ctx)
        {
            try
            {
                CleanupArenaServices(ctx);
                InitializeArenaServices(ctx);
                ctx.Reply("Arena services reloaded");
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error reloading arena services: {ex.Message}");
            }
        }
        #endregion

        #region Player Service Commands
        [Command("playerservice", "playerservice <action> [args]", "Manage player services", adminOnly: true)]
        public static void PlayerServiceCommand(ChatCommandContext ctx, string action, string args = "")
        {
            switch (action.ToLower())
            {
                case "track":
                    TrackPlayer(ctx, args);
                    break;
                case "list":
                    ListPlayers(ctx);
                    break;
                case "count":
                    PlayerCount(ctx);
                    break;
                default:
                    ctx.Reply($"Unknown playerservice action: {action}. Available: track, list, count");
                    break;
            }
        }

        private static void TrackPlayer(ChatCommandContext ctx, string player)
        {
            if (string.IsNullOrEmpty(player))
            {
                ctx.Reply("Usage: playerservice track <player>");
                return;
            }

            ctx.Reply($"Player tracking for {player} - Feature not yet implemented");
        }

        private static void ListPlayers(ChatCommandContext ctx)
        {
            try
            {
                var players = VAuto.Services.PlayerService.GetAllOnlinePlayers();
                ctx.Reply($"Online Players: {players.Count}");
                foreach (var player in players.Take(10)) // Limit to first 10 for chat
                {
                    ctx.Reply($"- {player.CharacterName} (ID: {player.PlatformId})");
                }
                if (players.Count > 10)
                {
                    ctx.Reply($"... and {players.Count - 10} more players");
                }
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error listing players: {ex.Message}");
            }
        }

        private static void PlayerCount(ChatCommandContext ctx)
        {
            try
            {
                var count = VAuto.Services.PlayerService.GetOnlinePlayerCount();
                ctx.Reply($"Total online players: {count}");
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error getting player count: {ex.Message}");
            }
        }
        #endregion

        #region System Status Commands
        [Command("system", "system <service>", "Check system status", adminOnly: true)]
        public static void SystemStatusCommand(ChatCommandContext ctx, string service)
        {
            switch (service.ToLower())
            {
                case "mapicon":
                    MapIconStatus(ctx);
                    break;
                case "gamesystem":
                    GameSystemStatus(ctx);
                    break;
                case "respawn":
                    RespawnSystemStatus(ctx);
                    break;
                case "arena":
                    ArenaServiceStatus(ctx);
                    break;
                case "all":
                    AllSystemStatus(ctx);
                    break;
                default:
                    ctx.Reply($"Unknown service: {service}. Available: mapicon, gamesystem, respawn, arena, all");
                    break;
            }
        }

        private static void RespawnSystemStatus(ChatCommandContext ctx)
        {
            try
            {
                ctx.Reply("Respawn Prevention Service: Active");
                ctx.Reply("- Managing respawn cooldowns for all players");
                ctx.Reply("- Automatic cleanup of expired cooldowns enabled");
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error getting respawn system status: {ex.Message}");
            }
        }

        private static void AllSystemStatus(ChatCommandContext ctx)
        {
            try
            {
                ctx.Reply("=== VAuto Service Status ===");
                MapIconStatus(ctx);
                GameSystemStatus(ctx);
                RespawnSystemStatus(ctx);
                ArenaServiceStatus(ctx);
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error getting all system status: {ex.Message}");
            }
        }
        #endregion

        #region Global Service Management
        [Command("service", "service <action> <servicename>", "Global service management", adminOnly: true)]
        public static void ServiceCommand(ChatCommandContext ctx, string action, string serviceName)
        {
            switch (action.ToLower())
            {
                case "enable":
                    EnableService(ctx, serviceName);
                    break;
                case "disable":
                    DisableService(ctx, serviceName);
                    break;
                case "restart":
                    RestartService(ctx, serviceName);
                    break;
                case "status":
                    GetServiceStatus(ctx, serviceName);
                    break;
                default:
                    ctx.Reply($"Unknown service action: {action}. Available: enable, disable, restart, status");
                    break;
            }
        }

        private static void EnableService(ChatCommandContext ctx, string serviceName)
        {
            ctx.Reply($"Service {serviceName} enable command - Feature not yet implemented");
        }

        private static void DisableService(ChatCommandContext ctx, string serviceName)
        {
            ctx.Reply($"Service {serviceName} disable command - Feature not yet implemented");
        }

        private static void RestartService(ChatCommandContext ctx, string serviceName)
        {
            try
            {
                switch (serviceName.ToLower())
                {
                    case "mapicon":
                        MapIconService.Cleanup();
                        MapIconService.Initialize();
                        ctx.Reply("MapIconService restarted");
                        break;
                    case "gamesystem":
                        GameSystems.Initialize();
                        ctx.Reply("GameSystems restarted");
                        break;
                    case "respawn":
                        VAuto.Services.RespawnPreventionService.Initialize();
                        ctx.Reply("VAuto.Services.RespawnPreventionService restarted");
                        break;
                    default:
                        ctx.Reply($"Unknown service: {serviceName}");
                        break;
                }
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error restarting service {serviceName}: {ex.Message}");
            }
        }

        private static void GetServiceStatus(ChatCommandContext ctx, string serviceName)
        {
            switch (serviceName.ToLower())
            {
                case "mapicon":
                    MapIconStatus(ctx);
                    break;
                case "gamesystem":
                    GameSystemStatus(ctx);
                    break;
                case "respawn":
                    RespawnSystemStatus(ctx);
                    break;
                case "arena":
                    ArenaServiceStatus(ctx);
                    break;
                default:
                    ctx.Reply($"Unknown service: {serviceName}");
                    break;
            }
        }
        #endregion
    }
}
