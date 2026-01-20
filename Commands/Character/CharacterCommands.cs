using System;
using VampireCommandFramework;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using VAuto.Core;
using VAuto.Services;

namespace VAuto.Commands.Character
{
    /// <summary>
    /// Character commands - pure character swapping operations.
    /// Separate from arena system - no arena lifecycle interactions.
    /// </summary>
    [CommandGroup("ch")]
    public static class CharacterCommands
    {
        public static void CharacterCommand(ChatCommandContext ctx, string action, string args)
        {
            switch (action.ToLower())
            {
                case "create":
                    CreateCharacterCommand(ctx);
                    break;
                case "enter":
                    EnterCharacterCommand(ctx);
                    break;
                case "exit":
                    ExitCharacterCommand(ctx);
                    break;
                case "swap":
                    SwapCharacterCommand(ctx);
                    break;
                case "status":
                    StatusCommand(ctx);
                    break;
                default:
                    ctx.Reply($"Unknown character action: {action}");
                    break;
            }
        }

        [Command("create", adminOnly: true, usage: ".ch create")]
        public static void CreateCharacterCommand(ChatCommandContext ctx)
        {
            try
            {
                var userEntity = ctx.Event.SenderUserEntity;
                var normalCharacter = ctx.Event.SenderCharacterEntity;
                var platformId = ctx.Event.User.PlatformId;

                if (DualCharacterManager.HasDualState(platformId))
                {
                    var state = DualCharacterManager.GetState(platformId);
                    if (state.PvPCharacter != Entity.Null && VRCore.EM.Exists(state.PvPCharacter))
                    {
                        ctx.Reply("PvP character already exists!");
                        return;
                    }
                }

                var dualState = DualCharacterManager.GetOrCreateState(platformId, normalCharacter);
                ctx.Reply("PvP character created successfully!");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in CreateCharacterCommand: {ex.Message}");
                ctx.Reply("An error occurred while creating the character.");
            }
        }

        [Command("enter", adminOnly: true, usage: ".ch enter")]
        public static void EnterCharacterCommand(ChatCommandContext ctx)
        {
            try
            {
                var userEntity = ctx.Event.SenderUserEntity;
                var platformId = ctx.Event.User.PlatformId;

                if (!DualCharacterManager.HasDualState(platformId))
                {
                    ctx.Reply("No PvP character found. Use .ch create first.");
                    return;
                }

                var state = DualCharacterManager.GetState(platformId);
                if (state.PvPCharacter == Entity.Null || !VRCore.EM.Exists(state.PvPCharacter))
                {
                    ctx.Reply("PvP character not available. Use .ch create first.");
                    return;
                }

                if (state.IsPvPActive)
                {
                    ctx.Reply("Already using PvP character!");
                    return;
                }

                if (DualCharacterManager.SwitchToPvP(platformId, userEntity))
                {
                    ctx.Reply("Switched to PvP character!");
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

        [Command("exit", adminOnly: true, usage: ".ch exit")]
        public static void ExitCharacterCommand(ChatCommandContext ctx)
        {
            try
            {
                var userEntity = ctx.Event.SenderUserEntity;
                var platformId = ctx.Event.User.PlatformId;

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

                if (DualCharacterManager.SwitchToNormal(platformId, userEntity))
                {
                    ctx.Reply("Switched back to normal character!");
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

        [Command("swap", adminOnly: true, usage: ".ch swap")]
        public static void SwapCharacterCommand(ChatCommandContext ctx)
        {
            try
            {
                var userEntity = ctx.Event.SenderUserEntity;
                var platformId = ctx.Event.User.PlatformId;

                if (!DualCharacterManager.HasDualState(platformId))
                {
                    ctx.Reply("No dual character setup found. Use .ch create first.");
                    return;
                }

                var state = DualCharacterManager.GetState(platformId);
                if (state.PvPCharacter == Entity.Null || !VRCore.EM.Exists(state.PvPCharacter))
                {
                    ctx.Reply("PvP character not available. Use .ch create first.");
                    return;
                }

                bool success;
                if (state.IsPvPActive)
                {
                    success = DualCharacterManager.SwitchToNormal(platformId, userEntity);
                    if (success) ctx.Reply("Swapped to normal character!");
                }
                else
                {
                    success = DualCharacterManager.SwitchToPvP(platformId, userEntity);
                    if (success) ctx.Reply("Swapped to PvP character!");
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

        [Command("status", adminOnly: true, usage: ".ch status")]
        public static void StatusCommand(ChatCommandContext ctx)
        {
            try
            {
                var platformId = ctx.Event.User.PlatformId;

                ctx.Reply("Character Status:");

                var hasDualState = DualCharacterManager.HasDualState(platformId);
                ctx.Reply($"  Dual Character Setup: {hasDualState}");

                if (hasDualState)
                {
                    var state = DualCharacterManager.GetState(platformId);
                    ctx.Reply($"  PvP Character Active: {state.IsPvPActive}");
                    ctx.Reply($"  PvP Character Exists: {(state.PvPCharacter != Entity.Null && VRCore.EM.Exists(state.PvPCharacter))}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in StatusCommand: {ex.Message}");
                ctx.Reply("An error occurred while getting character status.");
            }
        }
    }
}
