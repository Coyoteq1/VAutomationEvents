using System;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;

namespace VAuto.UI
{
    public static class UISettings
    {
        private static ConfigFile _config;
        private static Dictionary<string, ConfigEntry<string>> _colorSettings = new();
        private static ConfigEntry<float> _scaleEntry;
        private static ConfigEntry<string> _themeEntry;

        public static void Initialize(ConfigFile config)
        {
            _config = config;

            // Scale setting
            _scaleEntry = config.Bind("UI", "Scale", 1.0f, "UI scale factor (0.8-1.5)");

            // Theme setting
            _themeEntry = config.Bind("UI", "Theme", "Dark", "UI color theme (Dark, Light, Blue, Red, Green)");

            // Color settings
            _colorSettings["Background"] = config.Bind("UI.Colors", "Background", "#1A1A1A", "Background color");
            _colorSettings["Panel"] = config.Bind("UI.Colors", "Panel", "#2A2A2A", "Panel color");
            _colorSettings["Button"] = config.Bind("UI.Colors", "Button", "#3A3A3A", "Button color");
            _colorSettings["ButtonHover"] = config.Bind("UI.Colors", "ButtonHover", "#4A4A4A", "Button hover color");
            _colorSettings["ButtonPressed"] = config.Bind("UI.Colors", "ButtonPressed", "#252525", "Button pressed color");
            _colorSettings["Text"] = config.Bind("UI.Colors", "Text", "#FFFFFF", "Text color");
            _colorSettings["Accent"] = config.Bind("UI.Colors", "Accent", "#4A90E2", "Accent color");
            _colorSettings["Success"] = config.Bind("UI.Colors", "Success", "#4CAF50", "Success color");
            _colorSettings["Warning"] = config.Bind("UI.Colors", "Warning", "#FF9800", "Warning color");
            _colorSettings["Error"] = config.Bind("UI.Colors", "Error", "#F44336", "Error color");

            ApplyTheme(_themeEntry.Value);
        }

        public static float Scale
        {
            get => _scaleEntry?.Value ?? 1.0f;
            set
            {
                if (_scaleEntry != null)
                {
                    _scaleEntry.Value = Mathf.Clamp(value, 0.8f, 1.5f);
                    _config?.Save();
                }
            }
        }

        public static string Theme
        {
            get => _themeEntry?.Value ?? "Dark";
            set
            {
                if (_themeEntry != null)
                {
                    _themeEntry.Value = value;
                    ApplyTheme(value);
                    _config?.Save();
                }
            }
        }

        public static Color GetColor(string key)
        {
            if (_colorSettings.TryGetValue(key, out var entry))
            {
                return HexToColor(entry.Value);
            }
            return Color.white;
        }

        public static void SetColor(string key, Color color)
        {
            if (_colorSettings.TryGetValue(key, out var entry))
            {
                entry.Value = ColorToHex(color);
                _config?.Save();
            }
        }

        private static void ApplyTheme(string theme)
        {
            switch (theme.ToLower())
            {
                case "dark":
                    SetThemeColors("#1A1A1A", "#2A2A2A", "#3A3A3A", "#4A4A4A", "#252525", "#FFFFFF", "#4A90E2");
                    break;
                case "light":
                    SetThemeColors("#F5F5F5", "#FFFFFF", "#E0E0E0", "#D0D0D0", "#C0C0C0", "#000000", "#2196F3");
                    break;
                case "blue":
                    SetThemeColors("#0D1B2A", "#1B263B", "#415A77", "#778DA9", "#0D1B2A", "#E0E1DD", "#4A90E2");
                    break;
                case "red":
                    SetThemeColors("#1A0A0A", "#2A1515", "#3A2020", "#4A2A2A", "#251010", "#FFFFFF", "#E74C3C");
                    break;
                case "green":
                    SetThemeColors("#0A1A0A", "#152A15", "#203A20", "#2A4A2A", "#102510", "#FFFFFF", "#4CAF50");
                    break;
            }
        }

        private static void SetThemeColors(string bg, string panel, string btn, string btnHover, string btnPressed, string text, string accent)
        {
            SetColor("Background", HexToColor(bg));
            SetColor("Panel", HexToColor(panel));
            SetColor("Button", HexToColor(btn));
            SetColor("ButtonHover", HexToColor(btnHover));
            SetColor("ButtonPressed", HexToColor(btnPressed));
            SetColor("Text", HexToColor(text));
            SetColor("Accent", HexToColor(accent));
        }

        private static Color HexToColor(string hex)
        {
            hex = hex.Replace("#", "");
            if (hex.Length == 6)
            {
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                return new Color32(r, g, b, 255);
            }
            return Color.white;
        }

        private static string ColorToHex(Color color)
        {
            Color32 c = color;
            return $"#{c.r:X2}{c.g:X2}{c.b:X2}";
        }

        public static ColorScheme GetColorScheme()
        {
            return new ColorScheme
            {
                Background = GetColor("Background"),
                Panel = GetColor("Panel"),
                Button = GetColor("Button"),
                ButtonHover = GetColor("ButtonHover"),
                ButtonPressed = GetColor("ButtonPressed"),
                Text = GetColor("Text"),
                Accent = GetColor("Accent"),
                Success = GetColor("Success"),
                Warning = GetColor("Warning"),
                Error = GetColor("Error")
            };
        }
    }

    public class ColorScheme
    {
        public Color Background { get; set; }
        public Color Panel { get; set; }
        public Color Button { get; set; }
        public Color ButtonHover { get; set; }
        public Color ButtonPressed { get; set; }
        public Color Text { get; set; }
        public Color Accent { get; set; }
        public Color Success { get; set; }
        public Color Warning { get; set; }
        public Color Error { get; set; }
    }
}
