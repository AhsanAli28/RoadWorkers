using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficDestinationConfig
    {
        public float MinDistanceToTarget;
        public float MinDistanceToPathPointTarget;
        public float MinDistanceToNewLight;
        public float MaxDistanceFromPreviousLightSQ;
        public float MinDistanceToTargetRouteNode;
        public float MinDistanceToTargetRailRouteNode;
        public bool HighSpeedRouteNodeCalc;
        public float HighSpeedRouteNodeMult;
        public OutOfPathResolveMethod OutOfPathMethod;
        public float MinDistanceToOutOfPath;
        public float MaxDistanceToOutOfPath;
        public NoDestinationReactType NoDestinationReact;
    }

    public struct TrafficDestinationConfigReference : IComponentData
    {
        public BlobAssetReference<TrafficDestinationConfig> Config;
    }

    public enum OutOfPathResolveMethod
    {
        Disabled,
        SwitchNode, // switching to the next waypoint
        Backward, // car will try to reach the missed waypoint by reversing
        Cull // car will be culled 
    }

    public enum NoDestinationReactType
    {
        Idle,
        DestroyVehicle
    }
}