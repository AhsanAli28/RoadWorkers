using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    public static class BenchStateExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddBenchStateComponents(ref EntityCommandBuffer commandBuffer, Entity entity)
        {
            commandBuffer.AddComponent<BenchWaitForExitTag>(entity);
            commandBuffer.AddComponent<SeatAchievedTargetTag>(entity);
            commandBuffer.AddComponent<BenchCustomMovementTag>(entity);

            commandBuffer.SetComponentEnabled<BenchWaitForExitTag>(entity, false);
            commandBuffer.SetComponentEnabled<BenchCustomMovementTag>(entity, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveBenchStateComponents(ref EntityCommandBuffer commandBuffer, Entity entity)
        {
            commandBuffer.RemoveComponent<BenchWaitForExitTag>(entity);
            commandBuffer.RemoveComponent<SeatAchievedTargetTag>(entity);
            commandBuffer.RemoveComponent<BenchCustomMovementTag>(entity);
        }
    }
}