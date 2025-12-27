# Dual Character System for VRising Server

## Overview

This system creates two character entities for each player immediately upon login:

1. **Normal Character**: Uses the player's original name (e.g., "Luna")
2. **PvP Character**: Uses the naming convention "(playername pvp)" (e.g., "(Luna pvp)")

Players can instantly swap between these characters using commands without any delays or restrictions.

## Automatic Setup

- Both characters are created automatically when a player logs in
- No manual setup required
- PvP character gets default warrior gear and max blood quality
- Both characters are immediately available for instant switching

## Available Commands

### `.ch immswap` (Recommended)
**Immediate swap between characters - instant switching, no delays or restrictions**

- Works immediately after login
- No zone restrictions
- No delays or cooldowns
- Creates PvP character if missing
- Provides instant feedback with lightning bolt emoji ⚡

### `.ch swap` 
**Standard swap between normal and PvP character**

- Works without zone restrictions
- Requires dual character state to be initialized
- Standard feedback

### `.ch create`
**Create PvP character using KindredCommands approach**

- Sets PlatformId = 0 to force new character creation
- Stores PvP template for auto-application
- Player needs to reconnect after using this command

### `.ch charenter` / `.ch charexit`
**Zone-aware arena commands**

- `charenter`: Switch to PvP character (only in arena zones)
- `charexit`: Switch back to normal character
- Includes position tracking and zone validation

## Character Naming

- **Normal Character**: Original player name (e.g., "Luna")
- **PvP Character**: "(playername pvp)" format (e.g., "(Luna pvp)")

## Features

### Automatic PvP Character Creation
- Created immediately on login
- Gets warrior gear set by default
- Maximum blood quality (100%)
- Marked for instant switching capability

### Instant Switching
- No teleportation delays
- No zone restrictions (for `.ch immswap` and `.ch swap`)
- Immediate character activation/deactivation
- Position tracking for normal character

### State Management
- Tracks which character is currently active
- Maintains last normal position for return teleportation
- Handles character freezing/activation automatically
- Manages orphaned character cleanup

## Logging and Debugging

The system provides comprehensive logging:

- Login detection and dual state initialization
- PvP character creation with entity IDs
- Character switching operations with success/failure status
- Error handling with detailed exception logging

## Usage Example

```
[Luna logs in]
System: "New player detected: Luna (76561199507219786) - Setting up dual character state with immediate PvP character creation"
System: "Created PvP character '( Luna pvp)' for player"
System: "Successfully created PvP character for platformId 76561199507219786: 560193 - Ready for immediate swapping"

[Luna types: .ch immswap]
⚡ IMMEDIATE: Switched to PvP character '( Luna pvp)'

[Luna types: .ch immswap again]
⚡ IMMEDIATE: Switched back to normal character 'Luna'
```

## Technical Implementation

### Core Components

1. **PlayerLoginPatch**: Handles automatic dual character setup on login
2. **DualCharacterManager**: Manages character state and switching operations
3. **PvPCharacterSpawner**: Creates and configures PvP practice characters
4. **CharacterCommand**: Provides user commands for character management

### Entity Management

- Normal characters remain active in the world
- PvP characters are frozen (teleported underground) when not in use
- Instant activation/deactivation through network component management
- Proper cleanup of orphaned characters

## Troubleshooting

### PvP Character Not Found
- Use `.ch immswap` - it will automatically create the PvP character if missing
- Check server logs for creation errors
- Verify player has valid user and character entities

### Swap Fails
- Ensure both user and character entities are valid
- Check if dual character state exists (`.ch immswap` creates it automatically)
- Review server logs for specific error messages

### Character Not Switching
- Verify PvP character entity exists and is valid
- Check network component availability
- Ensure character is not in a protected state

## Server Configuration

The system integrates with existing VAuto arena configuration:
- Uses arena center coordinates for PvP character teleportation
- Respects zone restrictions for arena-specific commands
- Compatible with existing zone management services

## Performance Considerations

- Character switching is instantaneous
- No memory leaks from character creation/destruction
- Automatic cleanup of orphaned entities
- Efficient entity query usage with proper disposal

This system provides a seamless dual-character experience for VRising servers, enabling instant switching between normal and PvP practice characters with proper state management and error handling.