using HarmonyLib;
using ProjectM;
using Unity.Collections;
using Unity.Entities;
using VAuto.Core;
using VAuto.Services.Core;
using System.Collections.Generic;

namespace VAuto.Patches
{
    /// <summary>
    /// Harmony patch for RepairVBloodProgressionSystem to implement snapshot-driven unlock system
    /// Flow: Enter→Repair→Unlock(Snapshot)→Exit→Repair→Unlock(Snapshot) - Uses snapshot data, no hardcoding
    /// </summary>
    [HarmonyPatch(typeof(RepairVBloodProgressionSystem), nameof(RepairVBloodProgressionSystem.OnUpdate))]
    internal static class RepairVBloodProgressionSystemPatch
    {
        private static readonly ManualLogSource Log = Plugin.Logger;
        
        // Track repair phases for arena operations
        private static bool _isArenaEntryPhase = false;
        private static bool _isArenaExitPhase = false;

        /// <summary>
        /// Prefix patch - controls VBlood repair based on arena phase
        /// </summary>
        public static bool Prefix(RepairVBloodProgressionSystem __instance)
        {
            try
            {
                var arenaState = GetCurrentArenaState();
                
                switch (arenaState)
                {
                    case ArenaPhase.Entering:
                        return HandleArenaEntryRepair();
                        
                    case ArenaPhase.Exiting:
                        return HandleArenaExitRepair();
                        
                    case ArenaPhase.Active:
                        // Skip repairs during active arena phase
                        Log?.LogDebug("[RepairVBloodPatch] Skipping repair - arena active");
                        return false;
                        
                    default:
                        // Allow normal repairs outside arena
                        return true;
                }
            }
            catch (System.Exception ex)
            {
                Log?.LogError($"[RepairVBloodPatch] Error in prefix patch: {ex.Message}");
                return true; // Allow original to run on error
            }
        }

        /// <summary>
        /// Handle repair during arena entry phase
        /// </summary>
        private static bool HandleArenaEntryRepair()
        {
            if (!_isArenaEntryPhase)
            {
                Log?.LogInfo("[RepairVBloodPatch] Arena entry phase - allowing repair");
                _isArenaEntryPhase = true;
                return true; // Allow repair
            }
            
            // After repair, trigger snapshot-driven unlock
            TriggerSnapshotDrivenUnlock(isEntry: true);
            return false; // Skip additional repairs
        }

        /// <summary>
        /// Handle repair during arena exit phase
        /// </summary>
        private static bool HandleArenaExitRepair()
        {
            if (!_isArenaExitPhase)
            {
                Log?.LogInfo("[RepairVBloodPatch] Arena exit phase - allowing repair");
                _isArenaExitPhase = true;
                return true; // Allow repair
            }
            
            // After repair, trigger snapshot-driven unlock
            TriggerSnapshotDrivenUnlock(isEntry: false);
            return false; // Skip additional repairs
        }

