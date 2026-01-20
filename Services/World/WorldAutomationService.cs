using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM;
using ProjectM.Network;
using VAuto.Core;
using BepInEx.Logging;

namespace VAuto.Services.World
{
    /// <summary>
    /// World Automation Service - event-driven automation without player references.
    /// Trigger → Action pipelines for world objects only.
    /// </summary>
    public static class WorldAutomationService
    {
        private static readonly Dictionary<string, AutomationRule> _automationRules = new();
        private static readonly Dictionary<Entity, TriggerState> _triggerStates = new();
        private static readonly Queue<DelayedAction> _delayedActions = new();
        private static readonly object _lock = new object();
        private static ManualLogSource _log;

        static WorldAutomationService()
        {
            _log = Plugin.Logger;
        }

        #region Rule Management
        /// <summary>
        /// Add an automation rule.
        /// </summary>
        public static void AddRule(string ruleName, AutomationRule rule)
        {
            lock (_lock)
            {
                _automationRules[ruleName] = rule;
                _log?.LogInfo($"[WorldAutomationService] Added automation rule: {ruleName}");
            }
        }

        /// <summary>
        /// Remove an automation rule.
        /// </summary>
        public static void RemoveRule(string ruleName)
        {
            lock (_lock)
            {
                if (_automationRules.Remove(ruleName))
                {
                    _log?.LogInfo($"[WorldAutomationService] Removed automation rule: {ruleName}");
                }
            }
        }

        /// <summary>
        /// Get all automation rules.
        /// </summary>
        public static Dictionary<string, AutomationRule> GetRules()
        {
            lock (_lock)
            {
                return new Dictionary<string, AutomationRule>(_automationRules);
            }
        }

        /// <summary>
        /// Clear all automation rules.
        /// </summary>
        public static void ClearAllRules()
        {
            lock (_lock)
            {
                _automationRules.Clear();
                _triggerStates.Clear();
                _delayedActions.Clear();
                _log?.LogInfo("[WorldAutomationService] Cleared all automation rules");
            }
        }
        #endregion

        #region Trigger Management
        /// <summary>
        /// Register a trigger entity.
        /// </summary>
        public static void RegisterTrigger(Entity triggerEntity, string triggerName, float radius = 5f)
        {
            lock (_lock)
            {
                _triggerStates[triggerEntity] = new TriggerState
                {
                    Name = triggerName,
                    Radius = radius,
                    LastTriggerTime = DateTime.MinValue,
                    IsEnabled = true
                };
                _log?.LogInfo($"[WorldAutomationService] Registered trigger: {triggerName}");
            }
        }

        /// <summary>
        /// Unregister a trigger entity.
        /// </summary>
        public static void UnregisterTrigger(Entity triggerEntity)
        {
            lock (_lock)
            {
                if (_triggerStates.Remove(triggerEntity))
                {
                    _log?.LogInfo("[WorldAutomationService] Unregistered trigger");
                }
            }
        }

        /// <summary>
        /// Enable/disable a trigger.
        /// </summary>
        public static void SetTriggerEnabled(Entity triggerEntity, bool enabled)
        {
            lock (_lock)
            {
                if (_triggerStates.TryGetValue(triggerEntity, out var state))
                {
                    state.IsEnabled = enabled;
                    _log?.LogInfo($"[WorldAutomationService] Trigger {(enabled ? "enabled" : "disabled")}");
                }
            }
        }
        #endregion

        #region Update System
        /// <summary>
        /// Update automation system (called from ECS system).
        /// </summary>
        public static void Update(float deltaTime)
        {
            lock (_lock)
            {
                try
                {
                    // Process delayed actions
                    ProcessDelayedActions();

                    // Check triggers
                    CheckTriggers();
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[WorldAutomationService] Update error: {ex.Message}");
                }
            }
        }

        private static void ProcessDelayedActions()
        {
            var now = DateTime.UtcNow;
            var readyActions = new List<DelayedAction>();

            while (_delayedActions.Count > 0 && _delayedActions.Peek().ExecuteTime <= now)
            {
                readyActions.Add(_delayedActions.Dequeue());
            }

            foreach (var action in readyActions)
            {
                ExecuteAction(action.Action);
            }
        }

        private static void CheckTriggers()
        {
            var em = VRCore.EntityManager;
            var now = DateTime.UtcNow;

            foreach (var kvp in _triggerStates.ToList())
            {
                var triggerEntity = kvp.Key;
                var triggerState = kvp.Value;

                if (!triggerState.IsEnabled || !em.Exists(triggerEntity))
                    continue;

                // Check cooldown
                if ((now - triggerState.LastTriggerTime).TotalSeconds < 1.0)
                    continue;

                // Check for entities in radius
                var triggerPos = em.GetComponentData<Translation>(triggerEntity).Value;
                var nearbyEntities = WorldObjectService.GetInRadius(triggerPos, triggerState.Radius);

                if (nearbyEntities.Any())
                {
                    // Trigger fired
                    triggerState.LastTriggerTime = now;
                    _log?.LogInfo($"[WorldAutomationService] Trigger fired: {triggerState.Name}");

                    // Execute rules for this trigger
                    foreach (var rule in _automationRules.Values.Where(r => r.TriggerName == triggerState.Name))
                    {
                        ExecuteRule(rule);
                    }
                }
            }
        }
        #endregion

