using System;
using System.Collections.Concurrent;
using Unity.Entities;
using Unity.Mathematics;
using VAuto.Core;
using ProjectM.Network;
using BepInEx.Logging;
using VAuto.Services;

namespace VAuto.Services
{
    /// <summary>
    /// Authoritative manager for the Dual Character system.
    /// Orchestrates login setup, character spawning, and state tracking.
    /// </summary>
    public static class DualCharacterManager
    {
        private static readonly ConcurrentDictionary<ulong, DualCharacterState> _dualStates = new();
        private const string LOG_CONTEXT = "DualCharManager";

        /// <summary>
        /// Initializes the dual character state for a player on login.
        /// Follows the AI Directive: Detect -> Initialize -> Ensure PvP -> Freeze.
        /// </summary>
        public static void OnPlayerLogin(Entity userEntity, Entity characterEntity, ulong platformId)
        {
            try
            {
                VAuto.Core.VLoggerCore.Info($"Initializing dual state for {platformId}", LOG_CONTEXT);

                // 1. Detect UserEntity + CharacterEntity (Done via parameters)

                // 2. Initialize DualCharacterState
                var state = GetOrCreateState(platformId, characterEntity);

                // 3. Ensure PvP character exists
                if (state.ArenaCharacter == Entity.Null || !VRCore.EM.Exists(state.ArenaCharacter))
                {
                    VAuto.Core.VLoggerCore.Info($"PvP character missing for {platformId}, spawning new one...", LOG_CONTEXT);
                    state.ArenaCharacter = PvPCharacterSpawner.SpawnPvPCharacter(userEntity, platformId);
                    state.ArenaCreatedAt = DateTime.UtcNow;
                }

                // 4. Freeze PvP character until used (Handled by Spawner, but ensuring here)
                if (!state.IsArenaActive)
                {
                    CharacterFreezeService.Freeze(state.ArenaCharacter);
                }

                state.IsInitialized = true;
                VAuto.Core.VLoggerCore.Info($"Dual character state ready for platformId {platformId}", LOG_CONTEXT);
            }
            catch (Exception ex)
            {
                VAuto.Core.VLoggerCore.Error("Error during player login setup", ex, LOG_CONTEXT);
            }
        }

        public static DualCharacterState GetOrCreateState(ulong platformId, Entity normalCharacter)
        {
            return _dualStates.GetOrAdd(platformId, _ => new DualCharacterState
            {
                NormalCharacter = normalCharacter,
                ArenaCharacter = Entity.Null,
                IsArenaActive = false,
                IsInitialized = false
            });
        }

        public static DualCharacterState GetState(ulong platformId)
        {
            return _dualStates.TryGetValue(platformId, out var state) ? state : null;
        }

        public static bool HasDualState(ulong platformId)
        {
            return _dualStates.ContainsKey(platformId);
        }

        /// <summary>
        /// Legacy support for PvP naming
        /// </summary>
        public static bool IsPvPActive(ulong platformId)
        {
            var state = GetState(platformId);
            return state != null && state.IsArenaActive;
        }

        public static void ResetDualState(ulong platformId)
        {
            if (_dualStates.TryRemove(platformId, out var state))
            {
                if (state.ArenaCharacter != Entity.Null && VRCore.EM.Exists(state.ArenaCharacter))
                {
                    VRCore.EM.DestroyEntity(state.ArenaCharacter);
                }
                VAuto.Core.VLoggerCore.Info($"Reset dual state for {platformId}", LOG_CONTEXT);
            }
        }

        public static bool SwitchToPvP(ulong platformId, Entity userEntity)
        {
            var state = GetState(platformId);
            if (state == null) return false;
            state.IsArenaActive = true;
            return true;
        }

        public static bool SwitchToNormal(ulong platformId, Entity userEntity)
        {
            var state = GetState(platformId);
            if (state == null) return false;
            state.IsArenaActive = false;
            return true;
        }
    }
}