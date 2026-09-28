using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct StopTalkStateSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithAll<TalkComponent>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var stopTalkJob = new StopTalkJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                Timestamp = (float)SystemAPI.Time.ElapsedTime,
            };

            stopTalkJob.Run();
        }

        [WithAll(typeof(TalkComponent))]
        [BurstCompile]
        private partial struct StopTalkJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public float Timestamp;

            void Execute(
                Entity entity,
                ref TalkComponent talkComponent,
                ref NextStateComponent nextStateComponent,
                EnabledRefRW<MovementStateChangedEventTag> movementStateChangedEventTagRW,
                in StateComponent stateComponent)
            {
                bool outOfTime = Timestamp > talkComponent.StopTalkingTime && talkComponent.StopTalkingTime != 0;

                bool stateChanged =
                    ((!stateComponent.IsDefaltActionState() && !stateComponent.HasActionStateFlag(ActionState.Talking)) ||
                    (stateComponent.IsActionState(ActionState.Talking) && !stateComponent.IsMovementState(MovementState.Idle)));

                bool shouldStop = outOfTime || stateChanged;

                if (shouldStop)
                {
                    PedestrianInitUtils.DisableTalkState(ref CommandBuffer, entity, in stateComponent, ref nextStateComponent);
                    movementStateChangedEventTagRW.ValueRW = true;
                }
            }
        }
    }
}