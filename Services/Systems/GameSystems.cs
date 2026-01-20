using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using VAuto.Services.Interfaces;

namespace VAuto.Services.Systems
{
    public static class GameSystems
    {
        private static bool _initialized;
        private static ManualLogSource _log;
        private static readonly HashSet<ulong> _hookedPlayers = new();

        public static bool IsInitialized => _initialized;
        public static ManualLogSource Log => _log;

        public static void Initialize()
        {
            _log = Plugin.Logger;
            _hookedPlayers.Clear();
            _initialized = true;
        }

        public static void Cleanup()
        {
            _hookedPlayers.Clear();
            _initialized = false;
        }

        public static bool IsPlayerInArena(ulong platformId) => _hookedPlayers.Contains(platformId);

        public static void MarkPlayerEnteredArena(ulong platformId)
        {
            _hookedPlayers.Add(platformId);
            _log?.LogInfo($"[GameSystems] Player {platformId} marked as entered arena");
            // Logic to update player state in world/persistence if needed
        }

        public static void MarkPlayerExitedArena(ulong platformId)
        {
            _hookedPlayers.Remove(platformId);
            _log?.LogInfo($"[GameSystems] Player {platformId} marked as exited arena");
            // Logic to update player state in world/persistence if needed
        }

        public static List<ulong> GetActiveHookedPlayers() => _hookedPlayers.ToList();

        public static void ClearAllHooks() => _hookedPlayers.Clear();
    }
}