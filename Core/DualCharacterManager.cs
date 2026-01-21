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
                        PvPCharacter = Entity.Null,
                        IsPvPActive = false,
                        PvPCreatedAt = DateTime.UtcNow,
                        PvPNeedsRespawn = false,
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
            lock (_lock)
            {
                if (!_states.TryGetValue(platformId, out var state)) return false;
                if (state.PvPCharacter == Entity.Null || !VRCore.EM.Exists(state.PvPCharacter)) return false;

                // Freeze normal character
                CharacterFreezeService.Freeze(state.NormalCharacter);

                // Unfreeze PvP character
                CharacterFreezeService.Unfreeze(userEntity, state.PvPCharacter);

                // Swap positions
                if (VRCore.EM.HasComponent<Unity.Transforms.Translation>(state.NormalCharacter) &&
                    VRCore.EM.HasComponent<Unity.Transforms.Translation>(state.PvPCharacter))
                {
                    var normalPos = VRCore.EM.GetComponentData<Unity.Transforms.Translation>(state.NormalCharacter);
                    VRCore.EM.SetComponentData(state.PvPCharacter, normalPos);
                    VRCore.EM.SetComponentData(state.NormalCharacter, new Unity.Transforms.Translation { Value = new float3(-1000, 5, -500) });
                }

                state.IsPvPActive = true;
                state.LastSwapTime = DateTime.UtcNow;
                return true;
            }
        }

        public static bool SwitchToNormal(ulong platformId, Entity userEntity)
        {
            lock (_lock)
            {
                if (!_states.TryGetValue(platformId, out var state)) return false;
                if (state.PvPCharacter == Entity.Null || !VRCore.EM.Exists(state.PvPCharacter)) return false;

                // Freeze PvP character
                CharacterFreezeService.Freeze(state.PvPCharacter);

                // Unfreeze normal character
                CharacterFreezeService.Unfreeze(userEntity, state.NormalCharacter);

                // Swap positions
                if (VRCore.EM.HasComponent<Unity.Transforms.Translation>(state.NormalCharacter) &&
                    VRCore.EM.HasComponent<Unity.Transforms.Translation>(state.PvPCharacter))
                {
                    var pvpPos = VRCore.EM.GetComponentData<Unity.Transforms.Translation>(state.PvPCharacter);
                    VRCore.EM.SetComponentData(state.NormalCharacter, pvpPos);
                    VRCore.EM.SetComponentData(state.PvPCharacter, new Unity.Transforms.Translation { Value = new float3(-1000, 5, -500) });
                }

                state.IsPvPActive = false;
                state.LastSwapTime = DateTime.UtcNow;
                return true;
            }
        }
    }
}
