using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateAfter(typeof(NpcRecalculateNavTargetSystem))]
    [UpdateInGroup(typeof(NavSimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct LocalAvoidanceObstacleSystem : ISystem
    {
        private const float SamePointDirection = -0.4f;

        private SystemHandle carHashMapSystem;
        private SystemHandle npcRecalculateNavTargetSystem;
        private EntityQuery updateQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            carHashMapSystem = state.WorldUnmanaged.GetExistingUnmanagedSystem<CarHashMapSystem>();
            npcRecalculateNavTargetSystem = state.WorldUnmanaged.GetExistingUnmanagedSystem<NpcRecalculateNavTargetSystem>();

            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<CustomMovementTag>()
                .WithDisabledRW<PathLocalAvoidanceEnabledTag>()
                .WithAllRW<PathPointAvoidanceElement, NavAgentSteeringComponent>()
                .WithAllRW<NavAgentComponent, DestinationComponent>()
                .WithPresentRW<EnabledNavigationTag>()
                .WithAll<LocalAvoidanceAgentTag, UpdateNavTargetTag, LocalToWorld, CircleColliderComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
            state.RequireForUpdate<CarHashMapSystem.Singleton>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            ref var carHashMapSystemRef = ref state.WorldUnmanaged.ResolveSystemStateRef(carHashMapSystem);
            ref var npcRecalculateNavTargetSystemRef = ref state.WorldUnmanaged.ResolveSystemStateRef(npcRecalculateNavTargetSystem);

            var depJob = JobHandle.CombineDependencies(carHashMapSystemRef.Dependency, npcRecalculateNavTargetSystemRef.Dependency);

            var avoidanceJob = new AvoidanceJob()
            {
                CarHashMapSingleton = SystemAPI.GetSingleton<CarHashMapSystem.Singleton>(),
                WorldTransformLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                PedestrianNodeSettingsLookup = SystemAPI.GetComponentLookup<NodeSettingsComponent>(true),
                ObstacleAvoidanceSettingsReference = SystemAPI.GetSingleton<ObstacleAvoidanceSettingsReference>(),
                Timestamp = (float)SystemAPI.Time.ElapsedTime,
            };

            state.Dependency = avoidanceJob.ScheduleParallel(updateQuery, depJob);
        }

        [WithNone(typeof(CustomMovementTag))]
        [WithDisabled(typeof(PathLocalAvoidanceEnabledTag))]
        [WithAll(typeof(LocalAvoidanceAgentTag), typeof(UpdateNavTargetTag))]
        [BurstCompile]
        public partial struct AvoidanceJob : IJobEntity
        {
            [ReadOnly]
            public CarHashMapSystem.Singleton CarHashMapSingleton;

            [ReadOnly]
            public ComponentLookup<LocalToWorld> WorldTransformLookup;

            [ReadOnly]
            public ComponentLookup<NodeSettingsComponent> PedestrianNodeSettingsLookup;

            [ReadOnly]
            public ObstacleAvoidanceSettingsReference ObstacleAvoidanceSettingsReference;

            [ReadOnly]
            public float Timestamp;

            void Execute(
                Entity entity,
                DynamicBuffer<PathPointAvoidanceElement> pathBuffer,
                ref NavAgentSteeringComponent navAgentSteeringComponent,
                ref NavAgentComponent navAgentComponent,
                ref DestinationComponent destinationComponent,
                EnabledRefRW<PathLocalAvoidanceEnabledTag> pathLocalAvoidanceEnabledTagRW,
                EnabledRefRW<EnabledNavigationTag> enabledNavigationTagRW,
                EnabledRefRW<UpdateNavTargetTag> updateNavTargetTagRW,
                in CircleColliderComponent pedestrianCollider,
                in LocalToWorld worldTransform)
            {
                if (navAgentComponent.ObstacleEntity == Entity.Null)
                {
                    updateNavTargetTagRW.ValueRW = false;
                    return;
                }

                float3 localPedestrianPosition = worldTransform.Position.Flat();

                bool hasTarget = false;
                bool swapTarget = false;

                NativeArray<float3> points = default;
                NativeList<int> keys = HashMapHelper.GetHashMapPosition9Cells(localPedestrianPosition);

                for (int i = 0; i < keys.Length; i++)
                {
                    int pedestrianKey = keys[i];

                    if (CarHashMapSingleton.CarHashMap.TryGetFirstValue(pedestrianKey, out var carHashEntity, out var nativeMultiHashMapIterator))
                    {
                        do
                        {
                            if (navAgentComponent.ObstacleEntity != carHashEntity.Entity)
                                continue;

                            float3 extents = carHashEntity.BoundsExtents + new float3(pedestrianCollider.Radius, 0, pedestrianCollider.Radius);

                            float3 carPosition = carHashEntity.Position;
                            float3 carPositionFlatted = carPosition.Flat();

                            quaternion obstacleRotation = carHashEntity.Rotation;

                            float yDiff = carPosition.y - worldTransform.Position.y;

                            var obstacleLayout = new ObstacleLayout(carPositionFlatted, obstacleRotation, extents);
                            ObstacleSquare obstacleSquare = new ObstacleSquare(obstacleLayout);

                            VectorExtensions.Line routeLine = new VectorExtensions.Line(localPedestrianPosition, destinationComponent.Value);

                            Vector3 intersectPoint = VectorExtensions.LineWithSquareIntersect(routeLine, obstacleSquare.Square, true);

                            var isIntersect = intersectPoint != Vector3.zero;

                            if (isIntersect)
                            {
                                var carForward = math.mul(carHashEntity.Rotation, math.forward());

                                var q = Quaternion.FromToRotation(carForward, math.forward());

                                var angle = math.abs(q.eulerAngles.x);

                                if (angle > 180)
                                {
                                    angle = 360f - angle;
                                }

                                if (angle > ObstacleAvoidanceSettingsReference.SettingsReference.Value.MaxSurfaceAngle)
                                {
                                    swapTarget = true;
                                }

                                if (ObstacleAvoidanceSettingsReference.SettingsReference.Value.CheckTargetAvailability)
                                {
                                    var hasNewTarget = false;
                                    float3 newDestination = default;

                                    var rndGen = UnityMathematicsExtension.GetRandomGen(Timestamp, entity.Index, worldTransform.Position);

                                    if (PedestrianNodeSettingsLookup.HasComponent(destinationComponent.DestinationNode))
                                    {
                                        var targetNodeSettings = PedestrianNodeSettingsLookup[destinationComponent.DestinationNode];
                                        var targetNodePosititon = WorldTransformLookup[destinationComponent.DestinationNode];

                                        var localBounds = new AABB()
                                        {
                                            Extents = extents
                                        };

                                        var matrix = float4x4.TRS(carPositionFlatted, obstacleRotation, Vector3.one);

                                        var rotatedAABB = AABB.Transform(matrix, localBounds);

                                        if (rotatedAABB.Contains(destinationComponent.Value))
                                        {
                                            int attemptCount = ObstacleAvoidanceSettingsReference.SettingsReference.Value.SearchNewTargetAttemptCount;

                                            while (attemptCount > 0)
                                            {
                                                newDestination = DestinationNodeUtils.GetDestination(rndGen, in targetNodeSettings, in targetNodePosititon);

                                                if (!rotatedAABB.Contains(newDestination))
                                                {
                                                    hasNewTarget = true;
                                                    break;
                                                }
                                                else
                                                {
                                                    rndGen.state = MathUtilMethods.ModifySeed(rndGen.state, attemptCount);
                                                    attemptCount--;
                                                }
                                            }

                                            if (!hasNewTarget)
                                            {
                                                swapTarget = true;
                                            }
                                            else
                                            {
                                                destinationComponent.Value = newDestination;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        swapTarget = true;
                                    }
                                }

                                if (swapTarget)
                                {
                                    break;
                                }

                                switch (ObstacleAvoidanceSettingsReference.SettingsReference.Value.ObstacleAvoidanceMethod)
                                {
                                    case ObstacleAvoidanceMethod.Simple:
                                        break;
                                    case ObstacleAvoidanceMethod.FindNeighbors:
                                        {
                                            ref var config = ref ObstacleAvoidanceSettingsReference.SettingsReference;
                                            GetOverlappedBoundsData(in CarHashMapSingleton.CarHashMap, in carHashEntity, in keys, in carPositionFlatted, in obstacleRotation, in config, out var found, out var newExtents);

                                            if (found)
                                            {
                                                carPositionFlatted = newExtents.center.Flat();
                                                obstacleRotation = quaternion.identity;
                                                extents = newExtents.extents;
                                            }

                                            break;
                                        }
                                }

                                float avoidanceOffset = ObstacleAvoidanceSettingsReference.SettingsReference.Value.ObstacleAvoidanceOffset;

                                if (points == null || !points.IsCreated)
                                {
                                    points = new NativeArray<float3>(4, Allocator.Temp);
                                }

                                obstacleLayout = new ObstacleLayout(carPositionFlatted, obstacleRotation, extents + new float3(avoidanceOffset, 0, avoidanceOffset));

                                points[0] = obstacleLayout.LeftBottomPoint;
                                points[1] = obstacleLayout.LeftTopPoint;
                                points[2] = obstacleLayout.RightTopPoint;
                                points[3] = obstacleLayout.RightBottomPoint;

                                var minDistance = float.MaxValue;

                                var targetIndex = 0;
                                var edgeIndex1 = 0;
                                var edgeIndex2 = 0;

                                for (int index = 0; index < points.Length; index++)
                                {
                                    var dist = math.distancesq(points[index], worldTransform.Position);

                                    if (dist < minDistance && dist > ObstacleAvoidanceSettingsReference.SettingsReference.Value.AchieveDistanceSQ)
                                    {
                                        minDistance = dist;
                                        targetIndex = index;
                                        edgeIndex1 = index - 1;

                                        if (edgeIndex1 < 0)
                                        {
                                            edgeIndex1 = points.Length - 1;
                                        }

                                        edgeIndex2 = (index + 1) % points.Length;
                                    }
                                }

                                float distance1 = math.distancesq(points[edgeIndex1], destinationComponent.Value);
                                float distance2 = math.distancesq(points[edgeIndex2], destinationComponent.Value);

                                pathBuffer.Clear();
                                float3 point1 = default;
                                float3 point2 = default;

                                point1 = points[targetIndex];

                                if (distance1 < distance2)
                                {
                                    point2 = points[edgeIndex1];
                                }
                                else
                                {
                                    point2 = points[edgeIndex2];
                                }

                                var dstDir = math.normalize(destinationComponent.Value - worldTransform.Position);
                                var curDir = math.normalize(point1 - worldTransform.Position);
                                var nextDir = math.normalize(point2 - point1);

                                float dirDot = math.dot(curDir, nextDir);
                                float dstDot = math.dot(dstDir, nextDir);

                                float3 targetPoint = default;
                                var yOffset = new float3(0, worldTransform.Position.y, 0);

                                bool hasFirstPoint = dirDot > SamePointDirection;
                                bool hasSecondPoint = dstDot > SamePointDirection;

                                if (hasFirstPoint || !hasSecondPoint)
                                {
                                    targetPoint = point1 + yOffset;

                                    pathBuffer.Add(new PathPointAvoidanceElement()
                                    {
                                        Point = point1 + yOffset
                                    });
                                }
                                else
                                {
                                    targetPoint = point2 + yOffset;
                                }

                                if (hasSecondPoint)
                                {
                                    pathBuffer.Add(new PathPointAvoidanceElement()
                                    {
                                        Point = point2 + yOffset
                                    });
                                }

                                navAgentSteeringComponent.SteeringTarget = 1;
                                navAgentSteeringComponent.SteeringTargetValue = targetPoint;
                                navAgentComponent.PathEndPosition = point2;
                                hasTarget = true;

                                points.Dispose();

                                if (hasTarget)
                                    break;
                            }

                        } while (CarHashMapSingleton.CarHashMap.TryGetNextValue(out carHashEntity, ref nativeMultiHashMapIterator));
                    }

                    if (hasTarget || swapTarget)
                    {
                        break;
                    }
                }

                keys.Dispose();

                if (points != null && points.IsCreated)
                {
                    points.Dispose();
                }

                if (hasTarget && !swapTarget)
                {
                    navAgentComponent.LastUpdateTimestamp = Timestamp;
                    navAgentComponent.HasPath = 1;

                    pathLocalAvoidanceEnabledTagRW.ValueRW = true;
                    enabledNavigationTagRW.ValueRW = true;
                }
                else
                {
                    navAgentComponent.HasPath = 0;

                    if (swapTarget)
                    {
                        destinationComponent = destinationComponent.SwapBack();
                    }
                }

                updateNavTargetTagRW.ValueRW = false;
            }
        }

        private static void GetOverlappedBoundsData(in NativeParallelMultiHashMap<int, CarHashEntityComponent> carHashMap, in CarHashEntityComponent sourceCarHashEntityComponent, in NativeList<int> keys, in float3 sourcePosition, in quaternion sourceRotation, in BlobAssetReference<ObstacleAvoidanceSettingsData> config, out bool overlapFound, out Bounds newBounds)
        {
            overlapFound = false;
            var sourceExtents = sourceCarHashEntityComponent.BoundsExtents;
            var targetOffset = config.Value.ObstacleAvoidanceOffset * 2;
            Bounds bounds = ObstacleLayoutHelper.GetRotatedBound(sourcePosition, sourceRotation, sourceExtents);
            Bounds tempBounds = ObstacleLayoutHelper.GetRotatedBound(sourcePosition, sourceRotation, sourceExtents, targetOffset);

            for (int i = 0; i < keys.Length; i++)
            {
                int pedestrianKey = keys[i];

                if (carHashMap.TryGetFirstValue(pedestrianKey, out var carHashEntity, out var nativeMultiHashMapIterator))
                {
                    do
                    {
                        if (carHashEntity.Entity == sourceCarHashEntityComponent.Entity)
                        {
                            continue;
                        }

                        var currentTempBounds = ObstacleLayoutHelper.GetRotatedBound(carHashEntity.Position.Flat(), carHashEntity.Rotation, carHashEntity.BoundsExtents, targetOffset);

                        var closeEnough = tempBounds.Intersects(currentTempBounds);

                        if (closeEnough)
                        {
                            overlapFound = true;

                            tempBounds.Encapsulate(currentTempBounds);

                            var targetCarBounds = ObstacleLayoutHelper.GetRotatedBound(carHashEntity.Position.Flat(), carHashEntity.Rotation, carHashEntity.BoundsExtents);

                            bounds.Encapsulate(targetCarBounds);
                        }

                    } while (carHashMap.TryGetNextValue(out carHashEntity, ref nativeMultiHashMapIterator));
                }
            }

            newBounds = bounds;
        }
    }
}