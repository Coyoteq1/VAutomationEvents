using ProjectM;
using Unity.Entities;
using UnityEngine;
using VAuto.Services;

namespace VAuto.Core
{
    /// <summary>
    /// Single entry point for arena system initialization.
    /// Ensures proper boot order and prevents multiple initialization attempts.
    /// </summary>
    public static class ArenaBootstrap
    {
        /// <summary>
        /// Attempts to boot the arena system. Safe to call multiple times.
        /// </summary>
        public static void TryBoot(EntityManager em)
        {
            if (ArenaRuntime.State != ArenaRuntimeState.Off)
                return;

            try
            {
                ArenaRuntime.Set(ArenaRuntimeState.Booting);

                if (!InitManager.Ensure("LifecycleService",
                    () => EnsureLifecycleInitialized()))
                {
                    ArenaRuntime.Set(ArenaRuntimeState.Failed);
                    Plugin.Logger?.LogInfo("Arena bootstrap failed - LifecycleService initialization failed");
                    return;
                }

                ArenaRuntime.Set(ArenaRuntimeState.Ready);
            }
            catch (System.Exception ex)
            {
                ArenaRuntime.Set(ArenaRuntimeState.Failed);
                Plugin.Logger?.LogInfo($"Arena bootstrap failed with exception: {ex}");
            }
        }

        /// <summary>
        /// Ensures LifecycleService is properly initialized.
        /// </summary>
        private static bool EnsureLifecycleInitialized()
        {
            try
            {
                MissingServices.LifecycleService.Initialize();
                Plugin.Logger?.LogInfo("LifecycleService initialization completed");
                return true;
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogInfo($"LifecycleService initialization failed: {ex}");
                return false;
            }
        }
    }
}












