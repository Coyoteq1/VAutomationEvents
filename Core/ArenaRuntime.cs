using BepInEx.Logging;

namespace VAuto.Core
{
    /// <summary>
    /// Global arena system runtime state - single source of truth for arena readiness.
    /// Prevents timing issues, race conditions, and initialization spam.
    /// </summary>
    public enum ArenaRuntimeState
    {
        Off,
        Booting,
        Ready,
        Failed
    }

    public static class ArenaRuntime
    {
        public static ArenaRuntimeState State { get; private set; } = ArenaRuntimeState.Off;
        public static bool IsReady => State == ArenaRuntimeState.Ready;
        public static bool IsFailed => State == ArenaRuntimeState.Failed;

        /// <summary>
        /// Sets the arena runtime state and logs the transition.
        /// </summary>
        public static void Set(ArenaRuntimeState state)
        {
            State = state;
            Plugin.Logger?.LogError($"ArenaRuntime → {state}");
        }

        /// <summary>
        /// Resets the arena runtime to Off state (for testing or recovery).
        /// </summary>
        public static void Reset()
        {
            State = ArenaRuntimeState.Off;
            Plugin.Logger?.LogError("ArenaRuntime reset to Off");
        }
    }
}












