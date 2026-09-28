using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic.Obstacle
{
    public struct CarChangeLaneEntityComponent
    {
        public Entity Entity;
        public float3 Position;
        public quaternion Rotation;
        public float3 Destination;
        public int SourcePathIndex;
        public Entity ObstacleEntity;
        public float DistanceToEnd;
    }
}