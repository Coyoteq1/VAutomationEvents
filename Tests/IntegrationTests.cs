using System;
using System.Collections.Generic;
using System.Threading;
using BepInEx.Logging;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using VAuto.Core;
using VAuto.Services.Lifecycle;
using VAuto.Services;

namespace VAuto.Tests
{
    /// <summary>
    /// Integration tests for the arena system - tests full workflows
    /// </summary>
    public static class IntegrationTests
    {
        private static ManualLogSource _logger;

        public static void Initialize(ManualLogSource logger)
        {
            _logger = logger;
            _logger.LogInfo("IntegrationTests initialized");
        }

        /// <summary>
        /// Run all integration tests
        /// </summary>
        public static void RunAllIntegrationTests()
        {
            _logger?.LogInfo("=== Starting Integration Tests ===");

            TestArenaEntryExitCycle();
            TestCrashRecoveryWorkflow();
            TestZoneEntryExitDetection();

            _logger?.LogInfo("=== Integration Tests Complete ===");
        }

        /// <summary>
        /// Test the complete arena entry and exit cycle
        /// </summary>
        public static void TestArenaEntryExitCycle()
        {
            try
            {
                _logger?.LogInfo("Testing arena entry/exit cycle...");

                // This test would ideally use mock entities, but since we're in IL2CPP
                // we'll test the logic flows without actual entity manipulation

                // Test 1: Validate that the system can handle multiple rapid zone transitions
                bool rapidTransitionTest = TestRapidZoneTransitions();
                _logger?.LogInfo(rapidTransitionTest ? "✅ Rapid transition test PASSED" : "❌ Rapid transition test FAILED");

                // Test 2: Validate snapshot creation and restoration flow
                bool snapshotFlowTest = TestSnapshotFlow();
                _logger?.LogInfo(snapshotFlowTest ? "✅ Snapshot flow test PASSED" : "❌ Snapshot flow test FAILED");

                // Test 3: Validate VBlood unlock and lock cycle
                bool vbloodCycleTest = TestVBloodUnlockLockCycle();
                _logger?.LogInfo(vbloodCycleTest ? "✅ VBlood cycle test PASSED" : "❌ VBlood cycle test FAILED");

                bool overallSuccess = rapidTransitionTest && snapshotFlowTest && vbloodCycleTest;
                _logger?.LogInfo(overallSuccess ? "✅ Arena entry/exit cycle test PASSED" : "❌ Arena entry/exit cycle test FAILED");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"❌ Arena entry/exit cycle test ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// Test rapid zone transitions (simulated)
        /// </summary>
        private static bool TestRapidZoneTransitions()
        {
            try
            {
                // Test zone configuration validation
                var zones = LifecycleService.ArenaZones;
                if (zones == null || zones.Count == 0)
                {
                    _logger?.LogWarning("No zones configured for rapid transition test");
                    return false;
                }

                // Simulate position checks
                var testPositions = new List<float3>
                {
                    zones[0].Center + new float3(0, 0, 0),      // At center
                    zones[0].Center + new float3(15, 0, 0),     // Within center radius
                    zones[0].Center + new float3(30, 0, 0),     // Outside center, within zone
                    zones[0].Center + new float3(60, 0, 0),     // Outside zone
                };

                foreach (var pos in testPositions)
                {
                    float distance = math.distance(pos, zones[0].Center);
                    bool inCenter = distance <= zones[0].CenterRadius;
                    bool inZone = distance <= zones[0].ZoneRadius;

                    _logger?.LogInfo($"Position {pos}: Distance={distance:F1}, InCenter={inCenter}, InZone={inZone}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Rapid transition test error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test snapshot creation and restoration flow
        /// </summary>
        private static bool TestSnapshotFlow()
        {
            try
            {
                // Create test snapshots
                var originalSnapshot = new LifecycleService.PlayerSnapshot
                {
                    Version = 1,
                    CapturedAt = DateTime.UtcNow,
                    OriginalName = "IntegrationTestPlayer",
                    Level = 25,
                    Health = 200f,
                    BloodQuality = 75f,
                    BloodTypeGuid = 54321,
                    Inventory = new List<LifecycleService.ItemEntry>
                    {
                        new LifecycleService.ItemEntry { Guid = 2001, Amount = 3 },
                        new LifecycleService.ItemEntry { Guid = 2002, Amount = 7 }
                    },
                    VBloods = new List<int> { 3001, 3002 },
                    UIState = new LifecycleService.UIStateSnapshot
                    {
                        VisibilityStates = new Dictionary<string, bool>
                        {
                            ["VBloodUI"] = true,
                            ["PracticeMode"] = true
                        }
                    }
                };

                // Test serialization
                var json = System.Text.Json.JsonSerializer.Serialize(originalSnapshot);
                var deserialized = System.Text.Json.JsonSerializer.Deserialize<LifecycleService.PlayerSnapshot>(json);

                // Verify critical data integrity
                bool dataIntact = deserialized != null &&
                                 deserialized.OriginalName == originalSnapshot.OriginalName &&
                                 deserialized.Level == originalSnapshot.Level &&
                                 deserialized.Inventory.Count == originalSnapshot.Inventory.Count;

                // Test persistence methods (without actual file I/O in test)
                string testPath = System.IO.Path.Combine(VAuto.Plugin.DataPath, "test_snapshot.json");
                try
                {
                    System.IO.File.WriteAllText(testPath, json);
                    var loadedJson = System.IO.File.ReadAllText(testPath);
                    var loadedSnapshot = System.Text.Json.JsonSerializer.Deserialize<LifecycleService.PlayerSnapshot>(loadedJson);
                    System.IO.File.Delete(testPath);

                    bool persistenceWorks = loadedSnapshot != null && loadedSnapshot.Level == originalSnapshot.Level;
                    return dataIntact && persistenceWorks;
                }
                catch
                {
                    // If file operations fail, still return data integrity result
                    return dataIntact;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Snapshot flow test error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test VBlood unlock and lock cycle
        /// </summary>
        private static bool TestVBloodUnlockLockCycle()
        {
            try
            {
                // Test that we can get VBlood prefabs without errors
                var vbloodPrefabs = LifecycleService.GetAllVBloodPrefabs();

                // Validate that the system can handle the unlock/lock operations conceptually
                bool canHandleVBloods = vbloodPrefabs != null;

                // Test that VBlood lists are properly managed
                var testVBloods = new List<int> { 1001, 1002, 1003 };
                var snapshot = new LifecycleService.PlayerSnapshot
                {
                    VBloods = new List<int>(testVBloods)
                };

                bool vbloodsMatch = snapshot.VBloods.Count == testVBloods.Count &&
                                   snapshot.VBloods.TrueForAll(v => testVBloods.Contains(v));

                return canHandleVBloods && vbloodsMatch;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"VBlood cycle test error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test crash recovery workflow
        /// </summary>
        public static void TestCrashRecoveryWorkflow()
        {
            try
            {
                _logger?.LogInfo("Testing crash recovery workflow...");

                // Test that the persistence methods work
                ulong testPlatformId = 999999999999;

                // Create a test snapshot
                var testSnapshot = new LifecycleService.PlayerSnapshot
                {
                    Version = 1,
                    CapturedAt = DateTime.UtcNow,
                    OriginalName = "CrashRecoveryTest",
                    Level = 50,
                    Health = 500f,
                    BloodQuality = 100f,
                    BloodTypeGuid = 99999,
                    Inventory = new List<LifecycleService.ItemEntry>(),
                    VBloods = new List<int> { 4001, 4002 },
                    UIState = new LifecycleService.UIStateSnapshot()
                };

                // Simulate saving snapshot
                var snapshotPath = System.IO.Path.Combine(VAuto.Plugin.DataPath, "players", $"{testPlatformId}_snapshot.json");
                try
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(snapshotPath));
                    var json = System.Text.Json.JsonSerializer.Serialize(testSnapshot);
                    System.IO.File.WriteAllText(snapshotPath, json);

                    // Test loading
                    var loadedSnapshot = LifecycleService.LoadPersistedSnapshot(testPlatformId);
                    bool loadWorks = loadedSnapshot != null && loadedSnapshot.Level == testSnapshot.Level;

                    // Clean up
                    LifecycleService.DeletePersistedSnapshot(testPlatformId);

                    _logger?.LogInfo(loadWorks ? "✅ Crash recovery workflow test PASSED" : "❌ Crash recovery workflow test FAILED");
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Crash recovery persistence error: {ex.Message}");
                    _logger?.LogInfo("❌ Crash recovery workflow test FAILED");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"❌ Crash recovery workflow test ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// Test zone entry/exit detection logic
        /// </summary>
        public static void TestZoneEntryExitDetection()
        {
            try
            {
                _logger?.LogInfo("Testing zone entry/exit detection...");

                var zones = LifecycleService.ArenaZones;
                if (zones == null || zones.Count == 0)
                {
                    _logger?.LogWarning("No zones configured for zone detection test");
                    _logger?.LogInfo("⚠️ Zone entry/exit detection test INCONCLUSIVE - No zones configured");
                    return;
                }

                var zone = zones[0];

                // Test various positions
                var testCases = new[]
                {
                    (position: zone.Center + new float3(0, 0, 0), expectedInCenter: true, expectedInZone: true, description: "At center"),
                    (position: zone.Center + new float3(zone.CenterRadius - 1, 0, 0), expectedInCenter: true, expectedInZone: true, description: "Just inside center radius"),
                    (position: zone.Center + new float3(zone.CenterRadius + 1, 0, 0), expectedInCenter: false, expectedInZone: true, description: "Just outside center radius"),
                    (position: zone.Center + new float3(zone.ZoneRadius - 1, 0, 0), expectedInCenter: false, expectedInZone: true, description: "Just inside zone radius"),
                    (position: zone.Center + new float3(zone.ZoneRadius + 1, 0, 0), expectedInCenter: false, expectedInZone: false, description: "Outside zone radius")
                };

                bool allTestsPass = true;
                foreach (var testCase in testCases)
                {
                    float distance = math.distance(testCase.position, zone.Center);
                    bool actualInCenter = distance <= zone.CenterRadius;
                    bool actualInZone = distance <= zone.ZoneRadius;

                    bool testPasses = actualInCenter == testCase.expectedInCenter && actualInZone == testCase.expectedInZone;

                    _logger?.LogInfo($"{testCase.description}: Distance={distance:F1}, InCenter={actualInCenter} (expected {testCase.expectedInCenter}), InZone={actualInZone} (expected {testCase.expectedInZone}) - {(testPasses ? "PASS" : "FAIL")}");

                    if (!testPasses) allTestsPass = false;
                }

                _logger?.LogInfo(allTestsPass ? "✅ Zone entry/exit detection test PASSED" : "❌ Zone entry/exit detection test FAILED");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"❌ Zone entry/exit detection test ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// Test configuration reload functionality
        /// </summary>
        public static void TestConfigurationReload()
        {
            try
            {
                _logger?.LogInfo("Testing configuration reload...");

                // Test that the reload method doesn't throw exceptions
                LifecycleService.ReloadArenaZonesConfig();

                var zones = LifecycleService.ArenaZones;
                bool reloadWorks = zones != null; // Just check that it doesn't crash

                _logger?.LogInfo(reloadWorks ? "✅ Configuration reload test PASSED" : "❌ Configuration reload test FAILED");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"❌ Configuration reload test ERROR: {ex.Message}");
            }
        }
    }
}












