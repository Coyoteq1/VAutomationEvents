// Global using directives to make extensions and stubs available throughout the project
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.IO;
global using System.Threading.Tasks;

// Unity Core
global using Unity.Entities;
global using Unity.Mathematics;
global using Unity.Transforms;
global using Unity.Collections;
global using UnityEngine;
global using NativeArray = Unity.Collections.NativeArray;
global using LocalTransform = Unity.Transforms.LocalTransform;
global using ComponentType = Unity.Entities.ComponentType;
global using DynamicBuffer = Unity.Entities.DynamicBuffer;

// Unity ECS
global using Unity.Entities.Exposed;
global using Unity.Entities.Internal;
global using Unity.Entities.Exposed;
global using float2 = Unity.Mathematics.float2;

// ECS System Attributes
global using ISystem = Unity.Entities.ISystem;
global using EntityQuery = Unity.Entities.EntityQuery;
global using UpdateInGroupAttribute = Unity.Entities.UpdateInGroupAttribute;
global using UpdateAfterAttribute = Unity.Entities.UpdateAfterAttribute;
global using UpdateBeforeAttribute = Unity.Entities.UpdateBeforeAttribute;
global using SimulationSystemGroup = Unity.Entities.SimulationSystemGroup;
global using TransformSystemGroup = Unity.Entities.TransformSystemGroup;
global using PresentationSystemGroup = Unity.Entities.PresentationSystemGroup;
global using EntityCommandBuffer = Unity.Entities.EntityCommandBuffer;

// BepInEx
global using BepInEx;
global using BepInEx.Logging;
global using BepInEx.Configuration;
global using BepInEx.Unity.IL2CPP;
global using BepInPluginAttribute = BepInEx.BepInPluginAttribute;
global using BepInDependencyAttribute = BepInEx.BepInDependencyAttribute;
global using BepInProcessAttribute = BepInEx.BepInProcessAttribute;
global using ConfigFile = BepInEx.Configuration.ConfigFile;
global using Logging = BepInEx.Logging;

// JSON Serialization
global using System.Text.Json;
global using Utf8JsonWriter = System.Text.Json.Utf8JsonWriter;
global using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;
global using Json = System.Text.Json.JsonSerializer;
global using Utf8JsonReader = System.Text.Json.Utf8JsonReader;

// Unity UI
global using Rect = UnityEngine.Rect;
global using User = ProjectM.Shared.User;
global using Translation = ProjectM.Shared.Translation;
global using Color = UnityEngine.Color;
global using MonoBehaviour = UnityEngine.MonoBehaviour;
global using GameObject = UnityEngine.GameObject;
global using Vector3 = UnityEngine.Vector3;
global using World = UnityEngine.World;
global using SystemBase = UnityEngine.SystemBase;

// Harmony
global using HarmonyLib;

// Vampire Command Framework
global using VampireCommandFramework;

// ProjectM Core
global using ProjectM;
global using ProjectM.Network;
global using ProjectM.Shared;
global using ProjectM.Gameplay;
global using ProjectM.Gameplay.Systems;
global using Coroutine = ProjectM.Shared.Coroutine;
global using ActivateVBloodAbilitySystem = ProjectM.Gameplay.Systems.ActivateVBloodAbilitySystem;
global using DebugEventsSystem = ProjectM.Gameplay.Systems.DebugEventsSystem;

// Stunlock
global using Stunlock.Core;

// Common type aliases to resolve compilation issues
global using Entity = Unity.Entities.Entity;
global using float3 = Unity.Mathematics.float3;
global using float4 = Unity.Mathematics.float4;
global using quaternion = Unity.Mathematics.quaternion;
global using int2 = Unity.Mathematics.int2;
global using ManualLogSource = BepInEx.Logging.ManualLogSource;
global using PrefabGUID = Stunlock.Core.PrefabGUID;
global using FixedString64Bytes = Unity.Collections.FixedString64Bytes;
global using IComponentData = Unity.Entities.IComponentData;
global using SystemState = Unity.Entities.SystemState;
global using EntityManager = Unity.Entities.EntityManager;

// Missing data types - need to be defined or imported
global using Zone = VAuto.Data.Zone;
global using GearEntry = VAuto.Data.GearEntry;
global using BossEntry = VAuto.Data.BossEntry;
global using ZoneEffectResult = VAuto.Data.ZoneEffectResult;
global using ZoneManagerService = VAuto.Services.Systems.ZoneManagerService;
global using AILearningService = VAuto.Services.Systems.AILearningService;

// VRising mod extensions and stubs
global using VAuto.Extensions;

// Core namespaces
global using VAuto.Core;
global using VAuto.Data;
global using VAuto.Data.Zones;
global using VAuto.Utilities;
global using VAuto.Services;
global using VAuto.Services.Systems;
global using VAuto.Services.Interfaces;
global using VAuto.Automation;
global using VAuto.Commands;