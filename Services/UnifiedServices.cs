using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using VAuto.Core;
using VAuto.Data;
using VAuto.Extensions;
using BepInEx.Logging;

namespace VAuto.Services
{
    /// <summary>
    /// Unified Service Manager - Single entry point for all services
    /// </summary>
    public static class UnifiedServiceManager
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        #region Properties
        public static bool IsInitialized => _initialized;
        public static ManualLogSource Log => Plugin.Logger;
        #endregion

        #region Initialization
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized) return;

                try
                {
                    Log?.LogInfo("[UnifiedServices] Initializing unified service manager...");

                    // Initialize all services
                    PlayerService.Initialize();
                    MapIconService.Initialize();
                    GameSystems.Initialize();
                    RespawnPreventionService.Initialize();
                    NameTagService.Initialize();
                    PlayerProgressStore.Initialize();

                    _initialized = true;
                    Log?.LogInfo("[UnifiedServices] Unified service manager initialized successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[UnifiedServices] Failed to initialize: {ex.Message}");
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
                    Log?.LogInfo("[UnifiedServices] Cleaning up unified service manager...");

                    // Cleanup all services
                    NameTagService.Cleanup();
                    RespawnPreventionService.Cleanup();
                    GameSystems.Cleanup();
                    MapIconService.Cleanup();
                    PlayerService.Cleanup();
                    PlayerProgressStore.Cleanup();

                    _initialized = false;
                    Log?.LogInfo("[UnifiedServices] Unified service manager cleaned up successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[UnifiedServices] Failed to cleanup: {ex.Message}");
                }
            }
        }
        #endregion

        #region Service Status
        public static Dictionary<string, object> GetServiceStatus()
        {
            return new Dictionary<string, object>
            {
                ["PlayerService"] = new { Status = PlayerService.IsInitialized ? "Running" : "Stopped", Players = PlayerService.GetAllOnlinePlayers().Count },
                ["MapIconService"] = new { Status = MapIconService.IsInitialized ? "Running" : "Stopped", Icons = MapIconService.GetActiveIconCount() },
                ["GameSystems"] = new { Status = GameSystems.IsInitialized ? "Running" : "Stopped", Hooks = GameSystems.GetActiveHookedPlayers().Count },
                ["RespawnPrevention"] = new { Status = RespawnPreventionService.IsInitialized ? "Running" : "Stopped", Cooldowns = RespawnPreventionService.GetActiveCooldownCount() },
                ["NameTagService"] = new { Status = NameTagService.IsInitialized ? "Running" : "Stopped", Tags = NameTagService.GetActiveTagCount() },
                ["PlayerProgressStore"] = new { Status = PlayerProgressStore.IsInitialized ? "Running" : "Stopped", CachedPlayers = PlayerProgressStore.GetCachedPlayerCount() }
            };
        }

        public static string GetServiceStatusString()
        {
            var status = GetServiceStatus();
            var result = "=== VAuto Service Status ===\n";
            
            foreach (var service in status)
            {
                result += $"• {service.Key}: {service.Value}\n";
            }
            
            return result;
        }
        #endregion
    }

    /// <summary>
    /// Unified Player Service - Player management and tracking
    /// </summary>
    public static class PlayerService
    {
        private static bool _initialized = false;
        private static readonly Dictionary<ulong, PlayerData> _players = new();

        public static bool IsInitialized => _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            
            _players.Clear();
            _initialized = true;
            Plugin.Logger?.LogInfo("[PlayerService] Initialized");
        }

        public static void Cleanup()
        {
            if (!_initialized) return;
            
            _players.Clear();
            _initialized = false;
            Plugin.Logger?.LogInfo("[PlayerService] Cleaned up");
        }

        public static List<UserData> GetAllOnlinePlayers()
        {
            var result = new List<UserData>();

            try
            {
                var em = VAutoCore.EntityManager;
                if (em == null)
                {
                    Plugin.Logger?.LogWarning("[PlayerService] EntityManager is null");
                    return result;
                }

                var userQuery = em.CreateEntityQuery(ComponentType.ReadOnly<User>());
                if (userQuery.IsEmpty)
                {
                    Plugin.Logger?.LogDebug("[PlayerService] No user entities found");
                    return result;
                }

                var userEntities = userQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

                foreach (var userEntity in userEntities)
                {
                    try
                    {
                        if (!em.Exists(userEntity))
                        {
                            Plugin.Logger?.LogDebug($"[PlayerService] User entity {userEntity} does not exist, skipping");
                            continue;
                        }

                        if (em.TryGetComponentData(userEntity, out User user))
                        {
                            Entity characterEntity = Entity.Null;

                            // Safely get character entity - try different methods
                            try
                            {
                                if (!user.LocalCharacter.Equals(default))
                                {
                                    // Try the GetEntityOnServer method if it exists
                                    characterEntity = user.LocalCharacter.GetEntityOnServer();
                                }
                            }
                            catch (Exception methodEx)
                            {
                                // If GetEntityOnServer doesn't exist, try alternative approaches
                                Plugin.Logger?.LogDebug($"[PlayerService] GetEntityOnServer method not available: {methodEx.Message}");

                                // Try to find character entity through other means
                                try
                                {
                                    var characterQuery = em.CreateEntityQuery(ComponentType.ReadOnly<PlayerCharacter>());
                                    var characterEntities = characterQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

                                    foreach (var charEnt in characterEntities)
                                    {
                                        if (em.TryGetComponentData(charEnt, out PlayerCharacter playerChar) &&
                                            playerChar.UserEntity == userEntity)
                                        {
                                            characterEntity = charEnt;
                                            break;
                                        }
                                    }

                                    characterEntities.Dispose();
                                }
                                catch (Exception charEx)
                                {
                                    Plugin.Logger?.LogDebug($"[PlayerService] Could not find character entity: {charEx.Message}");
                                }
                            }

                            // Validate the character entity
                            bool isCharacterValid = characterEntity != Entity.Null &&
                                                   em.Exists(characterEntity) &&
                                                   em.HasComponent<PlayerCharacter>(characterEntity);

                            result.Add(new UserData
                            {
                                UserEntity = userEntity,
                                CharacterEntity = characterEntity,
                                PlatformId = user.PlatformId,
                                CharacterName = user.CharacterName.ToString(),
                                IsOnline = isCharacterValid
                            });
                        }
                    }
                    catch (Exception userEx)
                    {
                        Plugin.Logger?.LogDebug($"[PlayerService] Error processing user entity {userEntity}: {userEx.Message}");
                        continue;
                    }
                }

                userEntities.Dispose();
                Plugin.Logger?.LogDebug($"[PlayerService] Found {result.Count} online players");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[PlayerService] Error getting online players: {ex.Message}");
                Plugin.Logger?.LogError($"[PlayerService] Stack trace: {ex.StackTrace}");
            }

            return result;
        }

        public static UserData GetPlayer(ulong platformId)
        {
            return GetAllOnlinePlayers().FirstOrDefault(p => p.PlatformId == platformId);
        }

        public static int GetOnlinePlayerCount()
        {
            return GetAllOnlinePlayers().Count;
        }

        public static UserData GetPlayer(string characterName)
        {
            return GetAllOnlinePlayers().FirstOrDefault(p => 
                p.CharacterName.Equals(characterName, StringComparison.OrdinalIgnoreCase));
        }
    }



    /// <summary>
    /// Unified Game Systems Service - Game system hooks and management
    /// </summary>
    public static class GameSystems
    {
        private static bool _initialized = false;
        private static readonly HashSet<ulong> _hookedPlayers = new();

        public static bool IsInitialized => _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            
            _hookedPlayers.Clear();
            _initialized = true;
            Plugin.Logger?.LogInfo("[GameSystems] Initialized");
        }

        public static void Cleanup()
        {
            if (!_initialized) return;
            
            ClearAllHooks();
            _initialized = false;
            Plugin.Logger?.LogInfo("[GameSystems] Cleaned up");
        }

        public static bool IsPlayerInArena(ulong platformId) => _hookedPlayers.Contains(platformId);

        public static void MarkPlayerEnteredArena(ulong platformId) => _hookedPlayers.Add(platformId);

        public static void MarkPlayerExitedArena(ulong platformId) => _hookedPlayers.Remove(platformId);

        public static List<ulong> GetActiveHookedPlayers() => _hookedPlayers.ToList();

        public static void ClearAllHooks() => _hookedPlayers.Clear();
    }

    /// <summary>
    /// Unified Respawn Prevention Service - Respawn cooldown management
    /// </summary>
    public static class RespawnPreventionService
    {
        private static bool _initialized = false;
        private static readonly Dictionary<ulong, CooldownData> _cooldowns = new();

        public static bool IsInitialized => _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            
            _cooldowns.Clear();
            _initialized = true;
            Plugin.Logger?.LogInfo("[RespawnPreventionService] Initialized");
        }

        public static void Cleanup()
        {
            if (!_initialized) return;
            
            _cooldowns.Clear();
            _initialized = false;
            Plugin.Logger?.LogInfo("[RespawnPreventionService] Cleaned up");
        }

        public static bool IsPlayerOnCooldown(ulong platformId)
        {
            return _cooldowns.TryGetValue(platformId, out var cooldown) && 
                   cooldown.ExpiryTime > DateTime.UtcNow;
        }

        public static void SetCooldown(ulong platformId, int seconds)
        {
            _cooldowns[platformId] = new CooldownData
            {
                PlatformId = platformId,
                ExpiryTime = DateTime.UtcNow.AddSeconds(seconds)
            };
        }

        public static int GetActiveCooldownCount() => _cooldowns.Count;

        public static void CleanupExpiredCooldowns()
        {
            var expired = _cooldowns.Where(kvp => kvp.Value.ExpiryTime <= DateTime.UtcNow)
                                   .Select(kvp => kvp.Key)
                                   .ToList();
            
            foreach (var platformId in expired)
            {
                _cooldowns.Remove(platformId);
            }
        }

        // Additional methods for command compatibility
        public static void SetRespawnCooldown(ulong platformId, int duration)
        {
            SetCooldown(platformId, duration);
        }

        public static void ClearRespawnCooldown(ulong platformId)
        {
            _cooldowns.Remove(platformId);
        }

        public static bool IsRespawnPrevented(ulong platformId)
        {
            return IsPlayerOnCooldown(platformId);
        }
    }

    /// <summary>
    /// Unified Name Tag Service - Player name tag management
    /// </summary>
    public static class NameTagService
    {
        private static bool _initialized = false;
        private static readonly Dictionary<ulong, string> _tags = new();

        public static bool IsInitialized => _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            
            _tags.Clear();
            _initialized = true;
            Plugin.Logger?.LogInfo("[NameTagService] Initialized");
        }

        public static void Cleanup()
        {
            if (!_initialized) return;
            
            _tags.Clear();
            _initialized = false;
            Plugin.Logger?.LogInfo("[NameTagService] Cleaned up");
        }

        public static void SetPlayerTag(ulong platformId, string tag)
        {
            _tags[platformId] = tag;
        }

        public static string GetPlayerTag(ulong platformId)
        {
            return _tags.TryGetValue(platformId, out var tag) ? tag : null;
        }

        public static int GetActiveTagCount() => _tags.Count;
    }

    #region Data Structures
    public class PlayerData
    {
        public Entity UserEntity { get; set; }
        public Entity CharacterEntity { get; set; }
        public ulong PlatformId { get; set; }
        public string CharacterName { get; set; }
        public bool IsOnline { get; set; }
    }

    public class UserData : PlayerData
    {
        // Same as PlayerData but named for clarity
    }



    public class CooldownData
    {
        public ulong PlatformId { get; set; }
        public DateTime ExpiryTime { get; set; }
    }
    #endregion
}
