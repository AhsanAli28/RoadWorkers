using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.TrafficPublic
{
    [UpdateInGroup(typeof(SpawnerGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class TrafficPublicSpawnerSystem : SystemBase
    {
        private struct RouteNode
        {
            public int Index;
            public Entity TrafficNodeEntity;
            public float3 Position;
            public quaternion Rotation;

            public RouteNode(int index, FixedRouteNodeElement fixedRouteNode)
            {
                this.Index = index;
                this.TrafficNodeEntity = fixedRouteNode.TrafficNodeEntity;
                this.Position = fixedRouteNode.Position;
                this.Rotation = fixedRouteNode.Rotation;
            }
        }

        private TrafficSpawnerSystem trafficSpawner;
        private EntityQuery updateQuery;
        private float spawnTime;

        private EntityArchetype routeSettingsArchetype;
        private int nextSpawnIndex = -1;
        private bool spawned = false;

        protected override void OnCreate()
        {
            base.OnCreate();

            trafficSpawner = World.GetOrCreateSystemManaged<TrafficSpawnerSystem>();

            routeSettingsArchetype = EntityManager.CreateArchetype(typeof(RouteTempEntitySettingsComponent));

            updateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TrafficPublicRouteCapacityComponent>()
                .Build(this);

            RequireForUpdate(updateQuery);
            RequireForUpdate<PathGraphSystem.Singleton>();
        }

        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            if (spawnTime > currentTime && nextSpawnIndex == -1)
                return;

            if (!TrafficSpawnerSystem.IsInitialized) return;

            var trafficPublicSpawnerSettings = SystemAPI.GetSingleton<TrafficPublicSpawnerSettingsReference>();
            spawnTime = currentTime + trafficPublicSpawnerSettings.Config.Value.SpawnFrequency;

            var graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>();
            var fixedRouteNodeElementLookup = SystemAPI.GetBufferLookup<FixedRouteNodeElement>(true);
            var pathSettingsLookup = SystemAPI.GetBufferLookup<PathConnectionElement>(true);
            var cullStateComponentLookup = SystemAPI.GetComponentLookup<CullStateComponent>(true);
            var trafficNodeAvailableComponentLookup = SystemAPI.GetComponentLookup<TrafficNodeAvailableComponent>(true);
            var time = (float)SystemAPI.Time.ElapsedTime;
            spawned = false;

            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .WithReadOnly(fixedRouteNodeElementLookup)
            .WithReadOnly(pathSettingsLookup)
            .WithReadOnly(cullStateComponentLookup)
            .WithReadOnly(trafficNodeAvailableComponentLookup)
            .ForEach((
                Entity routeEntity,
                ref TrafficPublicRouteCapacityComponent busRouteComponent,
                in TrafficPublicRouteSettings busRouteSettings) =>
            {
                if (busRouteComponent.CurrentVehicleCount >= busRouteSettings.MaxVehicleCount && nextSpawnIndex == -1)
                    return;

                if (spawned)
                    return;

                var nodes = fixedRouteNodeElementLookup[routeEntity];
                var routeLength = nodes.Length;

                NativeList<RouteNode> availableNodes = GetAvailableNodes(ref nodes, in cullStateComponentLookup, in trafficNodeAvailableComponentLookup);

                if (availableNodes.Length <= 0)
                {
                    if (availableNodes.IsCreated)
                    {
                        availableNodes.Dispose();
                    }

                    return;
                }

                RouteNode randomNode = default;

                if (nextSpawnIndex == -1)
                {
                    var rnd = UnityMathematicsExtension.GetRandomGen(time, routeEntity.Index);
                    randomNode = availableNodes[rnd.NextInt(0, availableNodes.Length - 1)];
                }
                else
                {
                    randomNode = new RouteNode(nextSpawnIndex, nodes[nextSpawnIndex]);
                    nextSpawnIndex = -1;
                }

                var currentIndex = randomNode.Index;
                var targetIndex = (randomNode.Index + 1) % nodes.Length;
                var nextIndex = (randomNode.Index + 2) % nodes.Length;

                var currentNodeEntity = nodes[currentIndex].TrafficNodeEntity;
                var targetNodeEntity = nodes[targetIndex].TrafficNodeEntity;
                var nextTargetNodeEntity = nodes[nextIndex].TrafficNodeEntity;

                var destinationComponent = new TrafficDestinationComponent()
                {
                    CurrentNode = currentNodeEntity,
                    PreviousNode = currentNodeEntity,
                    DestinationNode = targetNodeEntity,
                    NextDestinationNode = nextTargetNodeEntity,
                    Destination = nodes[targetIndex].Position,
                    NextGlobalPathIndex = nodes[targetIndex].PathKey
                };

                DynamicBuffer<PathConnectionElement> pathConnections = pathSettingsLookup[currentNodeEntity];

                int globalPathIndex = -1;
                int localPathIndex = -1;
                int localPathNodeIndex = 1;

                for (int j = 0; j < pathConnections.Length; j++)
                {
                    var tempGlobalPathIndex = pathConnections[j].GlobalPathIndex;

                    if (pathConnections[j].ConnectedNodeEntity == targetNodeEntity)
                    {
                        globalPathIndex = tempGlobalPathIndex;
                        localPathIndex = j;
                        break;
                    }
                }

                if (globalPathIndex >= 0)
                {
                    var trafficPublicModel = busRouteSettings.VehicleModel;

                    if (trafficPublicModel != -1)
                    {
                        ref readonly var previousNode = ref graph.GetPathNodeData(globalPathIndex, localPathNodeIndex - 1);
                        ref readonly var targetNode = ref graph.GetPathNodeData(globalPathIndex, localPathNodeIndex);
                        var targetWayPoint = targetNode.Position;
                        var previoutTargetWayPoint = previousNode.Position;

                        var trafficPathComponent = new TrafficPathComponent()
                        {
                            CurrentGlobalPathIndex = globalPathIndex,
                            DestinationWayPoint = targetWayPoint,
                            PreviousDestination = previoutTargetWayPoint,
                            LocalPathNodeIndex = localPathNodeIndex
                        };

                        var trafficSpawnParams = new TrafficSpawnParams(randomNode.Position, randomNode.Rotation, destinationComponent)
                        {
                            carModelIndex = (int)trafficPublicModel,
                            hasDriver = true,
                            targetNodeEntity = targetNodeEntity,
                            spawnNodeEntity = currentNodeEntity,
                            previousNodeEntity = currentNodeEntity,
                            globalPathIndex = globalPathIndex,
                            trafficPathComponent = trafficPathComponent,
                            customSpawnData = true,
                            trafficCustomInit = TrafficCustomInitType.TrafficPublic,
                        };

                        var tempEntity = EntityManager.CreateEntity(routeSettingsArchetype);

                        EntityManager.SetComponentData(tempEntity, new RouteTempEntitySettingsComponent()
                        {
                            RouteEntity = routeEntity,
                            RouteIndex = (randomNode.Index + 1) % routeLength,
                            RouteLength = routeLength,
                            TrafficPublicType = busRouteSettings.TrafficPublicType
                        });

                        trafficSpawnParams.customRelatedEntityIndex = tempEntity;

                        trafficSpawner.Spawn(trafficSpawnParams, busRouteSettings.IgnoreCamera);

                        busRouteComponent.CurrentVehicleCount++;
                        spawned = true;
                    }
                }

                availableNodes.Dispose();
            }).Run();
        }

        public void ForceSpawn(int index)
        {
            this.nextSpawnIndex = index;
        }

        private NativeList<RouteNode> GetAvailableNodes(ref DynamicBuffer<FixedRouteNodeElement> nodes, in ComponentLookup<CullStateComponent> cullStateComponentLookup, in ComponentLookup<TrafficNodeAvailableComponent> trafficNodeAvailableComponentLookup)
        {
            var availableNodes = new NativeList<RouteNode>(Allocator.TempJob);

            for (int j = 0; j < nodes.Length; j++)
            {
                if (nodes[j].IsAvailable && nodes[j].IsChangeLaneNode == 0 && cullStateComponentLookup.HasComponent(nodes[j].TrafficNodeEntity))
                {
                    var cullState = cullStateComponentLookup[nodes[j].TrafficNodeEntity].State;
                    var isAvailable = trafficNodeAvailableComponentLookup[nodes[j].TrafficNodeEntity].IsAvailable;

                    if (cullState != CullState.Culled && isAvailable)
                    {
                        availableNodes.Add(new RouteNode(j, nodes[j]));
                    }
                }
            }

            return availableNodes;
        }
    }
}