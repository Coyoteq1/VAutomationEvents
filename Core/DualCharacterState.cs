using System;
using Unity.Entities;
using Unity.Mathematics;
using Il2CppInterop.Runtime;
using VAuto.Core;
using VAuto.Services;
using System.Collections.Generic;
using System.Threading;
using ProjectM.Shared;

namespace VAuto.Core
{
    /// <summary>
    /// Marker component for PvP practice characters
    /// </summary>
    public struct PvPPracticeTag { }

    /// <summary>
    /// Marker component for frozen characters
    /// </summary>
    public struct FrozenCharacterTag { }

    /// <summary>
    /// State tracking for dual character system per player
    /// </summary>
    public class DualCharacterState
    {
        public Entity NormalCharacter;           // PlayerName (original)
        public Entity ArenaCharacter;            // PlayerNamePvP (arena character)
        public bool IsArenaActive;               // Which character is currently active
        public float3 LastNormalPosition;        // Where to return normal character
        public DateTime ArenaCreatedAt;          // When arena character was created
        public bool ArenaNeedsRespawn;           // Flag for recreation after restart
        public DateTime LastSwapTime;            // Last time characters were swapped
        public string OriginalBloodType;         // Normal character's blood type
        public string ArenaBloodType;            // Arena character's blood type (default: Rogue)
        public bool IsInitialized;               // Whether dual state is fully initialized

        // Legacy properties for backward compatibility
        public Entity PvPCharacter { get => ArenaCharacter; set => ArenaCharacter = value; }
        public bool IsPvPActive { get => IsArenaActive; set => IsArenaActive = value; }
        public DateTime PvPCreatedAt { get => ArenaCreatedAt; set => ArenaCreatedAt = value; }
        public bool PvPNeedsRespawn { get => ArenaNeedsRespawn; set => ArenaNeedsRespawn = value; }
    }

    /// <summary>
    /// Global dual character manager
    /// </summary>
    public static class DualCharacterManager
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, DualCharacterState> _dualStates = new();

        /// <summary>
        /// Get or create dual state for a player
        /// </summary>
        public static DualCharacterState GetOrCreateState(ulong platformId, Entity normalCharacter)
        {
            return _dualStates.GetOrAdd(platformId, _ =>
            {
                var state = new DualCharacterState
                {
                    NormalCharacter = normalCharacter,
                    PvPCharacter = Entity.Null,
                    IsPvPActive = false,
                    PvPNeedsRespawn = true
                };

                // Get initial position
                var em = VRCore.EM;
                if (em.TryGetComponentData(normalCharacter, out Unity.Transforms.Translation translation))
                {
                    state.LastNormalPosition = translation.Value;
                }
                else if (em.TryGetComponentData(normalCharacter, out Unity.Transforms.LocalToWorld ltw))
                {
                    state.LastNormalPosition = ltw.Position;
                }

                VAuto.Plugin.Logger?.LogInfo($"Created dual character state for platformId {platformId}");
                return state;
            });
        }

        /// <summary>
        /// Get existing dual state
        /// </summary>
        public static DualCharacterState GetState(ulong platformId)
        {
            return _dualStates.TryGetValue(platformId, out var state) ? state : null;
        }

        /// <summary>
        /// Check if player has dual character setup
        /// </summary>
        public static bool HasDualState(ulong platformId)
        {
            return _dualStates.ContainsKey(platformId);
        }

        /// <summary>
        /// Set PvP character for a player
        /// </summary>
        public static void SetPvPCharacter(ulong platformId, Entity pvpCharacter)
        {
            if (_dualStates.TryGetValue(platformId, out var state))
            {
                state.PvPCharacter = pvpCharacter;
                state.PvPCreatedAt = DateTime.UtcNow;
                state.PvPNeedsRespawn = false;
                VAuto.Plugin.Logger?.LogInfo($"Set PvP character for platformId {platformId}: {pvpCharacter.Index}");
            }
        }

        /// <summary>
        /// Mark PvP character as needing respawn (after restart)
        /// </summary>
        public static void MarkPvPNeedsRespawn(ulong platformId)
        {
            if (_dualStates.TryGetValue(platformId, out var state))
            {
                state.PvPNeedsRespawn = true;
                state.PvPCharacter = Entity.Null;
            }
        }

