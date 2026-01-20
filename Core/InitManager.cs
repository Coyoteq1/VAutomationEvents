using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace VAuto.Core
{
    /// <summary>
    /// Global initialization manager for ensuring services are initialized exactly once
    /// in the correct order, preventing race conditions and initialization spam.
    /// </summary>
    public static class InitManager
    {
        private static readonly HashSet<string> _initialized = new();

        /// <summary>
        /// Ensures a service is initialized exactly once.
        /// Returns true if already initialized or initialization succeeds.
        /// Returns false if initialization failed.
        /// </summary>
        public static bool Ensure(string key, Func<bool> init)
        {
            if (_initialized.Contains(key))
                return true;

            if (!init())
                return false;

            _initialized.Add(key);
            Plugin.Logger?.LogError($"InitManager → {key} ready");
            return true;
        }

        /// <summary>
        /// Checks if a service is already initialized.
        /// </summary>
        public static bool IsInitialized(string key)
        {
            return _initialized.Contains(key);
        }

        /// <summary>
        /// Forces re-initialization of a service (use with caution).
        /// </summary>
        public static bool ForceReinit(string key, Func<bool> init)
        {
            _initialized.Remove(key);
            return Ensure(key, init);
        }

        /// <summary>
        /// Resets all initialization state (for testing only).
        /// </summary>
        public static void Reset()
        {
            _initialized.Clear();
            Plugin.Logger?.LogError("InitManager reset");
        }
    }
}












