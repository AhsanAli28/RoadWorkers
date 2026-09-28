using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.TrafficPublic
{
    public struct TrafficPublicTag : IComponentData { }

    public struct TrafficPublicProccessExitTag : IComponentData, IEnableableComponent { }

    public struct TrafficPublicExitCompleteTag : IComponentData, IEnableableComponent { }

    public struct TrafficPublicIdleComponent : IComponentData, ICleanupComponentData
    {
        public StationState StationState;
        public float DeactivateTime;
        public Entity NodeEntity;
    }

    public enum StationState : byte
    {
        Default,
        WaitingForStop,
        InitIdleAfterStop,
        IdleAfterStop,
        StartExitting,
        Proccesing
    }

    public struct TrafficPublicExitSettingsComponent : IComponentData
    {
        public int MinPedestrianExitCount;
        public int MaxPedestrianExitCount;
        public float2 EnterExitDelayDuration;
        public float LastExitTimestamp;
        public int CurrentPedestrianExitCount;
    }

    public struct TrafficPublicIdleSettingsComponent : IComponentData
    {
        public float MinIdleTime;
        public float MaxIdleTime;
        public float IdleTimeAfterStop;
    }

    public struct TrafficPublicRouteSettings : IComponentData
    {
        public int MaxVehicleCount;
        public float PreferredIntervalDistanceSQ;
        public bool IgnoreCamera;
        public TrafficPublicType TrafficPublicType;
        public int VehicleModel;
    }

    public struct TrafficPublicRouteCapacityComponent : IComponentData
    {
        public int CurrentVehicleCount;
    }

    public struct TrafficPublicInitComponent : IComponentData
    {
        public Entity RouteEntitySettings;
    }

    public struct RouteTempEntitySettingsComponent : IComponentData
    {
        public Entity RouteEntity;
        public int RouteIndex;
        public int RouteLength;
        public TrafficPublicType TrafficPublicType;
    }
}