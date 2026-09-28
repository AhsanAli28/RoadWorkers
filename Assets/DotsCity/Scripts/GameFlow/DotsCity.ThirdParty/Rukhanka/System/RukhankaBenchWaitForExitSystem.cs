using Rukhanka;
using Rukhanka.Toolbox;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Burst;
using Unity.Entities;
using FixedStringName = Unity.Collections.FixedString512Bytes;

namespace Spirit604.DotsCity.ThirdParty.RukhankaEngine
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct RukhankaBenchWaitForExitSystem : ISystem
    {
        private uint emptyStateHash;

        void ISystem.OnCreate(ref SystemState state)
        {
            emptyStateHash = new FixedStringName(BenchWaitForExitLegacySkinSystem.SitEmptyStateNameIndex).CalculateHash32();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            foreach (var (animatorState, seatSlotLinkedComponent, benchWaitForExitTagRW) in
                SystemAPI.Query<AnimatorStateQueryAspect, RefRW<SeatSlotLinkedComponent>, EnabledRefRW<BenchWaitForExitTag>>()
                .WithAll<BenchWaitForExitTag>())
            {
                var runtimeState = animatorState.GetLayerCurrentStateInfo(BenchWaitForExitLegacySkinSystem.AnimatorLayerSitIndex);

                bool exited = runtimeState.hash == emptyStateHash;

                if (exited)
                {
                    seatSlotLinkedComponent.ValueRW.Exited = true;
                    benchWaitForExitTagRW.ValueRW = false;
                }
            }
        }
    }
}
