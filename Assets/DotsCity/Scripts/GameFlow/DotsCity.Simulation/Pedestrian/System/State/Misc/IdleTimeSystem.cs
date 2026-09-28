using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct IdleTimeSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithAll<IdleTimeComponent, StateComponent>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var pedestrianIdleJob = new IdleJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                PedestrianNodeIdleLookup = SystemAPI.GetComponentLookup<NodeIdleComponent>(true),
                CurrentTime = (float)SystemAPI.Time.ElapsedTime
            };

            pedestrianIdleJob.Run();
        }

        [BurstCompile]
        public partial struct IdleJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public ComponentLookup<NodeIdleComponent> PedestrianNodeIdleLookup;

            [ReadOnly]
            public float CurrentTime;

            void Execute(
                Entity entity,
                ref NextStateComponent nextStateComponent,
                ref DestinationComponent destinationComponent,
                ref IdleTimeComponent pedestrianIdleComponent,
                in StateComponent stateComponent)
            {
                if (!pedestrianIdleComponent.IsInitialized)
                {
                    pedestrianIdleComponent.IsInitialized = true;
                    var idleNode = pedestrianIdleComponent.IdleNode;

                    bool revertIdleState = false;

                    if (PedestrianNodeIdleLookup.HasComponent(idleNode))
                    {
                        var pedestrianNodeIdleComponent = PedestrianNodeIdleLookup[idleNode];
                        var rdnGen = UnityMathematicsExtension.GetRandomGen(CurrentTime, entity.Index);

                        float randomTime = CurrentTime + rdnGen.NextFloat(pedestrianNodeIdleComponent.MinIdleTime, pedestrianNodeIdleComponent.MaxIdleTime);

                        pedestrianIdleComponent.DisableIdleTimestamp = randomTime;

                        if (!nextStateComponent.TryToSetNextState(ActionState.Idle, ref destinationComponent))
                        {
                            revertIdleState = true;
                        }
                    }
                    else
                    {
                        if (idleNode != Entity.Null)
                        {
#if UNITY_EDITOR
                            UnityEngine.Debug.Log($"Entity {idleNode.Index} doesn't have NodeIdleComponent component");
#endif
                        }
                        else
                        {
#if UNITY_EDITOR
                            UnityEngine.Debug.Log($"Entity doesn't have IdleEntity. PreviousTarget {destinationComponent.PreviuosDestinationNode.Index} CurrentTarget {destinationComponent.DestinationNode.Index}");
#endif
                        }

                        revertIdleState = true;
                    }

                    if (revertIdleState)
                    {
                        CommandBuffer.SetComponentEnabled<HasTargetTag>(entity, true);
                        CommandBuffer.RemoveComponent<IdleTimeComponent>(entity);
                    }
                }
                else
                {
                    bool outOfTime = CurrentTime >= pedestrianIdleComponent.DisableIdleTimestamp;

                    bool stateChanged =
                        (!stateComponent.HasActionState(in nextStateComponent, ActionState.Idle)) ||
                        (stateComponent.IsActionState(ActionState.Idle) && !stateComponent.IsMovementState(MovementState.Idle));

                    if (outOfTime || stateChanged)
                    {
                        nextStateComponent.TryToSetNextState(ActionState.MovingToNextTargetPoint);

                        CommandBuffer.RemoveComponent<IdleTimeComponent>(entity);
                        CommandBuffer.SetComponentEnabled<ProcessEnterDefaultNodeTag>(entity, true);
                    }
                }
            }
        }
    }
}