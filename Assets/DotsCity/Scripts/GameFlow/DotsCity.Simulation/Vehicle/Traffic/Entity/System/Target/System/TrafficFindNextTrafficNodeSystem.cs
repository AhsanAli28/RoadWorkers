using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateAfter(typeof(TrafficSwitchTargetNodeSystem))]
    [UpdateInGroup(typeof(TrafficProcessNodeGroup))]
    [BurstCompile]
    public partial struct TrafficFindNextTrafficNodeSystem : ISystem
    {
        private const int MaxSearchAttemptCount = 15;

        private struct TempPathData
        {
            public Entity ConnectedNode;
            public int GlobalPathIndex;
            public int LocalPathIndex;
            public float Weight;
            public PathConnectionType PathConnectionType;
        }

        private EntityQuery parkingTrafficGroup;
        private EntityQuery updateQuery;
        private NativeList<TempPathData> availablePaths;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            availablePaths = new NativeList<TempPathData>(30, Allocator.Persistent);

            parkingTrafficGroup = SystemAPI.QueryBuilder()
                .WithAll<TrafficTag, TrafficNodeLinkedComponent>()
                .Build();

            updateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<TrafficDestinationComponent, TrafficStateComponent>()
                .WithAllRW<TrafficNextTrafficNodeRequestTag>()
                .WithPresentRW<TrafficIdleTag>()
                .WithAll<TrafficDefaultTag, TrafficPathComponent, TrafficTypeComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            if (availablePaths.IsCreated)
            {
                availablePaths.Dispose();
            }
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var findNextNodeJob = new FindNextNodeJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                AvailablePaths = availablePaths,
                TrafficNodeCapacityLookup = SystemAPI.GetComponentLookup<TrafficNodeCapacityComponent>(true),
                NodeSettingsLookup = SystemAPI.GetComponentLookup<TrafficNodeSettingsComponent>(true),
                TrafficNodeAvailableLookup = SystemAPI.GetComponentLookup<TrafficNodeAvailableComponent>(true),
                PathConnectionLookup = SystemAPI.GetBufferLookup<PathConnectionElement>(true),
                Graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>(),
                TrafficSpawnerConfigBlobReference = SystemAPI.GetSingleton<TrafficSpawnerConfigBlobReference>(),
                TrafficGeneralSettingsReference = SystemAPI.GetSingleton<TrafficGeneralSettingsReference>(),
                TrafficObstacleConfigReference = SystemAPI.GetSingleton<TrafficObstacleConfigReference>(),
                TrafficDestinationConfigReference = SystemAPI.GetSingleton<TrafficDestinationConfigReference>(),
                CurrentParkingCount = parkingTrafficGroup.CalculateEntityCount(),
                CurrentTime = (float)SystemAPI.Time.ElapsedTime,
            };

            findNextNodeJob.Run(updateQuery);
        }

        [WithAll(typeof(TrafficDefaultTag))]
        [BurstCompile]
        private partial struct FindNextNodeJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [NativeDisableContainerSafetyRestriction]
            public NativeList<TempPathData> AvailablePaths;

            [ReadOnly]
            public ComponentLookup<TrafficNodeCapacityComponent> TrafficNodeCapacityLookup;

            [ReadOnly]
            public ComponentLookup<TrafficNodeSettingsComponent> NodeSettingsLookup;

            [ReadOnly]
            public ComponentLookup<TrafficNodeAvailableComponent> TrafficNodeAvailableLookup;

            [ReadOnly]
            public BufferLookup<PathConnectionElement> PathConnectionLookup;

            [ReadOnly]
            public PathGraphSystem.Singleton Graph;

            [ReadOnly]
            public TrafficSpawnerConfigBlobReference TrafficSpawnerConfigBlobReference;

            [ReadOnly]
            public TrafficGeneralSettingsReference TrafficGeneralSettingsReference;

            [ReadOnly]
            public TrafficObstacleConfigReference TrafficObstacleConfigReference;

            [ReadOnly]
            public TrafficDestinationConfigReference TrafficDestinationConfigReference;

            [ReadOnly]
            public int CurrentParkingCount;

            [ReadOnly]
            public float CurrentTime;

            private void Execute(
                Entity entity,
                ref TrafficDestinationComponent destinationComponent,
                ref TrafficStateComponent trafficStateComponent,
                EnabledRefRW<TrafficNextTrafficNodeRequestTag> trafficNextTrafficNodeRequestTagRW,
                EnabledRefRW<TrafficIdleTag> trafficIdleTagRW,
                in TrafficPathComponent trafficPathComponent,
                in TrafficTypeComponent trafficTypeComponent)
            {
                var nextConnectionType = PathConnectionType.TrafficNode;
                AvailablePaths.Clear();
                int changeLanePathKey = -1;
                bool nextShortPath = false;

                switch (destinationComponent.PathConnectionType)
                {
                    case PathConnectionType.TrafficNode:
                        {
                            var targetEntity = destinationComponent.DestinationNode;
                            float sumWeight = 0;

                            if (PathConnectionLookup.HasBuffer(targetEntity))
                            {
                                var trafficNodePathSettings = PathConnectionLookup[targetEntity];

                                for (int i = 0; i < trafficNodePathSettings.Length; i++)
                                {
                                    var localPathSettings = trafficNodePathSettings[i];
                                    var pathIndex = localPathSettings.GlobalPathIndex;

                                    ref readonly var pathData = ref Graph.GetPathData(pathIndex);

                                    var pathConnectedNode = localPathSettings.ConnectedNodeEntity;

                                    if (!TrafficNodeCapacityLookup.HasComponent(pathConnectedNode))
                                    {
                                        continue;
                                    }

                                    var trafficNodeCapacity = TrafficNodeCapacityLookup[pathConnectedNode];
                                    var trafficNodeSettings = NodeSettingsLookup[pathConnectedNode];

                                    bool isAvailable = true;

                                    switch (trafficNodeSettings.TrafficNodeType)
                                    {
                                        case TrafficNodeType.Parking:
                                            {
                                                var placeIsAvailable = TrafficNodeAvailableLookup[pathConnectedNode].IsAvailable;
                                                isAvailable = trafficNodeCapacity.HasSlots() && TrafficSpawnerConfigBlobReference.Reference.Value.MaxParkingCarsCount > CurrentParkingCount && placeIsAvailable;
                                                break;
                                            }
                                        default:
                                            {
                                                isAvailable = trafficNodeCapacity.HasSlots();
                                                break;
                                            }
                                    }

                                    if (isAvailable)
                                    {
                                        isAvailable = pathData.IsAvailable(in trafficTypeComponent);
                                    }

                                    if (isAvailable)
                                    {
                                        float weight = trafficNodeSettings.Weight;
                                        sumWeight += weight;

                                        AvailablePaths.Add(new TempPathData()
                                        {
                                            ConnectedNode = trafficNodePathSettings[i].ConnectedNodeEntity,
                                            GlobalPathIndex = trafficNodePathSettings[i].GlobalPathIndex,
                                            LocalPathIndex = i,
                                            Weight = weight,
                                            PathConnectionType = pathData.PathConnectionType
                                        });
                                    }
                                }
                            }

                            Entity connectedNodeEntity = Entity.Null;
                            int nextConnectedGlobalIndexPath = -1;

                            if (AvailablePaths.Length > 0)
                            {
                                int attemptCount = 0;

                                var baseSeed = UnityMathematicsExtension.GetSeed(CurrentTime, entity.Index);

                                while (true)
                                {
                                    var seed = MathUtilMethods.ModifySeed(baseSeed, attemptCount);
                                    var randomGen = new Unity.Mathematics.Random(seed);

                                    var randomWeight = randomGen.NextFloat(0, sumWeight);

                                    float currentSumWeight = 0;

                                    for (int i = 0; i < AvailablePaths.Length; i++)
                                    {
                                        float currentMinStepWeight = currentSumWeight;
                                        float currentMaxStepWeight = currentSumWeight + AvailablePaths[i].Weight;

                                        if (currentMinStepWeight < randomWeight && randomWeight <= currentMaxStepWeight)
                                        {
                                            nextConnectedGlobalIndexPath = AvailablePaths[i].GlobalPathIndex;
                                            connectedNodeEntity = AvailablePaths[i].ConnectedNode;
                                            nextConnectionType = AvailablePaths[i].PathConnectionType;

                                            ref readonly var pathData = ref Graph.GetPathData(AvailablePaths[i].GlobalPathIndex);

                                            if (pathData.PathLength < TrafficObstacleConfigReference.Config.Value.ShortPathLength)
                                            {
                                                nextShortPath = true;
                                            }

                                            break;
                                        }
                                        else
                                        {
                                            currentSumWeight += AvailablePaths[i].Weight;
                                        }
                                    }

                                    if (connectedNodeEntity != Entity.Null || attemptCount > MaxSearchAttemptCount)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        attemptCount++;
                                    }

                                }
                            }
                            else // No target
                            {
                                if (TrafficGeneralSettingsReference.Config.Value.ChangeLaneSupport)
                                {
                                    // Check buffer exist for unloading streaming case
                                    if (PathConnectionLookup.HasBuffer(targetEntity))
                                    {
                                        var trafficNodePathSettings = PathConnectionLookup[targetEntity];

                                        for (int i = 0; i < trafficNodePathSettings.Length; i++)
                                        {
                                            var localPathSettings = trafficNodePathSettings[i];
                                            var pathIndex = localPathSettings.GlobalPathIndex;

                                            ref readonly var pathData = ref Graph.GetPathData(pathIndex);

                                            if (pathData.ParallelCount > 0)
                                            {
                                                var parallelPathsIndexes = Graph.GetParallelPaths(pathIndex);

                                                for (int j = 0; j < parallelPathsIndexes.Length; j++)
                                                {
                                                    var parallelIndex = parallelPathsIndexes[j];
                                                    ref readonly var parallelPathData = ref Graph.GetPathData(parallelIndex);

                                                    var isAvailable =
                                                        localPathSettings.ConnectedNodeEntity != Entity.Null &&
                                                        parallelPathData.IsAvailable(in trafficTypeComponent);

                                                    if (isAvailable)
                                                    {
                                                        nextConnectedGlobalIndexPath = localPathSettings.GlobalPathIndex;
                                                        connectedNodeEntity = localPathSettings.ConnectedNodeEntity;
                                                        nextConnectionType = pathData.PathConnectionType;
                                                        changeLanePathKey = parallelIndex;
                                                        break;
                                                    }
                                                }
                                            }

                                            if (changeLanePathKey != -1)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (changeLanePathKey == -1)
                                {
                                    var targetNode = destinationComponent.DestinationNode;

                                    if (!NodeSettingsLookup.HasComponent(targetNode) || destinationComponent.CurrentNode == targetNode && NodeSettingsLookup[targetNode].TrafficNodeType != TrafficNodeType.DestroyVehicle)
                                    {
                                        // Destination unloaded due to road streaming or forbidden traffic group
                                        TrafficNoTargetUtils.AddNoTarget(ref CommandBuffer, entity, ref trafficStateComponent, ref trafficIdleTagRW, in TrafficDestinationConfigReference);
                                    }
                                }
                            }

                            destinationComponent.NextDestinationNode = connectedNodeEntity;
                            destinationComponent.NextGlobalPathIndex = nextConnectedGlobalIndexPath;
                            break;
                        }
                    case PathConnectionType.PathPoint:
                        {
                            ref readonly var currentPath = ref Graph.GetPathData(trafficPathComponent.CurrentGlobalPathIndex);

                            var connectedPathIndex = currentPath.ConnectedPathIndex;

                            if (connectedPathIndex != -1)
                            {
                                var newTargetEntity = destinationComponent.DestinationNode;

                                // For unloading streaming case
                                if (!PathConnectionLookup.HasBuffer(newTargetEntity))
                                {
                                    return;
                                }

                                var newTargetTrafficNodePathSettings = PathConnectionLookup[newTargetEntity];

                                Entity nextConnectedNodeEntity = Entity.Null;

                                for (int i = 0; i < newTargetTrafficNodePathSettings.Length; i++)
                                {
                                    if (newTargetTrafficNodePathSettings[i].GlobalPathIndex == connectedPathIndex)
                                    {
                                        nextConnectedNodeEntity = newTargetTrafficNodePathSettings[i].ConnectedNodeEntity;
                                        break;
                                    }
                                }

                                destinationComponent.NextDestinationNode = nextConnectedNodeEntity;
                                destinationComponent.NextGlobalPathIndex = connectedPathIndex;
                            }
                            else
                            {
#if UNITY_EDITOR
                                UnityEngine.Debug.LogError($"Source path {trafficPathComponent.CurrentGlobalPathIndex} index doesn't have connected path point path");
#endif
                            }

                            break;
                        }
                }

                destinationComponent.NextChangeLanePathIndex = changeLanePathKey;
                destinationComponent.NextPathConnectionType = nextConnectionType;
                destinationComponent.NextShortPath = nextShortPath;

                trafficNextTrafficNodeRequestTagRW.ValueRW = false;
            }
        }
    }
}