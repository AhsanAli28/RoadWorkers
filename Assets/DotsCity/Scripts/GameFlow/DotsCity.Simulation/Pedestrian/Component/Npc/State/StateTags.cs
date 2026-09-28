using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    public struct IdleTag : IComponentData, IEnableableComponent { }

    public struct MovementStateChangedEventTag : IComponentData, IEnableableComponent { }

    public struct ProcessEnterDefaultNodeTag : IComponentData, IEnableableComponent { }

    public struct ProcessEnterCarParkingNodeTag : IComponentData { }

    public struct ProcessEnterTrafficStationNodeTag : IComponentData { }

    public struct ProcessEnterTrafficEntryNodeTag : IComponentData { }
}