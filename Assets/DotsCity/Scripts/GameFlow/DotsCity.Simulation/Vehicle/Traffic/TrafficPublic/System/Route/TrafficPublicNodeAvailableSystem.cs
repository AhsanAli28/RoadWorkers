using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.TrafficPublic
{
    [UpdateInGroup(typeof(TrafficLateSimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficPublicNodeAvailableSystem : ISystem, ISystemStartStop
    {
        public struct TrafficPublicHashEntity
        {
            public Entity Entity;
            public float3 Position;
        }

        private EntityQuery busGroup;
        private EntityQuery updateGroup;

        private NativeParallelMultiHashMap<int, TrafficPublicHashEntity> routeHashMap;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            busGroup = SystemAPI.QueryBuilder()
                .WithAll<TrafficPublicTag, LocalTransform, TrafficFixedRouteComponent>()
                .Build();

            updateGroup = SystemAPI.QueryBuilder()
                .WithAll<TrafficPublicRouteCapacityComponent>()
                .Build();

            state.RequireForUpdate(updateGroup);
            state.RequireForUpdate<TrafficPublicSpawnerSettingsReference>();
            state.RequireForUpdate<TrafficNodeResolverSystem.RuntimePathDataRef>();
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            if (routeHashMap.IsCreated)
            {
                routeHashMap.Dispose();
            }
        }

        void ISystemStartStop.OnStartRunning(ref SystemState state)
        {
            if (!routeHashMap.IsCreated)
            {
                var settings = SystemAPI.GetSingleton<TrafficPublicSpawnerSettingsReference>();
                routeHashMap = new NativeParallelMultiHashMap<int, TrafficPublicHashEntity>(settings.Config.Value.RouteHashMapCapacity, Allocator.Persistent);
            }
        }

        void ISystemStartStop.OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var trafficPublicPositions = busGroup.ToComponentDataListAsync<LocalTransform>(Allocator.TempJob, state.Dependency, out var arrDep1);
            var trafficPublicRouteLinks = busGroup.ToComponentDataListAsync<TrafficFixedRouteComponent>(Allocator.TempJob, state.Dependency, out var arrDep2);

            var arrDep = JobHandle.CombineDependencies(arrDep1, arrDep2);

            routeHashMap.Clear();
            NativeParallelMultiHashMap<int, TrafficPublicHashEntity>.ParallelWriter routeNativeMultiHashMapLocalParallel = routeHashMap.AsParallelWriter();

            var routeHashMapJob = new RouteHashMapJob()
            {
                RouteNativeMultiHashMapParallel = routeNativeMultiHashMapLocalParallel,
                TrafficPublicPositions = trafficPublicPositions,
                TrafficPublicRouteLinks = trafficPublicRouteLinks,
            };

            var fillHashMapHandle = routeHashMapJob.ScheduleParallel(arrDep);

            trafficPublicPositions.Dispose(fillHashMapHandle);
            trafficPublicRouteLinks.Dispose(fillHashMapHandle);

            var availableJob = new AvailableRouteNodeJob()
            {
                RouteNativeMultiHashMapParallel = routeHashMap,
                RuntimePathDataRef = SystemAPI.GetSingleton<TrafficNodeResolverSystem.RuntimePathDataRef>(),
                TrafficNodeSettingsLookup = SystemAPI.GetComponentLookup<TrafficNodeSettingsComponent>(true),
                InViewOfCameraLookup = SystemAPI.GetComponentLookup<InViewOfCameraTag>(true),
            };

            state.Dependency = availableJob.ScheduleParallel(fillHashMapHandle);
        }

        [BurstCompile]
        public partial struct RouteHashMapJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            public NativeParallelMultiHashMap<int, TrafficPublicHashEntity>.ParallelWriter RouteNativeMultiHashMapParallel;

            [ReadOnly]
            public NativeList<LocalTransform> TrafficPublicPositions;

            [ReadOnly]
            public NativeList<TrafficFixedRouteComponent> TrafficPublicRouteLinks;

            void Execute(
                Entity routeEntity,
                ref TrafficPublicRouteCapacityComponent capacityComponent)
            {
                for (int i = 0; i < TrafficPublicRouteLinks.Length; i++)
                {
                    if (TrafficPublicRouteLinks[i].RouteEntity == routeEntity)
                    {
                        var busHashEntity = new TrafficPublicHashEntity()
                        {
                            Position = TrafficPublicPositions[i].Position,
                        };

                        int key = routeEntity.Index;

                        RouteNativeMultiHashMapParallel.Add(key, busHashEntity);
                    }
                }
            }
        }

        [BurstCompile]
        public partial struct AvailableRouteNodeJob : IJobEntity
        {
            [ReadOnly]
            public NativeParallelMultiHashMap<int, TrafficPublicHashEntity> RouteNativeMultiHashMapParallel;

            [ReadOnly]
            public TrafficNodeResolverSystem.RuntimePathDataRef RuntimePathDataRef;

            [ReadOnly]
            public ComponentLookup<TrafficNodeSettingsComponent> TrafficNodeSettingsLookup;

            [ReadOnly]
            public ComponentLookup<InViewOfCameraTag> InViewOfCameraLookup;

            void Execute(
                Entity entity,
                ref DynamicBuffer<FixedRouteNodeElement> routeElements,
                in TrafficPublicRouteSettings busRouteSettings,
                in TrafficPublicRouteCapacityComponent busRouteComponent)
            {
                bool hasSlots = busRouteComponent.CurrentVehicleCount < busRouteSettings.MaxVehicleCount;

                for (int i = 0; i < routeElements.Length; i++)
                {
                    var routeElement = routeElements[i];

                    var nodePosition = routeElement.Position;
                    var exist = TrafficNodeSettingsLookup.HasComponent(routeElement.TrafficNodeEntity);

                    if (!exist && routeElement.TrafficNodeEntity != Entity.Null)
                    {
                        routeElement.TrafficNodeEntity = Entity.Null;
                    }
                    else if (routeElement.TrafficNodeEntity == Entity.Null)
                    {
                        var sourceNode = RuntimePathDataRef.TryToGetSourceNode(routeElement.PathKey);

                        if (TrafficNodeSettingsLookup.HasComponent(sourceNode))
                        {
                            routeElement.TrafficNodeEntity = sourceNode;
                            exist = true;
                        }
                    }

                    var notInViewOfCamera = false;
                    var canSpawn = false;

                    if (exist)
                    {
                        var hasComponent = InViewOfCameraLookup.HasComponent(routeElement.TrafficNodeEntity);
                        notInViewOfCamera = !hasComponent || hasComponent && !InViewOfCameraLookup.IsComponentEnabled(routeElement.TrafficNodeEntity) || busRouteSettings.IgnoreCamera;
                        var nodeSettings = TrafficNodeSettingsLookup[routeElement.TrafficNodeEntity];
                        canSpawn = nodeSettings.ChanceToSpawn > 0;
                    }

                    bool isAvailable = true;

                    if (hasSlots && notInViewOfCamera && exist && canSpawn)
                    {
                        int key = entity.Index;

                        if (RouteNativeMultiHashMapParallel.TryGetFirstValue(key, out var busHashEntity, out var nativeMultiHashMapIterator))
                        {
                            do
                            {
                                var busPosition = busHashEntity.Position;

                                float distance = math.distancesq(busPosition, nodePosition);

                                if (distance < busRouteSettings.PreferredIntervalDistanceSQ)
                                {
                                    isAvailable = false;
                                    break;
                                }
                            } while (RouteNativeMultiHashMapParallel.TryGetNextValue(out busHashEntity, ref nativeMultiHashMapIterator));
                        }
                    }
                    else
                    {
                        isAvailable = false;
                    }

                    routeElement.IsAvailable = isAvailable;
                    routeElements[i] = routeElement;
                }
            }
        }
    }
}