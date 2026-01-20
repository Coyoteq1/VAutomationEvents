# VAuto Configuration System Documentation

## Overview
VAuto uses a unified JSON configuration system that consolidates all plugin settings into manageable files and directories.

## Directory Structure

```
c:\BepInEx\DedicatedServerLauncher\VRisingServer\BepInEx\config\
├── VAuto\
│   ├── Settings.json (main unified configuration)
│   ├── Victor2.json (player snapshot)
│   ├── Victor3.json (player snapshot)
│   ├── Victor4.json (player snapshot)
│   └── Snapchat.json (chat snapshot)
├── VAuto.Arena\
│   ├── builds.json (arena build configurations)
│   ├── zone.json (zone definitions)
│   └── snapshot.json (arena snapshot settings)
└── VAuto_Data\
    ├── KindredExtract\
    ├── CustomUuids\
    ├── Snapshots\
    ├── Schematics\
    └── database.db (SQLite database)
```

## Configuration Files

### Main Settings.json
The primary configuration file that contains all plugin settings:

```json
{
  "version": "1.0.0",
  "lastUpdated": "2026-01-17T09:44:00Z",
  "core": {
    "enable": true,
    "debugMode": true,
    "logLevel": "Debug",
    "pluginVersion": "2.0.0",
    "configVersion": "1.0"
  },
  "arena": {
    "enable": true,
    "center": { "x": -1000.0, "y": 5.0, "z": -500.0 },
    "radius": 50.0,
    "enterRadius": 50.0,
    "exitRadius": 75.0,
    "checkInterval": 3.0,
    "enableGlowEffects": true,
    "defaultGlowIntensity": 1.0,
    "maxGlowsPerArena": 50,
    "enableArenaBuild": true,
    "maxStructuresPerPlayer": 20,
    "buildRange": 10.0,
    "enableArenaObjects": true,
    "maxObjectsPerArena": 100,
    "enableArenaSnapshots": true,
    "maxSnapshotsPerPlayer": 5,
    "enableEntryEffects": true,
    "enableExitEffects": true,
    "enableVBloodHooks": true,
    "enableUnlockOverrides": true,
    "enableArenaCleanup": true,
    "cleanupInterval": 3600,
    "maxInactiveTime": 7200,
    "damageMultiplier": 1.0,
    "healingMultiplier": 1.0,
    "experienceMultiplier": 1.5,
    "environmentTheme": "arena",
    "arenaBuildingEnabled": false,
    "arenaHealingEnabled": false,
    "arenaResourceRespawn": false,
    "arenaRespawnTime": 300
  },
  "services": {
    "enableAutomation": true,
    "enableBuildingAutomation": true,
    "enableHarvestingAutomation": true,
    "enableCombatAutomation": true,
    "enableCraftingAutomation": true,
    "maxConcurrentTasks": 5,
    "automationTickRate": 1.0,
    "taskTimeout": 300,
    "enableIcons": true,
    "refreshInterval": 5.0,
    "showNames": true,
    "enableRespawn": true,
    "cooldown": 30,
    "enableDatabase": true,
    "databaseType": "SQLite",
    "databasePath": "./VAuto_Data/database.db",
    "enableBackup": true,
    "backupInterval": 6,
    "maxBackups": 24,
    "backupPath": "./VAuto_Data/Backups"
  },
  "respawn": {
    "isEnabled": false,
    "defaultCooldownSeconds": 30,
    "allowGlobalToggle": true,
    "maxCooldownSeconds": 300,
    "enablePerPlayerCooldowns": true,
    "playerCooldowns": {}
  }
}
```

### Victor2.json, Victor3.json, Victor4.json
Player snapshot configuration files:

```json
{
  "version": "1.0.0",
  "lastUpdated": "2026-01-17T09:53:00Z",
  "snapshots": [],
  "globalSettings": {
    "autoSave": true,
    "maxSnapshots": 100,
    "cleanupInterval": 3600,
    "compressionEnabled": true
  }
}
```

### Snapchat.json
Chat snapshot configuration:

```json
{
  "version": "1.0.0",
  "lastUpdated": "2026-01-17T09:53:00Z",
  "chatSnapshots": [],
  "globalSettings": {
    "autoSave": true,
    "maxSnapshots": 50,
    "cleanupInterval": 1800,
    "compressionEnabled": true,
    "enableSearch": true,
    "enableFilters": true
  },
  "filters": {
    "enablePlayerFilter": true,
    "enableGuildFilter": true,
    "enableSystemFilter": true,
    "enableCombatFilter": true,
    "enableTradeFilter": true
  },
  "search": {
    "maxHistory": 100,
    "enableRegex": false,
    "caseSensitive": false
  }
}
```

### builds.json
Arena build configurations:

```json
{
  "version": "1.0.0",
  "lastUpdated": "2026-01-17T09:44:00Z",
  "builds": {
    "warrior": {
      "name": "Warrior",
      "description": "Balanced combat build",
      "weapons": ["Sword_Iron_T1", "Shield_Wood_T1"],
      "armor": ["Armor_Leather_T1", "Helmet_Leather_T1"],
      "accessories": ["Ring_Health_T1"],
      "abilities": ["Whirlwind", "Charge"]
    },
    "mage": {
      "name": "Mage",
      "description": "Magic-focused build",
      "weapons": ["Staff_Wood_T1", "Wand_Apprentice_T1"],
      "armor": ["Armor_Cloth_T1", "Robe_Apprentice_T1"],
      "accessories": ["Amulet_Mana_T1", "Ring_Intelligence_T1"],
      "abilities": ["Fireball", "FrostShield", "Teleport"]
    }
  }
}
```

