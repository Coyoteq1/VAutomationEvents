using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using VAuto.Services.Interfaces;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Chat Grammar Service - validates and parses user chat commands for automation
    /// Optional service for enforcing command grammar and providing suggestions
    /// </summary>
    public class ChatGrammarService : IService, IServiceHealthMonitor
    {
        private static ChatGrammarService _instance;
        public static ChatGrammarService Instance => _instance ??= new ChatGrammarService();

        private bool _isInitialized;
        private ManualLogSource _log;

        // Grammar patterns
        private readonly Dictionary<string, CommandGrammar> _commandGrammars = new();

        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => _log;

        public void Initialize()
        {
            if (_isInitialized) return;
            _log = ServiceManager.Log;

            InitializeGrammars();

            _isInitialized = true;
            _log?.LogInfo("[ChatGrammarService] Initialized with command grammars");
        }

        public void Cleanup()
        {
            _commandGrammars.Clear();
            _isInitialized = false;
        }

        private void InitializeGrammars()
        {
            // Test plan command
            _commandGrammars["testplan"] = new CommandGrammar
            {
                Pattern = @"^testplan\s+(\w+)(?:\s+viz)?$",
                Description = "Test an automation plan",
                Examples = new[] { "testplan myplan", "testplan myplan viz" },
                Parameters = new[] { "planId", "visualization" }
            };

            // Run plan command
            _commandGrammars["runplan"] = new CommandGrammar
            {
                Pattern = @"^runplan\s+(\w+)$",
                Description = "Execute an automation plan",
                Examples = new[] { "runplan myplan" },
                Parameters = new[] { "planId" }
            };

            // Schedule plan command
            _commandGrammars["scheduleplan"] = new CommandGrammar
            {
                Pattern = @"^scheduleplan\s+(\w+)\s+at\s+(.+)$",
                Description = "Schedule a plan for future execution",
                Examples = new[] { "scheduleplan myplan at 2024-01-01 15:30" },
                Parameters = new[] { "planId", "datetime" }
            };

            // List plans command
            _commandGrammars["listplans"] = new CommandGrammar
            {
                Pattern = @"^listplans$",
                Description = "List all loaded automation plans",
                Examples = new[] { "listplans" },
                Parameters = new string[0]
            };

            // Unload plan command
            _commandGrammars["unloadplan"] = new CommandGrammar
            {
                Pattern = @"^unloadplan\s+(\w+)$",
                Description = "Unload an automation plan",
                Examples = new[] { "unloadplan myplan" },
                Parameters = new[] { "planId" }
            };

            // Analytics command
            _commandGrammars["analytics"] = new CommandGrammar
            {
                Pattern = @"^analytics\s+(\w+)$",
                Description = "Show analytics for a plan",
                Examples = new[] { "analytics myplan" },
                Parameters = new[] { "planId" }
            };
        }

        /// <summary>
        /// Parse and validate a chat command
        /// </summary>
        public GrammarResult ParseCommand(string message)
        {
            var result = new GrammarResult { OriginalMessage = message };

            try
            {
                var trimmed = message.Trim().ToLower();

                foreach (var kvp in _commandGrammars)
                {
                    var match = Regex.Match(trimmed, kvp.Value.Pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        result.IsValid = true;
                        result.Command = kvp.Key;
                        result.Parameters = new Dictionary<string, string>();

                        // Extract parameters
                        for (int i = 1; i < match.Groups.Count; i++)
                        {
                            var paramName = kvp.Value.Parameters[i - 1];
                            var paramValue = match.Groups[i].Value;
                            result.Parameters[paramName] = paramValue;
                        }

                        // Validate parameter values
                        if (ValidateParameters(result))
                        {
                            result.Success = true;
                        }
                        else
                        {
                            result.IsValid = false;
                            result.Error = "Invalid parameter values";
                        }

                        return result;
                    }
                }

                // No matching grammar found
                result.IsValid = false;
                result.Error = "Unknown command format";
                result.Suggestions = GetSuggestions(trimmed);

            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Error = $"Parse error: {ex.Message}";
                _log?.LogError($"[ChatGrammar] Parse error for '{message}': {ex.Message}");
            }

            return result;
        }

        private bool ValidateParameters(GrammarResult result)
        {
            foreach (var param in result.Parameters)
            {
                switch (param.Key)
                {
                    case "planId":
                        if (!IsValidPlanId(param.Value))
                            return false;
                        break;
                    case "datetime":
                        if (!DateTime.TryParse(param.Value, out _))
                            return false;
                        break;
                }
            }
            return true;
        }

        private bool IsValidPlanId(string planId)
        {
            // Plan IDs should be alphanumeric with underscores
            return Regex.IsMatch(planId, @"^[a-zA-Z0-9_]+$") && planId.Length <= 32;
        }

        private List<string> GetSuggestions(string input)
        {
            var suggestions = new List<string>();

            // Find similar commands
            foreach (var kvp in _commandGrammars)
            {
                if (kvp.Key.Contains(input) || input.Contains(kvp.Key))
                {
                    suggestions.AddRange(kvp.Value.Examples);
                }
            }

            // If no suggestions, show all commands
            if (suggestions.Count == 0)
            {
                foreach (var kvp in _commandGrammars)
                {
                    suggestions.AddRange(kvp.Value.Examples);
                }
            }

            return suggestions.Distinct().Take(5).ToList();
        }

        /// <summary>
        /// Get help for a command
        /// </summary>
        public CommandHelp GetCommandHelp(string command)
        {
            if (_commandGrammars.TryGetValue(command.ToLower(), out var grammar))
            {
                return new CommandHelp
                {
                    Command = command,
                    Description = grammar.Description,
                    Usage = grammar.Pattern,
                    Examples = grammar.Examples,
                    Parameters = grammar.Parameters
                };
            }
            return null;
        }

        /// <summary>
        /// Get all available commands
        /// </summary>
        public IEnumerable<string> GetAvailableCommands()
        {
            return _commandGrammars.Keys;
        }

        public ServiceHealthStatus GetHealthStatus()
        {
            return new ServiceHealthStatus
            {
                ServiceName = "ChatGrammarService",
                IsHealthy = _isInitialized,
                Status = _isInitialized ? "Active" : "Inactive",
                LastCheck = DateTime.UtcNow
            };
        }

        public ServicePerformanceMetrics GetPerformanceMetrics()
        {
            return new ServicePerformanceMetrics
            {
                ServiceName = "ChatGrammarService",
                ActiveOperations = _commandGrammars.Count,
                MeasuredAt = DateTime.UtcNow
            };
        }

        public int GetErrorCount() => 0;
        public string GetLastError() => null;
    }

    // Grammar data structures
    public class CommandGrammar
    {
        public string Pattern { get; set; }
        public string Description { get; set; }
        public string[] Examples { get; set; }
        public string[] Parameters { get; set; }
    }

    public class GrammarResult
    {
        public string OriginalMessage { get; set; }
        public bool IsValid { get; set; }
        public bool Success { get; set; }
        public string Command { get; set; }
        public Dictionary<string, string> Parameters { get; set; } = new();
        public string Error { get; set; }
        public List<string> Suggestions { get; set; } = new();
    }

    public class CommandHelp
    {
        public string Command { get; set; }
        public string Description { get; set; }
        public string Usage { get; set; }
        public string[] Examples { get; set; }
        public string[] Parameters { get; set; }
    }
}