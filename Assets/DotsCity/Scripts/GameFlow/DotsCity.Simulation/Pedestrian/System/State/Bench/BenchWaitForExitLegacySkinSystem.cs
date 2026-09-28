using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct BenchWaitForExitLegacySkinSystem : ISystem
    {
        public const int AnimatorLayerSitIndex = 1;
        public const string SitEmptyStateNameIndex = "Empty";

        private EntityQuery updateQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<BenchWaitForExitTag, Animator>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            foreach (var (animator, seatSlotLinkedComponent, benchWaitForExitTagRW) in
                SystemAPI.Query<SystemAPI.ManagedAPI.UnityEngineComponent<Animator>, RefRW<SeatSlotLinkedComponent>, EnabledRefRW<BenchWaitForExitTag>>()
                .WithAll<BenchWaitForExitTag>())
            {
                var stateInfo = animator.Value.GetCurrentAnimatorStateInfo(AnimatorLayerSitIndex);

                bool exited = stateInfo.IsName(SitEmptyStateNameIndex);

                if (exited)
                {
                    seatSlotLinkedComponent.ValueRW.Exited = true;
                    benchWaitForExitTagRW.ValueRW = false;
                }
            }
        }
    }
}
