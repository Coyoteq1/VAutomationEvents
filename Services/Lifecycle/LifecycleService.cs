using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VAuto.Core;
using VAuto.Data;
using VAuto.Services.Interfaces;
using VAuto.Services.Systems;
using VAuto.UI;
using BepInEx.Logging;

namespace VAuto.Services.Lifecycle
{
    /// <summary>
    /// PLAYER LIFECYCLE SERVICE - Manages player snapshot/restore operations only.
    /// MUST NOT: Spawn glow/buildings, track zones, manage visual elements.
    /// Lifecycle = pure player state management.
    /// </summary>
    public sealed class LifecycleService : IArenaLifecycleService, IService
    {
        private const string BaseDir = "BepInEx/config/VAuto.Arena";
        private const string PlayersDir = "players";
        private const string JsonConfigPath = "Snapshot";
        private const float POSITION_CHECK_INTERVAL = 3.0f;

        private bool _isInitialized = false;
        private ManualLogSource _log;

        // State tracking
        private readonly ConcurrentDictionary<ulong, PlayerState> _playerStates = new();
        private readonly ConcurrentDictionary<ulong, byte> _vbloodHookedPlayers = new();

        // Per-player override tracking for debug-style unlocks moved from AchievementUnlockService
        private readonly HashSet<ulong> _overrideUnlocks = new();
        /// <summary>
        /// When true, all ForceUnlock operations will run regardless of per-player flags
        /// </summary>
        public bool GlobalOverrideUnlocks { get; set; } = false;

        public bool IsOverrideEnabled(ulong platformId)
        {
            return GlobalOverrideUnlocks || _overrideUnlocks.Contains(platformId);
        }

