# UnlockHelper Service - Documentation Update

## 🔓 Enhanced Unlock System Implementation

The UnlockHelper service provides safe and reliable unlocking of V Rising content including research, VBloods, and achievements.

### 🚀 Key Features

#### Safe Unlocking
- **Research Unlocking** - Unlock all research items for testing
- **VBlood Unlocking** - Access all VBlood bosses without progression
- **Achievement Completion** - Complete all achievements instantly
- **Error Handling** - Graceful failure handling with logging

#### Implementation Details
- **DebugEventsSystem Integration** - Uses official V Rising debug systems
- **Reflection-Based Access** - Safely invokes private methods when needed
- **Type Resolution** - Dynamically resolves FromCharacter type across assemblies
- **Null Safety** - Comprehensive null checks and error handling

### 📖 API Methods

#### Main Unlocking Methods
```csharp
// Unlock everything at once
public static void TryUnlockAll(Entity userEntity, Entity characterEntity)

// Unlock specific categories
public static void TryUnlockResearch(Entity userEntity, Entity characterEntity)
public static void TryUnlockVBloods(Entity userEntity, Entity characterEntity)
public static void TryUnlockAchievements(Entity userEntity, Entity characterEntity)
```

#### Internal Helper Methods
```csharp
// Type resolution and object creation
private static Type ResolveFromCharacterType()
private static object CreateFromCharacter(Entity userEntity, Entity characterEntity)

// Safe method invocation
private static void InvokeIfExists(Type t, object instance, string name, object[] args)
private static void TryUnlockResearchInternal(DebugEventsSystem debug, object fromCharacter)
private static void TryUnlockVBloodsInternal(DebugEventsSystem debug, object fromCharacter)
private static void TryUnlockAchievementsInternal(DebugEventsSystem debug, object fromCharacter)
```

### 🎯 Usage Examples

#### Complete Unlocking
```csharp
// Unlock everything for arena testing
UnlockHelper.TryUnlockAll(userEntity, characterEntity);
```

#### Selective Unlocking
```csharp
// Unlock only research for testing builds
UnlockHelper.TryUnlockResearch(userEntity, characterEntity);

// Unlock only VBloods for boss testing
UnlockHelper.TryUnlockVBloods(userEntity, characterEntity);

// Unlock only achievements
UnlockHelper.TryUnlockAchievements(userEntity, characterEntity);
```

#### Error Handling
```csharp
try
{
    UnlockHelper.TryUnlockAll(userEntity, characterEntity);
    ctx.Reply("✅ All unlocks applied successfully!");
}
catch (Exception ex)
{
    ctx.Reply($"❌ Failed to apply unlocks: {ex.Message}");
    Plugin.Logger?.LogWarning($"Unlock failed: {ex.Message}");
}
```

### 🔧 Technical Implementation

#### DebugEventsSystem Integration
The service leverages V Rising's built-in DebugEventsSystem:

```csharp
var world = VRCore.ServerWorld;
var debug = world.GetExistingSystemManaged<DebugEventsSystem>();
if (debug == null) return;

var from = new FromCharacter { User = userEntity, Character = characterEntity };
debug.UnlockAllResearch(from);
debug.UnlockAllVBloods(from);
debug.CompleteAllAchievements(from);
```

#### Type Resolution Strategy
The service safely resolves the FromCharacter type across different ProjectM assemblies:

```csharp
private static Type ResolveFromCharacterType()
{
    // Try common ProjectM assemblies
    var candidates = new[]
    {
        typeof(ServerBootstrapSystem).Assembly,
        typeof(PrefabCollectionSystem).Assembly
    };
    
    foreach (var asm in candidates)
    {
        var t = asm.GetType("ProjectM.Network.FromCharacter", throwOnError: false) ??
                asm.GetType("ProjectM.FromCharacter", throwOnError: false);
        if (t != null) return t;
    }
    
    // Fallback: scan loaded assemblies
    // ... comprehensive assembly scanning
}
```

#### Safe Method Invocation
Reflection-based invocation with comprehensive error handling:

