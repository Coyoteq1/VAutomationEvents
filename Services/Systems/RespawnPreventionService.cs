using System;
using BepInEx.Logging;
using VAuto.Services.Interfaces;

namespace VAuto.Services.Systems
{
    public static class RespawnPreventionService
    {
        private static bool _initialized;
        private static ManualLogSource _log;

        public static bool IsInitialized => _initialized;
        public static ManualLogSource Log => _log;

        public static void Initialize()
        {
            _log = Plugin.Logger;
            _initialized = true;
        }

        public static void Cleanup()
        {
            _initialized = false;
        }
    }
}