# Lifecycle Service Consolidation

## Overview
Successfully consolidated multiple lifecycle management services into a unified `LifecycleService.cs` that provides a single point of control for player arena lifecycle operations.

## Changes Made

### New Files Created
- **Services/LifecycleService.cs** - Unified service consolidating all lifecycle functionality

### Files Modified
- **Core/Plugin.cs** - Updated to initialize LifecycleService instead of individual services
- **Commands/Commands.cs** - Updated to use LifecycleService.EnterArena/ExitArena/IsInArena
- **Commands/ArenaCommands.cs** - Updated to use LifecycleService.IsInArena
- **Services/ArenaMonitorSystem.cs** - Updated to use LifecycleService for VBlood hooks
- **Services/VBloodUIService.cs** - Updated to use LifecycleService for VBlood hooks
- **Services/RestrictionTicker.cs** - Updated to use LifecycleService.IsInArena
- **Services/ReviveTicker.cs** - Updated to use LifecycleService.IsInArena
- **Services/ProximityTicker.cs** - Updated to use LifecycleService.EnterArena/ExitArena/IsInArena
- **Services/EnterPostProcessor.cs** - Removed duplicate blood preset application
- **Patches/AbilityPatch.cs** - Updated to use LifecycleService.IsInArena
- **Progression/BossProgressionService.cs** - Removed external VBlood notification call

### Files Deprecated (Marked but Kept for Reference)
- **Services/Snapshots.cs** - Functionality moved to LifecycleService
- **Services/ItemManagementService.cs** - Functionality moved to LifecycleService
- **Services/VBloodHookService.cs** - Functionality moved to LifecycleService

## API Changes

### Old API (Deprecated)
```csharp
// Multiple services with scattered functionality
Snapshots.Enter(userEntity, character, buildName)
Snapshots.Exit(userEntity, character)
Snapshots.IsInArena(platformId)
ItemManagementService.ClearAndStoreItems(character, platformId)
ItemManagementService.RestorePlayerItems(character, platformId)
VBloodHookService.ApplyVBloodHook(platformId)
VBloodHookService.ReleaseVBloodHook(platformId)
VBloodHookService.IsVBloodUnlocked(platformId, vbloodId)
```

### New API (Unified)
```csharp
// Single service with unified functionality
LifecycleService.EnterArena(userEntity, character, buildName)
LifecycleService.ExitArena(userEntity, character)
LifecycleService.IsInArena(platformId)
LifecycleService.ApplyVBloodHook(platformId)
LifecycleService.ReleaseVBloodHook(platformId)
LifecycleService.IsVBloodHooked(platformId)
LifecycleService.CleanupDisconnectedPlayers()
```

## Benefits

### Single Responsibility
- One service owns the complete player lifecycle
- Clear entry point for arena operations
- Easier to understand and maintain

### Simplified State Management
- All player state tracked in unified `PlayerState` class
- Atomic operations prevent partial state updates
- Easier to add new state fields

### Better Code Organization
- Related functionality co-located
- Reduced coupling between services
- Fewer files to navigate

### Improved Maintainability
- Single source of truth for lifecycle logic
- Consistent error handling
- Centralized event emission

## Migration Notes

### For Developers
1. Replace all `Snapshots.*` calls with `LifecycleService.*`
2. Replace all `ItemManagementService.*` calls with `LifecycleService.*`
3. Replace all `VBloodHookService.*` calls with `LifecycleService.*`
4. Note: `IsVBloodUnlocked` is now `IsVBloodHooked` (more accurate naming)

### Backward Compatibility
- Deprecated services are still present but marked as deprecated
- They should not be used in new code
- Consider removing them in a future cleanup phase

## Testing Recommendations

1. **Arena Entry**: Test that players can enter arena and state is captured correctly
2. **Arena Exit**: Test that players can exit arena and state is restored correctly
3. **VBlood Hooks**: Verify VBlood unlocks work correctly in arena
4. **Item Management**: Verify inventory is cleared on entry and restored on exit
5. **Disconnection Handling**: Test cleanup of disconnected players
6. **Concurrent Operations**: Test multiple players entering/exiting simultaneously

## Future Improvements

1. Consider removing deprecated service files after thorough testing
2. Add unit tests for LifecycleService
3. Consider adding more detailed logging for debugging
4. Evaluate performance optimizations for large player counts
