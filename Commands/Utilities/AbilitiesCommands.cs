using VampireCommandFramework;
using VAuto.Services.Systems;
using Unity.Entities;
using ProjectM.Network;

namespace VAuto.Commands.Utilities
{
    public static class AbilitiesCommands
    {
        [Command("abilities", adminOnly: true, usage: ".abilities [unlock|restore]")]
        public static void AbilitiesCommand(ChatCommandContext ctx, string action = "")
        {
            if (string.IsNullOrEmpty(action))
            {
                ctx.Reply("Usage: .abilities [unlock|restore]");
                ctx.Reply("  unlock - Unlock all abilities and spellbook");
                ctx.Reply("  restore - Restore original abilities");
                return;
            }

            var userEntity = ctx.Event.SenderUserEntity;
            var charEntity = ctx.Event.SenderCharacterEntity;

            switch (action.ToLower())
            {
                case "unlock":
                    if (AbilityOverrideService.Instance.UnlockAllAbilities(userEntity, charEntity))
                    {
                        ctx.Reply("✓ Unlocked all abilities and spellbook!");
                        ctx.Reply("  - All VBlood abilities");
                        ctx.Reply("  - Complete spellbook (70+ spells)");
                        ctx.Reply("  - All shapeshifting forms");
                    }
                    else
                    {
                        ctx.Reply("✗ Failed to unlock abilities");
                    }
                    break;

                case "restore":
                    if (AbilityOverrideService.Instance.RestoreAbilities(userEntity, charEntity))
                    {
                        ctx.Reply("✓ Restored original abilities");
                    }
                    else
                    {
                        ctx.Reply("✗ Failed to restore abilities");
                    }
                    break;

                default:
                    ctx.Reply("Invalid action. Use: unlock or restore");
                    break;
            }
        }

        [Command("unlockall", adminOnly: true, usage: ".unlockall")]
        public static void UnlockAllCommand(ChatCommandContext ctx)
        {
            var userEntity = ctx.Event.SenderUserEntity;
            var charEntity = ctx.Event.SenderCharacterEntity;

            if (AbilityOverrideService.Instance.UnlockAllAbilities(userEntity, charEntity))
            {
                ctx.Reply("✓ Unlocked complete spellbook!");
            }
            else
            {
                ctx.Reply("✗ Failed to unlock abilities");
            }
        }

        [Command("overrideunlock", adminOnly: true, usage: ".overrideunlock [on|off]")]
        public static void OverrideUnlockCommand(ChatCommandContext ctx, string state = "")
        {
            if (string.IsNullOrEmpty(state) || (state.ToLower() != "on" && state.ToLower() != "off"))
            {
                ctx.Reply("Usage: .overrideunlock [on|off] - toggles forced DebugEvents unlock behavior for your account");
                return;
            }

            var userEntity = ctx.Event.SenderUserEntity;
            if (!VAuto.Core.Core.TryRead<User>(userEntity, out var user))
            {
                ctx.Reply("Failed to determine user/platform id");
                return;
            }

            var platformId = user.PlatformId;
            var lifecycleService = VAuto.Services.ServiceManager.GetService<VAuto.Services.Lifecycle.LifecycleService>();
            bool enable = state.ToLower() == "on";
            if (lifecycleService.SetOverrideUnlock(platformId, enable))
            {
                ctx.Reply(enable ? "✓ Override unlocks enabled for your account" : "✓ Override unlocks disabled for your account");
            }
            else
            {
                ctx.Reply("✗ Failed to set override unlock state");
            }
        }    }
}
