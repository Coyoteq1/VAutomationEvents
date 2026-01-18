using Microsoft.Extensions.Logging;
using VAutomationEvents.Services.Interfaces;

namespace VAutomationEvents.Commands.Arena
{
    /// <summary>
    /// Arena management commands - simplified and enhanced for user ease of use
    /// </summary>
    public class ArenaCommands
    {
        private readonly ILogger<ArenaCommands> _logger;
        private readonly IArenaService _arenaService;

        public ArenaCommands(ILogger<ArenaCommands> logger, IArenaService arenaService)
        {
            _logger = logger;
            _arenaService = arenaService;
        }

        /// <summary>
        /// Enter the arena zone - automatically handles all setup
        /// Usage: .arena enter
        /// </summary>
        [Command("arena enter", "Enters the arena zone with automatic setup")]
        public async Task<string> EnterArena(CommandContext context)
        {
            try
            {
                _logger.LogInformation("Player {PlayerName} entering arena", context.PlayerName);
                
                var result = await _arenaService.EnterArenaAsync(context.PlayerId);
                
                if (result.Success)
                {
                    return "✅ Successfully entered arena! All systems activated.";
                }
                
                return $"❌ Failed to enter arena: {result.ErrorMessage}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error entering arena for player {PlayerName}", context.PlayerName);
                return "❌ An error occurred while entering the arena.";
            }
        }

        /// <summary>
        /// Exit the arena zone - automatically handles all cleanup
        /// Usage: .arena exit
        /// </summary>
        [Command("arena exit", "Exits the arena zone with automatic cleanup")]
        public async Task<string> ExitArena(CommandContext context)
        {
            try
            {
                _logger.LogInformation("Player {PlayerName} exiting arena", context.PlayerName);
                
                var result = await _arenaService.ExitArenaAsync(context.PlayerId);
                
                if (result.Success)
                {
                    return "✅ Successfully exited arena! All systems restored.";
                }
                
                return $"❌ Failed to exit arena: {result.ErrorMessage}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exiting arena for player {PlayerName}", context.PlayerName);
                return "❌ An error occurred while exiting the arena.";
            }
        }

        /// <summary>
        /// Check arena status - shows current arena state and player information
        /// Usage: .arena status
        /// </summary>
        [Command("arena status", "Shows current arena status and player information")]
        public async Task<string> GetArenaStatus(CommandContext context)
        {
            try
            {
                var status = await _arenaService.GetArenaStatusAsync(context.PlayerId);
                
                if (status.IsInArena)
                {
                    return $"🏟️ **Arena Status**\n" +
                           $"   Status: Inside Arena\n" +
                           $"   Time Entered: {status.EnterTime:HH:mm:ss}\n" +
                           $"   Duration: {status.Duration:mm\\:ss}\n" +
                           $"   Active Players: {status.ActivePlayerCount}";
                }
                else
                {
                    return "🏟️ **Arena Status**\n" +
                           "   Status: Outside Arena\n" +
                           "   Use '.arena enter' to join";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting arena status for player {PlayerName}", context.PlayerName);
                return "❌ Unable to retrieve arena status.";
            }
        }
    }

    /// <summary>
    /// Command context for simplified command handling
    /// </summary>
    public class CommandContext
    {
        public string PlayerId { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// Command attribute for easy command registration
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }

        public CommandAttribute(string name, string description = "")
        {
            Name = name;
            Description = description;
        }
    }
}
