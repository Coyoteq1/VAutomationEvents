# VampireDB Integration Guide

This guide explains how to use the new database integration features in the VAuto mod.

## Overview

The database integration provides a centralized, persistent data storage solution that replaces or enhances the existing JSON file-based persistence. It offers:

- **Unified Data Management**: All persistent data in one location
- **Better Performance**: Indexed queries and efficient storage
- **Data Integrity**: Transaction support and backup capabilities
- **Scalability**: Better handling of large datasets
- **Fallback Support**: Automatic fallback to JSON if database fails

## Architecture

### Core Components

1. **DatabaseService**: Core database management and configuration
2. **EnhancedDataPersistenceService**: Drop-in replacement for DataPersistenceService
3. **EnhancedPlayerProgressStore**: Drop-in replacement for PlayerProgressStore  
4. **EnhancedArenaSnapshotService**: Enhanced snapshot service with database backup

### Storage Structure

The database creates the following collection structure:

```
BepInEx/config/VAuto/Database.db/
├── collections/
│   ├── Players.json           # Player data and statistics
│   ├── BossProgress.json      # Boss defeat tracking
│   ├── ArenaData.json         # Arena configurations and stats
│   ├── PlayerProgress.json    # Player progression data
│   └── PlayerSnapshots.json   # Arena entry/exit snapshots
```

## Configuration

Database settings are configured in the plugin configuration file:

```ini
[Database]
## Path to the LiteDB database file
DatabasePath = C:\BepInEx\DedicatedServerLauncher\VRisingServer\BepInEx\config\gg.Automation.arena\VAuto\Database.db

## Enable database persistence (VampireDB/LiteDB)
EnableDatabase = true

## Fallback to JSON files if database fails
EnableJsonFallback = true

## Automatically migrate data from JSON files to database on startup
EnableMigration = true
```

## Migration from JSON Files

The database service automatically migrates data from existing JSON files on first startup:

1. **Game Data**: Migrated from `BepInEx/config/VAuto/Data/game_data.json`
2. **Player Progress**: Migrated from `BepInEx/config/VAuto.Arena/player_progress.json`

Migration is enabled by default and can be disabled by setting `EnableMigration = false`.

## Usage Examples

### Basic Data Persistence

```csharp
// Get database collection
var players = DatabaseService.GetCollection<DatabasePlayer>("Players");

// Upsert player data
var player = new DatabasePlayer
{
    PlatformId = steamId,
    Name = "PlayerName",
    LastSeen = DateTime.UtcNow,
    Stats = new Dictionary<string, object>()
};
players.Upsert(player);

// Query data
var allPlayers = players.FindAll();
var playerCount = players.Count();
```

### Enhanced Data Persistence

```csharp
// The enhanced service automatically uses database or JSON fallback
EnhancedDataPersistenceService.Initialize();

// Use existing API - works with both database and JSON
var playerData = EnhancedDataPersistenceService.GetPlayerData(steamId);
EnhancedDataPersistenceService.UpdatePlayerData(steamId, playerData);
```

### Transaction Support

```csharp
// Run multiple operations in a transaction
DatabaseService.RunInTransaction(() =>
{
    var players = DatabaseService.GetCollection<DatabasePlayer>("Players");
    var progress = DatabaseService.GetCollection<DatabasePlayerProgress>("PlayerProgress");
    
    // These operations will be atomic
    players.Upsert(playerData);
    progress.Upsert(progressData);
});
```

## API Reference

### DatabaseService

- `Initialize()`: Initialize the database service
- `Configure(path, enable, fallback, migration)`: Configure service settings
- `GetCollection<T>(name)`: Get a typed collection
- `RunInTransaction(action)`: Execute actions in a transaction
- `GetStatistics()`: Get database statistics
- `Shutdown()`: Clean shutdown

### JsonCollection<T>

- `FindById(id)`: Find item by ID
- `Upsert(item)`: Insert or update item
- `Delete(id)`: Delete item by ID
- `Count()`: Get item count
- `FindAll()`: Get all items

### Enhanced Services

All enhanced services maintain the same API as their original counterparts but add database support:

- `EnhancedDataPersistenceService`: Same API as `DataPersistenceService`
- `EnhancedPlayerProgressStore`: Same API as `PlayerProgressStore`
- `EnhancedArenaSnapshotService`: Same API as `ArenaSnapshotService`

## Performance Considerations

### Benefits over JSON

1. **Indexed Queries**: Fast lookups by PlatformId, BossId, etc.
2. **Efficient Storage**: Binary format instead of text
3. **Transaction Support**: Atomic operations
4. **Connection Pooling**: Better resource management

### Best Practices

1. **Use Transactions**: Group related operations
2. **Batch Operations**: Use `Upsert` for multiple updates
3. **Monitor Statistics**: Check database health regularly
4. **Backup Regularly**: Implement backup procedures

## Troubleshooting

### Database Not Available

If the database fails to initialize, the service automatically falls back to JSON:

```
[DatabaseService] Database disabled in configuration
[DatabaseService] Falling back to JSON persistence
```

### Migration Issues

If migration fails, check the logs for specific errors:

```
[DatabaseService] Migration failed: <error message>
```

Manual migration can be triggered by setting `EnableMigration = true` and restarting.

### Performance Issues

Monitor database statistics and consider:

1. **Index Optimization**: Add missing indexes
2. **Collection Cleanup**: Remove old snapshots
3. **Memory Usage**: Monitor memory consumption

## Future Enhancements

Planned improvements include:

1. **LiteDB Integration**: Full LiteDB support for advanced features
2. **Backup/Restore**: Automated backup procedures
3. **Query Optimization**: Advanced indexing strategies
4. **Connection Pooling**: Better performance under load
5. **Data Validation**: Schema validation and constraints

## Migration Checklist

To migrate existing installations:

- [ ] Backup existing JSON files
- [ ] Enable database in configuration
- [ ] Enable migration
- [ ] Restart server
- [ ] Verify data migration in logs
- [ ] Monitor performance
- [ ] Remove old JSON files (optional)

## Support

For issues with database integration:

1. Check logs for error messages
2. Verify configuration settings
3. Test with fallback enabled
4. Review migration logs
5. Check file permissions for database path