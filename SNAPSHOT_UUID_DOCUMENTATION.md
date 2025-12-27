# Snapshot UUID Generator Documentation

## Overview

The Snapshot UUID Generator provides unique identification for arena snapshots using cryptographically secure UUID generation. This ensures each snapshot has a globally unique identifier that can be used for tracking, debugging, and management purposes.

## Features

### 1. UUID Generation Methods

#### `GenerateSnapshotUuid(ulong characterId, string arenaId)`

- Generates a UUID v5 using character ID, arena ID, and timestamp
- Returns a 32-character hexadecimal string
- Thread-safe with internal locking
- Combines multiple data sources for uniqueness

#### `GenerateMetadataUuid(string context = "snapshot")`

- Generates UUIDs for metadata tracking
- Uses SHA-256 hashing for security
- Suitable for session tracking, operation logging, etc.

### 2. Validation Methods

#### `IsValidUuid(string uuidString)`

- Validates UUID format using built-in .NET GUID parsing
- Returns true for valid 32-character hex strings
- Rejects empty or malformed UUIDs

#### `ExtractTimestamp(string uuidString)`

- Placeholder for future timestamp extraction
- Currently returns null (UUID v5 doesn't encode timestamps)

## Integration with Arena System

### Enhanced Arena Snapshot Service

The `EnhancedArenaSnapshotService` has been updated to use UUIDs:

#### Key Changes

- **Snapshot Storage**: Now uses UUID as primary key
- **Legacy Compatibility**: Maintains `{characterId}_{arenaId}` mapping for backward compatibility
- **UUID Field**: Added `SnapshotUuid` to `PlayerSnapshot` class
- **New Methods**: UUID-based retrieval and deletion methods

#### Storage Structure

```csharp
// Primary storage (UUID -> Snapshot)
private static readonly Dictionary<string, PlayerSnapshot> _playerSnapshots;

// Legacy mapping for compatibility (LegacyKey -> UUID)
private static readonly Dictionary<string, string> _snapshotToKeyMap;

// Timestamp tracking (UUID -> CreationTime)
private static readonly Dictionary<string, DateTime> _snapshotCreationTimes;
```

### New Service Methods

#### UUID-Based Operations

- `GetSnapshotByUuid(string snapshotUuid)` - Direct UUID lookup
- `GetSnapshotUuid(string characterId, string arenaId)` - Get UUID from legacy key
- `DeleteSnapshotByUuid(string snapshotUuid)` - Delete by UUID
- `HasSnapshot(string characterId, string arenaId)` - Updated to use UUID mapping

#### Legacy Compatibility

- All existing methods (`CreateSnapshot`, `RestoreSnapshot`, etc.) maintain their original signatures
- Internal UUID generation is transparent to existing code
- Legacy key format (`{characterId}_{arenaId}`) still works for lookups

## Usage Examples

### Basic Snapshot Creation

```csharp
// UUID is automatically generated
var success = EnhancedArenaSnapshotService.CreateSnapshot(user, character, "arena_1");

// Get the generated UUID
var snapshotUuid = EnhancedArenaSnapshotService.GetSnapshotUuid("12345", "arena_1");
Console.WriteLine($"Snapshot UUID: {snapshotUuid}"); // Output: 7d9b2c8a4f1e6b3c9d5a8e2f7b4c9d1e
```

### Direct UUID Operations

```csharp
// Get snapshot directly by UUID
var snapshot = EnhancedArenaSnapshotService.GetSnapshotByUuid("7d9b2c8a4f1e6b3c9d5a8e2f7b4c9d1e");

// Delete by UUID
var deleted = EnhancedArenaSnapshotService.DeleteSnapshotByUuid("7d9b2c8a4f1e6b3c9d5a8e2f7b4c9d1e");
```

### Metadata Tracking

```csharp
// Generate UUID for session tracking
var sessionUuid = SnapshotUuidGenerator.GenerateMetadataUuid("arena_session");
var operationUuid = SnapshotUuidGenerator.GenerateMetadataUuid("inventory_backup");
```

## Technical Details

### UUID Version 5 Implementation

- Uses DNS namespace (`6ba7b810-9dad-11d1-80b4-00c04fd430c8`)
- Combines character ID, arena ID, and timestamp as name
- SHA-1 hashing with version/variant bits set for UUID v5
- Thread-safe generation with internal locking

### Fallback Mechanisms

- If UUID v5 generation fails, falls back to `Guid.NewGuid()`
- Comprehensive error logging for debugging
- Graceful degradation ensures system continues working

### Performance Considerations

- UUID generation is cached and optimized
- Dictionary lookups use UUIDs as keys (fast hash-based access)
- Legacy mapping maintained for compatibility (minimal overhead)

## Security Considerations

### Cryptographic Security

- Uses SHA-1 and SHA-256 for hash generation
- Timestamp component ensures uniqueness across time
- Character ID and arena ID provide context-specific uniqueness

### UUID Validation

- All UUID operations validate format before processing
- Invalid UUIDs are rejected early with appropriate logging
- No SQL injection or format string vulnerabilities

## Error Handling

### Generation Failures

- Automatic fallback to random GUIDs
- Detailed error logging for troubleshooting
- System continues operating despite UUID generation issues

### Invalid Operations

- Clear error messages for invalid UUID formats
- Null returns for missing snapshots
- Boolean success indicators for delete operations

## Testing

### Unit Tests

```csharp
// Run basic functionality tests
SnapshotUuidGeneratorTests.RunTests();
```

### Test Coverage

- UUID generation uniqueness
- Format validation
- Length consistency
- Metadata UUID generation
- Error handling scenarios

## Migration Guide

### From Legacy System

1. Existing code continues to work without changes
2. New UUID features are opt-in
3. Gradual migration path available

### Best Practices

1. Use UUIDs for new development
2. Maintain legacy compatibility where needed
3. Log UUIDs for debugging and tracking
4. Validate UUIDs before operations

## Future Enhancements

### Planned Features

- UUID v1 implementation with embedded timestamps
- Batch UUID generation for performance
- UUID-based analytics and reporting
- Integration with external systems

### Extensibility
- Plugin architecture for custom UUID generators
- Configurable UUID versions and namespaces
- Custom validation rules and formats