        #region Action Execution
        private static void ExecuteRule(AutomationRule rule)
        {
            _log?.LogInfo($"[WorldAutomationService] Executing rule with {rule.Actions.Count} actions");

            foreach (var action in rule.Actions)
            {
                if (action.Type == AutomationActionType.Delay)
                {
                    // Schedule delayed action
                    var delayedAction = new DelayedAction
                    {
                        Action = action,
                        ExecuteTime = DateTime.UtcNow.AddSeconds(action.GetDelaySeconds())
                    };
                    _delayedActions.Enqueue(delayedAction);
                }
                else
                {
                    ExecuteAction(action);
                }
            }
        }

        private static void ExecuteAction(AutomationAction action)
        {
            try
            {
                switch (action.Type)
                {
                    case AutomationActionType.OpenDoor:
                        SetDoorState(action.Target, "open");
                        break;

                    case AutomationActionType.CloseDoor:
                        SetDoorState(action.Target, "closed");
                        break;

                    case AutomationActionType.ToggleDoor:
                        ToggleDoor(action.Target);
                        break;

                    case AutomationActionType.LockDoor:
                        SetDoorLocked(action.Target, true);
                        break;

                    case AutomationActionType.UnlockDoor:
                        SetDoorLocked(action.Target, false);
                        break;

                    case AutomationActionType.SpawnObject:
                        SpawnObject(action);
                        break;

                    case AutomationActionType.DestroyObject:
                        DestroyObject(action.Target);
                        break;

                    case AutomationActionType.TriggerEvent:
                        TriggerCustomEvent(action.EventName);
                        break;
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[WorldAutomationService] Failed to execute action {action.Type}: {ex.Message}");
            }
        }

        private static void SetDoorState(string doorName, string state)
        {
            var doors = WorldObjectService.GetByPrefab(doorName).Where(o => o.Type == WorldObjectType.Door);
            foreach (var door in doors)
            {
                door.Properties["state"] = state;
                _log?.LogInfo($"[WorldAutomationService] Set door {doorName} to {state}");
            }
        }

        private static void ToggleDoor(string doorName)
        {
            var doors = WorldObjectService.GetByPrefab(doorName).Where(o => o.Type == WorldObjectType.Door);
            foreach (var door in doors)
            {
                var currentState = door.Properties.GetValueOrDefault("state", "closed").ToString();
                var newState = currentState == "open" ? "closed" : "open";
                door.Properties["state"] = newState;
                _log?.LogInfo($"[WorldAutomationService] Toggled door {doorName} to {newState}");
            }
        }

        private static void SetDoorLocked(string doorName, bool locked)
        {
            var doors = WorldObjectService.GetByPrefab(doorName).Where(o => o.Type == WorldObjectType.Door);
            foreach (var door in doors)
            {
                door.Properties["locked"] = locked;
                _log?.LogInfo($"[WorldAutomationService] Set door {doorName} locked to {locked}");
            }
        }

        private static void SpawnObject(AutomationAction action)
        {
            var position = action.GetSpawnPosition();
            var prefabName = action.GetPrefabName();

            Entity entity;
            switch (action.GetObjectType())
            {
                case WorldObjectType.Tile:
                    entity = WorldSpawnService.SpawnTile(prefabName, position);
                    break;
                case WorldObjectType.Door:
                    entity = WorldSpawnService.SpawnDoor(prefabName, position);
                    break;
                case WorldObjectType.Trigger:
                    entity = WorldSpawnService.SpawnTrigger(prefabName, position);
                    break;
                default:
                    entity = WorldSpawnService.SpawnStructure(prefabName, position);
                    break;
            }

            _log?.LogInfo($"[WorldAutomationService] Spawned {action.GetObjectType()} {prefabName}");
        }

        private static void DestroyObject(string targetName)
        {
            var objects = WorldObjectService.GetByPrefab(targetName);
            foreach (var obj in objects)
            {
                WorldObjectService.Remove(obj.Entity);
            }
            _log?.LogInfo($"[WorldAutomationService] Destroyed {objects.Count()} objects: {targetName}");
        }

        private static void TriggerCustomEvent(string eventName)
        {
            _log?.LogInfo($"[WorldAutomationService] Custom event triggered: {eventName}");
            // Custom event handling can be extended here
        }
        #endregion

        #region Data Structures
        public class AutomationRule
        {
            public string TriggerName { get; set; }
            public List<AutomationAction> Actions { get; set; } = new();
        }

        public class AutomationAction
        {
            public AutomationActionType Type { get; set; }
            public string Target { get; set; }
            public string EventName { get; set; }
            public float3 Position { get; set; }
            public string PrefabName { get; set; }
            public WorldObjectType ObjectType { get; set; }
            public float DelaySeconds { get; set; }
            public Dictionary<string, object> Parameters { get; set; } = new();

            // Helper methods
            public float GetDelaySeconds() => DelaySeconds;
            public float3 GetSpawnPosition() => Position;
            public string GetPrefabName() => PrefabName;
            public WorldObjectType GetObjectType() => ObjectType;
        }

        private class TriggerState
        {
            public string Name { get; set; }
            public float Radius { get; set; }
            public DateTime LastTriggerTime { get; set; }
            public bool IsEnabled { get; set; }
        }

        private class DelayedAction
        {
            public AutomationAction Action { get; set; }
            public DateTime ExecuteTime { get; set; }
        }

        public enum AutomationActionType
        {
            OpenDoor,
            CloseDoor,
            ToggleDoor,
            LockDoor,
            UnlockDoor,
            SpawnObject,
            DestroyObject,
            Delay,
            TriggerEvent
        }
        #endregion
    }
}
