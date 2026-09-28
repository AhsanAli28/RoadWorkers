using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Train
{
    [UpdateInGroup(typeof(StructuralInitGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class TrafficMonoWagonInitSystem : SystemBase
    {
        private class InitData
        {
            public Entity Entity;
            public Rigidbody Rb;
        }

        private EntityQuery updateQuery;
        private List<InitData> initList;

        protected override void OnCreate()
        {
            base.OnCreate();

            initList = new List<InitData>();

            updateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Transform, TrainWagonMonoInitTag, TrafficFixedRouteComponent>()
                .Build(this);

            RequireForUpdate(updateQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            var Graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>();
            var trafficRailConfigReference = SystemAPI.GetSingleton<TrafficRailConfigReference>();

            var LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false);
            var TrafficPathComponentLookup = SystemAPI.GetComponentLookup<TrafficPathComponent>(false);
            var trafficSettingsComponentLookup = SystemAPI.GetComponentLookup<TrafficSettingsComponent>(false);
            var TrafficDestinationComponentLookup = SystemAPI.GetComponentLookup<TrafficDestinationComponent>(false);
            var TrafficFixedRouteComponentLookup = SystemAPI.GetComponentLookup<TrafficFixedRouteComponent>(false);
            var TrainDataComponentLookup = SystemAPI.GetComponentLookup<TrainDataComponent>(true);
            var BoundsComponentLookup = SystemAPI.GetComponentLookup<BoundsComponent>(true);
            var FixedRouteNodeLookup = SystemAPI.GetBufferLookup<FixedRouteNodeElement>(true);

            foreach (var (transform, rb, trafficWagonInitTagRW, trafficWagonBuffer, currentEntity)
                in SystemAPI.Query<
                    SystemAPI.ManagedAPI.UnityEngineComponent<Transform>,
                    SystemAPI.ManagedAPI.UnityEngineComponent<Rigidbody>,
                    EnabledRefRW<TrainWagonMonoInitTag>,
                    DynamicBuffer<TrafficWagonElement>>()
                .WithAll<TrafficFixedRouteComponent>()
                .WithEntityAccess())
            {
                if (!TrafficFixedRouteComponentLookup.HasComponent(currentEntity))
                    return;

                trafficWagonInitTagRW.ValueRW = false;

                var ownerPos = LocalTransformLookup[currentEntity].Position;
                var ownerRot = LocalTransformLookup[currentEntity].Rotation;
                var sourceTrafficPathComponent = TrafficPathComponentLookup[currentEntity];
                var sourceTrafficDestinationComponent = TrafficDestinationComponentLookup[currentEntity];
                var sourceTrafficRouteComponent = TrafficFixedRouteComponentLookup[currentEntity];
                var trainDataComponent = TrainDataComponentLookup[currentEntity];
                var ownerBoundsComponent = BoundsComponentLookup[currentEntity];

                var trafficSettingsComponent = trafficSettingsComponentLookup[currentEntity];

                trafficSettingsComponent.Acceleration = trafficRailConfigReference.Config.Value.Acceleration;
                trafficSettingsComponent.BrakePower = trafficRailConfigReference.Config.Value.BrakePower;

                trafficSettingsComponentLookup[currentEntity] = trafficSettingsComponent;

                transform.Value.SetPositionAndRotation(ownerPos, ownerRot);
                rb.Value.Move(ownerPos, ownerRot);

                var trainRuntimeAuthoring = transform.Value.GetComponent<TrainRuntimeAuthoring>();
                var wagons = trainRuntimeAuthoring.Wagons;

                float wagonOffset = trainDataComponent.WagonOffset;
                float currentTargetDistance = ownerBoundsComponent.Extents.z + wagonOffset;

                for (int i = 0; i < trafficWagonBuffer.Length; i++)
                {
                    var trafficPathComponent = sourceTrafficPathComponent;
                    var trafficDestinationComponent = sourceTrafficDestinationComponent;
                    var trafficRouteComponent = sourceTrafficRouteComponent;
                    var entity = trafficWagonBuffer[i].Entity;
                    var boundsComponent = BoundsComponentLookup[entity];

                    currentTargetDistance += boundsComponent.Extents.z;

                    var targetDistance = currentTargetDistance;
                    var localNodeIndex = trafficPathComponent.LocalPathNodeIndex;

                    var point = ownerPos;

                    var wagonTransform = wagons[i].transform;
                    wagonTransform.SetParent(transform.Value.parent);
                    var wagonRb = wagons[i].Rb;

                    while (targetDistance > 0)
                    {
                        localNodeIndex--;

                        if (localNodeIndex >= 0)
                        {
                            ref readonly var pathNode = ref Graph.GetPathNodeData(trafficPathComponent.CurrentGlobalPathIndex, localNodeIndex);

                            var currentDistance1 = math.distance(point, pathNode.Position);

                            targetDistance -= currentDistance1;

                            if (targetDistance < 0)
                            {
                                var dir = math.normalize(point - pathNode.Position);

                                wagonTransform.position = point - dir * (currentDistance1 + targetDistance);
                                wagonTransform.rotation = quaternion.LookRotationSafe(dir, math.up());

                                wagonRb.Move(wagonTransform.position, wagonTransform.rotation);

                                commandBuffer.SetComponent(entity, LocalTransform.FromPositionRotation(wagonTransform.position, wagonTransform.rotation));
                                ref readonly var targetPathNode = ref Graph.GetPathNodeData(trafficPathComponent.CurrentGlobalPathIndex, localNodeIndex + 1);

                                trafficPathComponent.DestinationWayPoint = targetPathNode.Position;
                                trafficPathComponent.LocalPathNodeIndex = localNodeIndex + 1;
                            }
                            else
                            {
                                point = pathNode.Position;
                            }
                        }
                        else
                        {
                            var connectedByPaths = Graph.GetConnectedByPaths(trafficPathComponent.CurrentGlobalPathIndex);

                            if (connectedByPaths.Length == 0)
                            {
                                commandBuffer.DestroyEntity(entity);
                                return;
                            }
                            else
                            {
                                var connectedByPathIndex = connectedByPaths[0];
                                ref readonly var connectedByPath = ref Graph.GetPathData(connectedByPathIndex);

                                trafficPathComponent.CurrentGlobalPathIndex = connectedByPathIndex;
                                localNodeIndex = connectedByPath.NodeCount - 1;

                                ref readonly var targetPathNode = ref Graph.GetPathNodeData(connectedByPathIndex, localNodeIndex);

                                trafficDestinationComponent.Destination = targetPathNode.Position;

                                var route = FixedRouteNodeLookup[trafficRouteComponent.RouteEntity];

                                trafficRouteComponent.RouteNodeIndex--;

                                if (trafficRouteComponent.RouteNodeIndex < 0)
                                {
                                    trafficRouteComponent.RouteNodeIndex = route.Length - 1;
                                }

                                int routeNodeIndex = trafficRouteComponent.RouteNodeIndex;
                                int newRouteNodeIndex = (trafficRouteComponent.RouteNodeIndex + 1) % route.Length;
                                int nextRouteNodeIndex = (trafficRouteComponent.RouteNodeIndex + 2) % route.Length;

                                var newTargetEntity = route[newRouteNodeIndex].TrafficNodeEntity;
                                var nextTargetEntity = route[nextRouteNodeIndex].TrafficNodeEntity;

                                trafficDestinationComponent.DestinationNode = newTargetEntity;
                                trafficDestinationComponent.NextDestinationNode = nextTargetEntity;
                                trafficDestinationComponent.NextGlobalPathIndex = route[routeNodeIndex].PathKey;
                            }
                        }
                    }

                    currentTargetDistance += boundsComponent.Extents.z + wagonOffset;

                    commandBuffer.RemoveComponent<Parent>(entity);
                    commandBuffer.AddComponent(entity, trafficRouteComponent);

                    initList.Add(new InitData()
                    {
                        Entity = entity,
                        Rb = wagonRb
                    });

                    TrafficPathComponentLookup[entity] = trafficPathComponent;
                    TrafficDestinationComponentLookup[entity] = trafficDestinationComponent;
                }
            }

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            for (int i = 0; i < initList.Count; i++)
            {
                var entity = initList[i].Entity;
                var rigidbody = initList[i].Rb;

                EntityManager.AddComponentObject(entity, rigidbody.transform);
                EntityManager.AddComponentObject(entity, rigidbody);

                var physicsHybridEntityAdapter = rigidbody.GetComponent<PhysicsHybridEntityAdapter>();
                physicsHybridEntityAdapter.Enabled = true;
                physicsHybridEntityAdapter.HasCulling = false;
                physicsHybridEntityAdapter.Initialize(entity);
                physicsHybridEntityAdapter.CheckCullState(CullState.InViewOfCamera);

                var trainWagonMonoAdapter = rigidbody.GetComponent<TrainWagonMonoAdapter>();
                trainWagonMonoAdapter.Initialize();

                EntityManager.AddComponentObject(entity, physicsHybridEntityAdapter);
            }

            initList.Clear();
        }
    }
}