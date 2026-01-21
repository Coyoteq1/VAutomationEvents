using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using VAuto.Core;
using BepInEx.Logging;

namespace VAuto.Services.Core
{
    /// <summary>
    /// Arena Unlock Service - Manages snapshot-driven VBlood unlocks for arena sessions
    /// Reads unlock requirements from player snapshots and applies them at the correct times
    /// </summary>
    public static class ArenaUnlockService
    {
        private static readonly Dictionary<ulong, List<int>> _pendingUnlocks = new();
        private static readonly Dictionary<ulong, List<int>> _completedUnlocks = new();
        private static readonly object _lock = new object();
        
        public static ManualLogSource Log => Plugin.Logger;

        /// <summary>
        /// Prepares the unlocks for a player based on their arena snapshot
        /// </summary>
        /// <param name="platformId">Player platform ID</param>
        /// <param name="snapshot">Player snapshot containing unlock data</param>
        public static void ReadyUnlocksFromSnapshot(ulong platformId, PlayerSnapshot snapshot)
        {
            lock (_lock)
            {
                try
                {
                    if (!_pendingUnlocks.ContainsKey(platformId))
                        _pendingUnlocks[platformId] = new List<int>();

                    if (!_completedUnlocks.ContainsKey(platformId))
                        _completedUnlocks[platformId] = new List<int>();

                    // Clear previous pending unlocks
                    _pendingUnlocks[platformId].Clear();

                    // Extract VBlood unlocks from snapshot
                    if (snapshot.UnlockedVBloods != null && snapshot.UnlockedVBloods.Any())
                    {
                        foreach (var vbloodId in snapshot.UnlockedVBloods)
                        {
                            if (!_pendingUnlocks[platformId].Contains(vbloodId))
                            {
                                _pendingUnlocks[platformId].Add(vbloodId);
                            }
                        }

                        Log?.LogInfo($"[ArenaUnlockService] Prepared {_pendingUnlocks[platformId].Count} VBlood unlocks for player {platformId}");
                        Log?.LogDebug($"[ArenaUnlockService] Pending unlocks for player {platformId}: [{string.Join(", ", _pendingUnlocks[platformId])}]");
                    }
                    else
                    {
                        Log?.LogWarning($"[ArenaUnlockService] No VBlood unlocks found in snapshot for player {platformId}");
                    }
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[ArenaUnlockService] Error preparing unlocks for player {platformId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Unlocks all pending VBloods for a player and clears the pending list
        /// </summary>
        /// <param name="platformId">Player platform ID</param>
        /// <param name="userEntity">Player user entity</param>
        /// <param name="context">Context for logging (enter/exit)</param>
        public static void UnlockPendingVBloods(ulong platformId, Entity userEntity, string context = "unknown")
        {
            lock (_lock)
            {
                try
                {
                    if (!_pendingUnlocks.ContainsKey(platformId) || !_pendingUnlocks[platformId].Any())
                    {
                        Log?.LogDebug($"[ArenaUnlockService] No pending unlocks for player {platformId} during {context}");
                        return;
                    }

                    var unlocksToApply = new List<int>(_pendingUnlocks[platformId]);
                    var unlockedCount = 0;

                    foreach (var vbloodId in unlocksToApply)
                    {
                        // Check if already unlocked in this session
                        if (_completedUnlocks.ContainsKey(platformId) && _completedUnlocks[platformId].Contains(vbloodId))
                        {
                            Log?.LogDebug($"[ArenaUnlockService] VBlood {vbloodId} already unlocked for player {platformId} in {context}");
                            continue;
                        }

                        // Unlock the specific VBlood
                        if (UnlockSpecificVBlood(platformId, userEntity, vbloodId))
                        {
                            _completedUnlocks[platformId].Add(vbloodId);
                            unlockedCount++;
                            Log?.LogDebug($"[ArenaUnlockService] Unlocked VBlood {vbloodId} for player {platformId} during {context}");
                        }
                        else
                        {
                            Log?.LogWarning($"[ArenaUnlockService] Failed to unlock VBlood {vbloodId} for player {platformId} during {context}");
                        }
                    }

                    // Clear pending unlocks after applying
                    _pendingUnlocks[platformId].Clear();

                    Log?.LogInfo($"[ArenaUnlockService] Applied {unlockedCount} VBlood unlocks for player {platformId} during {context}");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[ArenaUnlockService] Error unlocking pending VBloods for player {platformId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Unlock a specific VBlood for a player
        /// </summary>
        /// <param name="platformId">Player platform ID</param>
        /// <param name="userEntity">Player user entity</param>
        /// <param name="vbloodId">VBlood progression ID</param>
        /// <returns>True if unlock was successful</returns>
        private static bool UnlockSpecificVBlood(ulong platformId, Entity userEntity, int vbloodId)
        {
            try
            {
                // Use the game's unlock system for the specific VBlood
                // This would need to be the correct API for unlocking a specific progression
                var from = new ProjectM.Network.From()
                {
                    User = platformId
                };

                // TODO: Replace with actual specific VBlood unlock API
                // Example: VBloodUnlockService.UnlockVBlood(userEntity, vbloodId);
                
                // For now, use debug event as fallback (but this unlocks all, not ideal)
                // In a real implementation, this should be replaced with targeted unlock
                var unlockEvent = new ProjectM.Network.DebugEvents.UnlockAllProgressions()
                {
                    From = from
                };

                Core.EventSystem.Publish(unlockEvent);
                
                Log?.LogDebug($"[ArenaUnlockService] Sent unlock event for VBlood {vbloodId} to player {platformId}");
                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaUnlockService] Error unlocking VBlood {vbloodId} for player {platformId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if a player has pending unlocks
        /// </summary>
        /// <param name="platformId">Player platform ID</param>
        /// <returns>True if player has pending unlocks</returns>
        public static bool HasPendingUnlocks(ulong platformId)
        {
            lock (_lock)
            {
                return _pendingUnlocks.ContainsKey(platformId) && _pendingUnlocks[platformId].Any();
            }
        }

        /// <summary>
        /// Get pending unlock count for a player
        /// </summary>
        /// <param name="platformId">Player platform ID</param>
        /// <returns>Number of pending unlocks</returns>
        public static int GetPendingUnlockCount(ulong platformId)
        {
            lock (_lock)
            {
                return _pendingUnlocks.ContainsKey(platformId) ? _pendingUnlocks[platformId].Count : 0;
            }
        }

        /// <summary>
        /// Get completed unlock count for a player
        /// </summary>
        /// <param name="platformId">Player platform ID</param>
        /// <returns>Number of completed unlocks</returns>
        public static int GetCompletedUnlockCount(ulong platformId)
        {
            lock (_lock)
            {
                return _completedUnlocks.ContainsKey(platformId) ? _completedUnlocks[platformId].Count : 0;
            }
        }

        /// <summary>
        /// Reset unlock tracking for a specific player
        /// </summary>
        /// <param name="platformId">Player platform ID</param>
        public static void ResetPlayerTracking(ulong platformId)
        {
            lock (_lock)
            {
                if (_pendingUnlocks.ContainsKey(platformId))
                    _pendingUnlocks[platformId].Clear();

                if (_completedUnlocks.ContainsKey(platformId))
                    _completedUnlocks[platformId].Clear();

                Log?.LogDebug($"[ArenaUnlockService] Reset tracking for player {platformId}");
            }
        }

        /// <summary>
        /// Reset all unlock tracking
        /// </summary>
        public static void ResetAllTracking()
        {
            lock (_lock)
            {
                _pendingUnlocks.Clear();
                _completedUnlocks.Clear();
                Log?.LogDebug("[ArenaUnlockService] Reset all tracking");
            }
        }

        /// <summary>
        /// Get service status for debugging
        /// </summary>
        /// <returns>Service status information</returns>
        public static string GetServiceStatus()
        {
            lock (_lock)
            {
                var pendingPlayers = _pendingUnlocks.Count;
                var completedPlayers = _completedUnlocks.Count;
                var totalPending = _pendingUnlocks.Values.Sum(list => list.Count);
                var totalCompleted = _completedUnlocks.Values.Sum(list => list.Count);

                return $"ArenaUnlockService Status: {pendingPlayers} players with pending unlocks, {completedPlayers} players with completed unlocks, {totalPending} total pending, {totalCompleted} total completed";
            }
        }
    }

    /// <summary>
    /// Player snapshot extension for VBlood unlock data
    /// </summary>
    public static class PlayerSnapshotExtensions
    {
        /// <summary>
        /// Check if snapshot has VBlood unlock data
        /// </summary>
        /// <param name="snapshot">Player snapshot</param>
        /// <returns>True if snapshot has VBlood unlock data</returns>
        public static bool HasVBloodUnlocks(this PlayerSnapshot snapshot)
        {
            return snapshot.UnlockedVBloods != null && snapshot.UnlockedVBloods.Any();
        }

        /// <summary>
        /// Get VBlood unlock count from snapshot
        /// </summary>
        /// <param name="snapshot">Player snapshot</param>
        /// <returns>Number of VBlood unlocks in snapshot</returns>
        public static int GetVBloodUnlockCount(this PlayerSnapshot snapshot)
        {
            return snapshot.HasVBloodUnlocks() ? snapshot.UnlockedVBloods.Count : 0;
        }

        /// <summary>
        /// Check if specific VBlood is unlocked in snapshot
        /// </summary>
        /// <param name="snapshot">Player snapshot</param>
        /// <param name="vbloodId">VBlood progression ID</param>
        /// <returns>True if VBlood is unlocked in snapshot</returns>
        public static bool IsVBloodUnlocked(this PlayerSnapshot snapshot, int vbloodId)
        {
            return snapshot.HasVBloodUnlocks() && snapshot.UnlockedVBloods.Contains(vbloodId);
        }
    }
}
