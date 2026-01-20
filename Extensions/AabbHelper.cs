using System.Collections.Generic;
using Unity.Mathematics;

namespace VAuto.Extensions
{
    /// <summary>
    /// AABB (Axis-Aligned Bounding Box) Helper Extension Methods
    /// Provides sophisticated spatial relationship testing and collision detection
    /// Based on KindredSchematics implementation patterns
    /// VRising-compatible AABB implementation
    /// </summary>
    public struct Aabb
    {
        public float3 Min;
        public float3 Max;

        /// <summary>
        /// Check if this AABB contains another AABB
        /// </summary>
        public bool Contains(Aabb other)
        {
            return Min.x <= other.Min.x &&
                   Min.y <= other.Min.y &&
                   Min.z <= other.Min.z &&
                   Max.x >= other.Max.x &&
                   Max.y >= other.Max.y &&
                   Max.z >= other.Max.z;
        }

        /// <summary>
        /// Check if this AABB overlaps another AABB
        /// </summary>
        public bool Overlaps(Aabb other)
        {
            return Min.x <= other.Max.x && Max.x >= other.Min.x &&
                   Min.y <= other.Max.y && Max.y >= other.Min.y &&
                   Min.z <= other.Max.z && Max.z >= other.Min.z;
        }

        /// <summary>
        /// Include another AABB, expanding this one to contain it
        /// </summary>
        public void Include(Aabb other)
        {
            Min = math.min(Min, other.Min);
            Max = math.max(Max, other.Max);
        }

        /// <summary>
        /// Get the center point of the AABB
        /// </summary>
        public float3 GetCenter()
        {
            return (Min + Max) * 0.5f;
        }

        /// <summary>
        /// Get the size of the AABB
        /// </summary>
        public float3 GetSize()
        {
            return Max - Min;
        }

        /// <summary>
        /// Expand the AABB by a margin
        /// </summary>
        public Aabb Expand(float margin)
        {
            var expanded = this;
            expanded.Min -= new float3(margin);
            expanded.Max += new float3(margin);
            return expanded;
        }

        /// <summary>
        /// Calculate distance to another AABB (0 if overlapping)
        /// </summary>
        public float Distance(Aabb other)
        {
            float distance = 0f;

            // X-axis distance
            if (Max.x < other.Min.x)
                distance += other.Min.x - Max.x;
            else if (other.Max.x < Min.x)
                distance += Min.x - other.Max.x;

            // Y-axis distance
            if (Max.y < other.Min.y)
                distance += other.Min.y - Max.y;
            else if (other.Max.y < Min.y)
                distance += Min.y - other.Max.y;

            // Z-axis distance
            if (Max.z < other.Min.z)
                distance += other.Min.z - Max.z;
            else if (other.Max.z < Min.z)
                distance += Min.z - other.Max.z;

            return distance;
        }
    }

    /// <summary>
    /// AABB Helper Extension Methods
    /// Provides sophisticated spatial relationship testing and collision detection
    /// </summary>
    public static class AabbHelper
    {
        /// <summary>
        /// Check if two AABBs match on two axes and overlap on the third
        /// Used for precise spatial relationship testing
        /// </summary>
        /// <param name="thisOne">The current AABB</param>
        /// <param name="other">The other AABB to compare</param>
        /// <param name="tolerance">Tolerance for floating point comparison</param>
        /// <returns>True if AABBs match on two axes and overlap on the third</returns>
        public static bool MatchesOnTwoAxesAndOverlaps(this Aabb thisOne, Aabb other, float tolerance = 0.01f)
        {
            // Check X-axis alignment
            bool matchX = math.abs(thisOne.Min.x - other.Min.x) <= tolerance && 
                         math.abs(thisOne.Max.x - other.Max.x) <= tolerance;
            
            // Check Y-axis alignment
            bool matchY = math.abs(thisOne.Min.y - other.Min.y) <= tolerance && 
                         math.abs(thisOne.Max.y - other.Max.y) <= tolerance;
            
            // Check Z-axis alignment
            bool matchZ = math.abs(thisOne.Min.z - other.Min.z) <= tolerance && 
                         math.abs(thisOne.Max.z - other.Max.z) <= tolerance;

            // Check for overlap on the non-matching axis
            if (matchX && matchY)
            {
                // Match on X and Y, check Z overlap
                return (thisOne.Min.z <= other.Max.z && thisOne.Max.z >= other.Min.z);
            }
            
            if (matchX && matchZ)
            {
                // Match on X and Z, check Y overlap
                return (thisOne.Min.y <= other.Max.y && thisOne.Max.y >= other.Min.y);
            }
            
            if (matchY && matchZ)
            {
                // Match on Y and Z, check X overlap
                return (thisOne.Min.x <= other.Max.x && thisOne.Max.x >= other.Min.x);
            }

            return false;
        }

