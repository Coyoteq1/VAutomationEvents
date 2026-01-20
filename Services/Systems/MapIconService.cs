using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using ProjectM;
using Stunlock.Core;
using VAuto.Core;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Map Icon Service - Manages map icons for arena objects and players
    /// </summary>
    public static class MapIconService
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();
        
        public static bool IsInitialized => _initialized;
        public static ManualLogSource Log => Plugin.Logger;

        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized) return;
                _initialized = true;
                Log?.LogInfo("[MapIconService] Initialized");
            }
        }

        public static void Cleanup()
        {
            lock (_lock)
            {
                if (!_initialized) return;
                _initialized = false;
                Log?.LogInfo("[MapIconService] Cleaned up");
            }
        }

        public static void AddMapIcon(Entity entity, PrefabGUID iconPrefab) { }
        public static void RemoveMapIcon(Entity entity) { }
        public static void RefreshPlayerIcons() { }

        public static int GetActiveIconCount() => 0;
    }
}