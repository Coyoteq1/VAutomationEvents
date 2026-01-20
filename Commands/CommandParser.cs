using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using VAuto.Automation;
using VAuto.Services.Systems;

namespace VAuto.Commands
{
    /// <summary>
    /// Comprehensive command parser for the Vauto automation system
    /// Handles all command types: arena, automation, zone, schematic, castle, and logistics
    /// </summary>
    /* TEMPORARILY DISABLED DUE TO UNRESOLVED TYPE REFERENCES
    public class CommandParser
    {
        private readonly ZoneManagerService _zoneManager;
        private readonly AutomationExecutionEngine _executionEngine;
        private readonly ChatGrammarService _grammarService;

        public CommandParser(ZoneManagerService zoneManager, AutomationExecutionEngine executionEngine, ChatGrammarService grammarService)
        {
            _zoneManager = zoneManager ?? throw new ArgumentNullException(nameof(zoneManager));
            _executionEngine = executionEngine ?? throw new ArgumentNullException(nameof(executionEngine));
            _grammarService = grammarService ?? throw new ArgumentNullException(nameof(grammarService));
        }

        /// <summary>
        /// Parse and execute a command
        /// </summary>
        public CommandResult ParseAndExecute(string commandText, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult
            {
                OriginalCommand = commandText,
                Success = false,
                CharacterId = characterId,
                IsAdmin = isAdmin
            };

            try
            {
                // Normalize command
                var normalizedCommand = NormalizeCommand(commandText);
                result.NormalizedCommand = normalizedCommand;

                // Parse command type
                var commandType = DetectCommandType(normalizedCommand);
                result.CommandType = commandType;

                // Route to appropriate parser
                switch (commandType)
                {
                    case CommandType.Arena:
                        result = ParseArenaCommand(normalizedCommand, characterId, isAdmin);
                        break;
                    case CommandType.Automation:
                        result = ParseAutomationCommand(normalizedCommand, characterId, isAdmin);
                        break;
                    case CommandType.Zone:
                        result = ParseZoneCommand(normalizedCommand, characterId, isAdmin);
                        break;
                    case CommandType.Schematic:
                        result = ParseSchematicCommand(normalizedCommand, characterId, isAdmin);
                        break;
                    case CommandType.Castle:
                        result = ParseCastleCommand(normalizedCommand, characterId, isAdmin);
                        break;
                    case CommandType.Logistics:
                        result = ParseLogisticsCommand(normalizedCommand, characterId, isAdmin);
                        break;
                    case CommandType.Unknown:
                        result.Error = "Unknown command type";
                        result.Suggestions = GetCommandSuggestions(normalizedCommand);
                        break;
                }
            }
            catch (Exception ex)
            {
                result.Error = $"Command parsing failed: {ex.Message}";
                result.Success = false;
            }

            return result;
        }

        #region Command Type Detection

        private CommandType DetectCommandType(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return CommandType.Unknown;

            var firstWord = command.Split(' ')[0].ToLower();

            switch (firstWord)
            {
                case "arena":
                    return CommandType.Arena;
                case "testplan":
                case "runplan":
                case "scheduleplan":
                case "automation":
                    return CommandType.Automation;
                case "planzone":
                    return CommandType.Zone;
                case "schematic":
                    return CommandType.Schematic;
                case "castle":
                    return CommandType.Castle;
                case "logistics":
                    return CommandType.Logistics;
                default:
                    return CommandType.Unknown;
            }
        }

        #endregion

        #region Arena Command Parsing

        private CommandResult ParseArenaCommand(string command, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult
            {
                CommandType = CommandType.Arena,
                OriginalCommand = command,
                CharacterId = characterId,
                IsAdmin = isAdmin
            };

            var match = Regex.Match(command, @"^arena\s+(\w+)(?:\s+(.+))?$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                result.Error = "Invalid arena command format";
                result.Suggestions = new List<string> { "arena enter", "arena exit", "arena status", "arena reset", "arena setzone <id>" };
                return result;
            }

            var action = match.Groups[1].Value.ToLower();
            var args = match.Groups[2].Value;

            try
            {
                switch (action)
                {
                    case "enter":
                        result = HandleArenaEnter(args, characterId, isAdmin);
                        break;
                    case "exit":
                        result = HandleArenaExit(args, characterId, isAdmin);
                        break;
                    case "status":
                        result = HandleArenaStatus(args, characterId, isAdmin);
                        break;
                    case "reset":
                        result = HandleArenaReset(args, characterId, isAdmin);
                        break;
                    case "setzone":
                        result = HandleArenaSetZone(args, characterId, isAdmin);
                        break;
                    default:
                        result.Error = $"Unknown arena action: {action}";
                        result.Suggestions = new List<string> { "enter", "exit", "status", "reset", "setzone" };
                        break;
                }
            }
            catch (Exception ex)
            {
                result.Error = $"Arena command failed: {ex.Message}";
                result.Success = false;
            }

            return result;
        }

        private CommandResult HandleArenaEnter(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { Success = true, Message = "Arena enter command processed" };
            // TODO: Implement actual arena enter logic
            return result;
        }

        private CommandResult HandleArenaExit(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { Success = true, Message = "Arena exit command processed" };
            // TODO: Implement actual arena exit logic
            return result;
        }

        private CommandResult HandleArenaStatus(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { Success = true, Message = "Arena status command processed" };
            // TODO: Implement actual arena status logic
            return result;
        }

        private CommandResult HandleArenaReset(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { Success = true, Message = "Arena reset command processed" };
            // TODO: Implement actual arena reset logic
            return result;
        }

        private CommandResult HandleArenaSetZone(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { Success = true, Message = $"Arena setzone command processed for zone: {args}" };
            // TODO: Implement actual arena setzone logic
            return result;
        }

        #endregion

        #region Automation Command Parsing

        private CommandResult ParseAutomationCommand(string command, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult
            {
                CommandType = CommandType.Automation,
                OriginalCommand = command,
                CharacterId = characterId,
                IsAdmin = isAdmin
            };

            try
            {
                if (command.StartsWith("testplan"))
                {
                    result = ParseTestPlanCommand(command, characterId, isAdmin);
                }
                else if (command.StartsWith("runplan"))
                {
                    result = ParseRunPlanCommand(command, characterId, isAdmin);
                }
                else if (command.StartsWith("scheduleplan"))
                {
                    result = ParseSchedulePlanCommand(command, characterId, isAdmin);
                }
                else if (command.StartsWith("automation"))
                {
                    result = ParseAutomationSubCommand(command, characterId, isAdmin);
                }
                else
                {
                    result.Error = "Unknown automation command";
                    result.Suggestions = new List<string> { "testplan", "runplan", "scheduleplan", "automation" };
                }
            }
            catch (Exception ex)
            {
                result.Error = $"Automation command parsing failed: {ex.Message}";
                result.Success = false;
            }

            return result;
        }

        private CommandResult ParseTestPlanCommand(string command, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { CommandType = CommandType.Automation };

            var match = Regex.Match(command, @"^testplan\s+(\w+)(?:\s+viz)?$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                result.Error = "Invalid testplan command format. Usage: testplan <planName> [viz]";
                return result;
            }

            var planName = match.Groups[1].Value;
            var showVisualization = command.Contains("viz", StringComparison.OrdinalIgnoreCase);

            // Execute test plan
            var executionResult = _executionEngine.TestPlan(planName, characterId, showVisualization);

            result.Success = executionResult.Success;
            result.Message = executionResult.Message;
            result.Error = executionResult.Error;
            result.Data = executionResult.Data;

            return result;
        }

        private CommandResult ParseRunPlanCommand(string command, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { CommandType = CommandType.Automation };

            var match = Regex.Match(command, @"^runplan\s+(\w+)$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                result.Error = "Invalid runplan command format. Usage: runplan <planName>";
                return result;
            }

            var planName = match.Groups[1].Value;

            // Execute run plan
            var executionResult = _executionEngine.RunPlan(planName, characterId);

            result.Success = executionResult.Success;
            result.Message = executionResult.Message;
            result.Error = executionResult.Error;
            result.Data = executionResult.Data;

            return result;
        }

        private CommandResult ParseSchedulePlanCommand(string command, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { CommandType = CommandType.Automation };

            var match = Regex.Match(command, @"^scheduleplan\s+(\w+)\s+at\s+(.+)$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                result.Error = "Invalid scheduleplan command format. Usage: scheduleplan <planName> at <datetime>";
                return result;
            }

            var planName = match.Groups[1].Value;
            var dateTimeStr = match.Groups[2].Value;

            if (!DateTime.TryParse(dateTimeStr, out var scheduleTime))
            {
                result.Error = $"Invalid datetime format: {dateTimeStr}";
                return result;
            }

            // TODO: Implement actual scheduling logic
            result.Success = true;
            result.Message = $"Plan '{planName}' scheduled for {scheduleTime}";
            result.Data = new { PlanName = planName, ScheduledTime = scheduleTime };

            return result;
        }

        private CommandResult ParseAutomationSubCommand(string command, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { CommandType = CommandType.Automation };

            var match = Regex.Match(command, @"^automation\s+(\w+)(?:\s+(.+))?$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                result.Error = "Invalid automation command format";
                return result;
            }

            var action = match.Groups[1].Value.ToLower();
            var args = match.Groups[2].Value;

            switch (action)
            {
                case "status":
                    result = HandleAutomationStatus(args, characterId, isAdmin);
                    break;
                case "log":
                    result = HandleAutomationLog(args, characterId, isAdmin);
                    break;
                case "approve":
                    result = HandleAutomationApprove(args, characterId, isAdmin);
                    break;
                case "deny":
                    result = HandleAutomationDeny(args, characterId, isAdmin);
                    break;
                default:
                    result.Error = $"Unknown automation action: {action}";
                    result.Suggestions = new List<string> { "status", "log", "approve", "deny" };
                    break;
            }

            return result;
        }

        private CommandResult HandleAutomationStatus(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { Success = true, Message = "Automation status retrieved" };
            // TODO: Implement actual automation status logic
            return result;
        }

        private CommandResult HandleAutomationLog(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { Success = true, Message = $"Automation log retrieved for: {args}" };
            // TODO: Implement actual automation log logic
            return result;
        }

        private CommandResult HandleAutomationApprove(string args, ulong characterId, bool isAdmin)
        {
            if (!isAdmin)
            {
                return new CommandResult { Success = false, Error = "Admin privileges required for approve command" };
            }

            var result = new CommandResult { Success = true, Message = $"Plan approved: {args}" };
            // TODO: Implement actual plan approval logic
            return result;
        }

        private CommandResult HandleAutomationDeny(string args, ulong characterId, bool isAdmin)
        {
            if (!isAdmin)
            {
                return new CommandResult { Success = false, Error = "Admin privileges required for deny command" };
            }

            var result = new CommandResult { Success = true, Message = $"Plan denied: {args}" };
            // TODO: Implement actual plan denial logic
            return result;
        }

        #endregion

        #region Zone Command Parsing

        private CommandResult ParseZoneCommand(string command, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult
            {
                CommandType = CommandType.Zone,
                OriginalCommand = command,
                CharacterId = characterId,
                IsAdmin = isAdmin
            };

            try
            {
                var match = Regex.Match(command, @"^planzone\s+(\w+)\s+(.+)$", RegexOptions.IgnoreCase);
                if (!match.Success)
                {
                    result.Error = "Invalid planzone command format";
                    result.Suggestions = new List<string>
                    {
                        "planzone set <zoneId> <x> <y> <radius>",
                        "planzone addmob <zoneId> <mobName> <count>",
                        "planzone addgear <zoneId> <itemName> <count>",
                        "planzone setboss <zoneId> <bossName>",
                        "planzone setblood <zoneId> <bloodType>",
                        "planzone setui <zoneId> <uiEffect>",
                        "planzone setglow <zoneId> <glowEffect>",
                        "planzone setmap <zoneId> <mapEffect>",
                        "planzone setrespawn <zoneId> <interval|date> <value>"
                    };
                    return result;
                }

                var action = match.Groups[1].Value.ToLower();
                var args = match.Groups[2].Value;

                switch (action)
                {
                    case "set":
                        result = ParseZoneSetCommand(args, characterId, isAdmin);
                        break;
                    case "addmob":
                        result = ParseZoneAddMobCommand(args, characterId, isAdmin);
                        break;
                    case "addgear":
                        result = ParseZoneAddGearCommand(args, characterId, isAdmin);
                        break;
                    case "setboss":
                        result = ParseZoneSetBossCommand(args, characterId, isAdmin);
                        break;
                    case "setblood":
                        result = ParseZoneSetBloodCommand(args, characterId, isAdmin);
                        break;
                    case "setui":
                        result = ParseZoneSetUiCommand(args, characterId, isAdmin);
                        break;
                    case "setglow":
                        result = ParseZoneSetGlowCommand(args, characterId, isAdmin);
                        break;
                    case "setmap":
                        result = ParseZoneSetMapCommand(args, characterId, isAdmin);
                        break;
                    case "setrespawn":
                        result = ParseZoneSetRespawnCommand(args, characterId, isAdmin);
                        break;
                    default:
                        result.Error = $"Unknown planzone action: {action}";
                        break;
                }
            }
            catch (Exception ex)
            {
                result.Error = $"Zone command parsing failed: {ex.Message}";
                result.Success = false;
            }

            return result;
        }

        private CommandResult ParseZoneSetCommand(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { CommandType = CommandType.Zone };

            var parts = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4)
            {
                result.Error = "Invalid zone set command. Usage: planzone set <zoneId> <x> <y> <radius>";
                return result;
            }

            var zoneId = parts[0];
            if (!float.TryParse(parts[1], out var x) ||
                !float.TryParse(parts[2], out var y) ||
                !float.TryParse(parts[3], out var radius))
            {
                result.Error = "Invalid coordinates or radius. Must be numeric values.";
                return result;
            }

            // Create zone
            var zone = new Zone
            {
                ZoneId = zoneId,
                Name = $"Zone_{zoneId}",
                Enabled = true,
                Difficulty = "Medium",
                Description = $"Auto-generated zone {zoneId}",
                Location = new Location
                {
                    Center = new Center { X = x, Y = y, Z = 0 },
                    Radius = radius,
                    Bounds = new Bounds
                    {
                        Min = new Center { X = x - radius, Y = y - radius, Z = -radius },
                        Max = new Center { X = x + radius, Y = y + radius, Z = radius }
                    }
                },
                Entry = new Entry
                {
                    OnEnter = new OnEnter { GiveGear = false, OverrideBlood = false },
                    OnExit = new OnExit { RemoveGear = false, RestoreBlood = true }
                },
                Mobs = new List<Mob>(),
                Boss = null,
                Loot = new Loot { Chests = new List<Chest>(), DropTables = new List<DropTable>() },
                Schematic = new Schematic { Enabled = false, SaveOnComplete = false, AutoShareToLog = false },
                Effects = new Effects
                {
                    Ui = new Ui { EnterEffect = "none", ExitEffect = "none", HudMessage = $"Entered zone {zoneId}" },
                    Glow = new Glow { EnterGlow = "none", ExitGlow = "none" },
                    Map = new Map { EnterMapEffect = "none", ExitMapEffect = "none" }
                },
                Permissions = new Permissions { RequiresDevApproval = false, RequiresAdmin = false, RequiresSnapshot = false },
                Audit = new Audit { CreatedBy = characterId.ToString(), CreatedAt = DateTime.UtcNow, LogId = Guid.NewGuid().ToString() }
            };

            // Create zone via manager
            var createResult = _zoneManager.CreateZone(zone, new Data.AutomationContext());

            result.Success = createResult.Success;
            result.Message = createResult.Success ? $"Zone {zoneId} created successfully" : $"Failed to create zone: {string.Join(", ", createResult.Messages)}";
            result.Error = createResult.Success ? null : string.Join(", ", createResult.Messages);

            return result;
        }

        private CommandResult ParseZoneAddMobCommand(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { CommandType = CommandType.Zone };

            var parts = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                result.Error = "Invalid addmob command. Usage: planzone addmob <zoneId> <mobName> <count>";
                return result;
            }

            var zoneId = parts[0];
            var mobName = parts[1];
            if (!int.TryParse(parts[2], out var count) || count <= 0)
            {
                result.Error = "Invalid count. Must be a positive integer.";
                return result;
            }

            // TODO: Implement actual mob addition logic
            result.Success = true;
            result.Message = $"Added {count}x {mobName} to zone {zoneId}";

            return result;
        }

        private CommandResult ParseZoneAddGearCommand(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { CommandType = CommandType.Zone };

            var parts = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                result.Error = "Invalid addgear command. Usage: planzone addgear <zoneId> <itemName> <count>";
                return result;
            }

            var zoneId = parts[0];
            var itemName = parts[1];
            if (!int.TryParse(parts[2], out var count) || count <= 0)
            {
                result.Error = "Invalid count. Must be a positive integer.";
                return result;
            }

            // TODO: Implement actual gear addition logic
            result.Success = true;
            result.Message = $"Added {count}x {itemName} to zone {zoneId} gear list";

            return result;
        }

        private CommandResult ParseZoneSetBossCommand(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { CommandType = CommandType.Zone };

            var parts = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                result.Error = "Invalid setboss command. Usage: planzone setboss <zoneId> <bossName>";
                return result;
            }

            var zoneId = parts[0];
            var bossName = parts[1];

            // TODO: Implement actual boss setting logic
            result.Success = true;
            result.Message = $"Set boss {bossName} for zone {zoneId}";

            return result;
        }

        private CommandResult ParseZoneSetBloodCommand(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { CommandType = CommandType.Zone };

            var parts = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                result.Error = "Invalid setblood command. Usage: planzone setblood <zoneId> <bloodType>";
                return result;
            }

            var zoneId = parts[0];
            var bloodType = parts[1];

            // TODO: Implement actual blood type setting logic
            result.Success = true;
            result.Message = $"Set blood type {bloodType} for zone {zoneId}";

            return result;
        }

        private CommandResult ParseZoneSetUiCommand(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { CommandType = CommandType.Zone };

            var parts = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                result.Error = "Invalid setui command. Usage: planzone setui <zoneId> <uiEffect>";
                return result;
            }

            var zoneId = parts[0];
            var uiEffect = parts[1];

            // TODO: Implement actual UI effect setting logic
            result.Success = true;
            result.Message = $"Set UI effect {uiEffect} for zone {zoneId}";

            return result;
        }

        private CommandResult ParseZoneSetGlowCommand(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { CommandType = CommandType.Zone };

            var parts = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                result.Error = "Invalid setglow command. Usage: planzone setglow <zoneId> <glowEffect>";
                return result;
            }

            var zoneId = parts[0];
            var glowEffect = parts[1];

            // TODO: Implement actual glow effect setting logic
            result.Success = true;
            result.Message = $"Set glow effect {glowEffect} for zone {zoneId}";

            return result;
        }

        private CommandResult ParseZoneSetMapCommand(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { CommandType = CommandType.Zone };

            var parts = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                result.Error = "Invalid setmap command. Usage: planzone setmap <zoneId> <mapEffect>";
                return result;
            }

            var zoneId = parts[0];
            var mapEffect = parts[1];

            // TODO: Implement actual map effect setting logic
            result.Success = true;
            result.Message = $"Set map effect {mapEffect} for zone {zoneId}";

            return result;
        }

        private CommandResult ParseZoneSetRespawnCommand(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { CommandType = CommandType.Zone };

            var parts = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                result.Error = "Invalid setrespawn command. Usage: planzone setrespawn <zoneId> <interval|date> <value>";
                return result;
            }

            var zoneId = parts[0];
            var respawnType = parts[1].ToLower();
            var value = parts[2];

            // TODO: Implement actual respawn setting logic
            result.Success = true;
            result.Message = $"Set respawn {respawnType}={value} for zone {zoneId}";

            return result;
        }

        #endregion

        #region Schematic Command Parsing

        private CommandResult ParseSchematicCommand(string command, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult
            {
                CommandType = CommandType.Schematic,
                OriginalCommand = command,
                CharacterId = characterId,
                IsAdmin = isAdmin
            };

            var match = Regex.Match(command, @"^schematic\s+(\w+)(?:\s+(.+))?$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                result.Error = "Invalid schematic command format";
                result.Suggestions = new List<string> { "schematic save", "schematic load <id>", "schematic list", "schematic share <id>", "schematic paste <id>" };
                return result;
            }

            var action = match.Groups[1].Value.ToLower();
            var args = match.Groups[2].Value;

            try
            {
                switch (action)
                {
                    case "save":
                        result = HandleSchematicSave(args, characterId, isAdmin);
                        break;
                    case "load":
                        result = HandleSchematicLoad(args, characterId, isAdmin);
                        break;
                    case "list":
                        result = HandleSchematicList(args, characterId, isAdmin);
                        break;
                    case "share":
                        result = HandleSchematicShare(args, characterId, isAdmin);
                        break;
                    case "paste":
                        result = HandleSchematicPaste(args, characterId, isAdmin);
                        break;
                    default:
                        result.Error = $"Unknown schematic action: {action}";
                        break;
                }
            }
            catch (Exception ex)
            {
                result.Error = $"Schematic command failed: {ex.Message}";
                result.Success = false;
            }

            return result;
        }

        private CommandResult HandleSchematicSave(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { Success = true, Message = "Schematic saved successfully" };
            // TODO: Implement actual schematic save logic
            return result;
        }

        private CommandResult HandleSchematicLoad(string args, ulong characterId, bool isAdmin)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new CommandResult { Success = false, Error = "Schematic ID required for load command" };
            }

            var result = new CommandResult { Success = true, Message = $"Schematic {args} loaded successfully" };
            // TODO: Implement actual schematic load logic
            return result;
        }

        private CommandResult HandleSchematicList(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { Success = true, Message = "Schematic list retrieved" };
            // TODO: Implement actual schematic list logic
            return result;
        }

        private CommandResult HandleSchematicShare(string args, ulong characterId, bool isAdmin)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new CommandResult { Success = false, Error = "Schematic ID required for share command" };
            }

            var result = new CommandResult { Success = true, Message = $"Schematic {args} shared successfully" };
            // TODO: Implement actual schematic share logic
            return result;
        }

        private CommandResult HandleSchematicPaste(string args, ulong characterId, bool isAdmin)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new CommandResult { Success = false, Error = "Schematic ID required for paste command" };
            }

            var result = new CommandResult { Success = true, Message = $"Schematic {args} pasted successfully" };
            // TODO: Implement actual schematic paste logic
            return result;
        }

        #endregion

        #region Castle Command Parsing

        private CommandResult ParseCastleCommand(string command, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult
            {
                CommandType = CommandType.Castle,
                OriginalCommand = command,
                CharacterId = characterId,
                IsAdmin = isAdmin
            };

            var match = Regex.Match(command, @"^castle\s+(\w+)(?:\s+(.+))?$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                result.Error = "Invalid castle command format";
                result.Suggestions = new List<string> { "castle create <name>", "castle build <name> <schematicId>", "castle list", "castle delete <name>", "castle export <name>", "castle import <schematicId>" };
                return result;
            }

            var action = match.Groups[1].Value.ToLower();
            var args = match.Groups[2].Value;

            try
            {
                switch (action)
                {
                    case "create":
                        result = HandleCastleCreate(args, characterId, isAdmin);
                        break;
                    case "build":
                        result = HandleCastleBuild(args, characterId, isAdmin);
                        break;
                    case "list":
                        result = HandleCastleList(args, characterId, isAdmin);
                        break;
                    case "delete":
                        result = HandleCastleDelete(args, characterId, isAdmin);
                        break;
                    case "export":
                        result = HandleCastleExport(args, characterId, isAdmin);
                        break;
                    case "import":
                        result = HandleCastleImport(args, characterId, isAdmin);
                        break;
                    default:
                        result.Error = $"Unknown castle action: {action}";
                        break;
                }
            }
            catch (Exception ex)
            {
                result.Error = $"Castle command failed: {ex.Message}";
                result.Success = false;
            }

            return result;
        }

        private CommandResult HandleCastleCreate(string args, ulong characterId, bool isAdmin)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new CommandResult { Success = false, Error = "Castle name required for create command" };
            }

            var result = new CommandResult { Success = true, Message = $"Castle '{args}' created successfully" };
            // TODO: Implement actual castle create logic
            return result;
        }

        private CommandResult HandleCastleBuild(string args, ulong characterId, bool isAdmin)
        {
            var parts = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                return new CommandResult { Success = false, Error = "Usage: castle build <name> <schematicId>" };
            }

            var castleName = parts[0];
            var schematicId = parts[1];

            var result = new CommandResult { Success = true, Message = $"Castle '{castleName}' built with schematic {schematicId}" };
            // TODO: Implement actual castle build logic
            return result;
        }

        private CommandResult HandleCastleList(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { Success = true, Message = "Castle list retrieved" };
            // TODO: Implement actual castle list logic
            return result;
        }

        private CommandResult HandleCastleDelete(string args, ulong characterId, bool isAdmin)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new CommandResult { Success = false, Error = "Castle name required for delete command" };
            }

            var result = new CommandResult { Success = true, Message = $"Castle '{args}' deleted successfully" };
            // TODO: Implement actual castle delete logic
            return result;
        }

        private CommandResult HandleCastleExport(string args, ulong characterId, bool isAdmin)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new CommandResult { Success = false, Error = "Castle name required for export command" };
            }

            var result = new CommandResult { Success = true, Message = $"Castle '{args}' exported successfully" };
            // TODO: Implement actual castle export logic
            return result;
        }

        private CommandResult HandleCastleImport(string args, ulong characterId, bool isAdmin)
        {
            if (string.IsNullOrEmpty(args))
            {
                return new CommandResult { Success = false, Error = "Schematic ID required for import command" };
            }

            var result = new CommandResult { Success = true, Message = $"Castle imported from schematic {args}" };
            // TODO: Implement actual castle import logic
            return result;
        }

        #endregion

        #region Logistics Command Parsing

        private CommandResult ParseLogisticsCommand(string command, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult
            {
                CommandType = CommandType.Logistics,
                OriginalCommand = command,
                CharacterId = characterId,
                IsAdmin = isAdmin
            };

            var match = Regex.Match(command, @"^logistics\s+(\w+)\s+(.+)$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                result.Error = "Invalid logistics command format";
                result.Suggestions = new List<string>
                {
                    "logistics transfer <source> <dest> <item> <count>",
                    "logistics refill <castle> <item> <count>",
                    "logistics repair <castle> <equipmentId>",
                    "logistics balance <castleA> <castleB> <resource> <amount>"
                };
                return result;
            }

            var action = match.Groups[1].Value.ToLower();
            var args = match.Groups[2].Value;

            try
            {
                switch (action)
                {
                    case "transfer":
                        result = ParseLogisticsTransferCommand(args, characterId, isAdmin);
                        break;
                    case "refill":
                        result = ParseLogisticsRefillCommand(args, characterId, isAdmin);
                        break;
                    case "repair":
                        result = ParseLogisticsRepairCommand(args, characterId, isAdmin);
                        break;
                    case "balance":
                        result = ParseLogisticsBalanceCommand(args, characterId, isAdmin);
                        break;
                    default:
                        result.Error = $"Unknown logistics action: {action}";
                        break;
                }
            }
            catch (Exception ex)
            {
                result.Error = $"Logistics command failed: {ex.Message}";
                result.Success = false;
            }

            return result;
        }

        private CommandResult ParseLogisticsTransferCommand(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { CommandType = CommandType.Logistics };

            var parts = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4)
            {
                result.Error = "Invalid transfer command. Usage: logistics transfer <source> <dest> <item> <count>";
                return result;
            }

            var source = parts[0];
            var dest = parts[1];
            var item = parts[2];
            if (!int.TryParse(parts[3], out var count) || count <= 0)
            {
                result.Error = "Invalid count. Must be a positive integer.";
                return result;
            }

            // TODO: Implement actual logistics transfer logic
            result.Success = true;
            result.Message = $"Transferred {count}x {item} from {source} to {dest}";

            return result;
        }

        private CommandResult ParseLogisticsRefillCommand(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { CommandType = CommandType.Logistics };

            var parts = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                result.Error = "Invalid refill command. Usage: logistics refill <castle> <item> <count>";
                return result;
            }

            var castle = parts[0];
            var item = parts[1];
            if (!int.TryParse(parts[2], out var count) || count <= 0)
            {
                result.Error = "Invalid count. Must be a positive integer.";
                return result;
            }

            // TODO: Implement actual logistics refill logic
            result.Success = true;
            result.Message = $"Refilled {count}x {item} for castle {castle}";

            return result;
        }

        private CommandResult ParseLogisticsRepairCommand(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { CommandType = CommandType.Logistics };

            var parts = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                result.Error = "Invalid repair command. Usage: logistics repair <castle> <equipmentId>";
                return result;
            }

            var castle = parts[0];
            var equipmentId = parts[1];

            // TODO: Implement actual logistics repair logic
            result.Success = true;
            result.Message = $"Repaired equipment {equipmentId} for castle {castle}";

            return result;
        }

        private CommandResult ParseLogisticsBalanceCommand(string args, ulong characterId, bool isAdmin)
        {
            var result = new CommandResult { CommandType = CommandType.Logistics };

            var parts = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4)
            {
                result.Error = "Invalid balance command. Usage: logistics balance <castleA> <castleB> <resource> <amount>";
                return result;
            }

            var castleA = parts[0];
            var castleB = parts[1];
            var resource = parts[2];
            if (!float.TryParse(parts[3], out var amount) || amount <= 0)
            {
                result.Error = "Invalid amount. Must be a positive number.";
                return result;
            }

            // TODO: Implement actual logistics balance logic
            result.Success = true;
            result.Message = $"Balanced {amount}x {resource} between {castleA} and {castleB}";

            return result;
        }

        #endregion

        #region Utility Methods

        private string NormalizeCommand(string command)
        {
            return command?.Trim() ?? string.Empty;
        }

        private List<string> GetCommandSuggestions(string input)
        {
            var suggestions = new List<string>();

            // Basic suggestions based on command type
            var commandType = DetectCommandType(input);
            switch (commandType)
            {
                case CommandType.Arena:
                    suggestions.AddRange(new[] { "arena enter", "arena exit", "arena status", "arena reset", "arena setzone <id>" });
                    break;
                case CommandType.Automation:
                    suggestions.AddRange(new[] { "testplan <name>", "runplan <name>", "scheduleplan <name> at <time>", "automation status" });
                    break;
                case CommandType.Zone:
                    suggestions.AddRange(new[] { "planzone set <id> <x> <y> <radius>", "planzone addmob <id> <name> <count>" });
                    break;
                case CommandType.Schematic:
                    suggestions.AddRange(new[] { "schematic save", "schematic load <id>", "schematic list" });
                    break;
                case CommandType.Castle:
                    suggestions.AddRange(new[] { "castle create <name>", "castle build <name> <id>", "castle list" });
                    break;
                case CommandType.Logistics:
                    suggestions.AddRange(new[] { "logistics transfer <src> <dest> <item> <count>", "logistics refill <castle> <item> <count>" });
                    break;
            }

            // Add grammar service suggestions if available
            if (_grammarService != null)
            {
                var grammarResult = _grammarService.ParseCommand(input);
                if (grammarResult.Suggestions != null)
                {
                    suggestions.AddRange(grammarResult.Suggestions);
                }
            }

            return suggestions.Distinct().Take(5).ToList();
        }

        #endregion
    }

    /// <summary>
    /// Command result structure
    /// </summary>
    public class CommandResult
    {
        public string OriginalCommand { get; set; }
        public string NormalizedCommand { get; set; }
        public CommandType CommandType { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
        public object Data { get; set; }
        public List<string> Suggestions { get; set; } = new List<string>();
        public ulong CharacterId { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Command type enumeration
    /// </summary>
    public enum CommandType
    {
        Unknown,
        Arena,
        Automation,
        Zone,
        Schematic,
        Castle,
        Logistics
    }
    */
}