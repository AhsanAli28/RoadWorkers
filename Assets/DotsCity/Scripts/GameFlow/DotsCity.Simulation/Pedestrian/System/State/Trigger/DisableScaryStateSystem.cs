using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct DisableScaryStateSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<CustomMovementTag>()
                .WithAll<ScaryRunningTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var disableScaryJob = new DisableScaryJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                HasImpactTriggerLookup = SystemAPI.GetComponentLookup<HasImpactTriggerTag>(true),
                ImpactTriggerDataLookup = SystemAPI.GetComponentLookup<ImpactTriggerData>(true),
                NodeLightSettingsLookup = SystemAPI.GetComponentLookup<NodeLightSettingsComponent>(true),
            };

            disableScaryJob.Run();
        }

        [WithNone(typeof(CustomMovementTag))]
        [WithAll(typeof(ScaryRunningTag))]
        [BurstCompile]
        public partial struct DisableScaryJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public ComponentLookup<HasImpactTriggerTag> HasImpactTriggerLookup;

            [ReadOnly]
            public ComponentLookup<ImpactTriggerData> ImpactTriggerDataLookup;

            [ReadOnly]
            public ComponentLookup<NodeLightSettingsComponent> NodeLightSettingsLookup;

            void Execute(
                Entity entity,
                ref NextStateComponent nextStateComponent,
                in DestinationComponent destinationComponent)
            {
                var shouldDisable = !HasImpactTriggerLookup.HasComponent(entity);

                if (shouldDisable)
                {
                    var newState = PedestrianCheckTrafficLightUtils.GetCrossingState(in NodeLightSettingsLookup, in destinationComponent);

                    nextStateComponent.TryToSetNextState(newState);
                    nextStateComponent.RemoveState = ActionState.ScaryRunning;

                    CommandBuffer.RemoveComponent<ScaryRunningTag>(entity);
                    CommandBuffer.RemoveComponent<ProcessScaryRunningTag>(entity);

                    if (HasImpactTriggerLookup.HasComponent(entity))
                    {
                        CommandBuffer.RemoveComponent<HasImpactTriggerTag>(entity);
                    }

                    if (ImpactTriggerDataLookup.HasComponent(entity))
                    {
                        CommandBuffer.RemoveComponent<ImpactTriggerData>(entity);
                    }
                }
            }
        }
    }
}