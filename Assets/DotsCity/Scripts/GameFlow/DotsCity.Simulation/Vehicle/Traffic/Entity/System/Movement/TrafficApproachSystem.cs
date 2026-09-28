using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic.Obstacle;
using Spirit604.Gameplay.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateAfter(typeof(TrafficObstacleSystem))]
    [UpdateInGroup(typeof(TrafficSimulationGroup))]
    [BurstCompile]
    public partial struct TrafficApproachSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                  .WithNone<TrafficCustomApproachTag>()
                  .WithAll<HasDriverTag, TrafficApproachDataComponent>()
                  .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var approachJob = new ApproachJob()
            {
                NodeTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                TrafficApproachConfigReference = SystemAPI.GetSingleton<TrafficApproachConfigReference>(),
                Graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>(),
            };

            approachJob.ScheduleParallel();
        }

        [WithNone(typeof(TrafficCustomApproachTag))]
        [WithAll(typeof(HasDriverTag))]
        [BurstCompile]
        public partial struct ApproachJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<LocalTransform> NodeTransformLookup;

            [ReadOnly]
            public TrafficApproachConfigReference TrafficApproachConfigReference;

            [ReadOnly]
            public PathGraphSystem.Singleton Graph;

            void Execute(
                ref TrafficApproachDataComponent trafficApproachDataComponent,
                in TrafficObstacleComponent trafficObstacleComponent,
                in TrafficDestinationComponent destinationComponent,
                in TrafficLightDataComponent trafficLightDataComponent,
                in TrafficPathComponent trafficPathComponent,
                in SpeedComponent speedComponent,
                in LocalTransform transform)
            {
                trafficApproachDataComponent.ApproachSpeed = -1;

                ApproachType approachType = ApproachType.None;

                if (trafficObstacleComponent.ApproachSpeed >= 0)
                {
                    approachType = trafficObstacleComponent.ApproachType;

                    switch (trafficObstacleComponent.ApproachType)
                    {
                        case ApproachType.Default:
                            trafficApproachDataComponent.ApproachSpeed = math.max(TrafficApproachConfigReference.Config.Value.MinApproachSpeed, trafficObstacleComponent.ApproachSpeed);
                            break;
                        case ApproachType.Soft:
                            trafficApproachDataComponent.ApproachSpeed = math.max(TrafficApproachConfigReference.Config.Value.MinApproachSpeedSoft, trafficObstacleComponent.ApproachSpeed);
                            break;
                        default:
                            approachType = ApproachType.Default;
                            trafficApproachDataComponent.ApproachSpeed = math.max(TrafficApproachConfigReference.Config.Value.MinApproachSpeed, trafficObstacleComponent.ApproachSpeed);
                            break;
                    }
                }

                if (approachType == ApproachType.None)
                {
                    float distance = destinationComponent.DistanceToEndOfPath;

                    bool nextNodeIsNull = !NodeTransformLookup.HasComponent(destinationComponent.NextDestinationNode);

                    if (trafficLightDataComponent.NextNodeState && !nextNodeIsNull && distance < TrafficApproachConfigReference.Config.Value.StoppingDistanceToLight)
                    {
                        distance = math.distance(NodeTransformLookup[destinationComponent.NextDestinationNode].Position, transform.Position);
                    }

                    if (distance < TrafficApproachConfigReference.Config.Value.StoppingDistanceToLight)
                    {
                        if (nextNodeIsNull)
                        {
                            approachType = ApproachType.NoNextNode;
                            trafficApproachDataComponent.ApproachSpeed = TrafficApproachConfigReference.Config.Value.OnComingToRedLightSpeed;
                        }
                        else if (trafficLightDataComponent.LightStateOfTargetNode != LightState.Green && trafficLightDataComponent.LightStateOfTargetNode != LightState.Uninitialized)
                        {
                            approachType = ApproachType.Light;
                            trafficApproachDataComponent.ApproachSpeed = TrafficApproachConfigReference.Config.Value.OnComingToRedLightSpeed;
                        }
                    }
                }

                if (approachType == ApproachType.None && TrafficApproachConfigReference.Config.Value.AutoBrakeBeforeSpeedLimit && Graph.Exist(destinationComponent.NextGlobalPathIndex))
                {
                    if (destinationComponent.DistanceToEndOfPath < TrafficApproachConfigReference.Config.Value.BrakingDistance)
                    {
                        approachType = ApproachType.BrakingLane;
                    }
                    else if (destinationComponent.DistanceToEndOfPath < TrafficApproachConfigReference.Config.Value.SoftBrakingDistance)
                    {
                        approachType = ApproachType.BrakingLaneSoft;
                    }

                    if (approachType != ApproachType.None)
                    {
                        float targetApproachSpeed = 0;

                        ref readonly var path = ref Graph.GetPathData(destinationComponent.NextGlobalPathIndex);

                        if (path.PathLength < TrafficApproachConfigReference.Config.Value.SkipBrakingPathLength && path.NodeCount <= 2)
                        {
                            float maxApproach = float.MaxValue;

                            var nextConnectedPathsIndexes = Graph.GetConnectedPaths(destinationComponent.NextGlobalPathIndex);

                            for (int i = 0; i < nextConnectedPathsIndexes.Length; i++)
                            {
                                ref readonly var startNode = ref Graph.GetPathNodeData(nextConnectedPathsIndexes[i], 0);

                                if (startNode.SpeedLimit > 0 && startNode.SpeedLimit < maxApproach)
                                {
                                    maxApproach = startNode.SpeedLimit;
                                    targetApproachSpeed = maxApproach;
                                }
                            }
                        }
                        else
                        {
                            ref readonly var startNode = ref Graph.GetPathNodeData(in path, 0);
                            targetApproachSpeed = startNode.SpeedLimit;
                        }

                        if (targetApproachSpeed > 0 && speedComponent.LaneLimit > targetApproachSpeed)
                        {
                            if (approachType == ApproachType.BrakingLaneSoft)
                            {
                                targetApproachSpeed = math.lerp(targetApproachSpeed, speedComponent.LaneLimit, TrafficApproachConfigReference.Config.Value.SoftBrakingRate);
                            }

                            trafficApproachDataComponent.ApproachSpeed = targetApproachSpeed;
                        }
                        else
                        {
                            approachType = ApproachType.None;
                        }
                    }
                }

                trafficApproachDataComponent.ApproachType = approachType;
            }
        }
    }
}