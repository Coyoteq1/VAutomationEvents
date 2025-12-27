# PlayerProgressStore Usage Examples

The `PlayerProgressStore` has been successfully integrated into the VAutomatonEvents system. Here's how to use it:

## Service Integration

The `PlayerProgressStore` is now part of the unified service system and will be automatically:

- Initialized when the plugin starts
- Cleaned up when the plugin shuts down
- Included in service status reports

## Commands Available

Admins can use these commands to manage player progress data:

### `.progress get <platformId>`

View progress data for a specific player

```bash
.progress get 1234567890123456789
```

### `.progress list`

Show all cached players (first 10 to avoid spam)

```bash
.progress list
```

### `.progress save`

Force save all cached progress data to disk

```bash
.progress save
```

### `.progress remove <platformId>`

Remove progress data for a specific player

```bash
.progress remove 1234567890123456789
```

### `.progress status`

Show the current status of the PlayerProgressStore

```bash
.progress status
```

## Programmatic Usage

### Getting or Creating Player Progress

```csharp
// Get existing progress or create default if not found
var progress = PlayerProgressStore.GetOrCreate(platformId);

// Update player data
progress.Level = 25;
progress.Experience = 15000.5f;
progress.CharacterName = "PlayerName";

// Save changes
PlayerProgressStore.Save(progress);
```

### Getting Existing Progress (returns null if not found)

```csharp
var progress = PlayerProgressStore.Get(platformId);
if (progress != null)
{
    // Use progress data
    Console.WriteLine($"Player {progress.CharacterName} is level {progress.Level}");
}
```

### Managing VBlood Unlocks

```csharp
// Mark VBlood as unlocked
progress.UnlockedVBloods["Dracula"] = true;

// Check if VBlood is unlocked
if (progress.UnlockedVBloods.ContainsKey("Dracula"))
{
    // VBlood is unlocked
}
```

### Managing Ability Levels

```csharp
// Set ability level
progress.AbilityLevels["ChaosReach"] = 3;

// Get ability level
int level = progress.AbilityLevels.GetValueOrDefault("ChaosReach", 0);
```

### Managing Quest Completion

```csharp
// Mark quest as completed
progress.CompletedQuests.Add("MainQuest_001");

// Check if quest is completed
bool completed = progress.CompletedQuests.Contains("MainQuest_001");
```

## Data Storage

- **Location**: `BepInEx/config/VAuto.Arena/player_progress.json`
- **Format**: JSON with pretty printing
- **Thread Safety**: All operations are thread-safe with file locking
- **Auto-save**: Data is automatically saved when modified

## Service Status

The PlayerProgressStore appears in the unified service status:

```
=== VAuto Service Status ===
• PlayerService: Status: Running, Players: 5
• MapIconService: Status: Running, Icons: 3
• GameSystems: Status: Running, Hooks: 2
• RespawnPrevention: Status: Running, Cooldowns: 1
• NameTagService: Status: Running, Tags: 0
• PlayerProgressStore: Status: Running, CachedPlayers: 12
```

## Error Handling

The service includes comprehensive error handling:

- File I/O errors are logged but don't crash the service
- Invalid data is gracefully handled
- Service continues operating even if file operations fail
- Automatic fallback to empty cache on initialization errors

## Performance Considerations

- In-memory caching for fast access
- File operations only when data changes
- Thread-safe with minimal locking overhead
- JSON serialization is optimized for the data structure
