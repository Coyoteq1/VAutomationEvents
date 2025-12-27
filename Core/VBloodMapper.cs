using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Stunlock.Core;
using ProjectM;

namespace VAuto.Core
{
    /// <summary>
    /// VBlood Mapper for managing VBlood boss entities and operations
    /// Provides mapping between VBlood GUIDs and their properties
    /// </summary>
    public static class VBloodMapper
{
    // VBlood boss data structure
    public class VBloodBoss
    {
        public int GuidHash { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public int Level { get; set; }
        public string Location { get; set; }
        public string[] Drops { get; set; }
        public bool IsEnabled { get; set; }
        public float Health { get; set; }
        public string Description { get; set; }
    }

    // VBlood boss database
    private static readonly Dictionary<int, VBloodBoss> _vBloodDatabase = InitializeVBloodDatabase();

    // Active VBlood entities mapping
    private static readonly Dictionary<int, Entity> _activeVBloodEntities = new();

    /// <summary>
    /// Initialize the VBlood database with known bosses
    /// </summary>
    private static Dictionary<int, VBloodBoss> InitializeVBloodDatabase()
    {
        return new Dictionary<int, VBloodBoss>
        {
            // From config file VBloodGuids
            [-1905777458] = new VBloodBoss {
                GuidHash = -1905777458,
                Name = "Alpha Wolf",
                Category = "Beast",
                Level = 20,
                Location = "Farbane Woods",
                Drops = new[] { "Werewolf Heart", "Silver Ore", "Wolf Hide" },
                IsEnabled = true,
                Health = 2500f,
                Description = "Ferocious alpha wolf, leader of the pack"
            },
            [-1541423745] = new VBloodBoss {
                GuidHash = -1541423745,
                Name = "Errol the Stonebreaker",
                Category = "Human",
                Level = 27,
                Location = "Stonebreaker Quarry",
                Drops = new[] { "Stonebreaker's Mace", "Quarry Hammer", "Stone Dust" },
                IsEnabled = true,
                Health = 3500f,
                Description = "Massive stone giant with earth-shaking power"
            },
            [1851788208] = new VBloodBoss {
                GuidHash = 1851788208,
                Name = "Gravekeeper",
                Category = "Undead",
                Level = 30,
                Location = "Forgotten Cemetery",
                Drops = new[] { "Gravekeeper's Scythe", "Tomb Stone", "Spectral Dust" },
                IsEnabled = true,
                Health = 4200f,
                Description = "Ancient guardian of the cemetery crypts"
            },
            [-1329110591] = new VBloodBoss {
                GuidHash = -1329110591,
                Name = "Solarus the Immaculate",
                Category = "Vampire",
                Level = 35,
                Location = "Silverlight Hills",
                Drops = new[] { "Solarus' Blade", "Sunstone", "Holy Water" },
                IsEnabled = true,
                Health = 5000f,
                Description = "Radiant vampire hunter blessed by the sun"
            },
            [1847352945] = new VBloodBoss {
                GuidHash = 1847352945,
                Name = "Foulrot the Undead Sorcerer",
                Category = "Undead",
                Level = 40,
                Location = "Foulrot's Domain",
                Drops = new[] { "Foulrot's Grimoire", "Cursed Bone", "Necrotic Essence" },
                IsEnabled = true,
                Health = 6000f,
                Description = "Powerful undead sorcerer wielding necrotic magic"
            },
            // Baby Blood - Special weak VBlood for testing/training
            [-1996241419] = new VBloodBoss {
                GuidHash = -1996241419,
                Name = "Baby Blood",
                Category = "Training",
                Level = 1,
                Location = "Training Grounds",
                Drops = new[] { "Blood Essence", "Training Token", "Baby Fang" },
                IsEnabled = true,
                Health = 100f,
                Description = "Weak VBlood entity for training and testing purposes"
            }
        };
    }

    /// <summary>
    /// Get VBlood boss by GUID hash
    /// </summary>
    public static VBloodBoss GetVBloodBoss(int guidHash)
    {
        return _vBloodDatabase.TryGetValue(guidHash, out var boss) ? boss : null;
    }

