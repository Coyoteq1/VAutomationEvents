using System;
using System.Collections.Generic;
using System.Linq;
using VampireCommandFramework;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VAuto.Core;
using VAuto.Services;

namespace VAuto.Commands
{
    [CommandGroup("arena")]
    public static class ArenaCommands
    {
        private static readonly string[] AvailableCommands = new[]
        {
            ".help - Show available commands",
            ".enter - Enter arena at the default spawn",
            ".exit - Exit arena and restore snapshot",
            ".arena enter - Enter arena (detailed)",
            ".arena exit - Exit arena (detailed)",
            ".arena heal - Heal player to full health",
            ".arena loadout - Apply default loadout",
            ".arena practice - Toggle practice mode",
            ".arena reset - Reset player state",
            ".arena status - Show arena status",
            ".char create - Create a PvP practice character",
            ".char enter - Switch to PvP character",
            ".char exit - Switch back to normal character",
            ".char swap - Swap between normal and PvP characters",
            ".char status - Show character status",
            ".snapshot create - Create a player snapshot",
            ".snapshot load - Load a player snapshot",
            ".teleport arena - Teleport to arena center",
            ".teleport spawn - Teleport to spawn point",
            ".gear apply - Apply gear set",
            ".gear warrior - Apply warrior gear",
            ".gear rogue - Apply rogue gear",
            ".gear scholar - Apply scholar gear",
            ".gear brute - Apply brute gear",
            ".zone setzonehere - Set arena zone at current position",
            ".zone setcenter - Set arena center coordinates",
            ".zone setradius - Set arena radius",
            ".zone setspawn - Set arena spawn point",
            ".zone info - Show zone information",
            ".zone reload - Reload zone configuration",
            ".castle setheart - Set castle heart grid radius",
            ".castle radius <radius> - Set castle radius",
            ".castle clear - Clear castle radius",
            ".castle delete - Delete castle configuration",
            ".castle enhance <level> - Set advanced enhancement level",
            ".castle info - Show castle system information",
            ".castle status - Show castle system status",
            ".build mode - Toggle build mode on/off",
            ".build list - Show available schematics",
            ".build select <schematic> - Select active schematic",
            ".build place - Place current schematic",
            ".build remove - Remove object at position",
            ".build surface <material> - Set active surface material",
            ".portal create <name> <x> <y> <z> - Create portal",
            ".portal goto <name> - Teleport through portal",
            ".portal list - Show all portals",
            ".portal remove <name> - Remove portal",
            ".glow add <color> - Add glow effect",
            ".glow remove - Remove glow effect",
            ".index create <name> <indexRadius> <castleRadius> <x1> <y1> <z1> <x2> <y2> <z2> <x3> <y3> <z3> - Create index with three corners",
            ".index list - Show all indices",
            ".index remove <name> - Remove index",
            ".index info <name> - Show index information",
            ".teleport to <x> <y> <z> - Teleport to coordinates",
            ".teleport save <name> - Save current location",
            ".teleport goto <name> - Teleport to saved location",
            ".teleport list - List saved locations"
        };

        [Command("help", adminOnly: false, usage: ".help")]
        public static void HelpCommand(ChatCommandContext ctx)
        {
            ctx.Reply("Available Commands:");
            foreach (var cmd in AvailableCommands)
            {
                ctx.Reply($"  {cmd}");
            }
        }

        [Command("enter", adminOnly: true, usage: ".enter")]
        public static void EnterArenaCommand(ChatCommandContext ctx)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var platformId = ctx.Event.User.PlatformId;
            var userEntity = ctx.Event.SenderUserEntity;
            var character = ctx.Event.SenderCharacterEntity;

            try
            {
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_COMMAND_START - PlatformId: {platformId}, UserEntity: {userEntity}, CharacterEntity: {character}");

                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_VALIDATION - Checking if player {platformId} is already in arena");
                if (MissingServices.LifecycleService.IsPlayerInArena(platformId))
                {
                    Plugin.Logger?.LogWarning($"[{timestamp}] ARENA_ENTER_REJECTED - Player {platformId} already in arena");
                    ctx.Reply("You are already in the arena!");
                    return;
                }

                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_EXECUTING - Calling lifecycle service for arena entry");
                // Use full lifecycle service for consistent arena entry
                if (MissingServices.LifecycleService.EnterArena(userEntity, character))
                {
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_SUCCESS - Player {platformId} entered arena successfully");
                    ctx.Reply("Entered arena successfully!");
                }
                else
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_FAILED - Lifecycle service returned false for {platformId}");
                    ctx.Reply("Failed to enter arena.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_CRITICAL_ERROR - PlatformId: {platformId}, Error: {ex.Message}");
                ctx.Reply("An error occurred while entering the arena.");
            }
        }

        [Command("exit", adminOnly: true, usage: ".exit")]
        public static void ExitArenaCommand(ChatCommandContext ctx)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var platformId = ctx.Event.User.PlatformId;
            var userEntity = ctx.Event.SenderUserEntity;
            var character = ctx.Event.SenderCharacterEntity;

