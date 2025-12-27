# VAuto Mod - Comprehensive Documentation

## Overview
VAuto is a BepInEx plugin for V Rising that provides arena management, character progression, and automation features for PvP practice and server administration.

## Table of Contents
1. [Commands](#commands)
   - [Arena Commands](#arena-commands)
   - [Character Commands](#character-commands)
   - [General Commands](#general-commands)
2. [Services](#services)
   - [LifecycleService](#lifecycleservice)
   - [ArenaAuraService](#arenauraservice)
   - [ZoneService](#zoneservice)
   - [Other Services](#other-services)
3. [Patches](#patches)
4. [Core Systems](#core-systems)
5. [Configuration](#configuration)

---

# Commands

## Arena Commands

### Arena Management Commands (Admin Only)
Located in `Commands/ArenaCommands.cs`

#### `arena enter` / `arena en`
- **Purpose**: Enter arena mode with full progression unlocked
- **Admin Only**: Yes
- **Functionality**:
  - Calls `LifecycleService.EnterArena()` to create player snapshot
  - Unlocks all VBloods and progression
  - Applies arena gear and teleports to arena center
- **Usage**: `.arena enter`

#### `arena exit` / `arena ex`
- **Purpose**: Exit arena mode and restore original state
- **Admin Only**: Yes
- **Functionality**:
  - Calls `LifecycleService.ExitArena()` to restore player snapshot
  - Removes arena gear and teleports back to original position
- **Usage**: `.arena exit`

#### `arena setzonehere` / `arena szh`
- **Purpose**: Set the arena zone center at current admin position
- **Admin Only**: Yes
- **Parameters**:
  - `zoneName` (optional): Name for the zone (default: "Default Arena")
  - `centerRadius` (optional): Inner safe zone radius (default: 25f)
  - `zoneRadius` (optional): Outer zone boundary (default: 50f)
- **Functionality**:
  - Gets current position using Translation/LocalToWorld components
  - Updates BepInEx config with zone coordinates
  - Updates ZoneService with new coordinates
- **Usage**: `.arena setzonehere "My Arena" 20 40`

### Blood Quality Commands
#### `arena bpm` / `arena blood`
- **Purpose**: Set blood quality to 100% for faster leveling
- **Admin Only**: No (available to all players)
- **Parameters**:
  - `bloodType` (optional): Blood type to set (rogue/warrior/scholar/brute)
  - `secondaryBloodType` (optional): Secondary blood type
- **Functionality**:
  - Modifies BloodConsumeSource and Blood components
  - Sets Quality to 100.0f
  - Applies blood type if specified
- **Usage**: `.arena bpm rogue`

### Gear Commands
#### `arena kit` / `arena equip`
- **Purpose**: Equip predefined gear sets
- **Admin Only**: No
- **Parameters**:
  - `kit` (optional): Kit type (default: "rogue")
- **Supported Kits**:
  - `rogue`: Full Dracula Scholar set + weapons + potions
- **Functionality**:
  - Clears existing inventory
  - Applies complete armor sets using DebugEventsSystem.GiveItem
  - Includes weapons and healing potions
- **Usage**: `.arena kit rogue`

---

## Character Commands

### Dual Character System
VAuto implements a **dual character system** where each player has two separate character entities:

1. **`PlayerName`** - The original, persistent character (keeps all progress)
2. **`PlayerName [PvP]`** - Ephemeral practice character (unlimited rules, zone-restricted)

### Simplified Arena Commands
Located in `Commands/CharacterCommand.cs`

#### `charenter`
- **Purpose**: Create/switch to [PvP] practice character (zone-aware)
- **Admin Only**: No
- **Zone Requirement**: Must be within arena zone radius
- **Functionality**:
  - Checks distance from arena center (ZoneService.Center)
  - Creates PvP character if needed (named "PlayerName [PvP]")
  - Switches active character to PvP mode
  - Unlocks all progression and VBloods
  - Applies full Dracula armor set and weapons
  - Teleports to arena center
  - Provides distance feedback if outside zone
- **Usage**: `.charenter`

#### `charexit`
- **Purpose**: Switch back to normal character (self-apply)
- **Admin Only**: No
- **Functionality**:
  - Switches active character back to original "PlayerName"
  - Freezes the [PvP] character (removes from network)
  - Teleports back to original position
  - Works from anywhere (no zone check required)
- **Usage**: `.charexit`

### PvP Character Commands (Admin Only)

#### `ch createpvp` / `ch pvpchar`
- **Purpose**: Transform character to PvP mode
- **Admin Only**: Yes
- **Parameters**:
  - `player`: Target player data
- **Functionality**:
  - Stores original character state
  - Renames character with [PvP] prefix
  - Changes blood type to PvP blood
  - Unlocks all progression
  - Applies full Dracula armor set
  - Teleports to arena center
- **Usage**: `.ch createpvp @playername`

#### `ch switchnormal` / `ch normalchar`
- **Purpose**: Switch back to normal character
- **Admin Only**: Yes
- **Parameters**:
  - `player`: Target player data
- **Functionality**:
  - Restores original character name
  - Teleports back to original position
  - Clears PvP state
- **Usage**: `.ch switchnormal @playername`

---

## General Commands

### Commands System
Located in `Commands/Commands.cs`

#### `help`
- **Purpose**: Display available commands
- **Usage**: `.help`

#### `reload`
- **Purpose**: Reload configuration files
- **Admin Only**: Yes
- **Functionality**:
  - Reloads all BepInEx config files
  - Updates service configurations
- **Usage**: `.reload`

---

# Services

## LifecycleService

### Core Methods

#### `EnterArena(Entity user, Entity character, string zoneId)`
- **Purpose**: Create snapshot and enter arena mode
- **Parameters**:
  - `user`: User entity
  - `character`: Character entity
  - `zoneId`: Zone identifier
- **Functionality**:
  - Creates PlayerSnapshot with full state
  - Stores in snapshots dictionary
  - Calls progression unlock methods
  - Updates player tracking
- **Return**: bool (success)

#### `ExitArena(Entity user, Entity character)`
- **Purpose**: Restore original state and exit arena
- **Parameters**:
  - `user`: User entity
  - `character`: Character entity
- **Functionality**:
  - Retrieves stored PlayerSnapshot
  - Restores all original components
  - Removes from active players
  - Cleans up arena-specific data
- **Return**: bool (success)

#### `Update(float deltaTime)`
- **Purpose**: Update lifecycle every server tick
- **Functionality**:
  - Updates restriction ticker
  - Updates revive ticker
  - Handles position monitoring
  - Manages arena zone boundaries

### Properties
- `ArenaZones`: Dictionary of active arena zones
- `Snapshots`: Dictionary of player snapshots
- `ActivePlayers`: Set of players in arena mode

## ArenaAuraService

### Core Methods

#### `Initialize()`
- **Purpose**: Initialize aura system with config values
- **Configuration**:
  - `ArenaAuraBuffs`: Comma-separated buff IDs
  - `ArenaCornerBuffs`: Corner effect buff IDs
  - `ZoneRadius`: Arena zone radius
- **Functionality**:
  - Parses buff configurations
  - Sets arena center from ZoneService
  - Initializes effect arrays

#### `Update()`
- **Purpose**: Update aura effects every server tick
- **Functionality**:
  - Queries all users with Network.User component
  - Checks player positions against arena zones
  - Applies/removes buffs based on location
  - Handles Bloodmind cooldown boosts
  - Applies corner effects near arena boundaries

### Configuration Options
- `ArenaAuraBuffs`: Buff IDs to apply in arena
- `ArenaCornerBuffs`: Effects for arena corners
- `BloodMendBoostEnabled`: Enable Bloodmind boost
- `ZoneRadius`: Arena zone size

## ZoneService

### Properties
- `Center`: Arena center coordinates (float3)
- `Radius`: Arena zone radius
- `Spawn`: Safe spawn position

### Methods

#### `SetArenaZone(float3 center, float radius)`
- **Purpose**: Set arena zone boundaries
- **Parameters**:
  - `center`: Zone center position
  - `radius`: Zone radius

#### `SetSpawn(float3 position)`
- **Purpose**: Set safe spawn location
- **Parameters**:
  - `position`: Spawn coordinates

#### `IsInZone(float3 position)`
- **Purpose**: Check if position is within arena zone
- **Return**: bool

## Other Services

### TeleportService
- **Teleport(Entity character, float3 destination)**: Teleport character to position

### UnlockHelper
- **TryUnlockVBloods(Entity user, Entity character)**: Unlock all VBlood progression

### BuildService
- **ApplyArenaGear(Entity character)**: Apply arena equipment
- **CreateSnapshot(...)**: Create player state snapshot

---

# Patches

## UpdatePatch
Located in `Patches/UpdatePatch.cs`

### Harmony Patch
- **Target**: `ServerBootstrapSystem.OnUpdate`
- **Purpose**: Inject custom update logic into server tick

### Functionality
- Deferred arena system initialization
- Lifecycle service updates
- Position monitoring for arena zones
- Error handling and logging

---

# Core Systems

## VRCore
Located in `Core/VRCore.cs`

### Static Properties
- `ServerWorld`: Main ECS World
- `EM`: EntityManager instance
- `ServerGameManager`: Game management system

### Methods
- `Initialize()`: Initialize core systems
- `GetServerGameManager()`: Get game manager instance

## Plugin
Located in `Core/Plugin.cs`

### Configuration Options
- `ArenaZones`: Zone configuration string
- `ZoneRadius`: Arena radius (default: 50f)
- `ArenaAuraBuffs`: Buff configuration
- `ArenaCornerBuffs`: Corner effect configuration
- `BloodMendBoostEnabled`: Bloodmind boost toggle
- `PvPBloodTypeGuidHash`: PvP blood type

### Initialization
- Loads configurations
- Registers Harmony patches
- Initializes services
- Sets up command system

---

# Configuration

## BepInEx Config Files

### gg.vautomation.arena.cfg
```ini
[Arena]
# Arena zone configuration
ArenaZones = "Default Arena|0,0,0|25|50"

# Zone settings
ZoneRadius = 50

# Aura system
ArenaAuraBuffs = ""
ArenaCornerBuffs = ""
BloodMendBoostEnabled = false

# PvP settings
PvPBloodTypeGuidHash = -1464869978
```

## Command Usage Examples

### Basic Arena Usage
```
.charenter          # Enter arena (must be in zone)
.exit              # Exit arena
.arena setzonehere # Set arena at current position (admin)
```

### Advanced PvP Setup
```
.ch createpvp @player   # Create PvP character for player
.ch switchnormal @player # Switch back to normal
.arena kit rogue       # Equip full gear set
.arena bpm rogue       # Set blood quality to 100%
```

## Error Handling

### Common Error Messages
- "❌ No valid character or user entity found": Entity validation failed
- "❌ You must be in the arena zone to use this command": Zone boundary check
- "❌ Failed to enter arena. You may already be in the arena": Already in arena state
- "❌ Failed to exit arena. You may not be in the arena": Not in arena state

### Logging
- All commands log to BepInEx console with command tokens for tracking
- Errors include full stack traces for debugging
- Success operations log user and target information

## Architecture Notes

### Service Pattern
- All major functionality split into focused services
- Services communicate through VRCore for ECS access
- Configuration-driven behavior

### Command Framework
- Uses VampireCommandFramework for command registration
- Supports aliases and admin-only commands
- Integrated help system

### ECS Integration
- Heavy use of Unity ECS (Entity Component System)
- Direct manipulation of game components
- Harmony patches for game loop integration

### State Management
- Snapshot-based state preservation
- Dictionary-based tracking of player states
- Automatic cleanup on exit

This documentation covers the complete VAuto mod functionality as of the current implementation.