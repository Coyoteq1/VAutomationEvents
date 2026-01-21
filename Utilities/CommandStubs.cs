using System;

namespace VampireCommandFramework
{
    /// <summary>
    /// Command attribute stub for compilation
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class CommandAttribute : Attribute
    {
        public string Command { get; }
        public string Usage { get; }
        public string Description { get; }
        public bool AdminOnly { get; }

        public CommandAttribute(string command, string usage = "", string description = "", bool adminOnly = false)
        {
            Command = command;
            Usage = usage;
            Description = description;
            AdminOnly = adminOnly;
        }
    }

    /// <summary>
    /// Chat command context stub
    /// </summary>
    public class ChatCommandContext
    {
        public string UserName { get; set; } = string.Empty;
        public ulong PlatformId { get; set; }

        public void Reply(string message)
        {
            // Stub implementation
            Plugin.Logger?.LogInfo($"[ChatCommand] Reply to {UserName}: {message}");
        }
    }
}
