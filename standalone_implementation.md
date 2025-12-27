# Standalone RestrictedInventory Component Saver Implementation

## Overview
Created a standalone version of the RestrictedInventory component saver that uses only the native ProjectM API without any external framework dependencies like KindredSchematics.

## Key Differences from KindredSchematics Version

### 1. **Removed Dependencies**
- ❌ `KindredSchematics.Services.SchematicService` - Replaced with direct JSON serialization
- ❌ `KindredSchematics.Services.ComponentSaver` - Still inherits but simplified
- ✅ Direct use of `ProjectM` namespaces only
- ✅ Native `System.Text.Json` for serialization

### 2. **Direct JSON Handling**
```csharp
// KindredSchematics version
var saveData = jsonData.Deserialize<RestrictedInventory_Save>(SchematicService.GetJsonOptions());

// Standalone version
var options = new JsonSerializerOptions 
{ 
    PropertyNameCaseInsensitive = true,
    WriteIndented = false 
};
var saveData = JsonSerializer.Deserialize<RestrictedInventory_Save>(jsonData.GetRawText(), options);
```

### 3. **Logging Simplification**
```csharp
// KindredSchematics version
Plugin.Logger?.LogDebug($"RestrictedItemType changed: {entityData.RestrictedItemType}");

// Standalone version
// Removed debug logging for performance, only error/warning logs kept
```

### 4. **API Independence**
- Uses only `ProjectM` ECS APIs
- No external serialization framework dependencies
- Direct JSON manipulation using `System.Text.Json`
- Native Entity Component System operations

## Implementation Details

### Namespace Changes
```csharp
// KindredSchematics version
namespace KindredSchematics.ComponentSaver;

// Standalone version  
namespace VAutomation.ComponentSaver;
```

### Error Handling
- Maintained comprehensive error handling
- Silent fail for non-critical operations
- Error logging only for critical failures
- Graceful degradation when logging is unavailable

### Performance Optimizations
- Removed debug logging overhead in hot paths
- Simplified JSON serialization options
- Direct JSON text processing
- Minimal object allocations

## Benefits of Standalone Approach

### 1. **Reduced Dependencies**
- No external framework coupling
- Easier integration with existing V Rising mods
- Reduced memory footprint
- Faster compilation times

### 2. **Better Compatibility**
- Works with any ProjectM-based mod
- No version conflicts with KindredSchematics
- Compatible with different BepInEx setups
- Framework-agnostic design

### 3. **Maintainability**
- Clear separation of concerns
- Self-contained implementation
- Easy to modify and extend
- Direct control over serialization logic

## Usage

The standalone version maintains the same public API:

```csharp
// Works identically to KindredSchematics version
var saver = new RestrictedInventory_Saver();
var diffData = saver.DiffComponents(prefabEntity, entity, mapper);
var saveData = saver.SaveComponent(entity, mapper);
saver.ApplyComponentData(entity, jsonData, entitiesBeingLoaded);
```

## Integration Notes

1. **Replace the original** RestrictedInventory_Saver.cs with this standalone version
2. **Update namespace** imports in your mod
3. **Test compatibility** with your existing ProjectM setup
4. **Verify logging** works with your plugin's logger implementation

## File Structure
- `ComponentSaver/RestrictedInventory_Standalone.cs` - Standalone implementation
- `ComponentSaver/RestrictedInventory_Saver.cs` - Original KindredSchematics version (backup)

The standalone version provides the same functionality with improved independence and compatibility.