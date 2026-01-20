using System;
using VampireCommandFramework;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using VAuto.Core;
using VAuto.Services;
using VAuto.Services.Lifecycle;

namespace VAuto.Commands.Arena
{
    /// <summary>
    /// Arena commands - pure arena lifecycle operations.
    /// Uses LifecycleService only - no direct zone/building interactions.
    /// </summary>
    public static class ArenaCommands
    {
        [Command("arenaenter", description: "Enter the arena and begin arena gameplay", adminOnly: true)]
        public static void EnterArenaCommand(ChatCommandContext ctx)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var platformId = ctx.Event.User.PlatformId;
            var userEntity = ctx.Event.SenderUserEntity;
            var character = ctx.Event.SenderCharacterEntity;

            try
            {
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_COMMAND_START - PlatformId: {platformId}");

                // Get LifecycleService from ServiceManager
                var lifecycleService = ServiceManager.GetService<VAuto.Services.Lifecycle.LifecycleService>();
                if (lifecycleService != null && lifecycleService.IsPlayerInArena(platformId))
                {
                    Plugin.Logger?.LogWarning($"[{timestamp}] ARENA_ENTER_REJECTED - Player {platformId} already in arena");
                    ctx.Reply("You are already in arena!");
                    return;
                }

                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_EXECUTING - Calling LifecycleService");
                if (lifecycleService?.EnterArena(userEntity, character, "default_arena") == true)
                {
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_SUCCESS - Player {platformId} entered arena");
                    ctx.Reply("Entered arena successfully!");
                }
                else
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_FAILED - LifecycleService returned false");
                    ctx.Reply("Failed to enter arena.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_CRITICAL_ERROR - PlatformId: {platformId}, Error: {ex.Message}");
                ctx.Reply("An error occurred while entering the arena.");
            }
        }

        [Command("arenaexit", description: "Exit the arena and return to normal gameplay", adminOnly: true)]
        public static void ExitArenaCommand(ChatCommandContext ctx)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var platformId = ctx.Event.User.PlatformId;
            var userEntity = ctx.Event.SenderUserEntity;
            var character = ctx.Event.SenderCharacterEntity;

            try
            {
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_COMMAND_START - PlatformId: {platformId}");

                // Get LifecycleService from ServiceManager
                var lifecycleService = ServiceManager.GetService<VAuto.Services.Lifecycle.LifecycleService>();
                if (lifecycleService == null || !lifecycleService.IsPlayerInArena(platformId))
                {
                    Plugin.Logger?.LogWarning($"[{timestamp}] ARENA_EXIT_REJECTED - Player {platformId} not in arena");
                    ctx.Reply("You are not in arena!");
                    return;
                }

                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_EXECUTING - Calling LifecycleService");
                if (lifecycleService.ExitArena(userEntity, character))
                {
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_SUCCESS - Player {platformId} exited arena");
                    ctx.Reply("Exited arena successfully!");
                }
                else
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_EXIT_FAILED - LifecycleService returned false");
                    ctx.Reply("Failed to exit arena.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[{timestamp}] ARENA_EXIT_CRITICAL_ERROR - PlatformId: {platformId}, Error: {ex.Message}");
                ctx.Reply("An error occurred while exiting the arena.");
            }
        }

        [Command("arenastatus", description: "Display current arena status and player state information", adminOnly: true)]
        public static void StatusCommand(ChatCommandContext ctx)
        {
            try
            {
                var platformId = ctx.Event.User.PlatformId;
                var character = ctx.Event.SenderCharacterEntity;

                ctx.Reply("Arena Status:");

                // Get LifecycleService from ServiceManager
                var lifecycleService = ServiceManager.GetService<VAuto.Services.Lifecycle.LifecycleService>();
                var isInArena = lifecycleService?.IsPlayerInArena(platformId) ?? false;
                ctx.Reply($"  In Arena: {isInArena}");

                if (isInArena)
                {
                    // Use static GameSystems class directly
                    var vBloodHooked = VAuto.Services.Systems.GameSystems.IsPlayerInArena(platformId);
                    ctx.Reply($"  VBlood Hook: {vBloodHooked}");

                    var pos = VRCore.EM.GetComponentData<Unity.Transforms.Translation>(character).Value;
                    var zoneService = VAuto.Services.Systems.ArenaZoneService.Instance;
                    var inZone = zoneService.IsPositionInAnyArena(pos);
                    ctx.Reply($"  In Arena Zone: {inZone}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in StatusCommand: {ex.Message}");
                ctx.Reply("An error occurred while getting arena status.");
            }
        }

        [Command("arenareset", description: "Reset player state and exit arena if currently in one", adminOnly: true)]
        public static void ResetCommand(ChatCommandContext ctx)
        {
            try
            {
                var userEntity = ctx.Event.SenderUserEntity;
                var character = ctx.Event.SenderCharacterEntity;
                var platformId = ctx.Event.User.PlatformId;

                // Get LifecycleService from ServiceManager
                var lifecycleService = ServiceManager.GetService<VAuto.Services.Lifecycle.LifecycleService>();
                if (lifecycleService != null && lifecycleService.IsPlayerInArena(platformId))
                {
                    lifecycleService.ExitArena(userEntity, character);
                }

                var em = VRCore.EM;
                if (em.HasComponent<ProjectM.Health>(character))
                {
                    var health = em.GetComponentData<ProjectM.Health>(character);
                    health.Value = health.MaxHealth;
                    em.SetComponentData(character, health);
                }
                ctx.Reply("Player state reset!");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ResetCommand: {ex.Message}");
                ctx.Reply("An error occurred while resetting player state.");
            }
        }
    }
}
