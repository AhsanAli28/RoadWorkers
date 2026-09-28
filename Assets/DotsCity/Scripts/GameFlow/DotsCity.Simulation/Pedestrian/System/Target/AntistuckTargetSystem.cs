using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    [UpdateInGroup(typeof(LateEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct AntistuckTargetSystem : ISystem
    {
        private EntityQuery pedestrianGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            pedestrianGroup = SystemAPI.QueryBuilder()
                  .WithAll<AntistuckActivateTag, AntistuckDestinationComponent>()
                  .Build();

            state.RequireForUpdate(pedestrianGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var antistuckTargetJob = new AntistuckTargetJob()
            {
                CollisionLookup = SystemAPI.GetComponentLookup<CollisionComponent>(true),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                NextStateComponentLookup = SystemAPI.GetComponentLookup<NextStateComponent>(false),
                AntistuckDestinationComponentLookup = SystemAPI.GetComponentLookup<AntistuckDestinationComponent>(false),
                AntistuckConfigReference = SystemAPI.GetSingleton<AntistuckConfigReference>(),
                DeltaTime = SystemAPI.Time.DeltaTime,
                Timestamp = (float)SystemAPI.Time.ElapsedTime
            };

            antistuckTargetJob.Schedule();
        }

        [WithDisabled(typeof(AntistuckDeactivateTag))]
        [WithAll(typeof(AntistuckActivateTag))]
        [BurstCompile]
        public partial struct AntistuckTargetJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            [ReadOnly]
            public ComponentLookup<CollisionComponent> CollisionLookup;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransformLookup;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<NextStateComponent> NextStateComponentLookup;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<AntistuckDestinationComponent> AntistuckDestinationComponentLookup;

            [ReadOnly]
            public AntistuckConfigReference AntistuckConfigReference;

            [ReadOnly]
            public float DeltaTime;

            [ReadOnly]
            public float Timestamp;

            void Execute(
                Entity entity,
                EnabledRefRW<AntistuckDeactivateTag> antistuckDeactivateTagRW,
                EnabledRefRW<AntistuckActivateTag> antistuckActivateTagRW,
                in StateComponent stateComponent)
            {
                var nextStateComponent = NextStateComponentLookup[entity];

                if (nextStateComponent.HasNextState || stateComponent.IsActionState(ActionState.Idle) || stateComponent.IsDefaltActionState() || !nextStateComponent.TryToSetNextState(ActionState.Idle, true))
                {
#if UNITY_EDITOR
                    if (stateComponent.IsDefaltActionState())
                    {
                        UnityEngine.Debug.Log($"AntistuckTargetSystem. Pedestrian {entity.Index} stucked with default action state");
                    }
#endif

                    antistuckDeactivateTagRW.ValueRW = true;
                    NextStateComponentLookup[entity] = nextStateComponent;
                    return;
                }

                var localTransform = LocalTransformLookup[entity];
                var collisionComponent = CollisionLookup[entity];

                ref var antistuckConfig = ref AntistuckConfigReference.Config;
                float3 dstDirection = math.normalizesafe(localTransform.Position.Flat() - collisionComponent.CollidablePosition.Flat());

                var dstRotation = quaternion.LookRotationSafe(dstDirection, math.up());

                float3 destination = localTransform.Position.Flat() + dstDirection * antistuckConfig.Value.TargetPointOffset;

                AntistuckDestinationComponentLookup[entity] = new AntistuckDestinationComponent()
                {
                    ActivateTimestamp = Timestamp,
                    Destination = destination,
                    DstDirection = dstDirection,
                    DstRotation = dstRotation,
                    PreviousActionState = stateComponent.ActionState,
                    PreviousFlags = stateComponent.AdditiveStateFlags,
                };

                antistuckActivateTagRW.ValueRW = false;
            }
        }
    }
}