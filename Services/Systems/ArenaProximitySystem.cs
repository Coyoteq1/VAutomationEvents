using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using ProjectM;
using ProjectM.Network;
using VAuto.Core;
using VAuto.Data;
using VAuto.Services.Lifecycle;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Automatic zone detection system - monitors player proximity to arenas.
    /// Triggers automatic arena entry/exit based on distance.
    /// </summary>
    public sealed class ArenaProximitySystem
    {
        private static readonly Lazy<ArenaProximitySystem> _instance = new(() => new ArenaProximitySystem());
        public static ArenaProximitySystem Instance => _instance.Value;

        private const float UPDATE_INTERVAL = 3.0f; // Check every 3 seconds (matches map icon system)
        private const float ENTER_RADIUS = 50f;    // Auto-enter radius
        private const float EXIT_RADIUS = 75f;     // Auto-exit radius (larger than enter)

        private float _updateTimer = 0f;
        private readonly Dictionary<ulong, bool> _playerInArena = new();
        private readonly object _lock = new object();
        private ManualLogSource _log;
        private bool _foundPlayer = false; // Flag to track if players were found near arena

        private ArenaProximitySystem()
        {
            _log = Plugin.Logger;
        }

        /// <summary>
        /// Update method called from game system.
        /// </summary>
        public void Update(float deltaTime)
        {
            _updateTimer += deltaTime;
            if (_updateTimer >= UPDATE_INTERVAL)
            {
                _updateTimer = 0f;
                CheckPlayerProximity();
            }
        }

        /// <summary>
        /// Check all players for proximity to arena zones.
        /// </summary>
        private void CheckPlayerProximity()
        {
            _foundPlayer = false; // Reset flag for this check cycle

            var em = VAuto.Core.Core.EntityManager;

            var query = em.CreateEntityQuery(ComponentType.ReadOnly<User>());
            var users = query.ToEntityArray(Allocator.Temp);

            try
            {
                foreach (var userEntity in users)
                {
                    try
                    {
                        if (!VAuto.Core.Core.Exists(userEntity)) continue;
                        if (!VAuto.Core.Core.TryRead<User>(userEntity, out var user)) continue;

                        var character = user.LocalCharacter._Entity;
                        if (character == Entity.Null || !VAuto.Core.Core.Exists(character)) continue;

                        var platformId = user.PlatformId;
                        var currentPosition = GetEntityPosition(character);

                        lock (_lock)
                        {
                            var wasInArena = _playerInArena.GetValueOrDefault(platformId, false);
                            var isInArena = IsPlayerNearArena(currentPosition);

                            _playerInArena[platformId] = isInArena;

                            // Set found player flag if any player exists (for map icon updates)
                            _foundPlayer = true;

                            // Handle state changes
                            if (!wasInArena && isInArena)
                            {
                                // Player entered arena proximity
                                _log?.LogInfo($"[Proximity] Player {user.CharacterName} ({platformId}) entered arena proximity");
                                HandlePlayerEnter(userEntity, character, platformId);
                            }
                            else if (wasInArena && !isInArena)
                            {
                                // Player exited arena proximity
                                _log?.LogInfo($"[Proximity] Player {user.CharacterName} ({platformId}) exited arena proximity");
                                HandlePlayerExit(userEntity, character, platformId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _log?.LogError($"[Proximity] Error processing user {userEntity.Index}: {ex}");
                    }
                }

                // Update map icons for ALL players every 3 seconds
                if (_foundPlayer)
                {
                    GlobalMapIconService.UpdateAllPlayerIcons();
                    _log?.LogDebug("[Proximity] Updated global map icons for all players");
                }
            }
            finally
            {
                users.Dispose();
            }
        }

        /// <summary>
        /// Handle automatic player entry.
        /// </summary>
        private void HandlePlayerEnter(Entity userEntity, Entity character, ulong platformId)
        {
            try
            {
                // Check if player is already in arena lifecycle
                var lifecycleService = VAuto.Services.ServiceManager.GetService<VAuto.Services.Lifecycle.LifecycleService>();
                if (lifecycleService == null) return;

                if (lifecycleService.IsPlayerInArena(platformId))
                {
                    _log?.LogDebug($"[Proximity] Player {platformId} already in arena lifecycle");
                    return;
                }

                // Add player to zone (triggers zone activation if needed)
                var zoneService = ArenaZoneService.Instance;
                zoneService.AddPlayerToZone(0, platformId);

                // Enter arena lifecycle
                lifecycleService.EnterArena(userEntity, character, "default_arena");

                _log?.LogInfo($"[Proximity] Auto-entered player {platformId} into arena");
            }
            catch (Exception ex)
            {
                _log?.LogError($"[Proximity] Failed to handle player entry for {platformId}: {ex}");
            }
        }

        /// <summary>
        /// Handle automatic player exit.
        /// </summary>
        private void HandlePlayerExit(Entity userEntity, Entity character, ulong platformId)
        {
            try
            {
                // Check if player is in arena lifecycle
                var lifecycleService = VAuto.Services.ServiceManager.GetService<VAuto.Services.Lifecycle.LifecycleService>();
                if (lifecycleService == null) return;

                if (!lifecycleService.IsPlayerInArena(platformId))
                {
                    _log?.LogDebug($"[Proximity] Player {platformId} not in arena lifecycle");
                    return;
                }

                // Exit arena lifecycle
                lifecycleService.ExitArena(userEntity, character);

                // Remove player from zone (triggers zone deactivation if last player)
                var zoneService = ArenaZoneService.Instance;
                zoneService.RemovePlayerFromZone(0, platformId);

                _log?.LogInfo($"[Proximity] Auto-exited player {platformId} from arena");
            }
            catch (Exception ex)
            {
                _log?.LogError($"[Proximity] Failed to handle player exit for {platformId}: {ex}");
            }
        }

        /// <summary>
        /// Check if position is near any arena zone.
        /// </summary>
        private bool IsPlayerNearArena(float3 position)
        {
            var zoneService = ArenaZoneService.Instance;
            var activeZones = zoneService.GetActiveArenaIds();

            foreach (var arenaId in activeZones)
            {
                var zoneState = zoneService.GetZoneState(arenaId);
                if (zoneState?.Zone != null)
                {
                    var zone = zoneState.Zone;
                    var distance = math.distance(position, zone.Center);

                    // Use enter radius for detection
                    if (distance <= ENTER_RADIUS)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Get entity position safely.
        /// </summary>
        private static float3 GetEntityPosition(Entity entity)
        {
            if (VAuto.Core.Core.TryRead<Unity.Transforms.Translation>(entity, out var translation))
                return translation.Value;
            if (VAuto.Core.Core.TryRead<Unity.Transforms.LocalToWorld>(entity, out var ltw))
                return ltw.Position;
            if (VAuto.Core.Core.TryRead<Unity.Transforms.Translation>(entity, out var transform))
                return transform.Value;
            return float3.zero;
        }

        /// <summary>
        /// Clear all proximity tracking data.
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                _playerInArena.Clear();
                _updateTimer = 0f;
                _log?.LogInfo("[Proximity] Cleared all proximity tracking data");
            }
        }

        /// <summary>
        /// Get current proximity status for a player.
        /// </summary>
        public bool IsPlayerInProximity(ulong platformId)
        {
            lock (_lock)
            {
                return _playerInArena.GetValueOrDefault(platformId, false);
            }
        }
    }
}
