using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM;
using ProjectM.Network;
using VAuto.Core;
using BepInEx.Logging;

namespace VAuto.Services.Lifecycle
{
    /// <summary>
    /// Tracks player locations and triggers automatic lifecycle events
    /// </summary>
    public static class PlayerLocationTracker
    {
        private static bool _initialized = false;
        private static readonly Dictionary<ulong, PlayerLocationData> _playerLocations = new();
        private static readonly List<ZoneConfig> _configuredZones = new();
        private static readonly object _lock = new object();
        private static ManualLogSource _log;
        
        public static bool IsInitialized => _initialized;
        public static ManualLogSource Log => _log;

        #region Initialization
        public static void Initialize(ManualLogSource logger)
        {
            lock (_lock)
            {
                if (_initialized) return;
                
                try
                {
                    _log = logger;
                    logger?.LogInfo("[PlayerLocationTracker] Initializing player location tracker...");
                    
                    _playerLocations.Clear();
                    _configuredZones.Clear();
                    
                    // Load zone configurations from config files
                    LoadZoneConfigurations();
                    
                    _initialized = true;
                    
                    logger?.LogInfo("[PlayerLocationTracker] Player location tracker initialized successfully");
                }
                catch (Exception ex)
                {
                    logger?.LogError($"[PlayerLocationTracker] Failed to initialize: {ex.Message}");
                    throw;
                }
            }
        }

        public static void Cleanup()
        {
            lock (_lock)
            {
                if (!_initialized) return;
                
                try
                {
                    _log?.LogInfo("[PlayerLocationTracker] Cleaning up player location tracker...");
                    
                    _playerLocations.Clear();
                    _configuredZones.Clear();
                    _initialized = false;
                    
                    _log?.LogInfo("[PlayerLocationTracker] Player location tracker cleaned up successfully");
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[PlayerLocationTracker] Failed to cleanup: {ex.Message}");
                }
            }
        }

        private static void LoadZoneConfigurations()
        {
            try
            {
                // Load from configuration files like arena_zones.json
                // Example zone: -1000, 5, -500 with radius 50
                
                var defaultZones = new[]
                {
                    new ZoneConfig
                    {
                        Name = "MainArena",
                        Center = new float3(-1000f, 5f, -500f),
                        Radius = 50f,
                        ArenaId = "main_arena",
                        AutoEnter = true,
                        AutoExit = true
                    },
                    new ZoneConfig
                    {
                        Name = "PvPArena", 
                        Center = new float3(0f, 10f, 0f),
                        Radius = 30f,
                        ArenaId = "pvp_arena",
                        AutoEnter = true,
                        AutoExit = true
                    }
                };

                _configuredZones.AddRange(defaultZones);
                
                _log?.LogInfo($"[PlayerLocationTracker] Loaded {_configuredZones.Count} zone configurations");
            }
            catch (Exception ex)
            {
                _log?.LogError($"[PlayerLocationTracker] Failed to load zone configurations: {ex.Message}");
            }
        }
        #endregion