## Plugin API

### Directory Management
```csharp
// Get directory paths
public static string VAutoConfigDir => Path.Combine(ConfigPath, "VAuto");
public static string VAutoDataDir => Path.Combine(ConfigPath, "VAuto", "Data");
public static string VAutoBackupDir => Path.Combine(ConfigPath, "VAuto", "Backups");
public static string VAutoArenaDir => Path.Combine(ConfigPath, "VAuto.Arena");

// Ensure all directories exist
public static void EnsureDirectories()
```

### Configuration File Management
```csharp
// Get all JSON configuration files
public static List<string> GetJsonConfigFiles()

// Load specific JSON configuration
public static T LoadJsonConfig<T>(string fileName) where T : class, new()

// Save configuration to JSON
public static void SaveJsonConfig<T>(string fileName, T config) where T : class

// Get schematic files
public static List<string> GetSchematicFiles()
```

### Accessing Settings
```csharp
// Core settings
public static bool Enable => PluginSettings.GetSettings().Core.Enable;
public static bool DebugMode => PluginSettings.GetSettings().Core.DebugMode;
public static VAuto.Core.LogLevel LogLevel => Enum.Parse<VAuto.Core.LogLevel>(PluginSettings.GetSettings().Core.LogLevel);

// Arena settings
public static bool ZoneEnable => PluginSettings.GetSettings().Arena.Enable;
public static float3 ZoneCenter => PluginSettings.GetSettings().Arena.Center;
public static float ZoneRadius => PluginSettings.GetSettings().Arena.Radius;
public static float ZoneEnterRadius => PluginSettings.GetSettings().Arena.EnterRadius;
public static float ZoneExitRadius => PluginSettings.GetSettings().Arena.ExitRadius;
public static float ZoneCheckInterval => PluginSettings.GetSettings().Arena.CheckInterval;

// Service settings
public static bool RespawnEnable => PluginSettings.GetSettings().Respawn.IsEnabled;
public static int RespawnCooldown => PluginSettings.GetSettings().Respawn.DefaultCooldownSeconds;
public static bool AutomationEnable => PluginSettings.GetSettings().Services.EnableAutomation;
public static float AutomationTickRate => PluginSettings.GetSettings().Services.AutomationTickRate;
public static bool DatabaseEnable => PluginSettings.GetSettings().Services.EnableDatabase;
public static string DatabasePath => PluginSettings.GetSettings().Services.DatabasePath;
```

## Usage Examples

### Loading Configuration
```csharp
// Load main settings
var settings = PluginSettings.GetSettings();
Plugin.Logger?.LogInfo($"Arena enabled: {settings.Arena.Enable}");

// Load specific config file
var victorConfig = Plugin.LoadJsonConfig<VictorConfig>("Victor2.json");
Plugin.Logger?.LogInfo($"Victor2 has {victorConfig.Snapshots.Count} snapshots");

// Save configuration
Plugin.SaveJsonConfig("custom_build.json", newBuildConfig);
```

### Directory Operations
```csharp
// Ensure directories exist
Plugin.EnsureDirectories();

// Get all JSON files
var jsonFiles = Plugin.GetJsonConfigFiles();
foreach (var file in jsonFiles)
{
    Plugin.Logger?.LogInfo($"Found config: {file}");
}

// Get schematic files
var schematics = Plugin.GetSchematicFiles();
Plugin.Logger?.LogInfo($"Found {schematics.Count} schematic files");
```

### Zone Integration
```csharp
// Create zone using WorldAutomationService
var zoneRule = new AutomationRule
{
    TriggerName = "ArenaZone",
    Actions = new List<AutomationAction>
    {
        new AutomationAction
        {
            Type = AutomationActionType.SpawnObject,
            PrefabName = "Castle_Basic_T1_C",
            Position = new float3(-1000, 5, -500),
            ObjectType = WorldObjectType.Structure
        }
    }
};

WorldAutomationService.AddRule("ArenaZone1", zoneRule);
```

## Configuration Classes

### PluginSettings
Static class that manages loading and saving of all configuration files.

### VAutoSettings
Main settings container with properties for:
- CoreSettings (core plugin configuration)
- ArenaSettings (arena system configuration)  
- ServicesSettings (services configuration)
- RespawnSettings (respawn system configuration)

### JSON Converters
- SchematicFloat3Converter - Handles float3 serialization
- AabbConverter - Handles bounding box serialization

## Best Practices

1. **Always use PluginSettings** for accessing configuration rather than direct file access
2. **Call EnsureDirectories()** on plugin startup to guarantee directory structure
3. **Use SaveJsonConfig()** for persisting configuration changes
4. **Handle exceptions** when loading configurations - defaults will be applied automatically
5. **Log configuration operations** for debugging and monitoring

## Migration Notes

This configuration system replaces the old separate CFG files:
- ~~VAuto_Core.cfg~~ → Settings.json (core section)
- ~~VAuto_Arena.cfg~~ → Settings.json (arena section)  
- ~~VAuto_Services.cfg~~ → Settings.json (services section)

All legacy configuration files have been consolidated into the unified Settings.json format.
