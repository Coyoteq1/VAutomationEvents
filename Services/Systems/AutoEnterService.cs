using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM;
using ProjectM.Network;
using ProjectM.Gameplay;
using VAuto.Core;
using VAuto.Data;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Auto Enter Service - Handles automatic arena entry functionality
    /// </summary>
    public sealed class AutoEnterService : VAuto.Services.Interfaces.IService
    {
        private static readonly Lazy<AutoEnterService> _instance = new(() => new AutoEnterService());
        public static AutoEnterService Instance => _instance.Value;

        private bool _initialized = false;
        private readonly Dictionary<ulong, AutoEnterData> _autoEnterPlayers = new();
        private readonly object _lock = new object();
        
        public bool IsInitialized => _initialized;
        public ManualLogSource Log => Plugin.Logger;

        private AutoEnterService() { }

        #region Initialization
        public void Initialize()
        {
            lock (_lock)
            {
                if (_initialized) return;
                
                try
                {
                    Log?.LogInfo("[AutoEnterService] Initializing auto enter service...");
                    
                    _autoEnterPlayers.Clear();
                    _initialized = true;
                    
                    Log?.LogInfo("[AutoEnterService] Auto enter service initialized successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[AutoEnterService] Failed to initialize: {ex.Message}");
                    throw;
                }
            }
        }

        public void Cleanup()
        {
            lock (_lock)
            {
                if (!_initialized) return;
                
                try
                {
                    Log?.LogInfo("[AutoEnterService] Cleaning up auto enter service...");
                    
                    _autoEnterPlayers.Clear();
                    _initialized = false;
                    
                    Log?.LogInfo("[AutoEnterService] Auto enter service cleaned up successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[AutoEnterService] Failed to cleanup: {ex.Message}");
                }
            }
        }
        #endregion

        #region Core Functionality
        public void EnableAutoEnter(ulong platformId)
        {
            lock (_lock)
            {
                if (!_autoEnterPlayers.ContainsKey(platformId))
                {
                    _autoEnterPlayers[platformId] = new AutoEnterData
                    {
                        PlatformId = platformId,
                        IsEnabled = true,
                        EnabledAt = DateTime.UtcNow,
                        LastAttempt = DateTime.MinValue
                    };
                }
                else
                {
                    _autoEnterPlayers[platformId].IsEnabled = true;
                    _autoEnterPlayers[platformId].EnabledAt = DateTime.UtcNow;
                }
                
                Log?.LogInfo($"[AutoEnterService] Auto-enter enabled for player {platformId}");
            }
        }

        public void DisableAutoEnter(ulong platformId)
        {
            lock (_lock)
            {
                if (_autoEnterPlayers.ContainsKey(platformId))
                {
                    _autoEnterPlayers[platformId].IsEnabled = false;
                    _autoEnterPlayers[platformId].DisabledAt = DateTime.UtcNow;
                }
                
                Log?.LogInfo($"[AutoEnterService] Auto-enter disabled for player {platformId}");
            }
        }

        public bool IsAutoEnterEnabled(ulong platformId)
        {
            lock (_lock)
            {
                return _autoEnterPlayers.TryGetValue(platformId, out var data) && data.IsEnabled;
            }
        }

        public bool TryAutoEnter(Entity user, Entity character, float3 position)
        {
            try
            {
                if (user == Entity.Null || character == Entity.Null)
                    return false;

                if (!VAuto.Core.Core.TryRead<User>(user, out var userData))
                    return false;

                var platformId = userData.PlatformId;
                
                if (!IsAutoEnterEnabled(platformId))
                    return false;

                var data = _autoEnterPlayers[platformId];
                
                // Check cooldown
                if (DateTime.UtcNow - data.LastAttempt < TimeSpan.FromSeconds(5))
                    return false;

                // Check if player is already in arena
                if (ArenaZoneService.Instance.IsPositionInAnyArena(position))
                    return false;

                // Attempt to enter arena
                data.LastAttempt = DateTime.UtcNow;
                data.AttemptsCount++;

                Log?.LogInfo($"[AutoEnterService] Attempting auto-enter for {userData.CharacterName} ({platformId})");

                var lifecycleService = VAuto.Services.ServiceManager.GetService<VAuto.Services.Lifecycle.LifecycleService>();
                return lifecycleService?.EnterArena(user, character, "default_arena") ?? false;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[AutoEnterService] Auto-enter failed: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Statistics
        public int GetAutoEnterEnabledCount()
        {
            lock (_lock)
            {
                return _autoEnterPlayers.Values.Count(p => p.IsEnabled);
            }
        }

        public Dictionary<string, object> GetPlayerAutoEnterStats(ulong platformId)
        {
            lock (_lock)
            {
                if (!_autoEnterPlayers.TryGetValue(platformId, out var data))
                    return new Dictionary<string, object>();

                return new Dictionary<string, object>
                {
                    ["Enabled"] = data.IsEnabled,
                    ["EnabledAt"] = data.EnabledAt,
                    ["Attempts"] = data.AttemptsCount,
                    ["LastAttempt"] = data.LastAttempt,
                    ["CooldownRemaining"] = GetCooldownRemaining(platformId)
                };
            }
        }

        private TimeSpan GetCooldownRemaining(ulong platformId)
        {
            lock (_lock)
            {
                if (!_autoEnterPlayers.TryGetValue(platformId, out var data))
                    return TimeSpan.Zero;

                var timeSinceLastAttempt = DateTime.UtcNow - data.LastAttempt;
                return timeSinceLastAttempt < TimeSpan.FromSeconds(5)
                    ? TimeSpan.FromSeconds(5) - timeSinceLastAttempt
                    : TimeSpan.Zero;
            }
        }
        #endregion

        #region Data Structures
        public class AutoEnterData
        {
            public ulong PlatformId { get; set; }
            public bool IsEnabled { get; set; }
            public DateTime EnabledAt { get; set; }
            public DateTime DisabledAt { get; set; }
            public DateTime LastAttempt { get; set; }
            public int AttemptsCount { get; set; }
        }
        #endregion
    }
}