using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using VAuto.Core;

namespace VAuto.Services
{
    public class PlayerData
    {
        public Entity UserEntity { get; set; }
        public Entity CharacterEntity { get; set; }
        public ulong PlatformId { get; set; }
        public string CharacterName { get; set; }
        public bool IsOnline { get; set; }
        public float3 Position { get; set; }
        public string CurrentZone { get; set; }
        public DateTime LastSeen { get; set; }
        public PlayerState State { get; set; }
    }

    public enum PlayerState
    {
        Offline,
        Online,
        InArena,
        InBuildMode,
        Dead,
        Loading
    }

    public static class PlayerService
    {
        private static bool _initialized;
        private static readonly ConcurrentDictionary<ulong, PlayerData> _trackedPlayers = new();
        private static readonly ConcurrentDictionary<ulong, float3> _lastKnownPositions = new();

        public static bool IsInitialized => _initialized;
        public static BepInEx.Logging.ManualLogSource Log => Plugin.Logger;

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                _trackedPlayers.Clear();
                _lastKnownPositions.Clear();
                _initialized = true;

                Log?.LogInfo("[PlayerService] Player tracking service initialized");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[PlayerService] Failed to initialize: {ex.Message}");
            }
        }

        public static void Cleanup()
        {
            if (!_initialized) return;

            try
            {
                _trackedPlayers.Clear();
                _lastKnownPositions.Clear();
                _initialized = false;

                Log?.LogInfo("[PlayerService] Player tracking service cleaned up");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[PlayerService] Error during cleanup: {ex.Message}");
            }
        }

        public static void Update(float deltaTime)
        {
            if (!_initialized) return;

            try
            {
                // Update player positions and states
                UpdatePlayerTracking();
            }
            catch (Exception ex)
            {
                Log?.LogError($"[PlayerService] Error during update: {ex.Message}");
            }
        }

        private static void UpdatePlayerTracking()
        {
            try
            {
                var em = VAuto.Core.Core.EntityManager;
                if (em == null) return;

                var userQuery = em.CreateEntityQuery(ComponentType.ReadOnly<User>());
                if (userQuery.IsEmpty) return;

                var users = userQuery.ToEntityArray(Allocator.Temp);
                var currentOnlinePlayers = new HashSet<ulong>();

                try
                {
                    foreach (var userEntity in users)
                    {
                        if (!em.Exists(userEntity)) continue;
                        if (!em.TryGetComponentData(userEntity, out User user)) continue;

                        var platformId = user.PlatformId;
                        currentOnlinePlayers.Add(platformId);

                        Entity characterEntity = Entity.Null;
                        float3 position = float3.zero;
                        PlayerState state = PlayerState.Offline;

                        try
                        {
                            if (!user.LocalCharacter.Equals(default))
                                characterEntity = user.LocalCharacter.GetEntityOnServer();

                            if (characterEntity != Entity.Null && em.Exists(characterEntity))
                            {
                                // Get position
                                if (em.TryGetComponentData(characterEntity, out Unity.Transforms.Translation translation))
                                    position = translation.Value;

                                // Determine state
                                if (em.HasComponent<ProjectM.Dead>(characterEntity))
                                    state = PlayerState.Dead;
                                else if (IsPlayerInArena(platformId))
                                    state = PlayerState.InArena;
                                else if (IsPlayerInBuildMode(characterEntity))
                                    state = PlayerState.InBuildMode;
                                else
                                    state = PlayerState.Online;

                                // Update last known position
                                _lastKnownPositions[platformId] = position;
                            }
                            else
                            {
                                state = PlayerState.Loading;
                                // Keep last known position
                                _lastKnownPositions.TryGetValue(platformId, out position);
                            }
                        }
                        catch
                        {
                            state = PlayerState.Loading;
                            _lastKnownPositions.TryGetValue(platformId, out position);
                        }

                        // Update or create player data
                        var playerData = new PlayerData
                        {
                            UserEntity = userEntity,
                            CharacterEntity = characterEntity,
                            PlatformId = platformId,
                            CharacterName = user.CharacterName.ToString(),
                            IsOnline = characterEntity != Entity.Null && em.Exists(characterEntity),
                            Position = position,
                            CurrentZone = GetPlayerZone(platformId),
                            LastSeen = DateTime.UtcNow,
                            State = state
                        };

                        _trackedPlayers[platformId] = playerData;
                    }
                }
                finally
                {
                    users.Dispose();
                }

                // Mark offline players
                foreach (var kvp in _trackedPlayers)
                {
                    if (!currentOnlinePlayers.Contains(kvp.Key))
                    {
                        kvp.Value.IsOnline = false;
                        kvp.Value.State = PlayerState.Offline;
                        kvp.Value.LastSeen = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[PlayerService] Error updating player tracking: {ex.Message}");
            }
        }

        public static List<PlayerData> GetAllOnlinePlayers()
        {
            return _trackedPlayers.Values.Where(p => p.IsOnline).ToList();
        }

        public static List<PlayerData> GetAllTrackedPlayers()
        {
            return _trackedPlayers.Values.ToList();
        }

        public static PlayerData GetPlayerData(ulong platformId)
        {
            return _trackedPlayers.TryGetValue(platformId, out var data) ? data : null;
        }

        public static int GetOnlinePlayerCount()
        {
            return _trackedPlayers.Count(p => p.Value.IsOnline);
        }

        public static int GetTrackedPlayerCount()
        {
            return _trackedPlayers.Count;
        }

        public static bool IsPlayerOnline(ulong platformId)
        {
            return _trackedPlayers.TryGetValue(platformId, out var data) && data.IsOnline;
        }

        public static bool IsPlayerInArena(ulong platformId)
        {
            // Check with ArenaZoneService
            return VAuto.Services.Systems.ArenaZoneService.Instance.IsPlayerInAnyArena(platformId);
        }

        public static bool IsPlayerInBuildMode(Entity characterEntity)
        {
            // TODO: Implement proper build mode detection
            // For now, return false - build mode detection needs proper component identification
            return false;
        }

        public static string GetPlayerZone(ulong platformId)
        {
            // Check arena zones first
            if (IsPlayerInArena(platformId))
            {
                return "Arena";
            }

            // Could extend to check other zones (castles, etc.)
            return "Overworld";
        }

        public static float3 GetPlayerPosition(ulong platformId)
        {
            if (_trackedPlayers.TryGetValue(platformId, out var data))
                return data.Position;

            return _lastKnownPositions.TryGetValue(platformId, out var pos) ? pos : float3.zero;
        }

        public static PlayerState GetPlayerState(ulong platformId)
        {
            return _trackedPlayers.TryGetValue(platformId, out var data) ? data.State : PlayerState.Offline;
        }

        public static void TrackPlayer(ulong platformId, string reason = "")
        {
            Log?.LogInfo($"[PlayerService] Started tracking player {platformId}: {reason}");

            // Force update of this player's data
            UpdatePlayerTracking();
        }

        public static void StopTrackingPlayer(ulong platformId, string reason = "")
        {
            if (_trackedPlayers.TryRemove(platformId, out _))
            {
                _lastKnownPositions.TryRemove(platformId, out _);
                Log?.LogInfo($"[PlayerService] Stopped tracking player {platformId}: {reason}");
            }
        }

        public static List<PlayerData> GetPlayersInZone(string zoneName)
        {
            return _trackedPlayers.Values.Where(p => p.CurrentZone == zoneName).ToList();
        }

        public static List<PlayerData> GetPlayersByState(PlayerState state)
        {
            return _trackedPlayers.Values.Where(p => p.State == state).ToList();
        }
    }
}
