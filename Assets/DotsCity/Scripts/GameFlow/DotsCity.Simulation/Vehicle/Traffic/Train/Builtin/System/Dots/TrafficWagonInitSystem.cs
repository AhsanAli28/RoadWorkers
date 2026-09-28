using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Train
{
    [UpdateInGroup(typeof(StructuralInitGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficWagonInitSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<Transform>()
                .WithAll<TrainWagonInitTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var initJob = new InitJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                Graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>(),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false),
                TrafficPathComponentLookup = SystemAPI.GetComponentLookup<TrafficPathComponent>(false),
                TrafficDestinationComponentLookup = SystemAPI.GetComponentLookup<TrafficDestinationComponent>(false),
                TrafficFixedRouteComponentLookup = SystemAPI.GetComponentLookup<TrafficFixedRouteComponent>(false),
                TrainDataComponentLookup = SystemAPI.GetComponentLookup<TrainDataComponent>(true),
                BoundsComponentLookup = SystemAPI.GetComponentLookup<BoundsComponent>(true),
                ParentLookup = SystemAPI.GetComponentLookup<Parent>(true),
                FixedRouteNodeLookup = SystemAPI.GetBufferLookup<FixedRouteNodeElement>(true),
                TrafficCommonSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficCommonSettingsConfigBlobReference>(),
            };

            initJob.Run();
        }

        [WithNone(typeof(Transform))]
        [BurstCompile]
        partial struct InitJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public PathGraphSystem.Singleton Graph;

            public ComponentLookup<LocalTransform> LocalTransformLookup;
            public ComponentLookup<TrafficPathComponent> TrafficPathComponentLookup;
            public ComponentLookup<TrafficDestinationComponent> TrafficDestinationComponentLookup;
            public ComponentLookup<TrafficFixedRouteComponent> TrafficFixedRouteComponentLookup;

            [ReadOnly]
            public ComponentLookup<TrainDataComponent> TrainDataComponentLookup;

            [ReadOnly]
            public ComponentLookup<BoundsComponent> BoundsComponentLookup;

            [ReadOnly]
            public ComponentLookup<Parent> ParentLookup;

            [ReadOnly]
            public BufferLookup<FixedRouteNodeElement> FixedRouteNodeLookup;

            [ReadOnly]
            public TrafficCommonSettingsConfigBlobReference TrafficCommonSettingsConfigBlobReference;

            void Execute(
                Entity currentEntity,
                EnabledRefRW<TrainWagonInitTag> trafficWagonInitTagRW,
                DynamicBuffer<TrafficWagonElement> tafficWagonElement)
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

                float wagonOffset = trainDataComponent.WagonOffset;
                float currentTargetDistance = ownerBoundsComponent.Extents.z + wagonOffset;

                for (int i = 0; i < tafficWagonElement.Length; i++)
                {
                    var entity = tafficWagonElement[i].Entity;
                    var trafficPathComponent = sourceTrafficPathComponent;
                    var trafficDestinationComponent = sourceTrafficDestinationComponent;
                    var trafficRouteComponent = sourceTrafficRouteComponent;
                    var boundsComponent = BoundsComponentLookup[entity];

                    LocalTransform transform = default;

                    currentTargetDistance += boundsComponent.Extents.z;

                    var targetDistance = currentTargetDistance;

                    var localNodeIndex = trafficPathComponent.LocalPathNodeIndex;

                    var point = ownerPos;

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

                                var spawnPos = point - dir * (currentDistance1 + targetDistance);
                                var spawnRot = quaternion.LookRotationSafe(dir, math.up());

                                transform = LocalTransform.FromPositionRotation(spawnPos, spawnRot);

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
                                CommandBuffer.DestroyEntity(entity);
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

                    CommandBuffer.AddComponent(entity, trafficRouteComponent);

                    if (TrafficCommonSettingsConfigBlobReference.Reference.Value.EntityType != EntityType.PureEntityNoPhysics)
                        CommandBuffer.AddComponent<PhysicsVelocity>(entity);

                    if (ParentLookup.HasComponent(entity))
                        CommandBuffer.RemoveComponent<Parent>(entity);

                    LocalTransformLookup[entity] = transform;
                    TrafficPathComponentLookup[entity] = trafficPathComponent;
                    TrafficDestinationComponentLookup[entity] = trafficDestinationComponent;
                }
            }
        }
    }
}