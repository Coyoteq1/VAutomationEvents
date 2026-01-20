using System;
using System.Reflection;
using VampireCommandFramework;
using BepInEx.Logging;

namespace VAuto.Commands
{
    /// <summary>
    /// Central command registry for VAuto
    /// Handles registration of all command modules with VampireCommandFramework
    /// </summary>
    public static class CommandRegistry
    {
        private static ManualLogSource _log => Plugin.Logger;
        private static bool _registered = false;

        /// <summary>
        /// Register all commands with VampireCommandFramework
        /// VCF uses reflection to auto-discover [Command] attributes, so this mainly logs registration
        /// </summary>
        public static void RegisterAll()
        {
            if (_registered) return;

            try
            {
                _log?.LogInfo("[CommandRegistry] Registering all VAuto commands...");

                // VampireCommandFramework auto-discovers commands via reflection
                // We just need to ensure all command classes are loaded
                var assembly = Assembly.GetExecutingAssembly();
                var commandTypes = assembly.GetTypes();

                int commandCount = 0;
                foreach (var type in commandTypes)
                {
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                    foreach (var method in methods)
                    {
                        var cmdAttr = method.GetCustomAttribute<CommandAttribute>();
                        if (cmdAttr != null)
                        {
                            commandCount++;
                            _log?.LogDebug($"[CommandRegistry] Found command: .{cmdAttr.Name}");
                        }
                    }
                }

                _log?.LogInfo($"[CommandRegistry] Registered {commandCount} commands successfully");
                _registered = true;
            }
            catch (Exception ex)
            {
                _log?.LogError($"[CommandRegistry] Failed to register commands: {ex.Message}");
                throw;
            }
        }
    }
}
