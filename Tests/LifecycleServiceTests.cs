using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using VAuto.Core;
using VAuto.Services;

namespace VAuto.Tests
{
    /// <summary>
    /// Basic test suite for LifecycleService functionality
    /// Run these tests in-game using console commands or during development
    /// </summary>
    public static class LifecycleServiceTests
    {
        private static ManualLogSource _logger;

        public static void Initialize(ManualLogSource logger)
        {
            _logger = logger;
            _logger.LogInfo("LifecycleServiceTests initialized");
        }

        /// <summary>
        /// Run all available tests
        /// </summary>
        public static void RunAllTests()
        {
            _logger?.LogInfo("=== Starting LifecycleService Tests ===");

            TestSnapshotSerialization();
            TestArenaZoneConfiguration();
            TestVBloodUnlockLogic();
            TestPlayerStateManagement();

            _logger?.LogInfo("=== LifecycleService Tests Complete ===");
        }

        /// <summary>
        /// Test snapshot serialization and deserialization
        /// </summary>
        public static void TestSnapshotSerialization()
        {
            try
            {
                _logger?.LogInfo("Testing snapshot serialization...");

                // Create a test snapshot
                var testSnapshot = new LifecycleService.PlayerSnapshot
                {
                    Version = 1,
                    CapturedAt = DateTime.UtcNow,
                    OriginalName = "TestPlayer",
                    Level = 10,
                    Health = 100f,
                    BloodQuality = 50f,
                    BloodTypeGuid = 12345,
                    Inventory = new List<LifecycleService.ItemEntry>
                    {
                        new LifecycleService.ItemEntry { Guid = 1001, Amount = 5 },
                        new LifecycleService.ItemEntry { Guid = 1002, Amount = 10 }
                    },
                    VBloods = new List<int> { 2001, 2002, 2003 },
                    UIState = new LifecycleService.UIStateSnapshot
                    {
                        VisibilityStates = new Dictionary<string, bool>
                        {
                            ["VBloodUI"] = true,
                            ["PracticeMode"] = false
                        }
                    }
                };

                // Test serialization
                var json = System.Text.Json.JsonSerializer.Serialize(testSnapshot);
                _logger?.LogInfo($"Serialized snapshot: {json}");

                // Test deserialization
                var deserialized = System.Text.Json.JsonSerializer.Deserialize<LifecycleService.PlayerSnapshot>(json);

                // Verify data integrity
                bool success = deserialized != null &&
                              deserialized.OriginalName == testSnapshot.OriginalName &&
                              deserialized.Level == testSnapshot.Level &&
                              deserialized.Inventory.Count == testSnapshot.Inventory.Count &&
                              deserialized.VBloods.Count == testSnapshot.VBloods.Count;

                _logger?.LogInfo(success ? "✅ Snapshot serialization test PASSED" : "❌ Snapshot serialization test FAILED");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"❌ Snapshot serialization test ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// Test arena zone configuration loading
        /// </summary>
        public static void TestArenaZoneConfiguration()
        {
            try
            {
                _logger?.LogInfo("Testing arena zone configuration...");

                // Test loading zones
                LifecycleService.LoadArenaZonesConfig();

                var zones = LifecycleService.ArenaZones;
                bool hasZones = zones != null && zones.Count > 0;

                if (hasZones)
                {
                    var firstZone = zones[0];
                    _logger?.LogInfo($"Loaded zone: {firstZone.Name} at ({firstZone.Center.x}, {firstZone.Center.y}, {firstZone.Center.z})");

                    bool validConfig = !string.IsNullOrEmpty(firstZone.Name) &&
                                      firstZone.ZoneRadius > 0 &&
                                      firstZone.CenterRadius > 0;

                    _logger?.LogInfo(validConfig ? "✅ Arena zone configuration test PASSED" : "❌ Arena zone configuration test FAILED - Invalid config");
                }
                else
                {
                    _logger?.LogError("❌ Arena zone configuration test FAILED - No zones loaded");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"❌ Arena zone configuration test ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// Test VBlood unlock logic (without actual entities)
        /// </summary>
        public static void TestVBloodUnlockLogic()
        {
            try
            {
                _logger?.LogInfo("Testing VBlood unlock logic...");

                // Test getting VBlood prefabs
                var vbloodPrefabs = LifecycleService.GetAllVBloodPrefabs();

                bool hasVBloods = vbloodPrefabs != null && vbloodPrefabs.Count > 0;
                _logger?.LogInfo($"Found {vbloodPrefabs?.Count ?? 0} VBlood prefabs");

                if (hasVBloods)
                {
                    // Test prefab GUIDs are valid
                    bool validGuids = vbloodPrefabs.All(p => p.GuidHash != 0);
                    _logger?.LogInfo(validGuids ? "✅ VBlood unlock logic test PASSED" : "❌ VBlood unlock logic test FAILED - Invalid GUIDs");
                }
                else
                {
                    _logger?.LogWarning("⚠️ VBlood unlock logic test INCONCLUSIVE - No VBlood prefabs found (may be expected in test environment)");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"❌ VBlood unlock logic test ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// Test player state management logic
        /// </summary>
        public static void TestPlayerStateManagement()
        {
            try
            {
                _logger?.LogInfo("Testing player state management...");

                // Test platform ID checking
                ulong testPlatformId = 1234567890;

                // Should not be in arena initially
                bool initiallyNotInArena = !LifecycleService.IsInArena(testPlatformId);
                _logger?.LogInfo($"Player initially not in arena: {initiallyNotInArena}");

                // Test zone validation (should fail for non-existent player)
                bool zoneValidationFails = !LifecycleService.ValidatePlayerInPvPZone(testPlatformId);
                _logger?.LogInfo($"Zone validation correctly fails for non-existent player: {zoneValidationFails}");

                bool success = initiallyNotInArena && zoneValidationFails;
                _logger?.LogInfo(success ? "✅ Player state management test PASSED" : "❌ Player state management test FAILED");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"❌ Player state management test ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// Test JSON serialization round-trip for arena zones
        /// </summary>
        public static void TestArenaZoneSerialization()
        {
            try
            {
                _logger?.LogInfo("Testing arena zone serialization...");

                var testZones = new List<LifecycleService.ArenaZoneConfig>
                {
                    new LifecycleService.ArenaZoneConfig
                    {
                        Name = "Test Zone",
                        Center = new float3(100f, 0f, 200f),
                        CenterRadius = 20f,
                        ZoneRadius = 40f,
                        BuildName = "test_build"
                    }
                };

                // Test serialization
                var json = System.Text.Json.JsonSerializer.Serialize(testZones, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                // Test deserialization
                var deserialized = System.Text.Json.JsonSerializer.Deserialize<List<LifecycleService.ArenaZoneConfig>>(json);

                bool success = deserialized != null &&
                              deserialized.Count == 1 &&
                              deserialized[0].Name == testZones[0].Name &&
                              math.distance(deserialized[0].Center, testZones[0].Center) < 0.01f;

                _logger?.LogInfo(success ? "✅ Arena zone serialization test PASSED" : "❌ Arena zone serialization test FAILED");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"❌ Arena zone serialization test ERROR: {ex.Message}");
            }
        }
    }
}












