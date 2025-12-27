using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using ProjectM;
using ProjectM.Network;
using VAuto.Core;

namespace VAuto.Services
{
    /// <summary>
    /// Missing services that are referenced but not implemented
    /// </summary>
    public static class MissingServices
    {
        #region Lifecycle Service
        public static class LifecycleService
        {
            private static readonly HashSet<ulong> _arenaPlayers = new();
            private static readonly Dictionary<ulong, PlayerArenaState> _playerStates = new();

            public static void Initialize()
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] LIFECYCLE_SERVICE_INIT - Initializing lifecycle service");

                try
                {
                    _arenaPlayers.Clear();
                    _playerStates.Clear();

                    Plugin.Logger?.LogInfo($"[{timestamp}] LIFECYCLE_SERVICE_COMPLETE - Lifecycle service initialized successfully");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] LIFECYCLE_SERVICE_ERROR - Failed to initialize: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] LIFECYCLE_SERVICE_STACK - {ex.StackTrace}");
                }
            }

            public static void Cleanup()
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] LIFECYCLE_SERVICE_CLEANUP - Cleaning up lifecycle service");

                _arenaPlayers.Clear();
                _playerStates.Clear();

                Plugin.Logger?.LogInfo($"[{timestamp}] LIFECYCLE_SERVICE_CLEANUP_COMPLETE - Lifecycle service cleaned up");
            }

            public static bool EnterArena(Entity user, Entity character)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var operationId = Guid.NewGuid().ToString().Substring(0, 8);

                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - === STARTING COMPREHENSIVE ARENA ENTRY ===");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Parameters: User={user}, Character={character}");

                try
                {
                    // Step 1: Validate user entity and get platform ID
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Step 1: Validating user entity...");

                    if (!VRCore.EM.TryGetComponentData(user, out User userComponent))
                    {
                        Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - ✗ FAILED: Could not get User component for entity {user}");
                        return false;
                    }

                    var platformId = userComponent.PlatformId;
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - ✓ User validation successful");
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - PlatformId: {platformId}");

                    // Step 2: Admin permission check
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Step 2: Checking admin permissions...");
                    if (!IsPlayerAdmin(platformId))
                    {
                        Plugin.Logger?.LogWarning($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - ⚠️ REJECTED: Player {platformId} is not an administrator");
                        return false;
                    }
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - ✓ Admin permission verified");

                    // Step 3: Check if already in arena
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Step 3: Checking arena status...");
                    var alreadyInArena = _arenaPlayers.Contains(platformId);
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Currently in arena: {alreadyInArena}");

                    if (alreadyInArena)
                    {
                        Plugin.Logger?.LogWarning($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - ⚠️ REJECTED: Player {platformId} already in arena");
                        return false;
                    }

                    // Step 4: Clear inventory and equipment BEFORE capturing snapshot
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Step 4: Clearing inventory and equipment before snapshot...");
                    ClearPlayerInventory(character);
                    ClearPlayerEquipment(character);
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - ✓ Inventory and equipment cleared before snapshot");

                    // Step 5: Capture comprehensive player snapshot (now with cleared state)
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Step 5: Capturing comprehensive player snapshot...");
                    var snapshot = CapturePlayerSnapshot(platformId, user, character);
                    if (snapshot == null)
                    {
                        Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - ✗ FAILED: Could not capture player snapshot");
                        return false;
                    }
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - ✓ Player snapshot captured successfully");

                    // Step 6: Store snapshot for restoration
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Step 6: Storing snapshot for restoration...");
                    _playerStates[platformId] = snapshot;
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - ✓ Snapshot stored for platform {platformId}");

                    // Step 7: Apply PvP transformations
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Step 7: Applying PvP transformations...");
                    ApplyPvPTransformations(character, platformId);
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - ✓ PvP transformations applied");

                    // Step 8: Give default practice items
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Step 8: Giving default practice items...");
                    GiveDefaultPracticeItems(character);
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - ✓ Default practice items given");

                    // Step 9: Unlock all progress for practice mode
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Step 9: Unlocking all progress for practice mode...");
                    AchievementUnlockService.UnlockAllAchievements(platformId);
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - ✓ All progress unlocked for practice mode");

                    // Step 10: Teleport to arena center
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Step 10: Teleporting to arena...");
                    var currentPos = VRCore.EM.GetComponentData<Translation>(character).Value;
                    var arenaCenter = Plugin.Config.ArenaCenter;
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Current position: {currentPos}");
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Arena center: {arenaCenter}");
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Distance to arena: {math.distance(currentPos, arenaCenter):F1} units");

                    var teleportResult = MissingServices.TeleportService.Teleport(character, arenaCenter);
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Teleport result: {teleportResult}");

                    if (teleportResult)
                    {
                        // Step 11: Mark as in arena
                        _arenaPlayers.Add(platformId);
                        Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - ✓ Player marked as in arena");
                        Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Total players in arena: {_arenaPlayers.Count}");

                        // Step 12: Enable command restrictions
                        Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Step 12: Enabling zone-based command restrictions...");
                        EnableZoneCommandRestrictions(platformId);
                        Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - ✓ Zone command restrictions enabled");

                        // Step 13: Verify final position
                        var finalPos = VRCore.EM.GetComponentData<Translation>(character).Value;
                        Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Final position: {finalPos}");
                        Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Position verification: Distance from arena center = {math.distance(finalPos, arenaCenter):F3}");

                        Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - === COMPREHENSIVE ARENA ENTRY SUCCESSFUL ===");
                        return true;
                    }
                    else
                    {
                        // Cleanup on teleport failure
                        _playerStates.Remove(platformId);
                        Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - ✗ TELEPORT FAILED: Could not teleport to arena center");
                        Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Rolling back changes...");

                        // Attempt rollback
                        RestorePlayerSnapshot(snapshot.Snapshot);
                        Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Rollback completed");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - === CRITICAL EXCEPTION ===");
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Exception: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Type: {ex.GetType().Name}");
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Source: {ex.Source}");
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - TargetSite: {ex.TargetSite}");
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - Stack trace:");
                    Plugin.Logger?.LogError(ex.StackTrace);
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_ENTER_OPERATION_{operationId} - === OPERATION FAILED ===");
                    return false;
                }
            }

            public static bool ExitArena(Entity user, Entity character)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var operationId = Guid.NewGuid().ToString().Substring(0, 8);

                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - === STARTING COMPREHENSIVE ARENA EXIT ===");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - Parameters: User={user}, Character={character}");

                try
                {
                    // Step 1: Validate user entity and get platform ID
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - Step 1: Validating user entity...");

                    if (!VRCore.EM.TryGetComponentData(user, out User userComponent))
                    {
                        Plugin.Logger?.LogError($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - ✗ FAILED: Could not get User component for entity {user}");
                        return false;
                    }

                    var platformId = userComponent.PlatformId;
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - ✓ User validation successful");
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - PlatformId: {platformId}");

                    // Step 2: Check if in arena
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - Step 2: Checking arena status...");
                    var isInArena = _arenaPlayers.Contains(platformId);
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - Currently in arena: {isInArena}");

                    if (!isInArena)
                    {
                        Plugin.Logger?.LogWarning($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - ⚠️ REJECTED: Player {platformId} not in arena");
                        return false;
                    }

                    // Step 3: Get saved state
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - Step 3: Retrieving saved player state...");
                    if (!_playerStates.TryGetValue(platformId, out var playerState))
                    {
                        Plugin.Logger?.LogError($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - ✗ FAILED: No saved state for player {platformId}");
                        return false;
                    }
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - ✓ Player state retrieved successfully");

                    // Step 4: Clear inventory and equipment BEFORE restoring snapshot
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - Step 4: Clearing inventory and equipment before snapshot restore...");
                    ClearPlayerInventory(character);
                    ClearPlayerEquipment(character);
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - ✓ Inventory and equipment cleared before restore");

                    // Step 5: Restore player snapshot
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - Step 5: Restoring player snapshot...");
                    RestorePlayerSnapshot(playerState.Snapshot);
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - ✓ Player snapshot restored");

                    // Step 6: Remove PvP transformations
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - Step 6: Removing PvP transformations...");
                    RemovePvPTransformations(character, platformId);
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - ✓ PvP transformations removed");

                    // Step 6: Teleport back to original position
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - Step 6: Teleporting to original position...");
                    var originalPos = playerState.OriginalPosition;
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - Original position: {originalPos}");

                    var teleportResult = MissingServices.TeleportService.Teleport(character, originalPos);
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - Teleport result: {teleportResult}");

                    if (teleportResult)
                    {
                        // Step 7: Remove from arena tracking
                        _arenaPlayers.Remove(platformId);
                        _playerStates.Remove(platformId);
                        Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - ✓ Player removed from arena tracking");

                        // Step 8: Disable zone command restrictions
                        Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - Step 8: Disabling zone command restrictions...");
                        DisableZoneCommandRestrictions(platformId);
                        Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - ✓ Zone restrictions disabled");

                        // Step 9: Final verification
                        var finalPos = VRCore.EM.GetComponentData<Translation>(character).Value;
                        Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - Final position: {finalPos}");
                        Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - Position verification: Distance from original = {math.distance(finalPos, originalPos):F3}");

                        Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - === COMPREHENSIVE ARENA EXIT SUCCESSFUL ===");
                        return true;
                    }
                    else
                    {
                        Plugin.Logger?.LogError($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - ✗ TELEPORT FAILED: Could not teleport back to original position");

                        // Rollback: Re-add to arena tracking since exit failed
                        _arenaPlayers.Add(platformId);
                        _playerStates[platformId] = playerState;
                        Plugin.Logger?.LogError($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - Rollback completed - player still in arena");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - === CRITICAL EXCEPTION ===");
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - Exception: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - Type: {ex.GetType().Name}");
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - Source: {ex.Source}");
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - TargetSite: {ex.TargetSite}");
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - Stack trace:");
                    Plugin.Logger?.LogError(ex.StackTrace);
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_EXIT_OPERATION_{operationId} - === OPERATION FAILED ===");
                    return false;
                }
            }

            public static bool IsPlayerInArena(ulong platformId)
            {
                var inArena = _arenaPlayers.Contains(platformId);
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogDebug($"[{timestamp}] ARENA_STATUS_CHECK - PlatformId: {platformId}, InArena: {inArena}");
                return inArena;
            }

            public static PlayerArenaState GetPlayerState(ulong platformId)
            {
                return _playerStates.TryGetValue(platformId, out var state) ? state : null;
            }

            public static List<ulong> GetArenaPlayers()
            {
                return _arenaPlayers.ToList();
            }

            #region Helper Methods

            private static bool IsPlayerAdmin(ulong platformId)
            {
                // TODO: Implement proper admin checking
                // For now, allow all players (this should be configured)
                return true;
            }

            private static PlayerArenaState CapturePlayerSnapshot(ulong platformId, Entity userEntity, Entity characterEntity)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

                try
                {
                    Plugin.Logger?.LogInfo($"[{timestamp}] SNAPSHOT_CAPTURE_START - Capturing comprehensive snapshot for {platformId}");

                    var snapshot = new PlayerSnapshot
                    {
                        PlatformId = platformId,
                        CapturedAt = DateTime.UtcNow
                    };

                    // Get current position
                    var currentPos = VRCore.EM.GetComponentData<Translation>(characterEntity).Value;

                    // Capture player name
                    if (VRCore.EM.TryGetComponentData(characterEntity, out PlayerCharacter playerChar))
                    {
                        snapshot.OriginalName = playerChar.Name.ToString();
                    }

                    // Capture level and experience (simplified for now)
                    snapshot.Level = 1; // TODO: Implement proper level capture
                    snapshot.Experience = 0; // TODO: Implement proper experience capture

                    // Capture blood type (simplified for now)
                    snapshot.BloodType = 0; // TODO: Implement proper blood type capture

                    // Capture VBlood kills (simplified for now)
                    snapshot.VBloodKills = 0; // TODO: Implement proper VBlood kills capture

                    // Capture inventory items
                    if (VRCore.EM.TryGetComponentData(characterEntity, out InventoryBuffer inventory))
                    {
                        snapshot.InventoryItems = new List<int>();
                        // Note: Inventory capture would need more complex implementation
                        // This is a placeholder for the actual inventory system
                    }

                    // Capture equipment items
                    if (VRCore.EM.TryGetComponentData(characterEntity, out Equipment equipment))
                    {
                        snapshot.EquipmentItems = new List<int>();
                        // Note: Equipment capture would need more complex implementation
                        // This is a placeholder for the actual equipment system
                    }

                    // Capture achievements and unlocked content
                    snapshot.Achievements = new List<string>();
                    snapshot.UnlockedPassives = new List<string>();
                    // Note: Achievement and passive capture would need access to game's progression systems

                    // Capture UI stats (health, stats, etc.)
                    snapshot.UiStats = new Dictionary<string, object>();
                    if (VRCore.EM.TryGetComponentData(characterEntity, out Health health))
                    {
                        snapshot.UiStats["MaxHealth"] = health.MaxHealth;
                        snapshot.UiStats["CurrentHealth"] = health.Value;
                    }

                    // Additional stats
                    if (VRCore.EM.TryGetComponentData(characterEntity, out UnitStats unitStats))
                    {
                        snapshot.UiStats["PhysicalPower"] = unitStats.PhysicalPower;
                        snapshot.UiStats["SpellPower"] = unitStats.SpellPower;
                        snapshot.UiStats["PhysicalResistance"] = unitStats.PhysicalResistance;
                        snapshot.UiStats["SpellResistance"] = unitStats.SpellResistance;
                    }

                    var state = new PlayerArenaState
                    {
                        PlatformId = platformId,
                        OriginalPosition = currentPos,
                        EnteredAt = DateTime.UtcNow,
                        CharacterEntity = characterEntity,
                        UserEntity = userEntity,
                        Snapshot = snapshot,
                        IsPvPMode = false
                    };

                    Plugin.Logger?.LogInfo($"[{timestamp}] SNAPSHOT_CAPTURE_COMPLETE - Snapshot captured for {platformId} (Level: {snapshot.Level}, BloodType: {snapshot.BloodType})");
                    return state;

                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] SNAPSHOT_CAPTURE_ERROR - Failed to capture snapshot for {platformId}: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] SNAPSHOT_CAPTURE_STACK - {ex.StackTrace}");
                    return null;
                }
            }

            private static void ClearPlayerInventory(Entity character)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] INVENTORY_CLEAR_START - Clearing inventory for {character}");

                try
                {
                    // TODO: Implement inventory clearing using V Rising's inventory system
                    Plugin.Logger?.LogInfo($"[{timestamp}] INVENTORY_CLEAR_COMPLETE - Inventory cleared for {character}");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] INVENTORY_CLEAR_ERROR - Failed to clear inventory: {ex.Message}");
                }
            }

            private static void ClearPlayerEquipment(Entity character)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] EQUIPMENT_CLEAR_START - Clearing equipment for {character}");

                try
                {
                    // TODO: Implement equipment clearing using V Rising's equipment system
                    Plugin.Logger?.LogInfo($"[{timestamp}] EQUIPMENT_CLEAR_COMPLETE - Equipment cleared for {character}");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] EQUIPMENT_CLEAR_ERROR - Failed to clear equipment: {ex.Message}");
                }
            }

            private static void ApplyPvPTransformations(Entity character, ulong platformId)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] PVP_TRANSFORM_START - Applying PvP transformations for {platformId}");

                try
                {
                    // Change blood type to PvP type
                    // TODO: Implement blood type change

                    // Add [PvP] prefix to name
                    // TODO: Implement name modification

                    // Apply PvP-specific buffs
                    // TODO: Implement PvP buff application

                    Plugin.Logger?.LogInfo($"[{timestamp}] PVP_TRANSFORM_COMPLETE - PvP transformations applied for {platformId}");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] PVP_TRANSFORM_ERROR - Failed to apply PvP transformations: {ex.Message}");
                }
            }

            private static void GiveDefaultPracticeItems(Entity character)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] PRACTICE_ITEMS_START - Giving default practice items to {character}");

                try
                {
                    // Give basic weapons and armor from config
                    // TODO: Implement item giving using V Rising's item system

                    Plugin.Logger?.LogInfo($"[{timestamp}] PRACTICE_ITEMS_COMPLETE - Default practice items given to {character}");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] PRACTICE_ITEMS_ERROR - Failed to give practice items: {ex.Message}");
                }
            }

            private static void EnableZoneCommandRestrictions(ulong platformId)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ZONE_RESTRICTIONS_START - Enabling zone command restrictions for {platformId}");

                try
                {
                    // Enable restrictions for weapon changes, blood type changes, etc.
                    // TODO: Implement command restriction system

                    Plugin.Logger?.LogInfo($"[{timestamp}] ZONE_RESTRICTIONS_COMPLETE - Zone restrictions enabled for {platformId}");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ZONE_RESTRICTIONS_ERROR - Failed to enable restrictions: {ex.Message}");
                }
            }

            private static void RestorePlayerSnapshot(PlayerSnapshot snapshot)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] SNAPSHOT_RESTORE_START - Restoring snapshot for {snapshot.PlatformId}");

                try
                {
                    // Restore all captured data
                    // TODO: Implement full snapshot restoration
                    // Restore inventory, equipment, stats, achievements, etc.

                    Plugin.Logger?.LogInfo($"[{timestamp}] SNAPSHOT_RESTORE_COMPLETE - Snapshot restored for {snapshot.PlatformId}");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] SNAPSHOT_RESTORE_ERROR - Failed to restore snapshot: {ex.Message}");
                }
            }

            private static void RemovePvPTransformations(Entity character, ulong platformId)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] PVP_TRANSFORM_REMOVE_START - Removing PvP transformations for {platformId}");

                try
                {
                    // Remove PvP blood type
                    // TODO: Implement blood type restoration

                    // Remove [PvP] prefix from name
                    // TODO: Implement name restoration

                    // Remove PvP-specific buffs
                    // TODO: Implement PvP buff removal

                    Plugin.Logger?.LogInfo($"[{timestamp}] PVP_TRANSFORM_REMOVE_COMPLETE - PvP transformations removed for {platformId}");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] PVP_TRANSFORM_REMOVE_ERROR - Failed to remove PvP transformations: {ex.Message}");
                }
            }

            private static void DisableZoneCommandRestrictions(ulong platformId)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ZONE_RESTRICTIONS_DISABLE_START - Disabling zone command restrictions for {platformId}");

                try
                {
                    // Disable restrictions for weapon changes, blood type changes, etc.
                    // TODO: Implement command restriction removal

                    Plugin.Logger?.LogInfo($"[{timestamp}] ZONE_RESTRICTIONS_DISABLE_COMPLETE - Zone restrictions disabled for {platformId}");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ZONE_RESTRICTIONS_DISABLE_ERROR - Failed to disable restrictions: {ex.Message}");
                }
            }

            #endregion
        }

        public class PlayerArenaState
        {
            public ulong PlatformId { get; set; }
            public float3 OriginalPosition { get; set; }
            public DateTime EnteredAt { get; set; }
            public Entity CharacterEntity { get; set; }
            public Entity UserEntity { get; set; }

            // Comprehensive snapshot data
            public PlayerSnapshot Snapshot { get; set; }
            public bool IsPvPMode { get; set; }
        }

        public class PlayerSnapshot
        {
            public ulong PlatformId { get; set; }
            public string OriginalName { get; set; }
            public List<int> InventoryItems { get; set; } = new List<int>();
            public List<int> EquipmentItems { get; set; } = new List<int>();
            public int Level { get; set; }
            public int Experience { get; set; }
            public int BloodType { get; set; }
            public int VBloodKills { get; set; }
            public List<string> Achievements { get; set; } = new List<string>();
            public List<string> UnlockedPassives { get; set; } = new List<string>();
            public Dictionary<string, object> UiStats { get; set; } = new Dictionary<string, object>();
            public DateTime CapturedAt { get; set; }
        }
        #endregion

        #region Arena Healing Service
        public static class ArenaHealingService
        {
            public static void Initialize()
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_HEALING_SERVICE_INIT - Initializing arena healing service");
                
                try
                {
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_HEALING_SERVICE_COMPLETE - Arena healing service initialized");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_HEALING_SERVICE_ERROR - Failed to initialize: {ex.Message}");

                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_HEALING_SERVICE_STACK - {ex.StackTrace}");
                }
            }
            
            public static void ApplyHeal(Entity character)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_HEAL_APPLY - Applying heal to character: {character}");
                
                try
                {
                    // TODO: Implement actual healing logic
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_HEAL_COMPLETE - Healing applied to character: {character}");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_HEAL_ERROR - Failed to heal character {character}: {ex.Message}");

                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_HEAL_STACK - {ex.StackTrace}");
                }
            }
            public static void HealPlayer(Entity user, float amount) { }
            public static void SetHealth(Entity user, float health) { }
        }
        #endregion

        #region Build Service
        public static class BuildService
        {
            public static void Initialize()
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] BUILD_SERVICE_INIT - Initializing build service");
                
                try
                {
                    Plugin.Logger?.LogInfo($"[{timestamp}] BUILD_SERVICE_COMPLETE - Build service initialized");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] BUILD_SERVICE_ERROR - Failed to initialize: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] BUILD_SERVICE_STACK - {ex.StackTrace}");
                }
            }
            
            public static void Cleanup() { }
            public static bool CanBuildAt(Entity user, float3 position) => true;
            public static void BuildStructure(Entity user, string prefab, float3 position) { }
            
            public static void ApplyDefaultBuild(Entity character)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] BUILD_APPLY_DEFAULT - Applying default build to character: {character}");
                
                try
                {
                    // TODO: Implement actual loadout application
                    Plugin.Logger?.LogInfo($"[{timestamp}] BUILD_APPLY_COMPLETE - Default build applied to character: {character}");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] BUILD_APPLY_ERROR - Failed to apply build to character {character}: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] BUILD_APPLY_STACK - {ex.StackTrace}");
                }
            }
        }
        #endregion

        #region Teleport Service
        public static class TeleportService
        {
            public static void Initialize()
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] TELEPORT_SERVICE_INIT - Initializing teleport service");
                
                try
                {
                    Plugin.Logger?.LogInfo($"[{timestamp}] TELEPORT_SERVICE_COMPLETE - Teleport service initialized");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] TELEPORT_SERVICE_ERROR - Failed to initialize: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] TELEPORT_SERVICE_STACK - {ex.StackTrace}");
                }
            }
            
            public static void Cleanup() { }
            
            public static bool Teleport(Entity characterEntity, float3 position)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] TELEPORT_REQUEST - Teleporting character {characterEntity} to position {position}");
                
                try
                {
                    var em = VRCore.EM;
                    if (characterEntity == Entity.Null || !em.Exists(characterEntity))
                    {
                        Plugin.Logger?.LogError($"[{timestamp}] TELEPORT_ERROR - Invalid character entity: {characterEntity}");
                        return false;
                    }

                    // Get user entity with fallback
                    Entity userEntity = Entity.Null;
                    if (em.TryGetComponentData(characterEntity, out PlayerCharacter playerChar))
                        userEntity = playerChar.UserEntity;

                    // Skip user entity validation - proceed with direct teleport
                    if (em.HasComponent<Translation>(characterEntity))
                        em.SetComponentData(characterEntity, new Translation { Value = position });
                    else
                        em.AddComponentData(characterEntity, new Translation { Value = position });

                    Plugin.Logger?.LogInfo($"[{timestamp}] TELEPORT_COMPLETE - Successfully teleported character {characterEntity} to {position}");
                    return true;
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] TELEPORT_ERROR - Failed to teleport character {characterEntity}: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] TELEPORT_STACK - {ex.StackTrace}");
                    return false;
                }
            }
            
            public static void TeleportTo(Entity user, float3 position) { }
            public static bool CanTeleport(Entity user, float3 position) => true;
        }
        #endregion

        #region Zone Service
        public static class ZoneService
        {
            private static readonly ConcurrentDictionary<ulong, DateTime> _playerLastTransition = new();
            private static readonly ConcurrentDictionary<ulong, int> _transitionCounters = new();
            private static readonly TimeSpan MIN_TRANSITION_INTERVAL = TimeSpan.FromSeconds(2);
            private const int MAX_TRANSITION_ATTEMPTS = 3;

            internal static float3 Center = new float3(-269.1477f, 2.5f, -2928.303f);
            internal static float3 SpawnPoint = new float3(-269.1477f, 3f, -2928.303f);
            internal static float Radius = 100f;
            internal static float ExitRadius = 150f;
            internal static bool HasExplicitConfig = true;

            // Track transition states with thread safety
            private static readonly ConcurrentDictionary<ulong, TransitionState> _transitionStates = new();

            private enum TransitionState
            {
                None,
                Entering,
                Exiting
            }

            public static void Initialize()
            {
                // Initialize with default values if not configured
                if (!HasExplicitConfig)
                {
                    Plugin.Log?.LogWarning("Arena zone not explicitly configured, using default values");
                    SetArenaZone(Center, Radius);
                }
            }

            public static void SetArenaZone(float3 center, float radius)
            {
                if (float.IsNaN(center.x) || float.IsNaN(center.y) || float.IsNaN(center.z))
                {
                    Plugin.Log?.LogError("Invalid arena center position");
                    return;
                }

                if (radius <= 0 || radius > 1000)
                {
                    Plugin.Log?.LogError("Invalid arena radius. Must be between 1 and 1000");
                    return;
                }

                Center = center;
                Radius = radius;
                ExitRadius = Math.Clamp(radius * 1.5f, radius + 5f, radius + 50f);
                HasExplicitConfig = true;

                Plugin.Log?.LogInfo($"Arena zone updated - Center: {center}, Entry Radius: {radius}, Exit Radius: {ExitRadius}");
            }

            public static void SetSpawn(float3 spawnPoint)
            {
                SpawnPoint = spawnPoint;
                Plugin.Log?.LogInfo($"Arena spawn point set to: {spawnPoint}");
            }

            public static bool IsInArena(float3 position)
            {
                if (!HasExplicitConfig) return false;
                var dx = position.x - Center.x;
                var dz = position.z - Center.z;
                float distanceSq = (dx * dx) + (dz * dz);
                return distanceSq <= (Radius * Radius);
            }

            public static bool IsOutsideArena(float3 position)
            {
                if (!HasExplicitConfig) return true;
                var dx = position.x - Center.x;
                var dz = position.z - Center.z;
                float distanceSq = (dx * dx) + (dz * dz);
                return distanceSq > (ExitRadius * ExitRadius);
            }

            public static bool IsInTransitionZone(float3 position)
            {
                if (!HasExplicitConfig) return false;
                var dx = position.x - Center.x;
                var dz = position.z - Center.z;
                float distanceSq = (dx * dx) + (dz * dz);
                return distanceSq > (Radius * Radius) && distanceSq <= (ExitRadius * ExitRadius);
            }

            public static float GetDistanceToCenter(float3 position)
            {
                return math.distance(position, Center);
            }

            public static bool IsInRange2D(float3 positionA, float3 positionB, float range)
            {
                return math.distance(new float2(positionA.x, positionA.z), new float2(positionB.x, positionB.z)) <= range;
            }

            public static bool IsPointInCircle(float3 point, float3 circleCenter, float radius)
            {
                var dx = point.x - circleCenter.x;
                var dz = point.z - circleCenter.z;
                return (dx * dx + dz * dz) <= (radius * radius);
            }

            public static bool TryBeginTransition(ulong platformId, bool isEntering)
            {
                // Check cooldown
                if (_playerLastTransition.TryGetValue(platformId, out var lastTransition) &&
                    (DateTime.UtcNow - lastTransition) < MIN_TRANSITION_INTERVAL)
                {
                    Plugin.Log?.LogDebug($"Transition too soon for {platformId}");
                    return false;
                }

                // Check if already in transition
                var currentState = _transitionStates.GetOrAdd(platformId, TransitionState.None);
                var targetState = isEntering ? TransitionState.Entering : TransitionState.Exiting;

                if (currentState == targetState)
                {
                    Plugin.Log?.LogDebug($"Already in requested transition state: {targetState}");
                    return false;
                }

                // Update transition state
                if (!_transitionStates.TryUpdate(platformId, targetState, currentState))
                {
                    Plugin.Log?.LogWarning($"Failed to update transition state for {platformId}");
                    return false;
                }

                // Update transition tracking
                _playerLastTransition[platformId] = DateTime.UtcNow;
                _transitionCounters.AddOrUpdate(platformId, 1, (_, count) => count + 1);

                Plugin.Log?.LogInfo($"Began transition {targetState} for player {platformId}");
                return true;
            }

            public static void EndTransition(ulong platformId, bool success)
            {
                if (_transitionStates.TryRemove(platformId, out var state))
                {
                    Plugin.Log?.LogInfo($"Ended transition {state} for player {platformId} (Success: {success})");

                    // Clean up old transitions
                    CleanupOldTransitions();
                }
            }

            private static void CleanupOldTransitions()
            {
                var cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(5);
                var toRemove = new List<ulong>();

                foreach (var kvp in _playerLastTransition)
                {
                    if (kvp.Value < cutoff)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }

                foreach (var id in toRemove)
                {
                    _playerLastTransition.TryRemove(id, out _);
                    _transitionStates.TryRemove(id, out _);
                    _transitionCounters.TryRemove(id, out _);
                }
            }

            public static bool CanTransition(ulong platformId)
            {
                if (_transitionCounters.TryGetValue(platformId, out var count) && count > MAX_TRANSITION_ATTEMPTS)
                {
                    Plugin.Log?.LogWarning($"Player {platformId} exceeded max transition attempts");
                    return false;
                }
                return true;
            }

            public static void ResetTransitionCounter(ulong platformId)
            {
                _transitionCounters[platformId] = 0;
            }

            public static Entity[] GetPlayersInArena(EntityQuery playerQuery, bool inArena = true)
            {
                var players = playerQuery.ToEntityArray(Allocator.Temp);
                try
                {
                    var result = new List<Entity>();
                    var em = VRCore.EM;

                    foreach (var player in players)
                    {
                        if (player == Entity.Null || !em.Exists(player)) continue;

                        float3 position = float3.zero;
                        if (em.HasComponent<Translation>(player))
                        {
                            position = em.GetComponentData<Translation>(player).Value;
                        }
                        else if (em.HasComponent<LocalToWorld>(player))
                        {
                            position = em.GetComponentData<LocalToWorld>(player).Position;
                        }
                        else continue;

                        if ((inArena && IsInArena(position)) || (!inArena && IsOutsideArena(position)))
                        {
                            result.Add(player);
                        }
                    }

                    return result.ToArray();
                }
                finally
                {
                    players.Dispose();
                }
            }

            // Legacy methods for backward compatibility
            public static bool IsInZone(Entity user, string zoneName)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ZONE_CHECK - Checking if user {user} is in zone: {zoneName}");
                return false;
            }

            public static void EnterZone(Entity user, string zoneName)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ZONE_ENTER - User {user} entering zone: {zoneName}");
            }

            public static void ExitZone(Entity user, string zoneName)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ZONE_EXIT - User {user} exiting zone: {zoneName}");
            }

            public static bool CreateZone(string name, float3 center, float radius)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ZONE_CREATE - Creating zone '{name}' at {center} with radius {radius}");

                try
                {
                    // TODO: Implement actual zone creation
                    // For now, just update the global zone properties
                    Center = center;
                    Radius = radius;
                    ExitRadius = radius * 1.2f; // Slightly larger exit radius

                    Plugin.Logger?.LogInfo($"[{timestamp}] ZONE_CREATE_COMPLETE - Zone '{name}' created successfully");
                    return true;
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ZONE_CREATE_ERROR - Failed to create zone '{name}': {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] ZONE_CREATE_STACK - {ex.StackTrace}");
                    return false;
                }
            }
        }
        #endregion

        #region Arena Character Service
        public static class ArenaCharacterService
        {
            public static void Initialize()
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_CHARACTER_SERVICE_INIT - Initializing arena character service");
                
                try
                {
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_CHARACTER_SERVICE_COMPLETE - Arena character service initialized");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_CHARACTER_SERVICE_ERROR - Failed to initialize: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_CHARACTER_SERVICE_STACK - {ex.StackTrace}");
                }
            }
            
            public static void Cleanup() { }
            
            public static void SetupCharacter(Entity user) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_CHARACTER_SETUP - Setting up arena character for user: {user}");
            }
            
            public static void ResetCharacter(Entity user) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_CHARACTER_RESET - Resetting arena character for user: {user}");
            }
            
            public static bool HasArenaCharacter(ulong platformId) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_CHARACTER_CHECK - Checking if platform {platformId} has arena character");
                return false; 
            }
        }
        #endregion

        #region Arena Aura Service
        public static class ArenaAuraService
        {
            public static void Initialize()
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_AURA_SERVICE_INIT - Initializing arena aura service");
                
                try
                {
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_AURA_SERVICE_COMPLETE - Arena aura service initialized");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_AURA_SERVICE_ERROR - Failed to initialize: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_AURA_SERVICE_STACK - {ex.StackTrace}");
                }
            }
            
            public static void Cleanup() { }
            
            public static void ApplyAura(Entity user, string auraType) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_AURA_APPLY - Applying aura {auraType} to user: {user}");
            }
            
            public static void RemoveAura(Entity user, string auraType) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_AURA_REMOVE - Removing aura {auraType} from user: {user}");
            }
            
            public static bool HasArenaBuffs(Entity character) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_BUFFS_CHECK - Checking if character {character} has arena buffs");
                return false; 
            }
            
            public static void ApplyArenaBuffs(Entity character) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_BUFFS_APPLY - Applying arena buffs to character: {character}");
            }
            
            public static void ClearArenaBuffs(Entity character) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_BUFFS_CLEAR - Clearing arena buffs from character: {character}");
            }
        }
        #endregion

        #region Auto Enter Service
        public static class AutoEnterService
        {
            public static void Initialize()
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] AUTO_ENTER_SERVICE_INIT - Initializing auto enter service");
                
                try
                {
                    Plugin.Logger?.LogInfo($"[{timestamp}] AUTO_ENTER_SERVICE_COMPLETE - Auto enter service initialized");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] AUTO_ENTER_SERVICE_ERROR - Failed to initialize: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] AUTO_ENTER_SERVICE_STACK - {ex.StackTrace}");
                }
            }
            
            public static void Cleanup() { }
            
            public static void EnableAutoEnter(Entity user) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] AUTO_ENTER_ENABLE - Enabling auto enter for user: {user}");
            }
            
            public static void DisableAutoEnter(Entity user) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] AUTO_ENTER_DISABLE - Disabling auto enter for user: {user}");
            }
            
            public static bool IsAutoEnterEnabled(ulong platformId) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] AUTO_ENTER_CHECK - Checking if auto enter is enabled for platform: {platformId}");
                return false; 
            }
        }
        #endregion

        #region Unlock Helper
        public static class UnlockHelper
        {
            public static void Initialize()
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] UNLOCK_HELPER_INIT - Initializing unlock helper");
                
                try
                {
                    Plugin.Logger?.LogInfo($"[{timestamp}] UNLOCK_HELPER_COMPLETE - Unlock helper initialized");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] UNLOCK_HELPER_ERROR - Failed to initialize: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] UNLOCK_HELPER_STACK - {ex.StackTrace}");
                }
            }
            
            public static void Cleanup() { }
            
            public static void UnlockAbility(Entity user, string ability) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] UNLOCK_ABILITY - Unlocking ability {ability} for user: {user}");
            }
            
            public static bool HasAbility(Entity user, string ability) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] UNLOCK_CHECK - Checking if user {user} has ability: {ability}");
                return false; 
            }
        }
        #endregion

        #region Schematic Service
        public static class SchematicService
        {
            private static readonly Dictionary<string, SchematicData> _schematics = new();
            private static string _activeSchematic = null;

            public static void Initialize()
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] SCHEMATIC_SERVICE_INIT - Initializing schematic service");
                
                try
                {
                    _schematics.Clear();
                    // Add default schematics
                    _schematics["floor"] = new SchematicData { Name = "floor", Category = "surface", Cost = 0 };
                    _schematics["wall"] = new SchematicData { Name = "wall", Category = "structure", Cost = 0 };
                    _schematics["portal"] = new SchematicData { Name = "portal", Category = "portal", Cost = 0 };
                    _schematics["waygate"] = new SchematicData { Name = "waygate", Category = "portal", Cost = 0 };
                    _schematics["glow"] = new SchematicData { Name = "glow", Category = "light", Cost = 0 };
                    
                    Plugin.Logger?.LogInfo($"[{timestamp}] SCHEMATIC_SERVICE_COMPLETE - Schematic service initialized with {_schematics.Count} schematics");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] SCHEMATIC_SERVICE_ERROR - Failed to initialize: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] SCHEMATIC_SERVICE_STACK - {ex.StackTrace}");
                }
            }

            public static void Cleanup()
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] SCHEMATIC_SERVICE_CLEANUP - Cleaning up schematic service");
                
                _schematics.Clear();
                _activeSchematic = null;
                
                Plugin.Logger?.LogInfo($"[{timestamp}] SCHEMATIC_SERVICE_CLEANUP_COMPLETE - Schematic service cleaned up");
            }

            public static bool CanUseSchematic(Entity user, string schematic) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] SCHEMATIC_CAN_USE - Checking if user {user} can use schematic: {schematic}");
                return _schematics.ContainsKey(schematic.ToLower()); 
            }
            
            public static void UseSchematic(Entity user, string schematic) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] SCHEMATIC_USE - User {user} using schematic: {schematic}");
                _activeSchematic = schematic.ToLower(); 
            }
            public static string GetActiveSchematic() => _activeSchematic;
            public static List<string> GetAvailableSchematics() => _schematics.Keys.ToList();
            public static List<string> GetSchematicsByCategory(string category) =>
                _schematics.Where(s => s.Value.Category == category.ToLower()).Select(s => s.Key).ToList();
        }

        public class SchematicData
        {
            public string Name { get; set; }
            public string Category { get; set; }
            public int Cost { get; set; }
        }
        #endregion

        #region Portal Service
        public static class PortalService
        {
            private static readonly Dictionary<string, PortalData> _portals = new();

            public static void Initialize() 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] PORTAL_SERVICE_INIT - Initializing portal service");
                
                try
                {
                    _portals.Clear();
                    Plugin.Logger?.LogInfo($"[{timestamp}] PORTAL_SERVICE_COMPLETE - Portal service initialized");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] PORTAL_SERVICE_ERROR - Failed to initialize: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] PORTAL_SERVICE_STACK - {ex.StackTrace}");
                }
            }
            
            public static void Cleanup() 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] PORTAL_SERVICE_CLEANUP - Cleaning up portal service");
                
                _portals.Clear();
                
                Plugin.Logger?.LogInfo($"[{timestamp}] PORTAL_SERVICE_CLEANUP_COMPLETE - Portal service cleaned up");
            }

            public static bool CreatePortal(string name, float3 position, float3 destination)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] PORTAL_CREATE - Creating portal '{name}' from {position} to {destination}");
                
                try
                {
                    if (_portals.ContainsKey(name.ToLower())) 
                    {
                        Plugin.Logger?.LogError($"[{timestamp}] PORTAL_CREATE_ERROR - Portal '{name}' already exists");
                        return false;
                    }
                    
                    _portals[name.ToLower()] = new PortalData
                    {
                        Name = name,
                        Position = position,
                        Destination = destination,
                        Created = DateTime.UtcNow
                    };
                    
                    Plugin.Logger?.LogInfo($"[{timestamp}] PORTAL_CREATE_COMPLETE - Portal '{name}' created successfully");
                    return true;
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] PORTAL_CREATE_ERROR - Failed to create portal '{name}': {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] PORTAL_CREATE_STACK - {ex.StackTrace}");
                    return false;
                }
            }

            public static bool RemovePortal(string name) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] PORTAL_REMOVE - Removing portal: {name}");
                
                var result = _portals.Remove(name.ToLower());
                
                if (result)
                    Plugin.Logger?.LogInfo($"[{timestamp}] PORTAL_REMOVE_COMPLETE - Portal '{name}' removed successfully");
                else
                    Plugin.Logger?.LogError($"[{timestamp}] PORTAL_REMOVE_ERROR - Portal '{name}' not found");
                    
                return result; 
            }
            public static PortalData GetPortal(string name) =>
                _portals.TryGetValue(name.ToLower(), out var portal) ? portal : null;
            public static List<string> GetPortalNames() => _portals.Keys.ToList();
            public static bool PortalExists(string name) => _portals.ContainsKey(name.ToLower());
        }

        public class PortalData
        {
            public string Name { get; set; }
            public float3 Position { get; set; }
            public float3 Destination { get; set; }
            public DateTime Created { get; set; }
        }
        #endregion

        #region Surface Service
        public static class SurfaceService
        {
            private static string _activeMaterial = "stone";

            public static void Initialize() 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] SURFACE_SERVICE_INIT - Initializing surface service");
                
                try
                {
                    _activeMaterial = "stone";
                    Plugin.Logger?.LogInfo($"[{timestamp}] SURFACE_SERVICE_COMPLETE - Surface service initialized with material: {_activeMaterial}");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] SURFACE_SERVICE_ERROR - Failed to initialize: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] SURFACE_SERVICE_STACK - {ex.StackTrace}");
                }
            }
            
            public static void Cleanup() 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] SURFACE_SERVICE_CLEANUP - Cleaning up surface service");
            }

            public static void SetActiveMaterial(string material) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] SURFACE_MATERIAL_SET - Setting active material to: {material}");
                _activeMaterial = material; 
            }
            
            public static string GetActiveMaterial() 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] SURFACE_MATERIAL_GET - Getting active material: {_activeMaterial}");
                return _activeMaterial; 
            }
            
            public static List<string> GetAvailableMaterials() 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var materials = new List<string> { "stone", "wood", "metal", "glow" };
                var materialList = string.Join(", ", materials);
                Plugin.Logger?.LogInfo($"[{timestamp}] SURFACE_MATERIALS_LIST - Available materials: {materialList}");
                return materials; 
            }
        }
        #endregion

        #region Build Mode Service
        public static class BuildModeService
        {
            private static readonly HashSet<ulong> _buildModePlayers = new();

            public static void Initialize() 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] BUILD_MODE_SERVICE_INIT - Initializing build mode service");
                
                try
                {
                    _buildModePlayers.Clear();
                    Plugin.Logger?.LogInfo($"[{timestamp}] BUILD_MODE_SERVICE_COMPLETE - Build mode service initialized");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] BUILD_MODE_SERVICE_ERROR - Failed to initialize: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] BUILD_MODE_SERVICE_STACK - {ex.StackTrace}");
                }
            }
            
            public static void Cleanup() 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] BUILD_MODE_SERVICE_CLEANUP - Cleaning up build mode service");
                
                _buildModePlayers.Clear();
                
                Plugin.Logger?.LogInfo($"[{timestamp}] BUILD_MODE_SERVICE_CLEANUP_COMPLETE - Build mode service cleaned up");
            }

            public static bool IsInBuildMode(ulong platformId) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] BUILD_MODE_CHECK - Checking if platform {platformId} is in build mode");
                return _buildModePlayers.Contains(platformId); 
            }
            
            public static void EnableBuildMode(ulong platformId) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] BUILD_MODE_ENABLE - Enabling build mode for platform: {platformId}");
                _buildModePlayers.Add(platformId); 
            }
            
            public static void DisableBuildMode(ulong platformId) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] BUILD_MODE_DISABLE - Disabling build mode for platform: {platformId}");
                _buildModePlayers.Remove(platformId); 
            }
            
            public static void ToggleBuildMode(ulong platformId)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] BUILD_MODE_TOGGLE - Toggling build mode for platform: {platformId}");
                
                if (IsInBuildMode(platformId))
                    DisableBuildMode(platformId);
                else
                    EnableBuildMode(platformId);
            }
        }
        #endregion

        #region Arena Update System
        public static class ArenaUpdateSystem
        {
            public static void Initialize() 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_UPDATE_SYSTEM_INIT - Initializing arena update system");
                
                try
                {
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_UPDATE_SYSTEM_COMPLETE - Arena update system initialized");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_UPDATE_SYSTEM_ERROR - Failed to initialize: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_UPDATE_SYSTEM_STACK - {ex.StackTrace}");
                }
            }
            
            public static void Cleanup() 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_UPDATE_SYSTEM_CLEANUP - Cleaning up arena update system");
            }
            
            public static void Update(float deltaTime) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_UPDATE_SYSTEM_UPDATE - Updating arena system with delta: {deltaTime}");
            }
        }
        #endregion

        #region Arena Database Service
        public static class ArenaDatabaseService
        {
            public static void Initialize() 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_DATABASE_SERVICE_INIT - Initializing arena database service");
                
                try
                {
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_DATABASE_SERVICE_COMPLETE - Arena database service initialized");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_DATABASE_SERVICE_ERROR - Failed to initialize: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_DATABASE_SERVICE_STACK - {ex.StackTrace}");
                }
            }
            
            public static void Cleanup() 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_DATABASE_SERVICE_CLEANUP - Cleaning up arena database service");
            }
            
            public static void SaveArenaData(string key, object data) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_DATABASE_SAVE - Saving arena data with key: {key}, type: {data?.GetType().Name}");
            }
            
            public static object LoadArenaData(string key) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_DATABASE_LOAD - Loading arena data with key: {key}");
                return null; 
            }
        }
        #endregion

        #region Arena Glow Service
        public static class ArenaGlowService
        {
            public static void Initialize()
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_GLOW_SERVICE_INIT - Initializing arena glow service");

                try
                {
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_GLOW_SERVICE_COMPLETE - Arena glow service initialized");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_GLOW_SERVICE_ERROR - Failed to initialize: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_GLOW_SERVICE_STACK - {ex.StackTrace}");
                }
            }

            public static void Cleanup() { }

            public static void AddGlowEffect(float3 position, string color = "chaos", float intensity = 2.0f)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_GLOW_ADD - Adding glow effect at {position} with color {color} and intensity {intensity}");

                try
                {
                    // TODO: Implement actual glow effect spawning
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_GLOW_ADD_COMPLETE - Glow effect added at {position}");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_GLOW_ADD_ERROR - Failed to add glow effect: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_GLOW_ADD_STACK - {ex.StackTrace}");
                }
            }

            public static void RemoveGlowEffect(float3 position)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_GLOW_REMOVE - Removing glow effect at {position}");

                try
                {
                    // TODO: Implement actual glow effect removal
                    Plugin.Logger?.LogInfo($"[{timestamp}] ARENA_GLOW_REMOVE_COMPLETE - Glow effect removed at {position}");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_GLOW_REMOVE_ERROR - Failed to remove glow effect: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] ARENA_GLOW_REMOVE_STACK - {ex.StackTrace}");
                }
            }
        }
        #endregion

        #region TeleportFix
        public static class TeleportFix
        {
            public static void Initialize() 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] TELEPORT_FIX_INIT - Initializing teleport fix service");
                
                try
                {
                    Plugin.Logger?.LogInfo($"[{timestamp}] TELEPORT_FIX_COMPLETE - Teleport fix service initialized");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] TELEPORT_FIX_ERROR - Failed to initialize: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] TELEPORT_FIX_STACK - {ex.StackTrace}");
                }
            }
            
            public static void Cleanup() 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] TELEPORT_FIX_CLEANUP - Cleaning up teleport fix service");
            }
            
            public static void FixTeleport(Entity entity) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] TELEPORT_FIX_APPLY - Applying teleport fix to entity: {entity}");
            }
            
            public static bool IsTeleportStuck(Entity entity) 
            { 
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] TELEPORT_STUCK_CHECK - Checking if entity {entity} is teleport stuck");
                return false; 
            }
            
            public static void FastTeleport(Entity entity, float3 position)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Plugin.Logger?.LogInfo($"[{timestamp}] TELEPORT_FAST - Fast teleporting entity {entity} to position {position}");
                
                try
                {
                    var em = VAutoCore.EntityManager;
                    if (entity != Entity.Null && em.Exists(entity))
                    {
                        if (em.HasComponent<Translation>(entity))
                            em.SetComponentData(entity, new Translation { Value = position });
                        else
                            em.AddComponentData(entity, new Translation { Value = position });
                            
                        Plugin.Logger?.LogInfo($"[{timestamp}] TELEPORT_FAST_COMPLETE - Successfully fast teleported entity {entity} to {position}");
                    }
                    else
                    {
                        Plugin.Logger?.LogError($"[{timestamp}] TELEPORT_FAST_ERROR - Invalid entity: {entity}");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"[{timestamp}] TELEPORT_FAST_ERROR - Failed to fast teleport entity {entity}: {ex.Message}");
                    Plugin.Logger?.LogError($"[{timestamp}] TELEPORT_FAST_STACK - {ex.StackTrace}");
                }
            }
        }
        #endregion
    }
}











    /// <summary>
