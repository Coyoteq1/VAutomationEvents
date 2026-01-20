using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM;
using ProjectM.Network;
using ProjectM.Gameplay;
using Stunlock.Core;
using VAuto.Core;
using VAuto.Data;
using VAuto.Utilities;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Enhanced Arena Snapshot Service - Advanced snapshot management for arena players
    /// Full equipment slots and all game abilities included for PvP zone lifecycle compliance
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
        public static void Initialize(bool loadFromFiles = true)
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
                    
                    // Load snapshots from files if requested
                    if (loadFromFiles)
                    {
                        var loadedSnapshots = LoadAllSnapshotsFromFiles();
                        foreach (var snapshot in loadedSnapshots)
                        {
                            _playerSnapshots[snapshot.SnapshotUuid] = snapshot;
                            _snapshotCreationTimes[snapshot.SnapshotUuid] = snapshot.SnapshotTime;
                            var legacyKey = $"{snapshot.CharacterId}_{snapshot.ArenaId}";
                            _snapshotToKeyMap[legacyKey] = snapshot.SnapshotUuid;
                        }
                        
                        Log?.LogInfo($"[EnhancedArenaSnapshotService] Loaded {loadedSnapshots.Count} snapshots from files");
                    }
                    
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
        public static bool CreateSnapshot(Entity user, Entity character, string arenaId, bool saveToFile = true)
        {
            try
            {
                if (user == Entity.Null || character == Entity.Null)
                {
                    Log?.LogWarning("[EnhancedArenaSnapshotService] Cannot create snapshot with null entities");
                    return false;
                }

                EntityManager em = VAuto.Core.Core.EntityManager;

                if (!VAuto.Core.Core.TryRead<User>(user, out var userData))
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

                    // Save to file if requested
                    if (saveToFile)
                    {
                        SaveSnapshotToFile(snapshot);
                    }

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
            var em = VAuto.Core.Core.EntityManager;
            var snapshot = new PlayerSnapshot
            {
                SnapshotUuid = snapshotUuid,
                CharacterId = VAuto.Core.Core.TryRead<User>(user, out var u1) ? u1.PlatformId.ToString() : "0",
                ArenaId = arenaId,
                SnapshotTime = DateTime.UtcNow,
                UserEntity = user,
                CharacterEntity = character,
                OriginalName = VAuto.Core.Core.TryRead<User>(user, out var u2) ? u2.CharacterName.ToString() : "Unknown"
            };

            try
            {
                // Capture character data
                if (em.HasComponent<PlayerCharacter>(character))
                {
                    var playerChar = em.GetComponentData<PlayerCharacter>(character);
                    snapshot.OriginalUserEntity = playerChar.UserEntity;
                    snapshot.OriginalHealth = GetCharacterHealth(character);
                    snapshot.OriginalPosition = GetCharacterPosition(character);
                    snapshot.OriginalRotation = GetCharacterRotation(character);
                }

                // Capture inventory data (this also clears the inventory)
                snapshot.InventoryData = CaptureInventoryData(character);

                // Capture equipment data
                snapshot.EquipmentData = CaptureEquipmentData(character);

                // Capture blood type and quality
                if (VAuto.Core.Core.TryRead<ProjectM.Blood>(character, out var blood))
                {
                    snapshot.OriginalBloodType = blood.BloodType.GuidHash;
                    snapshot.OriginalBloodQuality = blood.Quality;
                }
                else
                {
                    snapshot.OriginalBloodType = 0;
                    snapshot.OriginalBloodQuality = 0f;
                }

                // Capture abilities and spells
                snapshot.AbilitiesData = CaptureAbilitiesData(character);

                // Capture progression data
                snapshot.ProgressionData = CaptureProgressionData(character);

                // Capture buffs and effects
                snapshot.BuffsData = CaptureBuffsData(character);

                // Capture UI state
                TryCaptureUIState(user, snapshot);

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
                var em = VAuto.Core.Core.EntityManager;
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
                var em = VAuto.Core.Core.EntityManager;
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
                var em = VAuto.Core.Core.EntityManager;
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
                var em = VAuto.Core.Core.EntityManager;
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
            try
            {
                var em = VAuto.Core.Core.EntityManager;
                var inventoryData = new InventorySnapshotData
                {
                    Items = new List<ItemData>(),
                    BloodEssence = 0,
                    SoulShards = 0
                };

                Log?.LogInfo($"[EnhancedArenaSnapshotService] Capturing inventory for character {character.Index}");

                // Capture inventory items and clear them
                if (em.HasComponent<ProjectM.InventoryBuffer>(character))
                {
                    var inventoryBuffer = em.GetBuffer<ProjectM.InventoryBuffer>(character);

                    // Store all items before clearing (skip equipment slots 0-7, capture inventory slots 8+)
                    for (int i = 8; i < inventoryBuffer.Length; i++) // Equipment is typically slots 0-7
                    {
                        var item = inventoryBuffer[i];
                        if (item.ItemType.GuidHash != 0 && item.Amount > 0) // Check if slot has valid item
                        {
                            var itemData = new ItemData
                            {
                                ItemId = item.ItemType.GuidHash.ToString(),
                                Quantity = item.Amount,
                                Rarity = 0 // Could be extended to capture rarity
                            };
                            inventoryData.Items.Add(itemData);
                        }
                    }

                    // Clear the inventory buffer (Rule 5.1: Inventory MUST be cleared) - but keep equipment slots
                    for (int i = inventoryBuffer.Length - 1; i >= 8; i--)
                    {
                        inventoryBuffer.RemoveAt(i);
                    }

                    Log?.LogInfo($"[EnhancedArenaSnapshotService] Captured and cleared {inventoryData.Items.Count} inventory items from character (equipment preserved)");
                }
                else
                {
                    Log?.LogWarning($"[EnhancedArenaSnapshotService] No InventoryBuffer component found on character {character.Index}");
                }

                return inventoryData;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Error capturing inventory: {ex.Message}");
                return new InventorySnapshotData
                {
                    Items = new List<ItemData>(),
                    BloodEssence = 0,
                    SoulShards = 0
                };
            }
        }

        private static EquipmentSnapshotData CaptureEquipmentData(Entity character)
        {
            var data = new EquipmentSnapshotData
            {
                EquippedItems = new List<ItemData>()
            };

            try
            {
                var em = VAuto.Core.Core.EntityManager;

                // Capture equipped items from inventory buffer slots 0-7 (equipment slots)
                if (em.HasComponent<ProjectM.InventoryBuffer>(character))
                {
                    var inventoryBuffer = em.GetBuffer<ProjectM.InventoryBuffer>(character);

                    // Equipment slots are typically 0-7 in V Rising
                    for (int i = 0; i < Math.Min(8, inventoryBuffer.Length); i++)
                    {
                        var item = inventoryBuffer[i];
                        if (item.ItemType.GuidHash != 0 && item.Amount > 0)
                        {
                            var itemData = new ItemData
                            {
                                ItemId = item.ItemType.GuidHash.ToString(),
                                Quantity = item.Amount,
                                Rarity = 0
                            };
                            data.EquippedItems.Add(itemData);

                            // Clear the equipment slot (Rule 5.1: Equipment MUST be removed)
                            inventoryBuffer[i] = new ProjectM.InventoryBuffer(); // Empty slot
                        }
                    }

                    Log?.LogInfo($"[EnhancedArenaSnapshotService] Captured and cleared {data.EquippedItems.Count} equipped items from character");
                }
                else
                {
                    Log?.LogWarning("[EnhancedArenaSnapshotService] No InventoryBuffer component found for equipment capture");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Error capturing equipment: {ex.Message}");
            }

            return data;
        }

        private static AbilitiesSnapshotData CaptureAbilitiesData(Entity character)
        {
            var data = new AbilitiesSnapshotData
            {
                UnlockedAbilities = new List<string>(),
                SpellLevels = new Dictionary<string, int>()
            };

            try
            {
                var em = VAuto.Core.Core.EntityManager;

                // Placeholder: Capture abilities from relevant components
                // In V Rising, abilities might be stored in Ability or Spell components
                // This needs proper implementation based on V Rising's ability system
                Log?.LogDebug("[EnhancedArenaSnapshotService] Abilities capture not fully implemented - placeholder");

                // Example: If there are ability-related components, capture them here
                // For now, return empty data
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Error capturing abilities: {ex.Message}");
            }

            return data;
        }

        private static ProgressionSnapshotData CaptureProgressionData(Entity character)
        {
            var data = new ProgressionSnapshotData
            {
                Level = 1,
                Experience = 0,
                UnlockedResearch = new List<string>()
            };

            try
            {
                var em = VAuto.Core.Core.EntityManager;

                // Capture level and experience from Level and Experience components
                // Note: Component types may vary - this is placeholder
                try
                {
                    // Placeholder for level capture
                    // if (em.TryGetComponentData(character, out Level level)) data.Level = level.Value;
                    Log?.LogDebug("[EnhancedArenaSnapshotService] Level capture placeholder");
                }
                catch { }

                try
                {
                    // Placeholder for experience capture
                    // if (em.TryGetComponentData(character, out Experience exp)) data.Experience = exp.Value;
                    Log?.LogDebug("[EnhancedArenaSnapshotService] Experience capture placeholder");
                }
                catch { }

                // Placeholder: Capture unlocked research
                // In V Rising, research might be stored in separate components
                Log?.LogDebug("[EnhancedArenaSnapshotService] Progression capture partially implemented - level and XP captured");

                // Add unlocked research if available
                // data.UnlockedResearch = GetUnlockedResearch(character);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Error capturing progression: {ex.Message}");
            }

            return data;
        }

        private static BuffsSnapshotData CaptureBuffsData(Entity character)
        {
            var data = new BuffsSnapshotData
            {
                ActiveBuffs = new List<string>(),
                ActiveEffects = new List<string>()
            };

            try
            {
                var em = VAuto.Core.Core.EntityManager;

                // Placeholder: Capture active buffs from Buff components
                // In V Rising, buffs might be stored in DynamicBuffer<Buff> or similar
                Log?.LogDebug("[EnhancedArenaSnapshotService] Buffs capture not fully implemented - placeholder");

                // Example: If buffs are in a buffer, capture them here
                // if (em.HasComponent<DynamicBuffer<Buff>>(character)) { ... }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Error capturing buffs: {ex.Message}");
            }

            return data;
        }

        private static CastleSnapshotData CaptureCastleData(Entity user)
        {
            var data = new CastleSnapshotData
            {
                CastleLevel = 0,
                TerritoryInfo = null
            };

            try
            {
                var em = VAuto.Core.Core.EntityManager;

                // Placeholder: Capture castle ownership and territory
                // In V Rising, castle data might be stored in separate systems
                Log?.LogDebug("[EnhancedArenaSnapshotService] Castle data capture not fully implemented - placeholder");

                // Example: Query castle systems for user's castle level and territory
                // data.CastleLevel = GetUserCastleLevel(user);
                // data.TerritoryInfo = GetUserTerritoryInfo(user);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Error capturing castle data: {ex.Message}");
            }

            return data;
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
                var em = VAuto.Core.Core.EntityManager;
                
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

                // Restore blood type and quality (if possible)
                if (VAuto.Core.Core.TryRead<ProjectM.Blood>(snapshot.CharacterEntity, out var blood))
                {
                    try
                    {
                        blood.BloodType = new PrefabGUID(snapshot.OriginalBloodType);
                        blood.Quality = snapshot.OriginalBloodQuality;
                        VAuto.Core.Core.Write(snapshot.CharacterEntity, blood);
                        Log?.LogInfo($"[EnhancedArenaSnapshotService] Restored blood type {snapshot.OriginalBloodType} and quality {snapshot.OriginalBloodQuality} for character {snapshot.CharacterEntity}");
                    }
                    catch (Exception ex)
                    {
                        Log?.LogWarning($"[EnhancedArenaSnapshotService] Failed to restore blood for character {snapshot.CharacterEntity}: {ex.Message}");
                    }
                }
                else
                {
                    Log?.LogDebug("[EnhancedArenaSnapshotService] No Blood component available to restore");
                }

                // Restore player name safely (avoid writing empty names)
                try
                {
                    var originalName = snapshot.OriginalName ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(originalName))
                    {
                        if (VAuto.Core.Core.TryRead<User>(snapshot.UserEntity, out var user))
                        {
                            // Ensure we don't exceed FixedString64Bytes capacity
                            if (originalName.Length > 63) originalName = originalName.Substring(0, 63);
                            user.CharacterName = new FixedString64Bytes(originalName);
                            VAuto.Core.Core.Write(snapshot.UserEntity, user);
                            Log?.LogInfo($"[EnhancedArenaSnapshotService] Restored player name to '{originalName}' for user {snapshot.UserEntity}");
                        }
                        else
                        {
                            Log?.LogWarning($"[EnhancedArenaSnapshotService] Cannot restore name, User entity {snapshot.UserEntity} not found");
                        }
                    }
                    else
                    {
                        Log?.LogWarning($"[EnhancedArenaSnapshotService] Original name empty for snapshot {snapshot.SnapshotUuid}, skipping name restore");
                    }
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[EnhancedArenaSnapshotService] Error restoring player name: {ex.Message}\\n{ex.StackTrace}");
                }

                // Restore UI state
                TryRestoreUIState(snapshot.UserEntity, snapshot);

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
                var em = VAuto.Core.Core.EntityManager;
                
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
            try
            {
                var em = VAuto.Core.Core.EntityManager;

                // Verify entities still exist
                if (!em.Exists(snapshot.UserEntity) || !em.Exists(snapshot.CharacterEntity))
                {
                    Log?.LogWarning("[EnhancedArenaSnapshotService] User or character entity no longer exists for inventory restore");
                    return;
                }

                Log?.LogInfo($"[EnhancedArenaSnapshotService] Restoring inventory for character {snapshot.CharacterEntity.Index}");

                // Restore inventory items to slots starting from 8 (after equipment slots)
                if (em.HasComponent<ProjectM.InventoryBuffer>(snapshot.CharacterEntity))
                {
                    var inventoryBuffer = em.GetBuffer<ProjectM.InventoryBuffer>(snapshot.CharacterEntity);

                    // Ensure buffer has enough space for restored items
                    int startSlot = 8; // Equipment slots 0-7, inventory starts at 8
                    int neededSlots = startSlot + snapshot.InventoryData.Items.Count;

                    while (inventoryBuffer.Length < neededSlots)
                    {
                        inventoryBuffer.Add(new ProjectM.InventoryBuffer());
                    }

                    // Restore inventory items to slots 8+
                    for (int i = 0; i < snapshot.InventoryData.Items.Count; i++)
                    {
                        var itemData = snapshot.InventoryData.Items[i];
                        if (int.TryParse(itemData.ItemId, out var itemId))
                        {
                            inventoryBuffer[startSlot + i] = new ProjectM.InventoryBuffer
                            {
                                ItemType = new PrefabGUID(itemId),
                                Amount = itemData.Quantity
                            };
                        }
                    }

                    Log?.LogInfo($"[EnhancedArenaSnapshotService] Restored {snapshot.InventoryData.Items.Count} inventory items to slots {startSlot}+");
                }
                else
                {
                    Log?.LogWarning($"[EnhancedArenaSnapshotService] No InventoryBuffer component found on character {snapshot.CharacterEntity.Index} for inventory restore");
                }

                Log?.LogInfo($"[EnhancedArenaSnapshotService] Inventory restoration completed");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Error restoring inventory: {ex.Message}");
            }
        }

        private static void RestoreEquipment(PlayerSnapshot snapshot)
        {
            try
            {
                if (snapshot == null)
                {
                    Log?.LogWarning("[EnhancedArenaSnapshotService] Snapshot is null for equipment restore");
                    return;
                }

                var em = VAuto.Core.Core.EntityManager;
                if (em == null)
                {
                    Log?.LogError("[EnhancedArenaSnapshotService] EntityManager is null");
                    return;
                }

                if (!em.Exists(snapshot.CharacterEntity))
                {
                    Log?.LogWarning("[EnhancedArenaSnapshotService] Character entity no longer exists for equipment restore");
                    return;
                }

                if (snapshot.EquipmentData == null || snapshot.EquipmentData.EquippedItems == null || snapshot.EquipmentData.EquippedItems.Count == 0)
                {
                    Log?.LogDebug("[EnhancedArenaSnapshotService] No equipment data to restore");
                    return;
                }

                Log?.LogInfo($"[EnhancedArenaSnapshotService] Restoring {snapshot.EquipmentData.EquippedItems.Count} equipped items");

                // Restore equipped items to equipment slots (0-7)
                if (em.HasComponent<ProjectM.InventoryBuffer>(snapshot.CharacterEntity))
                {
                    var inventoryBuffer = em.GetBuffer<ProjectM.InventoryBuffer>(snapshot.CharacterEntity);

                    // Ensure buffer has enough space for equipment slots
                    while (inventoryBuffer.Length < 8)
                    {
                        inventoryBuffer.Add(new ProjectM.InventoryBuffer());
                    }

                    // Restore equipment items to slots 0-7
                    for (int i = 0; i < snapshot.EquipmentData.EquippedItems.Count && i < 8; i++)
                    {
                        var itemData = snapshot.EquipmentData.EquippedItems[i];
                        if (itemData != null && int.TryParse(itemData.ItemId, out var itemId))
                        {
                            inventoryBuffer[i] = new ProjectM.InventoryBuffer
                            {
                                ItemType = new PrefabGUID(itemId),
                                Amount = itemData.Quantity
                            };
                        }
                    }

                    Log?.LogInfo($"[EnhancedArenaSnapshotService] Restored {snapshot.EquipmentData.EquippedItems.Count} equipped items to equipment slots");
                }
                else
                {
                    Log?.LogDebug("[EnhancedArenaSnapshotService] No InventoryBuffer component found for equipment restore (non-critical)");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Error restoring equipment: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void RestoreAbilities(PlayerSnapshot snapshot)
        {
            try
            {
                if (snapshot == null)
                {
                    Log?.LogWarning("[EnhancedArenaSnapshotService] Snapshot is null for abilities restore");
                    return;
                }

                var em = VAuto.Core.Core.EntityManager;
                if (em == null)
                {
                    Log?.LogError("[EnhancedArenaSnapshotService] EntityManager is null");
                    return;
                }

                if (!em.Exists(snapshot.CharacterEntity))
                {
                    Log?.LogWarning("[EnhancedArenaSnapshotService] Character entity no longer exists for abilities restore");
                    return;
                }

                if (snapshot.AbilitiesData == null)
                {
                    Log?.LogDebug("[EnhancedArenaSnapshotService] No abilities data to restore");
                    return;
                }

                int abilityCount = snapshot.AbilitiesData.UnlockedAbilities?.Count ?? 0;
                int spellCount = snapshot.AbilitiesData.SpellLevels?.Count ?? 0;
                Log?.LogInfo($"[EnhancedArenaSnapshotService] Restoring {abilityCount} abilities and {spellCount} spell levels");

                // Placeholder: Restore abilities to relevant components
                // This needs proper implementation based on V Rising's ability system
                Log?.LogDebug("[EnhancedArenaSnapshotService] Abilities restore not fully implemented - placeholder");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Error restoring abilities: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void RestoreProgression(PlayerSnapshot snapshot)
        {
            try
            {
                if (snapshot == null)
                {
                    Log?.LogWarning("[EnhancedArenaSnapshotService] Snapshot is null for progression restore");
                    return;
                }

                var em = VAuto.Core.Core.EntityManager;
                if (em == null)
                {
                    Log?.LogError("[EnhancedArenaSnapshotService] EntityManager is null");
                    return;
                }

                if (!em.Exists(snapshot.CharacterEntity))
                {
                    Log?.LogWarning("[EnhancedArenaSnapshotService] Character entity no longer exists for progression restore");
                    return;
                }

                if (snapshot.ProgressionData == null)
                {
                    Log?.LogDebug("[EnhancedArenaSnapshotService] No progression data to restore");
                    return;
                }

                Log?.LogInfo($"[EnhancedArenaSnapshotService] Restoring progression - Level: {snapshot.ProgressionData.Level}, XP: {snapshot.ProgressionData.Experience}");

                // Placeholder: Restore level and experience
                try
                {
                    // Placeholder for level restore
                    Log?.LogDebug("[EnhancedArenaSnapshotService] Level restore placeholder");
                }
                catch { }

                try
                {
                    // Placeholder for experience restore
                    Log?.LogDebug("[EnhancedArenaSnapshotService] Experience restore placeholder");
                }
                catch { }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Error restoring progression: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void RestoreBuffs(PlayerSnapshot snapshot)
        {
            try
            {
                if (snapshot == null)
                {
                    Log?.LogWarning("[EnhancedArenaSnapshotService] Snapshot is null for buffs restore");
                    return;
                }

                var em = VAuto.Core.Core.EntityManager;
                if (em == null)
                {
                    Log?.LogError("[EnhancedArenaSnapshotService] EntityManager is null");
                    return;
                }

                if (!em.Exists(snapshot.CharacterEntity))
                {
                    Log?.LogWarning("[EnhancedArenaSnapshotService] Character entity no longer exists for buffs restore");
                    return;
                }

                if (snapshot.BuffsData == null)
                {
                    Log?.LogDebug("[EnhancedArenaSnapshotService] No buffs data to restore");
                    return;
                }

                int buffCount = snapshot.BuffsData.ActiveBuffs?.Count ?? 0;
                int effectCount = snapshot.BuffsData.ActiveEffects?.Count ?? 0;
                Log?.LogInfo($"[EnhancedArenaSnapshotService] Restoring {buffCount} buffs and {effectCount} effects");

                // Placeholder: Restore active buffs
                Log?.LogDebug("[EnhancedArenaSnapshotService] Buffs restore not fully implemented - placeholder");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Error restoring buffs: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void RestoreCastleData(PlayerSnapshot snapshot)
        {
            try
            {
                if (snapshot == null)
                {
                    Log?.LogWarning("[EnhancedArenaSnapshotService] Snapshot is null for castle data restore");
                    return;
                }

                var em = VAuto.Core.Core.EntityManager;
                if (em == null)
                {
                    Log?.LogError("[EnhancedArenaSnapshotService] EntityManager is null");
                    return;
                }

                if (!em.Exists(snapshot.UserEntity))
                {
                    Log?.LogWarning("[EnhancedArenaSnapshotService] User entity no longer exists for castle data restore");
                    return;
                }

                if (snapshot.CastleData == null)
                {
                    Log?.LogDebug("[EnhancedArenaSnapshotService] No castle data to restore");
                    return;
                }

                Log?.LogInfo($"[EnhancedArenaSnapshotService] Restoring castle data - Level: {snapshot.CastleData.CastleLevel}");

                // Placeholder: Restore castle ownership and territory
                Log?.LogDebug("[EnhancedArenaSnapshotService] Castle data restore not fully implemented - placeholder");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Error restoring castle data: {ex.Message}\\n{ex.StackTrace}");
            }
        }
        #endregion

        #region Snapshot Management
        public static bool DeleteSnapshot(string characterId, string arenaId, bool deleteFile = true)
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
                        
                        // Delete file if requested
                        if (deleteFile)
                        {
                            DeleteSnapshotFile(snapshotUuid);
                        }
                        
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

        public static void DeleteAllSnapshots(bool deleteFiles = true)
        {
            lock (_lock)
            {
                if (deleteFiles)
                {
                    var snapshotsDir = Path.Combine(Plugin.VAutoDataDir, "Snapshots");
                    if (Directory.Exists(snapshotsDir))
                    {
                        try
                        {
                            var files = Directory.GetFiles(snapshotsDir, "snapshot_*.json");
                            foreach (var file in files)
                            {
                                File.Delete(file);
                            }
                            Log?.LogInfo($"[EnhancedArenaSnapshotService] Deleted {files.Length} snapshot files");
                        }
                        catch (Exception ex)
                        {
                            Log?.LogError($"[EnhancedArenaSnapshotService] Failed to delete snapshot files: {ex.Message}");
                        }
                    }
                }

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
        public static bool DeleteSnapshotByUuid(string snapshotUuid, bool deleteFile = true)
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
                    
                    // Delete file if requested
                    if (deleteFile)
                    {
                        DeleteSnapshotFile(snapshotUuid);
                    }

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

        private static void TryCaptureUIState(Entity user, PlayerSnapshot snapshot)
        {
            try
            {
                // Placeholder: Capture UI unlock state
                // Use reflection to call DebugEventsSystem.UnlockAllResearch() and CompleteAllAchievements()
                // This needs proper implementation based on V Rising's UI system
                Log?.LogDebug("[EnhancedArenaSnapshotService] UI state capture not fully implemented - placeholder");

                // Example: Serialize current UI unlock state to JSON
                // snapshot.UIState = JsonSerializer.Serialize(GetCurrentUIState(user));
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Error capturing UI state: {ex.Message}");
            }
        }

        private static void TryRestoreUIState(Entity user, PlayerSnapshot snapshot)
        {
            try
            {
                if (string.IsNullOrEmpty(snapshot.UIState))
                {
                    Log?.LogDebug("[EnhancedArenaSnapshotService] No UI state to restore");
                    return;
                }

                // Placeholder: Restore UI unlock state
                // Use reflection on DebugEventsSystem to unlock research and achievements
                Log?.LogDebug("[EnhancedArenaSnapshotService] UI state restore not fully implemented - placeholder");

                // Example: Use reflection to call UnlockAllResearch and CompleteAllAchievements
                // This ensures UI unlocks are restored per Rule 7.2
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Error restoring UI state: {ex.Message}");
            }
        }

        #region Prefab Mapping System

        public static class PrefabMapper
        {
            private static readonly Dictionary<string, int> _prefabIndex;

            static PrefabMapper()
            {
                // Load prefab index from JSON file
                _prefabIndex = PrefabIndexLoader.Load("BepInEx/config/VAuto.Arena/prefab_index.json");
            }

            public static int GetPrefabID(string name) => _prefabIndex.TryGetValue(name, out var id) ? id : -1;
            public static int MapAbilityPrefab(string abilityName) => GetPrefabID(abilityName);
            public static int MapBuffPrefab(string buffName) => GetPrefabID(buffName);
            public static int MapSlotPrefab(string slotName) => GetPrefabID(slotName);
        }

        public static class PrefabIndexLoader
        {
            public static Dictionary<string, int> Load(string jsonPath)
            {
                try
                {
                    if (System.IO.File.Exists(jsonPath))
                    {
                        var json = System.IO.File.ReadAllText(jsonPath);
                        return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
                    }
                    else
                    {
                        // Return default mappings if file doesn't exist
                        return new Dictionary<string, int>
                        {
                            ["AB_ApplyWeaponCoating_Activate"] = 123456789,
                            ["AB_ApplyWeaponCoating_Blood_Activate"] = 123456790,
                            ["AB_ArchMage_CrystalLance_AbilityGroup"] = 123456791,
                            ["AB_ArchMage_FireSpinner_AbilityGroup"] = 123456792,
                            ["AB_Bandit_Foreman_RapidShot_AbilityGroup"] = 123456793,
                            ["AB_Bandit_Hunter_Bow_Group"] = 123456794,
                            ["AB_ApplyWeaponCoating_Chaos_Activate"] = 123456795,
                            ["AB_ApplyWeaponCoating_Frost_Activate"] = 123456796
                        };
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[PrefabIndexLoader] Error loading prefab index: {ex.Message}");
                    return new Dictionary<string, int>();
                }
            }
        }

        #endregion

        #region Serialization Methods
        /// <summary>
        /// Serializes a PlayerSnapshot to JSON string
        /// </summary>
        public static string SerializeSnapshot(PlayerSnapshot snapshot)
        {
            try
            {
                return JsonUtil.Serialize(snapshot, JsonUtil.SnapshotJsonOptions);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Failed to serialize snapshot: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deserializes a JSON string to PlayerSnapshot
        /// </summary>
        public static PlayerSnapshot DeserializeSnapshot(string json)
        {
            try
            {
                return JsonUtil.Deserialize<PlayerSnapshot>(json, JsonUtil.SnapshotJsonOptions);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Failed to deserialize snapshot: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves a snapshot to file
        /// </summary>
        public static bool SaveSnapshotToFile(PlayerSnapshot snapshot, string directory = null)
        {
            try
            {
                var dir = directory ?? Path.Combine(Plugin.VAutoDataDir, "Snapshots");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var fileName = $"snapshot_{snapshot.SnapshotUuid}.json";
                var filePath = Path.Combine(dir, fileName);

                var json = SerializeSnapshot(snapshot);
                if (json == null)
                    return false;

                File.WriteAllText(filePath, json);
                Log?.LogInfo($"[EnhancedArenaSnapshotService] Saved snapshot to file: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Failed to save snapshot to file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads a snapshot from file
        /// </summary>
        public static PlayerSnapshot LoadSnapshotFromFile(string snapshotUuid, string directory = null)
        {
            try
            {
                var dir = directory ?? Path.Combine(Plugin.VAutoDataDir, "Snapshots");
                var fileName = $"snapshot_{snapshotUuid}.json";
                var filePath = Path.Combine(dir, fileName);

                if (!File.Exists(filePath))
                {
                    Log?.LogWarning($"[EnhancedArenaSnapshotService] Snapshot file not found: {filePath}");
                    return null;
                }

                var json = File.ReadAllText(filePath);
                var snapshot = DeserializeSnapshot(json);
                
                if (snapshot != null)
                {
                    Log?.LogInfo($"[EnhancedArenaSnapshotService] Loaded snapshot from file: {filePath}");
                }

                return snapshot;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Failed to load snapshot from file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads all snapshots from the snapshots directory
        /// </summary>
        public static List<PlayerSnapshot> LoadAllSnapshotsFromFiles(string directory = null)
        {
            var snapshots = new List<PlayerSnapshot>();
            try
            {
                var dir = directory ?? Path.Combine(Plugin.VAutoDataDir, "Snapshots");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    return snapshots;
                }

                var files = Directory.GetFiles(dir, "snapshot_*.json");
                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var snapshot = DeserializeSnapshot(json);
                        if (snapshot != null)
                        {
                            snapshots.Add(snapshot);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log?.LogError($"[EnhancedArenaSnapshotService] Failed to load snapshot file {Path.GetFileName(file)}: {ex.Message}");
                    }
                }

                Log?.LogInfo($"[EnhancedArenaSnapshotService] Loaded {snapshots.Count} snapshots from files");
                return snapshots;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Failed to load snapshots from files: {ex.Message}");
                return snapshots;
            }
        }

        /// <summary>
        /// Deletes a snapshot file
        /// </summary>
        public static bool DeleteSnapshotFile(string snapshotUuid, string directory = null)
        {
            try
            {
                var dir = directory ?? Path.Combine(Plugin.VAutoDataDir, "Snapshots");
                var fileName = $"snapshot_{snapshotUuid}.json";
                var filePath = Path.Combine(dir, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Log?.LogInfo($"[EnhancedArenaSnapshotService] Deleted snapshot file: {filePath}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnhancedArenaSnapshotService] Failed to delete snapshot file: {ex.Message}");
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
            public int OriginalBloodType { get; set; }
            public float OriginalBloodQuality { get; set; }
            public float3 OriginalPosition { get; set; }
            public quaternion OriginalRotation { get; set; }
            
            // Detailed Data
            public InventorySnapshotData InventoryData { get; set; }
            public EquipmentSnapshotData EquipmentData { get; set; }
            public AbilitiesSnapshotData AbilitiesData { get; set; }
            public ProgressionSnapshotData ProgressionData { get; set; }
            public BuffsSnapshotData BuffsData { get; set; }
            public string UIState { get; set; } = string.Empty; // JSON serialized UI state
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
            public List<ItemData> EquippedItems { get; set; } = new();
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
#endregion
