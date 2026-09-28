using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    public struct ProcessEnterSeatNodeTag : IComponentData { }

    public struct BenchWaitForExitTag : IComponentData, IEnableableComponent { }

    public struct BenchCustomMovementTag : IComponentData, IEnableableComponent { }

    public struct SeatAchievedTargetTag : IComponentData, IEnableableComponent { }
}