# VAuto Arena System - User Guide

## 🎮 Table of Contents
1. [Getting Started](#getting-started)
2. [Arena System](#arena-system)
3. [Character Management](#character-management)
4. [Command Reference](#command-reference)
5. [Tips and Tricks](#tips-and-tricks)
6. [FAQ](#faq)

---

## 🚀 Getting Started

### What is the VAuto Arena System?
The VAuto Arena System is a comprehensive mod for V Rising that provides:
- **Automated Arena Management** - Enter/exit arenas with instant state changes
- **Dual Character System** - Switch between normal and PvP characters instantly
- **Global Map Icons** - See all players on the map in real-time
- **Advanced Commands** - Powerful tools for gameplay and administration

### Basic Concepts
- **Arena Zones** - Special areas where PvP practice happens
- **Character Snapshots** - Your state is saved before entering arenas
- **Instant Switching** - Change characters without relogging
- **Auto-Entry** - Automatic arena detection and entry

---

## ⚔️ Arena System

### Entering an Arena

#### Automatic Entry
When you walk into an arena zone, the system automatically:
1. Saves your current state (inventory, equipment, progression)
2. Applies arena character with maxed stats
3. Unlocks all abilities and research
4. Heals you to full health
5. Teleports you to arena spawn

#### Manual Entry
Use commands to control arena entry:
```bash
.arena enter      # Enter the arena manually
.arena exit       # Exit and restore your state
.arena status     # Check if you're in arena
.arena heal       # Heal to full health
.arena loadout    # Apply default gear
```

### What Happens in Arena
- **All Research Unlocked** - Access every technology and recipe
- **Max Blood Quality** - All blood types at 100% effectiveness
- **Full Ability Access** - Every spell and ability available
- **Gear Sets** - Pre-configured equipment loadouts
- **Instant Respawn** - No death penalties in practice mode

### Exiting Arena
When you exit:
1. Your original state is restored
2. Inventory and equipment return to pre-arena state
3. Progression returns to normal
4. You're teleported back to your original location

---

## 👤 Character Management

### Dual Character System

#### Creating a PvP Character
```bash
.char create      # Create your PvP practice character
.charstatus       # Check your character status
```

Your PvP character gets:
- **Max Level** - Level 91 (maximum)
- **Dracula Build** - Everything maxed out
- **All Research** - Complete technology tree
- **Special Name** - "(YourName pvp)" format

#### Switching Characters
```bash
.charswap         # Instant switch between characters
.char enter       # Switch to PvP character (arena mode)
.char exit        # Switch to normal character
```

**Benefits of Instant Switching:**
- No logout/login required
- Maintains position in world
- Instant availability
- No cooldown or restrictions

#### Character Status
```bash
.charstatus       # Detailed character information
.char info        # Show character details
.char list        # List all online players
```

---

## 📋 Command Reference

### Essential Commands

#### Quick Actions
```bash
.help                    # Show all available commands
.tp 1000 50 2000        # Teleport to coordinates
.pos                    # Show your current position
.hp                     # Check health status
.stats                  # View character statistics
.list                   # List online players
.online PlayerName      # Check if player is online
```

#### Arena Management
```bash
.arena enter            # Enter arena
.arena exit             # Exit arena
.arena status           # Arena status
.arena heal             # Full heal
.arena loadout          # Apply gear
.arena reset            # Reset player state
.arena spawnvamp BossName # Spawn VBlood boss
.arena babyblood        # Spawn training VBlood
```

#### Character Commands
```bash
.char create            # Create PvP character
.charswap              # Switch characters
.char enter             # Enter PvP character
.char exit              # Exit PvP character
.char status            # Character status
.char info [player]     # Character info
.char teleport [x y z]  # Teleport character
```

#### Service Management (Admin)
```bash
.service status         # Check service status
.service restart [name] # Restart service
.map refresh           # Refresh map icons
.sys all               # System status overview
```

### Advanced Commands

#### Build System
```bash
.build mode            # Toggle build mode
.build list            # Show available schematics
.build select [name]   # Select schematic
.build place           # Place current schematic
```

#### Zone Management
```bash
.zone setzonehere [name] [radius] # Create zone here
.zone info             # Zone information
.zone reload           # Reload zone config
```

#### Debug Commands (Admin)
```bash
.debug coi             # Set center of interest
.debug track           # Track COI entity
.debug analyze         # Analyze entity
.debug list [radius]   # List nearby entities
.debug performance     # Performance stats
```

#### Spawn Commands
```bash
.spawn [prefab] [rows] [cols] [x] [y] [spacing]
.spawncastle [type] [rows] [cols] [x] [y]
.spawnfurniture [type] [rows] [cols] [x] [y]
.spawndecor [type] [rows] [cols] [x] [y]
```

---

## 💡 Tips and Tricks

### Arena Entry Strategies

#### For PvP Practice
1. Create your PvP character first: `.char create`
2. Enter arena: `.arena enter`
3. Practice with unlimited abilities
4. Exit when done: `.arena exit`

#### For Testing Builds
1. Configure loadouts in builds.json
2. Enter arena: `.arena enter`
3. Test gear: `.arena loadout`
4. Experiment freely
5. Restore state: `.arena exit`

### Character Management Tips

#### Efficient Character Switching
- Use `.charswap` for instant switching
- No need to logout/login
- Maintains your position
- Perfect for quick practice sessions

#### Status Monitoring
- Use `.charstatus` to monitor both characters
- Check `.arena status` for arena state
- Use `.service status` for system health

### Map and Navigation

#### Using Map Icons
- All players show as map icons
- Updates every 3 seconds automatically
- Different colors for different zones
- Use for finding other players

#### Teleportation
- Use `.tp x y z` for quick travel
- Save locations with `.teleport save`
- Return to saved spots with `.teleport goto`

### Administration Tips

#### Server Management
- Monitor with `.sys all`
- Restart services if needed: `.service restart [name]`
- Check logs for issues
- Use debug commands sparingly

#### Player Management
- List players: `.list`
- Check status: `.char online [name]`
- Teleport to players: `.char teleport [player]`

---

## ❓ Frequently Asked Questions

### General Questions

**Q: Do I lose my progress when entering arenas?**
A: No! Your state is completely saved and restored. You keep all your progress, items, and research.

**Q: Can I use arenas while offline?**
A: No, you need to be online and connected to the server.

**Q: Are there any cooldowns for arena entry?**
A: No cooldowns. You can enter and exit as often as you want.

**Q: What happens if the server restarts while I'm in an arena?**
A: Your state is automatically saved and restored when you reconnect.

### Character Questions

**Q: How do I create a PvP character?**
A: Use `.char create` and the system will automatically create one for you.

**Q: Can I have multiple PvP characters?**
A: No, each player can only have one PvP character.

**Q: What happens to my PvP character if I delete it?**
A: You can recreate it with `.char create`, but you'll lose any changes made to it.

**Q: Can other players see my PvP character?**
A: Only you can see and control your PvP character.

### Technical Questions

**Q: Why don't I see map icons?**
A: Check that the map icon service is enabled: `.service status mapicon`

**Q: Commands aren't working, what should I do?**
A: Make sure you're using the correct prefix (usually `.`) and have proper permissions.

**Q: The arena system seems slow, how can I fix this?**
A: Check server performance with `.debug performance` and ensure you have sufficient resources.

### Gameplay Questions

**Q: Can I fight other players in arenas?**
A: Yes, arenas are designed for PvP practice and testing.

**Q: Do I get experience in arenas?**
A: No, arenas are for practice only and don't affect your main character progression.

**Q: Can I customize my arena character?**
A: Yes, you can modify the builds.json file to customize gear and abilities.

**Q: How do I practice specific builds?**
A: Create different loadouts in builds.json and apply them with `.arena loadout`

### Troubleshooting

**Q: I can't enter the arena, what should I check?**
A: 
1. Check `.arena status` to see if you're already in
2. Verify zone is configured: `.zone info`
3. Try restarting arena services: `.service restart arena`

**Q: My character won't swap, what's wrong?**
A:
1. Ensure you've created a PvP character: `.char create`
2. Check character status: `.charstatus`
3. Restart character service: `.service restart character`

**Q: Map icons aren't updating, how do I fix this?**
A:
1. Check map icon service: `.service status mapicon`
2. Manual refresh: `.map refresh`
3. Restart service: `.service restart mapicon`

---

## 🎯 Advanced Usage

### Custom Loadouts
Edit `config/VAuto.Arena/builds.json` to create custom gear sets:
```json
{
  "mycustombuild": {
    "name": "My Custom Build",
    "gear": {
      "weapon": "Custom Weapon Name",
      "armor": "Custom Armor Set",
      "accessories": ["Custom Ring", "Custom Amulet"]
    },
    "blood": "Custom Blood Type",
    "abilities": ["Custom Ability 1", "Custom Ability 2"]
  }
}
```

Apply with: `.arena loadout mycustombuild`

### Multiple Arena Zones
Create different zones for different activities:
- **Practice Zone** - Safe testing area
- **PvP Zone** - Competitive arena
- **Build Test Zone** - Equipment testing

### Performance Optimization
For better server performance:
- Reduce update intervals in configuration
- Limit maximum tracked players
- Disable features you don't need

---

*Need more help? Check the Installation Guide, API Documentation, or contact support.*