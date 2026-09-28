using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct CarCollisionComponent : IComponentData
    {
        public float CollisionTime;
        public float LastCollisionEventTime;
        public float LastIdleTime;
        public float LastCalculation;
        public Entity LastCollisionEntity;
        public TrafficCollisionDirectionType SourceCollisionDirectionType;
        public TrafficCollisionDirectionType TargetCollisionDirectionType;

        public bool HasCollision => LastCollisionEntity != Entity.Null;
        public float CollisionDuration => LastCollisionEventTime - CollisionTime;
    }
}