        /// <summary>
        /// Trigger snapshot-driven unlock based on player snapshot data
        /// </summary>
        private static void TriggerSnapshotDrivenUnlock(bool isEntry)
        {
            try
            {
                // Get all online players
                var playerQuery = Core.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<ProjectM.Network.User>(),
                    ComponentType.ReadOnly<ProjectM.Network.PlayerCharacter>()
                );

                var userEntities = playerQuery.ToEntityArray(Allocator.Temp);
                var totalUnlocks = 0;

                foreach (var userEntity in userEntities)
                {
                    if (!userEntity.Has<ProjectM.Network.User>()) continue;

                    var user = userEntity.Read<ProjectM.Network.User>();
                    if (!user.IsConnected) continue;

                    // Check if this player has pending unlocks from their snapshot
                    if (!ArenaUnlockService.HasPendingUnlocks(user.PlatformId))
                    {
                        Log?.LogDebug($"[RepairVBloodPatch] No pending unlocks for player {user.PlatformId} during {(isEntry ? "entry" : "exit")}");
                        continue;
                    }

                    // Get pending unlock count before applying
                    var pendingCount = ArenaUnlockService.GetPendingUnlockCount(user.PlatformId);
                    
                    // Apply snapshot-driven unlocks
                    ArenaUnlockService.UnlockPendingVBloods(user.PlatformId, userEntity, isEntry ? "entry" : "exit");
                    
                    var appliedCount = pendingCount - ArenaUnlockService.GetPendingUnlockCount(user.PlatformId);
                    totalUnlocks += appliedCount;
                    
                    Log?.LogInfo($"[RepairVBloodPatch] Applied {appliedCount} snapshot-driven unlocks for player {user.PlatformId} during {(isEntry ? "entry" : "exit")}");
                }

                userEntities.Dispose();
                playerQuery.Dispose();
                
                Log?.LogInfo($"[RepairVBloodPatch] Total snapshot-driven unlocks applied: {totalUnlocks} during {(isEntry ? "entry" : "exit")}");
            }
            catch (System.Exception ex)
            {
                Log?.LogError($"[RepairVBloodPatch] Error in snapshot-driven unlock: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current arena state based on system conditions
        /// </summary>
        private static ArenaPhase GetCurrentArenaState()
        {
            try
            {
                // Check if any arena operations are currently active
                if (IsArenaOperationActive())
                {
                    return ArenaPhase.Active;
                }

                // Check for arena entry signals
                if (IsArenaEntrySignal())
                {
                    return ArenaPhase.Entering;
                }

                // Check for arena exit signals  
                if (IsArenaExitSignal())
                {
                    return ArenaPhase.Exiting;
                }

                return ArenaPhase.None;
            }
            catch (System.Exception ex)
            {
                Log?.LogWarning($"[RepairVBloodPatch] Error detecting arena state: {ex.Message}");
                return ArenaPhase.None;
            }
        }

        /// <summary>
        /// Check if arena operation is currently active
        /// </summary>
        private static bool IsArenaOperationActive()
        {
            try
            {
                if (ServiceManager.TryGetService<EnhancedArenaSnapshotService>(out var arenaService))
                {
                    return arenaService.IsArenaOperationActive();
                }

                if (ServiceManager.TryGetService<GameSystems>(out var gameSystems))
                {
                    return gameSystems.HasActiveArenaHooks();
                }

                return false;
            }
            catch (System.Exception ex)
            {
                Log?.LogWarning($"[RepairVBloodPatch] Error checking arena operations: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if arena entry signal is detected
        /// </summary>
        private static bool IsArenaEntrySignal()
        {
            try
            {
                // Check if players are entering arena
                if (ServiceManager.TryGetService<EnhancedArenaSnapshotService>(out var arenaService))
                {
                    return arenaService.HasPendingArenaEntries();
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if arena exit signal is detected
        /// </summary>
        private static bool IsArenaExitSignal()
        {
            try
            {
                // Check if players are exiting arena
                if (ServiceManager.TryGetService<EnhancedArenaSnapshotService>(out var arenaService))
                {
                    return arenaService.HasPendingArenaExits();
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Reset phase tracking (call when arena operations complete)
        /// </summary>
        public static void ResetPhaseTracking()
        {
            _isArenaEntryPhase = false;
            _isArenaExitPhase = false;
            Log?.LogDebug("[RepairVBloodPatch] Reset phase tracking");
        }

        /// <summary>
        /// Get service status for debugging
        /// </summary>
        public static string GetServiceStatus()
        {
            var unlockServiceStatus = ArenaUnlockService.GetServiceStatus();
            var phaseStatus = $"Entry Phase: {_isArenaEntryPhase}, Exit Phase: {_isArenaExitPhase}";
            
            return $"RepairVBloodPatch Status: {phaseStatus} | {unlockServiceStatus}";
        }
    }

    /// <summary>
    /// Arena phase enumeration for VBlood repair control
    /// </summary>
    public enum ArenaPhase
    {
        None,
        Entering,
        Active,
        Exiting
    }

    /// <summary>
    /// Additional helper methods for EnhancedArenaSnapshotService integration
    /// </summary>
    public static partial class EnhancedArenaSnapshotServiceExtensions
    {
        /// <summary>
        /// Check if any arena operations are currently active
        /// </summary>
        public static bool IsArenaOperationActive(this EnhancedArenaSnapshotService service)
        {
            try
            {
                // Check if service has any active arena operations
                return service.HasActiveOperations();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if service has any active operations
        /// </summary>
        public static bool HasActiveOperations(this EnhancedArenaSnapshotService service)
        {
            // This would need to be implemented in EnhancedArenaSnapshotService
            // For now, return false as a safe default
            return false;
        }

        /// <summary>
        /// Check if there are pending arena entries
        /// </summary>
        public static bool HasPendingArenaEntries(this EnhancedArenaSnapshotService service)
        {
            try
            {
                // Check if players are currently entering arena
                return service.GetPendingEntryCount() > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if there are pending arena exits
        /// </summary>
        public static bool HasPendingArenaExits(this EnhancedArenaSnapshotService service)
        {
            try
            {
                // Check if players are currently exiting arena
                return service.GetPendingExitCount() > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get pending entry count
        /// </summary>
        public static int GetPendingEntryCount(this EnhancedArenaSnapshotService service)
        {
            // This would need to be implemented in EnhancedArenaSnapshotService
            return 0;
        }

        /// <summary>
        /// Get pending exit count
        /// </summary>
        public static int GetPendingExitCount(this EnhancedArenaSnapshotService service)
        {
            // This would need to be implemented in EnhancedArenaSnapshotService
            return 0;
        }

        /// <summary>
        /// Check if a player has an arena snapshot
        /// </summary>
        public static bool HasPlayerSnapshot(this EnhancedArenaSnapshotService service, ulong platformId)
        {
            try
            {
                // This would need to be implemented in EnhancedArenaSnapshotService
                // Check if player has an existing arena snapshot
                return false;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Additional helper methods for GameSystems integration
    /// </summary>
    public static partial class GameSystemsExtensions
    {
        /// <summary>
        /// Check if any arena hooks are active
        /// </summary>
        public static bool HasActiveArenaHooks(this GameSystems gameSystems)
        {
            try
            {
                // Check if any VBlood hooks are currently active
                return gameSystems.HasActiveVBloodHooks();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if any VBlood hooks are active
        /// </summary>
        public static bool HasActiveVBloodHooks(this GameSystems gameSystems)
        {
            // This would need to be implemented in GameSystems
            // For now, return false as a safe default
            return false;
        }
    }
}