        #region Location Tracking
        public static void UpdatePlayerLocation(Entity user, Entity character)
        {
            try
            {
                if (user == Entity.Null || character == Entity.Null)
                    return;

                var em = VAutoCore.EntityManager;
                if (!em.TryGetComponentData(user, out User userData))
                    return;

                var platformId = userData.PlatformId;
                var characterName = userData.CharacterName.ToString();
                var position = GetEntityPosition(character);

                lock (_lock)
                {
                    // Update or create player location data
                    if (!_playerLocations.ContainsKey(platformId))
                    {
                        _playerLocations[platformId] = new PlayerLocationData
                        {
                            PlatformId = platformId,
                            CharacterName = characterName,
                            UserEntity = user,
                            CharacterEntity = character,
                            LastPosition = position,
                            CurrentZoneId = null,
                            LastZoneCheck = DateTime.UtcNow
                        };
                    }
                    else
                    {
                        var locationData = _playerLocations[platformId];
                        locationData.LastPosition = position;
                        locationData.CharacterEntity = character; // Update in case character changed
                    }

                    // Check for zone changes
                    CheckZoneChanges(platformId, position);
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[PlayerLocationTracker] Error updating player location: {ex.Message}");
            }
        }

        private static void CheckZoneChanges(ulong platformId, float3 position)
        {
            try
            {
                var playerLocation = _playerLocations[platformId];
                var currentZoneId = playerLocation.CurrentZoneId;
                var newZoneId = GetZoneAtPosition(position);

                // Zone change detected
                if (currentZoneId != newZoneId)
                {
                    var characterName = playerLocation.CharacterName;
                    
                    if (currentZoneId != null && newZoneId == null)
                    {
                        // Player exited a zone
                        _log?.LogInfo($"[PlayerLocationTracker] Player {characterName} exited zone {currentZoneId}");
                        TriggerPlayerExit(platformId, currentZoneId);
                    }
                    else if (currentZoneId == null && newZoneId != null)
                    {
                        // Player entered a zone
                        _log?.LogInfo($"[PlayerLocationTracker] Player {characterName} entered zone {newZoneId}");
                        TriggerPlayerEnter(platformId, newZoneId);
                    }
                    else if (currentZoneId != null && newZoneId != null && currentZoneId != newZoneId)
                    {
                        // Player moved from one zone to another
                        _log?.LogInfo($"[PlayerLocationTracker] Player {characterName} moved from zone {currentZoneId} to {newZoneId}");
                        TriggerPlayerExit(platformId, currentZoneId);
                        TriggerPlayerEnter(platformId, newZoneId);
                    }

                    // Update current zone
                    playerLocation.CurrentZoneId = newZoneId;
                    playerLocation.LastZoneChange = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[PlayerLocationTracker] Error checking zone changes: {ex.Message}");
            }
        }

        private static string GetZoneAtPosition(float3 position)
        {
            foreach (var zone in _configuredZones)
            {
                var distance = math.distance(position, zone.Center);
                if (distance <= zone.Radius)
                {
                    return zone.ArenaId;
                }
            }
            return null;
        }

        private static void TriggerPlayerEnter(ulong platformId, string arenaId)
        {
            try
            {
                var playerLocation = _playerLocations[platformId];
                
                // Trigger arena entry through lifecycle manager
                var success = ArenaLifecycleManager.OnPlayerEnter(
                    playerLocation.UserEntity, 
                    playerLocation.CharacterEntity, 
                    arenaId
                );

                if (success)
                {
                    _log?.LogInfo($"[PlayerLocationTracker] Successfully triggered arena entry for player {platformId} to {arenaId}");
                }
                else
                {
                    _log?.LogWarning($"[PlayerLocationTracker] Failed to trigger arena entry for player {platformId} to {arenaId}");
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[PlayerLocationTracker] Error triggering player enter: {ex.Message}");
            }
        }

        private static void TriggerPlayerExit(ulong platformId, string arenaId)
        {
            try
            {
                var playerLocation = _playerLocations[platformId];
                
                // Trigger arena exit through lifecycle manager
                var success = ArenaLifecycleManager.OnPlayerExit(
                    playerLocation.UserEntity, 
                    playerLocation.CharacterEntity, 
                    arenaId
                );

                if (success)
                {
                    _log?.LogInfo($"[PlayerLocationTracker] Successfully triggered arena exit for player {platformId} from {arenaId}");
                }
                else
                {
                    _log?.LogWarning($"[PlayerLocationTracker] Failed to trigger arena exit for player {platformId} from {arenaId}");
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[PlayerLocationTracker] Error triggering player exit: {ex.Message}");
            }
        }
        #endregion

        #region Helper Methods
        private static float3 GetEntityPosition(Entity entity)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                if (em.HasComponent<Translation>(entity))
                {
                    return em.GetComponentData<Translation>(entity).Value;
                }
                return float3.zero;
            }
            catch
            {
                return float3.zero;
            }
        }
        #endregion

        #region Query Methods
        public static List<string> GetConfiguredZoneNames()
        {
            lock (_lock)
            {
                return _configuredZones.Select(z => z.Name).ToList();
            }
        }

        public static List<ZoneConfig> GetAllZones()
        {
            lock (_lock)
            {
                return _configuredZones.ToList();
            }
        }

        public static PlayerLocationData GetPlayerLocation(ulong platformId)
        {
            lock (_lock)
            {
                return _playerLocations.TryGetValue(platformId, out var location) ? location : null;
            }
        }

        public static List<PlayerLocationData> GetAllPlayerLocations()
        {
            lock (_lock)
            {
                return _playerLocations.Values.ToList();
            }
        }

        public static string GetCurrentZoneForPlayer(ulong platformId)
        {
            lock (_lock)
            {
                return _playerLocations.TryGetValue(platformId, out var location) ? location.CurrentZoneId : null;
            }
        }

        public static List<PlayerLocationData> GetPlayersInZone(string arenaId)
        {
            lock (_lock)
            {
                return _playerLocations.Values.Where(p => p.CurrentZoneId == arenaId).ToList();
            }
        }

        public static int GetPlayerCountInZone(string arenaId)
        {
            lock (_lock)
            {
                return _playerLocations.Values.Count(p => p.CurrentZoneId == arenaId);
            }
        }

        public static bool IsPlayerInZone(ulong platformId, string arenaId)
        {
            lock (_lock)
            {
                return _playerLocations.TryGetValue(platformId, out var location) && location.CurrentZoneId == arenaId;
            }
        }
        #endregion

        #region Data Structures
        public class PlayerLocationData
        {
            public ulong PlatformId { get; set; }
            public string CharacterName { get; set; }
            public Entity UserEntity { get; set; }
            public Entity CharacterEntity { get; set; }
            public float3 LastPosition { get; set; }
            public string CurrentZoneId { get; set; }
            public DateTime LastZoneCheck { get; set; }
            public DateTime LastZoneChange { get; set; }
        }

        public class ZoneConfig
        {
            public string Name { get; set; }
            public float3 Center { get; set; }
            public float Radius { get; set; }
            public string ArenaId { get; set; }
            public bool AutoEnter { get; set; } = true;
            public bool AutoExit { get; set; } = true;
        }
        #endregion
    }
}