        /// <summary>
        /// Merge overlapping AABBs together to optimize spatial data
        /// Combines AABBs that overlap or contain each other
        /// </summary>
        /// <param name="aabbs">List of AABBs to merge</param>
        public static void MergeAabbsTogether(List<Aabb> aabbs)
        {
            for (int i = 0; i < aabbs.Count - 1; i++)
            {
                for (int j = aabbs.Count - 1; j > i; j--)
                {
                    // Check if AABBs match on two axes and overlap
                    if (aabbs[i].MatchesOnTwoAxesAndOverlaps(aabbs[j]))
                    {
                        // Merge the AABBs by including the second in the first
                        aabbs[i].Include(aabbs[j]);
                        aabbs.RemoveAt(j);
                    }
                    // Check if first AABB contains the second
                    else if (aabbs[i].Contains(aabbs[j]))
                    {
                        aabbs.RemoveAt(j);
                    }
                    // Check if second AABB contains the first
                    else if (aabbs[j].Contains(aabbs[i]))
                    {
                        aabbs[i] = aabbs[j];
                        aabbs.RemoveAt(j);
                    }
                }
            }
        }

        /// <summary>
        /// Create an AABB from center and size
        /// </summary>
        /// <param name="center">Center point</param>
        /// <param name="size">Size (width, height, depth)</param>
        /// <returns>AABB</returns>
        public static Aabb FromCenterAndSize(float3 center, float3 size)
        {
            var halfSize = size * 0.5f;
            return new Aabb
            {
                Min = center - halfSize,
                Max = center + halfSize
            };
        }

        /// <summary>
        /// Create an AABB from min and max points
        /// </summary>
        /// <param name="min">Minimum point</param>
        /// <param name="max">Maximum point</param>
        /// <returns>AABB</returns>
        public static Aabb FromMinMax(float3 min, float3 max)
        {
            return new Aabb { Min = min, Max = max };
        }

        /// <summary>
        /// Create an AABB from position and dimensions
        /// </summary>
        /// <param name="position">Position</param>
        /// <param name="dimensions">Dimensions (width, height, depth)</param>
        /// <returns>AABB</returns>
        public static Aabb FromPositionAndDimensions(float3 position, float3 dimensions)
        {
            return new Aabb
            {
                Min = position,
                Max = position + dimensions
            };
        }

        /// <summary>
        /// Create a spherical AABB (cube that contains a sphere)
        /// </summary>
        /// <param name="center">Center of sphere</param>
        /// <param name="radius">Radius of sphere</param>
        /// <returns>AABB containing the sphere</returns>
        public static Aabb FromSphere(float3 center, float radius)
        {
            var halfSize = new float3(radius);
            return new Aabb
            {
                Min = center - halfSize,
                Max = center + halfSize
            };
        }

        /// <summary>
        /// Check if a point is inside an AABB
        /// </summary>
        /// <param name="aabb">The AABB</param>
        /// <param name="point">The point to test</param>
        /// <returns>True if point is inside AABB</returns>
        public static bool Contains(this Aabb aabb, float3 point)
        {
            return aabb.Min.x <= point.x && aabb.Max.x >= point.x &&
                   aabb.Min.y <= point.y && aabb.Max.y >= point.y &&
                   aabb.Min.z <= point.z && aabb.Max.z >= point.z;
        }

        /// <summary>
        /// Get the volume of an AABB
        /// </summary>
        /// <param name="aabb">The AABB</param>
        /// <returns>Volume (width * height * depth)</returns>
        public static float GetVolume(this Aabb aabb)
        {
            var size = aabb.GetSize();
            return size.x * size.y * size.z;
        }

        /// <summary>
        /// Get the surface area of an AABB
        /// </summary>
        /// <param name="aabb">The AABB</param>
        /// <returns>Surface area</returns>
        public static float GetSurfaceArea(this Aabb aabb)
        {
            var size = aabb.GetSize();
            return 2 * (size.x * size.y + size.x * size.z + size.y * size.z);
        }

        /// <summary>
        /// Find the closest point on an AABB to a given point
        /// </summary>
        /// <param name="aabb">The AABB</param>
        /// <param name="point">The point</param>
        /// <returns>Closest point on AABB</returns>
        public static float3 GetClosestPoint(this Aabb aabb, float3 point)
        {
            return new float3(
                math.clamp(point.x, aabb.Min.x, aabb.Max.x),
                math.clamp(point.y, aabb.Min.y, aabb.Max.y),
                math.clamp(point.z, aabb.Min.z, aabb.Max.z)
            );
        }
    }
}












