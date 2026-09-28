using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Gameplay.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    [UpdateInGroup(typeof(PreEarlyJobGroup))]
    [BurstCompile]
    public partial struct IdleSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithAll<
                    IdleTag,
                    DestinationComponent,
                    StateComponent>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var idleJob = new IdleJob()
            {
                NodeLightSettingsLookup = SystemAPI.GetComponentLookup<NodeLightSettingsComponent>(true),
                LightHandlerLookup = SystemAPI.GetComponentLookup<LightHandlerComponent>(true),
                IdleTimeLookup = SystemAPI.GetComponentLookup<IdleTimeComponent>(true),
                CommonStateData = SystemAPI.GetSingleton<CommonStateData>(),
            };

            idleJob.ScheduleParallel();
        }

        [WithAll(typeof(IdleTag))]
        [BurstCompile]
        private partial struct IdleJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<NodeLightSettingsComponent> NodeLightSettingsLookup;

            [ReadOnly]
            public ComponentLookup<LightHandlerComponent> LightHandlerLookup;

            [ReadOnly]
            public ComponentLookup<IdleTimeComponent> IdleTimeLookup;

            [ReadOnly]
            public CommonStateData CommonStateData;

            void Execute(
                Entity entity,
                ref NextStateComponent nextStateComponent,
                EnabledRefRW<IdleTag> idleTagRW,
                in DestinationComponent destinationComponent,
                in StateComponent stateComponent)
            {
                bool hasIdle = false;

                if (stateComponent.HasActionState(in nextStateComponent, ActionState.WaitForGreenLight, true))
                {
                    if (LightHandlerLookup.HasComponent(destinationComponent.DestinationLightEntity))
                    {
                        if (LightHandlerLookup[destinationComponent.DestinationLightEntity].State != LightState.Green)
                        {
                            hasIdle = true;
                        }
                        else
                        {
                            nextStateComponent.TryToSetNextState(ActionState.CrossingTheRoad);
                        }
                    }
                    else
                    {
                        var state = PedestrianCheckTrafficLightUtils.GetCrossingState(in NodeLightSettingsLookup, in destinationComponent);
                        nextStateComponent.TryToSetNextState(state);
                    }
                }

                if (!hasIdle)
                {
                    hasIdle = IdleTimeLookup.HasComponent(entity);
                }

                if (!hasIdle)
                {
                    if ((stateComponent.HasActionStateFlag(CommonStateData.IdleStates) || (CommonStateData.IdleStates & nextStateComponent.NextActionState) != 0)
                          && !stateComponent.HasAnyAdditiveStateFlags())
                    {
                        hasIdle = true;
                    }
                }

                if (!hasIdle)
                {
                    idleTagRW.ValueRW = false;
                }
            }
        }
    }
}