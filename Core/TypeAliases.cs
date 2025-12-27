using Unity.Mathematics;

namespace VAuto
{
    // Standardize float3 usage
    using float3 = Unity.Mathematics.float3;
    
    // Common math constants and functions
    public static class MathUtils
    {
        public static readonly float3 Zero = float3.zero;
        public static float DistanceSquared(float3 a, float3 b) => math.distancesq(a, b);
        public static float Distance(float3 a, float3 b) => math.distance(a, b);
    }

    // Common component tags - using a marker interface pattern
    public interface IIsSecondaryComponent { }
    public struct IsSecondaryComponent : IIsSecondaryComponent { }
}















