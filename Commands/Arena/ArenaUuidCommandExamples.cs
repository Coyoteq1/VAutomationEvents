using System;
using Unity.Entities;
using ProjectM;
using VAuto.Services.Systems;
using VAuto.Utilities;

namespace VAuto.Commands.Arena
{
    /// <summary>
    /// Example command usage for UUID-enabled snapshot system
    /// </summary>
    public static class ArenaUuidCommandExamples
    {
        /// <summary>
        /// Example: Create snapshot with UUID tracking
        /// </summary>
        public static void CreateArenaSnapshotWithUuid(Entity user, Entity character, string arenaId)
        {
            // The UUID is now automatically generated in CreateSnapshot
            var success = EnhancedArenaSnapshotService.CreateSnapshot(user, character, arenaId);
            
            if (success)
            {
                // Get the generated UUID for reference
                var userData = VRCore.EM.GetComponentData<User>(user);
                var snapshotUuid = EnhancedArenaSnapshotService.GetSnapshotUuid(
                    userData.PlatformId.ToString(), 
                    arenaId
                );
                
                Console.WriteLine($"Arena snapshot created with UUID: {snapshotUuid}");
                
                // Can also retrieve snapshot directly by UUID
                var snapshot = EnhancedArenaSnapshotService.GetSnapshotByUuid(snapshotUuid);
                if (snapshot != null)
                {
                    Console.WriteLine($"Snapshot verified: Player {snapshot.OriginalName}, Arena {snapshot.ArenaId}");
                }
            }
        }
        
        /// <summary>
        /// Example: Restore snapshot using UUID
        /// </summary>
        public static void RestoreArenaSnapshotByUuid(string snapshotUuid)
        {
            if (!SnapshotUuidGenerator.IsValidUuid(snapshotUuid))
            {
                Console.WriteLine("Invalid snapshot UUID format");
                return;
            }
            
            var snapshot = EnhancedArenaSnapshotService.GetSnapshotByUuid(snapshotUuid);
            if (snapshot == null)
            {
                Console.WriteLine($"No snapshot found with UUID: {snapshotUuid}");
                return;
            }
            
            // Restore using the traditional method (it will find the UUID internally)
            var success = EnhancedArenaSnapshotService.RestoreSnapshot(snapshot.CharacterId, snapshot.ArenaId);
            
            if (success)
            {
                Console.WriteLine($"Snapshot {snapshotUuid} restored successfully for player {snapshot.CharacterId}");
            }
        }
        
        /// <summary>
        /// Example: List all snapshots with their UUIDs
        /// </summary>
        public static void ListAllSnapshotsWithUuids()
        {
            var snapshots = EnhancedArenaSnapshotService.GetAllSnapshots();
            
            Console.WriteLine($"=== Active Snapshots ({snapshots.Count}) ===");
            foreach (var snapshot in snapshots)
            {
                Console.WriteLine($"UUID: {snapshot.SnapshotUuid}");
                Console.WriteLine($"  Player: {snapshot.OriginalName} ({snapshot.CharacterId})");
                Console.WriteLine($"  Arena: {snapshot.ArenaId}");
                Console.WriteLine($"  Created: {snapshot.SnapshotTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine();
            }
        }
        
        /// <summary>
        /// Example: Delete specific snapshot by UUID
        /// </summary>
        public static void DeleteSnapshotByUuid(string snapshotUuid)
        {
            if (!SnapshotUuidGenerator.IsValidUuid(snapshotUuid))
            {
                Console.WriteLine("Invalid snapshot UUID format");
                return;
            }
            
            var success = EnhancedArenaSnapshotService.DeleteSnapshotByUuid(snapshotUuid);
            
            if (success)
            {
                Console.WriteLine($"Snapshot {snapshotUuid} deleted successfully");
            }
            else
            {
                Console.WriteLine($"Failed to delete snapshot {snapshotUuid} (not found)");
            }
        }
        
        /// <summary>
        /// Example: Generate metadata UUID for tracking purposes
        /// </summary>
        public static string GenerateSessionTrackingUuid(string sessionContext = "arena_session")
        {
            var metadataUuid = SnapshotUuidGenerator.GenerateMetadataUuid(sessionContext);
            Console.WriteLine($"Generated session tracking UUID: {metadataUuid}");
            return metadataUuid;
        }
    }
}
