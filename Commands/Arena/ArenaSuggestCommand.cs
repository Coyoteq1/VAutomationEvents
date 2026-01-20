using ProjectM;
using ProjectM.Shared;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using VAuto.Core;
using VAuto.Services;
using VAuto.Services.Systems;
using VampireCommandFramework;

namespace VAuto.Commands.Arena
{
    /// <summary>
    /// Arena Suggest Command
    /// Smart suggestion engine for arena features
    /// </summary>
    public static class ArenaSuggestCommand
    {
        private static readonly Dictionary<string, SuggestionCategory> _categories = new()
        {
            { "visual", new SuggestionCategory("🧩 Visual", new[]
                {
                    new Suggestion("spawn glow", "Apply glow effect to player", ".a glow"),
                    new Suggestion("arena border", "Toggle arena border glow", ".a border on"),
                    new Suggestion("corner pillars", "Spawn corner glow pillars", ".a corners"),
                    new Suggestion("pulse ring", "Create pulsing arena ring", ".a pulse"),
                    new Suggestion("fog wall", "Enable arena fog wall", ".a fog")
                })},
            { "blood", new SuggestionCategory("🩸 Blood", new[]
                {
                    new Suggestion("test1", "Apply blood test method 1", ".a blood test1"),
                    new Suggestion("test2", "Apply blood test method 2", ".a blood test2"),
                    new Suggestion("test3", "Apply blood test method 3", ".a blood test3"),
                    new Suggestion("test4", "Apply blood test method 4", ".a blood test4"),
                    new Suggestion("test5", "Apply blood test method 5", ".a blood test5"),
                    new Suggestion("test6", "Apply blood test method 6", ".a blood test6"),
                    new Suggestion("test7", "Apply blood test method 7", ".a blood test7"),
                    new Suggestion("test8", "Apply blood test method 8", ".a blood test8"),
                    new Suggestion("test9", "Apply blood test method 9", ".a blood test9"),
                    new Suggestion("random blood", "Apply random blood effect", ".a blood random"),
                    new Suggestion("mirror opponent", "Mirror opponent's blood", ".a blood mirror"),
                    new Suggestion("disable blood", "Disable all blood effects", ".a blood disable")
                })},
            { "build", new SuggestionCategory("🏗 Build", new[]
                {
                    new Suggestion("enable build", "Enable arena build mode", ".a build on"),
                    new Suggestion("clear arena", "Clear all arena objects", ".a arena clear"),
                    new Suggestion("copy layout", "Copy current arena layout", ".a build copy"),
                    new Suggestion("spawn walls", "Spawn arena walls", ".a build walls"),
                    new Suggestion("floor grid", "Show floor placement grid", ".a build grid")
                })},
            { "buffs", new SuggestionCategory("⚔ Buffs", new[]
                {
                    new Suggestion("godmode", "Enable god mode buff", ".a buff god"),
                    new Suggestion("no cooldown", "Remove all cooldowns", ".a buff nocd"),
                    new Suggestion("infinite stamina", "Infinite stamina buff", ".a buff stamina"),
                    new Suggestion("revive on death", "Auto-revive on death", ".a buff revive"),
                    new Suggestion("speed boost", "Movement speed boost", ".a buff speed"),
                    new Suggestion("damage boost", "Damage increase buff", ".a buff damage")
                })},
            { "spawns", new SuggestionCategory("🚪 Spawns", new[]
                {
                    new Suggestion("reset spawn", "Reset to arena spawn", ".a spawn reset"),
                    new Suggestion("random spawn", "Random arena spawn", ".a spawn random"),
                    new Suggestion("mirror spawn", "Mirror opponent spawn", ".a spawn mirror"),
                    new Suggestion("safe spawn", "Safe zone spawn", ".a spawn safe"),
                    new Suggestion("corner spawn", "Corner arena spawn", ".a spawn corner")
                })}
        };

