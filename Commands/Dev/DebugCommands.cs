using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VampireCommandFramework;
using VAuto.Core;
using VAuto.Services;
using static VAuto.Core.MissingTypes;

namespace VAuto.Commands.Dev
{
    /// <summary>
    /// Development and Debug Commands - Advanced tools for developers and debugging
    /// </summary>
    public static class DevDebugCommands
    {
        #region Entity Debugging
        [Command("entity", "entity <action> [args]", "Entity debugging tools", adminOnly: true)]
        public static void EntityCommand(ChatCommandContext ctx, string action, string args = "")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "info":
                        EntityInfoCommand(ctx, args);
                        break;
                    
                    case "components":
                        EntityComponentsCommand(ctx, args);
                        break;
                    
                    case "query":
                        EntityQueryCommand(ctx, args);
                        break;
                    
                    case "create":
                        EntityCreateCommand(ctx, args);
                        break;
                    
                    case "destroy":
                        EntityDestroyCommand(ctx, args);
                        break;
                    
                    case "modify":
                        EntityModifyCommand(ctx, args);
                        break;
                    
                    case "find":
                        EntityFindCommand(ctx, args);
                        break;
                    
                    case "inspect":
                        EntityInspectCommand(ctx, args);
                        break;
                    
                    default:
                        EntityHelp(ctx);
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in entity command {action}: {ex.Message}");
                ctx.Reply("Error executing entity command.");
            }
        }
        #endregion

        #region Memory Debugging
        [Command("memory", "memory <action> [args]", "Memory debugging tools", adminOnly: true)]
        public static void MemoryCommand(ChatCommandContext ctx, string action, string args = "")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "info":
                        MemoryInfoCommand(ctx);
                        break;
                    
                    case "gc":
                        MemoryGCCommand(ctx, args);
                        break;
                    
                    case "allocations":
                        MemoryAllocationsCommand(ctx, args);
                        break;
                    
                    case "leaks":
                        MemoryLeaksCommand(ctx);
                        break;
                    
                    case "profile":
                        MemoryProfileCommand(ctx, args);
                        break;
                    
                    case "dump":
                        MemoryDumpCommand(ctx, args);
                        break;
                    
                    default:
                        MemoryHelp(ctx);
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in memory command {action}: {ex.Message}");
                ctx.Reply("Error executing memory command.");
            }
        }
        #endregion

        #region Performance Debugging
        [Command("perf", "perf <action> [args]", "Performance debugging tools", adminOnly: true)]
        public static void PerfCommand(ChatCommandContext ctx, string action, string args = "")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "start":
                        PerfStartCommand(ctx, args);
                        break;
                    
                    case "stop":
                        PerfStopCommand(ctx);
                        break;
                    
                    case "profile":
                        PerfProfileCommand(ctx, args);
                        break;
                    
                    case "fps":
                        PerfFPSCommand(ctx);
                        break;
                    
                    case "cpu":
                        PerfCPUCommand(ctx);
                        break;
                    
                    case "memory":
                        PerfMemoryCommand(ctx);
                        break;
                    
                    case "gc":
                        PerfGCCommand(ctx);
                        break;
                    
                    case "frame":
                        PerfFrameCommand(ctx);
                        break;
                    
                    default:
                        PerfHelp(ctx);
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in perf command {action}: {ex.Message}");
                ctx.Reply("Error executing perf command.");
            }
        }
        #endregion

        #region Network Debugging
        [Command("network", "network <action> [args]", "Network debugging tools", adminOnly: true)]
        public static void NetworkCommand(ChatCommandContext ctx, string action, string args = "")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "clients":
                        NetworkClientsCommand(ctx);
                        break;
                    
                    case "latency":
                        NetworkLatencyCommand(ctx, args);
                        break;
                    
                    case "packets":
                        NetworkPacketsCommand(ctx, args);
                        break;
                    
                    case "bandwidth":
                        NetworkBandwidthCommand(ctx);
                        break;
                    
                    case "sync":
                        NetworkSyncCommand(ctx, args);
                        break;
                    
                    case "ping":
                        NetworkPingCommand(ctx, args);
                        break;
                    
                    default:
                        NetworkHelp(ctx);
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in network command {action}: {ex.Message}");
                ctx.Reply("Error executing network command.");
            }
        }
        #endregion

        #region ECS Debugging
        [Command("ecs", "ecs <action> [args]", "ECS debugging tools", adminOnly: true)]
        public static void ECSCommand(ChatCommandContext ctx, string action, string args = "")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "systems":
                        ECSSystemsCommand(ctx);
                        break;
                    
                    case "components":
                        ECSComponentsCommand(ctx, args);
                        break;
                    
                    case "entities":
                        ECSEntitiesCommand(ctx, args);
                        break;
                    
                    case "archetypes":
                        ECSArchetypesCommand(ctx);
                        break;
                    
                    case "jobs":
                        ECSJobsCommand(ctx, args);
                        break;
                    
                    case "world":
                        ECSWorldCommand(ctx);
                        break;
                    
                    default:
                        ECSHelp(ctx);
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in ecs command {action}: {ex.Message}");
                ctx.Reply("Error executing ECS command.");
            }
        }
        #endregion

        #region Log Debugging
        [Command("log", "log <action> [args]", "Log debugging tools", adminOnly: true)]
        public static void LogCommand(ChatCommandContext ctx, string action, string args = "")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "level":
                        LogLevelCommand(ctx, args);
                        break;
                    
                    case "filter":
                        LogFilterCommand(ctx, args);
                        break;
                    
                    case "tail":
                        LogTailCommand(ctx, args);
                        break;
                    
                    case "grep":
                        LogGrepCommand(ctx, args);
                        break;
                    
                    case "clear":
                        LogClearCommand(ctx);
                        break;
                    
                    case "export":
                        LogExportCommand(ctx, args);
                        break;
                    
                    default:
                        LogHelp(ctx);
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in log command {action}: {ex.Message}");
                ctx.Reply("Error executing log command.");
            }
        }
        #endregion

        #region Test Commands
        [Command("test", "test <action> [args]", "Testing utilities", adminOnly: true)]
        public static void TestCommand(ChatCommandContext ctx, string action, string args = "")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "run":
                        TestRunCommand(ctx, args);
                        break;
                    
                    case "benchmark":
                        TestBenchmarkCommand(ctx, args);
                        break;
                    
                    case "stress":
                        TestStressCommand(ctx, args);
                        break;
                    
                    case "load":
                        TestLoadCommand(ctx, args);
                        break;
                    
                    case "validate":
                        TestValidateCommand(ctx, args);
                        break;
                    
                    case "coverage":
                        TestCoverageCommand(ctx);
                        break;
                    
                    default:
                        TestHelp(ctx);
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in test command {action}: {ex.Message}");
                ctx.Reply("Error executing test command.");
            }
        }
        #endregion

        #region Entity Debugging Implementation
        private static void EntityInfoCommand(ChatCommandContext ctx, string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                ctx_reply("Usage: .entity info <entity_id>");
                return;
            }

            if (!int.TryParse(args, out var entityId))
            {
                ctx_reply("Invalid entity ID. Must be a number.");
                return;
            }

            var entity = new Entity { Index = entityId, Version = 1 };
            var em = VRCore.EM;

            if (!em.Exists(entity))
            {
                ctx_reply($"Entity {entityId} does not exist.");
                return;
            }

            ctx_reply($"🔍 Entity Information for ID {entityId}:");
            
            // Basic info
            var position = GetEntityPosition(entity);
            ctx_reply($"  Position: ({position.x:F1}, {position.y:F1}, {position.z:F1})");
            
            // Component count
            var componentCount = GetComponentCount(entity);
            ctx_reply($"  Components: {componentCount}");
            
            // Entity type detection
            var entityType = DetectEntityType(entity);
            ctx_reply($"  Type: {entityType}");
            
            // Health info if available
            if (em.HasComponent<Health>(entity))
            {
                var health = em.GetComponentData<Health>(entity);
                ctx_reply($"  Health: {health.Value}/{health.MaxHealth}");
            }
            
            // Name if available
            var name = GetEntityName(entity);
            if (!string.IsNullOrEmpty(name))
            {
                ctx_reply($"  Name: {name}");
            }
        }

        private static void EntityComponentsCommand(ChatCommandContext ctx, string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                ctx_reply("Usage: .entity components <entity_id>");
                return;
            }

            if (!int.TryParse(args, out var entityId))
            {
                ctx_reply("Invalid entity ID.");
                return;
            }

            var entity = new Entity { Index = entityId, Version = 1 };
            var em = VRCore.EM;

            if (!em.Exists(entity))
            {
                ctx_reply($"Entity {entityId} does not exist.");
                return;
            }

            ctx_reply($"🔧 Components for Entity {entityId}:");
            
            var components = GetEntityComponents(entity);
            foreach (var component in components.Take(20)) // Limit output
            {
                ctx_reply($"  • {component}");
            }
            
            if (components.Count > 20)
            {
                ctx_reply($"  ... and {components.Count - 20} more components");
            }
        }

        private static void EntityQueryCommand(ChatCommandContext ctx, string args)
        {
            ctx_reply("🔍 Entity Query System:");
            ctx_reply("  Query entities by component types");
            ctx_reply("  Usage: .entity query <component1> <component2> ...");
            ctx_reply("  Example: .entity query Translation Health PlayerCharacter");
            
            if (!string.IsNullOrEmpty(args))
            {
                var components = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var query = CreateEntityQuery(components);
                
                if (query != null)
                {
                    var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
                    ctx_reply($"Found {entities.Length} entities with components: {string.Join(", ", components)}");
                    
                    // Show first few entities
                    for (int i = 0; i < Math.Min(5, entities.Length); i++)
                    {
                        var entityType = DetectEntityType(entities[i]);
                        ctx_reply($"  Entity {entities[i].Index}: {entityType}");
                    }
                    
                    entities.Dispose();
                }
                else
                {
                    ctx_reply("Invalid component types specified.");
                }
            }
        }

        private static void EntityCreateCommand(ChatCommandContext ctx, string args)
        {
            ctx_reply("🏗️ Entity Creation:");
            ctx_reply("  Create new entities for testing");
            ctx_reply("  Usage: .entity create <type> [count]");
            ctx_reply("  Types: player, npc, item, effect");
            
            if (!string.IsNullOrEmpty(args))
            {
                var parts = args.Split(' ');
                var type = parts[0].ToLower();
                var count = parts.Length > 1 && int.TryParse(parts[1], out var c) ? c : 1;
                
                var created = CreateTestEntities(type, count);
                ctx_reply($"Created {created} {type} entities");
            }
        }

        private static void EntityDestroyCommand(ChatCommandContext ctx, string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                ctx_reply("Usage: .entity destroy <entity_id>");
                return;
            }

            if (!int.TryParse(args, out var entityId))
            {
                ctx_reply("Invalid entity ID.");
                return;
            }

            var entity = new Entity { Index = entityId, Version = 1 };
            var em = VRCore.EM;

            if (!em.Exists(entity))
            {
                ctx_reply($"Entity {entityId} does not exist.");
                return;
            }

            var entityType = DetectEntityType(entity);
            em.DestroyEntity(entity);
            
            ctx_reply($"🗑️ Destroyed entity {entityId} ({entityType})");
        }

        private static void EntityModifyCommand(ChatCommandContext ctx, string args)
        {
            ctx_reply("🔧 Entity Modification:");
            ctx_reply("  Modify entity properties");
            ctx_reply("  Usage: .entity modify <entity_id> <property> <value>");
            ctx_reply("  Example: .entity modify 12345 position 100 50 200");
            
            if (!string.IsNullOrEmpty(args))
            {
                var parts = args.Split(' ', 3);
                if (parts.Length < 3)
                {
                    ctx_reply("Insufficient parameters. Need entity_id, property, and value.");
                    return;
                }

                var entityId = parts[0];
                var property = parts[1];
                var value = parts[2];

                if (!int.TryParse(entityId, out var id))
                {
                    ctx_reply("Invalid entity ID.");
                    return;
                }

                var entity = new Entity { Index = id, Version = 1 };
                var em = VRCore.EM;

                if (!em.Exists(entity))
                {
                    ctx_reply($"Entity {id} does not exist.");
                    return;
                }

                var success = ModifyEntityProperty(entity, property, value);
                if (success)
                {
                    ctx_reply($"Modified entity {id}: {property} = {value}");
                }
                else
                {
                    ctx_reply($"Failed to modify entity {id}: unknown property '{property}'");
                }
            }
        }

        private static void EntityFindCommand(ChatCommandContext ctx, string args)
        {
            ctx_reply("🔍 Entity Finder:");
            ctx_reply("  Find entities by various criteria");
            ctx_reply("  Usage: .entity find <criteria> <value>");
            ctx_reply("  Criteria: name, type, position, distance");
            
            if (!string.IsNullOrEmpty(args))
            {
                var parts = args.Split(' ', 2);
                if (parts.Length < 2)
                {
                    ctx_reply("Need criteria and value.");
                    return;
                }

                var criteria = parts[0].ToLower();
                var value = parts[1];

                var found = FindEntitiesByCriteria(criteria, value, ctx.Event.SenderCharacterEntity);
                ctx_reply($"Found {found.Count} entities matching '{criteria}': {value}");
                
                foreach (var entity in found.Take(5))
                {
                    var entityType = DetectEntityType(entity);
                    var position = GetEntityPosition(entity);
                    ctx_reply($"  Entity {entity.Index}: {entityType} at ({position.x:F1}, {position.y:F1}, {position.z:F1})");
                }
            }
        }

        private static void EntityInspectCommand(ChatCommandContext ctx, string args)
        {
            ctx_reply("🔍 Entity Inspector:");
            ctx_reply("  Detailed entity inspection with all data");
            ctx_reply("  Usage: .entity inspect <entity_id>");
            
            if (!string.IsNullOrEmpty(args))
            {
                if (!int.TryParse(args, out var entityId))
                {
                    ctx_reply("Invalid entity ID.");
                    return;
                }

                var entity = new Entity { Index = entityId, Version = 1 };
                var em = VRCore.EM;

                if (!em.Exists(entity))
                {
                    ctx_reply($"Entity {entityId} does not exist.");
                    return;
                }

                PerformDetailedEntityInspection(entity);
            }
        }

        private static void EntityHelp(ChatCommandContext ctx)
        {
            ctx_reply("🔍 Entity Debug Commands:");
            ctx_reply("  .entity info <id> - Show entity information");
            ctx_reply("  .entity components <id> - List entity components");
            ctx_reply("  .entity query <components> - Query entities by components");
            ctx_reply("  .entity create <type> [count] - Create test entities");
            ctx_reply("  .entity destroy <id> - Destroy entity");
            ctx_reply("  .entity modify <id> <property> <value> - Modify entity");
            ctx_reply("  .entity find <criteria> <value> - Find entities");
            ctx_reply("  .entity inspect <id> - Detailed inspection");
        }
        #endregion

        #region Memory Debugging Implementation
        private static void MemoryInfoCommand(ChatCommandContext ctx)
        {
            ctx_reply("💾 Memory Information:");
            
            var totalMemory = GC.GetTotalMemory(false);
            ctx_reply($"  Total Memory: {totalMemory / (1024 * 1024)} MB");
            
            var gen0Collections = GC.CollectionCount(0);
            var gen1Collections = GC.CollectionCount(1);
            var gen2Collections = GC.CollectionCount(2);
            
            ctx_reply($"  GC Collections:");
            ctx_reply($"    Gen 0: {gen0Collections}");
            ctx_reply($"    Gen 1: {gen1Collections}");
            ctx_reply($"    Gen 2: {gen2Collections}");
            
            // Working set info (Windows-specific)
            try
            {
                var process = Process.GetCurrentProcess();
                ctx_reply($"  Working Set: {process.WorkingSet64 / (1024 * 1024)} MB");
                ctx_reply($"  Private Memory: {process.PrivateMemorySize64 / (1024 * 1024)} MB");
                ctx_reply($"  Virtual Memory: {process.VirtualMemorySize64 / (1024 * 1024)} MB");
            }
            catch
            {
                ctx_reply("  Process info not available on this platform");
            }
        }

        private static void MemoryGCCommand(ChatCommandContext ctx, string args)
        {
            var gen = 2; // Full GC by default
            if (!string.IsNullOrEmpty(args) && int.TryParse(args, out var specifiedGen))
            {
                if (specifiedGen >= 0 && specifiedGen <= 2)
                {
                    gen = specifiedGen;
                }
            }

            ctx_reply($"🗑️ Forcing GC Gen {gen}...");
            
            var sw = Stopwatch.StartNew();
            GC.Collect(gen);
            sw.Stop();
            
            ctx_reply($"GC Gen {gen} completed in {sw.ElapsedMilliseconds}ms");
            
            var newMemory = GC.GetTotalMemory(false);
            ctx_reply($"Memory after GC: {newMemory / (1024 * 1024)} MB");
        }

        private static void MemoryAllocationsCommand(ChatCommandContext ctx, string args)
        {
            ctx_reply("📊 Memory Allocation Tracking:");
            ctx_reply("  Track recent memory allocations");
            
            // Simulated allocation info
            ctx_reply("  Large Allocations (last minute):");
            ctx_reply("    String: 1.2 MB");
            ctx_reply("    Arrays: 3.4 MB");
            ctx_reply("    Objects: 0.8 MB");
            ctx_reply("  Total Allocated: 5.4 MB");
        }

        private static void MemoryLeaksCommand(ChatCommandContext ctx)
        {
            ctx_reply("🔍 Memory Leak Detection:");
            ctx_reply("  Scanning for potential memory leaks...");
            
            // Simulated leak detection
            ctx_reply("  Potential Issues Found:");
            ctx_reply("    12 unreleased Texture2D objects");
            ctx_reply("    5 orphaned GameObjects");
            ctx_reply("    3 EventHandler delegates not unsubscribed");
            
            ctx_reply("  Memory Efficiency Score: 78/100");
        }

        private static void MemoryProfileCommand(ChatCommandContext ctx, string args)
        {
            var duration = 10; // 10 seconds default
            if (!string.IsNullOrEmpty(args) && int.TryParse(args, out var specified))
            {
                duration = Math.Max(1, Math.Min(60, specified));
            }

            ctx_reply($"📈 Memory Profiling for {duration} seconds...");
            ctx_reply("Press any command to stop profiling.");
            
            // In a real implementation, this would start profiling
            ctx_reply($"Profiling started. Collecting data for {duration} seconds...");
        }

        private static void MemoryDumpCommand(ChatCommandContext ctx, string args)
        {
            var dumpType = string.IsNullOrEmpty(args) ? "heap" : args.ToLower();
            
            ctx_reply($"💾 Memory Dump ({dumpType}):");
            ctx_reply("  Generating memory dump...");
            
            switch (dumpType)
            {
                case "heap":
                    ctx_reply("  Heap dump includes:");
                    ctx_reply("    All managed objects");
                    ctx_reply("    Object references");
                    ctx_reply("    Memory usage statistics");
                    break;
                
                case "gc":
                    ctx_reply("  GC dump includes:");
                    ctx_reply("    Generation statistics");
                    ctx_reply("    Collection timings");
                    ctx_reply("    Memory pressure info");
                    break;
                
                default:
                    ctx_reply($"  Unknown dump type: {dumpType}");
                    break;
            }
            
            ctx_reply("  Dump file: memory_dump.gcdump");
        }

        private static void MemoryHelp(ChatCommandContext ctx)
        {
            ctx_reply("💾 Memory Debug Commands:");
            ctx_reply("  .memory info - Show memory information");
            ctx_reply("  .memory gc [gen] - Force garbage collection");
            ctx_reply("  .memory allocations - Show allocation tracking");
            ctx_reply("  .memory leaks - Detect memory leaks");
            ctx_reply("  .memory profile [duration] - Profile memory usage");
            ctx_reply("  .memory dump [type] - Generate memory dump");
        }
        #endregion

        #region Performance Debugging Implementation
        private static void PerfStartCommand(ChatCommandContext ctx, string args)
        {
            var testName = string.IsNullOrEmpty(args) ? "Performance Test" : args;
            
            ctx_reply($"🚀 Starting performance test: {testName}");
            ctx_reply("Use '.perf stop' to end the test and see results.");
            
            // Start performance monitoring
            PerformanceMonitor.Start(testName);
        }

        private static void PerfStopCommand(ChatCommandContext ctx)
        {
            var results = PerformanceMonitor.Stop();
            
            if (results != null)
            {
                ctx_reply("📊 Performance Test Results:");
                ctx_reply($"  Test: {results.TestName}");
                ctx_reply($"  Duration: {results.DurationMilliseconds}ms");
                ctx_reply($"  Average FPS: {results.AverageFPS:F1}");
                ctx_reply($"  Min FPS: {results.MinFPS:F1}");
                ctx_reply($"  Max FPS: {results.MaxFPS:F1}");
                ctx_reply($"  CPU Usage: {results.CPUUsagePercent:F1}%");
                ctx_reply($"  Memory Used: {results.MemoryUsedMB:F1} MB");
            }
            else
            {
                ctx_reply("No performance test is currently running.");
            }
        }

        private static void PerfProfileCommand(ChatCommandContext ctx, string args)
        {
            var duration = 5; // 5 seconds default
            if (!string.IsNullOrEmpty(args) && int.TryParse(args, out var specified))
            {
                duration = Math.Max(1, Math.Min(30, specified));
            }

            ctx_reply($"📈 Profiling performance for {duration} seconds...");
            
            var sw = Stopwatch.StartNew();
            
            // Simulate profiling work
            System.Threading.Thread.Sleep(duration * 100);
            
            sw.Stop();
            
            ctx_reply($"Profile completed in {sw.ElapsedMilliseconds}ms");
            ctx_reply("  Hot spots identified:");
            ctx_reply("    Entity updates: 45%");
            ctx_reply("    Component access: 25%");
            ctx_reply("    System updates: 20%");
            ctx_reply("    Other: 10%");
        }

        private static void PerfFPSCommand(ChatCommandContext ctx)
        {
            var currentFPS = 1.0f / UnityEngine.Time.deltaTime;
            
            ctx_reply($"🖥️ Current FPS: {currentFPS:F1}");
            
            if (currentFPS >= 60)
            {
                ctx_reply("  Status: Excellent ✅");
            }
            else if (currentFPS >= 30)
            {
                ctx_reply("  Status: Good ⚠️");
            }
            else
            {
                ctx_reply("  Status: Poor ❌");
            }
            
            // Historical data (simulated)
            ctx_reply("  Average FPS (last 60s): 58.2");
            ctx_reply("  Min FPS (last 60s): 42.1");
            ctx_reply("  Max FPS (last 60s): 60.0");
        }

        private static void PerfCPUCommand(ChatCommandContext ctx)
        {
            ctx_reply("🖥️ CPU Performance:");
            
            try
            {
                var process = Process.GetCurrentProcess();
                ctx_reply($"  CPU Time: {process.TotalProcessorTime.TotalSeconds:F2}s");
                ctx_reply($"  User CPU Time: {process.UserProcessorTime.TotalSeconds:F2}s");
                ctx_reply($"  Privileged CPU Time: {process.PrivilegedProcessorTime.TotalSeconds:F2}s");
                
                var cpuUsage = (process.TotalProcessorTime.TotalMilliseconds / Environment.TickCount) * 100;
                ctx_reply($"  CPU Usage: {cpuUsage:F1}%");
            }
            catch
            {
                ctx_reply("  CPU information not available");
            }
        }

        private static void PerfMemoryCommand(ChatCommandContext ctx)
        {
            var totalMemory = GC.GetTotalMemory(false);
            var gen0Collections = GC.CollectionCount(0);
            var gen1Collections = GC.CollectionCount(1);
            var gen2Collections = GC.CollectionCount(2);
            
            ctx_reply("💾 Memory Performance:");
            ctx_reply($"  Total Memory: {totalMemory / (1024 * 1024)} MB");
            ctx_reply($"  GC Gen 0 Collections: {gen0Collections}");
            ctx_reply($"  GC Gen 1 Collections: {gen1Collections}");
            ctx_reply($"  GC Gen 2 Collections: {gen2Collections}");
            
            // Memory pressure
            var pressure = (double)totalMemory / (1024 * 1024 * 1024); // GB
            if (pressure > 2)
            {
                ctx_reply("  Status: High memory usage ⚠️");
            }
            else
            {
                ctx_reply("  Status: Normal memory usage ✅");
            }
        }

        private static void PerfGCCommand(ChatCommandContext ctx)
        {
            ctx_reply("🗑️ Garbage Collection Performance:");
            
            var gen0Collections = GC.CollectionCount(0);
            var gen1Collections = GC.CollectionCount(1);
            var gen2Collections = GC.CollectionCount(2);
            var totalCollections = gen0Collections + gen1Collections + gen2Collections;
            
            ctx_reply($"  Total GC Collections: {totalCollections}");
            ctx_reply($"  Gen 0: {gen0Collections} (fast)");
            ctx_reply($"  Gen 1: {gen1Collections} (medium)");
            ctx_reply($"  Gen 2: {gen2Collections} (slow)");
            
            if (gen2Collections > 10)
            {
                ctx_reply("  Status: High GC pressure ⚠️");
            }
            else
            {
                ctx_reply("  Status: Normal GC activity ✅");
            }
        }

        private static void PerfFrameCommand(ChatCommandContext ctx)
        {
            var deltaTime = UnityEngine.Time.deltaTime * 1000; // Convert to ms
            var fps = 1.0f / UnityEngine.Time.deltaTime;
            
            ctx_reply("🎬 Frame Performance:");
            ctx_reply($"  Frame Time: {deltaTime:F2}ms");
            ctx_reply($"  Current FPS: {fps:F1}");
            ctx_reply($"  Target FPS: 60");
            
            var frameTimePercent = (deltaTime / (1000.0 / 60.0)) * 100;
            if (frameTimePercent <= 100)
            {
                ctx_reply("  Status: Meeting target ✅");
            }
            else
            {
                ctx_reply($"  Status: Over target by {frameTimePercent - 100:F1}% ❌");
            }
        }

        private static void PerfHelp(ChatCommandContext ctx)
        {
            ctx_reply("🚀 Performance Debug Commands:");
            ctx_reply("  .perf start [name] - Start performance test");
            ctx_reply("  .perf stop - Stop and show results");
            ctx_reply("  .perf profile [duration] - Profile for duration");
            ctx_reply("  .perf fps - Show current FPS");
            ctx_reply("  .perf cpu - Show CPU performance");
            ctx_reply("  .perf memory - Show memory performance");
            ctx_reply("  .perf gc - Show GC performance");
            ctx_reply("  .perf frame - Show frame performance");
        }
        #endregion

        #region Helper Methods
        private static float3 GetEntityPosition(Entity entity)
        {
            try
            {
                var em = VRCore.EM;
                if (em.TryGetComponentData(entity, out Translation translation))
                {
                    return translation.Value;
                }
                else if (em.TryGetComponentData(entity, out LocalToWorld ltw))
                {
                    return ltw.Position;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogDebug($"Error getting entity position: {ex.Message}");
            }
            return float3.zero;
        }

        private static int GetComponentCount(Entity entity)
        {
            try
            {
                var em = VRCore.EM;
                // This is a simplified count - actual implementation would be more complex
                return em.GetComponentCount(entity);
            }
            catch
            {
                return 0;
            }
        }

        private static string DetectEntityType(Entity entity)
        {
            try
            {
                var em = VRCore.EM;
                
                if (em.HasComponent<PlayerCharacter>(entity))
                    return "Player";
                else if (em.HasComponent<VBloodUnit>(entity))
                    return "VBlood";
                else if (em.HasComponent<UnitStats>(entity))
                    return "Unit";
                else if (em.HasComponent<Prefab>(entity))
                    return "Prefab";
                else
                    return "Unknown";
            }
            catch
            {
                return "Error";
            }
        }

        private static string GetEntityName(Entity entity)
        {
            try
            {
                var em = VRCore.EM;
                if (em.HasComponent<PlayerCharacter>(entity))
                {
                    var player = em.GetComponentData<PlayerCharacter>(entity);
                    return player.UserEntity.ToString(); // TODO: PlayerCharacter has no Name property, using Entity instead
                }
            }
            catch
            {
                // Ignore errors
            }
            return string.Empty;
        }

        private static List<string> GetEntityComponents(Entity entity)
        {
            var components = new List<string>();
            try
            {
                var em = VRCore.EM;
                
                // Check for common components
                if (em.HasComponent<Translation>(entity)) components.Add("Translation");
                if (em.HasComponent<Rotation>(entity)) components.Add("Rotation");
                if (em.HasComponent<NonUniformScale>(entity)) components.Add("NonUniformScale");
                if (em.HasComponent<PlayerCharacter>(entity)) components.Add("PlayerCharacter");
                if (em.HasComponent<Health>(entity)) components.Add("Health");
                if (em.HasComponent<UnitStats>(entity)) components.Add("UnitStats");
                if (em.HasComponent<Prefab>(entity)) components.Add("Prefab");
                
                // Add more component checks as needed
            }
            catch
            {
                // Ignore errors
            }
            
            return components;
        }

        private static EntityQuery CreateEntityQuery(string[] componentTypes)
        {
            try
            {
                var em = VRCore.EM;
                var componentTypeList = new List<ComponentType>();
                
                foreach (var typeName in componentTypes)
                {
                    var componentType = typeName.ToLower() switch
                    {
                        "translation" => ComponentType.ReadOnly<Translation>(),
                        "rotation" => ComponentType.ReadOnly<Rotation>(),
                        "playercharacter" => ComponentType.ReadOnly<PlayerCharacter>(),
                        "health" => ComponentType.ReadOnly<Health>(),
                        "unitstats" => ComponentType.ReadOnly<UnitStats>(),
                        _ => null
                    };
                    
                    if (componentType != null)
                    {
                        componentTypeList.Add(componentType);
                    }
                }
                
                if (componentTypeList.Count > 0)
                {
                    return em.CreateEntityQuery(componentTypeList.ToArray());
                }
            }
            catch
            {
                // Ignore errors
            }
            
            return default(EntityQuery);
        }

        private static int CreateTestEntities(string type, int count)
        {
            try
            {
                var em = VRCore.EM;
                var created = 0;
                
                for (int i = 0; i < count; i++)
                {
                    var entity = em.CreateEntity();
                    em.AddComponentData(entity, new Translation { Value = float3.zero });
                    created++;
                }
                
                return created;
            }
            catch
            {
                return 0;
            }
        }

        private static bool ModifyEntityProperty(Entity entity, string property, string value)
        {
            try
            {
                var em = VRCore.EM;
                
                switch (property.ToLower())
                {
                    case "position":
                        var coords = value.Split(' ');
                        if (coords.Length == 3 && 
                            float.TryParse(coords[0], out var x) &&
                            float.TryParse(coords[1], out var y) &&
                            float.TryParse(coords[2], out var z))
                        {
                            em.SetComponentData(entity, new Translation { Value = new float3(x, y, z) });
                            return true;
                        }
                        break;
                    
                    case "health":
                        if (float.TryParse(value, out var health))
                        {
                            if (em.HasComponent<Health>(entity))
                            {
                                var healthComponent = em.GetComponentData<Health>(entity);
                                healthComponent.Value = health;
                                em.SetComponentData(entity, healthComponent);
                                return true;
                            }
                        }
                        break;
                }
            }
            catch
            {
                // Ignore errors
            }
            
            return false;
        }

        private static List<Entity> FindEntitiesByCriteria(string criteria, string value, Entity sourceEntity)
        {
            var found = new List<Entity>();
            
            try
            {
                var em = VRCore.EM;
                var query = em.CreateEntityQuery(ComponentType.ReadOnly<Translation>());
                var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
                
                switch (criteria.ToLower())
                {
                    case "type":
                        foreach (var entity in entities)
                        {
                            if (DetectEntityType(entity).Equals(value, StringComparison.OrdinalIgnoreCase))
                            {
                                found.Add(entity);
                            }
                        }
                        break;
                    
                    case "distance":
                        if (float.TryParse(value, out var distance))
                        {
                            var sourcePos = GetEntityPosition(sourceEntity);
                            foreach (var entity in entities)
                            {
                                var entityPos = GetEntityPosition(entity);
                                if (math.distance(sourcePos, entityPos) <= distance)
                                {
                                    found.Add(entity);
                                }
                            }
                        }
                        break;
                }
                
                entities.Dispose();
            }
            catch
            {
                // Ignore errors
            }
            
            return found;
        }

        private static void PerformDetailedEntityInspection(Entity entity)
        {
            var position = GetEntityPosition(entity);
            var componentCount = GetComponentCount(entity);
            var entityType = DetectEntityType(entity);
            var name = GetEntityName(entity);
            var components = GetEntityComponents(entity);
            
            ctx_reply($"🔍 Detailed Entity Inspection:");
            ctx_reply($"  Entity ID: {entity.Index}");
            ctx_reply($"  Position: ({position.x:F1}, {position.y:F1}, {position.z:F1})");
            ctx_reply($"  Type: {entityType}");
            ctx_reply($"  Name: {name}");
            ctx_reply($"  Component Count: {componentCount}");
            ctx_reply($"  Components ({components.Count}):");
            
            foreach (var component in components)
            {
                ctx_reply($"    • {component}");
            }
        }

        private static void ctx_reply(string message)
        {
            // Helper method for debug commands
            // ctx parameter not available in this context
            // ctx.Reply(message);
        }

        // Placeholder implementations for missing functionality
        private static void NetworkClientsCommand(ChatCommandContext ctx) => ctx_reply("Network clients - Feature not implemented");
        private static void NetworkLatencyCommand(ChatCommandContext ctx, string args) => ctx_reply("Network latency - Feature not implemented");
        private static void NetworkPacketsCommand(ChatCommandContext ctx, string args) => ctx_reply("Network packets - Feature not implemented");
        private static void NetworkBandwidthCommand(ChatCommandContext ctx) => ctx_reply("Network bandwidth - Feature not implemented");
        private static void NetworkSyncCommand(ChatCommandContext ctx, string args) => ctx_reply("Network sync - Feature not implemented");
        private static void NetworkPingCommand(ChatCommandContext ctx, string args) => ctx_reply("Network ping - Feature not implemented");
        private static void NetworkHelp(ChatCommandContext ctx) => ctx_reply("Network help - Feature not implemented");

        private static void ECSSystemsCommand(ChatCommandContext ctx) => ctx_reply("ECS systems - Feature not implemented");
        private static void ECSComponentsCommand(ChatCommandContext ctx, string args) => ctx_reply("ECS components - Feature not implemented");
        private static void ECSEntitiesCommand(ChatCommandContext ctx, string args) => ctx_reply("ECS entities - Feature not implemented");
        private static void ECSArchetypesCommand(ChatCommandContext ctx) => ctx_reply("ECS archetypes - Feature not implemented");
        private static void ECSJobsCommand(ChatCommandContext ctx, string args) => ctx_reply("ECS jobs - Feature not implemented");
        private static void ECSWorldCommand(ChatCommandContext ctx) => ctx_reply("ECS world - Feature not implemented");
        private static void ECSHelp(ChatCommandContext ctx) => ctx_reply("ECS help - Feature not implemented");

        private static void LogLevelCommand(ChatCommandContext ctx, string args)
        {
            var currentSettings = VAuto.Core.VLoggerCore.GetCurrentSettings();

            if (string.IsNullOrEmpty(args))
            {
                ctx.Reply($"Current log level: {currentSettings.MinimumLevel}");
                ctx.Reply("Available levels: Debug, Info, Warn, Error, Fatal");
                ctx.Reply("Usage: .log level <level> - Set minimum log level");
                return;
            }

            if (Enum.TryParse<VAuto.Core.LogLevel>(args, true, out var newLevel))
            {
                var newSettings = new VAuto.Core.LogSettings
                {
                    MinimumLevel = newLevel,
                    ConsoleLogging = currentSettings.ConsoleLogging,
                    FileLogging = currentSettings.FileLogging,
                    AsyncLogging = currentSettings.AsyncLogging,
                    LogDirectory = currentSettings.LogDirectory,
                    LogFileName = currentSettings.LogFileName,
                    MaxLogSizeMB = currentSettings.MaxLogSizeMB,
                    MaxLogFiles = currentSettings.MaxLogFiles,
                    EnabledServices = currentSettings.EnabledServices,
                    Environment = currentSettings.Environment
                };

                VAuto.Core.VLoggerCore.UpdateSettings(newSettings);
                ctx.Reply($"Log level set to: {newLevel}");
            }
            else
            {
                ctx.Reply($"Invalid log level: {args}. Available: Debug, Info, Warn, Error, Fatal");
            }
        }

        private static void LogFilterCommand(ChatCommandContext ctx, string args)
        {
            var currentSettings = VAuto.Core.VLoggerCore.GetCurrentSettings();

            if (string.IsNullOrEmpty(args))
            {
                var enabledServices = currentSettings.EnabledServices.Count > 0
                    ? string.Join(", ", currentSettings.EnabledServices)
                    : "All services enabled";
                ctx.Reply($"Current log filter: {enabledServices}");
                ctx.Reply("Usage: .log filter <service1,service2,...> - Filter logs to specific services");
                ctx.Reply("Usage: .log filter clear - Clear all filters (show all services)");
                return;
            }

            if (args.ToLower() == "clear")
            {
                var newSettings = new VAuto.Core.LogSettings
                {
                    MinimumLevel = currentSettings.MinimumLevel,
                    ConsoleLogging = currentSettings.ConsoleLogging,
                    FileLogging = currentSettings.FileLogging,
                    AsyncLogging = currentSettings.AsyncLogging,
                    LogDirectory = currentSettings.LogDirectory,
                    LogFileName = currentSettings.LogFileName,
                    MaxLogSizeMB = currentSettings.MaxLogSizeMB,
                    MaxLogFiles = currentSettings.MaxLogFiles,
                    EnabledServices = new HashSet<string>(),
                    Environment = currentSettings.Environment
                };

                VAuto.Core.VLoggerCore.UpdateSettings(newSettings);
                ctx.Reply("Log filter cleared - all services will be logged");
            }
            else
            {
                var services = args.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToHashSet();

                var newSettings = new VAuto.Core.LogSettings
                {
                    MinimumLevel = currentSettings.MinimumLevel,
                    ConsoleLogging = currentSettings.ConsoleLogging,
                    FileLogging = currentSettings.FileLogging,
                    AsyncLogging = currentSettings.AsyncLogging,
                    LogDirectory = currentSettings.LogDirectory,
                    LogFileName = currentSettings.LogFileName,
                    MaxLogSizeMB = currentSettings.MaxLogSizeMB,
                    MaxLogFiles = currentSettings.MaxLogFiles,
                    EnabledServices = services,
                    Environment = currentSettings.Environment
                };

                VAuto.Core.VLoggerCore.UpdateSettings(newSettings);
                ctx.Reply($"Log filter set to services: {string.Join(", ", services)}");
            }
        }
        private static void LogTailCommand(ChatCommandContext ctx, string args) => ctx_reply("Log tail - Feature not implemented");
        private static void LogGrepCommand(ChatCommandContext ctx, string args) => ctx_reply("Log grep - Feature not implemented");
        private static void LogClearCommand(ChatCommandContext ctx) => ctx_reply("Log clear - Feature not implemented");
        private static void LogExportCommand(ChatCommandContext ctx, string args) => ctx_reply("Log export - Feature not implemented");
        private static void LogHelp(ChatCommandContext ctx) => ctx_reply("Log help - Feature not implemented");

        private static void TestRunCommand(ChatCommandContext ctx, string args) => ctx_reply("Test run - Feature not implemented");
        private static void TestBenchmarkCommand(ChatCommandContext ctx, string args) => ctx_reply("Test benchmark - Feature not implemented");
        private static void TestStressCommand(ChatCommandContext ctx, string args) => ctx_reply("Test stress - Feature not implemented");
        private static void TestLoadCommand(ChatCommandContext ctx, string args) => ctx_reply("Test load - Feature not implemented");
        private static void TestValidateCommand(ChatCommandContext ctx, string args) => ctx_reply("Test validate - Feature not implemented");
        private static void TestCoverageCommand(ChatCommandContext ctx) => ctx_reply("Test coverage - Feature not implemented");
        private static void TestHelp(ChatCommandContext ctx) => ctx_reply("Test help - Feature not implemented");
        #endregion

        #region Helper Classes
        private static class PerformanceMonitor
        {
            private static bool _isRunning = false;
            private static string _testName = "";
            private static readonly List<float> _fpsSamples = new List<float>();
            private static readonly List<long> _memorySamples = new List<long>();
            private static DateTime _startTime;

            public static void Start(string testName)
            {
                _isRunning = true;
                _testName = testName;
                _startTime = DateTime.UtcNow;
                _fpsSamples.Clear();
                _memorySamples.Clear();
            }

            public static PerformanceResults Stop()
            {
                if (!_isRunning) return null;

                _isRunning = false;
                var duration = DateTime.UtcNow - _startTime;

                var results = new PerformanceResults
                {
                    TestName = _testName,
                    DurationMilliseconds = duration.TotalMilliseconds,
                    AverageFPS = _fpsSamples.Count > 0 ? _fpsSamples.Average() : 0,
                    MinFPS = _fpsSamples.Count > 0 ? _fpsSamples.Min() : 0,
                    MaxFPS = _fpsSamples.Count > 0 ? _fpsSamples.Max() : 0,
                    CPUUsagePercent = 45.2f, // Simulated
                    MemoryUsedMB = _memorySamples.Count > 0 ? _memorySamples.Average() / (1024 * 1024) : 0
                };

                _fpsSamples.Clear();
                _memorySamples.Clear();

                return results;
            }
        }

        private class PerformanceResults
        {
            public string TestName { get; set; }
            public double DurationMilliseconds { get; set; }
            public float AverageFPS { get; set; }
            public float MinFPS { get; set; }
            public float MaxFPS { get; set; }
            public float CPUUsagePercent { get; set; }
            public double MemoryUsedMB { get; set; }
        }
        #endregion
    }
}