# PvP Zone Lifecycle Fixes Proposal

## Overview
This document proposes specific code changes to bring the `LifecycleService.cs` implementation into full compliance with the PvP Zone Lifecycle rules.

## Proposed Fixes

### 1. Complete Snapshot Contents (HIGH PRIORITY)

**Issue**: Snapshot missing stats/level, achievements, passive abilities, UI state.

**Proposed Changes**:

Add to `PlayerSnapshot` class:
```csharp
public sealed class PlayerSnapshot
{
    // ... existing fields ...

    // Add missing fields
    public int Level { get; set; }
    public float Experience { get; set; }
    public List<int> Stats { get; set; } = new(); // Strength, Dexterity, etc.
    public List<int> Achievements { get; set; } = new();
    public List<int> PassiveAbilities { get; set; } = new();
    public string UIState { get; set; } = string.Empty; // JSON serialized UI state
}
```

Update `CaptureSnapshot()` method:
```csharp
private static PlayerSnapshot CaptureSnapshot(Entity character, Entity userEntity)
{
    var em = VRCore.EM;
    var snap = new PlayerSnapshot();

    // ... existing capture code ...

    // Add stats/level capture
    try
    {
        if (em.TryGetComponentData(character, out Level level))
        {
            snap.Level = level.Value;
        }
        if (em.TryGetComponentData(character, out Experience exp))
        {
            snap.Experience = exp.Value;
        }
    }
    catch (Exception ex)
    {
        Plugin.Log?.LogDebug($"Failed to capture level/stats: {ex.Message}");
    }

    // Add achievements capture
    TryCaptureAchievements(userEntity, character, snap);

    // Add passive abilities capture
    TryCapturePassiveAbilities(userEntity, character, snap);

    // Add UI state capture
    TryCaptureUIState(userEntity, snap);

    return snap;
}
```

### 2. Implement Snapshot Disposal (HIGH PRIORITY)

**Issue**: Snapshot files not deleted after successful restore.

**Proposed Changes**:

Add to `ExitArena()` method after successful restore:
```csharp
// Delete snapshot files after successful restore
if (restore_success)
{
    try
    {
        var practiceSnapshotPath = Path.Combine(BaseDir, "snapshots", platformId.ToString(), "practice.json");
        var enterSnapshotPath = Path.Combine(BaseDir, "snapshots", platformId.ToString(), "enter.json");

        if (File.Exists(practiceSnapshotPath))
            File.Delete(practiceSnapshotPath);
        if (File.Exists(enterSnapshotPath))
            File.Delete(enterSnapshotPath);

        Plugin.Log?.LogInfo($"Deleted snapshot files for {platformId}");
    }
    catch (Exception ex)
    {
        Plugin.Log?.LogError($"Failed to delete snapshot files for {platformId}: {ex}");
    }
}
```

### 3. Complete Restoration Order (HIGH PRIORITY)

**Issue**: Missing equipment, UI state, passives/buffs restoration.

**Proposed Changes**:

Update `RestoreSnapshot()` method to follow exact order:
```csharp
private static bool RestoreSnapshot(Entity character, Entity userEntity, PlayerSnapshot snap)
{
    var em = VRCore.EM;

    try
    {
        // 1. Clear PvP inventory
        try { InventoryUtilitiesServer.ClearInventory(em, character); } catch { }

        // 2. Restore snapshot inventory
        foreach (var it in snap.Inventory)
        {
            // ... existing inventory restore code ...
        }

        // 3. Restore equipment
        TryRestoreEquipment(character, snap);

        // 4. Restore blood type & quality
        // ... existing blood restore code ...

        // 5. Restore player name
        // ... existing name restore code ...

        // 6. Restore UI state
        TryRestoreUIState(userEntity, snap);

        // 7. Restore passives & buffs
        TryRestorePassiveAbilities(userEntity, character, snap);

        return true;
    }
    catch (Exception ex)
    {
        Plugin.Log?.LogError($"Error restoring player state: {ex}");
        return false;
    }
}
```

### 4. Implement Crash Recovery (MEDIUM PRIORITY)

**Issue**: No restoration on next login if crashed in PvP state.

**Proposed Changes**:

Add to `LifecycleService.Initialize()` or create new method:
```csharp
public static void HandlePlayerLogin(ulong platformId, Entity userEntity, Entity character)
{
    try
    {
        // Check if player was in PvP state during crash
        var snapshot = LoadSnapshot(platformId);
        if (snapshot != null && !_playerStates.ContainsKey(platformId))
        {
            Plugin.Log?.LogInfo($"Detected crashed PvP state for {platformId}, restoring...");

            // Restore snapshot (snapshot restoration takes priority per Rule 8.2)
            if (RestoreSnapshot(character, userEntity, snapshot))
            {
                // Delete snapshot after successful restore
                DeleteSnapshotFiles(platformId);
                Plugin.Log?.LogInfo($"Successfully restored crashed PvP state for {platformId}");
            }
            else
            {
                Plugin.Log?.LogError($"Failed to restore crashed PvP state for {platformId}");
            }
        }
    }
    catch (Exception ex)
    {
        Plugin.Log?.LogError($"Error handling crash recovery for {platformId}: {ex}");
    }
}
```

Call this method from player login event handler in `Plugin.cs`.

### 5. Implement Blood Type Override (LOW PRIORITY)

**Issue**: Blood type not overridden for PvP identity.

**Proposed Changes**:

Add to `EnterArena()` after name modification:
```csharp
// Override blood type for PvP identity
try
{
    if (em.TryGetComponentData(character, out Blood blood))
    {
        // Store original blood type in snapshot (already done)
        // Override to PvP blood type (e.g., default or specific PvP blood)
        var pvpBloodType = new PrefabGUID(/* PvP blood type GUID */);
        blood.BloodType = pvpBloodType;
        blood.Quality = /* PvP blood quality */;
        em.SetComponentData(character, blood);
        Plugin.Log?.LogInfo($"Overrode blood type for PvP mode");
    }
}
catch (Exception ex)
{
    Plugin.Log?.LogDebug($"Failed to override blood type: {ex.Message}");
}
```

### 6. Add Zone Validation to Commands (LOW PRIORITY)

**Issue**: Not all relevant commands validate zone presence.

**Proposed Changes**:

Add validation helper method:
```csharp
private static bool ValidateInArena(ChatCommandContext ctx)
{
    var user = ctx.Event.SenderUserEntity;
    var em = VRCore.EM;
    if (!em.TryGetComponentData(user, out User u) || !LifecycleService.IsInArena(u.PlatformId))
    {
        ctx.Error("This command can only be used inside the PvP arena");
        return false;
    }
    return true;
}
```

Apply to relevant commands in `ArenaCommands.cs`:
- `.giveitem` (weapons/armor)
- `.unlockvbloods` (blood)
- `.unlockall` (blood/achievements)
- Any build-related commands

## Implementation Order

1. **Phase 1 (Critical)**: Complete snapshot contents and disposal
2. **Phase 2 (Important)**: Complete restoration order
3. **Phase 3 (Recovery)**: Implement crash recovery
4. **Phase 4 (Polish)**: Blood type override and additional command validation

## Testing Requirements

- Test snapshot capture includes all required data
- Test restoration restores all captured data
- Test snapshot files are deleted after exit
- Test crash recovery restores players correctly
- Test blood type override works
- Test zone validation prevents command abuse

## Risk Assessment

- **High Risk**: Snapshot content changes may break existing saves
- **Medium Risk**: Crash recovery may conflict with normal login flow
- **Low Risk**: Additional validations and blood type override
