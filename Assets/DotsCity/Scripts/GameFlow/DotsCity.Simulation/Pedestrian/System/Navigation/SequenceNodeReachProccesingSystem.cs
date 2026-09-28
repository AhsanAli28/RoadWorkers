using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateAfter(typeof(DefaultReachProccesingSystem))]
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct SequenceNodeReachProccesingSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithAllRW<DestinationComponent, NextStateComponent>()
                .WithAllRW<HasTargetTag, ReachTargetTag>()
                .WithAllRW<CustomReachTargetTag>()
                .WithAll<LocalToWorld>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var reachDestinationJob = new ReachDestinationJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                EnabledNavigationLookup = SystemAPI.GetComponentLookup<EnabledNavigationTag>(true),
                NodeConnectionBufferLookup = SystemAPI.GetBufferLookup<NodeConnectionDataElement>(true),
                NodeSettingsLookup = SystemAPI.GetComponentLookup<NodeSettingsComponent>(true),
                NodeCapacityLookup = SystemAPI.GetComponentLookup<NodeCapacityComponent>(true),
                NodeLightSettingsComponentLookup = SystemAPI.GetComponentLookup<NodeLightSettingsComponent>(true),
                LightHandlerLookup = SystemAPI.GetComponentLookup<LightHandlerComponent>(true),
                WorldTransformLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                DestinationConfigReference = SystemAPI.GetSingleton<DestinationConfigReference>(),
                PedestrianGeneralSettingsReference = SystemAPI.GetSingleton<PedestrianGeneralSettingsReference>(),
                Timestamp = (float)SystemAPI.Time.ElapsedTime,
            };

            reachDestinationJob.Run();
        }

        [WithAll(typeof(CustomReachTargetTag))]
        [BurstCompile]
        private partial struct ReachDestinationJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public ComponentLookup<EnabledNavigationTag> EnabledNavigationLookup;

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

            [ReadOnly]
            public ComponentLookup<LocalToWorld> WorldTransformLookup;

            [ReadOnly]
            public DestinationConfigReference DestinationConfigReference;

            [ReadOnly]
            public PedestrianGeneralSettingsReference PedestrianGeneralSettingsReference;

            [ReadOnly]
            public float Timestamp;

            void Execute(
                Entity entity,
                ref DestinationComponent destinationComponent,
                ref NextStateComponent nextStateComponent,
                DynamicBuffer<NodeSequenceElement> nodeSequence,
                EnabledRefRW<HasTargetTag> hasTargetTagRW,
                EnabledRefRW<ReachTargetTag> reachTargetTagRW,
                EnabledRefRW<CustomReachTargetTag> customReachTargetTagRW,
                in LocalToWorld worldTransform)
            {
                reachTargetTagRW.ValueRW = false;

                if (EnabledNavigationLookup.HasComponent(entity) && EnabledNavigationLookup.IsComponentEnabled(entity))
                {
                    CommandBuffer.SetComponentEnabled<EnabledNavigationTag>(entity, false);
                }

                if (nodeSequence.Length > 0)
                {
                    var newDestination = nodeSequence[0].Entity;
                    nodeSequence.RemoveAt(0);

                    var nodeType = PedestrianNodeType.Default;

                    var achievedTargetEntity = destinationComponent.DestinationNode;

                    if (NodeSettingsLookup.HasComponent(achievedTargetEntity))
                    {
                        var nodeSettingsComponent = NodeSettingsLookup[achievedTargetEntity];
                        nodeType = nodeSettingsComponent.NodeType;
                    }

                    if (nodeType == PedestrianNodeType.Default)
                    {
                        var rnd = UnityMathematicsExtension.GetRandomGen(Timestamp, entity.Index);

                        ProcessDefaultNodeSystem.SetNewDestination(
                            in NodeSettingsLookup,
                            in NodeLightSettingsComponentLookup,
                            in LightHandlerLookup,
                            in WorldTransformLookup,
                            ref destinationComponent,
                            ref nextStateComponent,
                            rnd,
                            newDestination);

                        return;
                    }
                }
                else
                {
                    customReachTargetTagRW.ValueRW = false;
                    return;
                }

                SelectAchievedTargetUtils.ProcessAchievedTarget(
                    ref CommandBuffer,
                    in NodeConnectionBufferLookup,
                    in NodeSettingsLookup,
                    in NodeCapacityLookup,
                    in NodeLightSettingsComponentLookup,
                    in LightHandlerLookup,
                    in WorldTransformLookup,
                    in DestinationConfigReference,
                    in PedestrianGeneralSettingsReference,
                    in Timestamp,
                    entity,
                    ref destinationComponent,
                    ref nextStateComponent,
                    ref hasTargetTagRW,
                    in worldTransform);
            }
        }
    }
}