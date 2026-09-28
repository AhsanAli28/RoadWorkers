using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Extensions;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(PreEarlyJobGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct ProcessDefaultNodeSystem : ISystem
    {
        #region Variables

        private EntityQuery pedestrianGroup;

        #endregion

        #region Unity lifecycle

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            pedestrianGroup = SystemAPI.QueryBuilder()
                .WithNone<CustomReachTargetTag>()
                .WithDisabled<IdleTag>()
                .WithPresentRW<HasTargetTag>()
                .WithAllRW<DestinationComponent, NextStateComponent>()
                .WithAllRW<ProcessEnterDefaultNodeTag>()
                .WithAll<LocalToWorld>()
                .Build();

            state.RequireForUpdate(pedestrianGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var achieveDefaultNodeJob = new AchieveDefaultNodeJob()
            {
                NodeConnectionBufferLookup = SystemAPI.GetBufferLookup<NodeConnectionDataElement>(true),
                NodeSettingsLookup = SystemAPI.GetComponentLookup<NodeSettingsComponent>(true),
                NodeCapacityLookup = SystemAPI.GetComponentLookup<NodeCapacityComponent>(true),
                NodeLightSettingsComponentLookup = SystemAPI.GetComponentLookup<NodeLightSettingsComponent>(true),
                LightHandlerLookup = SystemAPI.GetComponentLookup<LightHandlerComponent>(true),
                WorldTransformLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                DestinationConfigReference = SystemAPI.GetSingleton<DestinationConfigReference>(),
                Timestamp = (float)SystemAPI.Time.ElapsedTime
            };

            achieveDefaultNodeJob.Schedule(pedestrianGroup);
        }

        [WithDisabled(typeof(IdleTag))]
        [WithAll(typeof(ProcessEnterDefaultNodeTag))]
        [BurstCompile]
        public partial struct AchieveDefaultNodeJob : IJobEntity
        {
            [ReadOnly]
            public BufferLookup<NodeConnectionDataElement> NodeConnectionBufferLookup;

            [ReadOnly]
            public ComponentLookup<NodeSettingsComponent> NodeSettingsLookup;

            [ReadOnly]
            public ComponentLookup<NodeCapacityComponent> NodeCapacityLookup;

            [ReadOnly]
            public ComponentLookup<NodeLightSettingsComponent> NodeLightSettingsComponentLookup;

            [ReadOnly]
            public ComponentLookup<LightHandlerComponent> LightHandlerLookup;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly]
            public ComponentLookup<LocalToWorld> WorldTransformLookup;

            [ReadOnly]
            public DestinationConfigReference DestinationConfigReference;

            [ReadOnly]
            public float Timestamp;

            void Execute(
                Entity pedestrianEntity,
                ref DestinationComponent destinationComponent,
                ref NextStateComponent nextStateComponent,
                EnabledRefRW<HasTargetTag> hasTargetTagRW,
                EnabledRefRW<ProcessEnterDefaultNodeTag> processEnterDefaultNodeTagRW,
                in LocalToWorld worldTransform)
            {
                Process(
                    in NodeConnectionBufferLookup,
                    in NodeSettingsLookup,
                    in NodeCapacityLookup,
                    in NodeLightSettingsComponentLookup,
                    in LightHandlerLookup,
                    in WorldTransformLookup,
                    in DestinationConfigReference,
                    in Timestamp,
                    pedestrianEntity,
                    ref destinationComponent,
                    ref nextStateComponent,
                    in worldTransform);

                hasTargetTagRW.ValueRW = true;
                processEnterDefaultNodeTagRW.ValueRW = false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Process(
            in BufferLookup<NodeConnectionDataElement> nodeConnectionBufferLookup,
            in ComponentLookup<NodeSettingsComponent> nodeSettingsLookup,
            in ComponentLookup<NodeCapacityComponent> nodeCapacityLookup,
            in ComponentLookup<NodeLightSettingsComponent> nodeLightSettingsComponentLookup,
            in ComponentLookup<LightHandlerComponent> lightHandlerLookup,
            in ComponentLookup<LocalToWorld> transformLookup,
            in DestinationConfigReference destinationConfigReference,
            in float timestamp,
            Entity pedestrianEntity,
            ref DestinationComponent destinationComponent,
            ref NextStateComponent nextStateComponent,
            in LocalToWorld worldTransform)
        {
            if (!nodeSettingsLookup.HasComponent(destinationComponent.DestinationNode))
            {
                destinationComponent = destinationComponent.SwapBack();
                return;
            }

            var targetNodeSettings = nodeSettingsLookup[destinationComponent.DestinationNode];
            var seed = UnityMathematicsExtension.GetSeed(timestamp, pedestrianEntity.Index, worldTransform.Position);
            var rndGen = new Random(seed);
            var sumWeight = targetNodeSettings.SumWeight;

            var newDestinationEntity = GetNewDestination(
                in destinationComponent,
                seed,
                sumWeight,
                in nodeConnectionBufferLookup,
                in nodeSettingsLookup,
                in nodeCapacityLookup,
                in destinationConfigReference);

            if (!nodeSettingsLookup.HasComponent(newDestinationEntity))
            {
                destinationComponent = destinationComponent.SwapBack();
                return;
            }

            SetNewDestination(
                in nodeSettingsLookup,
                in nodeLightSettingsComponentLookup,
                in lightHandlerLookup,
                in transformLookup,
                ref destinationComponent,
                ref nextStateComponent,
                rndGen,
                newDestinationEntity);

            Entity GetNewDestination(
                in DestinationComponent pedestrianDestinationEntityLocal,
                uint sourceSeed,
                float sumWeight,
                in BufferLookup<NodeConnectionDataElement> commonConnectionBuffer,
                in ComponentLookup<NodeSettingsComponent> nodeSettingsLookup,
                in ComponentLookup<NodeCapacityComponent> nodeCapacityLookup,
                in DestinationConfigReference destinationConfigReference)
            {
                Entity achievedEntity = pedestrianDestinationEntityLocal.DestinationNode;
                Entity previousEntity = pedestrianDestinationEntityLocal.PreviuosDestinationNode;

                if (!commonConnectionBuffer.HasBuffer(achievedEntity))
                {
                    return pedestrianDestinationEntityLocal.PreviuosDestinationNode;
                }

                var connectionBuffer = commonConnectionBuffer[achievedEntity];

                if (connectionBuffer.Length > 0)
                {
                    Entity ignoreEntity = Entity.Null;

                    if (destinationConfigReference.Config.Value.IgnorePreviousDst)
                    {
                        ignoreEntity = previousEntity;
                    }

                    var newDestinationEntity = PedestrianNodeEntityUtils.GetRandomDestinationEntity(in connectionBuffer, in nodeSettingsLookup, in nodeCapacityLookup, sourceSeed, ignoreEntity, sumWeight);
                    return newDestinationEntity;
                }
                else
                {
                    return achievedEntity;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetNewDestination(
            in ComponentLookup<NodeSettingsComponent> nodeSettingsLookup,
            in ComponentLookup<NodeLightSettingsComponent> nodeLightSettingsComponentLookup,
            in ComponentLookup<LightHandlerComponent> lightHandlerLookup,
            in ComponentLookup<LocalToWorld> transformLookup,
            ref DestinationComponent destinationComponent,
            ref NextStateComponent nextStateComponent,
            Random rndGen,
            Entity newDestinationEntity)
        {
            destinationComponent.PreviuosDestinationNode = destinationComponent.DestinationNode;
            destinationComponent.DestinationNode = newDestinationEntity;

            var targetPedestrianNodeSettingsComponent = nodeSettingsLookup[newDestinationEntity];
            var nodetransform = transformLookup[newDestinationEntity];

            var destination = DestinationNodeUtils.GetDestination(rndGen, in targetPedestrianNodeSettingsComponent, in nodetransform);

            destinationComponent.PreviousDestination = destinationComponent.Value;
            destinationComponent.Value = destination;
            destinationComponent.SetCustomAchieveDistance(targetPedestrianNodeSettingsComponent.CustomAchieveDistance);

            PedestrianCheckTrafficLightUtils.Process(
                in lightHandlerLookup,
                in nodeLightSettingsComponentLookup,
                ref destinationComponent,
                ref nextStateComponent);
        }

        #endregion
    }
}