        public bool SetOverrideUnlock(ulong platformId, bool enabled)
        {
            try
            {
                if (enabled)
                {
                    _overrideUnlocks.Add(platformId);
                    _log?.LogInfo($"[Lifecycle] Override unlock enabled for {platformId}");
                }
                else
                {
                    _overrideUnlocks.Remove(platformId);
                    _log?.LogInfo($"[Lifecycle] Override unlock disabled for {platformId}");
                }
                return true;
            }
            catch (Exception ex)
            {
                _log?.LogError($"[Lifecycle] Failed to set override for {platformId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Force an unlock using DebugEventsSystem reflection when override enabled; if not enabled, falls back to normal unlock path
        /// </summary>
        public bool ForceUnlockIfEnabled(ulong platformId, Entity userEntity)
        {
            try
            {
                if (IsOverrideEnabled(platformId))
                {
                    return ForceUnlockAll(platformId, userEntity);
                }

                // Normal fallback unlock path (placeholder)
                _log?.LogInfo($"[Lifecycle] UnlockAllAchievements fallback for {platformId}");
                return true;
            }
            catch (Exception ex)
            {
                _log?.LogError($"[Lifecycle] ForceUnlockIfEnabled failed: {ex.Message}");
                return false;
            }
        }

        private bool ForceUnlockAll(ulong platformId, Entity userEntity)
        {
            try
            {
                _log?.LogInfo($"[Lifecycle] Forcing debug unlocks for {platformId}");

                try
                {
                    var debugSys = VRisingCore.DebugEventsSystem;
                    if (debugSys != null)
                    {
                        var dbgType = debugSys.GetType();
                        void TryInvoke(string methodName)
                        {
                            try
                            {
                                var method = dbgType.GetMethod(methodName);
                                if (method == null) return;

                                var parameters = method.GetParameters();
                                if (parameters.Length == 0)
                                {
                                    method.Invoke(debugSys, null);
                                    _log?.LogInfo($"[Lifecycle] Invoked DebugEventsSystem.{methodName}()");
                                }
                                else if (parameters.Length == 1)
                                {
                                    var pType = parameters[0].ParameterType;
                                    if (pType == typeof(Entity) && userEntity != Entity.Null)
                                    {
                                        method.Invoke(debugSys, new object[] { userEntity });
                                        _log?.LogInfo($"[Lifecycle] Invoked DebugEventsSystem.{methodName}(Entity)");
                                    }
                                    else if (pType == typeof(ulong) || pType == typeof(System.UInt64))
                                    {
                                        method.Invoke(debugSys, new object[] { platformId });
                                        _log?.LogInfo($"[Lifecycle] Invoked DebugEventsSystem.{methodName}(ulong)");
                                    }
                                }
                            }
                            catch (Exception mex)
                            {
                                _log?.LogWarning($"[Lifecycle] Failed to invoke DebugEventsSystem.{methodName}: {mex.Message}");
                            }
                        }

                        TryInvoke("UnlockAllResearch");
                        TryInvoke("CompleteAllAchievements");
                        TryInvoke("UnlockAllProgression");
                        TryInvoke("OpenSpellbook");
                    }
                }
                catch (Exception ex)
                {
                    _log?.LogWarning($"[Lifecycle] DebugEventsSystem invocation failed: {ex.Message}");
                }

                // Track override state (ensure revert later)
                _overrideUnlocks.Add(platformId);
                return true;
            }
            catch (Exception ex)
            {
                _log?.LogError($"[Lifecycle] ForceUnlockAll failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// If override was applied, attempt to call reset/revoke methods on DebugEventsSystem and then remove server-side unlocks
        /// </summary>
        public bool RemoveForcedUnlocksIfEnabled(ulong platformId, Entity userEntity)
        {
            try
            {
                if (!IsOverrideEnabled(platformId))
                {
                    // No special override - nothing to do
                    return true;
                }

                _log?.LogInfo($"[Lifecycle] Removing forced unlocks for {platformId}");

                try
                {
                    var debugSys = VRisingCore.DebugEventsSystem;
                    if (debugSys != null)
                    {
                        var dbgType = debugSys.GetType();
                        string[] resetMethods = new[] {
                            "ResetAllResearch",
                            "ResetResearch",
                            "ResetAllAchievements",
                            "ResetAchievements",
                            "ResetAllProgression",
                            "ResetProgression",
                            "RevokeAllResearch",
                            "RevokeAllAchievements"
                        };

                        foreach (var methodName in resetMethods)
                        {
                            try
                            {
                                var method = dbgType.GetMethod(methodName);
                                if (method == null) continue;

                                var parameters = method.GetParameters();
                                if (parameters.Length == 0)
                                {
                                    method.Invoke(debugSys, null);
                                    _log?.LogInfo($"[Lifecycle] Invoked DebugEventsSystem.{methodName}()");
                                }
                                else if (parameters.Length == 1)
                                {
                                    var pType = parameters[0].ParameterType;
                                    if (pType == typeof(Entity) && userEntity != Entity.Null)
                                    {
                                        method.Invoke(debugSys, new object[] { userEntity });
                                        _log?.LogInfo($"[Lifecycle] Invoked DebugEventsSystem.{methodName}(Entity)");
                                    }
                                    else if (pType == typeof(ulong) || pType == typeof(System.UInt64))
                                    {
                                        method.Invoke(debugSys, new object[] { platformId });
                                        _log?.LogInfo($"[Lifecycle] Invoked DebugEventsSystem.{methodName}(ulong)");
                                    }
                                }
                            }
                            catch (Exception mex)
                            {
                                _log?.LogWarning($"[Lifecycle] Failed to invoke DebugEventsSystem.{methodName}: {mex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log?.LogWarning($"[Lifecycle] DebugEventsSystem reset invocation failed: {ex.Message}");
                }

                // Remove override tracking
                _overrideUnlocks.Remove(platformId);
                return true;
            }
            catch (Exception ex)
            {
                _log?.LogError($"[Lifecycle] RemoveForcedUnlocksIfEnabled failed: {ex.Message}");
                return false;
            }
        }

        private static void FastTeleport(Entity entity, float3 position)
        {
            try
            {
                var em = VAuto.Core.Core.EntityManager;
                if (entity == Entity.Null)
                {
                    Plugin.Logger?.LogError("[Lifecycle] FastTeleport called with null entity");
                    return;
                }

                if (!em.Exists(entity))
                {
                    Plugin.Logger?.LogError($"[Lifecycle] FastTeleport called with non-existent entity {entity.Index}:{entity.Version}");
                    return;
                }

                // Validate entity is from the correct world
                if (entity.Index >= em.EntityCapacity)
                {
                    Plugin.Logger?.LogError($"[Lifecycle] Entity {entity.Index} is from a different world (capacity: {em.EntityCapacity})");
                    return;
                }

                if (em.HasComponent<Unity.Transforms.Translation>(entity))
                    em.SetComponentData(entity, new Unity.Transforms.Translation { Value = position });
                else
                    em.AddComponentData(entity, new Unity.Transforms.Translation { Value = position });

                Plugin.Logger?.LogDebug($"[Lifecycle] Successfully teleported entity {entity.Index} to {position}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[Lifecycle] Error in FastTeleport for entity {entity.Index}: {ex.Message}");
            }
        }
        private readonly object _snapshotLock = new();

        // Arena zone service integration
        private static ArenaZoneService ZoneService => ArenaZoneService.Instance;

        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => _log;

        public void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                _log = Plugin.Logger;
                var playersDir = Path.Combine(BaseDir, PlayersDir);
                if (!Directory.Exists(playersDir))
                {
                    Directory.CreateDirectory(playersDir);
                }
                
                _isInitialized = true;
                _log?.LogInfo("[LifecycleService] Lifecycle service initialized successfully.");
            }
            catch (Exception ex)
            {
                _log?.LogError($"[LifecycleService] Failed to initialize: {ex}");
                throw;
            }
        }

        public void Cleanup()
        {
            if (!_isInitialized) return;

            try
            {
                _playerStates.Clear();
                _vbloodHookedPlayers.Clear();
                _isInitialized = false;
                _log?.LogInfo("[LifecycleService] Lifecycle service cleaned up.");
            }
            catch (Exception ex)
            {
                _log?.LogError($"[LifecycleService] Error during cleanup: {ex}");
            }
        }

        public void Update(float deltaTime)
        {
            // LifecycleService no longer handles position monitoring
            // This is now handled by ArenaProximitySystem
        }

        public bool OnPlayerEnter(Entity user, Entity character, string arenaId)
        {
            return EnterArena(user, character, arenaId);
        }

        public bool OnPlayerExit(Entity user, Entity character, string arenaId)
        {
            return ExitArena(user, character);
        }

        // Events for UI and other systems to react to player lifecycle changes
        public event Action<ulong> OnPlayerEnterArena;
        public event Action<ulong> OnPlayerExitArena;

        private void RaisePlayerEnterEvent(ulong platformId)
        {
            try { OnPlayerEnterArena?.Invoke(platformId); } catch (Exception ex) { _log?.LogWarning($"[Lifecycle] OnPlayerEnterArena handler failed: {ex.Message}"); }
        }

        private void RaisePlayerExitEvent(ulong platformId)
        {
            try { OnPlayerExitArena?.Invoke(platformId); } catch (Exception ex) { _log?.LogWarning($"[Lifecycle] OnPlayerExitArena handler failed: {ex.Message}"); }
        }

        public bool OnArenaStart(string arenaId) => true;
        public bool OnArenaEnd(string arenaId) => true;
        public bool OnBuildStart(Entity user, string structureName, string arenaId) => true;
        public bool OnBuildComplete(Entity user, string structureName, string arenaId) => true;
        public bool OnBuildDestroy(Entity user, string structureName, string arenaId) => true;

        /// <summary>
        /// Check if player is currently in arena lifecycle.
        /// </summary>
        public bool IsPlayerInArena(ulong platformId)
        {
            return _playerStates.ContainsKey(platformId);
        }

        public bool EnterArena(Entity userEntity, Entity character, string arenaId)
        {
            if (!VAuto.Core.Core.Exists(userEntity)) return false;
            if (!VAuto.Core.Core.TryRead<User>(userEntity, out var user)) return false;

            var platformId = user.PlatformId;
            if (_playerStates.ContainsKey(platformId)) return false;

            try
            {
                // Get server-side character entity from User component
                var serverCharacter = user.LocalCharacter._Entity;
                if (!VAuto.Core.Core.Exists(serverCharacter))
                {
                    _log?.LogError($"[Lifecycle] Server character entity does not exist for {platformId}");
                    return false;
                }

                // 1. Validate player state
                float3 originalPosition = GetEntityPosition(serverCharacter);

                // 2. Create player snapshot
                _log?.LogInfo($"[Lifecycle] Capturing snapshot for {platformId}");
                try
                {
                    if (!EnhancedArenaSnapshotService.CreateSnapshot(userEntity, serverCharacter, "practice"))
                    {
                        _log?.LogError($"[Lifecycle] Failed to capture snapshot for {platformId}");
                        return false;
                    }
                }
                catch (Exception snapshotEx)
                {
                    _log?.LogWarning($"[Lifecycle] Snapshot capture error (non-critical): {snapshotEx.Message}");
                    // Continue anyway - snapshot is not critical for arena entry
                }

                // 3. Register player state
                _playerStates[platformId] = new PlayerState
                {
                    Character = serverCharacter,
                    UserEntity = userEntity,
                    EnteredAt = DateTime.UtcNow,
                    OriginalPosition = originalPosition
                };

                // 4. Add player to zone (triggers zone activation if needed)
                ZoneService.AddPlayerToZone(0, platformId); // Use arena ID 0 for default

                // 5. Unlock VBloods
                ApplyVBloodHook(platformId);

                // 5a. Unlock all abilities for PvP arena
                AbilityOverrideService.Instance.UnlockAllAbilities(userEntity, serverCharacter);

                // 5b. Additionally make sure VBlood unlock system is enabled for full spell access
                VAuto.Core.VBloodMapper.VBloodUnlockSystem.EnableVBloodUnlockMode(serverCharacter);
                
                // 5c. Force open the spellbook UI to make spells visible
                VAuto.Core.VBloodMapper.VBloodUnlockSystem.OpenSpellbookUI(serverCharacter);

                // 5d. Force unlocks are handled by LifecycleService (moved from AchievementUnlockService)
                try
                {
                    ForceUnlockIfEnabled(platformId, userEntity);
                }
                catch (Exception ex)
                {
                    _log?.LogWarning($"[Lifecycle] Failed to perform forced unlocks: {ex.Message}");
                }

                // 6. Apply PvP identity changes
                _log?.LogInfo($"[Lifecycle] Applying PvP identity for {platformId}");

                // Change player name to [PvP] prefix (Rule 5.2)
                if (VAuto.Core.Core.TryRead<User>(userEntity, out var userData))
                {
                    var originalName = userData.CharacterName.ToString();
                    var pvpName = $"[PvP]{originalName}";
                    userData.CharacterName = new FixedString64Bytes(pvpName);
                    VAuto.Core.Core.Write(userEntity, userData);
                    _log?.LogInfo($"[Lifecycle] Changed player name from '{originalName}' to '{pvpName}'");
                }

                // Override blood type (Rule 5.2)
                if (VAuto.Core.Core.TryRead<ProjectM.Blood>(serverCharacter, out var blood))
                {
                    blood.BloodType = new PrefabGUID(-1464869978); // PvP blood type from config
                    blood.Quality = 100.0f; // Max quality for PvP
                    VAuto.Core.Core.Write(serverCharacter, blood);
                    _log?.LogInfo($"[Lifecycle] Set PvP blood type for {platformId}");
                }

                // 7. Apply arena gear
                _log?.LogInfo($"[Lifecycle] Applying arena gear for {platformId}");
                if (!ArenaBuildService.ApplyBuild(serverCharacter, "Dracula_Scholar"))
                {
                    _log?.LogWarning($"[Lifecycle] Failed to apply arena gear for {platformId}");
                }

                // 8. Teleport to arena center
                var arenaCenter = ZoneService.GetZoneState(0)?.Zone.Center ?? new float3(-1000f, 5f, -500f);
                _log?.LogInfo($"[Lifecycle] Teleporting {platformId} to arena center {arenaCenter}");
                FastTeleport(serverCharacter, arenaCenter);

                // 9. Show arena UI
                ArenaUIManager.ShowArenaUI();

                // 10. Show ability slots UI
                try
                {
                    var abilityUI = VAuto.UI.AbilitySlotUI.Instance;
                    abilityUI.OpenAbilitySlots(platformId);
                }
                catch (Exception ex)
                {
                    _log?.LogWarning($"[Lifecycle] Failed to open ability slots UI: {ex.Message}");
                }

                // Raise event for other systems
                RaisePlayerEnterEvent(platformId);
                
                _log?.LogInfo($"[Lifecycle] Player {platformId} entered arena successfully");
                return true;
            }
            catch (Exception ex)
            {
                _log?.LogError($"[Lifecycle] Error during arena entry for {platformId}: {ex}");
                return false;
            }
        }

        public bool ExitArena(Entity userEntity, Entity character)
        {
            if (!VAuto.Core.Core.Exists(userEntity)) return false;
            if (!VAuto.Core.Core.TryRead<User>(userEntity, out var user)) return false;

            var platformId = user.PlatformId;
            if (!_playerStates.TryRemove(platformId, out var state)) return false;

            try
            {
                // Get server-side character entity from User component
                var serverCharacter = user.LocalCharacter._Entity;
                if (!VAuto.Core.Core.Exists(serverCharacter))
                {
                    _log?.LogError($"[Lifecycle] Server character entity does not exist for {platformId}");
                    return false;
                }

                // 1. Clear PvP inventory (Rule 7.2: Restoration order)
                _log?.LogInfo($"[Lifecycle] Clearing PvP inventory for {platformId}");
                // PvP inventory is already cleared by snapshot restoration

                // 2. Restore snapshot inventory
                _log?.LogInfo($"[Lifecycle] Restoring snapshot for {platformId}");
                if (!EnhancedArenaSnapshotService.RestoreSnapshot(platformId.ToString(), "practice"))
                {
                    _log?.LogError($"[Lifecycle] Failed to restore snapshot for {platformId}");
                }

                // 3. Player name and blood type restoration is handled by EnhancedArenaSnapshotService.RestoreSnapshot()

                // 4. Restore abilities
                AbilityOverrideService.Instance.RestoreAbilities(userEntity, serverCharacter);

                // 4b. Additionally make sure VBlood unlock system is disabled
                VAuto.Core.VBloodMapper.VBloodUnlockSystem.DisableVBloodUnlockMode(serverCharacter);

                // 4c. Remove achievement unlocks applied during arena entry (handles forced debug unlocks too)
                try
                {
                    RemoveForcedUnlocksIfEnabled(platformId, serverCharacter);
                }
                catch (Exception ex)
                {
                    _log?.LogWarning($"[Lifecycle] Failed to remove forced unlocks: {ex.Message}");
                }

                // 5. Restore equipment (handled by snapshot)
                // 6. Restore UI state (placeholder)
                // 7. Restore passives & buffs (placeholder)

                // 2. Remove player from zone (triggers zone deactivation if last player)
                ZoneService.RemovePlayerFromZone(0, platformId);

                // 3. Teleport back to original position
                _log?.LogInfo($"[Lifecycle] Teleporting {platformId} back to {state.OriginalPosition}");
                FastTeleport(serverCharacter, state.OriginalPosition);

                // 4. Clean VBlood hooks
                ReleaseVBloodHook(platformId);

                // 5. Hide arena UI
                ArenaUIManager.HideArenaUI();

                // 6. Close ability slots UI
                try
                {
                    var abilityUI = VAuto.UI.AbilitySlotUI.Instance;
                    abilityUI.CloseAbilitySlots(platformId);
                }
                catch (Exception ex)
                {
                    _log?.LogWarning($"[Lifecycle] Failed to close ability slots UI: {ex.Message}");
                }

                // Raise event for other systems
                RaisePlayerExitEvent(platformId);
                
                _log?.LogInfo($"[Lifecycle] Player {platformId} exited arena successfully");
                return true;
            }
            catch (Exception ex)
            {
                _log?.LogError($"[Lifecycle] Error during arena exit for {platformId}: {ex}");
                return false;
            }
        }

        public bool ApplyVBloodHook(ulong platformId)
        {
            if (_vbloodHookedPlayers.ContainsKey(platformId)) return true;
            
            try
            {
                var query = VAuto.Core.Core.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<User>());
                using var users = query.ToEntityArray(Allocator.Temp);

                foreach (var userEntity in users)
                {
                    if (!VAuto.Core.Core.TryRead<User>(userEntity, out var user)) continue;
                    if (user.PlatformId != platformId) continue;

                    var character = user.LocalCharacter._Entity;
                    if (character != Entity.Null)
                    {
                        _log?.LogInfo($"[Lifecycle] Applying VBlood hook for {platformId}");
                        // VBlood unlock placeholder - method doesn't exist in VBloodMapper
                        _log?.LogDebug($"[Lifecycle] VBlood unlock requested for {platformId} (placeholder)");
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[Lifecycle] Error applying VBlood hook for {platformId}: {ex.Message}");
                return false;
            }

            _vbloodHookedPlayers.TryAdd(platformId, 0);
            return true;
        }



        public bool ReleaseVBloodHook(ulong platformId)
        {
            return _vbloodHookedPlayers.TryRemove(platformId, out _);
        }

        /// <summary>
        /// Handle crash recovery for players who were in PvP when server crashed (Rule 8.1)
        /// </summary>
        public static void HandleCrashRecovery(ulong platformId, Entity userEntity, Entity character)
        {
            try
            {
                Plugin.Logger?.LogInfo($"[Lifecycle] Checking crash recovery for {platformId}");

                // Check if player has a persistent snapshot (indicating they were in PvP)
                if (EnhancedArenaSnapshotService.HasSnapshot(platformId.ToString(), "practice"))
                {
                    Plugin.Logger?.LogInfo($"[Lifecycle] Player {platformId} was in PvP during crash, restoring snapshot");

                    // Restore the snapshot
                    if (EnhancedArenaSnapshotService.RestoreSnapshot(platformId.ToString(), "practice"))
                    {
                        Plugin.Logger?.LogInfo($"[Lifecycle] Successfully restored player {platformId} from crash");
                    }
                    else
                    {
                        Plugin.Logger?.LogError($"[Lifecycle] Failed to restore player {platformId} from crash");
                    }
                }
                else
                {
                    Plugin.Logger?.LogDebug($"[Lifecycle] No crash recovery needed for {platformId}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[Lifecycle] Error during crash recovery for {platformId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Disable character entity from lifecycle management
        /// </summary>
        public bool DisableCharacterEntity(ulong platformId)
        {
            try
            {
                if (!_playerStates.TryGetValue(platformId, out var playerState))
                {
                    _log?.LogWarning($"[Lifecycle] Player {platformId} not found in active states");
                    return false;
                }

                var character = playerState.Character;
                if (!VAuto.Core.Core.Exists(character))
                {
                    _log?.LogWarning($"[Lifecycle] Character entity does not exist for {platformId}");
                    return false;
                }

                // Disable the character entity
                try
                {
                    if (VAuto.Core.Core.TryRead<Disabled>(character, out _))
                    {
                        _log?.LogInfo($"[Lifecycle] Character entity already disabled for {platformId}");
                        return true;
                    }

                    // Add Disabled component to entity
                    VAuto.Core.Core.EntityManager.AddComponentData(character, new Disabled());
                    _log?.LogInfo($"[Lifecycle] Disabled character entity for {platformId}");
                    return true;
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[Lifecycle] Failed to disable character entity: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[Lifecycle] DisableCharacterEntity failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Enable character entity in lifecycle management
        /// </summary>
        public bool EnableCharacterEntity(ulong platformId)
        {
            try
            {
                if (!_playerStates.TryGetValue(platformId, out var playerState))
                {
                    _log?.LogWarning($"[Lifecycle] Player {platformId} not found in active states");
                    return false;
                }

                var character = playerState.Character;
                if (!VAuto.Core.Core.Exists(character))
                {
                    _log?.LogWarning($"[Lifecycle] Character entity does not exist for {platformId}");
                    return false;
                }

                // Enable the character entity
                try
                {
                    if (!VAuto.Core.Core.TryRead<Disabled>(character, out _))
                    {
                        _log?.LogInfo($"[Lifecycle] Character entity already enabled for {platformId}");
                        return true;
                    }

                    // Remove Disabled component from entity
                    VAuto.Core.Core.EntityManager.RemoveComponent<Disabled>(character);
                    _log?.LogInfo($"[Lifecycle] Enabled character entity for {platformId}");
                    return true;
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[Lifecycle] Failed to enable character entity: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[Lifecycle] EnableCharacterEntity failed: {ex.Message}");
                return false;
            }
        }

        private static float3 GetEntityPosition(Entity entity)
        {
            if (VAuto.Core.Core.TryRead<Translation>(entity, out var translation))
                return translation.Value;
            if (VAuto.Core.Core.TryRead<LocalToWorld>(entity, out var ltw))
                return ltw.Position;
            if (VAuto.Core.Core.TryRead<LocalTransform>(entity, out var transform))
                return transform.Position;
            return float3.zero;
        }
    }

    public class PlayerState
    {
        public Entity Character { get; set; }
        public Entity UserEntity { get; set; }
        public DateTime EnteredAt { get; set; }
        public float3 OriginalPosition { get; set; }
    }
}