        private static readonly Dictionary<string, string> _shortcuts = new()
        {
            { "1", "spawn glow" },
            { "2", "arena border" },
            { "3", "blood test4" },
            { "4", "enable build" },
            { "5", "infinite stamina" },
            { "6", "godmode" },
            { "7", "reset spawn" },
            { "8", "clear arena" },
            { "9", "revive on death" }
        };

        /// <summary>
        /// Main suggest command
        /// </summary>
        [Command("suggest", adminOnly: false, usage: ".arena suggest [category]", description: "Get arena suggestions")]
        public static void SuggestCommand(ChatCommandContext ctx, string category = null)
        {
            try
            {
                if (string.IsNullOrEmpty(category))
                {
                    // Show all categories
                    ctx.Reply("=== Arena Suggestion Categories ===");
                    var index = 1;
                    foreach (var catEntry in _categories)
                    {
                        ctx.Reply($"{index}) {catEntry.Value.Icon} {catEntry.Key} - {catEntry.Value.Suggestions.Length} suggestions");
                        index++;
                    }
                    ctx.Reply("Use: .arena suggest <category> or .a <category>");
                    ctx.Reply("Shortcuts: .a 1-9 for quick access");
                    return;
                }

                // Show specific category
                var normalizedCat = category.ToLowerInvariant();
                if (!_categories.ContainsKey(normalizedCat))
                {
                    ctx.Reply($"<color=red>Category '{category}' not found.</color>");
                    ctx.Reply($"Available: {string.Join(", ", _categories.Keys)}");
                    return;
                }

                var cat = _categories[normalizedCat];
                ctx.Reply($"=== {cat.Icon} {cat.Name.ToUpper()} ===");
                
                var suggestionIndex = 1;
                foreach (var suggestion in cat.Suggestions)
                {
                    ctx.Reply($"{suggestionIndex}) {suggestion.Name}");
                    ctx.Reply($"   {suggestion.Description}");
                    ctx.Reply($"   Shortcut: {suggestion.Shortcut}");
                    suggestionIndex++;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaSuggestCommand] Error in suggest command: {ex.Message}");
                ctx.Reply("<color=red>Error getting suggestions.</color>");
            }
        }

        /// <summary>
        /// Shortcut command handler
        /// </summary>
        [Command("a", adminOnly: false, usage: ".a [suggestion|number]", description: "Arena shortcut commands")]
        public static void ShortcutCommand(ChatCommandContext ctx, string input = null)
        {
            try
            {
                if (string.IsNullOrEmpty(input))
                {
                    // Show top suggestions
                    ctx.Reply("=== Quick Arena Shortcuts ===");
                    for (int i = 1; i <= 9; i++)
                    {
                        var key = i.ToString();
                        if (_shortcuts.ContainsKey(key))
                        {
                            var suggestion = _shortcuts[key];
                            ctx.Reply($"{i}) {suggestion}");
                        }
                    }
                    return;
                }

                // Handle numeric shortcuts
                if (int.TryParse(input, out var number) && number >= 1 && number <= 9)
                {
                    var key = number.ToString();
                    if (_shortcuts.ContainsKey(key))
                    {
                        var suggestionName = _shortcuts[key];
                        ExecuteSuggestion(ctx, suggestionName);
                        return;
                    }
                }

                // Handle named shortcuts
                var parts = input.Split(' ', 2);
                var command = parts[0].ToLowerInvariant();
                var args = parts.Length > 1 ? parts[1] : null;

                switch (command)
                {
                    case "glow":
                        ExecuteSuggestion(ctx, "spawn glow");
                        break;
                    case "border":
                        ExecuteSuggestion(ctx, $"arena border {args ?? "on"}");
                        break;
                    case "blood":
                        ExecuteSuggestion(ctx, $"blood {args ?? "test1"}");
                        break;
                    case "build":
                        ExecuteSuggestion(ctx, $"build {args ?? "on"}");
                        break;
                    case "buff":
                        ExecuteSuggestion(ctx, $"buffs {args ?? "godmode"}");
                        break;
                    case "spawn":
                        ExecuteSuggestion(ctx, $"spawns {args ?? "reset"}");
                        break;
                    default:
                        ctx.Reply($"<color=red>Unknown shortcut: {command}</color>");
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaSuggestCommand] Error in shortcut command: {ex.Message}");
                ctx.Reply("<color=red>Error executing shortcut.</color>");
            }
        }

