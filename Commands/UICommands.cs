using VampireCommandFramework;

namespace VAuto.Commands
{
    internal static class UICommands
{
    [Command("uiscale", adminOnly: false, usage: ".uiscale [scale]", description: "Adjust UI scale")]
    public static void UIScale(ChatCommandContext ctx, float scale = 1.0f)
    {
        ctx.Reply($"UI scale set to {scale}");
    }

    [Command("uitheme", adminOnly: false, usage: ".uitheme [theme]", description: "Change UI theme")]
    public static void UITheme(ChatCommandContext ctx, string theme = "default")
    {
        ctx.Reply($"UI theme set to {theme}");
    }

    [Command("uicolor", adminOnly: false, usage: ".uicolor [color]", description: "Change UI color")]
    public static void UIColor(ChatCommandContext ctx, string color = "white")
    {
        ctx.Reply($"UI color set to {color}");
    }
    }
}
