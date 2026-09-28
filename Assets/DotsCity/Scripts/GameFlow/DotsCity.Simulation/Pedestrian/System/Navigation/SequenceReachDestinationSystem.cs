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
    [DisableAutoCreation]
    [DisableAutoTypeRegistration]
    public partial struct SequenceReachDestinationSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithDisabled<IdleTag, ReachTargetTag>()
                .WithAll<HasTargetTag, DestinationComponent, CustomReachTargetTag, NodeSequenceElement>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var reachDestinationJob = new ReachDestinationJob()
            {
                DestinationConfigReference = SystemAPI.GetSingleton<DestinationConfigReference>(),
            };

            reachDestinationJob.ScheduleParallel();
        }

        [WithDisabled(typeof(IdleTag), typeof(ReachTargetTag))]
        [WithAll(typeof(HasTargetTag), typeof(CustomReachTargetTag), typeof(NodeSequenceElement))]
        [BurstCompile]
        private partial struct ReachDestinationJob : IJobEntity
        {
            [ReadOnly]
            public DestinationConfigReference DestinationConfigReference;

            void Execute(
                ref DestinationDistanceComponent destinationDistanceComponent,
                EnabledRefRW<ReachTargetTag> reachTargetTagRW,
                in DestinationComponent destinationComponent,
                in LocalToWorld worldTransform)
            {
                float distanceSQ = math.distancesq(worldTransform.Position, destinationComponent.Value);
                destinationDistanceComponent.DestinationDistanceSQ = distanceSQ;

                float achieveDistanceSQ = destinationComponent.CustomAchieveDistanceSQ == 0 ?
                      DestinationConfigReference.Config.Value.AchieveDistanceSQ : destinationComponent.CustomAchieveDistanceSQ;

                if (distanceSQ < achieveDistanceSQ)
                {
                    reachTargetTagRW.ValueRW = true;
                }
            }
        }
    }
}