using System;
using System.Linq;
using System.Security.Cryptography;

namespace VAuto.Utilities
{
    /// <summary>
    /// UUID Generator for Snapshot System - Provides unique identifiers for arena snapshots
    /// </summary>
    public static class SnapshotUuidGenerator
    {
        private static readonly object _lock = new object();
        
        /// <summary>
        /// Generates a unique UUID for a snapshot
        /// </summary>
        /// <param name="characterId">Character platform ID</param>
        /// <param name="arenaId">Arena identifier</param>
        /// <returns>Unique UUID string</returns>
        public static string GenerateSnapshotUuid(ulong characterId, string arenaId)
        {
            try
            {
                lock (_lock)
                {
                    // Combine character ID, arena ID, and timestamp for uniqueness
                    var timestamp = DateTime.UtcNow.Ticks;
                    var combinedData = $"{characterId}_{arenaId}_{timestamp}";
                    
                    // Generate UUID v5 using namespace and combined data
                    var namespaceGuid = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8"); // DNS namespace
                    var nameBytes = System.Text.Encoding.UTF8.GetBytes(combinedData);
                    
                    using var sha1 = SHA1.Create();
                    var hash = sha1.ComputeHash(namespaceGuid.ToByteArray().Concat(nameBytes).ToArray());
                    
                    // Convert to UUID v5 format
                    hash[3] &= 0x0F;
                    hash[3] |= 0x50; // Version 5
                    hash[7] &= 0x3F;
                    hash[7] |= 0x80; // Variant bits
                    
                    var guid = new Guid(hash.Take(16).ToArray());
                    return guid.ToString("N"); // Return as 32-character hex string
                }
            }
            catch (Exception ex)
            {
                // Fallback to simple GUID if UUID generation fails
                Plugin.Logger?.LogError($"[SnapshotUuidGenerator] Failed to generate UUID v5, falling back to GUID: {ex.Message}");
                return Guid.NewGuid().ToString("N");
            }
        }
        
        /// <summary>
        /// Generates a UUID for snapshot metadata tracking
        /// </summary>
        /// <param name="context">Context description (e.g., "arena_entry", "manual_save")</param>
        /// <returns>Unique UUID string</returns>
        public static string GenerateMetadataUuid(string context = "snapshot")
        {
            try
            {
                lock (_lock)
                {
                    var timestamp = DateTime.UtcNow.Ticks;
                    var combinedData = $"{context}_{timestamp}_{Guid.NewGuid():N}";
                    
                    using var sha256 = SHA256.Create();
                    var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combinedData));
                    
                    // Use first 16 bytes for GUID
                    var guid = new Guid(hash.Take(16).ToArray());
                    return guid.ToString("N");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[SnapshotUuidGenerator] Failed to generate metadata UUID: {ex.Message}");
                return Guid.NewGuid().ToString("N");
            }
        }
        
        /// <summary>
        /// Validates if a string is a valid UUID format
        /// </summary>
        /// <param name="uuidString">String to validate</param>
        /// <returns>True if valid UUID format</returns>
        public static bool IsValidUuid(string uuidString)
        {
            return Guid.TryParse(uuidString, out var guid) && guid != Guid.Empty;
        }
        
        /// <summary>
        /// Extracts timestamp information from UUID (approximate)
        /// </summary>
        /// <param name="uuidString">UUID string</param>
        /// <returns>Estimated creation time or null if cannot determine</returns>
        public static DateTime? ExtractTimestamp(string uuidString)
        {
            if (!IsValidUuid(uuidString))
                return null;
            
            try
            {
                // For UUID v1, timestamp is encoded. For our custom UUIDs, we cannot reliably extract
                // This is a placeholder for future UUID v1 implementation if needed
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
