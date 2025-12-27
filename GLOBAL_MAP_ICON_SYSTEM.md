# Global Map Icon System Implementation

## Overview
This document describes the complete **Global Map Icon System** that provides:
- **Real-time player tracking** on the map for all players
- **3-second update intervals** using the PlayerLocationTracker
- **MapIcon_CastleObject_BloodAltar prefab** for all map icons
- **Lifecycle integration** with arena management
- **Performance optimization** for large player counts

## Core Components

### 1. **GlobalMapIconService** - Main Service
```csharp
public static class GlobalMapIconService
{
    // Main service that manages all map icons globally
    public static void Initialize();
    public static void Cleanup();
    
    // Updates all player icons every 3 seconds
    public static void UpdateAllPlayerIcons();
    
    // Uses MapIcon_CastleObject_BloodAltar prefab
    public static string MapIconPrefabName => "MapIcon_CastleObject_BloodAltar";
}
```

**Key Features:**
- Updates every 3 seconds automatically via timer
- Uses PlayerLocationTracker for player positions
- Creates map icons using specified prefab
- Automatic cleanup for offline players
- Performance optimized with distance-based updates

### 2. **PlayerLocationTracker Integration**
```csharp
public static class PlayerLocationTracker
{
    // Continuously tracks all player positions
    public static void UpdatePlayerLocation(Entity user, Entity character);
    
    // Automatically detects zone entries/exits
    private static void CheckZoneChanges(ulong platformId, float3 position);
    
    // Triggers lifecycle events for arena management
    private static void TriggerPlayerEnter(ulong platformId, string arenaId);
    private static void TriggerPlayerExit(ulong platformId, string arenaId);
}
```

**Integration Benefits:**
- Same tracking system used for arena auto-detection
- Consistent position data across all systems
- Automatic lifecycle event triggering
- No duplicate tracking overhead

### 3. **ECS Integration**
```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct GlobalMapIconSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Update player locations through tracker
        UpdatePlayerLocations();
        
        // Process map icon updates
        ProcessMapIconUpdates(ref state, deltaTime);
    }
}
```

**ECS Systems:**
- **GlobalMapIconSystem** - Main update system
- **MapIconLifecycleSystem** - Handles arena lifecycle integration
- **MapIconOptimizationSystem** - Performance optimization

## Map Icon Types and Configuration

### Icon Types
1. **Normal Player** (Cyan) - Default players in normal areas
2. **Arena Player** (Orange) - Players inside arena zones
3. **PvP Player** (Red) - Players inside PvP arenas

### Configuration (config/gg.vautomation.arena.cfg)
```ini
[GlobalMapIconService]
# Enable global map icon service
Enabled = true

# Update interval (3 seconds)
UpdateInterval = 3.0

# Prefab name (MapIcon_CastleObject_BloodAltar)
PrefabName = MapIcon_CastleObject_BloodAltar

# Show different player types
ShowNormalPlayers = true
ShowArenaPlayers = true
ShowPvPPlayers = true

# Icon scaling
NormalPlayerScale = 1.0
ArenaPlayerScale = 1.2
PvPPlayerScale = 1.5

# Icon colors (RGBA)
NormalPlayerColor = 0.2,0.8,1.0,1.0    # Cyan
ArenaPlayerColor = 1.0,0.5,0.0,1.0     # Orange
PvPPlayerColor = 1.0,0.2,0.2,1.0      # Red
```

## System Architecture

### Update Flow (Every 3 Seconds)
```
1. Timer triggers update (GlobalMapIconService._updateTimer)
   ↓
2. GlobalMapIconService.UpdateAllPlayerIcons() called
   ↓
3. Get all online players from PlayerLocationTracker
   ↓
4. For each player:
   - Get current position and rotation
   - Determine icon type (normal/arena/pvp)
   - Create or update map icon entity
   ↓
5. Remove icons for offline players
   ↓
6. Update complete - all players visible on map
```

### Player Tracking Integration
```
1. Player moves around the world
   ↓
2. PlayerLocationTracker.UpdatePlayerLocation() called every frame
   ↓
3. CheckZoneChanges() detects zone transitions
   ↓
4. Trigger lifecycle events (arena enter/exit)
   ↓
5. GlobalMapIconService updates icon type based on location
   ↓
6. Map icon reflects player's current zone/status
```

## Implementation Details

### Map Icon Creation
```csharp
private static Entity CreateMapIconEntity(float3 position, quaternion rotation, MapIconPrefabConfig config)
{
    var em = VAutoCore.EntityManager;
    var iconEntity = em.CreateEntity();
    
    // Add transform components
    em.AddComponentData(iconEntity, new Translation { Value = position });
    em.AddComponentData(iconEntity, new Rotation { Value = rotation });
    em.AddComponentData(iconEntity, new NonUniformScale { Value = new float3(config.Scale) });
    
    // Add map icon components
    AddMapIconComponents(iconEntity, config);
    
    // Add prefab reference (MapIcon_CastleObject_BloodAltar)
    AddPrefabReference(iconEntity, config.PrefabName);
    
    return iconEntity;
}
```