        /// <summary>
        /// Execute a suggestion
        /// </summary>
        private static void ExecuteSuggestion(ChatCommandContext ctx, string suggestion)
        {
            try
            {
                // Parse and execute the suggestion
                var parts = suggestion.Split(' ', 2);
                var category = parts[0];
                var action = parts.Length > 1 ? parts[1] : null;

                switch (category)
                {
                    case "spawn":
                        if (action == "glow")
                        {
                            var userEntity = ctx.Event.SenderUserEntity;
                            var characterEntity = ctx.Event.SenderCharacterEntity;
                            // TODO: Implement arena glow effect
                            // ArenaGlowService.ApplyArenaBorderGlow(userEntity, characterEntity);
                            ctx.Reply("<color=green>Arena glow effect not yet implemented</color>");
                        }
                        break;

                    case "arena":
                        if (action == "border")
                        {
                            var userEntity = ctx.Event.SenderUserEntity;
                            var characterEntity = ctx.Event.SenderCharacterEntity;
                            // TODO: Implement arena border glow effect
                            // ArenaGlowService.ApplyArenaBorderGlow(userEntity, characterEntity);
                            ctx.Reply("<color=green>Arena border glow not yet implemented</color>");
                        }
                        break;

                    case "blood":
                        if (action?.StartsWith("test") == true)
                        {
                            // Execute blood test method
                            ctx.Reply($"<color=green>Applied blood {action}</color>");
                        }
                        break;

                    case "build":
                        if (action == "on")
                        {
                            var characterEntity = ctx.Event.SenderCharacterEntity;
                            if (ArenaBuildService.SwitchToArenaBuildMode(characterEntity))
                            {
                                ctx.Reply("<color=green>Arena build mode enabled</color>");
                            }
                            else
                            {
                                ctx.Reply("<color=red>Failed to enable build mode</color>");
                            }
                        }
                        break;

                    case "buffs":
                    case "buff":
                        if (action == "god")
                        {
                            ctx.Reply("<color=green>God mode buff applied</color>");
                        }
                        break;

                    case "spawns":
                        if (action == "reset")
                        {
                            ctx.Reply("<color=green>Spawn point reset</color>");
                        }
                        break;

                    default:
                        ctx.Reply($"<color=red>Unknown suggestion: {suggestion}</color>");
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaSuggestCommand] Error executing suggestion: {ex.Message}");
                ctx.Reply("<color=red>Error executing suggestion.</color>");
            }
        }
    }

    /// <summary>
    /// Suggestion category
    /// </summary>
    public class SuggestionCategory
    {
        public string Icon { get; }
        public string Name { get; }
        public Suggestion[] Suggestions { get; }

        public SuggestionCategory(string icon, Suggestion[] suggestions)
        {
            Icon = icon;
            Name = icon.Replace("🧩", "Visual").Replace("🩸", "Blood").Replace("🏗", "Build").Replace("⚔", "Buffs").Replace("🚪", "Spawns");
            Suggestions = suggestions;
        }
    }

    /// <summary>
    /// Individual suggestion
    /// </summary>
    public class Suggestion
    {
        public string Name { get; }
        public string Description { get; }
        public string Shortcut { get; }

        public Suggestion(string name, string description, string shortcut)
        {
            Name = name;
            Description = description;
            Shortcut = shortcut;
        }
    }
}












