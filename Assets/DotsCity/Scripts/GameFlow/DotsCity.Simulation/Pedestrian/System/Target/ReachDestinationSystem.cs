using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(SimulationGroup))]
    [BurstCompile]
    public partial struct ReachDestinationSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithNone<CustomReachTargetTag>()
                .WithDisabled<IdleTag, ReachTargetTag>()
                .WithAll<HasTargetTag, DestinationComponent>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var reachDestinationJob = new ReachDestinationJob()
            {
                NodeSettingsComponentLookup = SystemAPI.GetComponentLookup<NodeSettingsComponent>(true),
                DestinationConfigReference = SystemAPI.GetSingleton<DestinationConfigReference>(),
            };

            reachDestinationJob.ScheduleParallel();
        }

        [WithNone(typeof(CustomReachTargetTag))]
        [WithDisabled(typeof(IdleTag), typeof(ReachTargetTag), typeof(ProcessEnterDefaultNodeTag))]
        [WithAll(typeof(HasTargetTag))]
        [BurstCompile]
        private partial struct ReachDestinationJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<NodeSettingsComponent> NodeSettingsComponentLookup;

            [ReadOnly]
            public DestinationConfigReference DestinationConfigReference;

            void Execute(
                ref DestinationDistanceComponent destinationDistanceComponent,
                EnabledRefRW<ReachTargetTag> reachTargetTagRW,
                EnabledRefRW<ProcessEnterDefaultNodeTag> processEnterDefaultNodeTagRW,
                in DestinationComponent destinationComponent,
                in LocalToWorld worldTransform)
            {
                float distanceSQ = math.distancesq(worldTransform.Position, destinationComponent.Value);
                destinationDistanceComponent.DestinationDistanceSQ = distanceSQ;

                float achieveDistanceSQ = destinationComponent.CustomAchieveDistanceSQ == 0 ?
                      DestinationConfigReference.Config.Value.AchieveDistanceSQ : destinationComponent.CustomAchieveDistanceSQ;

                if (distanceSQ < achieveDistanceSQ)
                {
                    bool defaultNode = false;

                    if (NodeSettingsComponentLookup.HasComponent(destinationComponent.DestinationNode))
                    {
                        var node = NodeSettingsComponentLookup[destinationComponent.DestinationNode];
                        defaultNode = node.NodeType == Gameplay.Road.PedestrianNodeType.Default;
                    }

                    if (!defaultNode)
                    {
                        reachTargetTagRW.ValueRW = true;
                    }
                    else
                    {
                        processEnterDefaultNodeTagRW.ValueRW = true;
                    }
                }
            }
        }
    }
}