using System;
using System.Collections.Generic;
using System.Linq;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VampireCommandFramework;
using VAuto.Core;
using VAuto.Services;
using static VAuto.Core.MissingTypes;

namespace VAuto.Commands.Utilities
{
    /// <summary>
    /// Utility Commands - Enhanced utility functions for players and admins
    /// </summary>
    public static class UtilityCommands
    {
        #region Time and Date Commands
        [Command("time", adminOnly: false, usage: "time [action]", description: "Time and date utilities")]
        public static void TimeCommand(ChatCommandContext ctx, string action = "now")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "now":
                        var now = DateTime.UtcNow;
                        ctx.Reply($"🕐 Server Time (UTC): {now:yyyy-MM-dd HH:mm:ss}");
                        ctx.Reply($"🌍 Your Local Time: {now.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
                        break;
                    
                    case "uptime":
                        var uptime = DateTime.UtcNow - ProcessStartTime;
                        ctx.Reply($"⏱️ Server Uptime: {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s");
                        break;
                    
                    case "zone":
                        var localZone = TimeZoneInfo.Local;
                        ctx.Reply($"🌍 Time Zone: {localZone.DisplayName}");
                        ctx.Reply($"⏰ UTC Offset: {localZone.BaseUtcOffset}");
                        break;
                    
                    default:
                        ctx.Reply("Time commands:");
                        ctx.Reply("  .time now - Current time");
                        ctx.Reply("  .time uptime - Server uptime");
                        ctx.Reply("  .time zone - Time zone info");
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in time command: {ex.Message}");
                ctx.Reply("Error getting time information.");
            }
        }
        #endregion

        #region Weather and Environment Commands
        [Command("weather", adminOnly: false, usage: "weather [action]", description: "Weather and environment info")]
        public static void WeatherCommand(ChatCommandContext ctx, string action = "current")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "current":
                        var character = ctx.Event.SenderCharacterEntity;
                        var position = GetEntityPosition(character);
                        var weather = GetWeatherAtPosition(position);
                        
                        ctx.Reply($"🌤️ Current Weather: {weather.Condition}");
                        ctx.Reply($"🌡️ Temperature: {weather.Temperature}°C");
                        ctx.Reply($"💧 Humidity: {weather.Humidity}%");
                        ctx.Reply($"🌬️ Wind: {weather.WindSpeed} km/h {weather.WindDirection}");
                        break;
                    
                    case "forecast":
                        ctx.Reply("🌤️ Weather Forecast:");
                        ctx.Reply("  Next hour: Clear skies");
                        ctx.Reply("  Next 3 hours: Light rain");
                        ctx.Reply("  Next 6 hours: Overcast");
                        ctx.Reply("  Next 3 hours: Light rain");
                        ctx.Reply("  Next 6 hours: Overcast");
                        break;
                    
                    case "set":
                        if (!ctx.Event.User.IsAdmin)
                        {
                            ctx.Reply("Admin access required.");
                            return;
                        }
                        ctx.Reply("Weather control not yet implemented.");
                        break;
                    