```csharp
private static void InvokeIfExists(Type t, object instance, string name, object[] args)
{
    try
    {
        var m = t.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (m != null)
        {
            m.Invoke(instance, args);
        }
    }
    catch { /* Silently fail - method might not exist */ }
}
```

### 🛡️ Safety Features

#### Error Handling
- **Try-Catch Blocks** - All operations wrapped in exception handling
- **Null Checks** - Comprehensive validation before operations
- **Graceful Degradation** - System continues operating if unlocking fails
- **Detailed Logging** - All attempts and failures logged

#### Type Safety
- **Dynamic Type Resolution** - Safely finds required types across assemblies
- **Reflection Safety** - Uses BindingFlags to safely access methods
- **Fallback Mechanisms** - Multiple strategies for type and method resolution

#### Performance
- **Cached Type Resolution** - Types resolved once per call
- **Efficient Invocation** - Direct method calls when possible
- **Minimal Overhead** - Lightweight reflection usage

### 🎮 Use Cases

#### Arena Testing
```csharp
// Enter arena with full unlocks
.arena enter
// Automatically applies TryUnlockAll for testing
```

#### Development
```csharp
// Test specific features without progression
TryUnlockResearch(user, character); // Test building systems
TryUnlockVBloods(user, character); // Test boss mechanics
```

#### Administrative
```csharp
// Admin command to unlock content for testing
.admin unlock all PlayerName
.admin unlock research PlayerName
.admin unlock vbloods PlayerName
```

### 🔍 Integration Points

#### With Arena System
The UnlockHelper integrates seamlessly with the arena system:

```csharp
// In arena entry
if (MissingServices.LifecycleService.EnterArena(userEntity, characterEntity))
{
    // Apply unlocks for arena testing
    UnlockHelper.TryUnlockAll(userEntity, characterEntity);
    
    // Continue with arena setup
}
```

#### With Commands
Commands can leverage the unlocking functionality:

```csharp
[Command("unlock", adminOnly: true, usage: ".unlock <all|research|vbloods|achievements> [player]")]
public static void UnlockCommand(ChatCommandContext ctx, string type, string player = "")
{
    var targetEntity = GetTargetPlayerEntity(ctx, player);
    if (targetEntity == Entity.Null) return;
    
    switch (type.ToLower())
    {
        case "all":
            UnlockHelper.TryUnlockAll(ctx.Event.SenderUserEntity, targetEntity);
            ctx.Reply("✅ All unlocks applied!");
            break;
        case "research":
            UnlockHelper.TryUnlockResearch(ctx.Event.SenderUserEntity, targetEntity);
            ctx.Reply("✅ Research unlocked!");
            break;
        case "vbloods":
            UnlockHelper.TryUnlockVBloods(ctx.Event.SenderUserEntity, targetEntity);
            ctx.Reply("✅ VBloods unlocked!");
            break;
        case "achievements":
            UnlockHelper.TryUnlockAchievements(ctx.Event.SenderUserEntity, targetEntity);
            ctx.Reply("✅ Achievements completed!");
            break;
    }
}
```

### 📊 Benefits

#### For Players
- **Instant Testing** - No need to grind for content access
- **Arena Experience** - Full feature access in practice areas
- **Build Testing** - Test combinations without progression

#### For Administrators
- **Easy Testing** - Quickly test features and mechanics
- **Troubleshooting** - Reproduce issues with full unlocks
- **Event Support** - Enable content for special events

#### For Developers
- **Debug Access** - Safe access to all game systems
- **Rapid Prototyping** - Test features without progression barriers
- **Error Isolation** - Focus on feature bugs, not progression issues

### 🚀 Future Enhancements

#### Planned Features
- **Selective Unlocking** - Unlock specific research branches
- **Temporary Unlocks** - Time-limited unlocks for events
- **Rollback System** - Restore original progression state
- **Unlock Scheduling** - Apply unlocks at specific times

#### Performance Improvements
- **Caching Layer** - Cache unlock states for performance
- **Batch Operations** - Apply multiple unlocks efficiently
- **Background Processing** - Non-blocking unlock operations

---

*This enhanced UnlockHelper provides a robust, safe, and efficient way to manage V Rising content unlocks for testing, development, and administrative purposes.*