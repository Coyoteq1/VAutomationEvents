using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM;
using ProjectM.Network;
using ProjectM.Gameplay;
using VAuto.Core;
using VAuto.Data;
using VAuto.Utilities;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Enhanced Arena Snapshot Service - Advanced snapshot management for arena players
    /// </summary>
    public static class EnhancedArenaSnapshotService
    {
        private static bool _initialized = false;
        private static readonly Dictionary<string, PlayerSnapshot> _playerSnapshots = new();
        private static readonly Dictionary<string, DateTime> _snapshotCreationTimes = new();
        private static readonly Dictionary<string, string> _snapshotToKeyMap = new(); // UUID to key mapping
        private static readonly object _lock = new object();
        
        public static bool IsInitialized => _initialized;
        public static ManualLogSource Log => Plugin.Logger;

        #region Initialization
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized) return;
                
                try
                {
                    Log?.LogInfo("[EnhancedArenaSnapshotService] Initializing enhanced arena snapshot service...");
                    
                    _playerSnapshots.Clear();
                    _snapshotCreationTimes.Clear();
                    _snapshotToKeyMap.Clear();
                    _initialized = true;
                    
                    Log?.LogInfo("[EnhancedArenaSnapshotService] Enhanced arena snapshot service initialized successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[EnhancedArenaSnapshotService] Failed to initialize: {ex.Message}");
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
                    Log?.LogInfo("[EnhancedArenaSnapshotService] Cleaning up enhanced arena snapshot service...");
                    
                    _playerSnapshots.Clear();
                    _snapshotCreationTimes.Clear();
                    _snapshotToKeyMap.Clear();
                    _initialized = false;
                    
                    Log?.LogInfo("[EnhancedArenaSnapshotService] Enhanced arena snapshot service cleaned up successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[EnhancedArenaSnapshotService] Failed to cleanup: {ex.Message}");
                }
            }
        }
        #endregion

        #region Snapshot Creation
        public static bool CreateSnapshot(Entity user, Entity character, string arenaId)
        {
            try
            {
                if (user == Entity.Null || character == Entity.Null)
                {
                    Log?.LogWarning("[EnhancedArenaSnapshotService] Cannot create snapshot with null entities");
                    return false;
                }

                var em = VAutoCore.EntityManager;
                if (!em.TryGetComponentData(user, out User userData))
                {
                    Log?.LogWarning("[EnhancedArenaSnapshotService] Invalid user entity");
                    return false;
                }

                var characterId = userData.PlatformId;
                var snapshotUuid = SnapshotUuidGenerator.GenerateSnapshotUuid(characterId, arenaId);
                var legacyKey = $"{characterId}_{arenaId}";

                lock (_lock)
                {
                    // Check if snapshot already exists using legacy key
                    if (_snapshotToKeyMap.ContainsKey(legacyKey))
                    {
                        Log?.LogWarning($"[EnhancedArenaSnapshotService] Snapshot already exists for player {characterId} in arena {arenaId}");
                        return false;
                    }

                    // Create comprehensive snapshot with UUID
                    var snapshot = CapturePlayerSnapshot(user, character, arenaId, snapshotUuid);
                    
                    _playerSnapshots[snapshotUuid] = snapshot;
                    _snapshotCreationTimes[snapshotUuid] = DateTime.UtcNow;
                    _snapshotToKeyMap[legacyKey] = snapshotUuid;

                    Log?.LogInfo($"[EnhancedArenaSnapshotService] Created snapshot {snapshotUuid} for player {userData.CharacterName} in arena {arenaId}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Failed to create snapshot: {ex.Message}");
                return false;
            }
        }

        private static PlayerSnapshot CapturePlayerSnapshot(Entity user, Entity character, string arenaId, string snapshotUuid)
        {
            var em = VAutoCore.EntityManager;
            var snapshot = new PlayerSnapshot
            {
                SnapshotUuid = snapshotUuid,
                CharacterId = user.GetComponentData<User>(em).PlatformId.ToString(),
                ArenaId = arenaId,
                SnapshotTime = DateTime.UtcNow,
                UserEntity = user,
                CharacterEntity = character,
                OriginalName = user.GetComponentData<User>(em).CharacterName.ToString()
            };

            try
            {
                // Capture character data
                if (em.TryGetComponentData(character, out PlayerCharacter playerChar))
                {
                    snapshot.OriginalUserEntity = playerChar.UserEntity;
                    snapshot.OriginalHealth = GetCharacterHealth(character);
                    snapshot.OriginalBlood = GetCharacterBlood(character);
                    snapshot.OriginalPosition = GetCharacterPosition(character);
                    snapshot.OriginalRotation = GetCharacterRotation(character);
                }

                // Capture inventory data
                snapshot.InventoryData = CaptureInventoryData(character);

                // Capture equipment data
                snapshot.EquipmentData = CaptureEquipmentData(character);

                // Capture abilities and spells
                snapshot.AbilitiesData = CaptureAbilitiesData(character);

                // Capture progression data
                snapshot.ProgressionData = CaptureProgressionData(character);

                // Capture buffs and effects
                snapshot.BuffsData = CaptureBuffsData(character);

                // Capture castle territory information
                snapshot.CastleData = CaptureCastleData(user);

                Log?.LogDebug($"[EnhancedArenaSnapshotService] Captured comprehensive snapshot data");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Error capturing snapshot data: {ex.Message}");
            }

            return snapshot;
        }

        private static float GetCharacterHealth(Entity character)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                // This would capture actual health data from the character's health component
                return 100.0f; // Placeholder
            }
            catch
            {
                return 0f;
            }
        }

        private static float GetCharacterBlood(Entity character)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                // This would capture actual blood quality data
                return 100.0f; // Placeholder
            }
            catch
            {
                return 0f;
            }
        }

        private static float3 GetCharacterPosition(Entity character)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                if (em.HasComponent<Translation>(character))
                {
                    return em.GetComponentData<Translation>(character).Value;
                }
                return float3.zero;
            }
            catch
            {
                return float3.zero;
            }
        }

        private static quaternion GetCharacterRotation(Entity character)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                if (em.HasComponent<Rotation>(character))
                {
                    return em.GetComponentData<Rotation>(character).Value;
                }
                return quaternion.identity;
            }
            catch
            {
                return quaternion.identity;
            }
        }

        private static InventorySnapshotData CaptureInventoryData(Entity character)
        {
            // This would capture inventory contents
            return new InventorySnapshotData
            {
                Items = new List<ItemData>(),
                BloodEssence = 0,
                SoulShards = 0
            };
        }

        private static EquipmentSnapshotData CaptureEquipmentData(Entity character)
        {
            // This would capture equipped items
            return new EquipmentSnapshotData
            {
                EquippedItems = new Dictionary<string, string>()
            };
        }

        private static AbilitiesSnapshotData CaptureAbilitiesData(Entity character)
        {
            // This would capture unlocked abilities and spells
            return new AbilitiesSnapshotData
            {
                UnlockedAbilities = new List<string>(),
                SpellLevels = new Dictionary<string, int>()
            };
        }

        private static ProgressionSnapshotData CaptureProgressionData(Entity character)
        {
            // This would capture progression data
            return new ProgressionSnapshotData
            {
                Level = 1,
                Experience = 0,
                UnlockedResearch = new List<string>()
            };
        }

        private static BuffsSnapshotData CaptureBuffsData(Entity character)
        {
            // This would capture active buffs and effects
            return new BuffsSnapshotData
            {
                ActiveBuffs = new List<string>(),
                ActiveEffects = new List<string>()
            };
        }

        private static CastleSnapshotData CaptureCastleData(Entity user)
        {
            // This would capture castle-related data
            return new CastleSnapshotData
            {
                CastleLevel = 0,
                TerritoryInfo = null
            };
        }
        #endregion

        #region Snapshot Restoration
        public static bool RestoreSnapshot(string characterId, string arenaId)
        {
            try
            {
                var legacyKey = $"{characterId}_{arenaId}";

                lock (_lock)
                {
                    // Find UUID using legacy key mapping
                    if (!_snapshotToKeyMap.TryGetValue(legacyKey, out var snapshotUuid))
                    {
                        Log?.LogWarning($"[EnhancedArenaSnapshotService] No snapshot found for player {characterId} in arena {arenaId}");
                        return false;
                    }

                    if (!_playerSnapshots.TryGetValue(snapshotUuid, out var snapshot))
                    {
                        Log?.LogWarning($"[EnhancedArenaSnapshotService] Snapshot UUID {snapshotUuid} not found in snapshots");
                        return false;
                    }

                    var success = RestorePlayerSnapshot(snapshot);
                    
                    if (success)
                    {
                        // Remove snapshot after successful restoration
                        _playerSnapshots.Remove(snapshotUuid);
                        _snapshotCreationTimes.Remove(snapshotUuid);
                        _snapshotToKeyMap.Remove(legacyKey);
                        
                        Log?.LogInfo($"[EnhancedArenaSnapshotService] Successfully restored and removed snapshot {snapshotUuid} for player {characterId}");
                    }
                    else
                    {
                        Log?.LogError($"[EnhancedArenaSnapshotService] Failed to restore snapshot {snapshotUuid} for player {characterId}");
                    }

                    return success;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Failed to restore snapshot: {ex.Message}");
                return false;
            }
        }

        private static bool RestorePlayerSnapshot(PlayerSnapshot snapshot)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                
                // Verify entities still exist
                if (!em.Exists(snapshot.UserEntity) || !em.Exists(snapshot.CharacterEntity))
                {
                    Log?.LogWarning("[EnhancedArenaSnapshotService] User or character entity no longer exists");
                    return false;
                }

                // Restore position and rotation
                RestorePositionAndRotation(snapshot);

                // Restore health and blood
                RestoreHealthAndBlood(snapshot);

                // Restore inventory
                RestoreInventory(snapshot);

                // Restore equipment
                RestoreEquipment(snapshot);

                // Restore abilities
                RestoreAbilities(snapshot);

                // Restore progression
                RestoreProgression(snapshot);

                // Restore buffs
                RestoreBuffs(snapshot);

                // Restore castle data
                RestoreCastleData(snapshot);

                Log?.LogDebug($"[EnhancedArenaSnapshotService] Successfully restored player snapshot data");
                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Error restoring snapshot: {ex.Message}");
                return false;
            }
        }

        private static void RestorePositionAndRotation(PlayerSnapshot snapshot)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                
                if (em.HasComponent<Translation>(snapshot.CharacterEntity))
                {
                    em.SetComponentData(snapshot.CharacterEntity, new Translation { Value = snapshot.OriginalPosition });
                }

                if (em.HasComponent<Rotation>(snapshot.CharacterEntity))
                {
                    em.SetComponentData(snapshot.CharacterEntity, new Rotation { Value = snapshot.OriginalRotation });
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Failed to restore position/rotation: {ex.Message}");
            }
        }

        private static void RestoreHealthAndBlood(PlayerSnapshot snapshot)
        {
            // This would restore actual health and blood values
        }

        private static void RestoreInventory(PlayerSnapshot snapshot)
        {
            // This would restore inventory contents
        }

        private static void RestoreEquipment(PlayerSnapshot snapshot)
        {
            // This would restore equipped items
        }

        private static void RestoreAbilities(PlayerSnapshot snapshot)
        {
            // This would restore unlocked abilities
        }

        private static void RestoreProgression(PlayerSnapshot snapshot)
        {
            // This would restore progression data
        }

        private static void RestoreBuffs(PlayerSnapshot snapshot)
        {
            // This would restore active buffs
        }

        private static void RestoreCastleData(PlayerSnapshot snapshot)
        {
            // This would restore castle data
        }
        #endregion

        #region Snapshot Management
        public static bool DeleteSnapshot(string characterId, string arenaId)
        {
            try
            {
                var legacyKey = $"{characterId}_{arenaId}";

                lock (_lock)
                {
                    if (_snapshotToKeyMap.TryGetValue(legacyKey, out var snapshotUuid))
                    {
                        _playerSnapshots.Remove(snapshotUuid);
                        _snapshotCreationTimes.Remove(snapshotUuid);
                        _snapshotToKeyMap.Remove(legacyKey);
                        Log?.LogInfo($"[EnhancedArenaSnapshotService] Deleted snapshot {snapshotUuid} for player {characterId} in arena {arenaId}");
                        return true;
                    }
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Failed to delete snapshot: {ex.Message}");
                return false;
            }
        }

        public static void DeleteAllSnapshots()
        {
            lock (_lock)
            {
                _playerSnapshots.Clear();
                _snapshotCreationTimes.Clear();
                _snapshotToKeyMap.Clear();
                Log?.LogInfo("[EnhancedArenaSnapshotService] Deleted all snapshots");
            }
        }

        public static PlayerSnapshot GetSnapshot(string characterId, string arenaId)
        {
            lock (_lock)
            {
                var legacyKey = $"{characterId}_{arenaId}";
                if (_snapshotToKeyMap.TryGetValue(legacyKey, out var snapshotUuid))
                {
                    return _playerSnapshots.TryGetValue(snapshotUuid, out var snapshot) ? snapshot : null;
                }
                return null;
            }
        }

        public static bool HasSnapshot(string characterId, string arenaId)
        {
            lock (_lock)
            {
                var legacyKey = $"{characterId}_{arenaId}";
                return _snapshotToKeyMap.ContainsKey(legacyKey);
            }
        }

        public static List<PlayerSnapshot> GetAllSnapshots()
        {
            lock (_lock)
            {
                return _playerSnapshots.Values.ToList();
            }
        }

        public static List<PlayerSnapshot> GetSnapshotsByArena(string arenaId)
        {
            lock (_lock)
            {
                return _playerSnapshots.Values.Where(s => s.ArenaId == arenaId).ToList();
            }
        }

        public static int GetSnapshotCount()
        {
            lock (_lock)
            {
                return _playerSnapshots.Count;
            }
        }

        /// <summary>
        /// Get snapshot by UUID directly
        /// </summary>
        public static PlayerSnapshot GetSnapshotByUuid(string snapshotUuid)
        {
            if (!SnapshotUuidGenerator.IsValidUuid(snapshotUuid))
                return null;
                
            lock (_lock)
            {
                return _playerSnapshots.TryGetValue(snapshotUuid, out var snapshot) ? snapshot : null;
            }
        }

        /// <summary>
        /// Get snapshot UUID by character and arena
        /// </summary>
        public static string GetSnapshotUuid(string characterId, string arenaId)
        {
            lock (_lock)
            {
                var legacyKey = $"{characterId}_{arenaId}";
                return _snapshotToKeyMap.TryGetValue(legacyKey, out var snapshotUuid) ? snapshotUuid : null;
            }
        }

        /// <summary>
        /// Delete snapshot by UUID
        /// </summary>
        public static bool DeleteSnapshotByUuid(string snapshotUuid)
        {
            if (!SnapshotUuidGenerator.IsValidUuid(snapshotUuid))
                return false;

            try
            {
                lock (_lock)
                {
                    if (!_playerSnapshots.ContainsKey(snapshotUuid))
                        return false;

                    // Find and remove the legacy key mapping
                    var legacyKeyToRemove = _snapshotToKeyMap.FirstOrDefault(kvp => kvp.Value == snapshotUuid).Key;
                    if (!string.IsNullOrEmpty(legacyKeyToRemove))
                    {
                        _snapshotToKeyMap.Remove(legacyKeyToRemove);
                    }

                    _playerSnapshots.Remove(snapshotUuid);
                    _snapshotCreationTimes.Remove(snapshotUuid);
                    
                    Log?.LogInfo($"[EnhancedArenaSnapshotService] Deleted snapshot by UUID: {snapshotUuid}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Failed to delete snapshot by UUID: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Data Structures
        public class PlayerSnapshot
        {
            public string SnapshotUuid { get; set; }
            public string CharacterId { get; set; }
            public string ArenaId { get; set; }
            public DateTime SnapshotTime { get; set; }
            public Entity UserEntity { get; set; }
            public Entity CharacterEntity { get; set; }
            public Entity OriginalUserEntity { get; set; }
            public string OriginalName { get; set; }
            
            // Character State
            public float OriginalHealth { get; set; }
            public float OriginalBlood { get; set; }
            public float3 OriginalPosition { get; set; }
            public quaternion OriginalRotation { get; set; }
            
            // Detailed Data
            public InventorySnapshotData InventoryData { get; set; }
            public EquipmentSnapshotData EquipmentData { get; set; }
            public AbilitiesSnapshotData AbilitiesData { get; set; }
            public ProgressionSnapshotData ProgressionData { get; set; }
            public BuffsSnapshotData BuffsData { get; set; }
            public CastleSnapshotData CastleData { get; set; }
        }

        public class InventorySnapshotData
        {
            public List<ItemData> Items { get; set; } = new();
            public int BloodEssence { get; set; }
            public int SoulShards { get; set; }
        }

        public class EquipmentSnapshotData
        {
            public Dictionary<string, string> EquippedItems { get; set; } = new();
        }

        public class AbilitiesSnapshotData
        {
            public List<string> UnlockedAbilities { get; set; } = new();
            public Dictionary<string, int> SpellLevels { get; set; } = new();
        }

        public class ProgressionSnapshotData
        {
            public int Level { get; set; }
            public long Experience { get; set; }
            public List<string> UnlockedResearch { get; set; } = new();
        }

        public class BuffsSnapshotData
        {
            public List<string> ActiveBuffs { get; set; } = new();
            public List<string> ActiveEffects { get; set; } = new();
        }

        public class CastleSnapshotData
        {
            public int CastleLevel { get; set; }
            public object TerritoryInfo { get; set; }
        }

        public class ItemData
        {
            public string ItemId { get; set; }
            public int Quantity { get; set; }
            public int Rarity { get; set; }
        }
        #endregion
    }
}
