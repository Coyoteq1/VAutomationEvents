using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using VAuto.Services.Interfaces;
using VAuto.Core;
using VAuto.Data;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Castle Automation Adapter - Translates automation plan commands into castle actions
    /// Validates build rules and executes operations via existing Castle services
    /// </summary>
    public class CastleAutomationAdapter : IService
    {
        private static CastleAutomationAdapter _instance;
        public static CastleAutomationAdapter Instance => _instance ??= new CastleAutomationAdapter();

        private bool _isInitialized;
        private readonly object _lock = new object();

        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => Plugin.Logger;

        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            Log?.LogInfo("[CastleAutomationAdapter] Initialized");
        }

        public void Cleanup()
        {
            if (!_isInitialized) return;
            _isInitialized = false;
            Log?.LogInfo("[CastleAutomationAdapter] Cleaned up");
        }

        /// <summary>
        /// Execute a castle build operation from automation plan
        /// </summary>
        public CastleBuildResult BuildStructure(CastleBuildCommand command)
        {
            lock (_lock)
            {
                try
                {
                    Log?.LogInfo($"[CastleAutomationAdapter] Executing build: {command.StructureType} at {command.Position}");

                    // Validate castle exists
                    var castleData = CastleRegistryService.Instance.GetCastle(command.CastleId);
                    if (castleData == null)
                    {
                        return new CastleBuildResult
                        {
                            Success = false,
                            ErrorMessage = $"Castle {command.CastleId} not found"
                        };
                    }

                    // Validate position is within castle bounds
                    if (!CastleRegistryService.Instance.IsPositionInCastle(command.CastleId, command.Position))
                    {
                        return new CastleBuildResult
                        {
                            Success = false,
                            ErrorMessage = $"Position {command.Position} is outside castle bounds"
                        };
                    }

                    // Validate structure can be placed
                    if (!CastleRegistryService.Instance.CanPlaceStructure(command.CastleId, command.Position, command.MinDistance))
                    {
                        return new CastleBuildResult
                        {
                            Success = false,
                            ErrorMessage = $"Cannot place structure at {command.Position} - too close to existing structures"
                        };
                    }

                    // Resolve prefab
                    var prefabGuid = PrefabResolverService.Instance.ResolvePrefab(command.StructureType);
                    if (prefabGuid == null)
                    {
                        return new CastleBuildResult
                        {
                            Success = false,
                            ErrorMessage = $"Unknown structure type: {command.StructureType}"
                        };
                    }

                    // Execute build via ArenaBuildService
                    var buildResult = ArenaBuildService.Instance.BuildStructure(
                        Entity.Null, // Admin entity
                        command.StructureType,
                        command.Position,
                        command.Rotation
                    );

                    if (buildResult)
                    {
                        // Register the new structure (this would need to be called after entity is created)
                        // For now, just log success
                        Log?.LogInfo($"[CastleAutomationAdapter] Successfully built {command.StructureType}");
                        return new CastleBuildResult { Success = true };
                    }
                    else
                    {
                        return new CastleBuildResult
                        {
                            Success = false,
                            ErrorMessage = "Build operation failed"
                        };
                    }
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[CastleAutomationAdapter] Build failed: {ex.Message}");
                    return new CastleBuildResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                }
            }
        }

        /// <summary>
        /// Execute multiple castle build operations
        /// </summary>
        public List<CastleBuildResult> BuildStructures(List<CastleBuildCommand> commands)
        {
            var results = new List<CastleBuildResult>();

            foreach (var command in commands)
            {
                var result = BuildStructure(command);
                results.Add(result);

                // Stop on first failure if not configured to continue
                if (!result.Success && !command.ContinueOnFailure)
                    break;
            }

            return results;
        }

        /// <summary>
        /// Validate a castle build command without executing it
        /// </summary>
        public CastleBuildResult ValidateBuildCommand(CastleBuildCommand command)
        {
            try
            {
                // Validate castle exists
                var castleData = CastleRegistryService.Instance.GetCastle(command.CastleId);
                if (castleData == null)
                {
                    return new CastleBuildResult
                    {
                        Success = false,
                        ErrorMessage = $"Castle {command.CastleId} not found"
                    };
                }

                // Validate position
                if (!CastleRegistryService.Instance.IsPositionInCastle(command.CastleId, command.Position))
                {
                    return new CastleBuildResult
                    {
                        Success = false,
                        ErrorMessage = $"Position {command.Position} is outside castle bounds"
                    };
                }

                // Validate placement
                if (!CastleRegistryService.Instance.CanPlaceStructure(command.CastleId, command.Position, command.MinDistance))
                {
                    return new CastleBuildResult
                    {
                        Success = false,
                        ErrorMessage = $"Cannot place structure at {command.Position}"
                    };
                }

                // Validate prefab
                var prefabGuid = PrefabResolverService.Instance.ResolvePrefab(command.StructureType);
                if (prefabGuid == null)
                {
                    return new CastleBuildResult
                    {
                        Success = false,
                        ErrorMessage = $"Unknown structure type: {command.StructureType}"
                    };
                }

                return new CastleBuildResult { Success = true };
            }
            catch (Exception ex)
            {
                return new CastleBuildResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Get available structure types for a castle
        /// </summary>
        public List<string> GetAvailableStructures(int castleId)
        {
            // This would integrate with castle-specific available structures
            // For now, return generic list
            return ArenaBuildService.Instance.GetAvailableStructures();
        }

        /// <summary>
        /// Check if a castle has reached its structure limit
        /// </summary>
        public bool IsAtStructureLimit(int castleId, int maxStructures = 100)
        {
            var currentCount = CastleRegistryService.Instance.GetStructureCount(castleId);
            return currentCount >= maxStructures;
        }
    }

    /// <summary>
    /// Castle build command structure
    /// </summary>
    public class CastleBuildCommand
    {
        public int CastleId { get; set; }
        public string StructureType { get; set; }
        public float3 Position { get; set; }
        public quaternion? Rotation { get; set; }
        public float MinDistance { get; set; } = 2f;
        public bool ContinueOnFailure { get; set; } = false;
    }

    /// <summary>
    /// Castle build result structure
    /// </summary>
    public class CastleBuildResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Entity? CreatedEntity { get; set; }
    }
}