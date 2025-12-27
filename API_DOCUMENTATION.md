# VAuto Arena System - API Documentation

## 📚 Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Core Services](#core-services)
3. [Command System](#command-system)
4. [Database Integration](#database-integration)
5. [Entity Component System](#entity-component-system)
6. [Plugin Architecture](#plugin-architecture)
7. [Configuration System](#configuration-system)
8. [Development Guide](#development-guide)
9. [API Reference](#api-reference)

---

## 🏗️ Architecture Overview

### System Architecture
The VAuto Arena System follows a service-oriented architecture with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                    VAuto Arena System                      │
├─────────────────────────────────────────────────────────────┤
│  Command Layer                                              │
│  ├── VampireCommandFramework                               │
│  ├── Command Handlers                                       │
│  └── Shortcut System                                        │
├─────────────────────────────────────────────────────────────┤
│  Service Layer                                              │
│  ├── LifecycleService        ├── MapIconService            │
│  ├── ArenaVirtualContext     ├── PlayerService             │
│  ├── DatabaseService         ├── GameSystems               │
│  ├── TeleportService         └── RespawnPreventionService  │
├─────────────────────────────────────────────────────────────┤
│  Data Layer                                                 │
│  ├── EnhancedDataPersistenceService                        │
│  ├── EnhancedPlayerProgressStore                           │
│  ├── EnhancedArenaSnapshotService                          │
│  └── Configuration Management                              │
├─────────────────────────────────────────────────────────────┤
│  Integration Layer                                          │
│  ├── V Rising ECS                                          │
│  ├── BepInEx Plugin System                                 │
│  ├── VampireCommandFramework                               │
│  └── Harmony Patching                                      │
└─────────────────────────────────────────────────────────────┘
```

### Key Design Principles
- **Service Isolation**: Each service has a single responsibility
- **Dependency Injection**: Services are managed through Plugin class
- **Event-Driven**: Services communicate through events and callbacks
- **Performance First**: Optimized for high-frequency operations
- **Error Resilient**: Comprehensive error handling and recovery

---

## 🔧 Core Services

### LifecycleService
**Primary service managing player arena lifecycle**

#### Key Methods
```csharp
// Enter arena with full state management
bool EnterArena(Entity userEntity, Entity characterEntity)

// Exit arena and restore original state
bool ExitArena(Entity userEntity, Entity characterEntity)

// Check if player is currently in arena
bool IsPlayerInArena(ulong platformId)

// Validate if player is in PvP zone
bool ValidatePlayerInPvPZone(Entity characterEntity)
```

#### Features
- Automatic state capture and restoration
- VBlood unlocking integration
- UI management and progression updates
- Position-based zone detection
- Crash recovery support

### ArenaVirtualContext
**Singleton context for arena operations**

#### Key Methods
```csharp
// Initialize arena virtual context
public static void Initialize()

// Check if context is active
public static bool Active { get; }

// Get current arena state
public static ArenaState GetArenaState()
```

#### Features
- Global arena state management
- System-wide configuration
- Service coordination
- Performance monitoring

### DatabaseService
**Centralized database management**

#### Key Methods
```csharp
// Initialize database service
public static void Initialize(string databasePath, bool enableDatabase, bool enableJsonFallback)

// Get typed collection
public static JsonCollection<T> GetCollection<T>(string collectionName)

// Execute in transaction
public static void RunInTransaction(Action action)

// Get database statistics
public static DatabaseStatistics GetStatistics()
```

#### Features
- LiteDB integration with JSON fallback
- Transaction support
- Automatic migration from JSON
- Connection pooling
- Backup and recovery

### MapIconService
**Real-time player tracking on map**

#### Key Methods
```csharp
// Initialize map icon system
public static void Initialize()

// Refresh all player icons
public static void RefreshPlayerIcons()

// Cleanup all icons
public static void Cleanup()

// Get service statistics
public static MapIconStatistics GetStatistics()
```

#### Features
- 3-second update intervals
- Entity Component System integration
- Performance optimization
- Multiple icon types support
- Automatic cleanup

---

## 💬 Command System

### VampireCommandFramework Integration
The system uses VampireCommandFramework for command processing:

#### Command Registration
```csharp
[Command("arena", adminOnly: true, usage: ".arena <action>")]
public static void ArenaCommand(ChatCommandContext ctx, string action = "help")
{
    // Command implementation
}
```

#### Command Groups
```csharp
[CommandGroup("character")]
public static class CharacterCommands
{
    [Command("info", adminOnly: false, usage: ".character info [player]")]
    public static void CharacterInfoCommand(ChatCommandContext ctx, string player = "")
    {
        // Implementation
    }
}
```

### Custom Command Attributes
- `adminOnly`: Restricts command to administrators
- `usage`: Provides command usage information
- `description`: Command description for help system

### Command Processing Flow
```
User Input → Command Parser → Permission Check → Handler Execution → Response
```

---

## 💾 Database Integration

### Enhanced Data Persistence
Drop-in replacements for existing services with database support:

#### EnhancedDataPersistenceService
```csharp
// Get player data with database fallback
public static PlayerData GetPlayerData(ulong platformId)

// Update player data
public static void UpdatePlayerData(ulong platformId, PlayerData data)

// Save data with transaction support
public static void SavePlayerData(ulong platformId, PlayerData data)
```

#### EnhancedPlayerProgressStore
```csharp
// Get player progress
public static PlayerProgress GetPlayerProgress(ulong platformId)

// Update progress
public static void UpdatePlayerProgress(ulong platformId, PlayerProgress progress)

// Get all progress data
public static Dictionary<ulong, PlayerProgress> GetAllProgress()
```

#### EnhancedArenaSnapshotService
```csharp
// Create arena snapshot
public static ArenaSnapshot CreateSnapshot(ulong platformId, Entity characterEntity)

// Restore snapshot
public static bool RestoreSnapshot(ulong platformId, string snapshotId)

// List snapshots
public static List<ArenaSnapshot> ListSnapshots(ulong platformId)
```

### Database Schema

#### Collections Structure
```json
{
  "Players": {
    "PlatformId": "ulong",
    "Name": "string",
    "LastSeen": "datetime",
    "Stats": "object"
  },
  "PlayerProgress": {
    "PlatformId": "ulong",
    "Rounds": "int",
    "Streak": "int",
    "BossKills": "int",
    "LastUpdated": "datetime"
  },
  "PlayerSnapshots": {
    "SnapshotId": "string",
    "PlatformId": "ulong",
    "Data": "object",
    "CreatedAt": "datetime",
    "IsValid": "bool"
  }
}
```

---

## ⚡ Entity Component System

### ECS Integration Patterns

#### Entity Querying
```csharp
// Query all players
var playerQuery = em.CreateEntityQuery(ComponentType.ReadOnly<PlayerCharacter>());
var players = playerQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

// Query entities with specific components
var arenaQuery = em.CreateEntityQuery(
    ComponentType.ReadOnly<PlayerCharacter>(),
    ComponentType.ReadOnly<Translation>(),
    ComponentType.ReadOnly<Health>()
);
```

#### Component Management
```csharp
// Add component
em.AddComponentData(entity, new Health { Value = 100, MaxHealth = 100 });

// Check component exists
if (em.HasComponent<Health>(entity))
{
    var health = em.GetComponentData<Health>(entity);
}

// Remove component
em.RemoveComponent<Health>(entity);
```

#### System Implementation
```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ArenaSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // System logic here
        foreach (var (health, entity) in SystemAPI.Query<RefRW<Health>>().WithEntityAccess())
        {
            // Component processing
        }
    }
}
```

### Custom Components

#### ArenaState Component
```csharp
public struct ArenaState : IComponentData
{
    public bool IsInArena;
    public ulong PlatformId;
    public Entity CharacterEntity;
    public string SnapshotId;
    public DateTime EnterTime;
}
```

#### PlayerTracking Component
```csharp
public struct PlayerTracking : IComponentData
{
    public ulong PlatformId;
    public Entity UserEntity;
    public float3 LastPosition;
    public DateTime LastUpdate;
    public bool IsOnline;
}
```

---

## 🔌 Plugin Architecture

### Plugin Entry Point
```csharp
public class VAutoPlugin : BaseUnityPlugin
{
    public static VAutoPlugin Instance { get; private set; }
    
    public override void Load()
    {
        Instance = this;
        
        // Initialize configuration
        InitializeConfig();
        
        // Initialize services
        InitializeServices();
        
        // Register commands
        RegisterCommands();
        
        // Apply harmony patches
        _harmony.PatchAll();
    }
    
    private void InitializeServices()
    {
        // Service initialization order matters
        DatabaseService.Initialize();
        ArenaVirtualContext.Initialize();
        LifecycleService.Initialize();
        MapIconService.Initialize();
        // ... other services
    }
}
```

### Service Initialization Pattern
```csharp
public static class ServiceName
{
    private static bool _initialized = false;
    private static readonly object _lock = new object();
    
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_initialized) return;
            
            // Initialize service
            // Setup event handlers
            // Register with plugin
            
            _initialized = true;
        }
    }
    
    public static void Cleanup()
    {
        lock (_lock)
        {
            if (!_initialized) return;
            
            // Cleanup resources
            // Unregister event handlers
            
            _initialized = false;
        }
    }
}
```

---

## ⚙️ Configuration System

### BepInEx Configuration
```csharp
public static class Config
{
    // General settings
    public static ConfigEntry<bool> Enabled { get; private set; }
    public static ConfigEntry<string> LogLevel { get; private set; }
    
    // Arena settings
    public static ConfigEntry<float> ArenaCenterX { get; private set; }
    public static ConfigEntry<float> ArenaCenterY { get; private set; }
    public static ConfigEntry<float> ArenaCenterZ { get; private set; }
    public static ConfigEntry<float> ArenaRadius { get; private set; }
    
    // Database settings
    public static ConfigEntry<bool> EnableDatabase { get; private set; }
    public static ConfigEntry<string> DatabasePath { get; private set; }
    public static ConfigEntry<bool> EnableJsonFallback { get; private set; }
    
    public static void Initialize()
    {
        // Initialize configuration entries
        // Set default values
        // Add configuration changed handlers
    }
}
```

### JSON Configuration Files

#### arena_zones.json
```json
{
  "zones": [
    {
      "name": "main_arena",
      "center": { "x": -1000, "y": 5, "z": -500 },
      "radius": 100,
      "type": "pvp",
      "autoEnter": true
    }
  ]
}
```

#### builds.json
```json
{
  "default": {
    "name": "Default Arena Build",
    "gear": {
      "weapon": "Legendary Sword",
      "armor": "Full Plate",
      "accessories": ["Ring of Power"]
    },
    "blood": "Rogue Blood 100%",
    "abilities": ["Shadow Dash", "Veil of Mist"]
  }
}
```

---

## 🛠️ Development Guide

### Adding New Services

#### 1. Create Service Class
```csharp
public static class NewService
{
    private static bool _initialized = false;
    
    public static void Initialize()
    {
        if (_initialized) return;
        
        // Service initialization logic
        
        _initialized = true;
    }
    
    public static void Cleanup()
    {
        if (!_initialized) return;
        
        // Cleanup logic
        
        _initialized = false;
    }
    
    // Service methods
    public static void DoSomething()
    {
        if (!_initialized)
            throw new InvalidOperationException("Service not initialized");
            
        // Implementation
    }
}
```

#### 2. Register Service
```csharp
// In Plugin.Load()
private void InitializeServices()
{
    // Existing services...
    NewService.Initialize();
}
```

#### 3. Add Commands
```csharp
[CommandGroup("newservice")]
public static class NewServiceCommands
{
    [Command("action", adminOnly: true, usage: ".newservice action")]
    public static void NewServiceActionCommand(ChatCommandContext ctx)
    {
        try
        {
            NewService.DoSomething();
            ctx.Reply("Action completed successfully!");
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error in new service action: {ex.Message}");
            ctx.Reply("Action failed.");
        }
    }
}
```

### Adding New Commands

#### Basic Command Structure
```csharp
[Command("commandname", adminOnly: false, usage: ".commandname [args]")]
public static void CommandNameCommand(ChatCommandContext ctx, string args = "")
{
    try
    {
        // Command logic
        ctx.Reply("Command executed successfully!");
    }
    catch (Exception ex)
    {
        Plugin.Logger?.LogError($"Error in command: {ex.Message}");
        ctx.Reply("Command failed.");
    }
}
```

### Error Handling Best Practices

#### Try-Catch Pattern
```csharp
public static void SomeMethod()
{
    try
    {
        // Operation that might fail
        PerformOperation();
    }
    catch (SpecificException ex)
    {
        // Handle specific exception
        Plugin.Logger?.LogWarning($"Specific error: {ex.Message}");
    }
    catch (Exception ex)
    {
        // Handle general exception
        Plugin.Logger?.LogError($"Unexpected error: {ex.Message}");
        throw; // Re-throw if cannot handle
    }
}
```

#### Logging Pattern
```csharp
var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
var platformId = ctx.Event.User.PlatformId;

Plugin.Logger?.LogInfo($"[{timestamp}] OPERATION_START - PlatformId: {platformId}");

try
{
    // Operation logic
    Plugin.Logger?.LogInfo($"[{timestamp}] OPERATION_SUCCESS - PlatformId: {platformId}");
}
catch (Exception ex)
{
    Plugin.Logger?.LogError($"[{timestamp}] OPERATION_ERROR - PlatformId: {platformId}, Error: {ex.Message}");
    throw;
}
```

### Testing Services

#### Unit Testing Pattern
```csharp
[Test]
public void NewService_DoSomething_ExpectedResult()
{
    // Arrange
    NewService.Initialize();
    
    // Act
    var result = NewService.DoSomething();
    
    // Assert
    Assert.That(result, Is.EqualTo(expectedValue));
}

[Test]
public void NewService_NotInitialized_ThrowsException()
{
    // Arrange
    NewService.Cleanup();
    
    // Act & Assert
    Assert.Throws<InvalidOperationException>(() => NewService.DoSomething());
}
```

### Performance Considerations

#### Object Pooling
```csharp
public class ObjectPool<T> where T : class, new()
{
    private readonly Stack<T> _stack = new Stack<T>();
    
    public T Get()
    {
        return _stack.Count > 0 ? _stack.Pop() : new T();
    }
    
    public void Return(T item)
    {
        _stack.Push(item);
    }
}
```

#### Efficient Querying
```csharp
// Cache queries when possible
private static EntityQuery _cachedQuery;

// Use cached query
var entities = _cachedQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

// Clean up temporary arrays
entities.Dispose();
```

---

## 📖 API Reference

### Core Classes

#### Plugin
```csharp
public static class Plugin
{
    public static BaseUnityPlugin Instance { get; set; }
    public static ManualLogSource Logger { get; set; }
}
```

#### VAutoCore
```csharp
public static class VAutoCore
{
    public static EntityManager EntityManager { get; set; }
    public static World World { get; set; }
    public static Game Game { get; set; }
}
```

#### MissingTypes
```csharp
public static class MissingTypes
{
    public static LifecycleService LifecycleService { get; set; }
    public static BuildService BuildService { get; set; }
    public static ZoneService ZoneService { get; set; }
    // ... other service references
}
```

### Data Models

#### PlayerState
```csharp
public class PlayerState
{
    public ulong PlatformId { get; set; }
    public Entity UserEntity { get; set; }
    public Entity CharacterEntity { get; set; }
    public bool IsInArena { get; set; }
    public string SnapshotId { get; set; }
    public DateTime EnterTime { get; set; }
}
```

#### ArenaSnapshot
```csharp
public class ArenaSnapshot
{
    public string SnapshotId { get; set; }
    public ulong PlatformId { get; set; }
    public Dictionary<string, object> Data { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsValid { get; set; }
}
```

#### DatabasePlayer
```csharp
public class DatabasePlayer
{
    public ulong PlatformId { get; set; }
    public string Name { get; set; }
    public DateTime LastSeen { get; set; }
    public Dictionary<string, object> Stats { get; set; }
}
```

### Event System

#### Event Types
```csharp
public enum ArenaEventType
{
    PlayerEnter,
    PlayerExit,
    SnapshotCreated,
    SnapshotRestored,
    ServiceInitialized,
    ServiceError
}

public class ArenaEvent
{
    public ArenaEventType Type { get; set; }
    public ulong PlatformId { get; set; }
    public object Data { get; set; }
    public DateTime Timestamp { get; set; }
}
```

#### Event Handler
```csharp
public static event EventHandler<ArenaEvent> ArenaEventHandler;

private static void OnArenaEvent(ArenaEvent arenaEvent)
{
    ArenaEventHandler?.Invoke(null, arenaEvent);
}
```

---

## 🔧 Extension Points

### Custom Service Integration
```csharp
public interface ICustomService
{
    void Initialize();
    void Cleanup();
    bool IsInitialized { get; }
}

public static class CustomServiceManager
{
    private static readonly List<ICustomService> _services = new List<ICustomService>();
    
    public static void RegisterService(ICustomService service)
    {
        _services.Add(service);
    }
    
    public static void InitializeAll()
    {
        foreach (var service in _services)
        {
            service.Initialize();
        }
    }
}
```

### Plugin API
```csharp
public interface IVAutoPluginAPI
{
    // Arena management
    bool EnterArena(ulong platformId);
    bool ExitArena(ulong platformId);
    bool IsInArena(ulong platformId);
    
    // Character management
    bool CreatePvPCharacter(ulong platformId);
    bool SwapCharacters(ulong platformId);
    
    // Service management
    void EnableService(string serviceName);
    void DisableService(string serviceName);
    Dictionary<string, bool> GetServiceStatus();
}
```

### Configuration Extensions
```csharp
public static class ConfigExtensions
{
    public static T GetValueOrDefault<T>(this ConfigEntry<T> configEntry, T defaultValue)
    {
        return configEntry.Value ?? defaultValue;
    }
    
    public static void AddToConfig(this string key, string description, Action<string> onChanged)
    {
        // Configuration extension logic
    }
}
```

---

*This API documentation provides a comprehensive reference for developing with the VAuto Arena System. For specific implementation details, refer to the source code and inline documentation.*