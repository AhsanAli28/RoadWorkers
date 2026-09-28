using Spirit604.Gameplay.Road;
using System;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficStateComponent : IComponentData
    {
        public TrafficState TrafficState;
        public TrafficLightCarState TrafficLightCarState;
        public TrafficIdleState TrafficIdleState;

        public bool IsIdle => TrafficIdleState != TrafficIdleState.Default;
    }

    public struct TrafficLightDataComponent : IComponentData
    {
        public Entity CurrentLightEntity;
        public Entity LastCurrentNode;
        public LightState LightStateOfTargetNode;
        public bool NextNodeState;
    }

    public enum TrafficLightCarState
    {
        FarFromLight,
        InRange,
        InRangeAndInitialized,
    }

    public enum TrafficState
    {
        Default = 0,
        IsWaitingForGreenLight = 1
    }

    [Flags]
    public enum TrafficIdleState
    {
        Default = 0,
        IsWaitingForGreenLight = 1 << 0,
        WaitForChangeLane = 1 << 1,
        Parking = 1 << 2,
        WaitForEnterArea = 1 << 3,
        WaitForExitArea = 1 << 4,
        IdleNode = 1 << 5,
        NoTarget = 1 << 6,
        PublicTransportStop = 1 << 7,
        Collided = 1 << 8,
        UserCreated = 1 << 9,
    }
}