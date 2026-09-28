using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct BenchWaitForExitNoSkinSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithNone<HasSkinTag>()
                .WithAll<BenchWaitForExitTag>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            new NoSkinWaitForExitJob()
            {
            }.Run();
        }

        [WithNone(typeof(HasSkinTag))]
        [WithAll(typeof(BenchWaitForExitTag))]
        [BurstCompile]
        public partial struct NoSkinWaitForExitJob : IJobEntity
        {
            void Execute(
                ref SeatSlotLinkedComponent seatSlotLinkedComponent,
                EnabledRefRW<BenchWaitForExitTag> benchWaitForExitTagRW)
            {
                seatSlotLinkedComponent.Exited = true;
                benchWaitForExitTagRW.ValueRW = false;
            }
        }
    }
}
