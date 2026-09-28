using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct AnimatorCustomWaitForSkinSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithAll<HasSkinTag, WaitForCustomAnimationTag>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var waitForSkinJob = new WaitForSkinJob()
            {
            };

            waitForSkinJob.Run();
        }

        [WithDisabled(typeof(HasCustomAnimationTag), typeof(UpdateCustomAnimationTag))]
        [WithAll(typeof(HasSkinTag), typeof(WaitForCustomAnimationTag))]
        [BurstCompile]
        private partial struct WaitForSkinJob : IJobEntity
        {
            void Execute(
                EnabledRefRW<WaitForCustomAnimationTag> waitForCustomAnimationTagRW,
                EnabledRefRW<HasCustomAnimationTag> hasCustomAnimationTagRW,
                EnabledRefRW<UpdateCustomAnimationTag> updateCustomAnimationTagRW)
            {
                waitForCustomAnimationTagRW.ValueRW = false;
                hasCustomAnimationTagRW.ValueRW = true;
                updateCustomAnimationTagRW.ValueRW = true;
            }
        }
    }
}