using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using VAuto.Data; // Ensure this is present for ValidationResult

namespace VAuto.Services.Systems
{
    #region Build Data Classes

    public class BuildData
    {
        public string BuildId { get; set; }
        public string StructureName { get; set; }
        public float3 Position { get; set; }
        public quaternion Rotation { get; set; }
        public string PrefabGUID { get; set; } // Now strictly validated against PrefabResolver
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ulong OwnerPlatformId { get; set; }
        public Dictionary<string, object> BuildProperties { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Self-integrity check for basic data presence
        /// </summary>
        public void CheckIntegrity(ValidationResult result)
        {
            if (string.IsNullOrEmpty(PrefabGUID))
                result.AddError($"BuildData '{StructureName}' is missing a PrefabGUID.");
            
            if (math.isnan(Position.x) || math.isnan(Position.y) || math.isnan(Position.z))
                result.AddError($"BuildData '{StructureName}' has an invalid (NaN) position.");
        }
    }

    public class BuildTemplate
    {
        public string TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string Description { get; set; }
        public List<BuildData> Structures { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public ulong CreatorPlatformId { get; set; }
        public bool IsPublic { get; set; } = false;
        public List<string> Categories { get; set; } = new();
        public Dictionary<string, object> TemplateMetadata { get; set; } = new();
        public int Version { get; set; } = 1;
    }

    // Existing BuildCategory remains stable
    public class BuildCategory
    {
        public string CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public List<string> Subcategories { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
        public int SortOrder { get; set; } = 0;
    }

    #endregion

    #region Logistics Repair Definition

    public class LogisticsRepairDefinition
    {
        public string RepairId { get; set; }
        public string StructureId { get; set; }
        public string StructureType { get; set; }
        public float RepairAmount { get; set; }
        public float RepairCost { get; set; }
        public List<string> RequiredResources { get; set; } = new();
        public Dictionary<string, int> ResourceQuantities { get; set; } = new();
        public float RepairTime { get; set; }
        public bool IsAutomaticRepair { get; set; } = false;
        public int Priority { get; set; } = 0;
        
        // Additional properties expected by the execution engine
        public string Castle { get; set; }
        public string EquipmentId { get; set; }
    }

    #endregion

    #region Structure Data Classes

    public class StructureData
    {
        public string StructureId { get; set; }
        public string StructureName { get; set; }
        public string StructureType { get; set; }
        public float3 Position { get; set; }
        public quaternion Rotation { get; set; }
        public string PrefabGUID { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ulong OwnerPlatformId { get; set; }
        public float Health { get; set; } = 100f;
        public float MaxHealth { get; set; } = 100f;
        public bool IsEnabled { get; set; } = true;
        public Dictionary<string, object> StructureProperties { get; set; } = new();
        public List<string> Tags { get; set; } = new();

        public StructureStatus Status { get; set; } = StructureStatus.Active;
    }

    public enum StructureStatus
    {
        Active,
        Inactive,
        Damaged,
        Destroyed,
        UnderConstruction,
        Maintenance
    }

    #endregion

    #region AI Content Classes

    // Standardized results for AI tool-calling integrations
    public class CallToolResult
    {
        public bool Success { get; set; }
        public string ToolName { get; set; }
        public object Result { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Exception Exception { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    #endregion
}