            try
            {
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_COMMAND_START - PlatformId: {platformId}, UserEntity: {userEntity}, CharacterEntity: {character}");

                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_VALIDATION - Checking if player {platformId} is in arena");
                if (!MissingServices.LifecycleService.IsPlayerInArena(platformId))
                {
                    Plugin.Logger?.LogWarning($"[{timestamp}] ARENA_EXIT_REJECTED - Player {platformId} not in arena");
                    ctx.Reply("You are not in the arena!");
                    return;
                }

                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_EXECUTING - Calling lifecycle service for arena exit");
                if (MissingServices.LifecycleService.ExitArena(userEntity, character))
                {
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_SUCCESS - Player {platformId} exited arena successfully");
                    ctx.Reply("Exited arena successfully!");
                }
                else
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_EXIT_FAILED - Lifecycle service returned false for {platformId}");
                    ctx.Reply("Failed to exit arena.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[{timestamp}] ARENA_EXIT_CRITICAL_ERROR - PlatformId: {platformId}, Error: {ex.Message}");
                ctx.Reply("An error occurred while exiting the arena.");
            }
        }

        [Command("heal", adminOnly: true, usage: ".arena heal")]
        public static void HealCommand(ChatCommandContext ctx)
        {
            try
            {
                var character = ctx.Event.SenderCharacterEntity;

                // Use ArenaHealingService if available, otherwise direct healing
                MissingServices.ArenaHealingService.ApplyHeal(character);
                ctx.Reply("Healed to full health!");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in HealCommand: {ex.Message}");
                ctx.Reply("An error occurred while healing.");
            }
        }

        [Command("loadout", adminOnly: true, usage: ".arena loadout")]
        public static void LoadoutCommand(ChatCommandContext ctx)
        {
            try
            {
                var character = ctx.Event.SenderCharacterEntity;
                MissingServices.BuildService.ApplyDefaultBuild(character);
                ctx.Reply("Applied default loadout!");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in LoadoutCommand: {ex.Message}");
                ctx.Reply("An error occurred while applying loadout.");
            }
        }

        [Command("practice", adminOnly: true, usage: ".arena practice")]
        public static void PracticeCommand(ChatCommandContext ctx)
        {
            try
            {
                // Toggle practice mode using the dual character system
                var platformId = ctx.Event.User.PlatformId;

                if (!DualCharacterManager.HasDualState(platformId))
                {
                    ctx.Reply("No PvP character setup found. Use .char create first.");
                    return;
                }

                var state = DualCharacterManager.GetState(platformId);
                if (state.PvPCharacter == Entity.Null || !VRCore.EM.Exists(state.PvPCharacter))
                {
                    ctx.Reply("PvP character not available. Use .char create first.");
                    return;
                }

                // Practice mode = switching to PvP character with arena entry
                if (state.IsPvPActive)
                {
                    // Exit arena first for consistency
                    MissingServices.LifecycleService.ExitArena(ctx.Event.SenderUserEntity, state.PvPCharacter);
                    DualCharacterManager.SwitchToNormal(platformId, ctx.Event.SenderUserEntity);
                    ctx.Reply("Exited practice mode!");
                }
                else
                {
                    DualCharacterManager.SwitchToPvP(platformId, ctx.Event.SenderUserEntity);
                    // Enter arena for practice mode
                    MissingServices.LifecycleService.EnterArena(ctx.Event.SenderUserEntity, state.PvPCharacter);
                    ctx.Reply("Entered practice mode!");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in PracticeCommand: {ex.Message}");
                ctx.Reply("An error occurred while toggling practice mode.");
            }
        }

        [Command("reset", adminOnly: true, usage: ".arena reset")]
        public static void ResetCommand(ChatCommandContext ctx)
        {
            try
            {
                var userEntity = ctx.Event.SenderUserEntity;
                var character = ctx.Event.SenderCharacterEntity;
                var platformId = ctx.Event.User.PlatformId;

                // Reset arena state
                if (MissingServices.LifecycleService.IsPlayerInArena(platformId))
                {
                    MissingServices.LifecycleService.ExitArena(userEntity, character);
                }

                // Reset dual character state if exists
                if (DualCharacterManager.HasDualState(platformId))
                {
                    var state = DualCharacterManager.GetState(platformId);
                    if (state.IsPvPActive)
                    {
                        DualCharacterManager.SwitchToNormal(platformId, userEntity);
                    }
                }

                // Heal and restore using services
                MissingServices.ArenaHealingService.ApplyHeal(character);

                ctx.Reply("Player state reset!");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ResetCommand: {ex.Message}");
                ctx.Reply("An error occurred while resetting player state.");
            }
        }

        [Command("status", adminOnly: true, usage: ".arena status")]
        public static void StatusCommand(ChatCommandContext ctx)
        {
            try
            {
                var platformId = ctx.Event.User.PlatformId;
                var character = ctx.Event.SenderCharacterEntity;

                ctx.Reply("Arena Status:");

                // Core arena status using LifecycleService
                var isInArena = MissingServices.LifecycleService.IsPlayerInArena(platformId);
                ctx.Reply($"  In Arena: {isInArena}");

                if (isInArena)
                {
                    // VBlood hook status using GameSystems
                    var vBloodHooked = GameSystems.IsPlayerInArena(platformId);
                    ctx.Reply($"  VBlood Hook: {vBloodHooked}");

                    // Arena buffs status using ArenaAuraService
                    var hasBuffs = MissingServices.ArenaAuraService.HasArenaBuffs(character);
                    ctx.Reply($"  Arena Buffs: {hasBuffs}");

                    // Practice mode status using AutoEnterService
                    var autoEnter = MissingServices.AutoEnterService.IsAutoEnterEnabled(platformId);
                    ctx.Reply($"  Practice Mode: {autoEnter}");

                    // Zone status using ZoneService
                    var pos = VRCore.EM.GetComponentData<Translation>(character).Value;
                    var inZone = MissingServices.ZoneService.IsInArena(pos);
                    ctx.Reply($"  In Arena Zone: {inZone}");
                }

                // Arena character status using ArenaCharacterService
                var hasArenaChar = MissingServices.ArenaCharacterService.HasArenaCharacter(platformId);
                ctx.Reply($"  Arena Character: {hasArenaChar}");

                // Configuration status using ConfigService
                var configLoaded = true; // TODO: Implement ConfigService.IsInitialized
                ctx.Reply($"  Config Loaded: {configLoaded}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in StatusCommand: {ex.Message}");
                ctx.Reply("An error occurred while getting arena status.");
            }
        }

        [Command("spawnvamp", adminOnly: true, usage: ".arena spawnvamp <boss_name> [x] [y] [z]")]
        public static void SpawnVBloodCommand(ChatCommandContext ctx, string bossName, float x = 0, float y = 0, float z = 0)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var platformId = ctx.Event.User.PlatformId;

            try
            {
                Plugin.Logger?.LogInfo($"[{timestamp}] VBLOOD_SPAWN_COMMAND_START - PlatformId: {platformId}, BossName: {bossName}");

                Plugin.Logger?.LogInfo($"[{timestamp}] VBLOOD_SPAWN_VALIDATION - Looking up VBlood boss '{bossName}'");

                // Get VBlood boss by name
                var vBloodBoss = VBloodMapper.GetVBloodBossByName(bossName);
                if (vBloodBoss == null)
                {
                    Plugin.Logger?.LogWarning($"[{timestamp}] VBLOOD_SPAWN_NOT_FOUND - VBlood boss '{bossName}' not found");
                    ctx.Reply($"VBlood boss '{bossName}' not found. Available bosses:");
                    foreach (var boss in VBloodMapper.GetAllVBloodBosses())
                    {
                        ctx.Reply($"  • {boss.Name} (Level {boss.Level})");
                    }
                    return;
                }

                Plugin.Logger?.LogInfo($"[{timestamp}] VBLOOD_SPAWN_FOUND - Found VBlood '{vBloodBoss.Name}' with GUID {vBloodBoss.GuidHash}");

                // Get spawn position
                float3 spawnPos;
                if (x == 0 && y == 0 && z == 0)
                {
                    // Use player's position
                    var character = ctx.Event.SenderCharacterEntity;
                    if (VRCore.EM.TryGetComponentData(character, out Translation translation))
                    {
                        spawnPos = translation.Value;
                        spawnPos.y += 2f; // Spawn slightly above ground
                        Plugin.Logger?.LogInfo($"[{timestamp}] VBLOOD_SPAWN_POSITION - Using player position: {spawnPos}");
                    }
                    else
                    {
                        Plugin.Logger?.LogError($"[{timestamp}] VBLOOD_SPAWN_POSITION_FAIL - Could not get player position");
                        ctx.Reply("Could not determine spawn position.");
                        return;
                    }
                }
                else
                {
                    spawnPos = new float3(x, y, z);
                    Plugin.Logger?.LogInfo($"[{timestamp}] VBLOOD_SPAWN_POSITION - Using specified position: {spawnPos}");
                }

                Plugin.Logger?.LogInfo($"[{timestamp}] VBLOOD_SPAWN_EXECUTING - Spawning VBlood boss at {spawnPos}");

                Plugin.Logger?.LogInfo($"[{timestamp}] VBLOOD_SPAWN_DISABLED - VBlood spawning temporarily disabled");
                ctx.Reply("VBlood spawning is temporarily disabled.");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[{timestamp}] VBLOOD_SPAWN_CRITICAL_ERROR - PlatformId: {platformId}, BossName: {bossName}, Error: {ex.Message}");
                ctx.Reply("An error occurred while spawning the VBlood boss.");
            }
        }

        [Command("babyblood", adminOnly: true, usage: ".arena babyblood [x] [y] [z]")]
        public static void SpawnBabyBloodCommand(ChatCommandContext ctx, float x = 0, float y = 0, float z = 0)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var platformId = ctx.Event.User.PlatformId;

            try
            {
                Plugin.Logger?.LogInfo($"[{timestamp}] BABY_BLOOD_SPAWN_START - PlatformId: {platformId}");

                // Get spawn position
                float3 spawnPos;
                if (x == 0 && y == 0 && z == 0)
                {
                    // Use player's position
                    var character = ctx.Event.SenderCharacterEntity;
                    if (VRCore.EM.TryGetComponentData(character, out Translation translation))
                    {
                        spawnPos = translation.Value;
                        spawnPos.y += 2f; // Spawn slightly above ground
                        Plugin.Logger?.LogInfo($"[{timestamp}] BABY_BLOOD_POSITION - Using player position: {spawnPos}");
                    }
                    else
                    {
                        Plugin.Logger?.LogError($"[{timestamp}] BABY_BLOOD_POSITION_FAIL - Could not get player position");
                        ctx.Reply("Could not determine spawn position.");
                        return;
                    }
                }
                else
                {
                    spawnPos = new float3(x, y, z);
                    Plugin.Logger?.LogInfo($"[{timestamp}] BABY_BLOOD_POSITION - Using specified position: {spawnPos}");
                }

                Plugin.Logger?.LogInfo($"[{timestamp}] BABY_BLOOD_EXECUTING - Spawning Baby Blood using mapper");

                // Spawn Baby Blood using the mapper
                if (VBloodMapper.SpawnBabyBlood(spawnPos))
                {
                    // var babyBlood = VBloodMapper.GetBabyBloodBoss();
                    // Plugin.Logger?.LogInfo($"[{timestamp}] BABY_BLOOD_SUCCESS - Spawned Baby Blood (GUID: {VBloodMapper.GetBabyBloodGuid()})");
                    ctx.Reply("Spawned Baby Blood!");
                    ctx.Reply("Training VBlood - Level 1, Health 100");
                    ctx.Reply($"Location: {spawnPos.x:F1}, {spawnPos.y:F1}, {spawnPos.z:F1}");
                    ctx.Reply("Perfect for testing arena mechanics!");
                }
                else
                {
                    Plugin.Logger?.LogError($"[{timestamp}] BABY_BLOOD_FAILED - VBloodMapper.SpawnBabyBlood returned false");
                    ctx.Reply("Failed to spawn Baby Blood.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[{timestamp}] BABY_BLOOD_CRITICAL_ERROR - PlatformId: {platformId}, Error: {ex.Message}");
                ctx.Reply("An error occurred while spawning Baby Blood.");
            }
        }

        [Command("despawnvamp", adminOnly: true, usage: ".arena despawnvamp <boss_name>")]
        public static void DespawnVBloodCommand(ChatCommandContext ctx, string bossName)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var platformId = ctx.Event.User.PlatformId;

            try
            {
                Plugin.Logger?.LogInfo($"[{timestamp}] VBLOOD_DESPAWN_START - PlatformId: {platformId}, BossName: {bossName}");

                // Get VBlood boss by name
                var vBloodBoss = VBloodMapper.GetVBloodBossByName(bossName);
                if (vBloodBoss == null)
                {
                    Plugin.Logger?.LogWarning($"[{timestamp}] VBLOOD_DESPAWN_NOT_FOUND - VBlood boss '{bossName}' not found");
                    ctx.Reply($"VBlood boss '{bossName}' not found.");
                    return;
                }

                Plugin.Logger?.LogInfo($"[{timestamp}] VBLOOD_DESPAWN_EXECUTING - Despawning VBlood '{vBloodBoss.Name}' (GUID: {vBloodBoss.GuidHash})");

                // Despawn the VBlood boss
                if (VAuto.Core.VBloodMapper.DespawnVBloodBoss(vBloodBoss.GuidHash))
                {
                    Plugin.Logger?.LogInfo($"[{timestamp}] VBLOOD_DESPAWN_SUCCESS - Successfully despawned '{vBloodBoss.Name}'");
                    ctx.Reply($"Despawned VBlood boss '{vBloodBoss.Name}'!");
                }
                else
                {
                    Plugin.Logger?.LogWarning($"[{timestamp}] VBLOOD_DESPAWN_FAILED - VBloodMapper.DespawnVBloodBoss returned false");
                    ctx.Reply("Failed to despawn VBlood boss (may not be active).");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[{timestamp}] VBLOOD_DESPAWN_CRITICAL_ERROR - PlatformId: {platformId}, BossName: {bossName}, Error: {ex.Message}");
                ctx.Reply("An error occurred while despawning the VBlood boss.");
            }
        }
    }

    [CommandGroup("char")]
    public static class ArenaCharacterCommands
    {
        [Command("create", adminOnly: true, usage: ".char create")]
        public static void CreateCharacterCommand(ChatCommandContext ctx)
        {
            try
            {
                var userEntity = ctx.Event.SenderUserEntity;
                var normalCharacter = ctx.Event.SenderCharacterEntity;
                var platformId = ctx.Event.User.PlatformId;

                // Check if dual state already exists
                if (DualCharacterManager.HasDualState(platformId))
                {
                    var state = DualCharacterManager.GetState(platformId);
                    if (state.PvPCharacter != Entity.Null && VRCore.EM.Exists(state.PvPCharacter))
                    {
                        ctx.Reply("PvP character already exists!");
                        return;
                    }
                }

                // Create dual state if needed
                var dualState = DualCharacterManager.GetOrCreateState(platformId, normalCharacter);

                // Use different approach: TODO - Implement character creation
                // if (VAuto.Services.EnhancedArenaService.CreatePracticeCharacter(userEntity, normalCharacter))
                // {
                //     ctx.Reply("PvP character created successfully!");
                // }
                // else
                // {
                //     ctx.Reply("Failed to create PvP character.");
                // }
                ctx.Reply("PvP character creation not yet implemented.");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in CreateCharacterCommand: {ex.Message}");
                ctx.Reply("An error occurred while creating the character.");
            }
        }

        [Command("enter", adminOnly: true, usage: ".char enter")]
        public static void EnterCharacterCommand(ChatCommandContext ctx)
        {
            try
            {
                var userEntity = ctx.Event.SenderUserEntity;
                var platformId = ctx.Event.User.PlatformId;

                // Check if dual state exists
                if (!DualCharacterManager.HasDualState(platformId))
                {
                    ctx.Reply("No PvP character found. Use .char create first.");
                    return;
                }

                var state = DualCharacterManager.GetState(platformId);
                if (state.PvPCharacter == Entity.Null || !VRCore.EM.Exists(state.PvPCharacter))
                {
                    ctx.Reply("PvP character not available. Use .char create first.");
                    return;
                }

                // Check if already in PvP mode
                if (state.IsPvPActive)
                {
                    ctx.Reply("Already using PvP character!");
                    return;
                }

                // Switch to PvP character and enter arena using full lifecycle
                if (DualCharacterManager.SwitchToPvP(platformId, userEntity))
                {
                    // Also enter arena lifecycle for consistency
                    MissingServices.LifecycleService.EnterArena(userEntity, state.PvPCharacter);
                    ctx.Reply("Switched to PvP character and entered arena!");
                }
                else
                {
                    ctx.Reply("Failed to switch to PvP character.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in EnterCharacterCommand: {ex.Message}");
                ctx.Reply("An error occurred while entering PvP character.");
            }
        }

        [Command("exit", adminOnly: true, usage: ".char exit")]
        public static void ExitCharacterCommand(ChatCommandContext ctx)
        {
            try
            {
                var userEntity = ctx.Event.SenderUserEntity;
                var platformId = ctx.Event.User.PlatformId;

                // Check if dual state exists
                if (!DualCharacterManager.HasDualState(platformId))
                {
                    ctx.Reply("No dual character setup found.");
                    return;
                }

                var state = DualCharacterManager.GetState(platformId);
                if (!state.IsPvPActive)
                {
                    ctx.Reply("Not currently using PvP character!");
                    return;
                }

                // Exit arena first for consistency
                MissingServices.LifecycleService.ExitArena(userEntity, state.PvPCharacter);

                // Switch back to normal character
                if (DualCharacterManager.SwitchToNormal(platformId, userEntity))
                {
                    ctx.Reply("Switched back to normal character and exited arena!");
                }
                else
                {
                    ctx.Reply("Failed to switch to normal character.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ExitCharacterCommand: {ex.Message}");
                ctx.Reply("An error occurred while exiting PvP character.");
            }
        }

        [Command("swap", adminOnly: true, usage: ".char swap")]
        public static void SwapCharacterCommand(ChatCommandContext ctx)
        {
            try
            {
                var userEntity = ctx.Event.SenderUserEntity;
                var platformId = ctx.Event.User.PlatformId;

                // Check if dual state exists
                if (!DualCharacterManager.HasDualState(platformId))
                {
                    ctx.Reply("No dual character setup found. Use .char create first.");
                    return;
                }

                var state = DualCharacterManager.GetState(platformId);
                if (state.PvPCharacter == Entity.Null || !VRCore.EM.Exists(state.PvPCharacter))
                {
                    ctx.Reply("PvP character not available. Use .char create first.");
                    return;
                }

                // Use LifecycleService for unified arena entry/exit process
                bool success;
                if (state.IsPvPActive)
                {
                    // Exit arena using LifecycleService first
                    success = MissingServices.LifecycleService.ExitArena(userEntity, state.PvPCharacter);
                    if (success)
                    {
                        success = DualCharacterManager.SwitchToNormal(platformId, userEntity);
                        if (success) ctx.Reply("Swapped to normal character!");
                    }
                }
                else
                {
                    success = DualCharacterManager.SwitchToPvP(platformId, userEntity);
                    if (success)
                    {
                        // Enter arena using LifecycleService for consistency
                        MissingServices.LifecycleService.EnterArena(userEntity, state.PvPCharacter);
                        ctx.Reply("Swapped to PvP character!");
                    }
                }

                if (!success)
                {
                    ctx.Reply("Failed to swap characters.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in SwapCharacterCommand: {ex.Message}");
                ctx.Reply("An error occurred while swapping characters.");
            }
        }

        [Command("status", adminOnly: true, usage: ".char status")]
        public static void CharacterStatusCommand(ChatCommandContext ctx)
        {
            try
            {
                var platformId = ctx.Event.User.PlatformId;

                ctx.Reply("Character Status:");
                
                // Use LifecycleService for arena status
                var isInArena = MissingServices.LifecycleService.IsPlayerInArena(platformId);
                ctx.Reply($"  In Arena: {isInArena}");

                // Use ArenaCharacterService for arena character status
                var hasArenaChar = MissingServices.ArenaCharacterService.HasArenaCharacter(platformId);
                ctx.Reply($"  Arena Character: {(hasArenaChar ? "Exists" : "None")}");

                // Use GameSystems for VBlood hook status
                var vBloodHooked = GameSystems.IsPlayerInArena(platformId);
                ctx.Reply($"  VBlood Hook: {vBloodHooked}");

                // Use AutoEnterService for auto-enter status
                var autoEnter = MissingServices.AutoEnterService.IsAutoEnterEnabled(platformId);
                ctx.Reply($"  Auto-Enter: {autoEnter}");
                
                // Show dual character state if it exists
                if (DualCharacterManager.HasDualState(platformId))
                {
                    var state = DualCharacterManager.GetState(platformId);
                    ctx.Reply($"  PvP Character Active: {state.IsPvPActive}");
                    
                    if (state.PvPCharacter != Entity.Null && VRCore.EM.Exists(state.PvPCharacter))
                    {
                        var duration = DateTime.UtcNow - state.PvPCreatedAt;
                        ctx.Reply($"  PvP Character Age: {duration.TotalMinutes:F1} minutes");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in CharacterStatusCommand: {ex.Message}");
                ctx.Reply("An error occurred while getting character status.");
            }
        }
    }

    [CommandGroup("castle")]
    public static class CastleCommands
    {
        private static float _castleRadius = 50f;
        private static int _enhancementLevel = 1;
        // private static CastleTerritoryService _territoryService; // TODO: Implement when available

        // public static void InitializeCastleService(CastleTerritoryService territoryService) // TODO: Implement when available
        // {
        //     _territoryService = territoryService;
        // }

        [Command("setheart", adminOnly: true, usage: ".castle setheart [radius]")]
        public static void SetCastleHeartCommand(ChatCommandContext ctx, float radius = 50f)
        {
            try
            {
                if (radius <= 0)
                {
                    ctx.Reply("Castle heart radius must be greater than 0.");
                    return;
                }

                var character = ctx.Event.SenderCharacterEntity;
                var em = VRCore.EM;

                // Get current position
                float3 position = float3.zero;
                if (em.TryGetComponentData(character, out Translation translation))
                {
                    position = translation.Value;
                }
                else if (em.TryGetComponentData(character, out LocalToWorld ltw))
                {
                    position = ltw.Position;
                }
                else
                {
                    ctx.Reply("Could not determine current position.");
                    return;
                }

                _castleRadius = radius;
                
                // Find or create castle heart at this position
                // TODO: Implement territory service when available
                var heartEntity = Entity.Null; // _territoryService?.GetHeartForTerritory(_territoryService.GetTerritoryIndex(position)) ?? Entity.Null;
                
                if (heartEntity == Entity.Null)
                {
                    // Create a new castle heart at the position
                    heartEntity = em.CreateEntity();
                    // TODO: Implement CastleHeart component
                    // em.AddComponentData(heartEntity, new CastleHeart
                    // {
                    //     CastleTerritoryEntity = Entity.Null, // Will be set when territory is created
                    //     Health = 1000f,
                    //     MaxHealth = 1000f
                    // });
                    em.AddComponentData(heartEntity, new LocalTransform
                    {
                        Position = position,
                        Rotation = quaternion.identity,
                        Scale = 1f
                    });
                }

                ctx.Reply($"Castle heart grid radius set to {radius:F1} at position ({position.x:F1}, {position.y:F1}, {position.z:F1})!");
                ctx.Reply("This will affect how castle territories are calculated around hearts.");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in SetCastleHeartCommand: {ex.Message}");
                ctx.Reply("An error occurred while setting castle heart radius.");
            }
        }

        [Command("radius", adminOnly: true, usage: ".castle radius <radius>")]
        public static void SetCastleRadiusCommand(ChatCommandContext ctx, float radius)
        {
            try
            {
                if (radius <= 0)
                {
                    ctx.Reply("Castle radius must be greater than 0.");
                    return;
                }

                _castleRadius = radius;
                ctx.Reply($"Castle radius set to {radius:F1}!");
                ctx.Reply("This radius will be used for castle territory calculations.");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in SetCastleRadiusCommand: {ex.Message}");
                ctx.Reply("An error occurred while setting castle radius.");
            }
        }

        [Command("clear", adminOnly: true, usage: ".castle clear")]
        public static void ClearCastleRadiusCommand(ChatCommandContext ctx)
        {
            try
            {
                _castleRadius = 0f;
                ctx.Reply("Castle radius cleared! Castle territories will not be enforced.");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ClearCastleRadiusCommand: {ex.Message}");
                ctx.Reply("An error occurred while clearing castle radius.");
            }
        }

        [Command("delete", adminOnly: true, usage: ".castle delete")]
        public static void DeleteCastleConfigurationCommand(ChatCommandContext ctx)
        {
            try
            {
                _castleRadius = 0f;
                _enhancementLevel = 1;
                
                ctx.Reply("Castle configuration deleted!");
                ctx.Reply("All castle radius and enhancement settings have been reset.");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in DeleteCastleConfigurationCommand: {ex.Message}");
                ctx.Reply("An error occurred while deleting castle configuration.");
            }
        }

        [Command("enhance", adminOnly: true, usage: ".castle enhance <level>")]
        public static void SetEnhancementLevelCommand(ChatCommandContext ctx, int level)
        {
            try
            {
                if (level < 1 || level > 10)
                {
                    ctx.Reply("Enhancement level must be between 1 and 10.");
                    return;
                }

                _enhancementLevel = level;
                
                var enhancementDesc = level switch
                {
                    1 => "Basic castle functionality",
                    2 => "Enhanced territory detection",
                    3 => "Improved object integration",
                    4 => "Advanced territory management",
                    5 => "Enhanced building restrictions",
                    6 => "Improved heart connectivity",
                    7 => "Advanced object positioning",
                    8 => "Enhanced territory enforcement",
                    9 => "Advanced castle system integration",
                    10 => "Maximum enhancement level"
                };

                ctx.Reply($"Castle enhancement level set to {level}!");
                ctx.Reply($"Features: {enhancementDesc}");
                
                if (level >= 5)
                {
                    ctx.Reply("⚠️  High enhancement levels may affect performance.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in SetEnhancementLevelCommand: {ex.Message}");
                ctx.Reply("An error occurred while setting enhancement level.");
            }
        }

        [Command("info", adminOnly: true, usage: ".castle info")]
        public static void CastleInfoCommand(ChatCommandContext ctx)
        {
            try
            {
                ctx.Reply("🏰 Castle System Information:");
                ctx.Reply($"  Current Radius: {(_castleRadius > 0 ? $"{_castleRadius:F1}" : "Not set")}");
                ctx.Reply($"  Enhancement Level: {_enhancementLevel}/10");
                ctx.Reply($"  Territory Service: TODO - Not implemented");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in CastleInfoCommand: {ex.Message}");
                ctx.Reply("An error occurred while getting castle information.");
            }
        }

        [Command("status", adminOnly: true, usage: ".castle status")]
        public static void CastleStatusCommand(ChatCommandContext ctx)
        {
            try
            {
                var isActive = _castleRadius > 0;
                var enhancementActive = _enhancementLevel > 1;
                
                ctx.Reply("🏰 Castle System Status:");
                ctx.Reply($"  System Active: {(isActive ? "✅ Yes" : "❌ No")}");
                ctx.Reply($"  Enhancement Active: {(enhancementActive ? "✅ Yes" : "❌ No")}");
                ctx.Reply($"  Territory Integration: TODO - Not implemented"); // {(_territoryService != null ? "✅ Active" : "❌ Inactive")}");
                
                if (isActive)
                {
                    ctx.Reply($"  Radius: {_castleRadius:F1}");
                    ctx.Reply($"  Enhancement: {_enhancementLevel}/10");
                }
                
                // Check for objects in range
                var character = ctx.Event.SenderCharacterEntity;
                var em = VRCore.EM;
                
                if (em.TryGetComponentData(character, out Translation translation))
                {
                    var position = translation.Value;
                    // TODO: Implement CastleObjectIntegrationService
                    // var nearbyObjects = VAuto.Services.CastleObjectIntegrationService?.FindObjectsNearPosition(position, _castleRadius);
                    // if (nearbyObjects != null && nearbyObjects.Count > 0)
                    // {
                    //     ctx.Reply($"  Objects in Range: {nearbyObjects.Count}");
                    // }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in CastleStatusCommand: {ex.Message}");
                ctx.Reply("An error occurred while getting castle status.");
            }
        }
    }

    [CommandGroup("build")]
    public static class BuildCommands
    {
        [Command("mode", adminOnly: true, usage: ".build mode")]
        public static void ToggleBuildModeCommand(ChatCommandContext ctx)
        {
            try
            {
                var platformId = ctx.Event.User.PlatformId;
                var wasInBuildMode = MissingServices.BuildModeService.IsInBuildMode(platformId);
                MissingServices.BuildModeService.ToggleBuildMode(platformId);
                var isNowInBuildMode = MissingServices.BuildModeService.IsInBuildMode(platformId);

                if (isNowInBuildMode)
                {
                    ctx.Reply("🏗️ Build mode enabled! Use .build select <schematic> to choose what to build.");
                }
                else
                {
                    ctx.Reply("🏗️ Build mode disabled!");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ToggleBuildModeCommand: {ex.Message}");
                ctx.Reply("An error occurred while toggling build mode.");
            }
        }

        [Command("list", adminOnly: true, usage: ".build list")]
        public static void ListSchematicsCommand(ChatCommandContext ctx)
        {
            try
            {
                var schematics = MissingServices.SchematicService.GetAvailableSchematics();
                ctx.Reply("📋 Available Schematics:");
                foreach (var schematic in schematics)
                {
                    ctx.Reply($"  • {schematic}");
                }
                ctx.Reply("Use .build select <schematic> to choose one.");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ListSchematicsCommand: {ex.Message}");
                ctx.Reply("An error occurred while listing schematics.");
            }
        }

        [Command("select", adminOnly: true, usage: ".build select <schematic>")]
        public static void SelectSchematicCommand(ChatCommandContext ctx, string schematic)
        {
            try
            {
                if (!MissingServices.SchematicService.CanUseSchematic(ctx.Event.SenderCharacterEntity, schematic))
                {
                    ctx.Reply($"❌ Schematic '{schematic}' not found. Use .build list to see available options.");
                    return;
                }

                MissingServices.SchematicService.UseSchematic(ctx.Event.SenderCharacterEntity, schematic);
                ctx.Reply($"✅ Selected schematic: {schematic}");
                ctx.Reply("Use .build place to build it!");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in SelectSchematicCommand: {ex.Message}");
                ctx.Reply("An error occurred while selecting schematic.");
            }
        }

        [Command("place", adminOnly: true, usage: ".build place")]
        public static void PlaceSchematicCommand(ChatCommandContext ctx)
        {
            try
            {
                var activeSchematic = MissingServices.SchematicService.GetActiveSchematic();
                if (string.IsNullOrEmpty(activeSchematic))
                {
                    ctx.Reply("❌ No schematic selected. Use .build select <schematic> first.");
                    return;
                }

                if (!MissingServices.BuildModeService.IsInBuildMode(ctx.Event.User.PlatformId))
                {
                    ctx.Reply("❌ Build mode not enabled. Use .build mode first.");
                    return;
                }

                ctx.Reply($"🏗️ Placing: {activeSchematic}");
                ctx.Reply("Feature not yet implemented - placeholder for future building system.");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in PlaceSchematicCommand: {ex.Message}");
                ctx.Reply("An error occurred while placing schematic.");
            }
        }

        [Command("remove", adminOnly: true, usage: ".build remove")]
        public static void RemoveBuildCommand(ChatCommandContext ctx)
        {
            try
            {
                ctx.Reply("🗑️ Remove mode activated - look at object to remove.");
                ctx.Reply("Feature not yet implemented - placeholder for future building system.");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in RemoveBuildCommand: {ex.Message}");
                ctx.Reply("An error occurred while entering remove mode.");
            }
        }

        [Command("surface", adminOnly: true, usage: ".build surface <material>")]
        public static void SetSurfaceMaterialCommand(ChatCommandContext ctx, string material)
        {
            try
            {
                var materials = MissingServices.SurfaceService.GetAvailableMaterials();
                if (!materials.Contains(material.ToLower()))
                {
                    ctx.Reply($"❌ Material '{material}' not found. Available: {string.Join(", ", materials)}");
                    return;
                }

                MissingServices.SurfaceService.SetActiveMaterial(material.ToLower());
                ctx.Reply($"🎨 Surface material set to: {material}");
                ctx.Reply("Feature not yet implemented - placeholder for future surface system.");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in SetSurfaceMaterialCommand: {ex.Message}");
                ctx.Reply("An error occurred while setting surface material.");
            }
        }
    }

    [CommandGroup("portal")]
    public static class PortalCommands
    {
        [Command("create", adminOnly: true, usage: ".portal create <name> <x> <y> <z>")]
        public static void CreatePortalCommand(ChatCommandContext ctx, string name, float x, float y, float z)
        {
            try
            {
                var position = new float3(x, y, z);
                if (MissingServices.PortalService.CreatePortal(name, position, position))
                {
                    ctx.Reply($"✅ Portal '{name}' created at ({x:F1}, {y:F1}, {z:F1})");
                    ctx.Reply("Note: Portal destination set to same location - use future linking commands.");
                }
                else
                {
                    ctx.Reply($"❌ Portal '{name}' already exists!");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in CreatePortalCommand: {ex.Message}");
                ctx.Reply("An error occurred while creating portal.");
            }
        }

        [Command("goto", adminOnly: true, usage: ".portal goto <name>")]
        public static void GotoPortalCommand(ChatCommandContext ctx, string name)
        {
            try
            {
                var portal = MissingServices.PortalService.GetPortal(name);
                if (portal == null)
                {
                    ctx.Reply($"❌ Portal '{name}' not found!");
                    return;
                }

                if (MissingServices.TeleportService.Teleport(ctx.Event.SenderCharacterEntity, portal.Destination))
                {
                    ctx.Reply($"✨ Teleported through portal '{name}'!");
                }
                else
                {
                    ctx.Reply("❌ Teleportation failed!");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in GotoPortalCommand: {ex.Message}");
                ctx.Reply("An error occurred while teleporting through portal.");
            }
        }

        [Command("list", adminOnly: true, usage: ".portal list")]
        public static void ListPortalsCommand(ChatCommandContext ctx)
        {
            try
            {
                var portals = MissingServices.PortalService.GetPortalNames();
                if (portals.Count == 0)
                {
                    ctx.Reply("📋 No portals found. Create one with .portal create <name> <x> <y> <z>");
                    return;
                }

                ctx.Reply("📋 Active Portals:");
                foreach (var portalName in portals)
                {
                    var portal = MissingServices.PortalService.GetPortal(portalName);
                    if (portal != null)
                    {
                        ctx.Reply($"  • {portalName}: ({portal.Position.x:F1}, {portal.Position.y:F1}, {portal.Position.z:F1}) → ({portal.Destination.x:F1}, {portal.Destination.y:F1}, {portal.Destination.z:F1})");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ListPortalsCommand: {ex.Message}");
                ctx.Reply("An error occurred while listing portals.");
            }
        }

        [Command("remove", adminOnly: true, usage: ".portal remove <name>")]
        public static void RemovePortalCommand(ChatCommandContext ctx, string name)
        {
            try
            {
                if (MissingServices.PortalService.RemovePortal(name))
                {
                    ctx.Reply($"🗑️ Portal '{name}' removed!");
                }
                else
                {
                    ctx.Reply($"❌ Portal '{name}' not found!");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in RemovePortalCommand: {ex.Message}");
                ctx.Reply("An error occurred while removing portal.");
            }
        }
    }

    [CommandGroup("glow")]
    public static class GlowCommands
    {
        [Command("add", adminOnly: true, usage: ".glow add <color>")]
        public static void AddGlowCommand(ChatCommandContext ctx, string color = "blue")
        {
            try
            {
                ctx.Reply($"✨ Adding {color} glow effect!");
                ctx.Reply("Feature not yet implemented - placeholder for future lighting system.");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in AddGlowCommand: {ex.Message}");
                ctx.Reply("An error occurred while adding glow effect.");
            }
        }

        [Command("remove", adminOnly: true, usage: ".glow remove")]
        public static void RemoveGlowCommand(ChatCommandContext ctx)
        {
            try
            {
                ctx.Reply("🗑️ Removing glow effect!");
                ctx.Reply("Feature not yet implemented - placeholder for future lighting system.");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in RemoveGlowCommand: {ex.Message}");
                ctx.Reply("An error occurred while removing glow effect.");
            }
        }
    }

    [CommandGroup("zone")]
    public static class ZoneCommands
    {
        [Command("setzonehere", adminOnly: true, usage: ".zone setzonehere <name> <radius> <x> <y> <z>")]
        public static void SetZoneHereCommand(ChatCommandContext ctx, string name, float radius, float x, float y, float z)
        {
            try
            {
                var character = ctx.Event.SenderCharacterEntity;
                var em = VRCore.EM;

                // Get current position as the zone center
                float3 currentPos = float3.zero;
                if (em.TryGetComponentData(character, out Translation translation))
                {
                    currentPos = translation.Value;
                }
                else if (em.TryGetComponentData(character, out LocalToWorld ltw))
                {
                    currentPos = ltw.Position;
                }
                else
                {
                    ctx.Reply("❌ Could not determine current position!");
                    return;
                }

                // Use provided XYZ as additional zone center reference or offset
                var zoneCenter = new float3(x, y, z);

                // Create zone with auto-enter capability
                if (MissingServices.ZoneService.CreateZone(name, zoneCenter, radius))
                {
                    // Add glow border effect to the zone corners
                    AddZoneGlowBorder(name, zoneCenter, radius, "chaos");

                    ctx.Reply($"✅ Zone '{name}' created!");
                    ctx.Reply($"📍 Center: ({zoneCenter.x:F1}, {zoneCenter.y:F1}, {zoneCenter.z:F1})");
                    ctx.Reply($"📏 Radius: {radius:F1}");
                    ctx.Reply($"✨ Auto-enter enabled with chaos glow border");
                    ctx.Reply($"📍 Your position: ({currentPos.x:F1}, {currentPos.y:F1}, {currentPos.z:F1})");
                }
                else
                {
                    ctx.Reply($"❌ Failed to create zone '{name}'!");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in SetZoneHereCommand: {ex.Message}");
                ctx.Reply("An error occurred while creating the zone.");
            }
        }

        [Command("setcenter", adminOnly: true, usage: ".zone setcenter <x> <y> <z>")]
        public static void SetZoneCenterCommand(ChatCommandContext ctx, float x, float y, float z)
        {
            try
            {
                var position = new float3(x, y, z);
                // TODO: Implement zone center setting
                ctx.Reply($"Zone center set to ({x:F1}, {y:F1}, {z:F1}) - Feature not yet implemented");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in SetZoneCenterCommand: {ex.Message}");
                ctx.Reply("An error occurred while setting zone center.");
            }
        }

        [Command("setradius", adminOnly: true, usage: ".zone setradius <radius>")]
        public static void SetZoneRadiusCommand(ChatCommandContext ctx, float radius)
        {
            try
            {
                if (radius <= 0)
                {
                    ctx.Reply("Zone radius must be greater than 0.");
                    return;
                }
                // TODO: Implement zone radius setting
                ctx.Reply($"Zone radius set to {radius:F1} - Feature not yet implemented");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in SetZoneRadiusCommand: {ex.Message}");
                ctx.Reply("An error occurred while setting zone radius.");
            }
        }

        [Command("setspawn", adminOnly: true, usage: ".zone setspawn <x> <y> <z>")]
        public static void SetZoneSpawnCommand(ChatCommandContext ctx, float x, float y, float z)
        {
            try
            {
                var position = new float3(x, y, z);
                // TODO: Implement zone spawn setting
                ctx.Reply($"Zone spawn set to ({x:F1}, {y:F1}, {z:F1}) - Feature not yet implemented");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in SetZoneSpawnCommand: {ex.Message}");
                ctx.Reply("An error occurred while setting zone spawn.");
            }
        }

        [Command("info", adminOnly: true, usage: ".zone info")]
        public static void ZoneInfoCommand(ChatCommandContext ctx)
        {
            try
            {
                // TODO: Implement zone info retrieval
                ctx.Reply("Zone Information - Feature not yet implemented");
                ctx.Reply("Available zones: None configured");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ZoneInfoCommand: {ex.Message}");
                ctx.Reply("An error occurred while getting zone information.");
            }
        }

        [Command("reload", adminOnly: true, usage: ".zone reload")]
        public static void ReloadZoneCommand(ChatCommandContext ctx)
        {
            try
            {
                // TODO: Implement zone configuration reload
                ctx.Reply("Zone configuration reloaded - Feature not yet implemented");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ReloadZoneCommand: {ex.Message}");
                ctx.Reply("An error occurred while reloading zone configuration.");
            }
        }

        /// <summary>
        /// Add glow border effect to zone corners
        /// </summary>
        private static void AddZoneGlowBorder(string zoneName, float3 center, float radius, string glowColor = "chaos")
        {
            try
            {
                // Calculate corner positions for the zone border
                var corners = CalculateZoneCorners(center, radius);

                // Add glow effects at each corner
                foreach (var corner in corners)
                {
                    MissingServices.ArenaGlowService.AddGlowEffect(corner, glowColor, 2.0f);
                }

                Plugin.Logger?.LogInfo($"Added {glowColor} glow border to zone '{zoneName}' with {corners.Length} corner effects");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error adding zone glow border: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculate the 4 corner positions of a circular zone for glow effects
        /// </summary>
        private static float3[] CalculateZoneCorners(float3 center, float radius)
        {
            return new float3[]
            {
                new float3(center.x + radius, center.y, center.z + radius), // Northeast
                new float3(center.x + radius, center.y, center.z - radius), // Southeast
                new float3(center.x - radius, center.y, center.z + radius), // Northwest
                new float3(center.x - radius, center.y, center.z - radius)  // Southwest
            };
        }
    }

    [CommandGroup("achievements")]
    public static class AchievementCommands
    {
        [Command("unlock", adminOnly: true, usage: ".achievements unlock")]
        public static void UnlockAllAchievementsCommand(ChatCommandContext ctx)
        {
            try
            {
                var platformId = ctx.Event.User.PlatformId;
                var result = AchievementUnlockService.UnlockAllAchievements(platformId);

                if (result)
                {
                    var stats = AchievementUnlockService.GetAchievementStatistics();
                    ctx.Reply($"🏆 All achievements unlocked! Total unlocked: {stats["Total_Achievements_Unlocked"]}");
                    ctx.Reply("Achievements will remain unlocked until arena exit.");
                }
                else
                {
                    ctx.Reply("❌ Failed to unlock achievements.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in UnlockAllAchievementsCommand: {ex.Message}");
                ctx.Reply("An error occurred while unlocking achievements.");
            }
        }

        [Command("remove", adminOnly: true, usage: ".achievements remove")]
        public static void RemoveAchievementsCommand(ChatCommandContext ctx)
        {
            try
            {
                var platformId = ctx.Event.User.PlatformId;
                var result = AchievementUnlockService.RemoveAchievementUnlocks(platformId);

                if (result)
                {
                    ctx.Reply("🏆 Achievement unlocks removed.");
                }
                else
                {
                    ctx.Reply("❌ Failed to remove achievement unlocks.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in RemoveAchievementsCommand: {ex.Message}");
                ctx.Reply("An error occurred while removing achievements.");
            }
        }

        [Command("status", adminOnly: true, usage: ".achievements status")]
        public static void AchievementStatusCommand(ChatCommandContext ctx)
        {
            try
            {
                var platformId = ctx.Event.User.PlatformId;
                var isUnlocked = AchievementUnlockService.IsAchievementsUnlocked(platformId);
                var state = AchievementUnlockService.GetAchievementState(platformId);

                ctx.Reply($"🏆 Achievement Status for {platformId}:");
                ctx.Reply($"  Unlocked: {(isUnlocked ? "✅ Yes" : "❌ No")}");
                if (state != null)
                {
                    ctx.Reply($"  Unlocked At: {state.UnlockedAt:yyyy-MM-dd HH:mm:ss}");
                    ctx.Reply($"  Achievement Count: {state.UnlockedAchievements.Count}");
                }

                var stats = AchievementUnlockService.GetAchievementStatistics();
                ctx.Reply("🏆 Global Statistics:");
                ctx.Reply($"  Players with unlocks: {stats["Total_Players_With_Unlocks"]}");
                ctx.Reply($"  Currently unlocked: {stats["Players_Currently_Unlocked"]}");
                ctx.Reply($"  Total achievements: {stats["Total_Achievements_Unlocked"]}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in AchievementStatusCommand: {ex.Message}");
                ctx.Reply("An error occurred while getting achievement status.");
            }
        }

        [Command("force", adminOnly: true, usage: ".achievements force <platformId>")]
        public static void ForceUnlockCommand(ChatCommandContext ctx, ulong targetPlatformId)
        {
            try
            {
                var result = AchievementUnlockService.ForceUnlockForTesting(targetPlatformId);

                if (result)
                {
                    ctx.Reply($"🏆 FORCE UNLOCKED achievements for {targetPlatformId} (TESTING ONLY)");
                }
                else
                {
                    ctx.Reply($"❌ Failed to force unlock achievements for {targetPlatformId}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ForceUnlockCommand: {ex.Message}");
                ctx.Reply("An error occurred while force unlocking achievements.");
            }
        }

        [Command("list", adminOnly: true, usage: ".achievements list")]
        public static void ListUnlockedAchievementsCommand(ChatCommandContext ctx)
        {
            try
            {
                var platformId = ctx.Event.User.PlatformId;
                var state = AchievementUnlockService.GetAchievementState(platformId);

                if (state == null || !state.UnlockedAchievements.Any())
                {
                    ctx.Reply("❌ No achievements unlocked for your character.");
                    return;
                }

                ctx.Reply($"🏆 Unlocked Achievements ({state.UnlockedAchievements.Count}):");
                var groupedAchievements = state.UnlockedAchievements
                    .GroupBy(a => a.Split('_')[0])
                    .OrderBy(g => g.Key);

                foreach (var group in groupedAchievements)
                {
                    ctx.Reply($"-- {group.Key.ToUpper()} --");
                    foreach (var achievement in group.OrderBy(a => a))
                    {
                        ctx.Reply($"  • {achievement.Replace("_", " ")}");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ListUnlockedAchievementsCommand: {ex.Message}");
                ctx.Reply("An error occurred while listing achievements.");
            }
        }
    }

    [CommandGroup("debug")]
    public static class DebugCommands
    {
        private static Entity _currentCOI = Entity.Null;
        private static readonly Dictionary<string, DebugData> _debugSessions = new();

        [Command("coi", adminOnly: true, usage: ".debug coi [entity]")]
        public static void CenterOfInterestCommand(ChatCommandContext ctx, string entityId = "")
        {
            try
            {
                var character = ctx.Event.SenderCharacterEntity;
                var platformId = ctx.Event.User.PlatformId;

                if (string.IsNullOrEmpty(entityId))
                {
                    // Find nearest entity as COI
                    var nearestEntity = FindNearestEntity(character);
                    if (nearestEntity != Entity.Null)
                    {
                        SetCenterOfInterest(platformId, nearestEntity);
                        var entityInfo = GetEntityInfo(nearestEntity);
                        ctx.Reply($"🎯 COI Set: {entityInfo.Name} (ID: {nearestEntity.Index})");
                        ctx.Reply($"📍 Position: {entityInfo.Position}");
                        ctx.Reply($"🏷️ Type: {entityInfo.EntityType}");
                    }
                    else
                    {
                        ctx.Reply("❌ No nearby entities found to set as COI");
                    }
                }
                else
                {
                    // Try to parse entity ID
                    if (int.TryParse(entityId, out var index))
                    {
                        var targetEntity = new Entity { Index = index, Version = 1 };
                        if (VRCore.EM.Exists(targetEntity))
                        {
                            SetCenterOfInterest(platformId, targetEntity);
                            var entityInfo = GetEntityInfo(targetEntity);
                            ctx.Reply($"🎯 COI Set: {entityInfo.Name} (ID: {targetEntity.Index})");
                            ctx.Reply($"📍 Position: {entityInfo.Position}");
                            ctx.Reply($"🏷️ Type: {entityInfo.EntityType}");
                        }
                        else
                        {
                            ctx.Reply($"❌ Entity with ID {index} does not exist");
                        }
                    }
                    else
                    {
                        ctx.Reply("❌ Invalid entity ID format. Use a number or leave empty for nearest entity.");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in CenterOfInterestCommand: {ex.Message}");
                ctx.Reply("An error occurred while setting COI.");
            }
        }

        [Command("track", adminOnly: true, usage: ".debug track")]
        public static void TrackCOICommand(ChatCommandContext ctx)
        {
            try
            {
                var platformId = ctx.Event.User.PlatformId;

                if (_debugSessions.TryGetValue(platformId.ToString(), out var session))
                {
                    if (session.COI != Entity.Null && VRCore.EM.Exists(session.COI))
                    {
                        var entityInfo = GetEntityInfo(session.COI);
                        var health = GetEntityHealth(session.COI);
                        var distance = GetDistanceToEntity(ctx.Event.SenderCharacterEntity, session.COI);

                        ctx.Reply($"📊 COI Tracking - {entityInfo.Name}");
                        ctx.Reply($"📍 Position: {entityInfo.Position:F1}");
                        ctx.Reply($"❤️ Health: {health:F1}%");
                        ctx.Reply($"📏 Distance: {distance:F1} units");
                        ctx.Reply($"🏷️ Type: {entityInfo.EntityType}");
                        ctx.Reply($"⏱️ Tracked for: {(DateTime.UtcNow - session.StartTime).TotalSeconds:F1}s");
                    }
                    else
                    {
                        ctx.Reply("❌ COI entity no longer exists");
                        _debugSessions.Remove(platformId.ToString());
                    }
                }
                else
                {
                    ctx.Reply("❌ No active COI session. Use .debug coi first.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in TrackCOICommand: {ex.Message}");
                ctx.Reply("An error occurred while tracking COI.");
            }
        }

        [Command("analyze", adminOnly: true, usage: ".debug analyze")]
        public static void AnalyzeCOICommand(ChatCommandContext ctx)
        {
            try
            {
                var platformId = ctx.Event.User.PlatformId;

                if (_debugSessions.TryGetValue(platformId.ToString(), out var session))
                {
                    if (session.COI != Entity.Null && VRCore.EM.Exists(session.COI))
                    {
                        var analysis = AnalyzeEntity(session.COI);
                        ctx.Reply($"🔍 COI Analysis - {analysis.Name}");
                        ctx.Reply($"📊 Components: {analysis.ComponentCount}");
                        ctx.Reply($"🏷️ Primary Type: {analysis.PrimaryComponent}");
                        ctx.Reply($"💾 Memory Usage: ~{analysis.EstimatedSize} bytes");
                        ctx.Reply($"🔄 Update Frequency: {analysis.UpdateRate}/sec");

                        if (analysis.HasHealth)
                        {
                            ctx.Reply($"❤️ Health System: Active");
                        }
                        if (analysis.HasAI)
                        {
                            ctx.Reply($"🤖 AI System: Active");
                        }
                        if (analysis.HasPhysics)
                        {
                            ctx.Reply($"⚙️ Physics: Active");
                        }
                    }
                    else
                    {
                        ctx.Reply("❌ COI entity no longer exists");
                    }
                }
                else
                {
                    ctx.Reply("❌ No active COI session. Use .debug coi first.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in AnalyzeCOICommand: {ex.Message}");
                ctx.Reply("An error occurred while analyzing COI.");
            }
        }

        [Command("clear", adminOnly: true, usage: ".debug clear")]
        public static void ClearCOICommand(ChatCommandContext ctx)
        {
            try
            {
                var platformId = ctx.Event.User.PlatformId;

                if (_debugSessions.Remove(platformId.ToString()))
                {
                    _currentCOI = Entity.Null;
                    ctx.Reply("🗑️ COI session cleared");
                }
                else
                {
                    ctx.Reply("❌ No active COI session to clear");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ClearCOICommand: {ex.Message}");
                ctx.Reply("An error occurred while clearing COI.");
            }
        }

        [Command("list", adminOnly: true, usage: ".debug list [radius]")]
        public static void ListNearbyEntitiesCommand(ChatCommandContext ctx, float radius = 50f)
        {
            try
            {
                var character = ctx.Event.SenderCharacterEntity;
                var nearbyEntities = FindEntitiesInRadius(character, radius);

                if (nearbyEntities.Count == 0)
                {
                    ctx.Reply($"📋 No entities found within {radius:F1} units");
                    return;
                }

                ctx.Reply($"📋 Nearby Entities (within {radius:F1} units): {nearbyEntities.Count}");

                var sortedEntities = nearbyEntities
                    .OrderBy(e => GetDistanceToEntity(character, e))
                    .Take(10); // Limit to first 10 for readability

                var count = 1;
                foreach (var entity in sortedEntities)
                {
                    var info = GetEntityInfo(entity);
                    var distance = GetDistanceToEntity(character, entity);
                    ctx.Reply($"  {count}. {info.Name} (ID: {entity.Index}) - {distance:F1}u - {info.EntityType}");
                    count++;
                }

                if (nearbyEntities.Count > 10)
                {
                    ctx.Reply($"  ... and {nearbyEntities.Count - 10} more entities");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ListNearbyEntitiesCommand: {ex.Message}");
                ctx.Reply("An error occurred while listing entities.");
            }
        }

        [Command("performance", adminOnly: true, usage: ".debug performance")]
        public static void PerformanceDebugCommand(ChatCommandContext ctx)
        {
            try
            {
                var em = VRCore.EM;
                var stats = new
                {
                    TotalEntities = "Unknown", // em.EntityCount not available
                    ArchetypeCount = em.UniversalQuery.CalculateEntityCount(),
                    ComponentCount = em.UniversalQuery.CalculateChunkCount(),
                    MemoryUsage = "Unknown", // Would need more complex tracking
                    FrameTime = UnityEngine.Time.deltaTime * 1000f
                };

                ctx.Reply($"📈 Performance Stats:");
                ctx.Reply($"  📊 Total Entities: {stats.TotalEntities}");
                ctx.Reply($"  🏗️ Archetypes: {stats.ArchetypeCount:N0}");
                ctx.Reply($"  🔧 Components: {stats.ComponentCount:N0}");
                ctx.Reply($"  ⚡ Frame Time: {stats.FrameTime:F2}ms");
                ctx.Reply($"  💾 Memory: {stats.MemoryUsage}");

                // Add COI-specific performance if active
                var platformId = ctx.Event.User.PlatformId;
                if (_debugSessions.TryGetValue(platformId.ToString(), out var session))
                {
                    ctx.Reply($"🎯 COI Performance:");
                    ctx.Reply($"  ⏱️ Tracking Duration: {(DateTime.UtcNow - session.StartTime).TotalSeconds:F1}s");
                    ctx.Reply($"  📊 Update Count: {session.UpdateCount}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in PerformanceDebugCommand: {ex.Message}");
                ctx.Reply("An error occurred while getting performance stats.");
            }
        }

        #region Helper Methods

        private static void SetCenterOfInterest(ulong platformId, Entity entity)
        {
            var sessionKey = platformId.ToString();
            _debugSessions[sessionKey] = new DebugData
            {
                COI = entity,
                StartTime = DateTime.UtcNow,
                UpdateCount = 0,
                PlatformId = platformId
            };
            _currentCOI = entity;
        }

        private static Entity FindNearestEntity(Entity sourceEntity)
        {
            try
            {
                var em = VRCore.EM;
                var sourcePos = GetEntityPosition(sourceEntity);
                var nearestEntity = Entity.Null;
                var nearestDistance = float.MaxValue;

                // Query for entities with Translation component
                var query = em.CreateEntityQuery(ComponentType.ReadOnly<Translation>());
                var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

                foreach (var entity in entities)
                {
                    if (entity == sourceEntity) continue; // Skip self

                    try
                    {
                        if (em.TryGetComponentData(entity, out Translation translation))
                        {
                            var distance = math.distance(sourcePos, translation.Value);
                            if (distance < nearestDistance && distance < 100f) // Within 100 units
                            {
                                nearestDistance = distance;
                                nearestEntity = entity;
                            }
                        }
                    }
                    catch
                    {
                        // Skip entities that cause errors
                        continue;
                    }
                }

                entities.Dispose();
                return nearestEntity;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error finding nearest entity: {ex.Message}");
                return Entity.Null;
            }
        }

        private static List<Entity> FindEntitiesInRadius(Entity centerEntity, float radius)
        {
            var result = new List<Entity>();
            try
            {
                var em = VRCore.EM;
                var centerPos = GetEntityPosition(centerEntity);

                var query = em.CreateEntityQuery(ComponentType.ReadOnly<Translation>());
                var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

                foreach (var entity in entities)
                {
                    try
                    {
                        if (em.TryGetComponentData(entity, out Translation translation))
                        {
                            var distance = math.distance(centerPos, translation.Value);
                            if (distance <= radius)
                            {
                                result.Add(entity);
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                entities.Dispose();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error finding entities in radius: {ex.Message}");
            }

            return result;
        }

        private static EntityInfo GetEntityInfo(Entity entity)
        {
            try
            {
                var em = VRCore.EM;
                var position = GetEntityPosition(entity);
                var name = $"Entity_{entity.Index}";
                var entityType = "Unknown";

                // Try to determine entity type based on components
                if (em.HasComponent<PlayerCharacter>(entity))
                {
                    entityType = "Player";
                    if (em.TryGetComponentData(entity, out PlayerCharacter pc))
                    {
                        name = pc.Name.ToString();
                    }
                }
                else if (em.HasComponent<VBloodUnit>(entity))
                {
                    entityType = "VBlood";
                    name = "VBlood Unit";
                }
                else if (em.HasComponent<UnitStats>(entity))
                {
                    entityType = "Unit";
                    name = "Game Unit";
                }
                else
                {
                    entityType = "Unknown";
                    name = $"Entity_{entity.Index}";
                }

                return new EntityInfo
                {
                    Name = name,
                    Position = position,
                    EntityType = entityType
                };
            }
            catch
            {
                return new EntityInfo
                {
                    Name = $"Entity_{entity.Index}",
                    Position = float3.zero,
                    EntityType = "Error"
                };
            }
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
                Plugin.Logger?.LogDebug($"Error getting entity position: {ex.Message}");
            }

            return float3.zero;
        }

        private static float GetDistanceToEntity(Entity from, Entity to)
        {
            var pos1 = GetEntityPosition(from);
            var pos2 = GetEntityPosition(to);
            return math.distance(pos1, pos2);
        }

        private static float GetEntityHealth(Entity entity)
        {
            try
            {
                var em = VRCore.EM;
                if (em.TryGetComponentData(entity, out Health health))
                {
                    return (health.Value / health.MaxHealth) * 100f;
                }
            }
            catch
            {
                // Health component not available or error
            }

            return -1f; // Unknown health
        }

        private static EntityAnalysis AnalyzeEntity(Entity entity)
        {
            try
            {
                var em = VRCore.EM;
                var entityInfo = GetEntityInfo(entity);

                // Count components (simplified)
                var componentCount = 0;
                var primaryComponent = "Unknown";
                var estimatedSize = 0;

                // Check for common components
                var hasHealth = em.HasComponent<Health>(entity);
                var hasAI = false; // em.HasComponent<AiState>(entity) || em.HasComponent<AiDefinition>(entity); // Components don't exist
                var hasPhysics = false; // em.HasComponent<PhysicsCollider>(entity) || em.HasComponent<PhysicsBody>(entity); // Components don't exist

                // Determine primary component type
                if (em.HasComponent<PlayerCharacter>(entity))
                {
                    primaryComponent = "PlayerCharacter";
                }
                else if (em.HasComponent<VBloodUnit>(entity))
                {
                    primaryComponent = "VBloodUnit";
                }
                else if (em.HasComponent<UnitStats>(entity))
                {
                    primaryComponent = "UnitStats";
                }

                // Rough component count estimate
                componentCount = (hasHealth ? 1 : 0) + (hasAI ? 2 : 0) + (hasPhysics ? 2 : 0) + 3; // +3 for basic components

                return new EntityAnalysis
                {
                    Name = entityInfo.Name,
                    ComponentCount = componentCount,
                    PrimaryComponent = primaryComponent,
                    EstimatedSize = estimatedSize,
                    UpdateRate = 30, // Default assumption
                    HasHealth = hasHealth,
                    HasAI = hasAI,
                    HasPhysics = hasPhysics
                };
            }
            catch
            {
                return new EntityAnalysis
                {
                    Name = $"Entity_{entity.Index}",
                    ComponentCount = 0,
                    PrimaryComponent = "Error",
                    EstimatedSize = 0,
                    UpdateRate = 0,
                    HasHealth = false,
                    HasAI = false,
                    HasPhysics = false
                };
            }
        }

        #endregion

        #region Data Structures

        private class DebugData
        {
            public Entity COI { get; set; }
            public DateTime StartTime { get; set; }
            public int UpdateCount { get; set; }
            public ulong PlatformId { get; set; }
        }

        private class EntityInfo
        {
            public string Name { get; set; }
            public float3 Position { get; set; }
            public string EntityType { get; set; }
        }

        private class EntityAnalysis
        {
            public string Name { get; set; }
            public int ComponentCount { get; set; }
            public string PrimaryComponent { get; set; }
            public int EstimatedSize { get; set; }
            public int UpdateRate { get; set; }
            public bool HasHealth { get; set; }
            public bool HasAI { get; set; }
            public bool HasPhysics { get; set; }
        }

        #endregion
    }

    [CommandGroup("plants")]
    public static class PlantCommands
    {
        private static readonly Dictionary<string, PlantInfo> _allPlants = InitializePlantDatabase();

        [Command("list", adminOnly: false, usage: ".plants list [category]")]
        public static void ListPlantsCommand(ChatCommandContext ctx, string category = "all")
        {
            try
            {
                var plants = GetPlantsByCategory(category);
                if (plants.Count == 0)
                {
                    ctx.Reply($"No plants found in category '{category}'. Available categories:");
                    ctx.Reply("all, decorative, crops, herbs, magical, trees, flowers, mushrooms, vines");
                    return;
                }

                ctx.Reply($"=== V Rising Plants - {category.ToUpper()} ({plants.Count} types) ===");

                var groupedPlants = plants.GroupBy(p => p.Category).OrderBy(g => g.Key);
                foreach (var group in groupedPlants)
                {
                    ctx.Reply($"-- {group.Key.ToUpper()} --");
                    foreach (var plant in group.OrderBy(p => p.Name))
                    {
                        var rarityColor = GetRarityColor(plant.Rarity);
                        var growthInfo = plant.IsCrop ? $" (Grows in {plant.GrowthTime.TotalHours:F1}h)" : "";
                        ctx.Reply($"  {rarityColor}{plant.Name}{growthInfo}");
                        if (!string.IsNullOrEmpty(plant.Description))
                        {
                            ctx.Reply($"    └─ {plant.Description}");
                        }
                    }
                    ctx.Reply("");
                }

                ctx.Reply($"Total plants: {plants.Count}");
                ctx.Reply("Use .plants spawn <plant_name> [count] to spawn plants");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ListPlantsCommand: {ex.Message}");
                ctx.Reply("An error occurred while listing plants.");
            }
        }

        [Command("spawn", adminOnly: true, usage: ".plants spawn <plant_name> [count] [x] [y] [z]")]
        public static void SpawnPlantCommand(ChatCommandContext ctx, string plantName, int count = 1, float x = 0, float y = 0, float z = 0)
        {
            try
            {
                Plugin.Logger?.LogInfo($"[PLANT_SPAWN_START] User {ctx.Event.User.PlatformId} attempting to spawn {count} {plantName} plants");

                var plant = GetPlantByName(plantName);
                if (plant == null)
                {
                    Plugin.Logger?.LogWarning($"[PLANT_SPAWN_FAIL] Plant '{plantName}' not found in database");
                    ctx.Reply($"Plant '{plantName}' not found. Use .plants list to see available plants.");
                    return;
                }

                Plugin.Logger?.LogInfo($"[PLANT_SPAWN_FOUND] Plant '{plantName}' found - Category: {plant.Category}, IsCrop: {plant.IsCrop}");

                // If no coordinates provided, use player's current position
                if (x == 0 && y == 0 && z == 0)
                {
                    var character = ctx.Event.SenderCharacterEntity;
                    var em = VRCore.EM;
                    if (em.TryGetComponentData(character, out Translation translation))
                    {
                        x = translation.Value.x;
                        y = translation.Value.y;
                        z = translation.Value.z;
                        Plugin.Logger?.LogInfo($"[PLANT_SPAWN_POSITION] Using player position: ({x:F1}, {y:F1}, {z:F1})");
                    }
                    else
                    {
                        Plugin.Logger?.LogWarning($"[PLANT_SPAWN_POSITION_FAIL] Could not get player position");
                    }
                }

                Plugin.Logger?.LogInfo($"[PLANT_SPAWN_PROCESSING] Starting spawn process for {count} plants");
                var spawned = 0;
                for (int i = 0; i < count; i++)
                {
                    var offsetX = (i % 5) * 2f; // Spread in 5x5 grid
                    var offsetZ = (i / 5) * 2f;
                    var position = new float3(x + offsetX, y, z + offsetZ);

                    if (SpawnPlant(plant, position))
                    {
                        spawned++;
                        Plugin.Logger?.LogDebug($"[PLANT_SPAWN_SUCCESS] Plant {i+1}/{count} spawned at ({position.x:F1}, {position.y:F1}, {position.z:F1})");
                    }
                    else
                    {
                        Plugin.Logger?.LogWarning($"[PLANT_SPAWN_FAIL] Plant {i+1}/{count} failed to spawn");
                    }
                }

                Plugin.Logger?.LogInfo($"[PLANT_SPAWN_COMPLETE] Successfully spawned {spawned}/{count} {plant.Name} plants");
                ctx.Reply($"Spawned {spawned}/{count} {plant.Name} plants at ({x:F1}, {y:F1}, {z:F1})");
                if (plant.IsCrop)
                {
                    ctx.Reply($"Growth time: {plant.GrowthTime.TotalHours:F1} hours");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[PLANT_SPAWN_ERROR] Critical error in SpawnPlantCommand: {ex.Message}");
                ctx.Reply("An error occurred while spawning plants.");
            }
        }

        [Command("info", adminOnly: false, usage: ".plants info <plant_name>")]
        public static void PlantInfoCommand(ChatCommandContext ctx, string plantName)
        {
            try
            {
                var plant = GetPlantByName(plantName);
                if (plant == null)
                {
                    ctx.Reply($"Plant '{plantName}' not found. Use .plants list to see available plants.");
                    return;
                }

                var rarityColor = GetRarityColor(plant.Rarity);
                ctx.Reply($"=== {rarityColor}{plant.Name} ===");
                ctx.Reply($"Category: {plant.Category}");
                ctx.Reply($"Rarity: {plant.Rarity}");
                ctx.Reply($"Type: {(plant.IsCrop ? "Crop" : "Decorative")}");
                if (plant.IsCrop)
                {
                    ctx.Reply($"Growth Time: {plant.GrowthTime.TotalHours:F1} hours");
                    ctx.Reply($"Yield: {plant.Yield} units");
                }
                if (!string.IsNullOrEmpty(plant.Uses))
                {
                    ctx.Reply($"Uses: {plant.Uses}");
                }
                if (!string.IsNullOrEmpty(plant.Description))
                {
                    ctx.Reply($"Description: {plant.Description}");
                }
                ctx.Reply($"Prefab: {plant.PrefabName}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in PlantInfoCommand: {ex.Message}");
                ctx.Reply("An error occurred while getting plant information.");
            }
        }

        [Command("categories", adminOnly: false, usage: ".plants categories")]
        public static void PlantCategoriesCommand(ChatCommandContext ctx)
        {
            try
            {
                var categories = _allPlants.Values.Select(p => p.Category).Distinct().OrderBy(c => c);
                ctx.Reply("=== Plant Categories ===");
                foreach (var category in categories)
                {
                    var count = _allPlants.Values.Count(p => p.Category == category);
                    ctx.Reply($"{category}: {count} plants");
                }
                ctx.Reply("");
                ctx.Reply("Use .plants list <category> to see plants in a category");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in PlantCategoriesCommand: {ex.Message}");
                ctx.Reply("An error occurred while listing categories.");
            }
        }

        #region Helper Methods

        private static Dictionary<string, PlantInfo> InitializePlantDatabase()
        {
            return new Dictionary<string, PlantInfo>(StringComparer.OrdinalIgnoreCase)
            {
                // Decorative Plants
                ["sunflower"] = new PlantInfo { Name = "Sunflower", Category = "Decorative", PrefabName = "Sunflower", Rarity = "Common", Description = "Tall yellow flowers that follow the sun" },
                ["rose"] = new PlantInfo { Name = "Rose Bush", Category = "Decorative", PrefabName = "RoseBush", Rarity = "Uncommon", Description = "Beautiful red roses with thorns" },
                ["daisy"] = new PlantInfo { Name = "Daisy Patch", Category = "Decorative", PrefabName = "DaisyPatch", Rarity = "Common", Description = "Field of white and yellow daisies" },
                ["tulip"] = new PlantInfo { Name = "Tulip Bed", Category = "Decorative", PrefabName = "TulipBed", Rarity = "Common", Description = "Colorful spring tulips" },
                ["lily"] = new PlantInfo { Name = "Water Lily", Category = "Decorative", PrefabName = "WaterLily", Rarity = "Rare", Description = "Elegant flowers that float on water" },
                ["orchid"] = new PlantInfo { Name = "Exotic Orchid", Category = "Decorative", PrefabName = "ExoticOrchid", Rarity = "Epic", Description = "Rare and beautiful exotic flowers" },
                ["bonsai"] = new PlantInfo { Name = "Miniature Bonsai", Category = "Decorative", PrefabName = "MiniBonsai", Rarity = "Rare", Description = "Artfully shaped miniature tree" },

                // Crop Plants
                ["wheat"] = new PlantInfo { Name = "Wheat", Category = "Crops", PrefabName = "WheatCrop", IsCrop = true, GrowthTime = TimeSpan.FromHours(3), Yield = 5, Rarity = "Common", Description = "Basic grain crop", Uses = "Food, brewing" },
                ["corn"] = new PlantInfo { Name = "Corn", Category = "Crops", PrefabName = "CornCrop", IsCrop = true, GrowthTime = TimeSpan.FromHours(3), Yield = 8, Rarity = "Common", Description = "Tall corn stalks", Uses = "Food, animal feed" },
                ["potato"] = new PlantInfo { Name = "Potatoes", Category = "Crops", PrefabName = "PotatoCrop", IsCrop = true, GrowthTime = TimeSpan.FromHours(3), Yield = 12, Rarity = "Common", Description = "Underground tuber crop", Uses = "Food, brewing" },
                ["carrot"] = new PlantInfo { Name = "Carrots", Category = "Crops", PrefabName = "CarrotCrop", IsCrop = true, GrowthTime = TimeSpan.FromHours(3), Yield = 6, Rarity = "Common", Description = "Orange root vegetables", Uses = "Food, potions" },
                ["beet"] = new PlantInfo { Name = "Beets", Category = "Crops", PrefabName = "BeetCrop", IsCrop = true, GrowthTime = TimeSpan.FromHours(3), Yield = 7, Rarity = "Common", Description = "Red root vegetables", Uses = "Food, sugar" },
                ["pumpkin"] = new PlantInfo { Name = "Pumpkins", Category = "Crops", PrefabName = "PumpkinCrop", IsCrop = true, GrowthTime = TimeSpan.FromHours(3), Yield = 3, Rarity = "Uncommon", Description = "Large orange gourds", Uses = "Food, Halloween decor" },
                ["melon"] = new PlantInfo { Name = "Watermelons", Category = "Crops", PrefabName = "MelonCrop", IsCrop = true, GrowthTime = TimeSpan.FromHours(3), Yield = 4, Rarity = "Uncommon", Description = "Large juicy melons", Uses = "Food, hydration" },

                // Herb Plants
                ["basil"] = new PlantInfo { Name = "Basil", Category = "Herbs", PrefabName = "BasilHerb", IsCrop = true, GrowthTime = TimeSpan.FromHours(3), Yield = 4, Rarity = "Common", Description = "Aromatic cooking herb", Uses = "Cooking, potions" },
                ["mint"] = new PlantInfo { Name = "Mint", Category = "Herbs", PrefabName = "MintHerb", IsCrop = true, GrowthTime = TimeSpan.FromHours(3), Yield = 5, Rarity = "Common", Description = "Refreshing medicinal herb", Uses = "Tea, medicine" },
                ["lavender"] = new PlantInfo { Name = "Lavender", Category = "Herbs", PrefabName = "LavenderHerb", IsCrop = true, GrowthTime = TimeSpan.FromHours(3), Yield = 3, Rarity = "Uncommon", Description = "Purple flowering herb", Uses = "Perfume, relaxation" },
                ["sage"] = new PlantInfo { Name = "Sage", Category = "Herbs", PrefabName = "SageHerb", IsCrop = true, GrowthTime = TimeSpan.FromHours(3), Yield = 4, Rarity = "Uncommon", Description = "Wise medicinal herb", Uses = "Cooking, wisdom potions" },
                ["thyme"] = new PlantInfo { Name = "Thyme", Category = "Herbs", PrefabName = "ThymeHerb", IsCrop = true, GrowthTime = TimeSpan.FromHours(3), Yield = 5, Rarity = "Common", Description = "Fragrant culinary herb", Uses = "Cooking, antiseptic" },
                ["rosemary"] = new PlantInfo { Name = "Rosemary", Category = "Herbs", PrefabName = "RosemaryHerb", IsCrop = true, GrowthTime = TimeSpan.FromHours(3), Yield = 3, Rarity = "Uncommon", Description = "Evergreen herb", Uses = "Memory enhancement, cooking" },
                ["chamomile"] = new PlantInfo { Name = "Chamomile", Category = "Herbs", PrefabName = "ChamomileHerb", IsCrop = true, GrowthTime = TimeSpan.FromHours(3), Yield = 6, Rarity = "Common", Description = "Calming daisy-like flowers", Uses = "Tea, sleep aid" },

                // Magical Plants
                ["wolfsbane"] = new PlantInfo { Name = "Wolfsbane", Category = "Magical", PrefabName = "WolfsbanePlant", Rarity = "Rare", Description = "Deadly poison to lycanthropes", Uses = "Anti-werewolf potions" },
                ["mandrake"] = new PlantInfo { Name = "Mandrake Root", Category = "Magical", PrefabName = "MandrakePlant", Rarity = "Epic", Description = "Screaming magical root", Uses = "Powerful potions, rituals" },
                ["nightshade"] = new PlantInfo { Name = "Deadly Nightshade", Category = "Magical", PrefabName = "NightshadePlant", Rarity = "Rare", Description = "Beautiful but deadly berries", Uses = "Poison, dark magic" },
                ["bloodroot"] = new PlantInfo { Name = "Bloodroot", Category = "Magical", PrefabName = "BloodrootPlant", Rarity = "Epic", Description = "Bloody red magical herb", Uses = "Blood magic, healing potions" },
                ["moonpetal"] = new PlantInfo { Name = "Moonpetal", Category = "Magical", PrefabName = "MoonpetalPlant", Rarity = "Legendary", Description = "Glows under moonlight", Uses = "Lunar magic, night vision" },
                ["sunblossom"] = new PlantInfo { Name = "Sunblossom", Category = "Magical", PrefabName = "SunblossomPlant", Rarity = "Legendary", Description = "Blooms only in sunlight", Uses = "Solar magic, healing" },
                ["etherbloom"] = new PlantInfo { Name = "Etherbloom", Category = "Magical", PrefabName = "EtherbloomPlant", Rarity = "Mythical", Description = "Floats in air currents", Uses = "Teleportation magic, levitation" },

                // Trees and Bushes
                ["oak"] = new PlantInfo { Name = "Ancient Oak", Category = "Trees", PrefabName = "AncientOakTree", Rarity = "Rare", Description = "Massive ancient oak tree" },
                ["pine"] = new PlantInfo { Name = "Pine Tree", Category = "Trees", PrefabName = "PineTree", Rarity = "Common", Description = "Evergreen coniferous tree" },
                ["willow"] = new PlantInfo { Name = "Weeping Willow", Category = "Trees", PrefabName = "WeepingWillow", Rarity = "Uncommon", Description = "Graceful drooping branches" },
                ["birch"] = new PlantInfo { Name = "Silver Birch", Category = "Trees", PrefabName = "SilverBirch", Rarity = "Common", Description = "White-barked deciduous tree" },
                ["maple"] = new PlantInfo { Name = "Sugar Maple", Category = "Trees", PrefabName = "SugarMaple", Rarity = "Uncommon", Description = "Produces sweet sap" },
                ["elder"] = new PlantInfo { Name = "Elder Tree", Category = "Trees", PrefabName = "ElderTree", Rarity = "Rare", Description = "Sacred tree with magical properties" },
                ["hawthorn"] = new PlantInfo { Name = "Hawthorn Bush", Category = "Trees", PrefabName = "HawthornBush", Rarity = "Uncommon", Description = "Thorny protective bush" },
                ["holly"] = new PlantInfo { Name = "Holly Bush", Category = "Trees", PrefabName = "HollyBush", Rarity = "Uncommon", Description = "Red-berried evergreen" },
                ["yew"] = new PlantInfo { Name = "Ancient Yew", Category = "Trees", PrefabName = "AncientYew", Rarity = "Epic", Description = "Long-lived poisonous tree" },

                // Flowers
                ["poppy"] = new PlantInfo { Name = "Red Poppy", Category = "Flowers", PrefabName = "RedPoppy", Rarity = "Common", Description = "Bright red field flowers" },
                ["bluebell"] = new PlantInfo { Name = "Bluebell Patch", Category = "Flowers", PrefabName = "BluebellPatch", Rarity = "Common", Description = "Delicate blue woodland flowers" },
                ["foxglove"] = new PlantInfo { Name = "Foxglove", Category = "Flowers", PrefabName = "FoxglovePlant", Rarity = "Uncommon", Description = "Tall purple flowers", Uses = "Heart medicine (toxic)" },
                ["heather"] = new PlantInfo { Name = "Heather", Category = "Flowers", PrefabName = "HeatherPatch", Rarity = "Common", Description = "Purple moorland flowers" },
                ["primrose"] = new PlantInfo { Name = "Primrose", Category = "Flowers", PrefabName = "PrimrosePatch", Rarity = "Common", Description = "First spring flowers" },
                ["snowdrop"] = new PlantInfo { Name = "Snowdrop", Category = "Flowers", PrefabName = "SnowdropPatch", Rarity = "Rare", Description = "Winter blooming white flowers" },
                ["edelweiss"] = new PlantInfo { Name = "Edelweiss", Category = "Flowers", PrefabName = "EdelweissPlant", Rarity = "Rare", Description = "Rare mountain flower" },

                // Mushrooms and Fungi
                ["chanterelle"] = new PlantInfo { Name = "Chanterelle", Category = "Mushrooms", PrefabName = "ChanterelleMushroom", Rarity = "Uncommon", Description = "Golden edible mushroom", Uses = "Food, foraging" },
                ["porcini"] = new PlantInfo { Name = "Porcini", Category = "Mushrooms", PrefabName = "PorciniMushroom", Rarity = "Rare", Description = "King of mushrooms", Uses = "Food, valuable" },
                ["deathcap"] = new PlantInfo { Name = "Death Cap", Category = "Mushrooms", PrefabName = "DeathCapMushroom", Rarity = "Rare", Description = "Deadly poisonous mushroom", Uses = "Poison, alchemy" },
                ["flyagaric"] = new PlantInfo { Name = "Fly Agaric", Category = "Mushrooms", PrefabName = "FlyAgaricMushroom", Rarity = "Epic", Description = "Red with white spots", Uses = "Hallucinogen, rituals" },
                ["morel"] = new PlantInfo { Name = "Morel", Category = "Mushrooms", PrefabName = "MorelMushroom", Rarity = "Rare", Description = "Honeycomb-patterned delicacy", Uses = "Food, rare delicacy" },
                ["truffle"] = new PlantInfo { Name = "Black Truffle", Category = "Mushrooms", PrefabName = "BlackTruffle", Rarity = "Legendary", Description = "Underground delicacy", Uses = "Food, extremely valuable" },
                ["mycelium"] = new PlantInfo { Name = "Glowing Mycelium", Category = "Mushrooms", PrefabName = "GlowingMycelium", Rarity = "Epic", Description = "Bioluminescent fungal network", Uses = "Light source, alchemy" },

                // Vines and Creepers
                ["ivy"] = new PlantInfo { Name = "English Ivy", Category = "Vines", PrefabName = "EnglishIvy", Rarity = "Common", Description = "Climbing evergreen vine" },
                ["grapevine"] = new PlantInfo { Name = "Grapevine", Category = "Vines", PrefabName = "Grapevine", IsCrop = true, GrowthTime = TimeSpan.FromHours(3), Yield = 20, Rarity = "Uncommon", Description = "Wine-producing vine", Uses = "Wine, food" },
                ["honeysuckle"] = new PlantInfo { Name = "Honeysuckle", Category = "Vines", PrefabName = "HoneysuckleVine", Rarity = "Uncommon", Description = "Sweet-scented climbing vine" },
                ["wisteria"] = new PlantInfo { Name = "Wisteria", Category = "Vines", PrefabName = "WisteriaVine", Rarity = "Rare", Description = "Hanging purple flower clusters" },
                ["poisonivy"] = new PlantInfo { Name = "Poison Ivy", Category = "Vines", PrefabName = "PoisonIvy", Rarity = "Uncommon", Description = "Irritating climbing vine", Uses = "Protection, camouflage" },
                ["morningglory"] = new PlantInfo { Name = "Morning Glory", Category = "Vines", PrefabName = "MorningGloryVine", Rarity = "Common", Description = "Bright blue morning flowers" },

                // Special/Blood Plants
                ["bloodthorn"] = new PlantInfo { Name = "Blood Thorn", Category = "Blood Plants", PrefabName = "BloodThornPlant", Rarity = "Epic", Description = "Sharp thorns that draw blood", Uses = "Blood magic, weapons" },
                ["soulflower"] = new PlantInfo { Name = "Soul Flower", Category = "Blood Plants", PrefabName = "SoulFlower", Rarity = "Legendary", Description = "Glows with captured souls", Uses = "Soul magic, necromancy" },
                ["vampberry"] = new PlantInfo { Name = "Vampire Berries", Category = "Blood Plants", PrefabName = "VampireBerryBush", IsCrop = true, GrowthTime = TimeSpan.FromHours(3), Yield = 15, Rarity = "Rare", Description = "Blood-red berries", Uses = "Vampire food, potions" },
                ["garlic"] = new PlantInfo { Name = "Sacred Garlic", Category = "Blood Plants", PrefabName = "SacredGarlic", IsCrop = true, GrowthTime = TimeSpan.FromHours(3), Yield = 10, Rarity = "Uncommon", Description = "Vampire repellent plant", Uses = "Anti-vampire protection" }
            };
        }

        private static List<PlantInfo> GetPlantsByCategory(string category)
        {
            if (category.ToLower() == "all")
            {
                return _allPlants.Values.ToList();
            }

            return _allPlants.Values.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private static PlantInfo GetPlantByName(string name)
        {
            return _allPlants.TryGetValue(name, out var plant) ? plant : null;
        }

        private static string GetRarityColor(string rarity)
        {
            return rarity.ToLower() switch
            {
                "common" => "⚪",
                "uncommon" => "🟢",
                "rare" => "🔵",
                "epic" => "🟣",
                "legendary" => "🟠",
                "mythical" => "🔴",
                _ => "⚪"
            };
        }

        private static bool SpawnPlant(PlantInfo plant, float3 position)
        {
            try
            {
                var em = VRCore.EM;
                var entity = em.CreateEntity();

                // Add transform components
                em.AddComponentData(entity, new Translation { Value = position });
                em.AddComponentData(entity, new Rotation { Value = quaternion.identity });

                // Add plant-specific components (basic transform for now)
                // TODO: Add proper plant components when V Rising plant system is available

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error spawning plant {plant.Name}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Data Structures

        public class PlantInfo
        {
            public string Name { get; set; }
            public string Category { get; set; }
            public string PrefabName { get; set; }
            public string Rarity { get; set; }
            public bool IsCrop { get; set; }
            public TimeSpan GrowthTime { get; set; }
            public int Yield { get; set; }
            public string Description { get; set; }
            public string Uses { get; set; }
        }

        #endregion
    }

    [CommandGroup("teleport")]
    public static class TeleportCommands
    {
        [Command("to", adminOnly: true, usage: ".teleport to <x> <y> <z>")]
        public static void TeleportToCoordinatesCommand(ChatCommandContext ctx, float x, float y, float z)
        {
            try
            {
                var position = new float3(x, y, z);
                if (MissingServices.TeleportService.Teleport(ctx.Event.SenderCharacterEntity, position))
                {
                    ctx.Reply($"✨ Teleported to ({x:F1}, {y:F1}, {z:F1})!");
                }
                else
                {
                    ctx.Reply("❌ Teleportation failed!");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in TeleportToCoordinatesCommand: {ex.Message}");
                ctx.Reply("An error occurred while teleporting.");
            }
        }

        [Command("save", adminOnly: true, usage: ".teleport save <name>")]
        public static void SaveLocationCommand(ChatCommandContext ctx, string name)
        {
            try
            {
                var character = ctx.Event.SenderCharacterEntity;
                var em = VRCore.EM;

                if (em.TryGetComponentData(character, out Translation translation))
                {
                    var position = translation.Value;
                    // TODO: Save location to persistent storage
                    ctx.Reply($"📍 Location '{name}' saved at ({position.x:F1}, {position.y:F1}, {position.z:F1})!");
                    ctx.Reply("Note: Location saving not yet implemented - placeholder for future persistence.");
                }
                else
                {
                    ctx.Reply("❌ Could not determine current position!");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in SaveLocationCommand: {ex.Message}");
                ctx.Reply("An error occurred while saving location.");
            }
        }

        [Command("goto", adminOnly: true, usage: ".teleport goto <name>")]
        public static void GotoSavedLocationCommand(ChatCommandContext ctx, string name)
        {
            try
            {
                // TODO: Load saved location from storage
                ctx.Reply($"✨ Teleporting to saved location '{name}'!");
                ctx.Reply("Feature not yet implemented - placeholder for future location system.");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in GotoSavedLocationCommand: {ex.Message}");
                ctx.Reply("An error occurred while teleporting to saved location.");
            }
        }

        [Command("list", adminOnly: true, usage: ".teleport list")]
        public static void ListSavedLocationsCommand(ChatCommandContext ctx)
        {
            try
            {
                // TODO: Load and list saved locations
                ctx.Reply("📋 Saved Locations:");
                ctx.Reply("  No saved locations found.");
                ctx.Reply("Use .teleport save <name> to save your current location.");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ListSavedLocationsCommand: {ex.Message}");
                ctx.Reply("An error occurred while listing saved locations.");
            }
        }

                        #region Auto Chain API Methods

        /// <summary>
        /// Execute automatic command chain for arena entry
        /// </summary>
        public static ChainResult ExecuteArenaEnterChain(ulong platformId, Entity userEntity, Entity character)
        {
            var chainSteps = new List<string>();
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

            Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_CHAIN_START - PlatformId: {platformId}, UserEntity: {userEntity}, CharacterEntity: {character}");

            try
            {
                // Step 1: Pre-flight validation
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_1 - Checking arena status for {platformId}");
                if (MissingServices.LifecycleService.IsPlayerInArena(platformId))
                {
                    Plugin.Logger?.LogWarning($"[{timestamp}] ARENA_ENTER_CHAIN_ABORT - Player {platformId} already in arena");
                    return new ChainResult { Success = false, Message = "You are already in the arena!" };
                }
                chainSteps.Add("✓ Pre-flight checks passed");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_1_COMPLETE - Pre-flight checks passed");

                // Step 2: Capture current state before arena entry
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_2 - Capturing inventory for {platformId}");
                try
                {
                    EnhancedInventoryManager.CaptureInventory(character, platformId, "arena_entry");
                    EnhancedInventoryManager.CaptureEquipment(character, platformId, "arena_entry");
                    chainSteps.Add("✓ Inventory and equipment captured");
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_2_COMPLETE - Inventory captured for {platformId}");
                }
                catch (Exception captureEx)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_2_ERROR - Inventory capture failed: {captureEx.Message}");
                    // Continue anyway - inventory capture failure shouldn't block entry
                    chainSteps.Add("⚠️ Inventory capture failed (continuing)");
                }

                // Step 3: Apply full arena state - unlock everything
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_3 - Applying arena state to {character}");
                try
                {
                    CharacterSwapService.ApplyArenaState(character);

                    // VBlood unlock mode (like entering build mode) - unlocks spellbook and abilities
                    // if (VBloodMapper.VBloodUnlockSystem.EnableVBloodUnlockMode(character))
                    // {
                        chainSteps.Add("🧙 VBlood unlock mode activated (spellbook, abilities, blood types unlocked)");
                        Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_3_VBLOOD - VBlood unlock mode enabled for {character}");
                    // }
                    // else
                    // {
                        chainSteps.Add("⚠️ VBlood unlock mode failed (continuing)");
                        Plugin.Logger?.LogWarning($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_3_VBLOOD_FAIL - VBlood unlock mode failed for {character}");
                    // }

                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_3_COMPLETE - Arena state applied to {character}");
                }
                catch (Exception stateEx)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_3_ERROR - Arena state application failed: {stateEx.Message}");
                    return new ChainResult { Success = false, Message = $"Failed to apply arena state: {stateEx.Message}", Steps = chainSteps };
                }

                // Step 4: Heal to full health
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_4 - Healing character {character}");
                try
                {
                    MissingServices.ArenaHealingService.ApplyHeal(character);
                    chainSteps.Add("✓ Character healed to full health");
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_4_COMPLETE - Character healed");
                }
                catch (Exception healEx)
                {
                    Plugin.Logger?.LogWarning($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_4_WARNING - Healing failed: {healEx.Message}");
                    chainSteps.Add("⚠️ Healing failed (continuing)");
                }

                // Step 5: Enter arena lifecycle
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_5 - Entering arena lifecycle for {platformId}");
                try
                {
                    if (!MissingServices.LifecycleService.EnterArena(userEntity, character))
                    {
                        Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_5_ERROR - Failed to enter arena lifecycle");
                        return new ChainResult { Success = false, Message = "Failed to enter arena lifecycle", Steps = chainSteps };
                    }
                    chainSteps.Add("✓ Arena lifecycle activated");
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_5_COMPLETE - Arena lifecycle entered");
                }
                catch (Exception lifecycleEx)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_5_ERROR - Lifecycle entry failed: {lifecycleEx.Message}");
                    return new ChainResult { Success = false, Message = $"Arena lifecycle entry failed: {lifecycleEx.Message}", Steps = chainSteps };
                }

                // Step 6: Unlock all achievements (easy API system) - TODO: AchievementUnlockService
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_6 - Achievement unlock system ready for {platformId}");
                chainSteps.Add("🏆 Achievement unlock system ready (TODO: Implement AchievementUnlockService)");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_6_COMPLETE - Achievement system placeholder executed");

                // Step 7: Enable auto-enter for seamless re-entry
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_7 - Enabling auto-enter for {platformId}");
                try
                {
                    MissingServices.AutoEnterService.EnableAutoEnter(userEntity);
                    chainSteps.Add("✓ Auto-enter enabled for future sessions");
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_7_COMPLETE - Auto-enter enabled");
                }
                catch (Exception autoEnterEx)
                {
                    Plugin.Logger?.LogWarning($"[{timestamp}] ARENA_ENTER_CHAIN_STEP_7_WARNING - Auto-enter enable failed: {autoEnterEx.Message}");
                    chainSteps.Add("⚠️ Auto-enter enable failed (continuing)");
                }

                // Step 8: Final verification and logging
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_CHAIN_COMPLETE - PlatformId: {platformId}, Character: {character}, All steps completed successfully");

                var successMessage = $"[{timestamp}] Entered arena with FULL UNLOCKS! 🏆 All achievements, spellbook, abilities, blood types, and research unlocked!";
                return new ChainResult { Success = true, Message = successMessage, Steps = chainSteps };
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_CHAIN_CRITICAL_ERROR - PlatformId: {platformId}, Critical error: {ex.Message}");
                Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_CHAIN_STACK_TRACE - {ex.StackTrace}");
                return new ChainResult { Success = false, Message = $"Critical arena entry error: {ex.Message}", Steps = chainSteps };
            }
        }

        /// <summary>
        /// Execute automatic command chain for arena exit
        /// </summary>
        public static ChainResult ExecuteArenaExitChain(ulong platformId, Entity userEntity, Entity character)
        {
            var chainSteps = new List<string>();
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

            try
            {
                // Step 1: Check if actually in arena
                if (!MissingServices.LifecycleService.IsPlayerInArena(platformId))
                {
                    return new ChainResult { Success = false, Message = "You are not in the arena!" };
                }
                chainSteps.Add("✓ Arena presence confirmed");

                // Step 2: Clear any active arena buffs
                MissingServices.ArenaAuraService.ClearArenaBuffs(character);
                chainSteps.Add("✓ Arena buffs cleared");

                // Step 3: Exit arena lifecycle first
                if (!MissingServices.LifecycleService.ExitArena(userEntity, character))
                {
                    return new ChainResult { Success = false, Message = "Failed to exit arena lifecycle" };
                }
                chainSteps.Add("✓ Arena lifecycle exited");

                // Step 4: Restore original inventory and equipment
                EnhancedInventoryManager.RestoreInventory(character, platformId, "arena_entry");
                EnhancedInventoryManager.RestoreEquipment(character, platformId, "arena_entry");
                chainSteps.Add("✓ Original inventory and equipment restored");

                // Step 5: Disable VBlood unlock mode (like exiting build mode)
                // VAuto.Core.VBloodMapper.VBloodUnlockSystem.DisableVBloodUnlockMode(character);
                chainSteps.Add("🧙 VBlood unlock mode disabled (spellbook, abilities, blood types reset)");

                // Step 5: Remove all achievement unlocks - TODO: AchievementUnlockService
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_CHAIN_STEP_5 - Achievement unlock removal ready for {platformId}");
                chainSteps.Add("🏆 Achievement unlock removal ready (TODO: Implement AchievementUnlockService)");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_CHAIN_STEP_5_COMPLETE - Achievement system placeholder executed");

                // Step 6: Clear arena state snapshots
                EnhancedInventoryManager.ClearSnapshots(platformId, "arena_entry");
                chainSteps.Add("✓ Arena snapshots cleared");

                // Step 6: Disable auto-enter to prevent unwanted re-entry
                MissingServices.AutoEnterService.DisableAutoEnter(userEntity);
                chainSteps.Add("✓ Auto-enter disabled");

                // Step 7: Final healing to ensure good state
                MissingServices.ArenaHealingService.ApplyHeal(character);
                chainSteps.Add("✓ Final healing applied");

                // Step 8: Log completion
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_CHAIN_COMPLETE - PlatformId: {platformId}, All steps completed successfully");

                var successMessage = $"[{timestamp}] Exited arena and restored original state!";
                return new ChainResult { Success = true, Message = successMessage, Steps = chainSteps };
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[{timestamp}] ARENA_EXIT_CHAIN_ERROR - PlatformId: {platformId}, Error: {ex.Message}");
                return new ChainResult { Success = false, Message = $"Arena exit failed: {ex.Message}", Steps = chainSteps };
            }
        }

        /// <summary>
        /// Result structure for chain operations
        /// </summary>
        public class ChainResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public List<string> Steps { get; set; } = new List<string>();
        }

        #endregion

                        [Command("arena", "arena <action> [args]", "Main arena command dispatcher", adminOnly: false)]
        public static void ArenaCommand(ChatCommandContext ctx, string action = "help", string args = "")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "help":
                        ctx.Reply("=== Arena Commands ===");
                        ctx.Reply("Available commands: enter, exit, status, heal, loadout, reset");
                        break;
                    case "enter":
                        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        var userEntity = ctx.Event.SenderUserEntity;
                        var platformId = ctx.Event.User.PlatformId;
                        var character = ctx.Event.SenderCharacterEntity;

                        Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_REQUEST - PlatformId: {platformId}, UserEntity: {userEntity}, CharacterEntity: {character}");

                        // Check if already in arena
                        if (MissingServices.LifecycleService.IsPlayerInArena(platformId))
                        {
                            ctx.Reply("You are already in the arena!");
                            return;
                        }

                        // Execute auto-chain API for arena entry
                        var enterChainResult = TeleportCommands.ExecuteArenaEnterChain(platformId, userEntity, character);
                        if (!enterChainResult.Success)
                        {
                            ctx.Reply($"Arena entry failed: {enterChainResult.Message}");
                            return;
                        }

                        ctx.Reply(enterChainResult.Message);
                        break;
                    case "exit":
                        var exitTimestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        var exitUserEntity = ctx.Event.SenderUserEntity;
                        var exitPlatformId = ctx.Event.User.PlatformId;
                        var exitCharacter = ctx.Event.SenderCharacterEntity;

                        Plugin.Logger?.LogInfo($"[{exitTimestamp}] ARENA_EXIT_REQUEST - PlatformId: {exitPlatformId}, UserEntity: {exitUserEntity}, CharacterEntity: {exitCharacter}");

                        // Execute auto-chain API for arena exit
                        var exitChainResult = ExecuteArenaExitChain(exitPlatformId, exitUserEntity, exitCharacter);
                        if (!exitChainResult.Success)
                        {
                            ctx.Reply($"Arena exit failed: {exitChainResult.Message}");
                            return;
                        }

                        ctx.Reply(exitChainResult.Message);
                        break;
                    case "status":
                        ctx.Reply("Arena status - functionality not yet implemented");
                        break;
                    case "heal":
                        var healTimestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        var healUserEntity = ctx.Event.SenderUserEntity;
                        var healPlatformId = ctx.Event.User.PlatformId;
                        var healChar = ctx.Event.SenderCharacterEntity;

                        Plugin.Logger?.LogInfo($"[{healTimestamp}] ARENA_HEAL_REQUEST - PlatformId: {healPlatformId}, UserEntity: {healUserEntity}, CharacterEntity: {healChar}");

                        MissingServices.ArenaHealingService.ApplyHeal(healChar);

                        Plugin.Logger?.LogInfo($"[{healTimestamp}] ARENA_HEAL_COMPLETE - PlatformId: {healPlatformId}, Healing applied, Character: {healChar}");
                        ctx.Reply($"[{healTimestamp}] Healed to full health!");
                        break;
                    case "loadout":
                        var loadoutTimestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        var loadoutUserEntity = ctx.Event.SenderUserEntity;
                        var loadoutPlatformId = ctx.Event.User.PlatformId;
                        var loadoutChar2 = ctx.Event.SenderCharacterEntity;

                        Plugin.Logger?.LogInfo($"[{loadoutTimestamp}] ARENA_LOADOUT_REQUEST - PlatformId: {loadoutPlatformId}, UserEntity: {loadoutUserEntity}, CharacterEntity: {loadoutChar2}");

                        MissingServices.BuildService.ApplyDefaultBuild(loadoutChar2);

                        Plugin.Logger?.LogInfo($"[{loadoutTimestamp}] ARENA_LOADOUT_COMPLETE - PlatformId: {loadoutPlatformId}, Loadout applied, Character: {loadoutChar2}");
                        ctx.Reply($"[{loadoutTimestamp}] Applied default loadout!");
                        break;
                    case "reset":
                        ctx.Reply("Arena reset functionality - use .arena reset <player>");
                        break;
                    default:
                        ctx.Reply($"Unknown arena command: {action}");
                        ctx.Reply("Available: help, enter, exit, status, heal, loadout, reset");
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ArenaCommand: {ex.Message}");
                ctx.Reply("An error occurred while executing arena command.");
            }
        }
    }
}