### Zone Detection for Icon Types
```csharp
private static string GetIconTypeForPlayer(UserData player, float3 position)
{
    // Check if player is in PvP arena
    if (IsPlayerInPvPArena(position))
        return "player_pvp";        // Red icon
    
    // Check if player is in any arena
    if (IsPlayerInArena(position))
        return "player_arena";      // Orange icon
    
    // Default normal player
    return "player_default";       // Cyan icon
}
```

### Performance Optimizations
1. **Distance-based updates** - Only update icons when players move significantly
2. **Visibility culling** - Hide icons for distant players if needed
3. **Batch processing** - Update multiple icons efficiently
4. **Cleanup system** - Remove inactive icons automatically

## Lifecycle Integration

### Arena Enter Event
```
1. Player enters arena zone (-1000, 5, -500)
   ↓
2. PlayerLocationTracker detects zone entry
   ↓
3. ArenaLifecycleManager.OnPlayerEnter() triggered
   ↓
4. MapIconLifecycleSystem updates icon to "arena" type
   ↓
5. Icon color changes from cyan to orange
   ↓
6. Player now visible as arena participant
```

### Arena Exit Event
```
1. Player exits arena zone
   ↓
2. PlayerLocationTracker detects zone exit
   ↓
3. ArenaLifecycleManager.OnPlayerExit() triggered
   ↓
4. MapIconLifecycleSystem updates icon to "normal" type
   ↓
5. Icon color changes from orange to cyan
   ↓
6. Player state restored, building system cleaned up
```

## Configuration Options

### Main Settings
- **Enabled** - Enable/disable the entire system
- **UpdateInterval** - How often to update icons (default: 3.0 seconds)
- **PrefabName** - Which prefab to use for icons

### Visual Settings
- **ShowNormalPlayers** - Show icons for players in normal areas
- **ShowArenaPlayers** - Show icons for players in arenas
- **ShowPvPPlayers** - Show icons for players in PvP zones

### Appearance Settings
- **Scale settings** - Size of icons for different player types
- **Color settings** - RGBA colors for each icon type
- **Visibility settings** - Control which icons are shown

## Usage Examples

### Manual Icon Update
```csharp
// Force update all player icons
GlobalMapIconService.UpdateAllPlayerIcons();

// Get specific player icon
var icon = GlobalMapIconService.GetPlayerIcon(platformId);

// Check active icon count
var count = GlobalMapIconService.GetActiveIconCount();
```

### Configuration Control
```csharp
// Change update interval
GlobalMapIconService.SetUpdateInterval(5.0f); // Update every 5 seconds

// Get current interval
var interval = GlobalMapIconService.GetUpdateInterval();
```

### Query Methods
```csharp
// Get all active icons
var allIcons = GlobalMapIconService.GetAllActiveIcons();

// Get players in specific zone
var playersInArena = PlayerLocationTracker.GetPlayersInZone("main_arena");

// Check if player is in zone
var isInArena = PlayerLocationTracker.IsPlayerInZone(platformId, "main_arena");
```

## Benefits

### 1. **Complete Player Visibility**
- All players visible on map at all times
- Real-time position updates every 3 seconds
- No blind spots or missing player information

### 2. **Zone-Aware Icons**
- Different colors for different zones
- Instant visual feedback for arena status
- Easy identification of PvP participants

### 3. **Performance Optimized**
- Efficient update system with configurable intervals
- Automatic cleanup of offline players
- Distance-based optimization for large player counts

### 4. **Lifecycle Integration**
- Icons automatically reflect arena status
- Seamless integration with existing lifecycle system
- Consistent with auto-detection functionality

### 5. **Highly Configurable**
- All aspects controllable via configuration file
- Easy to customize appearance and behavior
- Support for different prefabs and settings

## Implementation Status

✅ **GlobalMapIconService** - Main service implementation  
✅ **3-second timer system** - Automatic updates  
✅ **PlayerLocationTracker integration** - Uses existing tracking  
✅ **MapIcon_CastleObject_BloodAltar prefab** - Specified prefab usage  
✅ **ECS integration** - Performance-optimized systems  
✅ **Lifecycle integration** - Arena-aware icon updates  
✅ **Configuration support** - Complete config file setup  
✅ **Performance optimization** - Efficient update system  

## System Requirements

- **BepInEx Configuration** - Updated config/gg.vautomation.arena.cfg
- **ECS Framework** - Unity ECS for performance
- **Player Location Tracking** - Existing system integration
- **Lifecycle System** - Arena management integration

The system is **production-ready** and provides complete global map visibility with the specified 3-second update interval and MapIcon_CastleObject_BloodAltar prefab usage.