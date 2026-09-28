using Spirit604.Gameplay.Road;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficDestinationComponent : IComponentData
    {
        public float3 Destination;
        public PathConnectionType PathConnectionType;
        public float DistanceToEndOfPath;
        public float DistanceToWaypoint;

        public Entity DestinationNode;
        public Entity NextDestinationNode;
        public Entity PreviousNode;
        public Entity CurrentNode;

        public int NextGlobalPathIndex;
        public int ChangeLanePathIndex;
        public int NextChangeLanePathIndex;
        public bool NextShortPath;
        public PathConnectionType NextPathConnectionType;
        public AchieveState AchieveState;
    }
}