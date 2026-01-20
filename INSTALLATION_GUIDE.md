# VAuto Arena System - Installation & Setup Guide

## 📋 Table of Contents
1. [Prerequisites](#prerequisites)
2. [Installation Steps](#installation-steps)
3. [Configuration](#configuration)
4. [Initial Setup](#initial-setup)
5. [Testing Installation](#testing-installation)
6. [Troubleshooting](#troubleshooting)
7. [Advanced Configuration](#advanced-configuration)

---

## 🔧 Prerequisites

### System Requirements
- **V Rising Server** (Dedicated Server recommended)
- **BepInEx 5.4+** for V Rising
- **.NET 6.0 Runtime** (usually included with V Rising)
- **Administrator/Server Admin Access**
- **Minimum 2GB RAM** available for the mod

### Required Dependencies
- **VampireCommandFramework** - Command processing
- **ProjectM** - V Rising framework integration
- **Unity ECS** - Entity Component System support
- **Newtonsoft.Json** - Configuration and data handling

---

## 📦 Installation Steps

### Step 1: Install BepInEx
1. Download **BepInEx 5.4+** for V Rising
2. Extract to your V Rising server directory:
   ```
   VRisingServer/
   ├── BepInEx/
   ├── VRisingServer.exe
   └── [other V Rising files]
   ```
3. Start and stop the server once to generate BepInEx configuration

### Step 2: Install VAuto Arena System
1. Download the latest **VAuto Arena System** release
2. Copy `VAuto.dll` to your plugins directory:
   ```
   VRisingServer/
   └── BepInEx/
       └── plugins/
           └── VAuto.dll
   ```
3. Copy any additional resources:
   ```
   VRisingServer/
   └── BepInEx/
       └── config/
           └── VAuto/
               ├── arena_zones.json
               ├── builds.json
               └── arena_config.json
   ```

### Step 3: Verify Installation
1. Start your V Rising server
2. Check for these log entries:
   ```
   [VAuto] VAuto Arena System loaded successfully
   [VAuto] All services initialized
   [VAuto] Command system ready
   ```

---

## ⚙️ Configuration

### Basic Configuration File
Location: `BepInEx/config/gg.vautomation.arena.cfg`

```ini
[General]
# Enable/disable the entire system
Enabled = true

# Logging level (Debug, Info, Warning, Error)
LogLevel = Info

[Arena]
# Default arena center coordinates
ArenaCenterX = -1000
ArenaCenterY = 5
ArenaCenterZ = -500

# Arena radius for automatic detection
ArenaRadius = 100

# Enable automatic arena entry
AutoEnter = true

[Commands]
# Enable/disable command system
Enabled = true

# Admin only commands
AdminOnly = true

# Command prefix
Prefix = .

[Database]
# Enable database persistence
EnableDatabase = true

# Database file path
DatabasePath = BepInEx/config/VAuto/Database.db

# Fallback to JSON if database fails
EnableJsonFallback = true
```

### Advanced Configuration Options

#### Map Icon System
```ini
[GlobalMapIconService]
Enabled = true
UpdateInterval = 3.0
PrefabName = MapIcon_CastleObject_BloodAltar
ShowNormalPlayers = true
ShowArenaPlayers = true
ShowPvPPlayers = true
```

#### Dual Character System
```ini
[DualCharacter]
Enabled = true
AutoCreate = true
PvPCharacterPrefix = "(playername pvp)"
InstantSwap = true
```

#### Performance Settings
```ini
[Performance]
# Update intervals (in seconds)
PositionCheckInterval = 3.0
MapIconUpdateInterval = 3.0
ServiceUpdateInterval = 5.0

# Maximum entities to track
MaxTrackedPlayers = 100
MaxMapIcons = 200
```

---

## 🏗️ Initial Setup

### Step 1: Configure Arena Zone
1. Connect to your server as admin
2. Use the zone setup command:
   ```
   .zone setzonehere main_arena 100 -1000 5 -500
   ```
3. Verify zone creation:
   ```
   .zone info
   ```

### Step 2: Set Default Build
1. Create or edit the builds.json file:
   ```json
   {
     "default": {
       "name": "Default Arena Build",
       "gear": {
         "weapon": "Legendary Sword",
         "armor": "Full Plate",
         "accessories": ["Ring of Power", "Amulet of Strength"]
       },
       "blood": "Rogue Blood 100%",
       "abilities": ["Shadow Dash", "Veil of Mist", "Blood Surge"]
     }
   }
   ```
2. Apply the build:
   ```
   .arena loadout
   ```

### Step 3: Test Arena Entry
1. Enter the arena:
   ```
   .arena enter
   ```
2. Check your status:
   ```
   .arena status
   ```
3. Exit the arena:
   ```
   .arena exit
   ```

### Step 4: Setup Dual Characters (Optional)
1. Create PvP character:
   ```
   .char create
   ```
2. Test character swap:
   ```
   .charswap
   ```
3. Check status:
   ```
   .charstatus
   ```

---

## ✅ Testing Installation

### Basic Functionality Tests

#### Test 1: Core Commands
```bash
.help                    # Should show available commands
.arena status           # Should show arena system status
.char status            # Should show character information
.service status         # Should show service status
```

#### Test 2: Arena System
```bash
.arena enter            # Should enter arena successfully
.arena heal             # Should heal to full health
.arena loadout          # Should apply default build
.arena exit             # Should exit and restore state
```

#### Test 3: Map Icons
```bash
.service status mapicon # Should show map icon service active
.map refresh           # Should manually refresh player icons
```

#### Test 4: Dual Characters
```bash
.char create            # Should create PvP character
.charswap              # Should switch between characters
.charstatus            # Should show dual character info
```

### Expected Results
- All commands should respond with success messages
- Arena entry should unlock all abilities and research
- Map should show player icons updating every 3 seconds
- Character swap should be instantaneous

---

## 🐛 Troubleshooting

### Common Issues

#### Issue: Commands Not Working
**Symptoms:** Commands return "Unknown command" errors
**Solutions:**
1. Check if admin privileges are enabled
2. Verify command prefix (default: `.`)
3. Restart the server after installation
4. Check logs for command registration errors

#### Issue: Arena Entry Fails
**Symptoms:** `.arena enter` fails or hangs
**Solutions:**
1. Verify arena zone is configured: `.zone info`
2. Check if already in arena: `.arena status`
3. Restart arena services: `.service restart arena`
4. Check server logs for specific errors

#### Issue: Map Icons Not Showing
**Symptoms:** No player icons appear on map
**Solutions:**
1. Enable map icon service: `service enable mapicon`
2. Check update interval: Should be 3.0 seconds
3. Verify prefab exists: `MapIcon_CastleObject_BloodAltar`
4. Restart map service: `.service restart mapicon`

#### Issue: Dual Character Creation Fails
**Symptoms:** `.char create` fails or PvP character not found
**Solutions:**
1. Check if dual character system is enabled
2. Verify character entity exists
3. Restart character service: `.service restart character`
4. Check player login state

#### Issue: Database Errors
**Symptoms:** Data not persisting, JSON fallback errors
**Solutions:**
1. Check database path permissions
2. Verify database file is not corrupted
3. Enable JSON fallback: `EnableJsonFallback = true`
4. Check available disk space

### Log Analysis

#### Enable Debug Logging
Add to configuration:
```ini
[General]
LogLevel = Debug
```

#### Common Log Messages
- `[VAuto] VAuto Arena System loaded successfully` - ✅ System loaded
- `[VAuto] All services initialized` - ✅ Services ready
- `[VAuto] Command system ready` - ✅ Commands available
- `[VAuto] ERROR: [message]` - ❌ Check error details
- `[VAuto] WARNING: [message]` - ⚠️ Investigate warning

### Getting Help

####收集调试信息
1. Enable debug logging
2. Reproduce the issue
3. Collect relevant log sections
4. Note configuration changes
5. List installed mods/plugins

#### Support Channels
- **GitHub Issues**: Report bugs and feature requests
- **Discord**: Community support and discussion
- **Documentation**: Check this guide and API docs

---

## 🔧 Advanced Configuration

### Custom Arena Zones
Create multiple arena zones for different activities:

```json
{
  "zones": [
    {
      "name": "main_arena",
      "center": {"x": -1000, "y": 5, "z": -500},
      "radius": 100,
      "type": "pvp",
      "autoEnter": true
    },
    {
      "name": "practice_arena", 
      "center": {"x": -1200, "y": 5, "z": -500},
      "radius": 50,
      "type": "practice",
      "autoEnter": false
    }
  ]
}
```

### Custom Loadouts
Define multiple character builds:

```json
{
  "warrior": {
    "name": "Arena Warrior",
    "gear": {
      "weapon": "Legendary Greatsword",
      "armor": "Heavy Plate",
      "accessories": ["Warrior's Ring", "Guardian's Amulet"]
    },
    "blood": "Warrior Blood 100%",
    "abilities": ["Charge", "Ground Smash", "War Cry"]
  },
  "rogue": {
    "name": "Arena Rogue", 
    "gear": {
      "weapon": "Shadow Daggers",
      "armor": "Leather Armor",
      "accessories": ["Assassin's Ring", "Swift Boots"]
    },
    "blood": "Rogue Blood 100%",
    "abilities": ["Shadow Dash", "Veil of Mist", "Poison Strike"]
  }
}
```

### Performance Tuning
For high player count servers:

```ini
[Performance]
PositionCheckInterval = 5.0        # Reduce frequency
MapIconUpdateInterval = 5.0        # Reduce frequency  
ServiceUpdateInterval = 10.0       # Reduce frequency
MaxTrackedPlayers = 50             # Limit players
MaxMapIcons = 100                  # Limit icons
```

### Database Optimization
For large datasets:

```ini
[Database]
EnableDatabase = true
DatabasePath = BepInEx/config/VAuto/Database.db
EnableJsonFallback = true
EnableMigration = true
ConnectionTimeout = 30
CommandTimeout = 60
```

---

## 📚 Next Steps

After successful installation:

1. **Read the User Guide** (`USER_GUIDE.md`) for gameplay instructions
2. **Check Command Reference** (`COMMAND_REFERENCE.md`) for all available commands
3. **Review API Documentation** (`API_DOCUMENTATION.md`) for developers
4. **Configure Advanced Features** as needed for your server

---

*For additional help, consult the troubleshooting section or contact support.*