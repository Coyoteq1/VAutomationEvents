# VAuto API Conversion Guide
## Complete Namespace Standardization and Migration Guide

---

## 📋 API PATTERNS ANALYSIS

### **CURRENT NAMESPACE MIXES IDENTIFIED:**

| Pattern | Current Usage | Target Usage | Status |
|---------|---------------|--------------|---------|
| **EntityManager** | `Core.EntityManager`, `VAutoCore.EntityManager`, `VRCore.EM` | `VRCore.EM` | 🔄 PARTIAL |
| **ServerWorld** | `VRCore.ServerWorld`, `VAutoCore.World` | `VRCore.ServerWorld` | ✅ CONSISTENT |
| **PrefabCollection** | `Core.PrefabCollection`, `VAutoCore.SystemService.PrefabCollectionSystem` | `VAutoCore.SystemService.PrefabCollectionSystem` | 🔄 MIXED |
| **ServerTime** | `Core.ServerTime`, `Core.ServerGameManager.ServerTime` | `VRCore.ServerGameManager.ServerTime` | 🔄 MIXED |
| **Plugin** | `Core.Plugin`, `Plugin` | `Plugin` | ✅ CONSISTENT |
| **ComponentType** | `typeof(T)`, `new ComponentType(Il2CppType.Of<T>())` | `new ComponentType(Il2CppType.Of<T>())` | 🔄 MIXED |

---

## 🔧 COMPLETE CONVERSION MAPPINGS

### **1. ENTITY MANAGER PATTERNS**

```csharp
// ❌ OLD PATTERNS TO REPLACE:
Core.EntityManager.HasComponent<T>(entity)
Core.EntityManager.GetComponentData<T>(entity)
Core.EntityManager.SetComponentData<T>(entity, value)
Core.EntityManager.AddComponent<T>(entity)
Core.EntityManager.RemoveComponent<T>(entity)
VAutoCore.EntityManager.HasComponent<T>(entity)
VAutoCore.EntityManager.GetComponentData<T>(entity)
VAutoCore.EntityManager.SetComponentData<T>(entity, value)
VAutoCore.EntityManager.AddComponent<T>(entity)
VAutoCore.EntityManager.RemoveComponent<T>(entity)

// ✅ NEW STANDARDIZED PATTERN:
VRCore.EM.HasComponent(entity, new ComponentType(Il2CppType.Of<T>()))
VRCore.EM.GetComponentData<T>(entity)
VRCore.EM.SetComponentData(entity, value)
VRCore.EM.AddComponent(entity, new ComponentType(Il2CppType.Of<T>()))
VRCore.EM.RemoveComponent(entity, new ComponentType(Il2CppType.Of<T>()))
```

### **2. SERVER SYSTEM PATTERNS**

```csharp
// ❌ OLD PATTERNS TO REPLACE:
Core.ServerTime
Core.ServerGameManager.ServerTime
VAutoCore.World
VAutoCore.EntityManager

// ✅ NEW STANDARDIZED PATTERN:
VRCore.ServerGameManager.ServerTime
VRCore.ServerWorld
VRCore.EM
```

### **3. PREFAB COLLECTION PATTERNS**

```csharp
// ❌ OLD PATTERNS TO REPLACE:
Core.PrefabCollection._PrefabGuidToEntityMap.TryGetValue(guid, out entity)
VRCore.PrefabCollection._PrefabGuidToEntityMap.TryGetValue(guid, out entity)

// ✅ NEW STANDARDIZED PATTERN:
VAutoCore.SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(guid, out entity)
```

### **4. PLUGIN PATTERNS**

```csharp
// ❌ OLD PATTERNS TO REPLACE:
Core.Plugin.Logger
Core.Plugin.StartCoroutine()

// ✅ NEW STANDARDIZED PATTERN:
Plugin.Logger
Plugin.StartCoroutine()
```

---

## 📁 FILES REQUIRING CONVERSION

### **HIGH PRIORITY - CORE ARENA SERVICES:**

#### **✅ ALREADY CONVERTED:**
- `ArenaVirtualContext.cs` - ✅ Using VRCore.EM patterns
- `ArenaStateService.cs` - ✅ Using VRCore.EM + ComponentType(Il2CppType.Of<T>())
- `ArenaSnapshotService.cs` - ✅ Using VRCore.EM + VRCore.ServerGameManager.ServerTime
- `ArenaVBloodService.cs` - ✅ Using VRCore.ServerGameManager.ServerTime
- `ArenaObjectService.cs` - ✅ Using VRCore.EM patterns
- `ArenaGlowService.cs` - ✅ Using VRCore.EM + VAutoCore.SystemService.PrefabCollectionSystem

#### **🔄 NEEDS CONVERSION:**
- `ArenaOrchestrator.cs` - Still using `Core.EntityManager`
- `ArenaBuildService.cs` - Mixed patterns, needs Entity→User conversion fixes
- `ArenaHealthService.cs` - Still using `Core.Plugin`
- `ArenaPortalService.cs` - Mixed `VAutoCore.EntityManager` and `VAutoCore.SystemService`

### **MEDIUM PRIORITY - SUPPORTING SERVICES:**

#### **🔄 NEEDS CONVERSION:**
- `CastleObjectIntegrationService.cs` - Using `Core.EntityManager`
- `EntityValidationService.cs` - Using `VAutoCore.EntityManager`
- `MapIconService.cs` - Using `VAutoCore.EntityManager`
- `NameTagService.cs` - Using `VAutoCore.EntityManager`