                    default:
                        ctx.Reply("Weather commands:");
                        ctx.Reply("  .weather current - Current weather");
                        ctx.Reply("  .weather forecast - Weather forecast");
                        ctx.Reply("  .weather set [condition] - Set weather (admin)");
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in weather command: {ex.Message}");
                ctx.Reply("Error getting weather information.");
            }
        }
        #endregion

        #region Calculator Commands
        [Command("calc", adminOnly: false, usage: "calc <expression>", description: "Basic calculator")]
        public static void CalculatorCommand(ChatCommandContext ctx, string expression)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(expression))
                {
                    ctx.Reply("Usage: calc <expression>");
                    ctx.Reply("Examples:");
                    ctx.Reply("  calc 2+2");
                    ctx.Reply("  calc 10*5");
                    ctx.Reply("  calc 100/4");
                    return;
                }

                // Basic math evaluation (safe only)
                var result = EvaluateExpression(expression);
                ctx.Reply($"🧮 Result: {expression} = {result}");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in calc command: {ex.Message}");
                ctx.Reply("Error evaluating expression. Use basic arithmetic only.");
            }
        }

        [Command("convert", adminOnly: false, usage: "convert <value> <from> <to>", description: "Unit conversion")]
        public static void ConvertCommand(ChatCommandContext ctx, double value, string from, string to)
        {
            try
            {
                var result = ConvertUnits(value, from.ToLower(), to.ToLower());
                if (result.HasValue)
                {
                    ctx.Reply($"🔄 Conversion: {value} {from} = {result.Value} {to}");
                }
                else
                {
                    ctx.Reply($"❌ Unsupported conversion: {from} to {to}");
                    ctx.Reply("Supported: meters/feet, celsius/fahrenheit, kg/lbs");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in convert command: {ex.Message}");
                ctx.Reply("Error performing conversion.");
            }
        }
        #endregion

        #region Random Commands
        [Command("random", adminOnly: false, usage: "random [min] [max]", description: "Random number generator")]
        public static void RandomCommand(ChatCommandContext ctx, int min = 0, int max = 100)
        {
            try
            {
                var random = new System.Random();
                var result = random.Next(min, max + 1);
                ctx.Reply($"🎲 Random number between {min} and {max}: {result}");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in random command: {ex.Message}");
                ctx.Reply("Error generating random number.");
            }
        }

        [Command("coin", adminOnly: false, usage: ".coin", description: "Flip a coin")]
        public static void CoinCommand(ChatCommandContext ctx)
        {
            try
            {
                var random = new System.Random();
                var result = random.Next(2) == 0 ? "Heads" : "Tails";
                ctx.Reply($"🪙 Coin flip: {result}!");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in coin command: {ex.Message}");
                ctx.Reply("Error flipping coin.");
            }
        }

        [Command("roll", adminOnly: false, usage: "roll [dice]", description: "Roll dice (e.g., 2d6)")]
        public static void RollCommand(ChatCommandContext ctx, string dice = "1d6")
        {
            try
            {
                var rolls = ParseDiceNotation(dice);
                if (rolls == null)
                {
                    ctx.Reply("Invalid dice notation. Use format: NdM (e.g., 2d6, 1d20)");
                    return;
                }

                var random = new System.Random();
                var results = new List<int>();
                var total = 0;

                for (int i = 0; i < rolls.Value.count; i++)
                {
                    var roll = random.Next(1, rolls.Value.sides + 1);
                    results.Add(roll);
                    total += roll;
                }

                ctx.Reply($"🎲 {dice}: {string.Join(", ", results)} = {total}");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in roll command: {ex.Message}");
                ctx.Reply("Error rolling dice.");
            }
        }
        #endregion

        #region Color Commands
        [Command("color", adminOnly: false, usage: "color [action] [args]", description: "Color utilities")]
        public static void ColorCommand(ChatCommandContext ctx, string action = "palette", string args = "")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "palette":
                        ShowColorPalette(ctx);
                        break;
                    
                    case "hex":
                        if (string.IsNullOrEmpty(args))
                        {
                            ctx.Reply("Usage: color hex <color_name>");
                            return;
                        }
                        var hex = GetColorHex(args.ToLower());
                        if (!string.IsNullOrEmpty(hex))
                        {
                            ctx.Reply($"🎨 {args}: {hex}");
                        }
                        else
                        {
                            ctx.Reply($"Color '{args}' not found. Use color palette to see available colors.");
                        }
                        break;
                    
                    case "random":
                        var randomColor = GetRandomColor();
                        ctx.Reply($"🎨 Random color: {randomColor.Name} ({randomColor.Hex})");
                        break;
                    
                    default:
                        ctx.Reply("Color commands:");
                        ctx.Reply("  .color palette - Show color palette");
                        ctx.Reply("  .color hex <color> - Get hex code for color");
                        ctx.Reply("  .color random - Get random color");
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in color command: {ex.Message}");
                ctx.Reply("Error with color utilities.");
            }
        }
        #endregion

        #region Text Utilities
        [Command("text", adminOnly: false, usage: "text <action> [args]", description: "Text processing utilities")]
        public static void TextCommand(ChatCommandContext ctx, string action = "help", string args = "")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "reverse":
                        if (string.IsNullOrEmpty(args))
                        {
                            ctx.Reply("Usage: text reverse <text>");
                            return;
                        }
                        var reversed = new string(args.Reverse().ToArray());
                        ctx.Reply($"🔄 Reversed: {reversed}");
                        break;
                    
                    case "upper":
                        if (string.IsNullOrEmpty(args))
                        {
                            ctx.Reply("Usage: text upper <text>");
                            return;
                        }
                        ctx.Reply($"🔤 Uppercase: {args.ToUpper()}");
                        break;
                    
                    case "lower":
                        if (string.IsNullOrEmpty(args))
                        {
                            ctx.Reply("Usage: text lower <text>");
                            return;
                        }
                        ctx.Reply($"🔡 Lowercase: {args.ToLower()}");
                        break;
                    
                    case "length":
                        if (string.IsNullOrEmpty(args))
                        {
                            ctx.Reply("Usage: text length <text>");
                            return;
                        }
                        ctx.Reply($"📏 Length: {args.Length} characters");
                        break;
                    
                    case "count":
                        var words = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        ctx.Reply($"📊 Words: {words.Length}");
                        ctx.Reply($"📊 Characters: {args.Length}");
                        break;
                    
                    default:
                        ctx.Reply("Text utilities:");
                        ctx.Reply("  .text reverse <text> - Reverse text");
                        ctx.Reply("  .text upper <text> - Convert to uppercase");
                        ctx.Reply("  .text lower <text> - Convert to lowercase");
                        ctx.Reply("  .text length <text> - Get text length");
                        ctx.Reply("  .text count <text> - Count words and characters");
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in text command: {ex.Message}");
                ctx.Reply("Error with text utilities.");
            }
        }
        #endregion

        #region Server Info Commands
        [Command("server", adminOnly: false, usage: ".server [action]", description: "Server information")]
        public static void ServerCommand(ChatCommandContext ctx, string action = "info")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "info":
                        ShowServerInfo(ctx);
                        break;
                    
                    case "players":
                        ShowPlayerCount(ctx);
                        break;
                    
                    case "performance":
                        if (!ctx.Event.User.IsAdmin)
                        {
                            ctx.Reply("Admin access required.");
                            return;
                        }
                        ShowPerformanceInfo(ctx);
                        break;
                    
                    case "version":
                        ShowVersionInfo(ctx);
                        break;
                    
                    default:
                        ctx.Reply("Server commands:");
                        ctx.Reply("  .server info - Server information");
                        ctx.Reply("  .server players - Player count");
                        ctx.Reply("  .server performance - Performance stats (admin)");
                        ctx.Reply("  .server version - Version information");
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in server command: {ex.Message}");
                ctx.Reply("Error getting server information.");
            }
        }
        #endregion

        #region Helper Methods
        private static DateTime ProcessStartTime { get; } = DateTime.UtcNow;

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

        private static double EvaluateExpression(string expression)
        {
            // Basic safe evaluation - only supports basic arithmetic
            expression = expression.Replace(" ", "");
            
            // Simple validation to prevent code injection
            if (!System.Text.RegularExpressions.Regex.IsMatch(expression, @"^[0-9+\-*/().\s]+$"))
            {
                throw new ArgumentException("Invalid expression");
            }

            // Use DataTable for safe evaluation
            var table = new System.Data.DataTable();
            return Convert.ToDouble(table.Compute(expression, string.Empty));
        }

        private static double? ConvertUnits(double value, string from, string to)
        {
            // Temperature conversions
            if ((from == "celsius" && to == "fahrenheit") || (from == "c" && to == "f"))
            {
                return (value * 9/5) + 32;
            }
            if ((from == "fahrenheit" && to == "celsius") || (from == "f" && to == "c"))
            {
                return (value - 32) * 5/9;
            }

            // Length conversions
            if ((from == "meters" && to == "feet") || (from == "m" && to == "ft"))
            {
                return value * 3.28084;
            }
            if ((from == "feet" && to == "meters") || (from == "ft" && to == "m"))
            {
                return value / 3.28084;
            }

            // Weight conversions
            if ((from == "kg" && to == "lbs") || (from == "kilograms" && to == "pounds"))
            {
                return value * 2.20462;
            }
            if ((from == "lbs" && to == "kg") || (from == "pounds" && to == "kilograms"))
            {
                return value / 2.20462;
            }

            return null;
        }

        private static (int count, int sides)? ParseDiceNotation(string notation)
        {
            try
            {
                var parts = notation.ToLower().Split('d');
                if (parts.Length != 2) return null;

                if (!int.TryParse(parts[0], out int count)) return null;
                if (!int.TryParse(parts[1], out int sides)) return null;

                if (count < 1 || count > 20 || sides < 2 || sides > 100) return null;

                return (count, sides);
            }
            catch
            {
                return null;
            }
        }

        private static WeatherInfo GetWeatherAtPosition(float3 position)
        {
            // Simulated weather based on position
            var hash = (int)(position.x * position.z) % 100;
            var conditions = new[] { "Clear", "Cloudy", "Rain", "Storm", "Fog", "Snow" };
            var temperatures = new[] { -5, 0, 5, 10, 15, 20, 25, 30 };
            var humidity = 30 + (hash % 50);
            var windSpeed = 5 + (hash % 20);
            var windDirections = new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

            return new WeatherInfo
            {
                Condition = conditions[hash % conditions.Length],
                Temperature = temperatures[hash % temperatures.Length],
                Humidity = humidity,
                WindSpeed = windSpeed,
                WindDirection = windDirections[hash % windDirections.Length]
            };
        }

        private static ColorInfo GetRandomColor()
        {
            var random = new System.Random();
            var colors = GetColorPalette();
            return colors[random.Next(colors.Count)];
        }

        private static string GetColorHex(string colorName)
        {
            var colors = GetColorPalette();
            var color = colors.FirstOrDefault(c => c.Name.Equals(colorName, StringComparison.OrdinalIgnoreCase));
            return color?.Hex;
        }

        private static List<ColorInfo> GetColorPalette()
        {
            return new List<ColorInfo>
            {
                new ColorInfo { Name = "red", Hex = "#FF0000" },
                new ColorInfo { Name = "green", Hex = "#00FF00" },
                new ColorInfo { Name = "blue", Hex = "#0000FF" },
                new ColorInfo { Name = "yellow", Hex = "#FFFF00" },
                new ColorInfo { Name = "orange", Hex = "#FFA500" },
                new ColorInfo { Name = "purple", Hex = "#800080" },
                new ColorInfo { Name = "pink", Hex = "#FFC0CB" },
                new ColorInfo { Name = "brown", Hex = "#A52A2A" },
                new ColorInfo { Name = "black", Hex = "#000000" },
                new ColorInfo { Name = "white", Hex = "#FFFFFF" },
                new ColorInfo { Name = "gray", Hex = "#808080" }
            };
        }

        private static void ShowColorPalette(ChatCommandContext ctx)
        {
            ctx.Reply("🎨 Available Colors:");
            var colors = GetColorPalette();
            foreach (var color in colors)
            {
                ctx.Reply($"  {color.Name}: {color.Hex}");
            }
        }

        private static void ShowServerInfo(ChatCommandContext ctx)
        {
            ctx.Reply("🖥️ Server Information:");
            ctx.Reply($"  Server Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            ctx.Reply($"  Uptime: {DateTime.UtcNow - ProcessStartTime:dd\\d hh\\h mm\\m}");
            
            try
            {
                var playerCount = VAuto.Services.PlayerService.GetOnlinePlayerCount();
                ctx.Reply($"  Online Players: {playerCount}");
            }
            catch
            {
                ctx.Reply($"  Online Players: Unknown");
            }
        }

        private static void ShowPlayerCount(ChatCommandContext ctx)
        {
            try
            {
                var players = VAuto.Services.PlayerService.GetAllOnlinePlayers();
                ctx.Reply($"👥 Current Players ({players.Count}):");
                
                foreach (var player in players.Take(10))
                {
                    var status = VAuto.Services.Systems.GameSystems.IsPlayerInArena(player.PlatformId) ? "[Arena]" : "[Normal]";
                    ctx.Reply($"  {player.CharacterName} {status}");
                }
                
                if (players.Count > 10)
                {
                    ctx.Reply($"  ... and {players.Count - 10} more players");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error getting player count: {ex.Message}");
                ctx.Reply("Error getting player count.");
            }
        }

        private static void ShowPerformanceInfo(ChatCommandContext ctx)
        {
            ctx.Reply("📈 Performance Information:");
            ctx.Reply($"  Memory Usage: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
            ctx.Reply($"  GC Collections: {GC.CollectionCount(0)} (Gen 0), {GC.CollectionCount(1)} (Gen 1), {GC.CollectionCount(2)} (Gen 2)");
            
            try
            {
                var em = VRCore.EM;
                var entityCount = em.UniversalQuery.CalculateEntityCount();
                var chunkCount = em.UniversalQuery.CalculateChunkCount();
                ctx.Reply($"  Entity Count: {entityCount}");
                ctx.Reply($"  Entity Chunks: {chunkCount}");
            }
            catch
            {
                ctx.Reply($"  Entity Count: Unknown");
            }
        }

        private static void ShowVersionInfo(ChatCommandContext ctx)
        {
            ctx.Reply("ℹ️ Version Information:");
        }

        private static void ReplyHelper(string message)
        {
            // Helper method to send messages - would use proper chat context
            Plugin.Logger.LogInfo($"[UtilityCommands] {message}");
        }
        #endregion

        #region Data Structures
        private class WeatherInfo
        {
            public string Condition { get; set; }
            public int Temperature { get; set; }
            public int Humidity { get; set; }
            public int WindSpeed { get; set; }
            public string WindDirection { get; set; }
        }

        private class ColorInfo
        {
            public string Name { get; set; }
            public string Hex { get; set; }
        }
        #endregion
    }
}