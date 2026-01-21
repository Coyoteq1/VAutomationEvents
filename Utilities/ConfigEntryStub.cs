using System;

namespace BepInEx.Configuration
{
    /// <summary>
    /// ConfigEntry stub for compilation
    /// </summary>
    public class ConfigEntry<T>
    {
        public T Value { get; set; }
        public string Key { get; set; }
        public string Section { get; set; }
        public string Description { get; set; }
        public T DefaultValue { get; set; }

        public ConfigEntry(string key, T defaultValue, string description = "")
        {
            Key = key;
            Value = defaultValue;
            Description = description;
            DefaultValue = defaultValue;
        }
    }
}
