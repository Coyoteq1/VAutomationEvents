namespace VAuto
{
    public static class MyPluginInfo
    {
        public const string PLUGIN_GUID = "gg.deca.VAutomation";
        public const string PLUGIN_NAME = "VAutomation";
        public const string PLUGIN_VERSION = "1.0.0";
        
        // Legacy GUIDs for backward compatibility
        public const string LEGACY_ARENA_GUID = "VAuto-Arena";
        public const string LEGACY_CORE_GUID = "VAuto-Core";
        public const string LEGACY_SERVICES_GUID = "VAuto-Services";
        
        // List of individual GUID values
        public static readonly string[] PLUGIN_GUID_LIST = new string[]
        {
            "gg.deca.VAutomation",
            "VAuto-Core", 
            "VAuto-Services"
        };
        
        // Individual CFG file GUIDs for modular configuration
        public const string CORE_CFG_GUID = "VAuto-Core";
        public const string ARENA_CFG_GUID = "VAuto-Arena";
        public const string SERVICES_CFG_GUID = "VAuto-Services";
    }
}