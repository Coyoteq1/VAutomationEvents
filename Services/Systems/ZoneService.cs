using System;
using BepInEx.Logging;
using VAuto.Services.Interfaces;

namespace VAuto.Services.Systems
{
    public static class ZoneService
    {
        public static bool IsInitialized => ArenaZoneService.Instance.IsInitialized;
        public static ManualLogSource Log => ArenaZoneService.Instance.Log;

        public static void Initialize() => ArenaZoneService.Instance.Initialize();
        public static void Cleanup() => ArenaZoneService.Instance.Cleanup();
    }
}