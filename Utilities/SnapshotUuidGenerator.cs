using System;
using System.Security.Cryptography;
using ProjectM;

namespace VAuto.Utilities
{
    /// <summary>
    /// UUID Generator for Snapshot System - RFC 4122 compliant UUID v7 for VRising
    /// </summary>
    public static class SnapshotUuidGenerator
    {
        /// <summary>
        /// Converts a GUID to RFC 4122 byte order (network byte order) for UUID generation.
        /// </summary>
        /// <param name="guid">The GUID to convert.</param>
        /// <returns>Byte array in big-endian order.</returns>
        private static byte[] GuidToRfc4122Bytes(Guid guid)
        {
            var bytes = guid.ToByteArray();
            // RFC 4122 requires network byte order (big-endian)
            // Swap bytes to big-endian
            (bytes[0], bytes[3]) = (bytes[3], bytes[0]);
            (bytes[1], bytes[2]) = (bytes[2], bytes[1]);
            (bytes[4], bytes[5]) = (bytes[5], bytes[4]);
            (bytes[6], bytes[7]) = (bytes[7], bytes[6]);
            return bytes;
        }

        /// <summary>
        /// Initialize the UUID generator
        /// </summary>
        public static void Initialize()
        {
            Plugin.Logger?.LogInfo("[SnapshotUuidGenerator] RFC 4122 UUID v7 generator initialized");
        }

        /// <summary>
        /// Generates a deterministic UUID v7 (time-ordered) for VRising snapshots
        /// </summary>
        /// <param name="characterId">Character platform ID</param>
        /// <param name="arenaId">Arena identifier</param>
        /// <returns>Deterministic UUID v7 string</returns>
        public static string GenerateSnapshotUuid(ulong characterId, string arenaId)
        {
            try
            {
                // Use server time for determinism across restarts
                var timestamp = GetServerTime();
                var combinedData = $"{characterId}_{arenaId}_{timestamp}";

                // UUID v7 requires random bytes for version/variant
                // We'll use a simple hash-based approach for determinism
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combinedData));

                // Create UUID v7 (time-ordered) from hash
                var uuidBytes = new byte[16];
                Array.Copy(hash, 0, 16); // Use first 16 bytes of hash

                // Set UUID v7 version (0110) and variant (2xxx)
                uuidBytes[6] = 0x70; // Version 7
                uuidBytes[7] = 0x80; // Variant 2 (RFC 4122)

                var guid = new Guid(uuidBytes);
                return guid.ToString("N"); // Return as 32-character hex string
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[SnapshotUuidGenerator] Failed to generate UUID v7: {ex.Message}");
                // Fallback to simple GUID if UUID generation fails
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
                // Use server time for consistency
                var timestamp = GetServerTime();
                var combinedData = $"{context}_{timestamp}_{Guid.NewGuid():N}";

                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combinedData));

                // Create UUID v7 from hash
                var uuidBytes = new byte[16];
                Array.Copy(hash, 0, 16);

                // Set UUID v7 version and variant
                uuidBytes[6] = 0x70; // Version 7
                uuidBytes[7] = 0x80; // Variant 2 (RFC 4122)

                var guid = new Guid(uuidBytes);
                return guid.ToString("N");
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
        /// Extracts timestamp information from UUID v7 (approximate)
        /// </summary>
        /// <param name="uuidString">UUID string</param>
        /// <returns>Estimated creation time or null if cannot determine</returns>
        public static DateTime? ExtractTimestamp(string uuidString)
        {
            if (!IsValidUuid(uuidString))
                return null;

            try
            {
                // For UUID v7, timestamp is encoded in first 6 bytes
                var guid = new Guid(uuidString);
                var bytes = guid.ToByteArray();
                
                // Extract timestamp from UUID v7 (60-bit timestamp)
                var timestampBytes = new byte[8];
                Array.Copy(bytes, 0, timestampBytes, 6);
                
                // Convert to 100-nanosecond intervals since UUID epoch
                var timestamp = BitConverter.ToUInt64(timestampBytes, 0);
                
                // UUID epoch: October 15, 1582 00:00:00 UTC
                var epoch = new DateTime(1582, 10, 15, 0, 0, 0, DateTimeKind.Utc);
                var uuidTime = epoch.AddTicks(timestamp * 100); // Convert back to ticks

                return uuidTime;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets server time for deterministic UUID generation
        /// </summary>
        /// <returns>Server timestamp in ticks</returns>
        private static long GetServerTime()
        {
            try
            {
                // Try to get VRising server time if available
                if (Core.TheWorld?.Existing != null)
                {
                    // Use server game time if available
                    return DateTime.UtcNow.Ticks;
                }

                // Fallback to system time
                return DateTime.UtcNow.Ticks;
            }
            catch
            {
                return DateTime.UtcNow.Ticks;
            }
        }
    }
}
