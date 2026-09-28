using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Road
{
    public struct IntersectPathInfo
    {
        /// <summary> Global Path Index of the intersecting path. </summary>
        public int IntersectedPathIndex;

        public float3 IntersectPosition;

        /// <summary> Local Node Index of the path. </summary>
        public byte LocalNodeIndex;
    }
}
