using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct AnimatorCustomUnloadSkinListenerSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithNone<HasSkinTag>()
                .WithAll<HasCustomAnimationTag>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var noSkinListenerJob = new NoSkinListenerJob()
            {
            };

            noSkinListenerJob.Run();
        }

        [WithNone(typeof(HasSkinTag))]
        [WithDisabled(typeof(WaitForCustomAnimationTag))]
        [BurstCompile]
        private partial struct NoSkinListenerJob : IJobEntity
        {
            void Execute(
                EnabledRefRW<HasCustomAnimationTag> hasCustomAnimationTagRW,
                EnabledRefRW<WaitForCustomAnimationTag> waitForCustomAnimationTagRW)
            {
                hasCustomAnimationTagRW.ValueRW = false;
                waitForCustomAnimationTagRW.ValueRW = true;
            }
        }
    }
}