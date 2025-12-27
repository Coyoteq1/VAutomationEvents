# VampireDB Integration Strategy

## Current State Analysis

### Existing Data Persistence
The VAuto project currently uses JSON file-based persistence through two main services:

1. **DataPersistenceService.cs**
   - Location: `BepInEx/config/VAuto/Data/game_data.json`
   - Data Types: Player data, Boss data, Arena data
   - Format: Single JSON file with dictionaries

2. **PlayerProgressStore.cs**
   - Location: `BepInEx/config/VAuto.Arena/player_progress.json`
   - Data Types: Player progress (rounds, streaks, boss kills)
   - Format: Dictionary of platform IDs to PlayerProgressModel

3. **ArenaSnapshotService.cs**
   - Location: In-memory only
   - Data Types: Temporary player snapshots
   - Format: Dictionary in memory (lost on restart)

### Pain Points
- Multiple file locations and formats
- No indexing or efficient querying
- Manual file locking for thread safety
- No transaction support
- Data integrity issues on crashes
- Scalability limitations

## Integration Strategy

### Phase 1: Database Wrapper Implementation
1. Create `DatabaseService` as centralized database manager
2. Implement VampireDB wrapper with proper initialization
3. Add configuration options for database location and options
4. Provide migration utilities from JSON files

### Phase 2: Data Model Migration
1. Define database schemas for all data types
2. Create data access objects (DAOs) for each service
3. Implement proper indexing for performance
4. Add data validation and constraints

### Phase 3: Service Updates
1. Update `DataPersistenceService` to use database
2. Update `PlayerProgressStore` to use database
3. Update `ArenaSnapshotService` to persist snapshots
4. Maintain backward compatibility during transition

### Phase 4: Performance & Features
1. Add database connection pooling
2. Implement proper transaction management
3. Add database backup and recovery
4. Performance monitoring and optimization

## Database Schema Design

### Collections
1. **Players**: `ulong` (Platform ID)
   - Player data, statistics, preferences
   - Index: Platform ID (primary), LastSeen, Name

2. **BossProgress**: `string` (Boss ID)
   - Boss defeat tracking, statistics
   - Index: Boss ID (primary), Level, Region

3. **ArenaData**: `string` (Arena ID)
   - Arena configuration, statistics
   - Index: Arena ID (primary), Name, Region

4. **PlayerProgress**: `ulong` (Platform ID)
   - Progress tracking, streaks, rounds
   - Index: Platform ID (primary), Round, LastUpdated

5. **PlayerSnapshots**: `string` (Snapshot ID)
   - Arena entry/exit snapshots
   - Index: Platform ID, CreatedAt, IsValid

6. **GameSettings**: `string` (Setting Key)
   - Global configuration settings
   - Index: Setting Key (primary)

### Migration Plan
1. **Backward Compatibility**: Keep JSON files during transition
2. **Dual Write**: Write to both JSON and database initially
3. **Migration Tool**: One-time import from JSON to database
4. **Validation**: Compare data between JSON and database
5. **Cleanup**: Remove JSON files after successful migration

## Implementation Priority
1. High: Core database service and player data migration
2. Medium: Progress tracking and arena data migration  
3. Low: Snapshot persistence and advanced features

## Benefits
- Improved performance with indexed queries
- Better data integrity with transactions
- Centralized data management
- Reduced file I/O overhead
- Better scalability for large datasets
- Built-in backup and recovery features