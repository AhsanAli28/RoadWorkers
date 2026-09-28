using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct CollisionComponent : IComponentData
    {
        public float3 CollidablePosition;
        public float3 Position;
        public float3 Force;
        public float LastCollisionTimestamp;
        public float FirstCollisionTime;
        public int LastDamageDealerID;

        public float CollideTime => LastCollisionTimestamp - FirstCollisionTime;

        public bool HasCollision()
        {
            return Position.x != 0 || Position.y != 0 || Position.z != 0;
        }
    }

    public struct HasCollisionTag : IComponentData, IEnableableComponent { }
}