using System;
using System.Collections.Generic;
using Unity.Entities;
using VAuto.Services;

namespace VAuto.Core
{
    public static class DualCharacterManager
    {
        private static readonly object _lock = new();
        private static readonly Dictionary<ulong, DualCharacterState> _states = new();

        public static bool HasDualState(ulong platformId)
        {
            lock (_lock)
            {
                return _states.ContainsKey(platformId);
            }
        }

        public static DualCharacterState GetState(ulong platformId)
        {
            lock (_lock)
            {
                _states.TryGetValue(platformId, out var state);
                return state;
            }
        }

        public static DualCharacterState GetOrCreateState(ulong platformId, Entity normalCharacter)
        {
            lock (_lock)
            {
                if (!_states.TryGetValue(platformId, out var state) || state == null)
                {
                    state = new DualCharacterState
                    {
                        NormalCharacter = normalCharacter,
                        ArenaCharacter = Entity.Null,
                        IsArenaActive = false,
                        ArenaCreatedAt = DateTime.UtcNow,
                        ArenaNeedsRespawn = false,
                        LastSwapTime = DateTime.MinValue,
                        OriginalBloodType = string.Empty,
                        ArenaBloodType = "Rogue",
                        IsInitialized = false
                    };

                    _states[platformId] = state;
                    return state;
                }

                if (state.NormalCharacter == Entity.Null)
                    state.NormalCharacter = normalCharacter;

                return state;
            }
        }

        public static void ResetDualState(ulong platformId)
        {
            lock (_lock)
            {
                _states.Remove(platformId);
            }
        }

        public static void OnPlayerLogin(Entity userEntity, Entity characterEntity, ulong platformId)
        {
            try
            {
                GetOrCreateState(platformId, characterEntity);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[DualCharacterManager] Error in OnPlayerLogin: {ex.Message}");
            }
        }

        // Back-compat with older naming used by Commands/Character/CharacterCommands.cs
        public static bool SwitchToPvP(ulong platformId, Entity userEntity)
        {
            try
            {
                // TODO: Implement CharacterSwapService
                Plugin.Logger?.LogError("[DualCharacterManager] CharacterSwapService not implemented");
                return false;
                
                // Original code (commented out):
                // return CharacterSwapService.ForceActivateCharacter(platformId, userEntity, activateArena: true);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[DualCharacterManager] Error in SwitchToPvP: {ex.Message}");
                return false;
            }
        }

        public static bool SwitchToNormal(ulong platformId, Entity userEntity)
        {
            try
            {
                // TODO: Implement CharacterSwapService
                Plugin.Logger?.LogError("[DualCharacterManager] CharacterSwapService not implemented");
                return false;
                
                // Original code (commented out):
                // return CharacterSwapService.ForceActivateCharacter(platformId, userEntity, activateArena: false);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[DualCharacterManager] Error in SwitchToNormal: {ex.Message}");
                return false;
            }
        }
    }
}
