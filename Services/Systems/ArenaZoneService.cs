using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using VAuto.Core;
using VAuto.Data;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Central zone authority - manages arena activation, deactivation, and player tracking.
    /// Owns arena lifecycle and triggers building & glow services.
    /// </summary>
    public sealed class ArenaZoneService : VAuto.Services.Interfaces.IService
    {
        private static readonly Lazy<ArenaZoneService> _instance = new(() => new ArenaZoneService());
        public static ArenaZoneService Instance => _instance.Value;
        
        private readonly Dictionary<int, ArenaZoneState> _arenaZones = new();
        private readonly object _lock = new object();
        private ManualLogSource _log;
        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => _log;
        
        // Services owned by zone
        private readonly ArenaGlowService _glowService = new();
        private readonly ArenaBuildingService _buildingService = new();

        private ArenaZoneService()
        {
            _log = Plugin.Logger;
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            _log?.LogInfo("[ArenaZoneService] Initialized");
        }

        public void Cleanup()
        {
            if (!_isInitialized) return;
            ClearAll();
            _isInitialized = false;
            _log?.LogInfo("[ArenaZoneService] Cleaned up");
        }

        #region Zone Management
        /// <summary>
        /// Activate arena zone - spawns buildings and glow ONCE.
        /// </summary>
        public bool ActivateZone(int arenaId, VAuto.Data.ArenaZone zone)
        {
            if (!zone.IsValid)
            {
                _log?.LogWarning($"[ArenaZoneService] Invalid zone for arena {arenaId}");
                return false;
            }

            lock (_lock)
            {
                try
                {
                    if (_arenaZones.ContainsKey(arenaId) && _arenaZones[arenaId].IsActive)
                    {
                        _log?.LogInfo($"[ArenaZoneService] Arena {arenaId} already active");
                        return true;
                    }

                    // Create zone state
                    var zoneState = new ArenaZoneState
                    {
                        ArenaId = arenaId,
                        Zone = zone,
                        IsActive = true,
                        ActivePlayers = new HashSet<ulong>(),
                        GlowEntities = new List<Entity>(),
                        BuildingEntities = new List<Entity>(),
                        CreatedAt = DateTime.UtcNow,
                        ActivatedAt = DateTime.UtcNow
                    };

                    // Spawn buildings (zone-level)
                    var buildingEntities = _buildingService.SpawnArenaBuildings(arenaId, zone);
                    zoneState.BuildingEntities.AddRange(buildingEntities);

                    // Spawn glow borders (zone-level)
                    _glowService.BuildBorderGlows(arenaId, zone);
                    zoneState.GlowEntities = _glowService.GetGlowEntities(arenaId) ?? new List<Entity>();

                    // Store zone state
                    _arenaZones[arenaId] = zoneState;

                    _log?.LogInfo($"[ArenaZoneService] Activated arena {arenaId} with {zoneState.BuildingEntities.Count} buildings and {zoneState.GlowEntities.Count} glow entities");
                    return true;
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[ArenaZoneService] Failed to activate arena {arenaId}: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Deactivate arena zone - removes buildings and glow.
        /// </summary>
        public bool DeactivateZone(int arenaId)
        {
            lock (_lock)
            {
                try
                {
                    if (!_arenaZones.ContainsKey(arenaId))
                    {
                        _log?.LogWarning($"[ArenaZoneService] Arena {arenaId} not found");
                        return false;
                    }

                    var zoneState = _arenaZones[arenaId];

                    // Force exit all remaining players
                    var playersToExit = zoneState.ActivePlayers.ToList();
                    foreach (var platformId in playersToExit)
                    {
                        RemovePlayerFromZone(arenaId, platformId);
                    }

                    // Remove glow (zone-level)
                    _glowService.ClearArenaGlows(arenaId);

                    // Remove buildings (zone-level)
                    _buildingService.DespawnArenaBuildings(arenaId);

                    // Mark inactive
                    zoneState.IsActive = false;
                    zoneState.DeactivatedAt = DateTime.UtcNow;

                    _log?.LogInfo($"[ArenaZoneService] Deactivated arena {arenaId}");
                    return true;
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[ArenaZoneService] Failed to deactivate arena {arenaId}: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Get zone state by arena ID.
        /// </summary>
        public ArenaZoneState GetZoneState(int arenaId)
        {
            lock (_lock)
            {
                return _arenaZones.TryGetValue(arenaId, out var state) ? state : null;
            }
        }

        /// <summary>
        /// Get all active arena IDs.
        /// </summary>
        public List<int> GetActiveArenaIds()
        {
            lock (_lock)
            {
                return _arenaZones.Values
                    .Where(z => z.IsActive)
                    .Select(z => z.ArenaId)
                    .ToList();
            }
        }
        #endregion

        #region Player Tracking
        /// <summary>
        /// Add player to zone - triggers zone activation if first player.
        /// </summary>
        public bool AddPlayerToZone(int arenaId, ulong platformId)
        {
            lock (_lock)
            {
                try
                {
                    // Ensure zone exists
                    if (!_arenaZones.ContainsKey(arenaId))
                    {
                        // Create default zone if not exists - use configured center if available
                        var defaultCenter = Plugin.ZoneEnable ? Plugin.ZoneCenter : new float3(-1000f, 5f, -500f);
                        var defaultRadius = Plugin.ZoneEnable ? Plugin.ZoneRadius : 50f;

                        var defaultZone = new ArenaZone
                        {
                            Center = defaultCenter,
                            Radius = defaultRadius,
                            Shape = ArenaZoneShape.Circle,
                            Name = "Default Practice Arena"
                        };

                        if (!ActivateZone(arenaId, defaultZone))
                        {
                            return false;
                        }
                    }

                    var zoneState = _arenaZones[arenaId];

                    // Check if already in zone
                    if (zoneState.ActivePlayers.Contains(platformId))
                    {
                        _log?.LogDebug($"[ArenaZoneService] Player {platformId} already in arena {arenaId}");
                        return true;
                    }

                    // Add player
                    zoneState.ActivePlayers.Add(platformId);
                    zoneState.LastActivity = DateTime.UtcNow;

                    _log?.LogInfo($"[ArenaZoneService] Player {platformId} entered arena {arenaId}. Total players: {zoneState.ActivePlayers.Count}");
                    return true;
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[ArenaZoneService] Failed to add player {platformId} to arena {arenaId}: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Remove player from zone - triggers zone deactivation if last player (if configured).
        /// </summary>
        public bool RemovePlayerFromZone(int arenaId, ulong platformId)
        {
            lock (_lock)
            {
                try
                {
                    if (!_arenaZones.ContainsKey(arenaId))
                    {
                        _log?.LogWarning($"[ArenaZoneService] Arena {arenaId} not found for player removal");
                        return false;
                    }

                    var zoneState = _arenaZones[arenaId];

                    if (!zoneState.ActivePlayers.Contains(platformId))
                    {
                        _log?.LogDebug($"[ArenaZoneService] Player {platformId} not in arena {arenaId}");
                        return true;
                    }

                    // Remove player
                    zoneState.ActivePlayers.Remove(platformId);
                    zoneState.LastActivity = DateTime.UtcNow;

                    _log?.LogInfo($"[ArenaZoneService] Player {platformId} exited arena {arenaId}. Remaining players: {zoneState.ActivePlayers.Count}");

                    // Optional: Auto-deactivate if no players left
                    if (zoneState.ActivePlayers.Count == 0)
                    {
                        _log?.LogInfo($"[ArenaZoneService] No players remaining in arena {arenaId} - considering deactivation");
                        // Note: Deactivation is optional and controlled by configuration
                        // DeactivateZone(arenaId); // Uncomment for auto-deactivation
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[ArenaZoneService] Failed to remove player {platformId} from arena {arenaId}: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Check if player is in any arena zone.
        /// </summary>
        public bool IsPlayerInAnyArena(ulong platformId)
        {
            lock (_lock)
            {
                return _arenaZones.Values.Any(z => z.IsActive && z.ActivePlayers.Contains(platformId));
            }
        }

        /// <summary>
        /// Get arena ID for player, or null if not in arena.
        /// </summary>
        public int? GetPlayerArenaId(ulong platformId)
        {
            lock (_lock)
            {
                var zone = _arenaZones.Values.FirstOrDefault(z => z.IsActive && z.ActivePlayers.Contains(platformId));
                return zone?.ArenaId;
            }
        }

        /// <summary>
        /// Get active player count for arena.
        /// </summary>
        public int GetActivePlayerCount(int arenaId)
        {
            lock (_lock)
            {
                return _arenaZones.TryGetValue(arenaId, out var state) ? state.ActivePlayers.Count : 0;
            }
        }

        /// <summary>
        /// Get all active players in arena.
        /// </summary>
        public HashSet<ulong> GetActivePlayers(int arenaId)
        {
            lock (_lock)
            {
                return _arenaZones.TryGetValue(arenaId, out var state) 
                    ? new HashSet<ulong>(state.ActivePlayers) 
                    : new HashSet<ulong>();
            }
        }
        #endregion

        #region Zone Queries
        /// <summary>
        /// Check if position is within any active arena zone.
        /// </summary>
        public bool IsPositionInAnyArena(float3 position)
        {
            lock (_lock)
            {
                return _arenaZones.Values.Any(z => z.IsActive && IsPositionInZone(position, z.Zone));
            }
        }

        /// <summary>
        /// Alias for IsPositionInAnyArena for compatibility.
        /// </summary>
        public bool IsInArena(float3 position) => IsPositionInAnyArena(position);

        /// <summary>
        /// Check if position is in a transition zone (near arena borders).
        /// </summary>
        public bool IsInTransitionZone(float3 position)
        {
            lock (_lock)
            {
                // Simple implementation: check if near any arena border
                return _arenaZones.Values.Any(z => z.IsActive && IsNearZoneBorder(position, z.Zone));
            }
        }

        /// <summary>
        /// Check if a player is currently in an immortal zone.
        /// </summary>
        public bool IsPlayerImmortal(ulong platformId)
        {
            lock (_lock)
            {
                var zoneState = _arenaZones.Values.FirstOrDefault(z => z.IsActive && z.ActivePlayers.Contains(platformId));
                return zoneState?.Zone.IsImmortal ?? false;
            }
        }

        /// <summary>
        /// Check if position is within specific arena zone.
        /// </summary>
        public bool IsPositionInArena(float3 position, int arenaId)
        {
            lock (_lock)
            {
                if (!_arenaZones.TryGetValue(arenaId, out var zoneState) || !zoneState.IsActive)
                    return false;

                return IsPositionInZone(position, zoneState.Zone);
            }
        }

        /// <summary>
        /// Get arena zone containing position, or null if none.
        /// </summary>
        public ArenaZoneState GetZoneContainingPosition(float3 position)
        {
            lock (_lock)
            {
                return _arenaZones.Values.FirstOrDefault(z => z.IsActive && IsPositionInZone(position, z.Zone));
            }
        }
        #endregion

        #region Cleanup
        /// <summary>
        /// Clear all arena zones (server shutdown).
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                try
                {
                    var arenaIds = _arenaZones.Keys.ToList();
                    foreach (var arenaId in arenaIds)
                    {
                        DeactivateZone(arenaId);
                    }

                    _arenaZones.Clear();
                    _glowService.ClearAll();
                    _buildingService.ClearAll();

                    _log?.LogInfo("[ArenaZoneService] Cleared all arena zones");
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[ArenaZoneService] Failed to clear all arenas: {ex.Message}");
                }
            }
        }
        #endregion

        #region Helper Methods
        private static bool IsPositionInZone(float3 position, ArenaZone zone)
        {
            if (zone == null) return false;
            return zone.Shape switch
            {
                ArenaZoneShape.Circle => math.distance(new float2(position.x, position.z), new float2(zone.Center.x, zone.Center.z)) <= zone.Radius,
                ArenaZoneShape.Box => IsPositionInBox(position, zone.Center, zone.Dimensions),
                _ => false
            };
        }

        private static bool IsPositionInBox(float3 position, float3 center, float2 dimensions)
        {
            var halfWidth = dimensions.x / 2f;
            var halfLength = dimensions.y / 2f;
            
            return position.x >= center.x - halfWidth &&
                   position.x <= center.x + halfWidth &&
                   position.z >= center.z - halfLength &&
                   position.z <= center.z + halfLength;
        }
        private static bool IsNearZoneBorder(float3 position, ArenaZone zone)
        {
            if (zone == null) return false;
            var dist = zone.Shape switch
            {
                ArenaZoneShape.Circle => math.abs(math.distance(new float2(position.x, position.z), new float2(zone.Center.x, zone.Center.z)) - zone.Radius),
                _ => 5f // Default threshold
            };
            return dist < 2.0f; // 2 unit threshold for transition
        }
        #endregion
    }

    /// <summary>
    /// Arena zone state managed by ArenaZoneService.
    /// </summary>
    public class ArenaZoneState
    {
        public int ArenaId { get; set; }
        public ArenaZone Zone { get; set; }
        public bool IsActive { get; set; }
        public HashSet<ulong> ActivePlayers { get; set; }
        public List<Entity> GlowEntities { get; set; }
        public List<Entity> BuildingEntities { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public DateTime? DeactivatedAt { get; set; }
        public DateTime LastActivity { get; set; }
    }
}
