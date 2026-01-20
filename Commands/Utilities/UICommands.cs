using VampireCommandFramework;

namespace VAuto.Commands.Utilities
{
    public static class UICommands
    {
        [Command("ui", adminOnly: false, usage: ".ui [abilities|spellbook|menu|close]")]
        public static void UICommand(ChatCommandContext ctx, string menu = "")
        {
            if (string.IsNullOrEmpty(menu))
            {
                ctx.Reply("Usage: .ui [abilities|spellbook|menu|close]");
                ctx.Reply("  abilities - Open abilities menu");
                ctx.Reply("  spellbook - Open spellbook");
                ctx.Reply("  menu - Open main menu");
                ctx.Reply("  close - Close all UI");
                return;
            }

            switch (menu.ToLower())
            {
                case "abilities":
                case "spellbook":
                    ctx.Reply("Opening abilities menu...");
                    ctx.Reply("Press 'K' to open spellbook in-game");
                    break;

                case "menu":
                    ctx.Reply("Opening main menu...");
                    ctx.Reply("Press 'ESC' to open menu in-game");
                    break;

                case "close":
                    ctx.Reply("Closing all UI...");
                    ctx.Reply("Press 'ESC' to close menus in-game");
                    break;

                default:
                    ctx.Reply("Invalid menu. Use: abilities, spellbook, menu, or close");
                    break;
            }
        }
    }
}
