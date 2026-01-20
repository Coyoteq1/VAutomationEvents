using VampireCommandFramework;
using VAuto.UI;
using UnityEngine;

namespace VAuto.Commands.Utilities
{
    public static class BuildingCommands
    {
        [Command("build", adminOnly: true, usage: ".build [tile|glow|toggle|off] [color]")]
        public static void BuildCommand(ChatCommandContext ctx, string mode = "", string colorName = "blue")
        {
            if (string.IsNullOrEmpty(mode))
            {
                ctx.Reply("Usage: .build [tile|glow|toggle|off] [color]");
                ctx.Reply("Colors: red, blue, green, yellow, white, purple, orange");
                return;
            }

            switch (mode.ToLower())
            {
                case "tile":
                    var tileColor = GetColor(colorName);
                    MouseBuildingSystem.Instance.EnableBuildMode(
                        MouseBuildingSystem.BuildType.Tile,
                        -1576592687,
                        $"{colorName} Tile",
                        tileColor
                    );
                    ctx.Reply($"Tile build mode enabled ({colorName}). Left click to place, right click to remove, middle mouse to move.");
                    break;

                case "glow":
                    var color = GetColor(colorName);
                    MouseBuildingSystem.Instance.EnableBuildMode(
                        MouseBuildingSystem.BuildType.Glow,
                        -880131926,
                        $"{colorName} Glow",
                        color
                    );
                    ctx.Reply($"Glow build mode enabled ({colorName}). Left click to place, right click to remove, middle mouse to move.");
                    break;

                case "toggle":
                    MouseBuildingSystem.Instance.ToggleBuildMode();
                    ctx.Reply(MouseBuildingSystem.Instance.IsBuildModeEnabled ? "Build mode enabled" : "Build mode disabled");
                    break;

                case "off":
                    MouseBuildingSystem.Instance.DisableBuildMode();
                    ctx.Reply("Build mode disabled.");
                    break;

                default:
                    ctx.Reply("Invalid mode. Use: tile, glow, toggle, or off");
                    break;
            }
        }

        [Command("buildclear", adminOnly: true, usage: ".buildclear")]
        public static void BuildClearCommand(ChatCommandContext ctx)
        {
            MouseBuildingSystem.Instance.ClearAll();
            ctx.Reply("Cleared all placed objects.");
        }

        private static UnityEngine.Color GetColor(string colorName)
        {
            return colorName.ToLower() switch
            {
                "red" => UnityEngine.Color.red,
                "blue" => UnityEngine.Color.blue,
                "green" => UnityEngine.Color.green,
                "yellow" => UnityEngine.Color.yellow,
                "white" => UnityEngine.Color.white,
                "purple" => new UnityEngine.Color(0.5f, 0, 0.5f),
                "orange" => new UnityEngine.Color(1f, 0.5f, 0),
                "cyan" => UnityEngine.Color.cyan,
                "magenta" => UnityEngine.Color.magenta,
                _ => UnityEngine.Color.blue
            };
        }
    }
}
