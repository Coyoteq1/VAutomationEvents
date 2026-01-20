using VampireCommandFramework;
using VAuto.UI;
using UnityEngine;

namespace VAuto.Commands.Utilities
{
    public static class UIScaleCommands
    {
        [Command("uiscale", adminOnly: false, usage: ".uiscale [small|normal|large|xlarge]")]
        public static void UIScaleCommand(ChatCommandContext ctx, string size = "")
        {
            if (string.IsNullOrEmpty(size))
            {
                ctx.Reply($"Current UI scale: {GetScaleName(UISettings.Scale)}");
                ctx.Reply("Usage: .uiscale [small|normal|large|xlarge]");
                return;
            }

            UISettings.Scale = size.ToLower() switch
            {
                "small" => 0.8f,
                "normal" => 1.0f,
                "large" => 1.2f,
                "xlarge" => 1.5f,
                _ => UISettings.Scale
            };

            ctx.Reply($"UI scale set to: {GetScaleName(UISettings.Scale)}");
        }

        [Command("uitheme", adminOnly: false, usage: ".uitheme [dark|light|blue|red|green]")]
        public static void UIThemeCommand(ChatCommandContext ctx, string theme = "")
        {
            if (string.IsNullOrEmpty(theme))
            {
                ctx.Reply($"Current theme: {UISettings.Theme}");
                ctx.Reply("Available themes: dark, light, blue, red, green");
                return;
            }

            UISettings.Theme = theme;
            ctx.Reply($"UI theme set to: {theme}");
        }

        [Command("uicolor", adminOnly: true, usage: ".uicolor [element] [hex]")]
        public static void UIColorCommand(ChatCommandContext ctx, string element = "", string hex = "")
        {
            if (string.IsNullOrEmpty(element))
            {
                ctx.Reply("Elements: Background, Panel, Button, Text, Accent, Success, Warning, Error");
                return;
            }

            if (string.IsNullOrEmpty(hex))
            {
                var color = UISettings.GetColor(element);
                ctx.Reply($"{element} color: #{ColorUtility.ToHtmlStringRGB(color)}");
                return;
            }

            if (ColorUtility.TryParseHtmlString(hex, out var newColor))
            {
                UISettings.SetColor(element, newColor);
                ctx.Reply($"{element} color set to: {hex}");
            }
            else
            {
                ctx.Reply("Invalid hex color format. Use #RRGGBB");
            }
        }

        private static string GetScaleName(float scale)
        {
            return scale switch
            {
                0.8f => "Small",
                1.0f => "Normal",
                1.2f => "Large",
                1.5f => "XLarge",
                _ => "Custom"
            };
        }
    }
}
