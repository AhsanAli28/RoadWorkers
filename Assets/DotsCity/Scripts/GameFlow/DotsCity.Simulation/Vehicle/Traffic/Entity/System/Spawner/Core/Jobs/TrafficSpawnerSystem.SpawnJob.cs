using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Config;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using Spirit604.Gameplay.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.GraphicsIntegration;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public partial class TrafficSpawnerSystem : EndInitSystemBase
    {
        [BurstCompile]
        private partial struct SpawnJob : IJob
        {
            private const int MaxAttemptSearchCarModelCount = 20;
            private const int MaxInitialSpawnAttemptCount = 100;
            private const int MaxSpawnAttemptCountByCar = 20;
            private const int MaxSpawnAttemptCountByCycle = 3;

            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<Entity> TrafficNodeAvailableEntities;

            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<Entity> TrafficNodeEntities;

            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeKeyValueArrays<int, PrefabEntityData> EntitiesData;

            [ReadOnly]
            public ComponentLookup<TrafficNodeComponent> TrafficNodeLookup;

            [ReadOnly]
            public ComponentLookup<TrafficNodeSettingsComponent> TrafficNodeSettingsLookup;

            [ReadOnly]
            public ComponentLookup<TrafficNodeCapacityComponent> TrafficNodeCapacityLookup;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly]
            public ComponentLookup<LocalTransform> TransformLookup;

            [ReadOnly]
            public BufferLookup<PathConnectionElement> PathConnectionLookup;

            [ReadOnly]
            public CommonGeneralSettingsReference CommonGeneralSettingsData;

            [ReadOnly]
            public PathGraphSystem.Singleton Graph;

            [ReadOnly]
            public CarHashMapSystem.Singleton CarHashMapSingleton;

            [ReadOnly]
            public TrafficNodeResolverSystem.RuntimePathDataRef RuntimePathDataRef;

            [ReadOnly]
            public TrafficSpawnerConfigBlobReference TrafficSpawnerConfigBlobReference;

            [ReadOnly]
            public TrafficCommonSettingsConfigBlobReference TrafficCommonSettingsConfigBlobReference;

            [ReadOnly]
            public SpawnSettings SpawnSettings;

            [ReadOnly]
            public EntityType EntityType;

            [ReadOnly]
            public DetectObstacleMode trafficDetectObstacleMode;

            [ReadOnly]
            public CullSystemConfigReference CullSystemConfigReference;

            [ReadOnly]
            public CitySpawnConfigReference CitySpawnConfigReference;

            [ReadOnly]
            public int SpawnCount;

            [ReadOnly]
            public bool IsInitialSpawn;

            [ReadOnly]
            public float3 PlayerPosition;

            [ReadOnly]
            public uint RandomSeed;

            [ReadOnly]
            public bool UserCreated;

            public int currentParkingCarsCount;

            public NativeParallelMultiHashMap<int, CarSpawnInfo> TempSpawnCarHashMap;

            public TrafficSpawnParams trafficSpawnParams;

            public EntityCommandBuffer commandBuffer;

            private Random randomGen;

            public void Execute()
            {
                if (EntitiesData.Keys.Length == 0)
                    return;

                randomGen = new Random(RandomSeed);

                if (IsInitialSpawn)
                {
                    int startSpawnedCount = 0;
                    int spawnAttemptCount = 0;

                    while (startSpawnedCount < SpawnCount)
                    {
                        bool spawned = Spawn(true, out var canRetry);

                        if (spawned)
                        {
                            startSpawnedCount++;
                        }
                        else
                        {
                            spawnAttemptCount++;

                            if (spawnAttemptCount >= MaxInitialSpawnAttemptCount || trafficSpawnParams.CustomSpawnSystem || !canRetry)
                                break;
                        }
                    }

#if UNITY_EDITOR
                    if (trafficSpawnParams.trafficCustomInit == TrafficCustomInitType.Default)
                        UnityEngine.Debug.Log($"Initial spawned {startSpawnedCount} cars.");
#endif

                }
                else
                {
                    int spawnCycleAttempt = 0;

                    for (int i = 0; i < SpawnCount; i++)
                    {
                        bool spawned = false;

                        for (int j = 0; j < MaxSpawnAttemptCountByCar; j++)
                        {
                            spawned = Spawn(false, out var canRetry);

                            if (spawned || !canRetry)
                                break;
                        }

                        if (!spawned)
                        {
                            spawnCycleAttempt++;
                        }
                        else
                        {
                            spawnCycleAttempt = 0;
                        }

                        if (spawnCycleAttempt >= MaxSpawnAttemptCountByCycle)
                            break;
                    }
                }
            }

            private bool Spawn(bool isStartSpawn)
            {
                return Spawn(isStartSpawn, out var canRetry);
            }

            private bool Spawn(bool isStartSpawn, out bool canRetry)
            {
                canRetry = true;

                if (SetCarModel(ref trafficSpawnParams, ref canRetry) == -1)
                    return false;

                Entity spawnEntity;

                if (!trafficSpawnParams.customSpawnData)
                {
                    spawnEntity = InitializeSpawnData(isStartSpawn, ref trafficSpawnParams);
                }
                else
                {
                    spawnEntity = trafficSpawnParams.spawnNodeEntity;
                }

                if (spawnEntity != Entity.Null)
                {
                    InitializeTarget(spawnEntity);

                    Spawn(ref trafficSpawnParams);

                    trafficSpawnParams.Reset();

                    return true;
                }

                return false;
            }

            private void InitializeTarget(Entity spawnNode)
            {
                float3 targetPosition;

                if (trafficSpawnParams.trafficPathComponent.CurrentGlobalPathIndex < 0)
                {
                    var trafficPathComponent = new TrafficPathComponent()
                    {
                        CurrentGlobalPathIndex = trafficSpawnParams.globalPathIndex,
                        DestinationWayPoint = trafficSpawnParams.spawnPosition,
                        LocalPathNodeIndex = 1
                    };

                    trafficSpawnParams.trafficPathComponent = trafficPathComponent;
                    targetPosition = trafficSpawnParams.spawnPosition;
                }
                else
                {
                    targetPosition = Graph.GetEndPosition(trafficSpawnParams.trafficPathComponent.CurrentGlobalPathIndex);
                }

                if (trafficSpawnParams.targetNodeEntity == trafficSpawnParams.previousNodeEntity)
                {
                    targetPosition = TransformLookup[trafficSpawnParams.targetNodeEntity].Position;
                }

                if (!trafficSpawnParams.customSpawnData)
                {
                    TrafficDestinationComponent destinationComponent = new TrafficDestinationComponent
                    {
                        Destination = targetPosition,

                        DestinationNode = trafficSpawnParams.targetNodeEntity,
                        PreviousNode = trafficSpawnParams.previousNodeEntity,
                        CurrentNode = trafficSpawnParams.previousNodeEntity,
                        NextDestinationNode = Entity.Null,

                        NextGlobalPathIndex = -1,
                        PathConnectionType = trafficSpawnParams.pathConnectionType
                    };

                    trafficSpawnParams.destinationComponent = destinationComponent;
                    trafficSpawnParams.spawnNodeEntity = spawnNode;
                    trafficSpawnParams.hasDriver = true;
                }
            }

            public Entity Spawn(ref TrafficSpawnParams trafficSpawnParams)
            {
                var prefabEntityData = GetPrefabEntityData(trafficSpawnParams.carModelIndex, out var found);

                if (found)
                {
                    var trafficEntity = commandBuffer.Instantiate(prefabEntityData.PrefabEntity);

                    var carModel = trafficSpawnParams.carModelIndex;
                    SetDefaultSettings(trafficEntity, carModel, in trafficSpawnParams, in prefabEntityData);
                    SetAdditionalSettings(trafficEntity, in trafficSpawnParams);

                    return trafficEntity;
                }

                return Entity.Null;
            }

            private PrefabEntityData GetPrefabEntityData(int key)
            {
                return GetPrefabEntityData(key, out var found);
            }

            private PrefabEntityData GetPrefabEntityData(int key, out bool found)
            {
                found = false;

                int index = EntitiesData.Keys.IndexOf(key);

                if (index >= 0)
                {
                    found = true;
                    return EntitiesData.Values[index];
                }

                return default;
            }

            private int SetCarModel(ref TrafficSpawnParams trafficSpawnParams, ref bool canRetry)
            {
                int carModel = -1;

                if (trafficSpawnParams.carModelIndex == -1)
                {
                    var hasModel = false;
                    int attemptCount = 0;

                    while (true)
                    {
                        float currentSumWeight = 0;

                        float randomWeight = randomGen.NextFloat(0, SpawnSettings.SumSpawnWeight);

                        for (int i = 0; i < EntitiesData.Values.Length; i++)
                        {
                            var prefabData = EntitiesData.Values[i];
                            var currentWeight = prefabData.Weight;
                            float currentMinStepWeight = currentSumWeight;
                            float currentMaxStepWeight = currentSumWeight + currentWeight;

                            if (currentMinStepWeight < randomWeight && randomWeight <= currentMaxStepWeight)
                            {
                                if (EntitiesData.Values[i].AvailableForSpawnByDefault)
                                {
                                    carModel = EntitiesData.Keys[i];
                                    hasModel = true;
                                }

                                break;
                            }

                            currentSumWeight += currentWeight;
                        }

                        if (hasModel)
                        {
                            break;
                        }
                        else
                        {
                            attemptCount++;

                            if (attemptCount >= MaxAttemptSearchCarModelCount)
                            {
                                break;
                            }
                        }
                    }

                    if (hasModel)
                    {
                        trafficSpawnParams.carModelIndex = (int)carModel;
                    }
                }
                else
                {
                    carModel = trafficSpawnParams.carModelIndex;
                }

                var found = false;
                GetPrefabEntityData(carModel, out found);

                if (!found)
                {
                    switch (trafficSpawnParams.trafficCustomInit)
                    {
                        case TrafficCustomInitType.TrafficPublic:
                            canRetry = false;

#if UNITY_EDITOR
                            UnityEngine.Debug.LogError($"Traffic car spawner doesn't have {carModel} car for TrafficPublic route");
#endif

                            return -1;
                        default:
                            trafficSpawnParams.Reset();

#if UNITY_EDITOR
                            UnityEngine.Debug.LogError($"Traffic car spawner doesn't have {carModel} car");
#endif

                            break;
                    }
                }

                return carModel;
            }

            private void SetDefaultSettings(Entity trafficEntity, int carModel, in TrafficSpawnParams trafficSpawnParams, in PrefabEntityData prefabEntityData)
            {
                var spawnPosition = trafficSpawnParams.spawnPosition;

                if (prefabEntityData.OffsetY != 0)
                {
                    spawnPosition.y += prefabEntityData.OffsetY;
                }

                commandBuffer.SetComponent(trafficEntity, LocalTransform.FromPositionRotation(spawnPosition, trafficSpawnParams.spawnRotation));

                if (prefabEntityData.Interpolation)
                {
                    commandBuffer.SetComponent(trafficEntity, new PhysicsGraphicalInterpolationBuffer()
                    {
                        PreviousTransform = new RigidTransform(trafficSpawnParams.spawnRotation, spawnPosition)
                    });
                }

                commandBuffer.SetComponent(trafficEntity, new TrafficMovementComponent
                {
                    CurrentCalculatedRotation = trafficSpawnParams.spawnRotation,
                    CurrentMovementDirection = 1
                });

                var destinationComponent = trafficSpawnParams.destinationComponent;

                commandBuffer.SetComponent(trafficEntity, destinationComponent);
                commandBuffer.SetComponent(trafficEntity, trafficSpawnParams.trafficPathComponent);

                int direction = 0;

                var globalPathIndex = trafficSpawnParams.trafficPathComponent.CurrentGlobalPathIndex;

                if (destinationComponent.PathConnectionType == PathConnectionType.PathPoint)
                {
                    commandBuffer.SetComponentEnabled<TrafficNextTrafficNodeRequestTag>(trafficEntity, true);
                }

                commandBuffer.SetComponent(trafficEntity, new TrafficTargetDirectionComponent() { Direction = direction });

                var data = GetPrefabEntityData((int)carModel);
                var boundsComponent = data.Bounds;

                AddSpawnedCar(globalPathIndex, trafficSpawnParams.spawnPosition, boundsComponent.size);

                if (trafficSpawnParams.hasDriver)
                {
                    commandBuffer.AddComponent<HasDriverTag>(trafficEntity);
                    commandBuffer.AddComponent<CarEngineStartedTag>(trafficEntity);
                }

                if (trafficSpawnParams.hasStoppingEngine)
                {
                    commandBuffer.AddComponent<CarEngineStartedTag>(trafficEntity);
                    InteractCarUtils.StopEngine(ref commandBuffer, trafficEntity, true);
                }

                if (CommonGeneralSettingsData.Config.Value.HealthSupport)
                {
                    if (trafficSpawnParams.customInitialHealth != 0)
                    {
                        commandBuffer.SetComponent(trafficEntity, new HealthComponent(trafficSpawnParams.customInitialHealth));
                    }
                }

                if (EntityType != EntityType.PureEntityNoPhysics && !trafficSpawnParams.velocity.Equals(float3.zero))
                {
                    commandBuffer.SetComponent(trafficEntity, new PhysicsVelocity
                    {
                        Linear = trafficSpawnParams.velocity
                    });
                }

                commandBuffer.SetComponent(trafficEntity, new SpeedComponent
                {
                    LaneLimit = trafficSpawnParams.speedLimit,
                });

                if (trafficSpawnParams.spawnNodeEntity == trafficSpawnParams.targetNodeEntity)
                {
                    commandBuffer.SetComponentEnabled<TrafficInitTag>(trafficEntity, true);
                }
            }

            private void SetAdditionalSettings(Entity trafficEntity, in TrafficSpawnParams trafficSpawnParams)
            {
                switch (trafficSpawnParams.trafficCustomInit)
                {
                    case TrafficCustomInitType.TrafficPublic:
                        {
                            commandBuffer.AddComponent(trafficEntity, new TrafficPublicInitComponent()
                            {
                                RouteEntitySettings = trafficSpawnParams.customRelatedEntityIndex
                            });

                            break;
                        }
                    case TrafficCustomInitType.RoadSegmentDebug:
                        {
                            commandBuffer.AddComponent(trafficEntity, new TrafficRoadSegmentInitComponent()
                            {
                                HashID = trafficSpawnParams.customInitIndex
                            });

                            break;
                        }
                    case TrafficCustomInitType.PlayerControlled:
                        {
                            commandBuffer.AddComponent<TrafficPlayerSelected>(trafficEntity);
                            break;
                        }
                    case TrafficCustomInitType.UserCallback:
                        {
                            commandBuffer.AddComponent<TrafficSpawnCallbackTag>(trafficEntity);
                            break;
                        }
                }
            }
        }
    }
}