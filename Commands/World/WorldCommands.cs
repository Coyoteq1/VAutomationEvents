using System;
using System.Linq;
using VampireCommandFramework;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VAuto.Core;
using VAuto.Services.World;
using BepInEx.Logging;

namespace VAuto.Commands.World
{
    /// <summary>
    /// World automation commands - pure world-based, no player references.
    /// Spawn, glow, and automation commands for world objects.
    /// </summary>
    public static class WorldCommands
    {
        #region Spawn Commands
        [Command("worldspawn", "Spawn world objects", "<type> <prefab> [x] [y] [z]")]
        public static void SpawnCommand(ChatCommandContext ctx, string type, string prefab, float x = 0, float y = 0, float z = 0)
        {
            try
            {
                var position = new float3(x, y, z);
                Entity entity;

                switch (type.ToLower())
                {
                    case "tile":
                        entity = WorldSpawnService.SpawnTile(prefab, position);
                        break;
                    case "structure":
                        entity = WorldSpawnService.SpawnStructure(prefab, position);
                        break;
                    case "door":
                        entity = WorldSpawnService.SpawnDoor(prefab, position);
                        break;
                    case "trigger":
                        entity = WorldSpawnService.SpawnTrigger(prefab, position);
                        break;
                    default:
                        ctx.Reply($"Unknown type: {type}. Use: tile, structure, door, trigger");
                        return;
                }

                if (entity != Entity.Null)
                {
                    ctx.Reply($"Spawned {type} '{prefab}' at ({x}, {y}, {z})");
                }
                else
                {
                    ctx.Reply($"Failed to spawn {type} '{prefab}'");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[WorldCommands] Spawn error: {ex.Message}");
                ctx.Reply("Error spawning object");
            }
        }

        [Command("grid", "Spawn objects in grid", "<type> <prefab> <rows> <cols> <centerX> <centerY> <centerZ> <spacing>")]
        public static void GridCommand(ChatCommandContext ctx, string type, string prefab, int rows, int cols, float centerX, float centerY, float centerZ, float spacing)
        {
            try
            {
                var center = new float3(centerX, centerY, centerZ);
                var objectType = ParseObjectType(type);

                var entities = WorldSpawnService.SpawnGrid(prefab, rows, cols, center, spacing, objectType);
                
                if (entities.Any())
                {
                    ctx.Reply($"Spawned {entities.Count} {type} objects in {rows}x{cols} grid");
                }
                else
                {
                    ctx.Reply("Failed to spawn grid");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[WorldCommands] Grid error: {ex.Message}");
                ctx.Reply("Error spawning grid");
            }
        }

        [Command("remove", "Remove world objects", "<type> [radius] [x] [y] [z]")]
        public static void RemoveCommand(ChatCommandContext ctx, string type, float radius = 5f, float x = 0, float y = 0, float z = 0)
        {
            try
            {
                var center = new float3(x, y, z);
                var objectType = ParseObjectType(type);

                var count = WorldSpawnService.RemoveInRadius(center, radius, objectType);
                ctx.Reply($"Removed {count} {type} objects in {radius}m radius");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[WorldCommands] Remove error: {ex.Message}");
                ctx.Reply("Error removing objects");
            }
        }

        [Command("nearest", "Remove nearest object", "<type>")]
        public static void NearestCommand(ChatCommandContext ctx, string type)
        {
            try
            {
                var character = ctx.Event.SenderCharacterEntity;
                var position = VRCore.EntityManager.GetComponentData<Translation>(character).Value;
                var objectType = ParseObjectType(type);

                var success = WorldSpawnService.RemoveNearest(position, objectType);
                ctx.Reply(success ? $"Removed nearest {type}" : $"No {type} found nearby");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[WorldCommands] Nearest error: {ex.Message}");
                ctx.Reply("Error removing nearest object");
            }
        }
        #endregion

        #region Glow Commands
        [Command("glow", "Create glow zones", "<shape> <params>")]
        public static void GlowCommand(ChatCommandContext ctx, string shape, params string[] args)
        {
            try
            {
                switch (shape.ToLower())
                {
                    case "circle":
                        if (args.Length >= 2 && float.TryParse(args[0], out var radius) && float.TryParse(args[1], out var circleSpacing))
                        {
                            var character = ctx.Event.SenderCharacterEntity;
                            var position = VRCore.EntityManager.GetComponentData<Translation>(character).Value;
                            var zoneName = $"circle_{DateTime.Now:yyyyMMdd_HHmmss}";
                            
                            GlowZoneService.BuildCircleZone(zoneName, position, radius, circleSpacing);
                            ctx.Reply($"Created circular glow zone (radius: {radius}, spacing: {circleSpacing})");
                        }
                        else
                        {
                            ctx.Reply("Usage: glow circle <radius> <spacing>");
                        }
                        break;

                    case "box":
                        if (args.Length >= 3 && float.TryParse(args[0], out var width) && float.TryParse(args[1], out var length) && float.TryParse(args[2], out var spacing))
                        {
                            var character = ctx.Event.SenderCharacterEntity;
                            var position = VRCore.EntityManager.GetComponentData<Translation>(character).Value;
                            var zoneName = $"box_{DateTime.Now:yyyyMMdd_HHmmss}";
                            
                            GlowZoneService.BuildBoxZone(zoneName, position, new float2(width, length), spacing);
                            ctx.Reply($"Created box glow zone (width: {width}, length: {length}, spacing: {spacing})");
                        }
                        else
                        {
                            ctx.Reply("Usage: glow box <width> <length> <spacing>");
                        }
                        break;

                    default:
                        ctx.Reply("Unknown shape. Use: circle or box");
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[WorldCommands] Glow error: {ex.Message}");
                ctx.Reply("Error creating glow zone");
            }
        }

        [Command("glowclear", "Clear glow zones", "[zoneName]")]
        public static void GlowClearCommand(ChatCommandContext ctx, string zoneName = "")
        {
            try
            {
                if (string.IsNullOrEmpty(zoneName))
                {
                    GlowZoneService.ClearAll();
                    ctx.Reply("Cleared all glow zones");
                }
                else
                {
                    GlowZoneService.ClearZone(zoneName);
                    ctx.Reply($"Cleared glow zone: {zoneName}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[WorldCommands] GlowClear error: {ex.Message}");
                ctx.Reply("Error clearing glow zone");
            }
        }
        #endregion

        #region Automation Commands
        [Command("auto", "Automation commands", "<action> [params]")]
        public static void AutoCommand(ChatCommandContext ctx, string action, params string[] args)
        {
            try
            {
                switch (action.ToLower())
                {
                    case "trigger":
                        HandleTriggerCommand(ctx, args);
                        break;
                    case "rule":
                        HandleRuleCommand(ctx, args);
                        break;
                    case "clear":
                        HandleAutoClearCommand(ctx, args);
                        break;
                    case "list":
                        HandleAutoListCommand(ctx);
                        break;
                    default:
                        ShowAutoUsage(ctx);
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[WorldCommands] Auto error: {ex.Message}");
                ctx.Reply("Error in auto command");
            }
        }

        private static void HandleTriggerCommand(ChatCommandContext ctx, string[] args)
        {
            if (args.Length < 2)
            {
                ctx.Reply("Usage: auto trigger <add|remove|enable|disable> <triggerName> [params]");
                return;
            }

            var triggerAction = args[0].ToLower();
            var triggerName = args[1];

            switch (triggerAction)
            {
                case "add":
                    if (args.Length >= 3 && float.TryParse(args[2], out var radius))
                    {
                        var character = ctx.Event.SenderCharacterEntity;
                        var position = VRCore.EntityManager.GetComponentData<Translation>(character).Value;
                        var entity = WorldSpawnService.SpawnTrigger("trigger_plate", position);
                        
                        if (entity != Entity.Null)
                        {
                            WorldAutomationService.RegisterTrigger(entity, triggerName, radius);
                            ctx.Reply($"Added trigger '{triggerName}' with {radius}m radius");
                        }
                    }
                    else
                    {
                        ctx.Reply("Usage: auto trigger add <triggerName> <radius>");
                    }
                    break;

                case "remove":
                    var triggers = WorldObjectService.GetByPrefab(triggerName);
                    foreach (var trigger in triggers)
                    {
                        WorldAutomationService.UnregisterTrigger(trigger.Entity);
                        WorldObjectService.Remove(trigger.Entity);
                    }
                    ctx.Reply($"Removed trigger '{triggerName}'");
                    break;

                case "enable":
                case "disable":
                    var triggerObjs = WorldObjectService.GetByPrefab(triggerName);
                    foreach (var triggerObj in triggerObjs)
                    {
                        WorldAutomationService.SetTriggerEnabled(triggerObj.Entity, triggerAction == "enable");
                    }
                    ctx.Reply($"{(triggerAction == "enable" ? "Enabled" : "Disabled")} trigger '{triggerName}'");
                    break;

                default:
                    ctx.Reply("Unknown trigger action. Use: add, remove, enable, disable");
                    break;
            }
        }

        private static void HandleRuleCommand(ChatCommandContext ctx, string[] args)
        {
            if (args.Length < 2)
            {
                ctx.Reply("Usage: auto rule <add|remove> <ruleName> [triggerName]");
                return;
            }

            var ruleAction = args[0].ToLower();
            var ruleName = args[1];

            switch (ruleAction)
            {
                case "add":
                    if (args.Length >= 3)
                    {
                        var triggerName = args[2];
                        var rule = new WorldAutomationService.AutomationRule
                        {
                            TriggerName = triggerName,
                            Actions = new List<WorldAutomationService.AutomationAction>()
                        };

                        // Add a simple open door action as example
                        rule.Actions.Add(new WorldAutomationService.AutomationAction
                        {
                            Type = WorldAutomationService.AutomationActionType.OpenDoor,
                            Target = "door_stone"
                        });

                        WorldAutomationService.AddRule(ruleName, rule);
                        ctx.Reply($"Added rule '{ruleName}' for trigger '{triggerName}'");
                    }
                    else
                    {
                        ctx.Reply("Usage: auto rule add <ruleName> <triggerName>");
                    }
                    break;

                case "remove":
                    WorldAutomationService.RemoveRule(ruleName);
                    ctx.Reply($"Removed rule '{ruleName}'");
                    break;

                default:
                    ctx.Reply("Unknown rule action. Use: add, remove");
                    break;
            }
        }

        private static void HandleAutoClearCommand(ChatCommandContext ctx, string[] args)
        {
            if (args.Length == 0)
            {
                WorldAutomationService.ClearAllRules();
                ctx.Reply("Cleared all automation rules");
            }
            else
            {
                foreach (var ruleName in args)
                {
                    WorldAutomationService.RemoveRule(ruleName);
                }
                ctx.Reply($"Cleared rules: {string.Join(", ", args)}");
            }
        }

        private static void HandleAutoListCommand(ChatCommandContext ctx)
        {
            var rules = WorldAutomationService.GetRules();
            ctx.Reply($"Active automation rules: {rules.Count}");

            foreach (var kvp in rules)
            {
                ctx.Reply($"  {kvp.Key}: {kvp.Value.Actions.Count} actions, trigger: {kvp.Value.TriggerName}");
            }
        }

        private static void ShowAutoUsage(ChatCommandContext ctx)
        {
            ctx.Reply("Automation commands:");
            ctx.Reply("  .auto trigger add <name> <radius>");
            ctx.Reply("  .auto trigger remove <name>");
            ctx.Reply("  .auto trigger enable|disable <name>");
            ctx.Reply("  .auto rule add <name> <trigger>");
            ctx.Reply("  .auto rule remove <name>");
            ctx.Reply("  .auto clear [ruleName...]");
            ctx.Reply("  .auto list");
        }
        #endregion

        #region Info Commands
        [Command("info", "World information", "[type]")]
        public static void InfoCommand(ChatCommandContext ctx, string type = "")
        {
            try
            {
                if (string.IsNullOrEmpty(type))
                {
                    // General world info
                    var totalObjects = WorldObjectService.GetAll().Count();
                    var zones = GlowZoneService.GetActiveZones();
                    
                    ctx.Reply("World Information:");
                    ctx.Reply($"  Total Objects: {totalObjects}");
                    ctx.Reply($"  Active Glow Zones: {zones.Count}");
                    
                    ctx.Reply("Object Types:");
                    foreach (var objType in Enum.GetValues<WorldObjectType>())
                    {
                        var count = WorldObjectService.GetCount(objType);
                        if (count > 0)
                        {
                            ctx.Reply($"    {objType}: {count}");
                        }
                    }
                }
                else
                {
                    // Specific type info
                    var objectType = ParseObjectType(type);
                    var objects = WorldObjectService.GetByType(objectType);
                    
                    var objectList = objects.ToList();
                    ctx.Reply($"{type} Objects: {objectList.Count}");
                    foreach (var obj in objectList.Take(10))
                    {
                        ctx.Reply($"  {obj.PrefabName} at ({obj.Position.x:F1}, {obj.Position.y:F1}, {obj.Position.z:F1})");
                    }
                    
                    if (objectList.Count > 10)
                    {
                        ctx.Reply($"  ... and {objectList.Count - 10} more");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[WorldCommands] Info error: {ex.Message}");
                ctx.Reply("Error getting world information");
            }
        }
        #endregion

        #region Utility Methods
        private static WorldObjectType ParseObjectType(string type)
        {
            return type.ToLower() switch
            {
                "tile" => WorldObjectType.Tile,
                "structure" => WorldObjectType.Structure,
                "door" => WorldObjectType.Door,
                "glow" => WorldObjectType.Glow,
                "trigger" => WorldObjectType.Trigger,
                "automation" => WorldObjectType.Automation,
                _ => WorldObjectType.Structure
            };
        }
        #endregion
    }
}
