using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Spirit604.DotsCity
{
    [UpdateBefore(typeof(BeginInitializationEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial class InitGroup : ComponentSystemGroup
    {
    }

    [UpdateBefore(typeof(DestroyGroup))]
    [UpdateInGroup(typeof(InitGroup), OrderFirst = true)]
    public partial class BeforeDestroyGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(InitGroup), OrderFirst = true)]
    public partial class DestroyGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(PreEarlyJobGroup), OrderFirst = true)]
    public partial class CullSimulationGroup : ComponentSystemGroup
    {
    }

    /// <summary>
    /// Group for empty systems without update.
    /// </summary>
    [UpdateInGroup(typeof(InitGroup))]
    public partial class DummyGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(LateInitGroup))]
    public partial class MainThreadInitGroup : ComponentSystemGroup
    {
    }

    [UpdateBefore(typeof(EndInitializationEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public partial class LateInitGroup : ComponentSystemGroup
    {
    }

    [UpdateAfter(typeof(EndInitializationEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public partial class BeforePhysXFixedStepGroup : ComponentSystemGroup
    {
    }

    [UpdateBefore(typeof(BeginSimulationEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class BeginSimulationGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(BeginSimulationGroup))]
    public partial class StructuralSystemGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(StructuralSystemGroup))]
    public partial class MainThreadEventGroup : ComponentSystemGroup
    {
    }

    [UpdateAfter(typeof(MainThreadEventGroup))]
    [UpdateInGroup(typeof(StructuralSystemGroup), OrderLast = true)]
    public partial class MainThreadEventPlaybackGroup : ComponentSystemGroup
    {
    }

    [UpdateAfter(typeof(MainThreadEventPlaybackGroup))]
    [UpdateInGroup(typeof(StructuralSystemGroup), OrderLast = true)]
    public partial class CleanupGroup : ComponentSystemGroup
    {
    }

    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class EarlySimulationGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(EarlySimulationGroup), OrderFirst = true)]
    public partial class PreEarlyJobGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(PreEarlyJobGroup), OrderFirst = true)]
    public partial class EarlyEventGroup : ComponentSystemGroup
    {
    }

    [UpdateAfter(typeof(PreEarlyJobGroup))]
    [UpdateInGroup(typeof(EarlySimulationGroup))]
    public partial class EarlyJobGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class FixedStepGroup : ComponentSystemGroup
    {
    }

    [UpdateBefore(typeof(TransformSystemGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class BeforeTransformGroup : ComponentSystemGroup
    {
    }

    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class SimulationGroup : ComponentSystemGroup
    {
    }

    [UpdateAfter(typeof(PhysicsInitializeGroup)), UpdateBefore(typeof(PhysicsSimulationGroup))]
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    public partial class RaycastGroup : ComponentSystemGroup
    {
    }

    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class PhysicsSimGroup : ComponentSystemGroup
    {
    }

    [UpdateAfter(typeof(PhysicsSystemGroup))]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class PhysicsTriggerGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class LateSimulationGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(LateSimulationGroup), OrderLast = true)]
    public partial class LateEventGroup : ComponentSystemGroup
    {
    }

    [UpdateBefore(typeof(BeginPresentationEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
    public partial class BeginPresentationGroup : ComponentSystemGroup
    {
    }
}