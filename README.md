# VAuto Arena System

[![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)](https://github.com/Coyoteq1/VAutomationVrising)[![V Rising](https://img.shields.io/badge/game-V%20Rising-red.svg)](https://vrisinggame.com/)

A comprehensive arena automation system for V Rising servers, featuring complete VBlood unlock integration, dual character management, automated PvP zones, real-time map tracking, and extensive administrative tools.

## 🎮 Features

### Core Functionality
- **⚔️ Automated Arena Management** - Enter/exit arenas with instant state changes
- **👥 Dual Character System** - Switch between normal and PvP characters instantly
- **🗺️ Global Map Icons** - Real-time player tracking on the map
- **🔧 Comprehensive Command System** - 100+ commands for players and admins
- **💾 Database Integration** - Persistent data storage with JSON fallback
- **🏗️ Service-Oriented Architecture** - Modular, scalable design

### Arena System
- **Automatic Zone Detection** - Walk into zones to auto-enter arena (50m entry, 75m exit radius)
- **Complete State Preservation** - Snapshot-based state management with full restoration
- **VBlood Hook System** - All VBlood abilities appear unlocked in arena UI (100+ VBlood database)
- **Crash Recovery** - Automatic state restoration on server restart
- **VBlood Integration** - Spawn/despawn VBlood bosses with achievement unlock system
- **Progression Integrity** - Real progression never modified, only UI override in arena

### Character Management
- **Instant Character Switching** - No logout required
- **PvP Character Creation** - Automatic arena-ready characters
- **Loadout Management** - Configurable gear sets and builds
- **Character Statistics** - Track progress and performance

### Administrative Tools
- **Player Management** - Kick, ban, mute, teleport, heal players
- **Server Administration** - Shutdown, restart, backup, maintenance
- **World Management** - Time, weather, object manipulation
- **Performance Monitoring** - FPS, memory, CPU usage tracking
- **Debug Tools** - Entity inspection, memory analysis

## 📚 Documentation

### User Documentation
- **[Installation Guide](INSTALLATION_GUIDE.md)** - Step-by-step installation instructions
- **[User Guide](USER_GUIDE.md)** - Complete user manual with examples
- **[Command Reference](COMMAND_REFERENCE.md)** - Comprehensive command documentation

### Developer Documentation
- **[API Documentation](API_DOCUMENTATION.md)** - Developer API reference
- **[Architecture Overview](ARCHITECTURE.md)** - System architecture details
- **[Database Integration](DatabaseIntegrationGuide.md)** - Database setup and usage

### System Documentation
- **[Dual Character System](DUAL_CHARACTER_SYSTEM.md)** - Character switching implementation
- **[Global Map Icon System](GLOBAL_MAP_ICON_SYSTEM.md)** - Map tracking system
- **[API Conversion Guide](API_CONVERSION_GUIDE.md)** - Migration guide for API changes

## 🚀 Quick Start

### Installation
1. Install BepInEx 5.4+ for V Rising
2. Copy `VAuto.dll` to `BepInEx/plugins/`
3. Configure settings in `BepInEx/config/gg.vautomation.arena.cfg`
4. Start server and verify installation

### Basic Usage
```bash
# Enter arena (automatic proximity or manual)
.arena enter [build]

# Check arena status
.arena status

# Switch builds while in arena
.arena warrior
.arena mage

# Exit arena
.arena exit

# System information
.arena info
```

### Admin Quick Commands
```bash
# Player management
.admin heal PlayerName
.admin teleport PlayerName
.admin kick PlayerName

# Server management  
.serveradmin broadcast "Maintenance in 10 minutes"
.serveradmin save
.serveradmin backup

# World control
.world time day
.world weather clear
.world clear 100
```

## 🔧 Configuration

### Basic Configuration
```ini
[General]
Enabled = true
LogLevel = Info

[Arena]
ArenaCenterX = -1000
ArenaCenterY = 5
ArenaCenterZ = -500
ArenaEnterRadius = 50
ArenaExitRadius = 75
AutoEnter = true

[Database]
EnableDatabase = true
DatabasePath = BepInEx/config/VAuto/Database.db
EnableJsonFallback = true

[VBlood]
EnableVBloodSystem = true
AutoSpawnEnabled = false
AchievementUnlockEnabled = true
```

### Advanced Configuration
- **Map Icon System** - Configure player tracking and update intervals
- **Dual Character System** - Customize character creation and naming
- **Performance Tuning** - Adjust update frequencies for high player counts
- **Security Settings** - Configure admin permissions and access control

## 📖 Command Categories

### Player Commands
- **Arena System** - `arena enter`, `arena exit`, `arena status`, `arena info`, `arena [build]`
- **Character Management** - `char`, `character`, `stats`, `info`
- **Quick Actions** - `tp`, `pos`, `hp`, `list`, `online`
- **VBlood System** - `vblood list`, `vblood spawn`, `vblood unlock`
- **Utilities** - `time`, `weather`, `calc`, `random`

### Administrative Commands
- **Player Control** - `admin kick`, `admin ban`, `admin heal`, `admin teleport`
- **Server Management** - `serveradmin shutdown`, `serveradmin save`, `serveradmin backup`
- **World Control** - `world time`, `world weather`, `world clear`
- **System Monitoring** - `system status`, `service restart`, `monitor performance`

### Development Commands
- **Entity Debugging** - `entity info`, `entity query`, `entity inspect`
- **Performance Analysis** - `perf start`, `perf fps`, `perf memory`
- **Memory Debugging** - `memory info`, `memory gc`, `memory leaks`
- **Testing Tools** - `test run`, `test benchmark`, `test stress`

## 🏗️ Architecture

### Service Architecture
```
┌─────────────────────────────────────────────────────────────┐
│                    VAuto Arena System                      │
├─────────────────────────────────────────────────────────────┤
│  Command Layer: VampireCommandFramework                    │
│  ├── UtilityCommands  ├── AdminCommands                    │
│  ├── DevDebugCommands └── ArenaCommands                    │
├─────────────────────────────────────────────────────────────┤
│  Service Layer: Service-Oriented Design                    │
│  ├── LifecycleService     ├── MapIconService               │
│  ├── DatabaseService      ├── PlayerService                │
│  ├── ArenaVirtualContext  └── GameSystems                  │
├─────────────────────────────────────────────────────────────┤
│  Data Layer: Enhanced Persistence                          │
│  ├── EnhancedDataPersistenceService                        │
│  ├── EnhancedPlayerProgressStore                           │
│  └── EnhancedArenaSnapshotService                          │
├─────────────────────────────────────────────────────────────┤
│  Integration: V Rising ECS + BepInEx                       │
└─────────────────────────────────────────────────────────────┘
```

### Key Services
- **SnapshotManagerService** - Complete arena entry/exit lifecycle management
- **GameSystems** - VBlood hook activation/deactivation system
- **ArenaProximitySystem** - Automatic arena entry/exit based on distance
- **ArenaVirtualContext** - Single global flag for arena state management
- **ProgressionCaptureService** - VBlood and ability capture/restore
- **VBloodMapper** - 100+ VBlood boss database with complete GUID mapping
- **AchievementUnlockService** - Multi-category achievement unlock system

## 💾 Database System

### Features
- **LiteDB Integration** - Fast, efficient database storage
- **JSON Fallback** - Automatic fallback if database fails
- **Migration Support** - Automatic migration from JSON files
- **Transaction Safety** - ACID compliance for data integrity

### Collections
- **Players** - Player data and statistics
- **PlayerProgress** - Progress tracking and streaks
- **PlayerSnapshots** - Arena entry/exit snapshots
- **ArenaData** - Arena configurations and statistics

## 🔧 Development

### Adding New Commands
```csharp
[Command("newcommand", adminOnly: false, usage: ".newcommand [args]")]
public static void NewCommand(ChatCommandContext ctx, string args = "")
{
    try
    {
        // Command logic
        ctx.Reply("Command executed successfully!");
    }
    catch (Exception ex)
    {
        Plugin.Instance.Log?.LogError($"Error: {ex.Message}");
        ctx.Reply("Command failed.");
    }
}
```

### Adding New Services
```csharp
public static class NewService
{
    private static bool _initialized = false;
    
    public static void Initialize()
    {
        if (_initialized) return;
        
        // Service initialization
        _initialized = true;
    }
}
```

## 📊 Performance

### Optimization Features
- **Efficient Entity Queries** - Optimized ECS queries
- **Concurrent Collections** - Thread-safe operations
- **Lazy Service Initialization** - Initialize only when needed
- **Configurable Update Intervals** - Tune for your server size

### Monitoring
- **Real-time FPS Tracking** - Monitor server performance
- **Memory Usage Analysis** - Detect memory leaks and pressure
- **Entity Count Tracking** - Monitor ECS performance
- **GC Performance** - Garbage collection optimization

## 🛡️ Security

### Features
- **Admin-Only Commands** - Sensitive operations require admin privileges
- **Permission Validation** - Server-side permission checking
- **Audit Logging** - Comprehensive operation logging
- **Input Validation** - Sanitize all user inputs

### Best Practices
- Regular security audits
- Input sanitization
- Permission-based access control
- Comprehensive logging

## 🐛 Troubleshooting

### Common Issues
1. **Commands not working** - Check admin privileges and command prefix
2. **Arena entry fails** - Verify zone configuration, player state, and VBlood hook status
3. **VBlood abilities not showing** - Ensure GameSystems.MarkPlayerEnteredArena was called
4. **State restoration issues** - Check snapshot integrity and service initialization
5. **Database errors** - Enable JSON fallback and check permissions

### Debug Commands
```bash
.system all              # Check all system status
.debug performance       # Performance analysis
.memory info            # Memory usage information
.entity info 12345      # Entity debugging
.log level debug        # Enable debug logging
```

### Getting Help
- Check the [Troubleshooting Guide](USER_GUIDE.md#faq)
- Review [Installation Guide](INSTALLATION_GUIDE.md#troubleshooting)
- Use debug commands to gather information
- Check server logs for detailed error messages

## 📈 Roadmap

### Upcoming Features
- **Multi-Zone Support** - Multiple concurrent arena zones
- **Tournament System** - Competitive arena events
- **Advanced Statistics** - Player performance analytics
- **Custom Build Editor** - In-game build creation
- **Plugin API** - Third-party extension support

### Version History
- **v1.0.0** - Initial release with complete arena lifecycle and VBlood hook system
- **v1.1.0** - Enhanced VBlood database and achievement system (current)
- **v1.2.0** - Multi-zone support and tournament system (planned)
- **v1.3.0** - Advanced analytics and custom build editor (planned)

## 🤝 Contributing

We welcome contributions! Please see our contributing guidelines:

1. **Code Standards** - Follow existing code style and patterns
2. **Testing** - Add tests for new functionality
3. **Documentation** - Update documentation for changes
4. **Security** - Follow security best practices

### Development Setup
1. Clone the repository
2. Open in Visual Studio or your preferred IDE
3. Restore NuGet packages
4. Build and test
5. Submit pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **V Rising Team** - For creating an amazing game
- **BepInEx** - For the excellent modding framework
- **VampireCommandFramework** - For command processing
- **ProjectM** - For V Rising integration
- **Community** - For feedback and contributions

## 📞 Support

- **GitHub Issues** - Bug reports and feature requests
- **Discord** - Community support and discussion
- **Documentation** - Comprehensive guides and references

---

**VAuto Arena System** - *Enhancing the V Rising experience through automation and advanced tooling.*

For more information, explore our [documentation](docs/) or join our [community](https://discord.gg/your-server).
