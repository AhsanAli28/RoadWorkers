using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct SwitchTalkingKeySystem : ISystem
    {
        private EntityQuery updateGroup;

        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithDisabled<UpdateCustomAnimationTag>()
                .WithAll<TalkComponent>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var switchTalkKeyJob = new SwitchTalkKeyJob()
            {
                TalkSpawnSettingsReference = SystemAPI.GetSingleton<TalkSpawnSettingsReference>(),
                Timestamp = (float)SystemAPI.Time.ElapsedTime,
            };

            switchTalkKeyJob.Run();
        }

        [WithDisabled(typeof(UpdateCustomAnimationTag))]
        [WithAll(typeof(TalkComponent))]
        [BurstCompile]
        private partial struct SwitchTalkKeyJob : IJobEntity
        {
            [ReadOnly]
            public TalkSpawnSettingsReference TalkSpawnSettingsReference;

            [ReadOnly]
            public float Timestamp;

            void Execute(
                Entity entity,
                ref TalkComponent talkComponent,
                ref AnimationStateComponent animationStateComponent,
                EnabledRefRW<UpdateCustomAnimationTag> switchTalkingEventComponentRW)
            {
                if (talkComponent.SwitchTalkingTime > Timestamp)
                    return;

                var rndGen = UnityMathematicsExtension.GetRandomGen(Timestamp, entity.Index);

                var randomTalkParameter = rndGen.NextInt(0, TalkSpawnSettingsReference.Config.Value.TalkingStates.Length);

                var switchDuration = rndGen.NextDouble(TalkSpawnSettingsReference.Config.Value.MinMaxSwitchDuration.x, TalkSpawnSettingsReference.Config.Value.MinMaxSwitchDuration.y);
                talkComponent.SwitchTalkingTime = Timestamp + switchDuration;

                animationStateComponent.NewAnimationState = TalkSpawnSettingsReference.Config.Value.TalkingStates[randomTalkParameter];

                switchTalkingEventComponentRW.ValueRW = true;
            }
        }
    }
}