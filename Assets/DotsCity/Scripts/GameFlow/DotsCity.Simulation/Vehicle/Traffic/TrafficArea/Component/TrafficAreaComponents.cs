using System;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.TrafficArea
{
    [Flags]
    public enum TrafficAreaNodeType
    {
        Default = 1 << 0,
        Enter = 1 << 1,
        Queue = 1 << 2,
        Exit = 1 << 3,
    }

    public struct TrafficAreaTag : IComponentData { }

    public struct TrafficAreaComponent : IComponentData
    {
        public int MaxEntryQueueCount;
        public int MaxSkipOrderCount;
        public int ActiveCurrentCarCount;
        public int ExitCarCount;
        public bool Locked;
        public int SkippedOrderCount;

        public bool SkipOrderSupported => MaxSkipOrderCount > 0;
    }

    public struct TrafficAreaEnterCarQueueElement : IBufferElementData
    {
        public Entity TrafficEntity;
    }

    public struct TrafficAreaExitCarQueueElement : IBufferElementData
    {
        public Entity TrafficEntity;
    }

    public struct TrafficAreaQueueNodeElement : IBufferElementData
    {
        public Entity NodeEntity;
    }

    public struct TrafficAreaEnterNodeElement : IBufferElementData
    {
        public Entity NodeEntity;
    }

    public struct TrafficAreaHasExitParkingOrderTag : IComponentData { }

    public struct TrafficAreaProcessingEnterQueueTag : IComponentData, IEnableableComponent
    {
    }

    public struct TrafficAreaProcessingExitQueueTag : IComponentData, IEnableableComponent
    {
    }

    public struct TrafficAreaUpdateLockStateTag : IComponentData, IEnableableComponent
    {
    }

    public struct TrafficAreaCarObserverEnabledTag : IComponentData, IEnableableComponent
    {
    }
}