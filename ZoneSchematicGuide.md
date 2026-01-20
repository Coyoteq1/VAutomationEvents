# Zone and Schematic Integration Guide

## Overview
This guide shows how to integrate zones and schematics into your VAuto plugin using the WorldAutomationService and JSON converters.

## Zone Management

### 1. Creating Zones

```csharp
using VAuto.Services.World;
using VAuto.Utilities;

// Create a new zone rule
var zoneRule = new AutomationRule
{
    TriggerName = "PlayerEnterZone",
    Actions = new List<AutomationAction>
    {
        new AutomationAction
        {
            Type = AutomationActionType.TriggerEvent,
            EventName = "OnZoneEnter",
            Parameters = new Dictionary<string, object>
            {
                ["zoneId"] = 1,
                ["message"] = "Player entered arena zone"
            }
        }
    }
};

// Add the rule
WorldAutomationService.AddRule("ArenaZone1", zoneRule);
```

### 2. Zone Triggers

```csharp
// Register a zone trigger entity
var triggerEntity = CreateTriggerEntity(new float3(-1000, 5, -500), "ArenaTrigger");
WorldAutomationService.RegisterTrigger(triggerEntity, "ArenaEntry", 50f);

// Enable/disable the trigger
WorldAutomationService.SetTriggerEnabled(triggerEntity, true);
```

### 3. Zone Actions

```csharp
// Spawn objects in zones
var spawnAction = new AutomationAction
{
    Type = AutomationActionType.SpawnObject,
    Position = new float3(-1000, 10, -500),
    PrefabName = "Castle_Basic_T1_C",
    ObjectType = WorldObjectType.Structure,
    Parameters = new Dictionary<string, object>
    {
        ["teamId"] = 1,
        ["health"] = 1000
    }
};

// Add to existing rule
var existingRule = WorldAutomationService.GetRules()["ArenaZone1"];
existingRule.Actions.Add(spawnAction);
```

## Schematic Integration

### 1. Using JSON Converters

```csharp
using System.Text.Json;
using VAuto.Utilities;

// Configure JSON options with schematic converters
var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Converters = 
    {
        new SchematicFloat3Converter(),
        new AabbConverter(),
        // Add more converters as needed
    }
};

// Serialize schematic data
var schematicData = new SchematicData
{
    Name = "Arena Layout",
    Bounds = new Aabb 
    {
        Min = new float3(-1100, 0, -600),
        Max = new float3(-900, 20, -400)
    },
    Objects = new List<SchematicObject>
    {
        new SchematicObject
        {
            PrefabName = "Castle_Basic_T1_C",
            Position = new float3(-1000, 5, -500),
            Rotation = quaternion.identity
        }
    }
};

var json = JsonSerializer.Serialize(schematicData, jsonOptions);
File.WriteAllText("schematic.json", json);
```

### 2. Schematic Data Structures

```csharp
/// <summary>
/// Schematic object definition
/// </summary>
public class SchematicData
{
    public string Name { get; set; }
    public Aabb Bounds { get; set; }
    public List<SchematicObject> Objects { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Individual schematic object
/// </summary>
public class SchematicObject
{
    public string PrefabName { get; set; }
    public float3 Position { get; set; }
    public quaternion Rotation { get; set; }
    public float3 Scale { get; set; } = new float3(1, 1, 1);
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Axis-aligned bounding box
/// </summary>
public struct Aabb
{
    public float3 Min { get; set; }
    public float3 Max { get; set; }
}
```

## Advanced Zone Features

### 1. Conditional Zone Logic

```csharp
// Create conditional zone rules
var conditionalRule = new AutomationRule
{
    TriggerName = "PlayerInZone",
    Actions = new List<AutomationAction>
    {
        new AutomationAction
        {
            Type = AutomationActionType.Delay,
            DelaySeconds = 5.0f,
            Parameters = new Dictionary<string, object>
            {
                ["condition"] = "player.health < 50%"
            }
        },
        new AutomationAction
        {
            Type = AutomationActionType.SpawnObject,
            PrefabName = "Health_Potion_Small",
            Position = new float3(-1000, 10, -500)
        }
    }
};
```

### 2. Zone State Management

```csharp
// Get zone status
var zoneStatus = WorldAutomationService.GetZoneStatus("ArenaZone1");
if (zoneStatus.IsActive)
{
    // Zone is active, perform actions
    var playerCount = zoneStatus.PlayerCount;
    Plugin.Logger?.LogInfo($"Zone has {playerCount} players");
}

// Update zone state
WorldAutomationService.UpdateZoneState("ArenaZone1", new ZoneState
{
    IsActive = true,
    PlayerCount = GetCurrentPlayerCount(),
    Timer = 300.0f // 5 minutes
});
```

## Integration with Plugin Settings

### 1. Zone Configuration

```csharp
// Add zone settings to your PluginSettings
public class ZoneSettings
{
    public List<ZoneDefinition> Zones { get; set; } = new();
    public float DefaultRadius { get; set; } = 50f;
    public bool EnableZoneLogging { get; set; } = true;
}

public class ZoneDefinition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public float3 Center { get; set; }
    public float Radius { get; set; }
    public List<string> AllowedPrefabs { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();
}
```

### 2. Loading Zone Configuration

```csharp
// Load zones from Settings.json
public static void LoadZonesFromSettings()
{
    var settings = PluginSettings.GetSettings();
    foreach (var zone in settings.Zones.Zones)
    {
        var zoneRule = CreateZoneRule(zone);
        WorldAutomationService.AddRule(zone.Id, zoneRule);
        
        Plugin.Logger?.LogInfo($"Loaded zone: {zone.Name}");
    }
}
```

## Best Practices

### 1. Performance
- Use object pooling for frequently spawned objects
- Implement zone enter/exit cooldowns
- Batch zone updates instead of per-frame checks

### 2. Error Handling
- Always validate zone boundaries
- Handle duplicate zone names gracefully
- Log zone creation/destruction events

### 3. Persistence
- Save zone states to JSON using converters
- Load zone configurations on plugin startup
- Implement zone state backup/restore

## Example Usage

```csharp
// Complete example: Create arena zone with schematic loading
public static void CreateArenaZoneWithSchematic()
{
    // 1. Load schematic
    var schematicJson = File.ReadAllText("arena_schematic.json");
    var schematic = JsonSerializer.Deserialize<SchematicData>(schematicJson, jsonOptions);
    
    // 2. Create zone rule
    var zoneRule = new AutomationRule
    {
        TriggerName = "ArenaZone",
        Actions = new List<AutomationAction>()
    };
    
    // 3. Add schematic objects to zone actions
    foreach (var obj in schematic.Objects)
    {
        zoneRule.Actions.Add(new AutomationAction
        {
            Type = AutomationActionType.SpawnObject,
            PrefabName = obj.PrefabName,
            Position = obj.Position,
            Rotation = obj.Rotation,
            Scale = obj.Scale,
            Parameters = obj.Properties
        });
    }
    
    // 4. Register the zone
    WorldAutomationService.AddRule("ArenaSchematic", zoneRule);
    
    Plugin.Logger?.LogInfo($"Created arena zone with {schematic.Objects.Count} objects");
}
```

This integration provides a robust foundation for zone management and schematic loading in your VAuto plugin.
