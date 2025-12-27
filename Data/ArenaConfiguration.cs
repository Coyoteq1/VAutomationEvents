using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;

namespace VAuto.Data
{
    /// <summary>
    /// Arena position and configuration data
    /// </summary>
    public struct ArenaPosition
    {
        public float3 Position { get; set; }
        public int CenterRadius { get; set; }
        public int ZoneRadius { get; set; }
        public string Name { get; set; }
        
        public ArenaPosition(float3 position, int centerRadius, int zoneRadius, string name = "")
        {
            Position = position;
            CenterRadius = centerRadius;
            ZoneRadius = zoneRadius;
            Name = name;
        }
    }

    /// <summary>
    /// Arena configuration data manager
    /// </summary>
    public static class ArenaConfiguration
    {
        private static readonly List<ArenaPosition> _arenaPositions = new();
        
        /// <summary>
        /// Add arena position from coordinate data
        /// </summary>
        public static void AddArenaPosition(float3 position, int centerRadius, int zoneRadius, string name = "")
        {
            _arenaPositions.Add(new ArenaPosition(position, centerRadius, zoneRadius, name));
        }
        
        /// <summary>
        /// Get all arena positions
        /// </summary>
        public static IReadOnlyList<ArenaPosition> GetArenaPositions()
        {
            return _arenaPositions.AsReadOnly();
        }
        
        /// <summary>
        /// Get arena position by name
        /// </summary>
        public static ArenaPosition? GetArenaPosition(string name)
        {
            return _arenaPositions.FirstOrDefault(pos => pos.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Parse zone configuration string and add positions
        /// Format: "Name|x,y,z|centerRadius|zoneRadius;Name2|x2,y2,z2|centerRadius2|zoneRadius2"
        /// </summary>
        public static void ParseAndAddZones(string zoneData)
        {
            if (string.IsNullOrEmpty(zoneData)) return;
            
            var zones = zoneData.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var zone in zones)
            {
                var trimmedZone = zone.Trim();
                if (string.IsNullOrEmpty(trimmedZone)) continue;
                
                // Parse format: "Arena|-1000,5,-500|25|50"
                var parts = trimmedZone.Split('|');
                if (parts.Length >= 3)
                {
                    var name = parts[0].Trim();
                    var coordsPart = parts[1].Trim();
                    var radiiPart = parts[2].Trim();
                    
                    if (ParseCoordinates(coordsPart, out var position) && 
                        ParseRadii(radiiPart, out var centerRadius, out var zoneRadius))
                    {
                        _arenaPositions.Add(new ArenaPosition(position, centerRadius, zoneRadius, name));
                    }
                }
            }
        }
        
        /// <summary>
        /// Parse coordinate string "x,y,z"
        /// </summary>
        private static bool ParseCoordinates(string coordStr, out float3 result)
        {
            var parts = coordStr.Split(',');
            if (parts.Length == 3 &&
                float.TryParse(parts[0].Trim(), out var x) &&
                float.TryParse(parts[1].Trim(), out var y) &&
                float.TryParse(parts[2].Trim(), out var z))
            {
                result = new float3(x, y, z);
                return true;
            }
            
            result = float3.zero;
            return false;
        }
        
        /// <summary>
        /// Parse radii string "centerRadius|zoneRadius"
        /// </summary>
        private static bool ParseRadii(string radiiStr, out int centerRadius, out int zoneRadius)
        {
            var parts = radiiStr.Split('|');
            if (parts.Length >= 2 &&
                int.TryParse(parts[0].Trim(), out centerRadius) &&
                int.TryParse(parts[1].Trim(), out zoneRadius))
            {
                return true;
            }
            
            centerRadius = zoneRadius = 0;
            return false;
        }
        
        /// <summary>
        /// Clear all arena positions
        /// </summary>
        public static void ClearPositions()
        {
            _arenaPositions.Clear();
        }
        
        /// <summary>
        /// Get arena position count
        /// </summary>
        public static int GetPositionCount()
        {
            return _arenaPositions.Count;
        }
        
        /// <summary>
        /// Check if a position is within any arena zone
        /// </summary>
        public static bool IsWithinArena(float3 position)
        {
            foreach (var arenaPos in _arenaPositions)
            {
                var distance = math.distance(position, arenaPos.Position);
                if (distance <= arenaPos.ZoneRadius)
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Get nearest arena position
        /// </summary>
        public static ArenaPosition? GetNearestArena(float3 position)
        {
            ArenaPosition? nearest = null;
            var minDistance = float.MaxValue;
            
            foreach (var arenaPos in _arenaPositions)
            {
                var distance = math.distance(position, arenaPos.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = arenaPos;
                }
            }
            
            return nearest;
        }
    }
}












