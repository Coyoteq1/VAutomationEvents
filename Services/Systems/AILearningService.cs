using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using VAuto.Commands;
using VAuto.Services.Systems;
using VAuto.Services.Interfaces;
using BepInEx;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// AI Learning Service for the Vauto automation system
    /// Learns from user commands, validation failures, and system patterns
    /// </summary>
    public class AILearningService : IService, IServiceHealthMonitor
    {
        private static AILearningService _instance;
        public static AILearningService Instance => _instance ??= new AILearningService();

        private bool _isInitialized;
        private ManualLogSource _log;
        private LiteDatabase _database;

        // LiteDB collections
        private ILiteCollection<CommandPattern> _commandPatterns;
        private ILiteCollection<ValidationFailure> _validationFailures;
        private ILiteCollection<UserPreference> _userPreferences;
        private ILiteCollection<SystemPattern> _systemPatterns;

        // Configuration
        private const int MaxDatasetSize = 10000; // Increased for LiteDB efficiency
        private const string DatabaseFileName = "ai_learning.db";

        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => _log;

        public void Initialize()
        {
            if (_isInitialized) return;
            _log = ServiceManager.Log;

            try
            {
                var dbPath = System.IO.Path.Combine(BepInEx.Paths.ConfigPath, "VAuto", "Data", DatabaseFileName);
                var directory = System.IO.Path.GetDirectoryName(dbPath);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                _database = new LiteDatabase(dbPath);

                // Initialize collections
                _commandPatterns = _database.GetCollection<CommandPattern>("command_patterns");
                _validationFailures = _database.GetCollection<ValidationFailure>("validation_failures");
                _userPreferences = _database.GetCollection<UserPreference>("user_preferences");
                _systemPatterns = _database.GetCollection<SystemPattern>("system_patterns");

                // Create indexes for efficient queries
                _commandPatterns.EnsureIndex(x => x.CommandType);
                _commandPatterns.EnsureIndex(x => x.Timestamp);
                _commandPatterns.EnsureIndex(x => x.CharacterId);
                _commandPatterns.EnsureIndex(x => x.Success);

                _validationFailures.EnsureIndex(x => x.Timestamp);
                _validationFailures.EnsureIndex(x => x.PlanName);

                _userPreferences.EnsureIndex(x => x.CharacterId);
                _userPreferences.EnsureIndex(x => x.PreferenceType);

                _systemPatterns.EnsureIndex(x => x.PatternType);
                _systemPatterns.EnsureIndex(x => x.Timestamp);

                _isInitialized = true;
                _log?.LogInfo("[AILearningService] Initialized with LiteDB AI learning capabilities");
            }
            catch (Exception ex)
            {
                _log?.LogError($"[AILearningService] Failed to initialize: {ex.Message}");
                throw;
            }
        }

        public void Cleanup()
        {
            if (_database != null)
            {
                _database.Dispose();
                _database = null;
            }
            _isInitialized = false;
            _log?.LogInfo("[AILearningService] Cleaned up LiteDB AI learning service");
        }

        #region Command Learning

        /// <summary>
        /// Record a user command for learning
        /// </summary>
        public void RecordCommand(CommandResult commandResult)
        {
            try
            {
                var commandPattern = new CommandPattern
                {
                    CommandId = Guid.NewGuid().ToString(),
                    OriginalCommand = commandResult.OriginalCommand,
                    NormalizedCommand = commandResult.NormalizedCommand,
                    CommandType = commandResult.CommandType,
                    CharacterId = commandResult.CharacterId,
                    IsAdmin = commandResult.IsAdmin,
                    Success = commandResult.Success,
                    Timestamp = DateTime.UtcNow,
                    Parameters = ExtractCommandParameters(commandResult)
                };

                // Add to LiteDB collection
                _commandPatterns.Insert(commandPattern);

                // Keep dataset size manageable
                if (_commandPatterns.Count() > MaxDatasetSize)
                {
                    // Remove oldest entries
                    var oldestEntries = _commandPatterns.Query()
                        .OrderBy(x => x.Timestamp)
                        .Limit(_commandPatterns.Count() - MaxDatasetSize)
                        .ToList();

                    foreach (var entry in oldestEntries)
                    {
                        _commandPatterns.Delete(entry.CommandId);
                    }
                }

                // Analyze patterns
                AnalyzeCommandPatterns();

                _log?.LogDebug($"[AILearning] Recorded command: {commandResult.OriginalCommand}");
            }
            catch (Exception ex)
            {
                _log?.LogError($"[AILearning] Error recording command: {ex.Message}");
            }
        }

        private Dictionary<string, string> ExtractCommandParameters(CommandResult commandResult)
        {
            var parameters = new Dictionary<string, string>();

            try
            {
                // Extract parameters based on command type
                switch (commandResult.CommandType)
                {
                    case CommandType.Zone:
                        if (commandResult.OriginalCommand.StartsWith("planzone set"))
                        {
                            var parts = commandResult.OriginalCommand.Split(' ');
                            if (parts.Length >= 6)
                            {
                                parameters["zoneId"] = parts[2];
                                parameters["x"] = parts[3];
                                parameters["y"] = parts[4];
                                parameters["radius"] = parts[5];
                            }
                        }
                        break;

                    case CommandType.Automation:
                        if (commandResult.OriginalCommand.StartsWith("testplan") ||
                            commandResult.OriginalCommand.StartsWith("runplan"))
                        {
                            var parts = commandResult.OriginalCommand.Split(' ');
                            if (parts.Length >= 2)
                            {
                                parameters["planName"] = parts[1];
                            }
                        }
                        break;

                    // Add more parameter extraction logic for other command types
                }
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[AILearning] Error extracting parameters: {ex.Message}");
            }

            return parameters;
        }

        private void AnalyzeCommandPatterns()
        {
            try
            {
                // Group commands by type and analyze frequency using LiteDB
                var commandFrequency = _commandPatterns.Query()
                    .GroupBy(x => x.CommandType)
                    .Select(x => new { CommandType = x.Key, Count = x.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                // Identify common command patterns
                var commonPatterns = _commandPatterns.Query()
                    .GroupBy(x => x.NormalizedCommand)
                    .Where(g => g.Count() > 1)
                    .Select(g => new { Command = g.Key, Frequency = g.Count() })
                    .OrderByDescending(x => x.Frequency)
                    .Limit(5)
                    .ToList();

                // Log insights
                _log?.LogInfo($"[AILearning] Command frequency analysis: {string.Join(", ", commandFrequency.Select(c => $"{c.CommandType}:{c.Count}"))}");
                _log?.LogInfo($"[AILearning] Common patterns: {string.Join(", ", commonPatterns.Select(p => $"{p.Command}({p.Frequency})"))}");
            }
            catch (Exception ex)
            {
                _log?.LogError($"[AILearning] Error analyzing command patterns: {ex.Message}");
            }
        }

        #endregion

        #region Validation Learning

        /// <summary>
        /// Record a validation failure for learning
        /// </summary>
        public void RecordValidationFailure(ComprehensiveValidationResult validationResult, string planName, ulong characterId)
        {
            try
            {
                var failure = new ValidationFailure
                {
                    FailureId = Guid.NewGuid().ToString(),
                    PlanName = planName,
                    CharacterId = characterId,
                    Timestamp = DateTime.UtcNow,
                    FailureMessages = validationResult.Messages ?? new List<string>(),
                    ZoneFailures = validationResult.ZoneValidations?.Where(v => !v.IsValid).Count() ?? 0,
                    SchematicFailures = validationResult.SchematicValidations?.Where(v => !v.IsValid).Count() ?? 0,
                    LogisticsFailures = validationResult.LogisticsValidations?.Where(v => !v.IsValid).Count() ?? 0,
                    CastleFailures = validationResult.CastleValidations?.Where(v => !v.IsValid).Count() ?? 0,
                    RequiresDevApproval = validationResult.RequiresDevApproval,
                    RequiresSnapshot = validationResult.RequiresSnapshot
                };

                // Add to LiteDB collection
                _validationFailures.Insert(failure);

                // Keep dataset size manageable
                if (_validationFailures.Count() > MaxDatasetSize)
                {
                    // Remove oldest entries
                    var oldestEntries = _validationFailures.Query()
                        .OrderBy(x => x.Timestamp)
                        .Limit(_validationFailures.Count() - MaxDatasetSize)
                        .ToList();

                    foreach (var entry in oldestEntries)
                    {
                        _validationFailures.Delete(entry.FailureId);
                    }
                }

                // Analyze failure patterns
                AnalyzeValidationFailures();

                _log?.LogDebug($"[AILearning] Recorded validation failure for plan: {planName}");
            }
            catch (Exception ex)
            {
                _log?.LogError($"[AILearning] Error recording validation failure: {ex.Message}");
            }
        }

        private void AnalyzeValidationFailures()
        {
            try
            {
                // Analyze failure frequency by type using LiteDB aggregation
                var failureByType = new
                {
                    ZoneFailures = _validationFailures.Sum(f => f.ZoneFailures),
                    SchematicFailures = _validationFailures.Sum(f => f.SchematicFailures),
                    LogisticsFailures = _validationFailures.Sum(f => f.LogisticsFailures),
                    CastleFailures = _validationFailures.Sum(f => f.CastleFailures)
                };

                // Identify common failure messages using LiteDB
                var allFailures = _validationFailures.Query()
                    .SelectMany(f => f.FailureMessages.Select(m => new { Message = m }))
                    .GroupBy(x => x.Message)
                    .Where(g => g.Count() > 1)
                    .Select(g => new { Message = g.Key, Frequency = g.Count() })
                    .OrderByDescending(x => x.Frequency)
                    .Limit(5)
                    .ToList();

                // Log insights
                _log?.LogInfo($"[AILearning] Validation failure analysis - Zones: {failureByType.ZoneFailures}, Schematics: {failureByType.SchematicFailures}, Logistics: {failureByType.LogisticsFailures}, Castles: {failureByType.CastleFailures}");
                _log?.LogInfo($"[AILearning] Common failure messages: {string.Join(", ", allFailures.Select(m => $"{m.Message}({m.Frequency})"))}");
            }
            catch (Exception ex)
            {
                _log?.LogError($"[AILearning] Error analyzing validation failures: {ex.Message}");
            }
        }

        #endregion

        #region User Preference Learning

        /// <summary>
        /// Record a user preference
        /// </summary>
        public void RecordUserPreference(string preferenceType, string preferenceValue, ulong characterId)
        {
            try
            {
                var preference = new UserPreference
                {
                    PreferenceId = Guid.NewGuid().ToString(),
                    CharacterId = characterId,
                    PreferenceType = preferenceType,
                    PreferenceValue = preferenceValue,
                    Timestamp = DateTime.UtcNow,
                    Frequency = 1
                };

                // Check if this preference already exists for the user using LiteDB
                var existingPreference = _userPreferences.FindOne(p =>
                    p.CharacterId == characterId &&
                    p.PreferenceType == preferenceType &&
                    p.PreferenceValue == preferenceValue);

                if (existingPreference != null)
                {
                    existingPreference.Frequency++;
                    existingPreference.Timestamp = DateTime.UtcNow;
                    _userPreferences.Update(existingPreference);
                }
                else
                {
                    _userPreferences.Insert(preference);

                    // Keep dataset size manageable
                    if (_userPreferences.Count() > MaxDatasetSize)
                    {
                        // Remove oldest entries
                        var oldestEntries = _userPreferences.Query()
                            .OrderBy(x => x.Timestamp)
                            .Limit(_userPreferences.Count() - MaxDatasetSize)
                            .ToList();

                        foreach (var entry in oldestEntries)
                        {
                            _userPreferences.Delete(entry.PreferenceId);
                        }
                    }
                }

                _log?.LogDebug($"[AILearning] Recorded user preference: {preferenceType}={preferenceValue} for user {characterId}");
            }
            catch (Exception ex)
            {
                _log?.LogError($"[AILearning] Error recording user preference: {ex.Message}");
            }
        }

        #endregion

        #region System Pattern Learning

        /// <summary>
        /// Record a system pattern
        /// </summary>
        public void RecordSystemPattern(string patternType, string patternData, bool isSuccessful)
        {
            try
            {
                var pattern = new SystemPattern
                {
                    PatternId = Guid.NewGuid().ToString(),
                    PatternType = patternType,
                    PatternData = patternData,
                    IsSuccessful = isSuccessful,
                    Timestamp = DateTime.UtcNow,
                    Frequency = 1
                };

                // Check if this pattern already exists using LiteDB
                var existingPattern = _systemPatterns.FindOne(p =>
                    p.PatternType == patternType &&
                    p.PatternData == patternData &&
                    p.IsSuccessful == isSuccessful);

                if (existingPattern != null)
                {
                    existingPattern.Frequency++;
                    existingPattern.Timestamp = DateTime.UtcNow;
                    _systemPatterns.Update(existingPattern);
                }
                else
                {
                    _systemPatterns.Insert(pattern);

                    // Keep dataset size manageable
                    if (_systemPatterns.Count() > MaxDatasetSize)
                    {
                        // Remove oldest entries
                        var oldestEntries = _systemPatterns.Query()
                            .OrderBy(x => x.Timestamp)
                            .Limit(_systemPatterns.Count() - MaxDatasetSize)
                            .ToList();

                        foreach (var entry in oldestEntries)
                        {
                            _systemPatterns.Delete(entry.PatternId);
                        }
                    }
                }

                _log?.LogDebug($"[AILearning] Recorded system pattern: {patternType}={patternData} (Success: {isSuccessful})");
            }
            catch (Exception ex)
            {
                _log?.LogError($"[AILearning] Error recording system pattern: {ex.Message}");
            }
        }

        #endregion

        #region Suggestion Generation

        /// <summary>
        /// Generate command suggestions based on learned patterns
        /// </summary>
        public List<string> GenerateCommandSuggestions(string partialCommand, ulong characterId)
        {
            var suggestions = new List<string>();

            try
            {
                // Get user-specific command patterns using LiteDB
                var userCommands = _commandPatterns.Query()
                    .Where(c => c.CharacterId == characterId)
                    .OrderByDescending(c => c.Timestamp)
                    .Limit(10)
                    .ToList();

                // Get global command patterns
                var globalCommands = _commandPatterns.Query()
                    .OrderByDescending(c => c.Timestamp)
                    .Limit(20)
                    .ToList();

                // Combine and find matching patterns
                var allCommands = userCommands.Concat(globalCommands)
                    .GroupBy(c => c.NormalizedCommand)
                    .Select(g => g.First())
                    .ToList();

                foreach (var command in allCommands)
                {
                    if (command.NormalizedCommand.StartsWith(partialCommand, StringComparison.OrdinalIgnoreCase))
                    {
                        suggestions.Add(command.NormalizedCommand);
                    }
                }

                // Add common patterns if we don't have enough suggestions
                if (suggestions.Count < 3)
                {
                    var commonPatterns = _commandPatterns.Query()
                        .GroupBy(x => x.NormalizedCommand)
                        .Where(g => g.Count() > 1)
                        .Select(g => new { Command = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Limit(5)
                        .ToList()
                        .Select(x => x.Command)
                        .ToList();

                    foreach (var pattern in commonPatterns)
                    {
                        if (!suggestions.Contains(pattern) && pattern.StartsWith(partialCommand, StringComparison.OrdinalIgnoreCase))
                        {
                            suggestions.Add(pattern);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[AILearning] Error generating command suggestions: {ex.Message}");
            }

            return suggestions.Distinct().Take(5).ToList();
        }

        /// <summary>
        /// Generate validation improvement suggestions
        /// </summary>
        public List<string> GenerateValidationImprovementSuggestions()
        {
            var suggestions = new List<string>();

            try
            {
                // Analyze common validation failures
                var commonFailures = _validationFailures
                    .SelectMany(vf => vf.FailureMessages)
                    .GroupBy(m => m)
                    .Where(g => g.Count() > 1)
                    .OrderByDescending(g => g.Count())
                    .Take(3)
                    .ToList();

                foreach (var failure in commonFailures)
                {
                    suggestions.Add($"Common issue: {failure.Key} (occurred {failure.Count()} times)");
                }

                // Suggest improvements based on failure patterns
                if (commonFailures.Any())
                {
                    suggestions.Add("Consider adding more detailed error messages for common validation failures");
                    suggestions.Add("Review zone configuration templates to prevent common mistakes");
                }

                // Check for frequent dev approval requirements
                var devApprovalCount = _validationFailures.Count(vf => vf.RequiresDevApproval);
                if (devApprovalCount > _validationFailures.Count * 0.3) // More than 30% require dev approval
                {
                    suggestions.Add($"High dev approval rate ({devApprovalCount}/{_validationFailures.Count}). Consider relaxing some validation rules.");
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[AILearning] Error generating validation suggestions: {ex.Message}");
            }

            return suggestions;
        }

        #endregion

        #region Training Data Export

        /// <summary>
        /// Export training data for external analysis
        /// </summary>
        public string ExportTrainingData()
        {
            try
            {
                var exportData = new
                {
                    Statistics = new
                    {
                        CommandPatterns = _commandPatterns.Count,
                        ValidationFailures = _validationFailures.Count,
                        UserPreferences = _userPreferences.Count,
                        SystemPatterns = _systemPatterns.Count,
                        TotalDataPoints = _commandPatterns.Count + _validationFailures.Count + _userPreferences.Count + _systemPatterns.Count
                    },
                    CommandPatternAnalysis = AnalyzeCommandPatternsForExport(),
                    ValidationFailureAnalysis = AnalyzeValidationFailuresForExport(),
                    UserPreferenceAnalysis = AnalyzeUserPreferencesForExport(),
                    SystemPatternAnalysis = AnalyzeSystemPatternsForExport()
                };

                return JsonSerializer.Serialize(exportData, VAuto.Extensions.VRCoreStubs.SchematicService.GetJsonOptions());
            }
            catch (Exception ex)
            {
                _log?.LogError($"[AILearning] Error exporting training data: {ex.Message}");
                return "{\"error\": \"Failed to export training data\"}";
            }
        }

        private object AnalyzeCommandPatternsForExport()
        {
            return new
            {
                TotalCommands = _commandPatterns.Count,
                ByType = _commandPatterns
                    .GroupBy(c => c.CommandType)
                    .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
                    .OrderByDescending(x => x.Count),
                MostCommonCommands = _commandPatterns
                    .GroupBy(c => c.NormalizedCommand)
                    .Select(g => new { Command = g.Key, Frequency = g.Count() })
                    .OrderByDescending(x => x.Frequency)
                    .Take(10),
                SuccessRate = _commandPatterns.Count(c => c.Success) / (float)Math.Max(1, _commandPatterns.Count)
            };
        }

        private object AnalyzeValidationFailuresForExport()
        {
            return new
            {
                TotalFailures = _validationFailures.Count,
                ByType = new
                {
                    ZoneFailures = _validationFailures.Sum(f => f.ZoneFailures),
                    SchematicFailures = _validationFailures.Sum(f => f.SchematicFailures),
                    LogisticsFailures = _validationFailures.Sum(f => f.LogisticsFailures),
                    CastleFailures = _validationFailures.Sum(f => f.CastleFailures)
                },
                MostCommonFailureMessages = _validationFailures
                    .SelectMany(f => f.FailureMessages)
                    .GroupBy(m => m)
                    .Select(g => new { Message = g.Key, Frequency = g.Count() })
                    .OrderByDescending(x => x.Frequency)
                    .Take(10),
                DevApprovalRate = _validationFailures.Count(f => f.RequiresDevApproval) / (float)Math.Max(1, _validationFailures.Count),
                SnapshotRequirementRate = _validationFailures.Count(f => f.RequiresSnapshot) / (float)Math.Max(1, _validationFailures.Count)
            };
        }

        private object AnalyzeUserPreferencesForExport()
        {
            return new
            {
                TotalPreferences = _userPreferences.Count,
                ByType = _userPreferences
                    .GroupBy(p => p.PreferenceType)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count),
                MostCommonPreferences = _userPreferences
                    .GroupBy(p => new { p.PreferenceType, p.PreferenceValue })
                    .Select(g => new { Type = g.Key.PreferenceType, Value = g.Key.PreferenceValue, Frequency = g.Count() })
                    .OrderByDescending(x => x.Frequency)
                    .Take(10)
            };
        }

        private object AnalyzeSystemPatternsForExport()
        {
            return new
            {
                TotalPatterns = _systemPatterns.Count,
                ByType = _systemPatterns
                    .GroupBy(p => p.PatternType)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count),
                SuccessRate = _systemPatterns.Count(p => p.IsSuccessful) / (float)Math.Max(1, _systemPatterns.Count),
                MostCommonPatterns = _systemPatterns
                    .GroupBy(p => new { p.PatternType, p.PatternData, p.IsSuccessful })
                    .Select(g => new { Type = g.Key.PatternType, Data = g.Key.PatternData, Success = g.Key.IsSuccessful, Frequency = g.Count() })
                    .OrderByDescending(x => x.Frequency)
                    .Take(10)
            };
        }

        #endregion

        public ServiceHealthStatus GetHealthStatus()
        {
            return new ServiceHealthStatus
            {
                ServiceName = "AILearningService",
                IsHealthy = _isInitialized,
                Status = _isInitialized ? "Active" : "Inactive",
                LastCheck = DateTime.UtcNow
            };
        }

        public ServicePerformanceMetrics GetPerformanceMetrics()
        {
            return new ServicePerformanceMetrics
            {
                ServiceName = "AILearningService",
                ActiveOperations = _commandPatterns.Count + _validationFailures.Count,
                MeasuredAt = DateTime.UtcNow
            };
        }

        public int GetErrorCount() => 0;
        public string GetLastError() => null;
    }

    #region Learning Data Classes

    /// <summary>
    /// Represents a recorded command pattern
    /// </summary>
    public class CommandPattern
    {
        public string CommandId { get; set; }
        public string OriginalCommand { get; set; }
        public string NormalizedCommand { get; set; }
        public CommandType CommandType { get; set; }
        public ulong CharacterId { get; set; }
        public bool IsAdmin { get; set; }
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Represents a validation failure for learning
    /// </summary>
    public class ValidationFailure
    {
        public string FailureId { get; set; }
        public string PlanName { get; set; }
        public ulong CharacterId { get; set; }
        public DateTime Timestamp { get; set; }
        public List<string> FailureMessages { get; set; } = new();
        public int ZoneFailures { get; set; }
        public int SchematicFailures { get; set; }
        public int LogisticsFailures { get; set; }
        public int CastleFailures { get; set; }
        public bool RequiresDevApproval { get; set; }
        public bool RequiresSnapshot { get; set; }
    }

    /// <summary>
    /// Represents a user preference
    /// </summary>
    public class UserPreference
    {
        public string PreferenceId { get; set; }
        public ulong CharacterId { get; set; }
        public string PreferenceType { get; set; }
        public string PreferenceValue { get; set; }
        public DateTime Timestamp { get; set; }
        public int Frequency { get; set; }
    }

    /// <summary>
    /// Represents a system pattern
    /// </summary>
    public class SystemPattern
    {
        public string PatternId { get; set; }
        public string PatternType { get; set; }
        public string PatternData { get; set; }
        public bool IsSuccessful { get; set; }
        public DateTime Timestamp { get; set; }
        public int Frequency { get; set; }
    }

    #endregion
}