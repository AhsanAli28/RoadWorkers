using Spirit604.DotsCity.Simulation.Traffic.Obstacle;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficObstacleComponent : IComponentData
    {
        public ApproachType ApproachType;
        public float ApproachSpeed;
        public Entity ObstacleEntity;
        public ObstacleType ObstacleType;
        public IgnoreType IgnoreType;

        public bool HasObstacle => ObstacleEntity != Entity.Null && !Ignore;

        public bool Ignore => IgnoreType != IgnoreType.None;

        public static TrafficObstacleComponent GetDefault()
        {
            return new TrafficObstacleComponent()
            {
                ObstacleEntity = Entity.Null,
                ObstacleType = ObstacleType.Undefined,
                ApproachSpeed = -1
            };
        }
    }

    public enum ApproachType : byte { None, Default, Soft, Light, NoNextNode, BrakingLaneSoft, BrakingLane }

    public enum IgnoreType : byte { None, Collision, Avoidance }

    public struct ApproachData
    {
        public float Speed;
        public ApproachType ApproachType;
        public float Distance;

        public static ApproachData GetDefault()
        {
            return new ApproachData()
            {
                Speed = -1,
                ApproachType = ApproachType.None,
                Distance = float.MaxValue
            };
        }
    }

    public struct TrafficApproachDataComponent : IComponentData
    {
        public ApproachType ApproachType;
        public float ApproachSpeed;
    }

    public struct TrafficNpcObstacleComponent : IComponentData
    {
        public bool HasObstacle;
    }

    public struct TrafficRaycastObstacleComponent : IComponentData
    {
        public bool HasObstacle;
        public Entity ObstacleEntity;
        public float NextCastTime;
    }

    public struct TrafficRaycastTag : IComponentData, IEnableableComponent
    {
    }
}