        /// <summary>
        /// Check if PvP character needs respawn
        /// </summary>
        public static bool PvPNeedsRespawn(ulong platformId)
        {
            return _dualStates.TryGetValue(platformId, out var state) && state.PvPNeedsRespawn;
        }

        /// <summary>
        /// Update normal character position when not in PvP mode
        /// </summary>
        public static void UpdateNormalPosition(ulong platformId, float3 position)
        {
            if (_dualStates.TryGetValue(platformId, out var state) && !state.IsPvPActive)
            {
                state.LastNormalPosition = position;
            }
        }

        /// <summary>
        /// Switch to PvP character
        /// </summary>
        public static bool SwitchToPvP(ulong platformId, Entity userEntity)
        {
            if (!_dualStates.TryGetValue(platformId, out var state))
                return false;

            if (state.PvPCharacter == Entity.Null || !VRCore.EM.Exists(state.PvPCharacter))
                return false;

            try
            {
                // Freeze normal character
                FreezeCharacter(state.NormalCharacter);

                // Activate PvP character
                ActivateCharacter(userEntity, state.PvPCharacter);

                // Teleport to arena spawn
                var spawnPos = MissingServices.ZoneService.Center + new float3(0, 2f, 0);
                MissingServices.TeleportFix.FastTeleport(state.PvPCharacter, spawnPos);

                state.IsPvPActive = true;
                VAuto.Plugin.Logger?.LogInfo($"Switched to PvP character for platformId {platformId}");
                return true;
            }
            catch (Exception ex)
            {
                VAuto.Plugin.Logger?.LogError($"Failed to switch to PvP for platformId {platformId}: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Switch to normal character
        /// </summary>
        public static bool SwitchToNormal(ulong platformId, Entity userEntity)
        {
            if (!_dualStates.TryGetValue(platformId, out var state))
                return false;

            try
            {
                // Freeze PvP character
                FreezeCharacter(state.PvPCharacter);

                // Activate normal character
                ActivateCharacter(userEntity, state.NormalCharacter);

                // Teleport back to last normal position
                MissingServices.TeleportFix.FastTeleport(state.NormalCharacter, state.LastNormalPosition);

                state.IsPvPActive = false;
                VAuto.Plugin.Logger?.LogInfo($"Switched to normal character for platformId {platformId}");
                return true;
            }
            catch (Exception ex)
            {
                VAuto.Plugin.Logger?.LogError($"Failed to switch to normal for platformId {platformId}: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Freeze a character (remove from network and teleport underground)
        /// </summary>
        public static void FreezeCharacter(Entity character)
        {
            if (character == Entity.Null || !VRCore.EM.Exists(character))
                return;

            var em = VRCore.EM;

            try
            {
                // Remove network component
                if (em.HasComponent<ProjectM.Network.FromCharacter>(character))
                    em.RemoveComponent<ProjectM.Network.FromCharacter>(character);

                // Teleport underground to hidden location
                var hiddenLocation = new float3(-1000, 5, -500); // Underground
                MissingServices.TeleportFix.FastTeleport(character, hiddenLocation);

                // Add frozen tag - use safer approach
                var frozenType = new ComponentType(Il2CppType.Of<FrozenCharacterTag>());
                if (!em.HasComponent(character, frozenType))
                {
                    em.AddComponent(character, frozenType);
                }
            }
            catch (System.Exception ex)
            {
                VAuto.Plugin.Logger?.LogWarning($"Failed to freeze character: {ex.Message}");
            }
        }

        /// <summary>
        /// Activate a character (add to network and remove frozen tag)
        /// </summary>
        public static void ActivateCharacter(Entity user, Entity character)
        {
            if (character == Entity.Null || !VRCore.EM.Exists(character))
                return;

            var em = VRCore.EM;

            try
            {
                // Add network component
                if (!em.HasComponent<ProjectM.Network.FromCharacter>(character))
                {
                    em.AddComponentData(character, new ProjectM.Network.FromCharacter { User = user });
                }

                // Remove frozen tag
                if (em.HasComponent<FrozenCharacterTag>(character))
                {
                    em.RemoveComponent<FrozenCharacterTag>(character);
                }
            }
            catch (System.Exception ex)
            {
                VAuto.Plugin.Logger?.LogWarning($"Failed to activate character: {ex.Message}");
            }
        }

        /// <summary>
        /// Clean up orphaned PvP characters
        /// </summary>
        public static void CleanupOrphanedPvPCharacters()
        {
            try
            {
                var em = VRCore.EM;
                var query = em.CreateEntityQuery(
                    ComponentType.ReadOnly<PvPPracticeTag>(),
                    ComponentType.ReadOnly<ProjectM.Network.User>()
                );

                var pvpChars = query.ToEntityArray(Unity.Collections.Allocator.Temp);

                try
                {
                    foreach (var pvpChar in pvpChars)
                    {
                        if (!em.Exists(pvpChar)) continue;

                        // Check if this PvP character belongs to an active player
                        if (em.TryGetComponentData(pvpChar, out ProjectM.Network.User user))
                        {
                            var platformId = user.PlatformId;

                            if (_dualStates.TryGetValue(platformId, out var state))
                            {
                                // If this isn't the active PvP character for this player, freeze it
                                if (state.PvPCharacter != pvpChar)
                                {
                                    FreezeCharacter(pvpChar);
                                    VAuto.Plugin.Logger?.LogInfo($"Froze orphaned PvP character for platformId {platformId}");
                                }
                            }
                            else
                            {
                                // No state for this player, freeze orphaned PvP character
                                FreezeCharacter(pvpChar);
                                VAuto.Plugin.Logger?.LogInfo($"Froze orphaned PvP character for inactive platformId {platformId}");
                            }
                        }
                    }
                }
                finally
                {
                    pvpChars.Dispose();
                }
            }
            catch (Exception ex)
            {
                VAuto.Plugin.Logger?.LogError($"Error during PvP character cleanup: {ex}");
            }
        }

        /// <summary>
        /// Get all dual states (for admin commands)
        /// </summary>
        public static System.Collections.Concurrent.ConcurrentDictionary<ulong, DualCharacterState> GetAllDualStates()
        {
            return _dualStates;
        }

        /// <summary>
        /// Check if a character is a PvP practice character
        /// </summary>
        public static bool IsPvPPracticeCharacter(Entity character)
        {
            return character != Entity.Null &&
                   VRCore.EM.Exists(character) &&
                   VRCore.EM.HasComponent<PvPPracticeTag>(character);
        }

        /// <summary>
        /// Get platform ID from character entity
        /// </summary>
        public static ulong GetPlatformIdFromCharacter(Entity character)
        {
            if (character == Entity.Null || !VRCore.EM.Exists(character))
                return 0;

            // Try to find via user component
            var em = VRCore.EM;
            if (em.TryGetComponentData(character, out ProjectM.Network.User user))
            {
                return user.PlatformId;
            }

            // Try to find via reverse lookup in dual states
            foreach (var kvp in _dualStates)
            {
                if (kvp.Value.NormalCharacter == character || kvp.Value.PvPCharacter == character)
                {
                    return kvp.Key;
                }
            }

            return 0;
        }

        /// <summary>
        /// Reset dual character system for a player
        /// </summary>
        public static void ResetDualState(ulong platformId)
        {
            if (_dualStates.TryRemove(platformId, out var state))
            {
                try
                {
                    // Freeze and cleanup arena character if it exists
                    if (state.ArenaCharacter != Entity.Null && VRCore.EM.Exists(state.ArenaCharacter))
                    {
                        FreezeCharacter(state.ArenaCharacter);
                        // Note: In a full implementation, we might want to destroy the character entity
                        // But for now, just freeze it
                    }

                    VAuto.Plugin.Logger?.LogInfo($"Reset dual character state for platformId {platformId}");
                }
                catch (Exception ex)
                {
                    VAuto.Plugin.Logger?.LogError($"Error during dual state reset for platformId {platformId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Initialize dual state with arena character
        /// </summary>
        public static void InitializeArenaState(ulong platformId, Entity normalCharacter, Entity arenaCharacter)
        {
            var state = GetOrCreateState(platformId, normalCharacter);
            state.ArenaCharacter = arenaCharacter;
            state.ArenaCreatedAt = DateTime.UtcNow;
            state.ArenaNeedsRespawn = false;
            state.LastSwapTime = DateTime.UtcNow;
            state.ArenaBloodType = "Rogue"; // Default arena blood type
            state.IsInitialized = true;

            VAuto.Plugin.Logger?.LogInfo($"Initialized arena dual state for platformId {platformId}");
        }
    }
}
