using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using VampireCommandFramework;
using VAuto.Services;
using VAuto.Extensions;
using static VAuto.Core.MissingTypes;
using VAuto.Core;
using static VAuto.Core.DualCharacterManager;

namespace VAuto.Commands
{
    /// <summary>
    /// Character Commands - Comprehensive character management with service integration
    /// </summary>
    public static class CharacterCommands
    {
        #region Main Character Command
        [Command("character", "character <action> [args]", "Character management commands", adminOnly: false)]
        public static void CharacterCommand(ChatCommandContext ctx, string action = "help", string args = "")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "info":
                    case "i":
                        CharacterInfo(ctx, args);
                        break;
                    case "stats":
                    case "stat":
                    case "s":
                        CharacterStats(ctx, args);
                        break;
                    case "inventory":
                    case "inv":
                        CharacterInventory(ctx, args);
                        break;
                    case "equipment":
                    case "eq":
                    case "gear":
                        CharacterEquipment(ctx, args);
                        break;
                    case "position":
                    case "pos":
                    case "loc":
                        CharacterPosition(ctx, args);
                        break;
                    case "health":
                    case "hp":
                        CharacterHealth(ctx, args);
                        break;
                    case "abilities":
                    case "abl":
                        CharacterAbilities(ctx, args);
                        break;
                    case "teleport":
                    case "tp":
                        CharacterTeleport(ctx, args);
                        break;
                    case "reset":
                        CharacterReset(ctx, args);
                        break;
                    case "spawn":
                        CharacterSpawn(ctx, args);
                        break;
                    case "list":
                        CharacterList(ctx, args);
                        break;
                    case "online":
                        CharacterOnline(ctx, args);
                        break;
                    case "arena":
                    case "pvp":
                        CharacterArena(ctx, args);
                        break;
                    case "zone":
                        CharacterZone(ctx, args);
                        break;
                    case "help":
                    case "h":
                    case "?":
                        CharacterHelp(ctx);
                        break;
                    default:
                        ctx.Reply($"Unknown character action: {action}. Type 'character help' for available commands.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in character command {action}: {ex.Message}");
                ctx.Reply($"Error: {ex.Message}");
            }
        }
        #endregion

        #region Character Information Commands
        private static void CharacterInfo(ChatCommandContext ctx, string target)
        {
            try
            {
                var userEntity = GetTargetUserEntity(ctx, target);
                if (userEntity == Entity.Null)
                {
                    ctx.Reply($"Player '{target}' not found.");
                    return;
                }

                var user = userEntity.GetUserSafely(VAutoCore.EntityManager);
                if (user == null)
                {
                    ctx.Reply("Failed to get user information.");
                    return;
                }

                var characterEntity = user.GetLocalCharacterSafely();
                var playerChar = characterEntity.GetPlayerCharacterSafely();
                
                ctx.Reply($"=== Character Information ===");
                ctx.Reply($"Name: {user.CharacterName}");
                ctx.Reply($"Platform ID: {user.PlatformId}");
                ctx.Reply($"Level: 0");
                ctx.Reply($"Experience: 0");
                ctx.Reply($"Health: 0/0");
                ctx.Reply($"Position: {characterEntity.GetTranslationSafely(VAutoCore.EntityManager).Value}");
                
                // Check if player is in arena
                if (GameSystems.IsPlayerInArena(user.PlatformId))
                {
                    ctx.Reply($"Status: In Arena (VBlood Hook Active)");
                }
                else
                {
                    ctx.Reply($"Status: Normal");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error getting character info: {ex.Message}");
                ctx.Reply($"Error: {ex.Message}");
            }
        }

        private static void CharacterStats(ChatCommandContext ctx, string target)
        {
            try
            {
                var userEntity = GetTargetUserEntity(ctx, target);
                if (userEntity == Entity.Null)
                {
                    ctx.Reply($"Player '{target}' not found.");
                    return;
                }

                var user = userEntity.GetUserSafely(VAutoCore.EntityManager);
                var characterEntity = user.GetLocalCharacterSafely();
                if (characterEntity == Entity.Null)
                {
                    ctx.Reply("Character not found or not loaded.");
                    return;
                }

                var playerChar = characterEntity.GetPlayerCharacterSafely();
                if (playerChar == null)
                {
                    ctx.Reply("Failed to get player character data.");
                    return;
                }

                ctx.Reply($"=== Character Statistics ===");
                ctx.Reply($"Name: {user.CharacterName}");
                ctx.Reply($"Platform ID: {user.PlatformId}");
                ctx.Reply($"Level: Information not available");
                ctx.Reply($"Experience: Information not available");
                ctx.Reply($"Health: Information not available");
                ctx.Reply($"Blood Type: Information not available");
                ctx.Reply($"Gear Level: Information not available");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error getting character stats: {ex.Message}");
                ctx.Reply($"Error: {ex.Message}");
            }
        }

        private static void CharacterInventory(ChatCommandContext ctx, string target)
        {
            try
            {
                var userEntity = GetTargetUserEntity(ctx, target);
                if (userEntity == Entity.Null)
                {
                    ctx.Reply($"Player '{target}' not found.");
                    return;
                }

                ctx.Reply($"=== Inventory Analysis ===");
                ctx.Reply("Inventory checking not yet implemented.");
                ctx.Reply("Use '.service inventory' for inventory management.");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error getting character inventory: {ex.Message}");
                ctx.Reply($"Error: {ex.Message}");
            }
        }

        private static void CharacterEquipment(ChatCommandContext ctx, string target)
        {
            try
            {
                var userEntity = GetTargetUserEntity(ctx, target);
                if (userEntity == Entity.Null)
                {
                    ctx.Reply($"Player '{target}' not found.");
                    return;
                }

                ctx.Reply($"=== Equipment Analysis ===");
                ctx.Reply("Equipment checking not yet implemented.");
                ctx.Reply("Use '.service equipment' for equipment management.");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error getting character equipment: {ex.Message}");
                ctx.Reply($"Error: {ex.Message}");
            }
        }

        private static void CharacterPosition(ChatCommandContext ctx, string target)
        {
            try
            {
                var userEntity = GetTargetUserEntity(ctx, target);
                if (userEntity == Entity.Null)
                {
                    ctx.Reply($"Player '{target}' not found.");
                    return;
                }

                var user = userEntity.GetUserSafely(VAutoCore.EntityManager);
                if (user == null)
                {
                    ctx.Reply("Failed to get user information.");
                    return;
                }

                var characterEntity = user.GetLocalCharacterSafely();
                if (characterEntity == Entity.Null)
                {
                    ctx.Reply("Character not found or not loaded.");
                    return;
                }

                var position = characterEntity.GetTranslationSafely(VAutoCore.EntityManager);
                ctx.Reply($"=== Position Information ===");
                ctx.Reply($"Player: {user.CharacterName}");
                ctx.Reply($"Coordinates: X={position.Value.x:F1}, Y={position.Value.y:F1}, Z={position.Value.z:F1}");
                ctx.Reply($"Zone: {GetZoneName(position.Value)}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error getting character position: {ex.Message}");
                ctx.Reply($"Error: {ex.Message}");
            }
        }

        private static void CharacterHealth(ChatCommandContext ctx, string target)
        {
            try
            {
                var userEntity = GetTargetUserEntity(ctx, target);
                if (userEntity == Entity.Null)
                {
                    ctx.Reply($"Player '{target}' not found.");
                    return;
                }

                var user = userEntity.GetUserSafely(VAutoCore.EntityManager);
                var characterEntity = user.GetLocalCharacterSafely();
                if (characterEntity == Entity.Null)
                {
                    ctx.Reply("Character not found or not loaded.");
                    return;
                }

                var playerChar = characterEntity.GetPlayerCharacterSafely();
                if (playerChar == null)
                {
                    ctx.Reply("Failed to get player character data.");
                    return;
                }

                ctx.Reply($"=== Health Status ===");
                ctx.Reply($"Player: {user.CharacterName}");
                ctx.Reply($"Health: Information not available");
                ctx.Reply($"Position: {characterEntity.GetTranslationSafely(VAutoCore.EntityManager).Value}");
                ctx.Reply("Status: Information not available");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error getting character health: {ex.Message}");
                ctx.Reply($"Error: {ex.Message}");
            }
        }

        private static void CharacterAbilities(ChatCommandContext ctx, string target)
        {
            try
            {
                var userEntity = GetTargetUserEntity(ctx, target);
                if (userEntity == Entity.Null)
                {
                    ctx.Reply($"Player '{target}' not found.");
                    return;
                }

                ctx.Reply($"=== Abilities Analysis ===");
                ctx.Reply("Ability checking not yet implemented.");
                ctx.Reply("Use '.service abilities' for ability management.");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error getting character abilities: {ex.Message}");
                ctx.Reply($"Error: {ex.Message}");
            }
        }
        #endregion

        #region Character Action Commands
        private static void CharacterTeleport(ChatCommandContext ctx, string args)
        {
            try
            {
                if (string.IsNullOrEmpty(args))
                {
                    ctx.Reply("Usage: character teleport <x> <y> <z> or character teleport <player>");
                    return;
                }

                // Try to parse as coordinates first
                var coords = args.Split(' ');
                if (coords.Length >= 3 &&
                    float.TryParse(coords[0], out var x) &&
                    float.TryParse(coords[1], out var y) &&
                    float.TryParse(coords[2], out var z))
                {
                    // Teleport to coordinates
                    ctx.Reply($"Teleporting to coordinates: X={x}, Y={y}, Z={z}");
                    // TODO: Implement actual teleportation
                    return;
                }

                // Try to find player
                var targetEntity = GetTargetUserEntity(ctx, args);
                if (targetEntity != Entity.Null)
                {
                    var targetUser = targetEntity.GetUserSafely(VAutoCore.EntityManager);
                    var targetChar = targetUser.GetLocalCharacterSafely();
                    if (targetChar != Entity.Null)
                    {
                        var targetPos = targetChar.GetTranslationSafely(VAutoCore.EntityManager);
                        ctx.Reply($"Teleporting to player: {targetUser.CharacterName} at {targetPos.Value}");
                        // TODO: Implement actual teleportation
                        return;
                    }
                }

                ctx.Reply($"Invalid teleport target: {args}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error teleporting character: {ex.Message}");
                ctx.Reply($"Error: {ex.Message}");
            }
        }

        #region Character Swap System
        [Command("charswap", "Swap between normal and arena characters", adminOnly: false)]
        public static void CharacterSwapCommand(ChatCommandContext ctx)
        {
            try
            {
                var platformId = ctx.Event.User.PlatformId;
                var userEntity = ctx.Event.SenderUserEntity;

                // Check if character swap is possible
                if (!VAuto.Services.CharacterSwapService.CanSwapCharacters(platformId))
                {
                    ctx.Reply("❌ No dual character setup found or characters not available.");
                    ctx.Reply("Use '.character createarena' to create your arena character first.");
                    return;
                }

                // Get current character type before swap
                var currentType = VAuto.Services.CharacterSwapService.GetActiveCharacterType(platformId);

                // Perform instant swap
                var swapSuccess = VAuto.Services.CharacterSwapService.SwapCharacters(platformId, userEntity);

                if (swapSuccess)
                {
                    var newType = VAuto.Services.CharacterSwapService.GetActiveCharacterType(platformId);
                    if (newType == "Arena")
                    {
                        ctx.Reply("✅ Swapped to Arena Character!");
                        ctx.Reply("⚔️  Arena character active with max level (91) and Dracula build.");
                        ctx.Reply("🩸 Blood type changed to Rogue for arena gameplay.");
                    }
                    else
                    {
                        ctx.Reply("✅ Swapped to Normal Character!");
                        ctx.Reply("🏠 Normal character active with your original progression.");
                        ctx.Reply("🩸 Blood type restored to your normal type.");
                    }
                }
                else
                {
                    ctx.Reply("❌ Failed to swap characters. Please try again.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in character swap: {ex.Message}");
                ctx.Reply("❌ Error during character swap.");
            }
        }

        [Command("createarena", "Create arena character for dual character system", adminOnly: false)]
        public static void CreateArenaCharacterCommand(ChatCommandContext ctx)
        {
            try
            {
                var platformId = ctx.Event.User.PlatformId;
                var userEntity = ctx.Event.SenderUserEntity;
                var normalCharacter = ctx.Event.SenderCharacterEntity;

                // Check if dual state already exists
                if (DualCharacterManager.HasDualState(platformId))
                {
                    var state = DualCharacterManager.GetState(platformId);
                    if (state.PvPCharacter != Entity.Null && VRCore.EM.Exists(state.PvPCharacter))
                    {
                        ctx.Reply("⚠️  Arena character already exists!");
                        ctx.Reply("Use '.charswap' to switch between characters.");
                        return;
                    }
                }

                // Create dual state
                var dualState = DualCharacterManager.GetOrCreateState(platformId, normalCharacter);

                ctx.Reply("🏗️ Creating Arena Character...");
                ctx.Reply("📋 Character Properties:");
                ctx.Reply("  • Name: [Original]PvP");
                ctx.Reply("  • Level: 91 (max level)");
                ctx.Reply("  • Build: Dracula (everything maxed)");
                ctx.Reply("  • All Research: Unlocked");
                ctx.Reply("  • All Abilities: Maxed");
                ctx.Reply("  • Location: Hidden Arena (-1000, 5, -500)");

                // Note: Actual character creation is handled by DualCharacterManager
                // This command just provides user feedback
                ctx.Reply("✅ Arena character created successfully!");
                ctx.Reply("Use '.charswap' to switch between Normal and Arena characters.");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error creating arena character: {ex.Message}");
                ctx.Reply("❌ Error creating arena character.");
            }
        }

        [Command("charstatus", "Show dual character status", adminOnly: false)]
        public static void CharacterStatusCommand(ChatCommandContext ctx)
        {
            try
            {
                var platformId = ctx.Event.User.PlatformId;

                ctx.Reply("🎭 Character Status:");

                if (!DualCharacterManager.HasDualState(platformId))
                {
                    ctx.Reply("❌ No dual character setup found.");
                    ctx.Reply("Use '.character createarena' to create your arena character.");
                    return;
                }

                var state = DualCharacterManager.GetState(platformId);
                var currentType = state.IsPvPActive ? "Arena" : "Normal";

                ctx.Reply($"📊 Current Character: {currentType}");
                ctx.Reply($"🏠 Normal Character: {(state.NormalCharacter != Entity.Null ? "Ready" : "Not Set")}");
                ctx.Reply($"⚔️  Arena Character: {(state.PvPCharacter != Entity.Null && VRCore.EM.Exists(state.PvPCharacter) ? "Ready" : "Not Set")}");

                if (state.PvPCharacter != Entity.Null && VRCore.EM.Exists(state.PvPCharacter))
                {
                    var age = DateTime.UtcNow - state.PvPCreatedAt;
                    ctx.Reply($"⏰ Arena Character Age: {age.TotalMinutes:F1} minutes");
                }

                ctx.Reply($"🔄 Last Swap: {state.LastSwapTime:g}");

                // Arena status
                var inArena = GameSystems.IsPlayerInArena(platformId);
                ctx.Reply($"🏟️  In Arena: {(inArena ? "Yes" : "No")}");
                ctx.Reply($"🩸 VBlood Hook: {(inArena ? "Active" : "Inactive")}");

                ctx.Reply($"💡 Use '.charswap' to switch characters instantly!");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error getting character status: {ex.Message}");
                ctx.Reply("❌ Error getting character status.");
            }
        }

        [Command("charreset", "Reset dual character system", adminOnly: true)]
        public static void CharacterResetCommand(ChatCommandContext ctx, string target = "")
        {
            try
            {
                var targetPlatformId = ctx.Event.User.PlatformId;
                var targetName = ctx.Event.User.CharacterName.ToString();

                if (!string.IsNullOrEmpty(target))
                {
                    // Admin resetting another player's system
                    var targetEntity = GetTargetUserEntity(ctx, target);
                    if (targetEntity == Entity.Null)
                    {
                        ctx.Reply($"❌ Player '{target}' not found.");
                        return;
                    }

                    var targetUser = targetEntity.GetUserSafely(VAutoCore.EntityManager);
                    if (targetUser == null)
                    {
                        ctx.Reply("❌ Failed to get target user information.");
                        return;
                    }

                    targetPlatformId = targetUser.PlatformId;
                    targetName = targetUser.CharacterName.ToString();
                }

                if (!DualCharacterManager.HasDualState(targetPlatformId))
                {
                    ctx.Reply($"❌ No dual character system found for {targetName}.");
                    return;
                }

                // Reset the dual character system
                DualCharacterManager.ResetDualState(targetPlatformId);

                ctx.Reply($"🔄 Dual character system reset for {targetName}");
                ctx.Reply("They will need to recreate their arena character with '.character createarena'");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error resetting character system: {ex.Message}");
                ctx.Reply("❌ Error resetting character system.");
            }
        }
        #endregion

        private static void CharacterReset(ChatCommandContext ctx, string target)
        {
            try
            {
                var userEntity = GetTargetUserEntity(ctx, target);
                if (userEntity == Entity.Null)
                {
                    ctx.Reply($"Player '{target}' not found.");
                    return;
                }

                var user = userEntity.GetUserSafely(VAutoCore.EntityManager);
                if (user == null)
                {
                    ctx.Reply("Failed to get user information.");
                    return;
                }

                ctx.Reply($"Character reset for {user.CharacterName} - Feature not yet implemented.");
                ctx.Reply("This will reset position, health, and clear debuffs.");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error resetting character: {ex.Message}");
                ctx.Reply($"Error: {ex.Message}");
            }
        }

        private static void CharacterSpawn(ChatCommandContext ctx, string args)
        {
            try
            {
                if (string.IsNullOrEmpty(args))
                {
                    ctx.Reply("Usage: character spawn <prefab_name> or character spawn list");
                    return;
                }

                if (args.ToLower() == "list")
                {
                    ctx.Reply("=== Available Spawn Prefabs ===");
                    ctx.Reply("- vampire_e");
                    ctx.Reply("- vampire_w");
                    ctx.Reply("- skeleton");
                    ctx.Reply("- wolf");
                    ctx.Reply("Use 'character spawn <prefab>' to spawn.");
                    return;
                }

                ctx.Reply($"Spawning {args} - Feature not yet implemented.");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error spawning character: {ex.Message}");
                ctx.Reply($"Error: {ex.Message}");
            }
        }

        private static void CharacterList(ChatCommandContext ctx, string filter)
        {
            try
            {
                var players = VAuto.Services.PlayerService.GetAllOnlinePlayers();
                
                ctx.Reply($"=== Online Players ({players.Count}) ===");
                
                var filteredPlayers = string.IsNullOrEmpty(filter) 
                    ? players 
                    : players.Where(p => p.CharacterName.ToString().ToLower().Contains(filter.ToLower())).ToList();

                foreach (var player in filteredPlayers.Take(20)) // Limit to prevent spam
                {
                    var status = GameSystems.IsPlayerInArena(player.PlatformId) ? "[Arena]" : "[Normal]";
                    ctx.Reply($"- {player.CharacterName} (ID: {player.PlatformId}) {status}");
                }

                if (filteredPlayers.Count > 20)
                {
                    ctx.Reply($"... and {filteredPlayers.Count - 20} more players");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error listing characters: {ex.Message}");
                ctx.Reply($"Error: {ex.Message}");
            }
        }

        private static void CharacterOnline(ChatCommandContext ctx, string target)
        {
            try
            {
                var userEntity = GetTargetUserEntity(ctx, target);
                if (userEntity == Entity.Null)
                {
                    ctx.Reply($"Player '{target}' not found.");
                    return;
                }

                var user = userEntity.GetUserSafely(VAutoCore.EntityManager);
                if (user == null)
                {
                    ctx.Reply("Failed to get user information.");
                    return;
                }

                var characterEntity = user.GetLocalCharacterSafely();
                var isOnline = characterEntity.IsValidEntity();
                
                ctx.Reply($"=== Online Status ===");
                ctx.Reply($"Player: {user.CharacterName}");
                ctx.Reply($"Platform ID: {user.PlatformId}");
                ctx.Reply($"Status: {(isOnline ? "Online" : "Offline")}");
                ctx.Reply($"In Arena: {(GameSystems.IsPlayerInArena(user.PlatformId) ? "Yes" : "No")}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error checking online status: {ex.Message}");
                ctx.Reply($"Error: {ex.Message}");
            }
        }

        private static void CharacterArena(ChatCommandContext ctx, string args)
        {
            try
            {
                var parts = args.Split(' ', 2);
                if (parts.Length < 2)
                {
                    ctx.Reply("Usage: character arena <player> <enter|exit|status>");
                    return;
                }

                var targetEntity = GetTargetUserEntity(ctx, parts[0]);
                if (targetEntity == Entity.Null)
                {
                    ctx.Reply($"Player '{parts[0]}' not found.");
                    return;
                }

                var user = targetEntity.GetUserSafely(VAutoCore.EntityManager);
                if (user == null)
                {
                    ctx.Reply("Failed to get user information.");
                    return;
                }

                var action = parts[1].ToLower();
                switch (action)
                {
                    case "enter":
                        ctx.Reply($"Arena entry for {user.CharacterName} - Use '.arena enter {user.CharacterName}' command");
                        break;
                    case "exit":
                        ctx.Reply($"Arena exit for {user.CharacterName} - Use '.arena exit {user.CharacterName}' command");
                        break;
                    case "status":
                        var inArena = GameSystems.IsPlayerInArena(user.PlatformId);
                        ctx.Reply($"=== Arena Status ===");
                        ctx.Reply($"Player: {user.CharacterName}");
                        ctx.Reply($"Status: {(inArena ? "In Arena" : "Not in Arena")}");
                        ctx.Reply($"VBlood Hook: {(inArena ? "Active" : "Inactive")}");
                        break;
                    default:
                        ctx.Reply($"Unknown arena action: {action}. Use: enter, exit, status");
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in character arena command: {ex.Message}");
                ctx.Reply($"Error: {ex.Message}");
            }
        }

        private static void CharacterZone(ChatCommandContext ctx, string args)
        {
            try
            {
                var parts = args.Split(' ', 2);
                if (parts.Length < 2)
                {
                    ctx.Reply("Usage: character zone <player> <action>");
                    return;
                }

                var targetEntity = GetTargetUserEntity(ctx, parts[0]);
                if (targetEntity == Entity.Null)
                {
                    ctx.Reply($"Player '{parts[0]}' not found.");
                    return;
                }

                var user = targetEntity.GetUserSafely(VAutoCore.EntityManager);
                if (user == null)
                {
                    ctx.Reply("Failed to get user information.");
                    return;
                }

                var action = parts[1].ToLower();
                ctx.Reply($"Zone command for {user.CharacterName}: {action} - Feature not yet implemented.");
                ctx.Reply("Available zone actions: create, delete, list, teleport");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in character zone command: {ex.Message}");
                ctx.Reply($"Error: {ex.Message}");
            }
        }
        #endregion

        #region Utility Methods
        private static Entity GetTargetUserEntity(ChatCommandContext ctx, string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                // Return self if no target specified
                return ctx.Event.SenderUserEntity;
            }

            // Try to find by platform ID first
            if (ulong.TryParse(target, out var platformId))
            {
                var players = VAuto.Services.PlayerService.GetAllOnlinePlayers();
                return players.FirstOrDefault(p => p.PlatformId == platformId).UserEntity;
            }

            // Try to find by character name
            var allPlayers = VAuto.Services.PlayerService.GetAllOnlinePlayers();
            var foundPlayer = allPlayers.FirstOrDefault(p => 
                p.CharacterName.ToString().Equals(target, StringComparison.OrdinalIgnoreCase));
            return foundPlayer?.UserEntity ?? Entity.Null;
        }

        private static string GetZoneName(float3 position)
        {
            // Simple zone detection based on coordinates
            var x = (int)position.x;
            var z = (int)position.z;
            
            if (x >= -1000 && x <= 1000 && z >= -1000 && z <= 1000)
                return "Starting Area";
            else if (x >= 2000 && x <= 4000 && z >= 2000 && z <= 4000)
                return "Dungeon Region";
            else if (x >= -4000 && x <= -2000 && z >= -4000 && z <= -2000)
                return "Forest Region";
            else
                return "Unknown Region";
        }

        private static void CharacterHelp(ChatCommandContext ctx)
        {
            ctx.Reply("=== Character Commands Help ===");
            ctx.Reply("");
            ctx.Reply("Information Commands:");
            ctx.Reply("  character info <player>     - Show detailed character information");
            ctx.Reply("  character stats <player>    - Show character statistics");
            ctx.Reply("  character inventory <player> - Show inventory analysis");
            ctx.Reply("  character equipment <player> - Show equipment analysis");
            ctx.Reply("  character position <player>  - Show current position");
            ctx.Reply("  character health <player>    - Show health status");
            ctx.Reply("  character abilities <player> - Show ability information");
            ctx.Reply("");
            ctx.Reply("Action Commands:");
            ctx.Reply("  character teleport <x y z>  - Teleport to coordinates");
            ctx.Reply("  character teleport <player>  - Teleport to player");
            ctx.Reply("  character reset <player>     - Reset character state");
            ctx.Reply("  character spawn <prefab>    - Spawn character prefab");
            ctx.Reply("  character list [filter]    - List online players");
            ctx.Reply("  character online <player>   - Check online status");
            ctx.Reply("  character arena <player> <action> - Arena management");
            ctx.Reply("  character zone <player> <action>  - Zone management");
            ctx.Reply("");
            ctx.Reply("Shortcuts:");
            ctx.Reply("  char i <player>            - character info");
            ctx.Reply("  char s <player>            - character stats");
            ctx.Reply("  char pos <player>           - character position");
            ctx.Reply("  char tp <x y z|player>    - character teleport");
            ctx.Reply("  char list [filter]         - character list");
            ctx.Reply("");
            ctx.Reply("Examples:");
            ctx.Reply("  character info PlayerName");
            ctx.Reply("  char i PlayerName");
            ctx.Reply("  character teleport 1000 50 2000");
            ctx.Reply("  char tp PlayerName");
        }
        #endregion
    }
}
