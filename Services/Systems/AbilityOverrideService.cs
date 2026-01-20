using System;
using System.Collections.Generic;
using Unity.Entities;
using ProjectM;
using Stunlock.Core;
using VAuto.Core;
using VAuto.Services.Interfaces;

namespace VAuto.Services.Systems
{
    public class AbilityOverrideService : IService
    {
        private static AbilityOverrideService _instance;
        public static AbilityOverrideService Instance => _instance ??= new AbilityOverrideService();

        private bool _isInitialized = false;
        private readonly Dictionary<ulong, AbilityState> _playerAbilities = new();

        public bool IsInitialized => _isInitialized;
        public BepInEx.Logging.ManualLogSource Log => Plugin.Logger;

        public void Initialize()
        {
            if (_isInitialized) return;
            Log?.LogInfo("[AbilityOverride] Initializing ability override service");
            _isInitialized = true;
        }

        public void Cleanup()
        {
            _playerAbilities.Clear();
            _isInitialized = false;
        }

        public bool UnlockAllAbilities(Entity userEntity, Entity character)
        {
            try
            {
                if (!VAuto.Core.Core.TryRead<User>(userEntity, out var user)) return false;
                var platformId = user.PlatformId;

                Log?.LogInfo($"[AbilityOverride] Unlocking all abilities and spellbook for {platformId}");

                // Use VBlood unlock system to enable all abilities and spellbook
                var success = VAuto.Core.VBloodMapper.VBloodUnlockSystem.EnableVBloodUnlockMode(character);
                
                if (success)
                {
                    // Also try to open the spellbook UI
                    VAuto.Core.VBloodMapper.VBloodUnlockSystem.OpenSpellbookUI(character);
                    
                    // Store original state
                    _playerAbilities[platformId] = new AbilityState
                    {
                        IsOverridden = true,
                        OverrideTime = DateTime.UtcNow
                    };

                    Log?.LogInfo($"[AbilityOverride] Successfully unlocked complete spellbook for {platformId}");
                    return true;
                }
                else
                {
                    Log?.LogError($"[AbilityOverride] Failed to enable VBlood unlock mode for {platformId}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[AbilityOverride] Error unlocking abilities: {ex.Message}");
                return false;
            }
        }

        private int[] GetAllShapeshiftForms()
        {
            return new[]
            {
                -1843569636, // Bat Form
                -1936323368, // Wolf Form
                -1007062401, // Rat Form
                1163490655,  // Human Form
                -880131926   // Bear Form
            };
        }

        public bool RestoreAbilities(Entity userEntity, Entity character)
        {
            try
            {
                if (!VAuto.Core.Core.TryRead<User>(userEntity, out var user)) return false;
                var platformId = user.PlatformId;

                if (!_playerAbilities.ContainsKey(platformId)) return true;

                Log?.LogInfo($"[AbilityOverride] Restoring original abilities and spellbook for {platformId}");

                // Disable VBlood unlock mode to restore original state
                var success = VAuto.Core.VBloodMapper.VBloodUnlockSystem.DisableVBloodUnlockMode(character);
                
                if (success)
                {
                    // Remove override state
                    _playerAbilities.Remove(platformId);

                    Log?.LogInfo($"[AbilityOverride] Successfully restored original spellbook for {platformId}");
                    return true;
                }
                else
                {
                    Log?.LogError($"[AbilityOverride] Failed to disable VBlood unlock mode for {platformId}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[AbilityOverride] Error restoring abilities: {ex.Message}");
                return false;
            }
        }

        private int[] GetAllVBloodAbilities()
        {
            return new[]
            {
                // Chaos abilities
                -1576592687, // Chaos Volley
                -880131926,  // Chaos Barrier
                -1003607124, // Chaos Vortex
                1163490655,  // Power Surge
                
                // Blood abilities
                -1055766373, // Blood Rage
                -1266285883, // Sanguine Coil
                -1828387635, // Blood Rite
                1936301176,  // Crimson Beam
                
                // Frost abilities
                -1829944323, // Ice Nova
                -1007062401, // Frost Bat
                -1535981450, // Crystal Lance
                -1890538688, // Ice Block
                
                // Unholy abilities
                -1905498927, // Corpse Explosion
                -1413304449, // Ward of the Damned
                -1576592687, // Volatile Arachnid
                -1007062401, // Wraith Spear
                
                // Illusion abilities
                -1266285883, // Spectral Wolf
                -1055766373, // Mist Trance
                -880131926,  // Mirror Strike
                1163490655,  // Phantom Aegis
                
                // Storm abilities
                -1003607124, // Lightning Wall
                -1828387635, // Cyclone
                -1007062401, // Ball Lightning
                1936301176,  // Discharge
                
                // Spellbook - Basic Spells
                -1905498927, // Shadowbolt
                -1413304449, // Blood Fountain
                1163490655,  // Frost Bolt
                -1890538688, // Bone Spirit
                
                // Spellbook - Advanced Spells
                -1576592687, // Purgatory
                -880131926,  // Aftershock
                -1003607124, // Void
                -1055766373, // Spectral Guardian
                
                // Spellbook - Ultimate Spells
                -1266285883, // Crimson Meteor
                -1828387635, // Arctic Leap
                -1007062401, // Summon Fallen Angel
                1936301176,  // Chaos Barrage
                
                // Spellbook - Utility Spells
                -1829944323, // Veil of Blood
                -1535981450, // Veil of Frost
                -1905498927, // Veil of Chaos
                -1413304449, // Veil of Bones
                
                // Spellbook - Movement Spells
                1163490655,  // Bat Form
                -1890538688, // Wolf Form
                -1576592687, // Rat Form
                -880131926,  // Human Form
                
                // Spellbook - Defensive Spells
                -1003607124, // Blood Barrier
                -1055766373, // Frost Barrier
                -1266285883, // Chaos Shield
                -1828387635  // Bone Shield
            };
        }

        public class AbilityState
        {
            public bool IsOverridden { get; set; }
            public DateTime OverrideTime { get; set; }
        }

        /// <summary>
        /// Check if a player's abilities are currently overridden (unlock active)
        /// </summary>
        public bool IsPlayerOverridden(ulong platformId)
        {
            return _playerAbilities.TryGetValue(platformId, out var state) && state.IsOverridden;
        }
    }
}
