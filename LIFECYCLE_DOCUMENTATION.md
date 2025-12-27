# VAuto Arena Lifecycle Service - Complete Documentation

## Table of Contents
- [Overview](#overview)
- [Architecture](#architecture)
- [Core Components](#core-components)
- [API Reference](#api-reference)
- [Usage Examples](#usage-examples)
- [Configuration](#configuration)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)
- [Compliance & Rules](#compliance--rules)
- [Implementation Notes](#implementation-notes)

## Overview

The **LifecycleService** is the unified core service that manages the complete player lifecycle within V Rising practice arenas. It consolidates functionality from multiple legacy services (Snapshots, ItemManagementService, VBloodHookService) into a single, cohesive system.

### Key Features
- **State-driven lifecycle management** with atomic operations
- **Crash recovery** with persistent snapshots
- **Automatic zone detection** and entry/exit triggers
- **VBlood progression unlocks** during arena sessions
- **Complete inventory and equipment management**
- **UI element unlocks** for testing purposes
- **Comprehensive logging and error handling**
- **Single player compatibility** with ServerLaunchFix support
- **Dual-process loading** for client and server integration

### Core Principles
1. **One state per player** - No duplicate arena entries
2. **Snapshot integrity** - Complete state capture before modifications
3. **Atomic operations** - All-or-nothing state changes
4. **Crash recovery** - Automatic restoration on server restart
5. **Admin-only control** - Server-side validation and permissions

## Architecture

### Service Structure
```
LifecycleService (Main Service)
├── Player State Management
│   ├── PlayerState class
│   ├── ConcurrentDictionary<ulong, PlayerState>
│   └── PlayerLifecycleState enum
├── Snapshot System
│   ├── PlayerSnapshot class
│   ├── Persistence (JSON files)
│   └── Crash Recovery
├── Zone Management
│   ├── ArenaZoneConfig class
│   ├── Automatic position monitoring
│   └── Zone validation
├── Build Integration
│   ├── BuildService integration
│   ├── Loadout application
│   └── Equipment management
└── UI/Progression System
    ├── DebugEventsSystem integration
    ├── VBlood unlocks
    └── Achievement completion
```

### Data Flow
1. **Entry Trigger** → Position monitoring or admin command
2. **State Validation** → Check if player already in arena
3. **Snapshot Creation** → Capture complete player state
4. **State Modification** → Apply PvP identity and loadout
5. **Persistence** → Save snapshot to disk
6. **Exit Trigger** → Position monitoring or admin command
7. **State Restoration** → Restore from snapshot
8. **Cleanup** → Delete persisted snapshot

## Core Components

### PlayerLifecycleState Enum
```csharp
public enum PlayerLifecycleState
{
    Normal,      // Outside arena, normal gameplay
    Entering,    // Transitioning into arena
    PvPPractice, // In arena with PvP practice mode active
    Exiting      // Transitioning out of arena
}
```

### PlayerState Class
```csharp
public class PlayerState
{
    public Entity Character { get; set; }
    public Entity UserEntity { get; set; }
    public PlayerSnapshot Snapshot { get; set; }
    public DateTime EnteredAt { get; set; }
    public PlayerLifecycleState State { get; set; }
}
```

### PlayerSnapshot Class
```csharp
public class PlayerSnapshot
{
    public int Version { get; set; }
    public DateTime CapturedAt { get; set; }
    public string OriginalName { get; set; }
    public int Level { get; set; }
    public float Health { get; set; }
    public float BloodQuality { get; set; }
    public int BloodTypeGuid { get; set; }
    public List<ItemEntry> Inventory { get; set; }
    public List<EquipmentEntry> Equipment { get; set; }
    public List<int> VBloods { get; set; }
    public List<int> Achievements { get; set; }
    public List<BuffEntry> PassiveAbilities { get; set; }
    public UIStateSnapshot UIState { get; set; }
}
```

### ArenaZoneConfig Class
```csharp
public class ArenaZoneConfig
{
    public string Name { get; set; }
    public float3 Center { get; set; }
    public float CenterRadius { get; set; }
    public float ZoneRadius { get; set; }
    public string BuildName { get; set; }
}
```

## API Reference

### Core Methods

#### EnterArena
```csharp
public static bool EnterArena(Entity userEntity, Entity character, string buildName = null)
```
**Purpose**: Initiates arena entry sequence
**Parameters**:
- `userEntity`: Player's user entity
- `character`: Player's character entity
- `buildName`: Optional specific build to apply
**Returns**: `true` if successful, `false` otherwise
**Behavior**:
1. Validates entities exist
2. Checks if player already in arena
3. Captures or reuses existing snapshot
4. Clears current inventory and equipment
5. Applies build and PvP identity
6. Teleports to arena center
7. Unlocks UI elements and VBloods

#### ExitArena
```csharp
public static bool ExitArena(Entity userEntity, Entity character)
```
**Purpose**: Restores player to normal gameplay
**Parameters**:
- `userEntity`: Player's user entity
- `character`: Player's character entity
**Returns**: `true` if successful, `false` otherwise
**Behavior**:
1. Validates entities exist
2. Retrieves player state
3. Restores from snapshot
4. Deletes persisted snapshot
5. Removes from arena tracking

#### IsInArena
```csharp
public static bool IsInArena(ulong platformId)
```
**Purpose**: Checks if player is currently in arena
**Returns**: `true` if in arena, `false` otherwise
**Behavior**:
1. Retrieves player state from playerStates dictionary
2. Returns true if state exists and State is PlayerLifecycleState.PvPPractice

#### ValidatePlayerInPvPZone
```csharp
public static bool ValidatePlayerInPvPZone(ulong platformId)
```
**Purpose**: Validates player is in PvP zone with proper state
**Returns**: `true` if valid, `false` otherwise
**Behavior**:
1. Checks if player is in arena using IsInArena()
2. Retrieves player position and checks if within configured arena zone boundaries
3. Validates player state is consistent with PvP zone requirements

### Configuration Methods

#### LoadArenaZonesConfig
```csharp
public static void LoadArenaZonesConfig()
```
**Purpose**: Loads arena zone configuration from JSON file
**File**: `VAuto/Builds/arena_zones.json`

#### ReloadArenaZonesConfig
```csharp
public static void ReloadArenaZonesConfig()
```
**Purpose**: Reloads arena configuration at runtime

### Persistence Methods

#### LoadPersistedSnapshot
```csharp
public static PlayerSnapshot LoadPersistedSnapshot(ulong platformId)
```
**Purpose**: Loads persisted snapshot from disk
**Returns**: Snapshot if exists, `null` otherwise
**Behavior**:
1. Constructs file path using platformId
2. Checks if file exists
3. Deserializes JSON content to PlayerSnapshot object
4. Returns deserialized snapshot or null if file doesn't exist or deserialization fails

#### SaveSnapshotToDisk
```csharp
public static void SaveSnapshotToDisk(ulong platformId, PlayerSnapshot snapshot)
```
**Purpose**: Persists snapshot to disk
**Behavior**:
1. Constructs file path using platformId
2. Serializes snapshot to JSON format
3. Writes JSON content to file with atomic operation
4. Logs success or handles serialization errors

#### DeletePersistedSnapshot
```csharp
public static void DeletePersistedSnapshot(ulong platformId)
```
**Purpose**: Removes persisted snapshot file
**Behavior**:
1. Constructs file path using platformId
2. Checks if file exists
3. Deletes the file if it exists
4. Logs deletion or handles missing file gracefully

### Update Methods

#### Update
```csharp
public static void Update(float deltaTime)
```
**Purpose**: Main update loop for position monitoring and cleanup
**Parameters**:
- `deltaTime`: Time elapsed since last update

### Initialization

#### Initialize
```csharp
public static void Initialize()
```
**Purpose**: Initializes the lifecycle service
**Behavior**:
1. Creates players directory
2. Loads arena zone configuration
3. Logs initialization status

## Usage Examples

### Basic Arena Entry (Admin Command)
```csharp
// From ArenaCommands.cs
[Command("enter", adminOnly: true)]
public void EnterArenaCommand(ChatCommandContext ctx, string buildName = null)
{
    var userEntity = ctx.Event.SenderUserEntity;
    var character = ctx.Event.SenderCharacterEntity;

    if (LifecycleService.EnterArena(userEntity, character, buildName))
    {
        ctx.Reply($"🏟️ Welcome to the arena! Build: {buildName ?? "default"}");
    }
    else
    {
        ctx.Reply("❌ Failed to enter arena");
    }
}
```

### Automatic Zone Entry
```csharp
// From position monitoring in Update()
private static void CheckPlayerPositions()
{
    // ... entity queries ...

    foreach (var userEntity in users)
    {
        // ... position calculations ...

        if (!wasInArena && isInCenterRadius)
        {
            // Automatic entry trigger
            LifecycleService.EnterArena(userEntity, character);
        }
        else if (wasInArena && !isInZoneBoundary)
        {
            // Automatic exit trigger
            LifecycleService.ExitArena(userEntity, character);
        }
    }
}
```

### State Validation
```csharp
// Check if player can use arena-specific commands
if (!LifecycleService.IsInArena(platformId))
{
    ctx.Reply("❌ You must be in the arena to use this command");
    return;
}

// Validate PvP zone state
if (!LifecycleService.ValidatePlayerInPvPZone(platformId))
{
    ctx.Reply("❌ Invalid arena state");
    return;
}
```

### Crash Recovery
```csharp
// Called on player login via Harmony patch
public static void HandleCrashRecovery(ulong platformId, Entity userEntity, Entity character)
{
    var snapshot = LoadPersistedSnapshot(platformId);
    if (snapshot != null)
    {
        RestoreFromSnapshot(character, userEntity, snapshot);
        DeletePersistedSnapshot(platformId);
    }
}
```

## Configuration

### Arena Zones Configuration
**Configuration Key**: `Arena.Zones` (BepInEx config)

**Format**: `ZoneName|x,y,z|centerRadius|zoneRadius|optionalBuildName;ZoneName2|x,y,z|centerRadius|zoneRadius`

**Default**: `Default Arena|-1000,5,-500|25|50`

**Examples**:
- Single zone: `Practice Arena|-1000,5,-500|25|50`
- Multiple zones: `Arena1|-1000,5,-500|25|50;Arena2|500,5,200|20|40|tank`

**Configuration Location**: `BepInEx/config/gg.automation.arena.cfg`

### Plugin Configuration Values
- `PositionCheckInterval`: How often to check player positions (default: 3.0 seconds)
- `DefaultBuild`: Default build name to apply (default: "default")
- `PvPBloodTypeGuidHash`: Blood type GUID for PvP identity
- `CornerBuffGuidHash`: GUID for corner buff effects
- `CornerBuffDetectionRadius`: Radius for corner buff detection
- `CornerBuffReapplyInterval`: How often to reapply corner buffs

### Build Configuration
Builds are managed through `BuildService` and stored in `VAuto/Builds/builds.json`

## Testing

### Unit Tests
Located in `Tests/LifecycleServiceTests.cs`

```csharp
// Run all tests
LifecycleServiceTests.RunAllTests();

// Individual test methods
LifecycleServiceTests.TestSnapshotSerialization();
LifecycleServiceTests.TestArenaZoneConfiguration();
LifecycleServiceTests.TestVBloodUnlockLogic();
LifecycleServiceTests.TestPlayerStateManagement();
LifecycleServiceTests.TestArenaZoneSerialization();
```

### Integration Tests
Located in `Tests/IntegrationTests.cs`

### Manual Testing Commands
```bash
# Enter arena
.enter default

# Exit arena
.exit

# Create zone
.setzonehere "Test Arena" 25 50

# Test builds
.arena list builds
.arena give build "TestBuild"

# Run tests
.test run
.test integration
```

### Test Coverage Areas
- ✅ Snapshot serialization/deserialization
- ✅ Arena zone configuration loading
- ✅ VBlood unlock logic
- ✅ Player state management
- ✅ Crash recovery
- ✅ Automatic zone entry/exit
- ✅ Build application and restoration
- ✅ UI element unlocks
- ✅ Concurrent player handling

## Single Player Compatibility

### ServerLaunchFix Integration
The VAuto mod is fully compatible with single player mode using ServerLaunchFix (SLF). Key implementation details:

#### Process Loading
```csharp
[BepInProcess("VRising.exe")]
[BepInProcess("VRisingServer.exe")]
```
- **Dual-process support**: Mod loads in both client and server processes
- **Automatic detection**: Works with SLF's server spawning mechanism
- **Unified functionality**: Consistent behavior across single player and multiplayer

#### Initialization Logging
Enhanced logging for troubleshooting single player issues:
```csharp
base.Log.LogInfo($"[VAuto] Load() started in process: {System.Diagnostics.Process.GetCurrentProcess().ProcessName}");
```
- **Process identification**: Logs which process the mod loads in
- **Step-by-step tracking**: Detailed initialization progress
- **Error capture**: Try-catch blocks prevent silent failures

#### Compatibility Requirements
- **ServerLaunchFix**: Required for single player server functionality
- **VampireCommandFramework**: Must be installed for command processing
- **BepInEx 6.0.0-dev**: Compatible with current development version

### Single Player Setup
1. Install ServerLaunchFix from Thunderstore
2. Install VampireCommandFramework
3. Place VAuto.dll in `BepInEx/plugins/`
4. Run V Rising in single player mode
5. Check logs for `[VAuto]` initialization messages

## Troubleshooting

### Common Issues

#### Players Can't Enter Arena
**Symptoms**: `.enter` command fails
**Causes**:
- Player already in arena
- Invalid entities
- Snapshot persistence failure
- Build service unavailable
**Solutions**:
1. Check if player already in arena with state validation
2. Verify entity validity
3. Check disk space for snapshot files
4. Ensure BuildService is properly initialized

#### Players Stuck in Arena State
**Symptoms**: Player shows as in arena but not actually there
**Causes**:
- State desynchronization
- Failed exit operation
- Memory state corruption
**Solutions**:
1. Manual state cleanup via admin commands
2. Server restart to clear memory state
3. Check persisted snapshot files

#### Build Application Failures
**Symptoms**: Player enters arena but build not applied
**Causes**:
- Invalid build name
- BuildService errors
- Timing issues with async operations
**Solutions**:
1. Verify build exists with `.arena list builds`
2. Check BuildService logs
3. Ensure proper async operation timing

#### Teleport Positioning Issues
**Symptoms**: Players teleported to wrong location
**Causes**:
- Arena zone configuration errors
- Build/teleport timing conflicts
- Coordinate system issues
**Solutions**:
1. Verify `arena_zones.json` configuration
2. Check for build application timing issues
3. Validate coordinate values

#### Crash Recovery Failures
**Symptoms**: Players lose progress after server restart
**Causes**:
- Snapshot file corruption
- Deserialization errors
- Missing crash recovery patch
**Solutions**:
1. Check snapshot file integrity
2. Verify JSON format
3. Ensure Harmony patch is applied on login

### Debug Commands
```bash
# Check player state
.player state <platformId>

# List active arena players
.arena players

# Force state cleanup
.admin cleanup <platformId>

# Test zone configuration
.test zones

# Test snapshot operations
.test snapshots
```

### Log Analysis
Key log patterns to monitor:

```
[ARENA_ENTRY_START] - Arena entry initiated
[ARENA_SNAPSHOT_CAPTURED] - Snapshot successfully captured
[ARENA_BUILD_APPLIED] - Build applied successfully
[ARENA_TELEPORT_SUCCESS] - Teleport completed
[ARENA_ENTRY_SUCCESS] - Entry sequence completed

[ARENA_ENTRY_FAILED] - Entry failed (check details)
[ARENA_BUILD_FAILED] - Build application failed
[ARENA_TELEPORT_WARNING] - Teleport had issues but continued
```

### Performance Monitoring
- Monitor position check intervals (default 3s)
- Track snapshot file sizes
- Monitor concurrent player counts
- Check async operation completion times

## Compliance & Rules

### Core Principles (Non-Negotiable)
1. **State-driven lifecycle** - All operations based on PlayerLifecycleState
2. **One state per player** - Prevents duplicate arena entries
3. **No mixing across states** - Separate inventory and identity management
4. **Snapshot integrity > convenience** - Complete state capture before modifications

### Authority & Permissions
- **Admin-only commands** - All arena lifecycle commands require admin privileges
- **Server-side validation** - Enforced by VampireCommandFramework
- **Command uniqueness** - No duplicate command definitions
- **Unique command tokens** - 8-character GUID tracking for all operations

### Zone Management Rules
- **Automatic entry** - Triggered by center radius entry
- **Automatic exit** - Triggered by zone boundary exit
- **Zone configuration** - JSON-based with runtime reloading
- **Zone validation** - Comprehensive checks for arena-specific operations

### Snapshot Rules (Critical)
- **Atomic capture** - All-or-nothing snapshot operations
- **Complete contents** - All required state fields captured
- **Persistent storage** - JSON files with error handling
- **Automatic disposal** - Files deleted after successful restoration
- **Crash recovery** - Automatic restoration on server restart

### PvP Identity Rules
- **Name prefixing** - `[PvP]` prefix for arena players
- **Blood type override** - Configurable PvP blood type
- **Inventory separation** - PvP inventory separate from normal
- **Equipment clearing** - All equipped items removed on entry

### Restoration Rules
- **Complete restoration** - All captured state restored
- **Order enforcement** - Proper sequence of operations
- **Validation checks** - Entity and state validation
- **Error recovery** - Partial failure handling

### Failure Handling Rules
- **Crash recovery** - Harmony patches for login restoration
- **Corruption guards** - State validation prevents conflicts
- **Error logging** - Comprehensive error reporting
- **Rollback support** - Failed operations don't leave partial state

---

## Implementation Notes

### Thread Safety
- Uses `ConcurrentDictionary` for player states
- Atomic snapshot operations with file locking
- Thread-safe VBlood hook management

### Performance Considerations
- Efficient position monitoring with configurable intervals
- Lazy snapshot loading
- Minimal entity queries
- Async operations for network stability

### Extensibility
- Event-driven architecture with OnArenaEnter/OnArenaExit events
- Configurable build resolution
- Plugin-based configuration system
- Modular service architecture

### Security
- Admin-only command validation
- Server-side state enforcement
- Input sanitization and validation
- Audit logging for all operations

### Best Practices
- **Always validate entities** before calling lifecycle methods
- **Check arena state** before applying arena-specific logic
- **Handle exceptions gracefully** in calling code
- **Monitor snapshot file sizes** to prevent disk space issues
- **Use atomic operations** when modifying player state
- **Log all state changes** for debugging and auditing
- **Test crash recovery** regularly in development environments
- **Backup configuration files** before runtime modifications

---

**Version**: 1.0.0
**Last Updated**: December 2025
**Maintainer**: VAuto Development Team
