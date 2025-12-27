# VAuto Arena System - Command Reference

## 📚 Table of Contents
1. [Command Syntax](#command-syntax)
2. [Core Commands](#core-commands)
3. [Arena Commands](#arena-commands)
4. [Character Commands](#character-commands)
5. [Service Commands](#service-commands)
6. [Utility Commands](#utility-commands)
7. [Admin Commands](#admin-commands)
8. [Debug Commands](#debug-commands)
9. [Spawn Commands](#spawn-commands)
10. [Logistics Commands](#logistics-commands)
11. [Automation Commands](#automation-commands)
12. [Analytics Commands](#analytics-commands)
13. [AI Assistant Commands](#ai-assistant-commands)
14. [Command Shortcuts](#command-shortcuts)

---

## 📝 Command Syntax

### Basic Structure
```
[prefix][command] [subcommand] [arguments]
```

### Common Prefixes
- `.` - Standard command prefix
- `/` - Alternative prefix (if configured)
- No prefix - Direct commands in some contexts

### Syntax Rules
- **Required parameters**: `[parameter]`
- **Optional parameters**: `[parameter]`
- **Choices**: `option1|option2`
- **Multiple values**: `param1 param2 param3`
- **Ranges**: `1-10`

### Permission Levels
- **Public** - Available to all players
- **Admin** - Server administrators only
- **Developer** - Development and debugging
- **Restricted** - Special authorization required

---

## 🔧 Core Commands

### Help System
**`.help [topic]`** - Show help information
- **Public**
- Shows available commands and usage
- Examples:
  ```
  .help                    # General help
  .help arena              # Arena-specific help
  .help character          # Character command help
  .help shortcuts          # Show all shortcuts
  ```

### System Status
**`.system [service|all]`** - Check system status
- **Admin**
- Shows status of various system components
- Examples:
  ```
  .system all              # All systems status
  .system mapicon          # Map icon service
  .system arena            # Arena service
  .system gamesystem       # Game systems
  ```

---

## ⚔️ Arena Commands

### Basic Arena Operations

**`.arena enter`** - Enter arena
- **Admin**
- Enters the arena with full unlocks
- Saves current state automatically
- Example: `.arena enter`

**`.arena exit`** - Exit arena
- **Admin**
- Restores original state
- Returns to pre-arena location
- Example: `.arena exit`

**`.arena status`** - Arena status
- **Admin**
- Shows current arena state and configuration
- Example: `.arena status`

**`.arena heal`** - Heal to full health
- **Admin**
- Restores health to maximum
- Works inside and outside arena
- Example: `.arena heal`

**`.arena loadout`** - Apply default loadout
- **Admin**
- Applies configured gear set
- Example: `.arena loadout`

**`.arena reset`** - Reset player state
- **Admin**
- Clears all arena effects
- Restores normal state
- Example: `.arena reset`

### Arena Management

**`.arena practice`** - Toggle practice mode
- **Admin**
- Switches to PvP character
- Enables arena mode
- Example: `.arena practice`

**`.arena spawnvamp <boss_name> [x] [y] [z]`** - Spawn VBlood boss
- **Admin**
- Spawns specified VBlood boss for testing
- Examples:
  ```
  .arena spawnvamp Dracula
  .arena spawnvamp Keely 1000 50 2000
  ```

**`.arena babyblood [x] [y] [z]`** - Spawn training VBlood
- **Admin**
- Spawns low-level training boss
- Perfect for testing arena mechanics
- Example: `.arena babyblood`

**`.arena despawnvamp <boss_name>`** - Despawn VBlood boss
- **Admin**
- Removes specified VBlood boss
- Example: `.arena despawnvamp Dracula`

### Arena Suggestion System

**`.arena suggest [category]`** - Get arena suggestions
- **Public**
- Smart suggestion engine for arena features
- Categories: visual, blood, build, buffs, spawns
- Examples:
  ```
  .arena suggest           # Show all categories
  .arena suggest visual    # Visual suggestions
  .arena suggest blood     # Blood-related suggestions
  ```

**`.a [suggestion|number]`** - Arena shortcut commands
- **Public**
- Quick access to common arena actions
- Examples:
  ```
  .a 1                     # First shortcut (spawn glow)
  .a glow                  # Apply glow effect
  .a border on             # Enable arena border
  ```

---

## 👤 Character Commands

### Character Information

**`.character info [player]`** - Character information
- **Public**
- Shows detailed character data
- Examples:
  ```
  .character info          # Your info
  .character info PlayerName # Other player's info
  ```

**`.character stats [player]`** - Character statistics
- **Public**
- Shows level, experience, abilities
- Examples:
  ```
  .character stats         # Your stats
  .character stats PlayerName # Other player's stats
  ```

**`.character position [player]`** - Position information
- **Public**
- Shows coordinates and zone
- Examples:
  ```
  .character position      # Your position
  .character position PlayerName # Other player's position
  ```

**`.character health [player]`** - Health status
- **Public**
- Shows current and max health
- Examples:
  ```
  .character health        # Your health
  .character health PlayerName # Other player's health
  ```

### Character Actions

**`.character teleport <x> <y> <z|player>`** - Teleport character
- **Admin**
- Teleport to coordinates or player
- Examples:
  ```
  .character teleport 1000 50 2000
  .character teleport PlayerName
  ```

**`.character reset [player]`** - Reset character state
- **Admin**
- Resets position, health, and effects
- Example: `.character reset PlayerName`

**`.character spawn <prefab_name>`** - Spawn character prefab
- **Admin**
- Creates character-like entities
- Examples:
  ```
  .character spawn vampire_e
  .character spawn list     # Show available prefabs
  ```

### Dual Character System

**`.charswap`** - Swap between characters
- **Public**
- Instant switch between normal and PvP character
- Example: `.charswap`

**`.char create`** - Create PvP character
- **Public**
- Creates arena practice character
- Example: `.char create`

**`.charstatus`** - Show dual character status
- **Public**
- Detailed status of both characters
- Example: `.charstatus`

**`.char reset [player]`** - Reset dual character system
- **Admin**
- Clears dual character setup
- Example: `.char reset PlayerName`

### Character Lists

**`.character list [filter]`** - List online players
- **Public**
- Shows all connected players
- Examples:
  ```
  .character list          # All players
  .character list admin    # Filter by name
  ```

**`.character online <player>`** - Check online status
- **Public**
- Verifies if player is connected
- Example: `.character online PlayerName`

---

## 🔧 Service Commands

### Service Management

**`.service enable <servicename>`** - Enable service
- **Admin**
- Activates specified service
- Examples:
  ```
  .service enable mapicon
  .service enable arena
  ```

**`.service disable <servicename>`** - Disable service
- **Admin**
- Deactivates specified service
- Examples:
  ```
  .service disable mapicon
  .service disable arena
  ```

**`.service restart <servicename>`** - Restart service
- **Admin**
- Restarts specified service
- Examples:
  ```
  .service restart mapicon
  .service restart gamesystem
  .service restart respawn
  ```

**`.service status <servicename>`** - Service status
- **Admin**
- Shows service health and statistics
- Examples:
  ```
  .service status mapicon
  .service status all
  ```

### Map Icon Management

**`.mapicon refresh`** - Refresh player icons
- **Admin**
- Manually updates all map icons
- Example: `.mapicon refresh`

**`.mapicon clear`** - Clear all map icons
- **Admin**
- Removes all player icons
- Example: `.mapicon clear`

**`.mapicon status`** - Map icon status
- **Admin**
- Shows map icon service information
- Example: `.mapicon status`

**`.mapicon toggle <true|false>`** - Toggle map icons
- **Admin**
- Enable/disable map icon system
- Examples:
  ```
  .mapicon toggle true
  .mapicon toggle false
  ```

### Game System Management

**`.gamesystem status`** - Game system status
- **Admin**
- Shows VBlood hook status
- Example: `.gamesystem status`

**`.gamesystem clearhooks`** - Clear all VBlood hooks
- **Admin**
- Removes all active VBlood hooks
- Example: `.gamesystem clearhooks`

**`.gamesystem check <platform_id>`** - Check player hook
- **Admin**
- Checks if specific player is hooked
- Example: `.gamesystem check 76561198000000000`

### Respawn Management

**`.respawn set <player> <duration>`** - Set respawn cooldown
- **Admin**
- Prevents player respawning temporarily
- Example: `.respawn set 76561198000000000 30`

**`.respawn clear <player>`** - Clear respawn cooldown
- **Admin**
- Removes respawn prevention
- Example: `.respawn clear 76561198000000000`

**`.respawn check <player>`** - Check respawn status
- **Admin**
- Shows if respawn is prevented
- Example: `.respawn check 76561198000000000`

**`.respawn cleanup`** - Cleanup expired cooldowns
- **Admin**
- Removes old respawn cooldowns
- Example: `.respawn cleanup`

---

## 🛠️ Utility Commands

### Teleportation

**`.teleport to <x> <y> <z>`** - Teleport to coordinates
- **Admin**
- Instant teleportation to location
- Example: `.teleport to 1000 50 2000`

**`.teleport save <name>`** - Save current location
- **Admin**
- Saves position for quick return
- Example: `.teleport save home`

**`.teleport goto <name>`** - Teleport to saved location
- **Admin**
- Returns to saved position
- Example: `.teleport goto home`

**`.teleport list`** - List saved locations
- **Admin**
- Shows all saved positions
- Example: `.teleport list`

### Quick Actions

**`.tp <target>`** - Quick teleport
- **Public**
- Shortcut for teleportation
- Examples:
  ```
  .tp 1000 50 2000      # Teleport to coordinates
  .tp PlayerName        # Teleport to player
  ```

**`.pos [target]`** - Quick position check
- **Public**
- Shows current position
- Examples:
  ```
  .pos                  # Your position
  .pos PlayerName       # Other player's position
  ```

**`.hp [target]`** - Quick health check
- **Public**
- Shows health status
- Examples:
  ```
  .hp                   # Your health
  .hp PlayerName        # Other player's health
  ```

**`.stats [target]`** - Quick stats check
- **Public**
- Shows character statistics
- Examples:
  ```
  .stats                # Your stats
  .stats PlayerName     # Other player's stats
  ```

**`.inv [target]`** - Quick inventory check
- **Public**
- Shows inventory analysis
- Examples:
  ```
  .inv                  # Your inventory
  .inv PlayerName       # Other player's inventory
  ```

**`.list [filter]`** - Quick player list
- **Public**
- Lists online players
- Examples:
  ```
  .list                 # All players
  .list admin          # Filtered list
  ```

**`.online [target]`** - Quick online check
- **Public**
- Checks online status
- Examples:
  ```
  .online               # Check self
  .online PlayerName    # Check player
  ```

---

## 🔐 Admin Commands

### Build System

**`.build mode`** - Toggle build mode
- **Admin**
- Enables/disables building system
- Example: `.build mode`

**`.build list`** - Show available schematics
- **Admin**
- Lists all buildable objects
- Example: `.build list`

**`.build select <schematic>`** - Select active schematic
- **Admin**
- Choose what to build
- Example: `.build select castle_wall`

**`.build place`** - Place current schematic
- **Admin**
- Places selected build
- Example: `.build place`

**`.build remove`** - Remove object at position
- **Admin**
- Deletes objects at target location
- Example: `.build remove`

**`.build surface <material>`** - Set surface material
- **Admin**
- Changes building surface
- Example: `.build surface stone`

### Zone Management

**`.zone setzonehere <name> <radius> <x> <y> <z>`** - Create zone
- **Admin**
- Creates arena zone at current position
- Example: `.zone setzonehere main_arena 100 -1000 5 -500`

**`.zone setcenter <x> <y> <z>`** - Set zone center
- **Admin**
- Changes zone center coordinates
- Example: `.zone setcenter -1000 5 -500`

**`.zone setradius <radius>`** - Set zone radius
- **Admin**
- Changes zone detection radius
- Example: `.zone setradius 150`

**`.zone setspawn <x> <y> <z>`** - Set zone spawn
- **Admin**
- Changes arena spawn point
- Example: `.zone setspawn -1000 5 -500`

**`.zone info`** - Zone information
- **Admin**
- Shows current zone configuration
- Example: `.zone info`

**`.zone reload`** - Reload zone configuration
- **Admin**
- Reloads zone settings from file
- Example: `.zone reload`

### Castle Commands

**`.castle setheart [radius]`** - Set castle heart
- **Admin**
- Configures castle territory
- Example: `.castle setheart 50`

**`.castle radius <radius>`** - Set castle radius
- **Admin**
- Changes castle territory radius
- Example: `.castle radius 75`

**`.castle clear`** - Clear castle radius
- **Admin**
- Removes castle territory restrictions
- Example: `.castle clear`

**`.castle delete`** - Delete castle configuration
- **Admin**
- Removes all castle settings
- Example: `.castle delete`

**`.castle enhance <level>`** - Set enhancement level
- **Admin**
- Sets castle system enhancement
- Example: `.castle enhance 5`

**`.castle info`** - Castle system information
- **Admin**
- Shows castle configuration
- Example: `.castle info`

**`.castle status`** - Castle system status
- **Admin**
- Shows castle system health
- Example: `.castle status`

### Portal Commands

**`.portal create <name> <x> <y> <z>`** - Create portal
- **Admin**
- Creates teleportation portal
- Example: `.portal create myportal 1000 50 2000`

**`.portal goto <name>`** - Use portal
- **Admin**
- Teleports through named portal
- Example: `.portal goto myportal`

**`.portal list`** - List all portals
- **Admin**
- Shows all created portals
- Example: `.portal list`

**`.portal remove <name>`** - Remove portal
- **Admin**
- Deletes specified portal
- Example: `.portal remove myportal`

### Glow Commands

**`.glow add <color>`** - Add glow effect
- **Admin**
- Applies glow to objects/area
- Examples:
  ```
  .glow add blue
  .glow add red
  .glow add chaos
  ```

**`.glow remove`** - Remove glow effect
- **Admin**
- Removes active glow effects
- Example: `.glow remove`

---

## 🐛 Debug Commands

### Entity Debugging

**`.debug coi [entity]`** - Set center of interest
- **Admin**
- Sets entity for detailed tracking
- Examples:
  ```
  .debug coi              # Set nearest entity as COI
  .debug coi 12345        # Set specific entity as COI
  ```

**`.debug track`** - Track COI entity
- **Admin**
- Shows detailed COI tracking info
- Example: `.debug track`

**`.debug analyze`** - Analyze COI entity
- **Admin**
- Shows entity component analysis
- Example: `.debug analyze`

**`.debug clear`** - Clear COI session
- **Admin**
- Removes current COI tracking
- Example: `.debug clear`

**`.debug list [radius]`** - List nearby entities
- **Admin**
- Shows entities within radius
- Examples:
  ```
  .debug list             # Default 50 unit radius
  .debug list 100         # Custom radius
  ```

### Performance Monitoring

**`.debug performance`** - Performance statistics
- **Admin**
- Shows system performance metrics
- Example: `.debug performance`

### Character Stealing (Admin Only)

**`.stealchar <playerName> [force]`** - Steal character
- **Admin**
- Takes control of another player's character
- Examples:
  ```
  .stealchar PlayerName
  .stealchar PlayerName force
  ```

**`.returnchar <playerName>`** - Return stolen character
- **Admin**
- Returns character to original owner
- Example: `.returnchar PlayerName`

**`.liststolen`** - List stolen characters
- **Admin**
- Shows all stolen characters
- Example: `.liststolen`

---

## 🏗️ Spawn Commands

### Basic Spawning

**`.spawn <prefab> <rows> <cols> <x> <y> <spacing>`** - Spawn grid
- **Admin**
- Creates grid of objects
- Example: `.spawn MapIcon_CastleObject_BloodAltar 5 5 -1000 5 -500 10`

### Specialized Spawning

**`.spawncastle <type> <rows> <cols> <x> <y>`** - Spawn castle objects
- **Admin**
- Spawns castle building elements
- Types: wall, floor, tower, gate, throne, workbench, forge
- Example: `.spawncastle wall 3 3 -1000 5 -500`

**`.spawnfurniture <type> <rows> <cols> <x> <y>`** - Spawn furniture
- **Admin**
- Spawns furniture objects
- Types: sofa, chair, table, bed, tire, castor, wheel
- Example: `.spawnfurniture sofa 2 2 -1000 5 -500`

**`.spawndecor <type> <rows> <cols> <x> <y>`** - Spawn decorations
- **Admin**
- Spawns decorative objects
- Types: torch, candle, plant, statue, fountain, heart
- Example: `.spawndecor torch 4 4 -1000 5 -500`

**`.spawnfurniturex <type> <rows> <cols> <x> <y> [settings]`** - Enhanced furniture
- **Admin**
- Spawns furniture with custom settings
- Settings: color, material, size
- Example: `.spawnfurniturex castor 3 3 -1000 5 color=red,material=metal`

### Index Management

**`.createindex <name> <type> <x> <y> <radius>`** - Create spawn index
- **Admin**
- Creates reusable spawn location
- Example: `.createindex mycastle castle -1000 5 -500 100`

**`.listindexes`** - List all indexes
- **Admin**
- Shows all created spawn indexes
- Example: `.listindexes`

**`.spawnatindex <indexName> <prefab> <rows> <cols>`** - Spawn at index
- **Admin**
- Spawns using predefined index
- Example: `.spawnatindex mycastle MapIcon_CastleObject_BloodAltar 5 5`

### Tile Spawning

**`.spawntiles <tileNumber> <rows> <cols> <x> <y>`** - Spawn tiles
- **Admin**
- Spawns floor tiles by type
- Numbers: 1=Stone, 2=Wood, 3=Metal, 4=Grass, 5=Sand, 6=Water, 7=Dirt, 8=Cobblestone
- Example: `.spawntiles 1 10 10 -1000 5 -500`

### Plant System

**`.plants list [category]`** - List plants
- **Public**
- Shows available plant types
- Categories: all, decorative, crops, herbs, magical, trees, flowers, mushrooms, vines
- Examples:
  ```
  .plants list            # All plants
  .plants list crops      # Crop plants only
  ```

**`.plants spawn <plantName> [count] [x] [y] [z]`** - Spawn plants

- **Admin**
- Spawns specified plants
- Examples:

  ```
  .plants spawn wheat 5
  .plants spawn rose 3 -1000 5 -500
  ```

**`.plants info <plantName>`** - Plant information
- **Public**
- Shows detailed plant information
- Example: `.plants info sunflower`

**`.plants categories`** - Plant categories
- **Public**
- Lists all plant categories
- Example: `.plants categories`

---

## 🚀 Logistics Commands

### Conveyor System Management

**`.logistics conveyor [enable|disable|status|list|debug]`** - Conveyor system management
- **Public**
- Manage automated material flow between inventories
- Examples:
  ```
  .logistics conveyor enable    # Enable conveyor system for your territory
  .logistics conveyor disable   # Disable conveyor system
  .logistics conveyor status    # Show conveyor status
  .logistics conveyor list      # List active conveyor links
  .logistics conveyor debug     # Debug information (admin only)
  ```

**`.l co [enable|disable|status|list|debug]`** - Logistics shortcut
- **Public**
- Shortcut for conveyor commands
- Example: `.l co status`

### Conveyor System Features

- **Naming Convention**: Use `s#` for senders and `r#` for receivers (e.g., `s0`, `r0`)
- **Groups**: Same number connects senders to receivers (s0 → r0)
- **Buffer Management**: Maintains 5x per-craft requirement for inputs
- **Permissions**: Only works on territories owned by players who enable it

---

## 🤖 Automation Commands

### Script Management

**`.script run <name>`** - Execute automation script
- **Admin**
- Runs specified automation script
- Example: `.script run optimize_memory`

**`.script list`** - List available scripts
- **Admin**
- Shows all created scripts with status
- Example: `.script list`

**`.script create <name> <commands>`** - Create new script
- **Admin**
- Creates custom automation script
- Example: `.script create cleanup "batch cleanup memory all"`

**`.script delete <name>`** - Delete script
- **Admin**
- Removes specified script
- Example: `.script delete cleanup`

**`.script schedule <name> <time>`** - Schedule script execution
- **Admin**
- Schedules script to run at specified time
- Example: `.script schedule optimize "0 2 * * *"`

**`.script stop <name>`** - Stop running script
- **Admin**
- Terminates executing script
- Example: `.script stop optimize`

**`.script status <name>`** - Check script status
- **Admin**
- Shows script execution status
- Example: `.script status optimize`

### Workflow Automation

**`.workflow start <name>`** - Start workflow
- **Admin**
- Begins automated workflow execution
- Example: `.workflow start maintenance`

**`.workflow stop <name>`** - Stop workflow
- **Admin**
- Terminates workflow execution
- Example: `.workflow stop maintenance`

**`.workflow create <name> <steps>`** - Create workflow
- **Admin**
- Creates multi-step automation workflow
- Example: `.workflow create backup "script backup; report system"`

**`.workflow list`** - List workflows
- **Admin**
- Shows all available workflows
- Example: `.workflow list`

**`.workflow trigger <name>`** - Trigger workflow
- **Admin**
- Manually starts workflow execution
- Example: `.workflow trigger maintenance`

### Smart Actions

**`.smart heal [targets] [options]`** - Intelligent healing
- **Admin**
- Smart healing based on health thresholds
- Examples:
  ```
  .smart heal critical
  .smart heal all mode=smart
  ```

**`.smart balance [targets] [options]`** - Smart balancing
- **Admin**
- Intelligent resource balancing
- Examples:
  ```
  .smart balance players health
  .smart balance system performance
  ```

**`.smart optimize [targets] [options]`** - Smart optimization
- **Admin**
- AI-powered optimization suggestions
- Examples:
  ```
  .smart optimize system performance
  .smart optimize memory memory
  ```

**`.smart maintain [targets] [options]`** - Automated maintenance
- **Admin**
- Intelligent maintenance scheduling
- Examples:
  ```
  .smart maintain system daily
  .smart maintain database weekly
  ```

**`.smart analyze [targets] [options]`** - Smart analysis
- **Admin**
- Intelligent system analysis
- Examples:
  ```
  .smart analyze performance trends
  .smart analyze players behavior
  ```

### Batch Operations

**`.batch optimize [targets] [options]`** - Batch optimization
- **Admin**
- Apply optimization to multiple targets
- Examples:
  ```
  .batch optimize all performance
  .batch optimize players memory
  ```

**`.batch balance [targets] [options]`** - Batch balancing
- **Admin**
- Apply balancing to multiple targets
- Examples:
  ```
  .batch balance all health
  .batch balance servers load
  ```

**`.batch cleanup [targets] [options]`** - Batch cleanup
- **Admin**
- Clean up multiple targets
- Examples:
  ```
  .batch cleanup memory all
  .batch cleanup entities inactive
  ```

**`.batch update [targets] [options]`** - Batch updates
- **Admin**
- Apply updates to multiple targets
- Examples:
  ```
  .batch update all configurations
  .batch update players settings
  ```

**`.batch sync [targets] [options]`** - Batch synchronization
- **Admin**
- Synchronize multiple targets
- Examples:
  ```
  .batch sync all databases
  .batch sync players progress
  ```

### Conditional Logic

**`.if <condition> <action> [else_action]`** - Conditional execution
- **Admin**
- Execute commands based on conditions
- Examples:
  ```
  .if memory_usage > 80 "batch cleanup memory all"
  .if player_count > 30 "broadcast 'Server full'" "broadcast 'Space available'"
  ```

**`.when <trigger> <action> [cooldown]`** - Event triggers
- **Admin**
- Execute actions on specific events
- Examples:
  ```
  .when player_count > 50 "auto optimize"
  .when memory_usage > 90 "batch cleanup memory all" 300
  ```

---

## 📊 Analytics Commands

### Performance Analytics

**`.analytics performance [options]`** - Performance analysis
- **Admin**
- Comprehensive performance metrics and analysis
- Examples:
  ```
  .analytics performance
  .analytics performance detail=detailed timeframe=1h
  ```

**`.analytics players [options]`** - Player analytics
- **Admin**
- Player behavior and activity analysis
- Examples:
  ```
  .analytics players
  .analytics players metric=activity timeframe=24h
  ```

**`.analytics system [options]`** - System analytics
- **Admin**
- System health and performance analysis
- Examples:
  ```
  .analytics system
  .analytics system component=all detail=full
  ```

**`.analytics memory [options]`** - Memory analytics
- **Admin**
- Memory usage and optimization analysis
- Examples:
  ```
  .analytics memory
  .analytics memory detail=detailed
  ```

**`.analytics network [options]`** - Network analytics
- **Admin**
- Network performance and latency analysis
- Examples:
  ```
  .analytics network
  .analytics network timeframe=1h
  ```

**`.analytics entities [options]`** - Entity analytics
- **Admin**
- Entity system performance analysis
- Examples:
  ```
  .analytics entities
  .analytics entities detail=performance
  ```

**`.analytics trends [options]`** - Trend analysis
- **Admin**
- Historical trend analysis and forecasting
- Examples:
  ```
  .analytics trends
  .analytics trends timeframe=7d metric=performance
  ```

**`.analytics predictions [options]`** - Predictive analytics
- **Admin**
- AI-powered predictive analysis
- Examples:
  ```
  .analytics predictions
  .analytics predictions timeframe=24h
  ```

**`.analytics report [options]`** - Generate reports
- **Admin**
- Comprehensive analytics reporting
- Examples:
  ```
  .analytics report
  .analytics report format=detailed graphs=true
  ```

**`.analytics export [options]`** - Export data
- **Admin**
- Export analytics data for external analysis
- Examples:
  ```
  .analytics export
  .analytics export format=csv timeframe=7d
  ```

### Real-time Monitoring

**`.monitor start [target]`** - Start monitoring
- **Admin**
- Begin real-time monitoring session
- Examples:
  ```
  .monitor start
  .monitor start performance
  ```

**`.monitor stop [target]`** - Stop monitoring
- **Admin**
- End monitoring session and show summary
- Examples:
  ```
  .monitor stop
  .monitor stop performance
  ```

**`.monitor status [target]`** - Check monitoring status
- **Admin**
- Show current monitoring status
- Examples:
  ```
  .monitor status
  .monitor status all
  ```

**`.monitor alerts [action]`** - Alert management
- **Admin**
- Configure and manage monitoring alerts
- Examples:
  ```
  .monitor alerts enable
  .monitor alerts configure threshold=80
  ```

**`.monitor dashboard`** - Show dashboard
- **Admin**
- Display real-time monitoring dashboard
- Example: `.monitor dashboard`

### Data Mining

**`.datamine patterns [params]`** - Discover patterns
- **Admin**
- Mine data for behavioral and performance patterns
- Examples:
  ```
  .datamine patterns
  .datmine patterns timeframe=7d type=behavior
  ```

**`.datamine anomalies [params]`** - Detect anomalies
- **Admin**
- Identify system anomalies and unusual patterns
- Examples:
  ```
  .datamine anomalies
  .datamine anomalies sensitivity=high
  ```

**`.datamine correlations [params]`** - Find correlations
- **Admin**
- Discover correlations between different metrics
- Examples:
  ```
  .datamine correlations
  .datamine correlations metric1=performance metric2=memory
  ```

**`.datamine clusters [params]`** - Cluster analysis
- **Admin**
- Perform clustering analysis on data
- Examples:
  ```
  .datamine clusters
  .datamine clusters type=player_behavior
  ```

**`.datamine trends [params]`** - Mine trends
- **Admin**
- Extract trend information from data
- Examples:
  ```
  .datamine trends
  .datamine trends timeframe=30d
  ```

**`.datamine insights [params]`** - Generate insights
- **Admin**
- Generate actionable insights from data
- Examples:
  ```
  .datamine insights
  .datamine insights type=operational
  ```

### Predictive Analytics

**`.predict performance [timeframe]`** - Performance prediction
- **Admin**
- Predict future performance metrics
- Examples:
  ```
  .predict performance 2h
  .predict performance 1d
  ```

**`.predict load [timeframe]`** - Load prediction
- **Admin**
- Predict server load and capacity needs
- Examples:
  ```
  .predict load 4h
  .predict load 1w
  ```

**`.predict issues [timeframe]`** - Issue prediction
- **Admin**
- Predict potential system issues
- Examples:
  ```
  .predict issues 6h
  .predict issues 1d
  ```

**`.predict growth [timeframe]`** - Growth prediction
- **Admin**
- Predict player growth and resource needs
- Examples:
  ```
  .predict growth 1w
  .predict growth 1m
  ```

**`.predict optimal [timeframe]`** - Optimal configuration
- **Admin**
- Predict optimal configuration settings
- Examples:
  ```
  .predict optimal 2h
  .predict optimal performance
  ```

### Advanced Reporting

**`.report performance [params]`** - Performance reports
- **Admin**
- Generate detailed performance reports
- Examples:
  ```
  .report performance
  .report performance format=detailed graphs=true
  ```

**`.report player [params]`** - Player reports
- **Admin**
- Generate player behavior reports
- Examples:
  ```
  .report player
  .report player timeframe=7d detail=full
  ```

**`.report system [params]`** - System reports
- **Admin**
- Generate comprehensive system reports
- Examples:
  ```
  .report system
  .report system component=all
  ```

**`.report security [params]`** - Security reports
- **Admin**
- Generate security analysis reports
- Examples:
  ```
  .report security
  .report security timeframe=30d
  ```

**`.report usage [params]`** - Usage reports
- **Admin**
- Generate usage pattern reports
- Examples:
  ```
  .report usage
  .report usage metric=commands
  ```

**`.report health [params]`** - Health reports
- **Admin**
- Generate overall system health reports
- Examples:
  ```
  .report health
  .report health detail=comprehensive
  ```

**`.report custom [params]`** - Custom reports
- **Admin**
- Generate custom reports with specified parameters
- Examples:
  ```
  .report custom type=weekly_summary
  .report custom template=executive
  ```

---

## 🤖 AI Assistant Commands

### Natural Language Processing

**`.ask <question>`** - Ask AI assistant
- **Public**
- Ask natural language questions
- Examples:
  ```
  .ask How do I optimize server performance?
  .ask Which players are online?
  .ask What commands help with memory management?
  ```

**`.suggest [context]`** - Get intelligent suggestions
- **Public**
- Receive context-aware suggestions
- Examples:
  ```
  .suggest
  .suggest performance
  .suggest player management
  ```

**`.explain <concept> [detail_level]`** - Explain concepts
- **Public**
- Get detailed explanations of game concepts
- Examples:
  ```
  .explain arena
  .explain commands detailed
  .explain performance basic
  ```

**`.analyze <target> [aspect]`** - AI analysis
- **Admin**
- Perform AI-powered analysis
- Examples:
  ```
  .analyze system performance
  .analyze player behavior
  .analyze memory usage
  ```

### Smart Recommendations

**`.recommend <category> [parameters]`** - Get recommendations
- **Public**
- Receive AI-powered recommendations
- Examples:
  ```
  .recommend performance
  .recommend security
  .recommend players optimization
  ```

**`.optimize <target> [goal]`** - Get optimization suggestions
- **Admin**
- Receive AI optimization recommendations
- Examples:
  ```
  .optimize system performance
  .optimize memory usage
  .optimize network latency
  ```

### Predictive Intelligence

**`.predict <event> [timeframe]`** - Predict events
- **Admin**
- Get AI predictions about future events
- Examples:
  ```
  .predict performance 2h
  .predict load 4h
  .predict issues 1d
  ```

**`.anticipate <scenario>`** - Scenario planning
- **Admin**
- Get AI scenario analysis and planning
- Examples:
  ```
  .anticipate high load
  .anticipate memory issues
  .anticipate player surge
  ```

### Intelligent Automation

**`.auto <task> [parameters]`** - Intelligent automation
- **Admin**
- Execute AI-powered automated tasks
- Examples:
  ```
  .auto cleanup
  .auto optimize
  .auto balance
  ```

**`.learn <pattern> <action>`** - Teach AI
- **Admin**
- Teach AI new patterns and responses
- Examples:
  ```
  .learn "high load" "auto optimize"
  .learn "low memory" "batch cleanup memory"
  ```

### Conversational Interface

**`.chat <message>`** - Natural conversation
- **Public**
- Have natural conversation with AI
- Examples:
  ```
  .chat How can you help me today?
  .chat What's the server status?
  .chat Can you explain arena mode?
  ```

**`.helpai [topic]`** - AI help system
- **Public**
- Get help about AI assistant capabilities
- Examples:
  ```
  .helpai
  .helpai commands
  .helpai examples
  .helpai capabilities
  ```

---

## ⚡ Command Shortcuts

### Character Shortcuts
- `char` → `character`
- `c` → `character`
- `player` → `character`
- `p` → `character`

### Quick Action Shortcuts
- `tp` → `character teleport`
- `pos` → `character position`
- `loc` → `character position`
- `hp` → `character health`
- `stats` → `character stats`
- `inv` → `character inventory`
- `list` → `character list`
- `online` → `character online`

### Service Shortcuts
- `svc` → `service`
- `sys` → `system`
- `map` → `mapicon`
- `resp` → `respawn`
- `gs` → `gamesystem`

### Arena Shortcuts
- `arena` → `arena`
- `pvp` → `arena`

### Utility Shortcuts
- `help` → `help`

### Usage Examples
```
char i PlayerName         # character info PlayerName
tp 1000 50 2000          # character teleport 1000 50 2000
svc status map           # service status mapicon
arena enter              # arena enter
```

---

## 📖 Command Examples

### Complete Workflow Examples

#### Arena Practice Session
```bash
.char create              # Create PvP character
.arena enter             # Enter arena
.arena heal              # Ensure full health
.arena loadout           # Apply gear
# Practice with unlimited abilities
.arena exit              # Exit and restore state
.charswap                # Return to normal character
```

#### Character Management
```bash
.char info PlayerName     # Check player info
.char teleport PlayerName # Teleport to player
.char position PlayerName # Check player position
.char health PlayerName   # Check player health
.char list               # List all players
```

#### Server Administration
```bash
.system all              # Check all systems
.service restart arena   # Restart arena service
.map refresh             # Refresh map icons
.debug performance       # Check performance
.arena spawnvamp Dracula # Spawn test boss
```

#### Object Spawning
```bash
.spawn MapIcon_CastleObject_BloodAltar 5 5 -1000 5 -500 10
.spawncastle wall 3 3 -1000 5 -500
.spawnfurniture sofa 2 2 -1000 5 -500
.plants spawn wheat 10
```

---

*This command reference covers all available commands in the VAuto Arena System. For more detailed information about specific commands, use the `.help` command in-game.*
