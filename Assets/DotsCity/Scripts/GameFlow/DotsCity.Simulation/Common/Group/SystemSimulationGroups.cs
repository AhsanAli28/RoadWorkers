using Unity.Entities;

namespace Spirit604.DotsCity.Simulation
{
    [UpdateInGroup(typeof(LateInitGroup))]
    public partial class SpawnerGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(BeginSimulationGroup), OrderFirst = true)]
    public partial class MonoSyncGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(StructuralSystemGroup), OrderFirst = true)]
    public partial class StructuralInitGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(StructuralSystemGroup))]
    public partial class TrafficProcessNodeGroup : ComponentSystemGroup
    {
    }

    [UpdateBefore(typeof(CleanupGroup))]
    [UpdateAfter(typeof(TrafficProcessNodeGroup))]
    [UpdateInGroup(typeof(StructuralSystemGroup), OrderLast = true)]
    public partial class TrafficAreaSimulationGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(PreEarlyJobGroup))]
    public partial class HashMapGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(EarlyJobGroup))]
    public partial class NavSimulationGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(FixedStepGroup))]
    public partial class PedestrianFixedSimulationGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(FixedStepGroup))]
    public partial class TrafficFixedUpdateGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(SimulationGroup))]
    public partial class TrafficSimulationGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(SimulationGroup))]
    public partial class TrafficInputGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(SimulationGroup))]
    public partial class CarSimulationGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(LateSimulationGroup))]
    public partial class TrafficLateSimulationGroup : ComponentSystemGroup
    {
    }
}