#### **✅ ALREADY CONSISTENT:**
- `LifecycleService.cs` - ✅ Using VRCore.EM
- `TeleportService.cs` - ✅ Using VRCore.EM + VRCore.ServerWorld
- `PlayerService.cs` - ✅ Using VRCore.EM
- `VBloodService.cs` - ✅ Using VRCore.EM
- `VBloodUIService.cs` - ✅ Using VRCore.EM

---

## 🔨 AUTOMATED CONVERSION SCRIPT

### **PATTERN 1: ENTITY MANAGER CONVERSION**
```regex
# Find and replace patterns:
Core\.EntityManager\.HasComponent<([^>]+)>\(([^)]+)\)
# Replace with:
VRCore.EM.HasComponent($2, new ComponentType(Il2CppType.Of<$1>()))

Core\.EntityManager\.GetComponentData<([^>]+)>\(([^)]+)\)
# Replace with:
VRCore.EM.GetComponentData<$1>($2)

Core\.EntityManager\.SetComponentData<([^>]+)>\(([^,]+),\s*([^)]+)\)
# Replace with:
VRCore.EM.SetComponentData($1, $2)

Core\.EntityManager\.AddComponent<([^>]+)>\(([^)]+)\)
# Replace with:
VRCore.EM.AddComponent($2, new ComponentType(Il2CppType.Of<$1>()))

Core\.EntityManager\.RemoveComponent<([^>]+)>\(([^)]+)\)
# Replace with:
VRCore.EM.RemoveComponent($2, new ComponentType(Il2CppType.Of<$1>()))
```

### **PATTERN 2: CORE NAMESPACE CONVERSION**
```regex
# Find and replace patterns:
Core\.ServerTime
# Replace with:
VRCore.ServerGameManager.ServerTime

Core\.ServerGameManager\.ServerTime
# Replace with:
VRCore.ServerGameManager.ServerTime

Core\.Plugin\.
# Replace with:
Plugin.

Core\.EntityManager
# Replace with:
VRCore.EM

VAutoCore\.EntityManager
# Replace with:
VRCore.EM

VAutoCore\.World
# Replace with:
VRCore.ServerWorld
```

### **PATTERN 3: PREFAB COLLECTION CONVERSION**
```regex
# Find and replace patterns:
Core\.PrefabCollection\._
# Replace with:
VAutoCore.SystemService.PrefabCollectionSystem._

VRCore\.PrefabCollection\._
# Replace with:
VAutoCore.SystemService.PrefabCollectionSystem._
```

---

## 📊 CONVERSION STATUS TRACKER

### **PHASE 1: CORE ARENA SERVICES (COMPLETED)**
- [x] ArenaVirtualContext.cs
- [x] ArenaStateService.cs  
- [x] ArenaSnapshotService.cs
- [x] ArenaVBloodService.cs
- [x] ArenaObjectService.cs
- [x] ArenaGlowService.cs

### **PHASE 2: REMAINING ARENA SERVICES (IN PROGRESS)**
- [ ] ArenaOrchestrator.cs
- [ ] ArenaBuildService.cs (Entity→User conversion)
- [ ] ArenaHealthService.cs
- [ ] ArenaPortalService.cs

### **PHASE 3: SUPPORTING SERVICES (PENDING)**
- [ ] CastleObjectIntegrationService.cs
- [ ] EntityValidationService.cs
- [ ] MapIconService.cs
- [ ] NameTagService.cs

### **PHASE 4: LEGACY SERVICES (LOW PRIORITY)**
- [ ] ArenaComponentSaverMinimal.cs (BlobAssetReference API issues)
- [ ] Other legacy services with mixed patterns

---

## 🎯 FINAL TARGET STATE

### **STANDARDIZED API PATTERNS:**
```csharp
// Entity Management
VRCore.EM.HasComponent(entity, new ComponentType(Il2CppType.Of<T>()))
VRCore.EM.GetComponentData<T>(entity)
VRCore.EM.SetComponentData(entity, value)
VRCore.EM.AddComponent(entity, new ComponentType(Il2CppType.Of<T>()))
VRCore.EM.RemoveComponent(entity, new ComponentType(Il2CppType.Of<T>()))

// Server Systems
VRCore.ServerWorld
VRCore.ServerGameManager.ServerTime
VRCore.EM

// Prefab Collection
VAutoCore.SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap
VAutoCore.SystemService.PrefabCollectionSystem._SpawnableNameToPrefabGuidDictionary

// Plugin Access
Plugin.Logger
Plugin.StartCoroutine()

// Component Types
new ComponentType(Il2CppType.Of<T>())
```

---

## 🚀 EXECUTION PLAN

### **IMMEDIATE ACTIONS:**
1. **Fix remaining Core.EntityManager references** in ArenaOrchestrator.cs
2. **Complete Entity→User conversion** in ArenaBuildService.cs  
3. **Standardize Plugin access** in ArenaHealthService.cs
4. **Fix mixed patterns** in ArenaPortalService.cs

### **MEDIUM TERM:**
1. **Convert supporting services** to VRCore.EM patterns
2. **Standardize all VAutoCore.EntityManager** references
3. **Fix PrefabCollection access** across all services

### **LONG TERM:**
1. **Address BlobAssetReference API issues** in legacy services
2. **Create comprehensive unit tests** for standardized patterns
3. **Document final API surface** for external consumers

---

## 📝 NOTES

- **VRCore.EM** is the target standard for all EntityManager access
- **VAutoCore.SystemService.PrefabCollectionSystem** is the target for PrefabCollection access
- **VRCore.ServerGameManager.ServerTime** is the target for all time access
- **Plugin** is the target for all plugin access (no Core. prefix)
- **ComponentType(Il2CppType.Of<T>())** is required for type-safe component operations

This conversion guide ensures complete API consistency across all VAuto services while maintaining compatibility with VRising's Entity Component System.
