using System;
using VAuto.Utilities;

namespace VAuto.Tests
{
    /// <summary>
    /// Simple test for UUID Generator functionality
    /// </summary>
    public static class SnapshotUuidGeneratorTests
    {
        public static void RunTests()
        {
            Console.WriteLine("=== Snapshot UUID Generator Tests ===");
            
            // Test 1: Basic UUID generation
            var uuid1 = SnapshotUuidGenerator.GenerateSnapshotUuid(12345, "arena_1");
            var uuid2 = SnapshotUuidGenerator.GenerateSnapshotUuid(12345, "arena_2");
            var uuid3 = SnapshotUuidGenerator.GenerateSnapshotUuid(67890, "arena_1");
            
            Console.WriteLine($"UUID 1: {uuid1}");
            Console.WriteLine($"UUID 2: {uuid2}");
            Console.WriteLine($"UUID 3: {uuid3}");
            
            // Test 2: Uniqueness
            bool allUnique = uuid1 != uuid2 && uuid2 != uuid3 && uuid1 != uuid3;
            Console.WriteLine($"All UUIDs unique: {allUnique}");
            
            // Test 3: Validation
            bool valid1 = SnapshotUuidGenerator.IsValidUuid(uuid1);
            bool valid2 = SnapshotUuidGenerator.IsValidUuid("invalid-uuid");
            Console.WriteLine($"UUID1 valid: {valid1}");
            Console.WriteLine($"Invalid UUID valid: {valid2}");
            
            // Test 4: Metadata UUID generation
            var metaUuid1 = SnapshotUuidGenerator.GenerateMetadataUuid("test_context");
            var metaUuid2 = SnapshotUuidGenerator.GenerateMetadataUuid();
            
            Console.WriteLine($"Meta UUID 1: {metaUuid1}");
            Console.WriteLine($"Meta UUID 2: {metaUuid2}");
            Console.WriteLine($"Meta UUIDs unique: {metaUuid1 != metaUuid2}");
            
            // Test 5: Length consistency
            bool correctLength = uuid1.Length == 32 && metaUuid1.Length == 32;
            Console.WriteLine($"Correct length (32 chars): {correctLength}");
            
            Console.WriteLine("=== UUID Generator Tests Complete ===");
        }
    }
}