    /// <summary>
    /// Get VBlood boss by name (case-insensitive)
    /// </summary>
    public static VBloodBoss GetVBloodBossByName(string name)
    {
        return _vBloodDatabase.Values.FirstOrDefault(b =>
            b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get all VBlood bosses
    /// </summary>
    public static IEnumerable<VBloodBoss> GetAllVBloodBosses()
    {
        return _vBloodDatabase.Values;
    }

    /// <summary>
    /// Get VBlood bosses by category
    /// </summary>
    public static IEnumerable<VBloodBoss> GetVBloodBossesByCategory(string category)
    {
        return _vBloodDatabase.Values.Where(b =>
            b.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Check if GUID hash corresponds to a known VBlood boss
    /// </summary>
    public static bool IsVBloodBoss(int guidHash)
    {
        return _vBloodDatabase.ContainsKey(guidHash);
    }

    /// <summary>
    /// Register an active VBlood entity
    /// </summary>
    public static void RegisterVBloodEntity(int guidHash, Entity entity)
    {
        _activeVBloodEntities[guidHash] = entity;
        Plugin.Logger?.LogInfo($"[VBloodMapper] Registered VBlood entity {guidHash} -> {entity.Index}");
    }

    /// <summary>
    /// Unregister a VBlood entity
    /// </summary>
    public static void UnregisterVBloodEntity(int guidHash)
    {
        if (_activeVBloodEntities.Remove(guidHash))
        {
            Plugin.Logger?.LogInfo($"[VBloodMapper] Unregistered VBlood entity {guidHash}");
        }
    }

    /// <summary>
    /// Get active VBlood entity by GUID hash
    /// </summary>
    public static Entity GetVBloodEntity(int guidHash)
    {
        return _activeVBloodEntities.TryGetValue(guidHash, out var entity) ? entity : Entity.Null;
    }

    /// <summary>
    /// Get all active VBlood entities
    /// </summary>
    public static IEnumerable<KeyValuePair<int, Entity>> GetActiveVBloodEntities()
    {
        return _activeVBloodEntities;
    }

    /* 
    /// <summary>
    /// Spawn VBlood boss at location
    /// </summary>
    public static bool SpawnVBloodBoss(int guidHash, float3 position)
    {
        var boss = GetVBloodBoss(guidHash);
        if (boss == null)
        {
            Plugin.Logger?.LogWarning($"[VBloodMapper] Unknown VBlood GUID: {guidHash}");
            return false;
        }

        try
        {
            // Create VBlood entity using V Rising's VBlood spawning system
            var entity = VRCore.EM.CreateEntity();

            // Add VBlood components
            entity.AddComponentData(new VBloodUnit { UnitId = guidHash });
            entity.AddComponentData(new Translation { Value = position });
            entity.AddComponentData(new Rotation { Value = quaternion.identity });

            // Register the entity
            RegisterVBloodEntity(guidHash, entity);

            Plugin.Logger?.LogInfo($"[VBloodMapper] Spawned VBlood '{boss.Name}' at {position}");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"[VBloodMapper] Failed to spawn VBlood {guidHash}: {ex.Message}");
            return false;
        }
    }
    */

    /// <summary>
    /// Spawn Baby Blood (special training VBlood)
    /// </summary>
    public static bool SpawnBabyBlood(float3 position)
    {
        const int babyBloodGuid = -1996241419; // Baby Blood GUID
        // return SpawnVBloodBoss(babyBloodGuid, position);
        Plugin.Logger?.LogInfo($"[VBloodMapper] SpawnBabyBlood called for {position} - method disabled");
        return false;
    }

    /// <summary>
    /// Despawn VBlood boss
    /// </summary>
    public static bool DespawnVBloodBoss(int guidHash)
    {
        var entity = GetVBloodEntity(guidHash);
        if (entity == Entity.Null || !VRCore.EM.Exists(entity))
        {
            return false;
        }

        try
        {
            // Remove from active entities
            UnregisterVBloodEntity(guidHash);

            // Destroy the entity
            VRCore.EM.DestroyEntity(entity);

            Plugin.Logger?.LogInfo($"[VBloodMapper] Despawned VBlood {guidHash}");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"[VBloodMapper] Failed to despawn VBlood {guidHash}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get VBlood boss health
    /// </summary>
    public static float GetVBloodHealth(int guidHash)
    {
        var entity = GetVBloodEntity(guidHash);
        if (entity == Entity.Null || !VRCore.EM.Exists(entity))
        {
            return 0f;
        }

        try
        {
            if (VRCore.EM.TryGetComponentData(entity, out Health health))
            {
                return health.Value;
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogDebug($"[VBloodMapper] Error getting VBlood health: {ex.Message}");
        }

        return 0f;
    }

    /// <summary>
    /// Set VBlood boss health
    /// </summary>
    public static bool SetVBloodHealth(int guidHash, float health)
    {
        var entity = GetVBloodEntity(guidHash);
        if (entity == Entity.Null || !VRCore.EM.Exists(entity))
        {
            return false;
        }

        try
        {
            if (VRCore.EM.TryGetComponentData(entity, out Health currentHealth))
            {
                VRCore.EM.SetComponentData(entity, new Health
                {
                    Value = health,
                    MaxHealth = currentHealth.MaxHealth
                });
                return true;
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"[VBloodMapper] Error setting VBlood health: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Check if VBlood boss is alive
    /// </summary>
    public static bool IsVBloodAlive(int guidHash)
    {
        return GetVBloodHealth(guidHash) > 0f;
    }

    /// <summary>
    /// Get VBlood boss position
    /// </summary>
    public static float3 GetVBloodPosition(int guidHash)
    {
        var entity = GetVBloodEntity(guidHash);
        if (entity == Entity.Null || !VRCore.EM.Exists(entity))
        {
            return float3.zero;
        }

        try
        {
            if (VRCore.EM.TryGetComponentData(entity, out Translation translation))
            {
                return translation.Value;
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogDebug($"[VBloodMapper] Error getting VBlood position: {ex.Message}");
        }

        return float3.zero;
    }

    /* 
    /// <summary>
    /// VBlood Unlock System - Simulates VBlood progression to unlock spellbook and abilities
    /// Works like build mode - activates on enter, deactivates on exit using ability modifications
    /// </summary>
    public static class VBloodUnlockSystem
    {
        // VBlood unlock buff - prevents aggro like build mode
        public static readonly PrefabGUID VBloodUnlockBuff = new PrefabGUID(-480024073); // Different from build buff

        // Track which characters have VBlood unlock mode enabled
        private static readonly Dictionary<Entity, bool> _vBloodUnlockEnabled = new();

        // Spellbook unlock abilities - mapped to keyboard shortcuts like build mode
        // Using proper AbilityGroupSlot GUIDs for spellbook unlocking
        private static readonly PrefabGUID SpellUnlockQ = new PrefabGUID(-633717863); // Q key - Spellbook ability group
        private static readonly PrefabGUID SpellUnlockE = new PrefabGUID(-633717863); // E key - Spellbook ability group
        private static readonly PrefabGUID SpellUnlockR = new PrefabGUID(-633717863); // R key - Spellbook ability group
        private static readonly PrefabGUID BloodTypeUnlock = new PrefabGUID(-633717863); // C key - Spellbook ability group
        private static readonly PrefabGUID AbilityTreeUnlock = new PrefabGUID(-633717863); // T key - Spellbook ability group

        // Cooldown buff for VBlood unlock mode
        private static readonly ModifyUnitStatBuff_DOTS VBloodCooldown = new()
        {
            StatType = UnitStatType.CooldownRecoveryRate,
            Value = 100,
            ModificationType = ModificationType.Set,
            Modifier = 1,
            Id = ModificationId.NewId(0)
        };

        /// <summary>
        /// Check if character is in VBlood unlock mode (like IsCharacterInBuildMode)
        /// </summary>
        public static bool IsCharacterInVBloodUnlockMode(Entity charEntity)
        {
            return charEntity.Has<VBloodUnlockAbilityBuffer>();
        }

        /// <summary>
        /// Enable VBlood unlock mode for a character (simulates defeating all VBloods)
        /// Like entering build mode - adds spell unlock abilities and opens spellbook
        /// </summary>
        public static bool EnableVBloodUnlockMode(Entity character)
        {
            try
            {
                Plugin.Logger?.LogInfo($"[VBloodUnlockSystem] Enabling VBlood unlock mode for character {character.Index}");

                // Check if already enabled
                if (IsCharacterInVBloodUnlockMode(character))
                {
                    Plugin.Logger?.LogInfo($"[VBloodUnlockSystem] VBlood unlock mode already enabled for {character.Index}");
                    return true;
                }

                // Add spell unlock abilities to slots (like build mode does)
                AddSpellUnlockAbility(character, 1, SpellUnlockQ); // Q - Basic spells
                AddSpellUnlockAbility(character, 4, SpellUnlockE); // E - Advanced spells
                AddSpellUnlockAbility(character, 5, SpellUnlockR); // R - Ultimate spells
                AddSpellUnlockAbility(character, 6, BloodTypeUnlock); // C - Blood types
                AddSpellUnlockAbility(character, 7, AbilityTreeUnlock); // T - Ability trees

                // Apply VBlood unlock buff (like build buff)
                var userEntity = character.Read<PlayerCharacter>().UserEntity;
                BuffUtility.AddBuff(userEntity, character, VBloodUnlockBuff, -1, UpdateVBloodUnlockBuff);

                // Mark as enabled
                _vBloodUnlockEnabled[character] = true;

                Plugin.Logger?.LogInfo($"[VBloodUnlockSystem] VBlood unlock mode enabled for character {character.Index}");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[VBloodUnlockSystem] Error enabling VBlood unlock mode: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disable VBlood unlock mode for a character (resets unlocks)
        /// Like exiting build mode - removes spell unlock abilities and closes enhanced spellbook
        /// </summary>
        public static bool DisableVBloodUnlockMode(Entity character)
        {
            try
            {
                Plugin.Logger?.LogInfo($"[VBloodUnlockSystem] Disabling VBlood unlock mode for character {character.Index}");

                // Check if enabled
                if (!IsCharacterInVBloodUnlockMode(character))
                {
                    Plugin.Logger?.LogInfo($"[VBloodUnlockSystem] VBlood unlock mode not enabled for {character.Index}");
                    return true;
                }

                // Remove all spell unlock abilities (like RemoveBuildMode does)
                var buffer = VRCore.EM.GetBuffer<VBloodUnlockAbilityBuffer>(character);
                foreach (var mod in buffer)
                {
                    // Remove ability modification
                    VRCore.ServerGameManager?.RemoveAbilityGroupModificationOnSlot(character, mod.Slot, mod.ModificationId);
                }
                VRCore.EM.RemoveComponent<VBloodUnlockAbilityBuffer>(character);

                // Remove VBlood unlock buff
                BuffUtility.RemoveBuff(character, VBloodUnlockBuff);

                // Mark as disabled
                _vBloodUnlockEnabled.Remove(character);

                Plugin.Logger?.LogInfo($"[VBloodUnlockSystem] VBlood unlock mode disabled for character {character.Index}");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[VBloodUnlockSystem] Error disabling VBlood unlock mode: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if VBlood unlock mode is enabled for a character
        /// </summary>
        public static bool IsVBloodUnlockModeEnabled(Entity character)
        {
            return _vBloodUnlockEnabled.TryGetValue(character, out var enabled) && enabled;
        }

        /// <summary>
        /// Add spell unlock ability to specific slot (like AddAbility in BuildService)
        /// </summary>
        private static void AddSpellUnlockAbility(Entity charEntity, int slot, PrefabGUID abilityPrefab)
        {
            var modificationId = VRCore.ServerGameManager?.ModifyAbilityGroupOnSlot(charEntity, charEntity, slot, abilityPrefab, 1000);
            if (modificationId.HasValue)
            {
                var mod = new VAuto.Core.MissingTypes.VBloodUnlockAbilityBuffer()
                {
                    Owner = charEntity,
                    Target = charEntity,
                    ModificationId = modificationId.Value.Id, // Convert to int
                    NewAbilityGroup = abilityPrefab.GuidHash, // Convert to int
                    Slot = slot,
                    Priority = 1000,
                };

                // Use dynamic buffer since VBloodUnlockAbilityBuffer is not a proper ECS component
                // For now, we'll track this in a different way
                // TODO: Implement proper ECS buffer tracking
            }
        }

        /// <summary>
        /// Update VBlood unlock buff (like UpdateBuildBuff)
        /// </summary>
        private static void UpdateVBloodUnlockBuff(Entity buffEntity)
        {
            var prefabGuid = buffEntity.Read<PrefabGUID>();
            if (prefabGuid != VBloodUnlockBuff) return;

            if (!buffEntity.Has<ModifyUnitStatBuff_DOTS>())
            {
                var modifyStatBuffer = VRCore.EM.AddBuffer<ModifyUnitStatBuff_DOTS>(buffEntity);
                modifyStatBuffer.Clear();
                modifyStatBuffer.Add(VBloodCooldown);
            }

            // Prevent aggro like build mode
            buffEntity.Add<DisableAggroBuff>();
            buffEntity.Write(new DisableAggroBuff
            {
                Mode = DisableAggroBuffMode.OthersDontAttackTarget
            });
        }

        /// <summary>
        /// Legacy method - unlock all VBlood progression for a character (simulates defeating all VBloods)
        /// </summary>
        public static bool UnlockAllVBloodProgression(Entity character)
        {
            return EnableVBloodUnlockMode(character);
        }

        /// <summary>
        /// Unlock progression for a specific VBlood boss
        /// </summary>
        public static bool UnlockVBloodProgression(Entity character, int guidHash)
        {
            try
            {
                var boss = GetVBloodBoss(guidHash);
                if (boss == null)
                {
                    Plugin.Logger?.LogWarning($"[VBloodUnlockSystem] Unknown VBlood GUID: {guidHash}");
                    return false;
                }

                Plugin.Logger?.LogInfo($"[VBloodUnlockSystem] Unlocking progression for {boss.Name} (GUID: {guidHash})");

                // Simulate VBlood defeat by adding progression data
                // This mimics what happens when you actually defeat a VBlood in the game

                // TODO: Implement actual VBlood progression unlocking
                // This would involve:
                // 1. Adding VBlood defeat records to player progression
                // 2. Unlocking associated abilities/spells
                // 3. Updating blood type availability
                // 4. Triggering spellbook UI updates

                Plugin.Logger?.LogInfo($"[VBloodUnlockSystem] Progression unlocked for {boss.Name}");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[VBloodUnlockSystem] Error unlocking VBlood progression for {guidHash}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Force open the spellbook UI
        /// </summary>
        public static bool OpenSpellbookUI(Entity character)
        {
            try
            {
                Plugin.Logger?.LogInfo($"[VBloodUnlockSystem] Opening spellbook UI for character {character.Index}");

                // TODO: Implement spellbook UI opening
                // This would involve triggering the game's spellbook UI system

                Plugin.Logger?.LogInfo($"[VBloodUnlockSystem] Spellbook UI opened");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[VBloodUnlockSystem] Error opening spellbook UI: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unlock all abilities and blood types
        /// </summary>
        public static bool UnlockAllAbilitiesAndBloodTypes(Entity character)
        {
            try
            {
                Plugin.Logger?.LogInfo($"[VBloodUnlockSystem] Unlocking all abilities and blood types for character {character.Index}");

                // TODO: Implement ability and blood type unlocking
                // This would involve:
                // 1. Adding all abilities to the player's ability set
                // 2. Unlocking all blood types for transformation
                // 3. Making all spells available in spellbook

                Plugin.Logger?.LogInfo($"[VBloodUnlockSystem] All abilities and blood types unlocked");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[VBloodUnlockSystem] Error unlocking abilities and blood types: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if VBlood progression is unlocked for a character
        /// </summary>
        public static bool IsVBloodProgressionUnlocked(Entity character, int guidHash)
        {
            try
            {
                // TODO: Implement check for VBlood progression unlock status
                return true; // For now, assume all are unlocked in arena
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[VBloodUnlockSystem] Error checking VBlood progression: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reset VBlood progression (for arena exit)
        /// </summary>
        public static bool ResetVBloodProgression(Entity character)
        {
            try
            {
                Plugin.Logger?.LogInfo($"[VBloodUnlockSystem] Resetting VBlood progression for character {character.Index}");

                // TODO: Implement VBlood progression reset
                // This would remove the simulated progression unlocks

                Plugin.Logger?.LogInfo($"[VBloodUnlockSystem] VBlood progression reset");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[VBloodUnlockSystem] Error resetting VBlood progression: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Teleport VBlood boss to position
    /// </summary>
    public static bool TeleportVBlood(int guidHash, float3 position)
    {
        var entity = GetVBloodEntity(guidHash);
        if (entity == Entity.Null || !VRCore.EM.Exists(entity))
        {
            return false;
        }

        try
        {
            VRCore.EM.SetComponentData(entity, new Translation { Value = position });
            Plugin.Logger?.LogInfo($"[VBloodMapper] Teleported VBlood {guidHash} to {position}");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"[VBloodMapper] Error teleporting VBlood: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get Baby Blood GUID hash
    /// </summary>
    public static int GetBabyBloodGuid()
    {
        return -1996241419;
    }

    /// <summary>
    /// Check if GUID is Baby Blood
    /// </summary>
    public static bool IsBabyBlood(int guidHash)
    {
        return guidHash == GetBabyBloodGuid();
    }

    /// <summary>
    /// Get Baby Blood boss data
    /// </summary>
    public static VBloodBoss GetBabyBloodBoss()
    {
        return GetVBloodBoss(GetBabyBloodGuid());
    }
    */
}
}
