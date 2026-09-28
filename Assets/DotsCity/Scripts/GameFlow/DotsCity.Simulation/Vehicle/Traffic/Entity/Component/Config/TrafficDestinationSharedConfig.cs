using System;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficDestinationSharedConfig : ISharedComponentData, IEquatable<TrafficDestinationSharedConfig>
    {
        public float MinDistanceToTarget;
        public float MinDistanceToPathPointTarget;
        public float MinDistanceToNewLight;
        public float MaxDistanceFromPreviousLightSQ;
        public float MinDistanceToTargetRouteNode;
        public float MinDistanceToTargetRailRouteNode;
        public bool Unique;

        private int UniqueValue => Unique ? 1245 : -1;

        public bool Equals(TrafficDestinationSharedConfig other) => this.GetHashCode() == other.GetHashCode();

        public override int GetHashCode() =>
            UniqueValue +
            (int)MinDistanceToPathPointTarget << 0 +
            (int)MinDistanceToNewLight << 1 +
            (int)MaxDistanceFromPreviousLightSQ << 2 +
            (int)MinDistanceToTargetRouteNode << 3 +
            (int)MinDistanceToTargetRouteNode << 4 +
            (int)MinDistanceToTargetRailRouteNode << 5;
    }
}