using Spirit604.Attributes;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

#if RUNTIME_ROAD
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Road.Authoring;
using Spirit604.DotsCity.Simulation.Traffic;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Transforms;
#endif

namespace Spirit604.DotsCity.RuntimeRoad
{
    [DefaultExecutionOrder(-10000)]
    public class RuntimeRoadManager : RuntimeRoadManagerBase
    {
        public enum ConnectionType { MergeOnly, ConnectOrMerge }
        public enum MergeType { None, Merged, Connected }

        public class BindingData
        {
            public TrafficNode Node1;
            public TrafficNode Node2;
            public MergeType ConnectionType;
        }

        public class ConnectionData
        {
            /// <summary> First & Last Nodes EntityPair Nodes of the connection, inner nodes in order as they are connected 1 by 1. </summary>
            public List<Entity> Nodes;

            public bool InitiallyConnected;
        }

        private const int ConnectionBufferSize = 20;

#pragma warning disable 0414

        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/runtimeRoad.html#runtimetileroad-demo-structure")]
        [SerializeField] private string link;
        [SerializeField] private bool addCullingComponents = true;

        private ConnectionType connectionType = ConnectionType.MergeOnly;
        [SerializeField] private float castDistance = 2f;
        private float mergeDistance = 2f;

#pragma warning restore 0414

#if RUNTIME_ROAD

#if UNITY_EDITOR
        [NonSerialized] public List<TrafficNode> brokenNodes = new List<TrafficNode>();
#endif

        private EntityQuery trafficQuery;
        private EntityQuery pedestrianQuery;
        private EntityQuery trafficNodeAvailableQuery;
        private EntityQuery allPedestrianNodeQuery;
        private EntityQuery allTrafficNodeQuery;

        private Queue<RuntimeSegment> initQueue = new Queue<RuntimeSegment>();
        private Coroutine coroutine;
        private int crossroadIndex;
        private int crosswalkIndex;

        private Dictionary<TrafficNode, BindingData> nodeMerge1 = new Dictionary<TrafficNode, BindingData>();
        private Dictionary<TrafficNode, BindingData> nodeMerge2 = new Dictionary<TrafficNode, BindingData>();

        private List<PedestrianNode> recalcNodes = new List<PedestrianNode>();
        private Dictionary<TrafficNode, int> crosswalkBinding = new Dictionary<TrafficNode, int>();
        private Dictionary<Entity, List<Entity>> lightLinkedEntities = new Dictionary<Entity, List<Entity>>();

        private Dictionary<TrafficNode, List<Entity>> sceneTrafficNodeBindingRight = new Dictionary<TrafficNode, List<Entity>>();
        private Dictionary<TrafficNode, List<Entity>> sceneTrafficNodeBindingLeft = new Dictionary<TrafficNode, List<Entity>>();
        private Dictionary<PedestrianNode, Entity> scenePedestrianNodeBinding = new Dictionary<PedestrianNode, Entity>();
        private Dictionary<TrafficLightHandler, Entity> lightEntities = new Dictionary<TrafficLightHandler, Entity>();

        // Disabled paths connected to the TrafficNode of the key that merged which other TrafficNode but no way
        private Dictionary<TrafficNode, List<Path>> disabledPathData = new();

        // Traffic node containing disabled traffic node entities that don't have way with merged traffic node
        private HashSet<TrafficNode> disabledNodes = new();

        // Extra entities for lanes generated if lane count doesn't match merged node
        private Dictionary<TrafficNode, List<Entity>> partialMergeNodes = new();

        private Coroutine removeRoutine;
        private Queue<RuntimeSegment> segmentToAdds = new Queue<RuntimeSegment>();
        private bool simulationDisabled;
        private List<Path> tempConnectedPaths = new List<Path>();

        private RaycastHit[] hitsBuffer = new RaycastHit[ConnectionBufferSize];
        private List<PedestrianNode> tempNodes = new List<PedestrianNode>();
        private List<PedestrianNode> tempNodes2 = new List<PedestrianNode>();

        // Local runtime connections between pedestrian nodes
        private Dictionary<Entity, List<Entity>> runtimeConnection = new Dictionary<Entity, List<Entity>>();

        // Local runtime inversed connections between pedestrian nodes 
        private Dictionary<Entity, List<Entity>> runtimeConnectionInversed = new Dictionary<Entity, List<Entity>>();

        // All runtime connections between pedestrian nodes
        private Dictionary<EntityPair, ConnectionData> connections = new Dictionary<EntityPair, ConnectionData>();

        // Inner nodes linked to pair connection
        private Dictionary<Entity, EntityPair> entityInnerToConnectionLink = new Dictionary<Entity, EntityPair>();

        // Only pair nodes linked to pairs
        private Dictionary<Entity, List<EntityPair>> entityPairToConnectionLink = new Dictionary<Entity, List<EntityPair>>();

        private List<RuntimeSegment> segments = new List<RuntimeSegment>();

        public event Action<RuntimeSegment> OnSegmentAdded = delegate { };
        public event Action<RuntimeSegment> OnSegmentRemoved = delegate { };

        public static RuntimeRoadManager Instance { get; private set; }

#endif

        protected override void Awake()
        {
            base.Awake();

#if !RUNTIME_ROAD
            Debug.Log($"RuntimeRoadManager. Make sure you have added the scripting define 'RUNTIME_ROAD' to the project's player settings for the current platform.\r\n" +
                "For more info read the <a href=\"https://dotstrafficcity.readthedocs.io/en/latest/runtimeRoad.html#installation\">https://dotstrafficcity.readthedocs.io/en/latest/runtimeRoad.html#installation</a>\r\n\r\n\r\n\r\n\r\n");
#else
            Instance = this;
            trafficQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficTag>());
            pedestrianQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PedestrianMovementSettings>());
            trafficNodeAvailableQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficNodeComponent>(), ComponentType.Exclude<CulledEventTag>());
            allPedestrianNodeQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NodeSettingsComponent>());
            allTrafficNodeQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficNodeComponent>());
#endif
        }

#if RUNTIME_ROAD

        private void OnDestroy()
        {
            for (int i = 0; i < segments.Count; i++)
            {
                segments[i].Dispose();
            }
        }

        public void AddSegment(RuntimeSegment runtimeSegment)
        {
            if (!Init)
            {
                initQueue.Enqueue(runtimeSegment);

                if (coroutine == null)
                {
                    coroutine = StartCoroutine(WaitForInit());
                }

                return;
            }

            if (removeRoutine != null)
            {
                segmentToAdds.Enqueue(runtimeSegment);
                return;
            }

            AddSegmentInternal(runtimeSegment);
            SaveGraph();

            PostAddAction();
        }

        public void RemoveSegment(RuntimeSegment runtimeSegment)
        {
            OnSegmentRemoved(runtimeSegment);

            var destroyEntities = new NativeHashSet<Entity>(100, Allocator.Temp);
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            var crossroadData = runtimeSegment.crossroadData;

            var connectedNodeList = new NativeList<Entity>(Allocator.Temp);

            for (int i = 0; i < runtimeSegment.pedestrianNodes.Length; i++)
            {
                var pedestrianNode = runtimeSegment.pedestrianNodes[i];
                var entity = TryToGetEntity(pedestrianNode);

                DestroyPedestrian(ref commandBuffer, entity, ref connectedNodeList);
            }

            for (int i = 0; i < runtimeSegment.pedestrianNodes.Length; i++)
            {
                var pedestrianNode = runtimeSegment.pedestrianNodes[i];
                var pos = pedestrianNode.transform.position;
                var entity = TryToGetEntity(pedestrianNode);

                var mergeNode = TryToGetMergeNode(pedestrianNode);

                if (mergeNode != null)
                {
                    recalcNodes.Add(mergeNode);
                }
                else
                {
                    destroyEntities.Add(entity);
                }
            }

            for (int i = 0; i < runtimeSegment.pedestrianNodes.Length; i++)
            {
                var pedestrianNode = runtimeSegment.pedestrianNodes[i];
                var entity = TryToGetEntity(pedestrianNode);

                if (!destroyEntities.Contains(entity))
                    continue;

                RemoveRuntimeConnection(entity, true);

                var connectedBuffer = EntityManager.GetBuffer<NodeConnectionDataElement>(entity, true);

                for (int j = 0; j < connectedBuffer.Length; j++)
                {
                    var connectedEntity = connectedBuffer[j].ConnectedEntity;

                    if (!destroyEntities.Contains(connectedEntity))
                    {
                        RemoveConnectedEntity(connectedEntity, entity);
                    }

                    DestroyPedestrian(ref commandBuffer, entity, connectedEntity);
                }
            }

            foreach (var data in crossroadData)
            {
                var crossroad = data.Key;
                if (crossroad == null) continue;

                DestroyLights(crossroad, ref destroyEntities);

                var nodes = crossroad.TrafficNodes;

                for (int i = 0; i < nodes.Count; i++)
                {
                    var trafficNode = nodes[i];

                    if (crosswalkBinding.ContainsKey(trafficNode))
                    {
                        crosswalkBinding.Remove(trafficNode);
                    }

                    var mergeType = GetMergeType(trafficNode);

                    trafficNode.IterateAllLanes((laneIndex, external) =>
                    {
                        var entity = TryToGetEntity(trafficNode, external, laneIndex);

                        if (mergeType == MergeType.None)
                        {
                            var buffer = EntityManager.GetBuffer<PathConnectionElement>(entity, true);

                            for (int i = 0; i < buffer.Length; i++)
                            {
                                var currentPathIndex = buffer[i].GlobalPathIndex;
                                RemovePath(currentPathIndex);
                            }

                            destroyEntities.Add(entity);
                        }
                        else
                        {
                            if (entity != Entity.Null)
                            {
                                var buffer = EntityManager.GetBuffer<PathConnectionElement>(entity, false);

                                int localBufferIndex = 0;

                                while (localBufferIndex < buffer.Length)
                                {
                                    var currentPathIndex = buffer[localBufferIndex].GlobalPathIndex;

                                    if (runtimeSegment.PathIsTaken(currentPathIndex))
                                    {
                                        buffer.RemoveAt(localBufferIndex);
                                        RemovePath(currentPathIndex);
                                    }
                                    else
                                    {
                                        localBufferIndex++;
                                    }
                                }
                            }
                        }

                    }, true, false);

                    if (mergeType != MergeType.None)
                    {
                        var mergeNode = TryToGetMergeNode(trafficNode);

                        if (disabledPathData.ContainsKey(trafficNode))
                        {
                            disabledPathData.Remove(trafficNode);
                        }

                        if (disabledPathData.TryGetValue(mergeNode, out var disabledPaths))
                        {
                            var mergeOwner = GetOwnerSegment(mergeNode);

                            for (int j = 0; j < disabledPaths.Count; j++)
                            {
                                Path disabledPath = disabledPaths[j];
                                SwitchPathEnabled(mergeOwner, disabledPath, true);
                            }

                            disabledPathData.Remove(mergeNode);
                        }

                        RestoreNodes(ref commandBuffer, mergeNode, mergeNode);
                        RestoreNodes(ref commandBuffer, trafficNode, mergeNode);

                        if (partialMergeNodes.TryGetValue(trafficNode, out var partialEntities))
                        {
                            for (var j = 0; j < partialEntities.Count; j++)
                            {
                                commandBuffer.DestroyEntity(partialEntities[j]);
                            }

                            partialMergeNodes.Remove(trafficNode);
                        }
                        else if (partialMergeNodes.ContainsKey(mergeNode))
                        {
                            partialMergeNodes.Remove(mergeNode);
                        }

                        if (nodeMerge2.ContainsKey(trafficNode))
                        {
                            nodeMerge1.Remove(nodeMerge2[trafficNode].Node1);
                            nodeMerge2.Remove(trafficNode);
                        }
                        else if (nodeMerge1.ContainsKey(trafficNode))
                        {
                            nodeMerge2.Remove(nodeMerge1[trafficNode].Node2);
                            nodeMerge1.Remove(trafficNode);
                        }
                    }

                    if (sceneTrafficNodeBindingRight.ContainsKey(trafficNode))
                    {
                        sceneTrafficNodeBindingRight.Remove(trafficNode);
                    }

                    if (sceneTrafficNodeBindingLeft.ContainsKey(trafficNode))
                    {
                        sceneTrafficNodeBindingLeft.Remove(trafficNode);
                    }
                }
            }

            connectedNodeList.Dispose();

            if (destroyEntities.Count > 0)
            {
                var arr = destroyEntities.ToNativeArray(Allocator.Temp);
                commandBuffer.DestroyEntity(arr);
                arr.Dispose();
            }

            destroyEntities.Dispose();

            var tempConnectedEntities = new NativeHashSet<Entity>(10, Allocator.Temp);

            for (int i = 0; i < recalcNodes.Count; i++)
            {
                var pedestrianNode = recalcNodes[i];
                var entity = TryToGetEntity(pedestrianNode);


                tempConnectedEntities.Clear();

                for (int j = 0; j < pedestrianNode.DefaultConnectedPedestrianNodes.Count; j++)
                {
                    var connectedNode = pedestrianNode.DefaultConnectedPedestrianNodes[j];
                    var connectedEntity = TryToGetEntity(connectedNode);
                    tempConnectedEntities.Add(connectedEntity);
                }

                int n = 0;
                float removeWeight = 0;
                float previousWeight = 0;

                var settings = EntityManager.GetComponentData<NodeSettingsComponent>(entity);

                if (pedestrianNode.RelatedTrafficLightHandler != null)
                {
                    var nodeLightSettingsComponent = EntityManager.GetComponentData<NodeLightSettingsComponent>(entity);

                    var lightEntity = TryToGetEntity(pedestrianNode.RelatedTrafficLightHandler);

                    if (lightEntity != Entity.Null)
                    {
                        nodeLightSettingsComponent.LightEntity = lightEntity;
                        EntityManager.SetComponentData(entity, nodeLightSettingsComponent);
                    }
                }
                if (EntityManager.HasBuffer<NodeConnectionDataElement>(entity))
                {
                    var buffer = EntityManager.GetBuffer<NodeConnectionDataElement>(entity);
                    while (n < buffer.Length)
                    {
                        var connectedEntity = buffer[n].ConnectedEntity;
                        var found = tempConnectedEntities.Contains(connectedEntity);

                        if (found)
                        {
                            previousWeight = buffer[n].SumWeight;
                            n++;
                        }
                        else
                        {
                            if (IsCustomConnectionLine(entity, connectedEntity, out var pair))
                            {
                                TryToRemovePairConnection(pair);
                                continue;
                            }

                            var currentRemoveWeight = buffer[n].SumWeight - previousWeight;
                            removeWeight += currentRemoveWeight;

                            for (int k = n + 1; k < buffer.Length; k++)
                            {
                                var entry = buffer[k];
                                entry.SumWeight -= currentRemoveWeight;
                                buffer[k] = entry;
                            }

                            DestroyPedestrian(ref commandBuffer, entity, connectedEntity);

                            buffer.RemoveAt(n);
                        }
                    }
                }

                if (removeWeight > 0)
                {
                    settings.SumWeight -= removeWeight;
                    EntityManager.SetComponentData(entity, settings);
                }
            }

            recalcNodes.Clear();

            for (int i = 0; i < runtimeSegment.pedestrianNodes.Length; i++)
            {
                var pedestrianNode = runtimeSegment.pedestrianNodes[i];

                scenePedestrianNodeBinding.Remove(pedestrianNode);
            }

            tempConnectedEntities.Dispose();

            DestroyTraffic(ref commandBuffer, runtimeSegment.TakenPaths);

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            removeRoutine = StartCoroutine(Remove(runtimeSegment));
        }

        private void AddSegmentInternal(RuntimeSegment runtimeSegment)
        {
            EntityManager.CompleteAllTrackedJobs();

            EntityCommandBuffer commandBuffer = default;

            var crossroadData = runtimeSegment.crossroadData;

            var recorded = false;

            commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var data in crossroadData)
            {
                var trafficLightCrossroad = data.Key;

                trafficLightCrossroad.CrossroadIndex = crossroadIndex++;

                if (trafficLightCrossroad == null || !trafficLightCrossroad.HasLights) continue;

                recorded = true;
                CreateLight(trafficLightCrossroad, data.Value.Lights, ref commandBuffer);
            }

            if (recorded)
                commandBuffer.Playback(EntityManager);

            commandBuffer.Dispose();

            commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var data in crossroadData)
            {
                var trafficLightCrossroad = data.Key;
                if (trafficLightCrossroad == null) continue;

                var nodes = trafficLightCrossroad.TrafficNodes;

                for (int i = 0; i < nodes.Count; i++)
                {
                    CreateTrafficNodeEntities(runtimeSegment, trafficLightCrossroad, nodes[i], ref commandBuffer);
                }
            }

            AllocatePaths(runtimeSegment);

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            foreach (var data in crossroadData)
            {
                var trafficLightCrossroad = data.Key;
                if (trafficLightCrossroad == null) continue;

                var nodes = trafficLightCrossroad.TrafficNodes;

                for (int i = 0; i < nodes.Count; i++)
                {
                    GenerateInnerPaths(runtimeSegment, trafficLightCrossroad, nodes[i]);
                }
            }

            foreach (var data in crossroadData)
            {
                var trafficLightCrossroad = data.Key;
                if (trafficLightCrossroad == null) continue;

                var nodes = trafficLightCrossroad.TrafficNodes;

                for (int i = 0; i < nodes.Count; i++)
                {
                    GenerateConnectedPaths(runtimeSegment, trafficLightCrossroad, nodes[i]);
                }
            }

            commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            CreateAllPedNodes(runtimeSegment, ref commandBuffer);

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            ConnectPedNodes(runtimeSegment);

            segments.Add(runtimeSegment);
            OnSegmentAdded(runtimeSegment);
        }

        private void RestoreNodes(ref EntityCommandBuffer commandBuffer, TrafficNode sourceNode, TrafficNode remainNode)
        {
            if (!disabledNodes.Contains(sourceNode))
                return;

            if (sceneTrafficNodeBindingRight.TryGetValue(remainNode, out var rightEntities))
            {
                for (int j = 0; j < rightEntities.Count; j++)
                {
                    var entity = rightEntities[j];

                    if (!EntityManager.IsComponentEnabled<TrafficNodeComponent>(entity))
                    {
                        commandBuffer.SetComponentEnabled<TrafficNodeComponent>(entity, true);

                        var transform = EntityManager.GetComponentData<LocalTransform>(entity);

                        transform.Rotation = remainNode.GetNodeRotation(!remainNode.IsOneWay || !remainNode.IsEndOfOneWay ? 1 : -1);
                        commandBuffer.SetComponent(entity, transform);
                    }
                }
            }

            if (sceneTrafficNodeBindingLeft.TryGetValue(remainNode, out var leftEntities))
            {
                for (int j = 0; j < leftEntities.Count; j++)
                {
                    var entity = leftEntities[j];

                    if (!EntityManager.IsComponentEnabled<TrafficNodeComponent>(entity))
                    {
                        commandBuffer.SetComponentEnabled<TrafficNodeComponent>(entity, true);

                        var transform = EntityManager.GetComponentData<LocalTransform>(entity);

                        transform.Rotation = remainNode.GetNodeRotation(-1);
                        commandBuffer.SetComponent(entity, transform);
                    }
                }
            }

            disabledNodes.Remove(sourceNode);
        }

        public void AddRuntimeConnection(PedestrianNode sourceNode, PedestrianNode connectedNode)
        {
            var entity1 = TryToGetEntity(sourceNode);
            var entity2 = TryToGetEntity(connectedNode);

            if (entity1 != Entity.Null && entity2 != Entity.Null)
            {
                AddConnection(entity1, entity2);
                PedestrianNodeEntityUtils.AddConnection(EntityManager, entity1, entity2, sourceNode.PriorityWeight, connectedNode.PriorityWeight);
            }
        }

        public bool JoinRuntimeConnection(PedestrianNode node1, PedestrianNode node2, PedestrianNode joinNode)
        {
            if (!CanJoin(node1, node2, joinNode))
                return false;

            var entity1 = TryToGetEntity(node1);
            var entity2 = TryToGetEntity(node2);
            var joinEntity = TryToGetEntity(joinNode);

            JoinRuntimeConnection(entity1, entity2, joinEntity);
            return true;
        }

        public bool CanJoin(PedestrianNode node1, PedestrianNode node2, PedestrianNode joinNode)
        {
            var dist1 = Vector3.SqrMagnitude(node1.transform.position - joinNode.transform.position);
            var dist2 = Vector3.SqrMagnitude(node2.transform.position - joinNode.transform.position);

            const float minDistanceSq = 0.6f * 0.6f;

            return dist1 > minDistanceSq && dist2 > minDistanceSq;
        }

        public void JoinRuntimeConnection(Entity entity1, Entity entity2, Entity joinEntity)
        {
            EntityPair entityPair = default;
            bool registered = false;

            bool disconnected = false;

            if (PedestrianNodeEntityUtils.RemoveConnection(EntityManager, entity1, entity2))
                disconnected = true;

            if (entityInnerToConnectionLink.ContainsKey(entity1))
            {
                registered = true;
                entityPair = entityInnerToConnectionLink[entity1];
            }
            else if (entityInnerToConnectionLink.ContainsKey(entity2))
            {
                registered = true;
                entityPair = entityInnerToConnectionLink[entity2];
            }
            else
            {
                entityPair = new EntityPair(entity1, entity2);
                registered = connections.ContainsKey(entityPair);
            }

            if (!registered)
            {
                connections.Add(entityPair, new ConnectionData()
                {
                    Nodes = new List<Entity>(),
                    InitiallyConnected = disconnected
                });

                connections[entityPair].Nodes.Add(entity1);
                connections[entityPair].Nodes.Add(entity2);
            }

            if (!entityPairToConnectionLink.ContainsKey(entity1))
            {
                entityPairToConnectionLink.Add(entity1, new List<EntityPair>());
            }

            if (!entityPairToConnectionLink.ContainsKey(entity2))
            {
                entityPairToConnectionLink.Add(entity2, new List<EntityPair>());
            }

            if (!entityPairToConnectionLink[entity1].Contains(entityPair))
            {
                entityPairToConnectionLink[entity1].Add(entityPair);
            }

            if (!entityPairToConnectionLink[entity2].Contains(entityPair))
            {
                entityPairToConnectionLink[entity2].Add(entityPair);
            }

            var index = connections[entityPair].Nodes.IndexOf(entity1);

            if (index != -1)
            {
                connections[entityPair].Nodes.Insert(index + 1, joinEntity);
            }
            else
            {
                index = connections[entityPair].Nodes.IndexOf(entity2);
                connections[entityPair].Nodes.Insert(index, joinEntity);
            }
            if (!entityInnerToConnectionLink.ContainsKey(joinEntity))
                entityInnerToConnectionLink.Add(joinEntity, entityPair);

            var nodeSettings1 = EntityManager.GetComponentData<NodeSettingsComponent>(entity1);
            var nodeSettings2 = EntityManager.GetComponentData<NodeSettingsComponent>(entity2);
            var nodeSettingsJoin = EntityManager.GetComponentData<NodeSettingsComponent>(joinEntity);

            PedestrianNodeEntityUtils.AddConnection(EntityManager, entity1, joinEntity, nodeSettings1.Weight, nodeSettingsJoin.Weight);

            PedestrianNodeEntityUtils.AddConnection(EntityManager, joinEntity, entity2, nodeSettingsJoin.Weight, nodeSettings2.Weight);
        }

        public void RemoveRuntimeConnection(PedestrianNode sourceNode, PedestrianNode connectedNode)
        {
            var entity1 = TryToGetEntity(sourceNode);
            var entity2 = TryToGetEntity(connectedNode);

            PedestrianNodeEntityUtils.RemoveConnection(EntityManager, entity1, entity2);
        }

        [HideIf(nameof(simulationDisabled))]
        [Button]
        public void DisableSimulation()
        {
            if (simulationDisabled || !Application.isPlaying) return;

            simulationDisabled = true;

            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TrafficSpawnerSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PedestrianEntitySpawnerSystem>().Enabled = false;
            ClearTraffic();
        }


        [ShowIf(nameof(simulationDisabled))]
        [Button]
        public void ResumeSimulation()
        {
            if (!simulationDisabled || !Application.isPlaying) return;

            simulationDisabled = false;
            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TrafficSpawnerSystem>().Enabled = true;
            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PedestrianEntitySpawnerSystem>().Enabled = true;
        }

        public Entity TryToGetEntity(TrafficNode node, bool rightDirection, int laneIndex = 0)
        {
            if (rightDirection || node.IsOneWay)
            {
                if (sceneTrafficNodeBindingRight.TryGetValue(node, out List<Entity> list) && list.Count > laneIndex && laneIndex >= 0)
                {
                    return list[laneIndex];
                }
            }
            else
            {
                if (sceneTrafficNodeBindingLeft.TryGetValue(node, out List<Entity> list) && list.Count > laneIndex && laneIndex >= 0)
                {
                    return list[laneIndex];
                }
            }

            return Entity.Null;
        }

        public Entity TryToGetEntity(PedestrianNode node)
        {
            if (scenePedestrianNodeBinding.TryGetValue(node, out var entity))
            {
                return entity;
            }

            return Entity.Null;
        }

        public Entity TryToGetEntity(TrafficLightHandler value)
        {
            if (!value)
                return Entity.Null;

            if (lightEntities.TryGetValue(value, out var entity))
            {
                return entity;
            }

            return Entity.Null;
        }

        public void ClearGraph(bool includeTraffic = false)
        {
            if (!Init)
                return;

            if (includeTraffic)
            {
                ClearTraffic(true);
            }

            graph.ClearGraph();
            nodeResolver.Clear();

            EntityManager.DestroyEntity(allPedestrianNodeQuery);
            EntityManager.DestroyEntity(trafficNodeAvailableQuery);

            nodeMerge1.Clear();
            nodeMerge2.Clear();
            recalcNodes.Clear();
            crosswalkBinding.Clear();
            lightLinkedEntities.Clear();
            sceneTrafficNodeBindingRight.Clear();
            sceneTrafficNodeBindingLeft.Clear();
            scenePedestrianNodeBinding.Clear();
            lightEntities.Clear();
            tempNodes.Clear();
            tempNodes2.Clear();
            runtimeConnection.Clear();
            runtimeConnectionInversed.Clear();
            connections.Clear();
            crossroadIndex = 0;
            crosswalkIndex = 0;
        }

        public void ForceTrafficSpawn()
        {
            TraficSpawnInternal();
        }

        public void ForceTrafficSpawnAsync()
        {
            StartCoroutine(TrafficSpawnAsyncInternal(true));
        }

        public void ClearTraffic(bool immediate = false)
        {
            if (!immediate)
            {
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TrafficCleanerSystem>().Clear();
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PedestrianCleanerSystem>().Clear();
            }
            else
            {

                EntityManager.DestroyEntity(trafficQuery);
                EntityManager.DestroyEntity(pedestrianQuery);
            }
        }

        public void RegenerateGraphAsync(Action regenerateSceneAction, bool autoRegisterSegments = true, Action onComplete = null)
        {
            StartCoroutine(RegenerateGraphAsyncInternal(regenerateSceneAction, autoRegisterSegments, onComplete));
        }

        public TrafficNode TryToGetMergeNode(TrafficNode trafficNode)
        {
            if (nodeMerge1.ContainsKey(trafficNode))
            {
                return nodeMerge1[trafficNode].Node2;
            }

            if (nodeMerge2.ContainsKey(trafficNode))
            {
                return nodeMerge2[trafficNode].Node1;
            }

            return null;
        }

        public PedestrianNode TryToGetMergeNode(PedestrianNode pedestrianNode)
        {
            var connectedTrafficNode = pedestrianNode.ConnectedTrafficNode;

            if (connectedTrafficNode == null) return null;

            var index = connectedTrafficNode.GetCrosswalkNodeIndex(pedestrianNode);

            if (index == -1) return null;

            index = (index + 1) % 2;

            var oppositeTrafficNode = TryToGetMergeNode(connectedTrafficNode);

            if (oppositeTrafficNode)
            {
                var oppositeNode = oppositeTrafficNode.GetCrosswalkNode(index);

                if (oppositeNode && oppositeNode.gameObject.activeSelf)
                    return oppositeNode;
            }

            return null;
        }

        private IEnumerator RegenerateGraphAsyncInternal(Action regenerateSceneAction, bool autoRegisterSegments = true, Action onComplete = null)
        {
            DisableSimulation();

            yield return new WaitWhile(() => trafficQuery.CalculateEntityCount() > 0);

            ClearGraph();

            regenerateSceneAction();

            if (autoRegisterSegments)
            {
                var segments = ObjectUtils.FindObjectsOfType<RuntimeSegment>();

                foreach (var segment in segments)
                {
                    segment.PlaceSegment();
                }
            }

            ResumeSimulation();

            yield return TrafficSpawnAsyncInternal();

            onComplete?.Invoke();

            yield return null;
        }

        private IEnumerator TrafficSpawnAsyncInternal(bool userSpawn = false)
        {
            if (!userSpawn)
            {
                if (allTrafficNodeQuery.CalculateEntityCount() == 0)
                    yield break;

                yield return new WaitWhile(() => !graph.IsCreated() || graph.Count() == 0);
                yield return new WaitWhile(() => trafficNodeAvailableQuery.CalculateEntityCount() == 0);
            }
            else
            {
                yield return new WaitForSeconds(0.2f);
            }

            TraficSpawnInternal();
        }

        private static void TraficSpawnInternal()
        {
            if (!TrafficSpawnerSystem.IsInitialized) return;

            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TrafficSpawnerSystem>().InitialSpawn();
            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PedestrianEntitySpawnerSystem>().InitialSpawn();
        }

        private bool IsCustomConnectionLine(Entity entity, Entity connectedEntity, out EntityPair pair)
        {
            pair = default;

            if (entityInnerToConnectionLink.ContainsKey(entity) && entityInnerToConnectionLink.ContainsKey(connectedEntity))
            {
                pair = entityInnerToConnectionLink[entity];
                var pair2 = entityInnerToConnectionLink[connectedEntity];

                if (pair.Equals(pair2))
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator Remove(RuntimeSegment runtimeSegment)
        {
            yield return new WaitForEndOfFrame();
            EntityManager.CompleteAllTrackedJobs();
            graph.RemovePaths(runtimeSegment.TakenPaths);
            runtimeSegment.Dispose();
            removeRoutine = null;

            while (segmentToAdds.Count > 0)
            {
                var segment = segmentToAdds.Dequeue();
                AddSegment(segment);
            }

            PostRemoveAction();
            segments.Remove(runtimeSegment);
        }

        private IEnumerator WaitForInit()
        {
            yield return InitRoutine();

            var count = initQueue.Count;

            for (int i = 0; i < count; i++)
            {
                var segment = initQueue.Dequeue();
                AddSegment(segment);
            }
        }

        private void DestroyLights(TrafficLightCrossroad trafficLightCrossroad, ref NativeHashSet<Entity> destroyEntities)
        {
            if (!trafficLightCrossroad.HasLights) return;

            var trafficLightHandlers = trafficLightCrossroad.TrafficLightHandlers;

            foreach (var item in trafficLightHandlers)
            {
                var lightEntity = lightEntities[item.Value];
                lightEntities.Remove(item.Value);
                destroyEntities.Add(lightEntity);

                if (!lightLinkedEntities.TryGetValue(lightEntity, out var list))
                    continue;

                while (list.Count > 0)
                {
                    Entity entity = list[0];

                    if (EntityManager.HasComponent<TrafficNodeComponent>(entity))
                    {
                        var nodeComponent = EntityManager.GetComponentData<TrafficNodeComponent>(entity);
                        nodeComponent.LightEntity = Entity.Null;
                        nodeComponent.CrossRoadIndex = -1;
                        EntityManager.SetComponentData(entity, nodeComponent);
                    }

                    if (EntityManager.HasComponent<NodeLightSettingsComponent>(entity))
                    {
                        var nodeComponent = EntityManager.GetComponentData<NodeLightSettingsComponent>(entity);
                        nodeComponent.LightEntity = Entity.Null;
                        EntityManager.SetComponentData(entity, nodeComponent);
                    }

                    list.RemoveAt(0);
                }

                lightLinkedEntities.Remove(lightEntity);
            }
        }

        private void CreateLight(TrafficLightCrossroad trafficLightCrossroad, List<TrafficLightObjectAuthoring> lights, ref EntityCommandBuffer commandBuffer)
        {
            var handlers = trafficLightCrossroad.TrafficLightHandlers;

            trafficLightCrossroad.TryToGenerateID(true);

            var crossroadID = trafficLightCrossroad.UniqueId;

            foreach (var handler in handlers)
            {
                var lightStates = handler.Value.LightStates;

                var lightEntity = CreateLightEntity(ref commandBuffer, lightStates, trafficLightCrossroad.CrossroadIndex, crossroadID, handler.Key);

                AddLightEntity(handler.Value, lightEntity);
            }

            for (int i = 0; i < lights?.Count; i++)
            {
                lights[i].TrafficLightObject.ConnectedId = crossroadID;
                lights[i].RegisterFrames();
            }
        }

        private void CreateTrafficNodeEntities(RuntimeSegment runtimeSegment, TrafficLightCrossroad trafficLightCrossroad, TrafficNode node, ref EntityCommandBuffer entityCommandBuffer)
        {
            TrafficNode connectedNode = TryToGetConnectedNode(runtimeSegment, node);
            ConnectOrMergeInternal(ref entityCommandBuffer, trafficLightCrossroad, node, connectedNode);
        }

        private TrafficNode TryToGetConnectedNode(RuntimeSegment runtimeSegment, TrafficNode node)
        {
            if (runtimeSegment.TrafficCasterNode(node))
            {
                var settings = new TrafficNode.AutoConnectionSettings()
                {
                    MaxConnectionLength = castDistance,
                    CloseRaycast = true
                };

                var flipConnection = node.IsOneWay && !node.IsEndOfOneWay;
                return node.CastConnectedNode(settings, flipConnection);
            }

            return null;
        }

        private void ConnectOrMergeInternal(ref EntityCommandBuffer entityCommandBuffer, TrafficLightCrossroad trafficLightCrossroad, TrafficNode node, TrafficNode connectedNode)
        {
            var lightEntity = TryToGetEntity(node.TrafficLightHandler);

            if (connectedNode != null)
            {
                var owner = GetOwnerSegment(connectedNode);

                if (owner != null && owner.Placed && crosswalkBinding.ContainsKey(connectedNode))
                {
                    var type = MergeType.Merged;

                    if (connectionType == ConnectionType.ConnectOrMerge)
                    {
                        var distance = Vector3.Distance(node.transform.position, connectedNode.transform.position);
                        type = distance < mergeDistance ? MergeType.Merged : MergeType.Connected;
                    }

                    var bindingData = new BindingData()
                    {
                        Node1 = connectedNode,
                        Node2 = node,
                        ConnectionType = type,
                    };

                    if (!nodeMerge1.ContainsKey(connectedNode))
                        nodeMerge1.Add(connectedNode, bindingData);

                    if (!nodeMerge2.ContainsKey(node))
                        nodeMerge2.Add(node, bindingData);
                }
            }

            var mergeType = GetMergeType(node);
            var differentMerge = false;

            if (mergeType == MergeType.Merged)
            {
                differentMerge = DifferentNodes(node, connectedNode);

                if (!differentMerge)
                {
                    if (partialMergeNodes.ContainsKey(connectedNode))
                    {
                        partialMergeNodes.Remove(connectedNode);
                    }
                }
            }

            IterateAllLanes(node, ref entityCommandBuffer, (commandBuffer, laneIndex, external) =>
            {
                var side = !external ? 1 : -1;

                Entity entity = default;

                if (mergeType != MergeType.Merged)
                {
                    entity = CreateTrafficNodeEntity(ref commandBuffer, trafficLightCrossroad, node, laneIndex, external, lightEntity);

                    if (!crosswalkBinding.ContainsKey(node))
                        crosswalkBinding.Add(node, crosswalkIndex++);
                }
                else
                {
                    if (differentMerge)
                    {
                        if (!connectedNode.IsOneWay)
                        {
                            var laneCount = connectedNode.GetLaneCount(!external);

                            var currentExternal = external;

                            var mergeLaneIndex = node.TotalLaneCount - laneIndex - 1;

                            if (mergeLaneIndex >= laneCount)
                            {
                                mergeLaneIndex -= laneCount;
                                currentExternal = !currentExternal;
                            }

                            entity = TryToGetEntity(connectedNode, currentExternal, mergeLaneIndex);
                        }
                        else
                        {
                            var mergeLaneIndex = laneIndex;
                            var currentExternal = !external;

                            if (connectedNode.IsEndOfOneWay)
                            {
                                currentExternal = !currentExternal;
                            }

                            var laneCount = node.GetLaneCount(external);

                            if (!currentExternal)
                            {
                                mergeLaneIndex += laneCount;
                            }

                            var laneDiff = node.TotalLaneCount - connectedNode.TotalLaneCount;

                            mergeLaneIndex -= laneDiff;

                            entity = TryToGetEntity(connectedNode, currentExternal, mergeLaneIndex);
                        }

                        if (entity == Entity.Null)
                        {
                            entity = CreateTrafficNodeEntity(ref commandBuffer, trafficLightCrossroad, node, laneIndex, external, lightEntity);

                            if (!crosswalkBinding.ContainsKey(node))
                                crosswalkBinding.Add(node, crosswalkBinding[connectedNode]);

                            if (!partialMergeNodes.ContainsKey(node))
                            {
                                partialMergeNodes.Add(node, new List<Entity>());
                            }

                            partialMergeNodes[node].Add(entity);

                            // Disable node due to no connection with one-way
                            if (connectedNode.IsEndOfOneWay && external || !connectedNode.IsEndOfOneWay && !external)
                            {
                                if (!disabledNodes.Contains(connectedNode))
                                    disabledNodes.Add(connectedNode);

                                commandBuffer.SetComponentEnabled<TrafficNodeComponent>(entity, false);
                            }
                        }
                        else
                        {
                            if (!HasConnectedPath(node, connectedNode, laneIndex, external))
                            {
                                if (!disabledNodes.Contains(connectedNode))
                                    disabledNodes.Add(connectedNode);

                                commandBuffer.SetComponentEnabled<TrafficNodeComponent>(entity, false);
                            }
                        }
                    }
                    else
                    {
                        entity = TryToGetEntity(connectedNode, external, laneIndex);
                    }

                    var crossWalkIndex = crosswalkBinding[connectedNode];

                    if (lightEntity != Entity.Null && node.HasLight(side))
                    {
                        if (entity != Entity.Null)
                        {
                            AddLightLink(lightEntity, entity);

                            commandBuffer.SetComponent(entity, new TrafficNodeComponent()
                            {
                                CrossRoadIndex = trafficLightCrossroad.CrossroadIndex,
                                LightEntity = lightEntity
                            });
                        }
                    }

                    if (!crosswalkBinding.ContainsKey(node))
                        crosswalkBinding.Add(node, crossWalkIndex);
                }

                if (side == 1 || node.IsOneWay)
                {
                    if (!sceneTrafficNodeBindingRight.ContainsKey(node))
                    {
                        sceneTrafficNodeBindingRight.Add(node, new List<Entity>());
                    }

                    sceneTrafficNodeBindingRight[node].Add(entity);
                }
                else
                {
                    if (!sceneTrafficNodeBindingLeft.ContainsKey(node))
                    {
                        sceneTrafficNodeBindingLeft.Add(node, new List<Entity>());
                    }

                    sceneTrafficNodeBindingLeft[node].Add(entity);
                }
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Entity CreateTrafficNodeEntity(ref EntityCommandBuffer commandBuffer, TrafficLightCrossroad trafficLightCrossroad, TrafficNode node, int laneIndex, bool external, Entity lightEntity)
        {
            var side = !external ? 1 : -1;

            Entity entity;
            var pos = node.GetLanePosition(laneIndex, external);
            var rot = node.GetNodeRotation(side);

            entity = EntityManager.CreateEntity(
                typeof(TrafficNodeComponent),
                typeof(TrafficNodeAvailableComponent),
                typeof(TrafficNodeSettingsComponent),
                typeof(TrafficNodeCapacityComponent),
                typeof(TrafficNodeAvailableTag),
                typeof(LocalToWorld),
                typeof(LocalTransform),
                typeof(PathConnectionElement));

            if (addCullingComponents)
                commandBuffer.AddComponent(entity, CullComponentsExtension.GetComponentSet());

            var currentLightEntity = Entity.Null;

            if (node.HasLight(side))
                currentLightEntity = lightEntity;

            commandBuffer.SetComponent(entity, new TrafficNodeComponent()
            {
                CrossRoadIndex = trafficLightCrossroad.CrossroadIndex,
                LightEntity = currentLightEntity
            });

            var isAvailableForSpawnTarget = true;
            int capacity = -1;

            if (IsLinkedTrafficNode(node))
            {
                commandBuffer.AddBuffer<ConnectedPedestrianNodeElement>(entity);
                capacity = 1;
                isAvailableForSpawnTarget = false;
            }

            commandBuffer.SetComponent(entity, new TrafficNodeCapacityComponent()
            {
                Capacity = capacity
            });

            commandBuffer.SetComponent(entity, new TrafficNodeSettingsComponent()
            {
                TrafficNodeType = node.TrafficNodeType,
                LaneDirectionSide = side,
                LaneIndex = laneIndex,
                ChanceToSpawn = node.ChanceToSpawn,
                Weight = node.Weight,
                CustomAchieveDistance = node.CustomAchieveDistance,
                HasCrosswalk = node.HasCrosswalk,
                AllowedRouteRandomizeSpawning = true,
                IsAvailableForSpawn = true,
                IsAvailableForSpawnTarget = isAvailableForSpawnTarget,
            });

            commandBuffer.SetComponent(entity, LocalTransform.FromPositionRotation(pos, rot));

            commandBuffer.AddBuffer<PathConnectionElement>(entity);
            return entity;
        }

        private bool IsLinkedTrafficNode(TrafficNode node) => RuntimeSegment.IsLinkedTrafficNode(node);

        private void AllocatePaths(RuntimeSegment runtimeSegment)
        {
            runtimeSegment.TakenPaths = graph.GetEmptyPaths(runtimeSegment.paths.Length, Allocator.Persistent);
        }

        private void GenerateInnerPaths(RuntimeSegment runtimeSegment, TrafficLightCrossroad crossroad, TrafficNode node)
        {
            bool differentMerge = false;
            var mergeNode = TryToGetMergeNode(node);

            if (mergeNode && DifferentNodes(node, mergeNode))
            {
                differentMerge = true;
            }

            node.IterateAllPaths((path, sourceLaneIndex) =>
            {
                var external = path.IsExternal;
                var sourceEntity = TryToGetEntity(node, !external, sourceLaneIndex);

                if (sourceEntity == Entity.Null)
                {
                    if (differentMerge)
                    {
                        if (mergeNode.IsOneWay != node.IsOneWay)
                        {
                            if (node.IsOneWay)
                            {
                                int laneIndex = sourceLaneIndex - mergeNode.GetLaneCount(false);
                                sourceEntity = TryToGetEntity(mergeNode, external, laneIndex);
                            }
                            else
                            {
                                int laneIndex = mergeNode.GetLaneCount(true) - sourceLaneIndex - 1;
                                sourceEntity = TryToGetEntity(mergeNode, external, laneIndex);
                            }
                        }
                    }
                }

                var pathBuffer = EntityManager.GetBuffer<PathConnectionElement>(sourceEntity);
                var connectedEntity = Entity.Null;

                var connectedPathIndex = -1;

                var hasConnectedPath = true;

                if (path.PathConnectionType != PathConnectionType.PathPoint)
                {
                    connectedEntity = TryToGetEntity(path.ConnectedTrafficNode, external, path.ConnectedLaneIndex);

                    var sourceNode = path.SourceTrafficNode;
                    var connectedNode = path.ConnectedTrafficNode;
                    var sourceMergeNode = TryToGetMergeNode(sourceNode);
                    var connectedMergeNode = TryToGetMergeNode(connectedNode);

                    if (connectedMergeNode != null)
                    {
                        hasConnectedPath = HasConnectedPath(connectedNode, connectedMergeNode, path.ConnectedLaneIndex, true);
                    }

                    if (!hasConnectedPath)
                    {
                        if (!disabledPathData.ContainsKey(connectedNode))
                        {
                            disabledPathData.Add(connectedNode, new List<Path>());
                        }

                        disabledPathData[connectedNode].Add(path);
                    }
                }
                else
                {
                    var connectedPath = path.ConnectedPath;

                    if (connectedPath != null)
                    {
                        connectedPathIndex = GetGlobalPathIndex(runtimeSegment, connectedPath);
                        connectedEntity = TryToGetEntity(connectedPath.SourceTrafficNode, !external, connectedPath.SourceLaneIndex);
                    }
                    else
                    {
                        hasConnectedPath = false;
                    }
                }

                var sourcePathIndex = GetGlobalPathIndex(runtimeSegment, path);

                pathBuffer.Add(new PathConnectionElement()
                {
                    ConnectedHash = -1,
                    ConnectedSubHash = -1,
                    ConnectedSubNodeEntity = connectedEntity,
                    ConnectedNodeEntity = connectedEntity,
                    StartLocalNodeIndex = 0,
                    GlobalPathIndex = sourcePathIndex,
                });

                var options = TrafficNodeBaker.GetOptions(path, true);

                var pathData = new PathGraphSystem.PathData()
                {
                    PathIndex = sourcePathIndex,
                    ConnectedPathIndex = connectedPathIndex,
                    SourceLaneIndex = path.SourceLaneIndex,
                    PathLength = path.PathLength,
                    Priority = path.Priority,
                    Options = options,
                    PathCurveType = path.PathCurveType,
                    PathRoadType = path.PathRoadType,
                    PathConnectionType = path.PathConnectionType,
                    TrafficGroup = hasConnectedPath ? path.TrafficGroup : default,
                };

#if UNITY_EDITOR
                if (!hasConnectedPath)
                {
                    path.HighlightColor = Color.red;
                    path.HasPathCustomColor = true;
                }
#endif

                graph.AddPath(pathData);

                if (path.Intersects.Count > 0)
                {
                    graph.InitIntersection(sourcePathIndex, path.Intersects.Count);

                    for (int i = 0; i < path.Intersects.Count; i++)
                    {
                        var intersect = path.Intersects[i];

                        graph.AddIntersection(sourcePathIndex, new IntersectPathInfo()
                        {
                            IntersectedPathIndex = GetGlobalPathIndex(runtimeSegment, intersect.IntersectedPath),
                            IntersectPosition = crossroad.transform.TransformPoint(intersect.IntersectPoint),
                            LocalNodeIndex = (byte)intersect.LocalNodeIndex
                        });
                    }
                }

                if (path.WayPoints.Count > 0)
                {
                    graph.InitRouteNodes(sourcePathIndex, path.WayPoints.Count);

                    for (int i = 0; i < path.WayPoints.Count; i++)
                    {
                        PathNode waypoint = path.WayPoints[i];

                        graph.AddRouteNode(sourcePathIndex, new RouteNodeData()
                        {
                            SpeedLimit = waypoint.SpeedLimitMs,
                            ForwardNodeDirection = !waypoint.BackwardDirection,
                            Position = waypoint.transform.position,
                            TrafficGroup = waypoint.CustomGroupType
                        });
                    }
                }

                var neighbourPaths = runtimeSegment.GetNeighbourPaths(path);

                if (neighbourPaths?.Count > 0)
                {
                    graph.InitNeighbourPaths(sourcePathIndex, neighbourPaths.Count);

                    for (int i = 0; i < neighbourPaths.Count; i++)
                    {
                        Path neighbourPath = neighbourPaths[i];

                        var neighbourPathIndex = GetGlobalPathIndex(runtimeSegment, neighbourPath);

                        graph.AddNeighbourPath(sourcePathIndex, neighbourPathIndex);
                    }
                }

                var parallelPaths = runtimeSegment.GetParallelPaths(path);

                if (parallelPaths?.Count > 0)
                {
                    graph.InitParallelPaths(sourcePathIndex, parallelPaths.Count);

                    for (int i = 0; i < parallelPaths.Count; i++)
                    {
                        Path parallelPath = parallelPaths[i];

                        var parallelPathPathIndex = GetGlobalPathIndex(runtimeSegment, parallelPath);

                        graph.AddParallelPath(sourcePathIndex, parallelPathPathIndex);
                    }
                }

                nodeResolver.AddPath(sourcePathIndex, sourceEntity, connectedEntity);

            }, true);

            if (mergeNode != null)
            {
                var owner = GetOwnerSegment(mergeNode);

                if (differentMerge)
                {
                    // Should be replaced by iterating lanes & checking connection, for now hard code temporary solution for 2x2 & 2x1 lanes
                    if (node.TotalLaneCount == mergeNode.TotalLaneCount)
                    {
                        if (!mergeNode.IsOneWay || mergeNode.IsEndOfOneWay)
                        {
                            var paths = owner.GetAllInnerPathsConnectedToNode(mergeNode);

                            if (paths?.Count > 0)
                            {
                                if (!disabledPathData.ContainsKey(mergeNode))
                                {
                                    disabledPathData.Add(mergeNode, new List<Path>());
                                }

                                for (var i = 0; i < paths.Count; i++)
                                {
                                    var connected = HasConnectedPath(mergeNode, node, paths[i].ConnectedLaneIndex, true);

                                    SwitchPathEnabled(owner, paths[i], connected);

                                    if (!connected)
                                        disabledPathData[mergeNode].Add(paths[i]);
                                }
                            }
                        }
                        else
                        {
                            // In opposite direction nodes
                            disabledNodes.Add(mergeNode);

                            var entities = sceneTrafficNodeBindingRight[mergeNode];

                            for (int i = 0; i < node.GetLaneCount(false); i++)
                            {
                                var entity = entities[i];
                                EntityManager.SetComponentEnabled<TrafficNodeComponent>(entity, false);
                            }
                        }
                    }
                    else
                    {
                        if (!mergeNode.IsOneWay && node.IsEndOfOneWay)
                        {
                            var paths = owner.GetAllInnerPathsConnectedToNode(mergeNode);

                            if (paths?.Count > 0)
                            {
                                if (!disabledPathData.ContainsKey(mergeNode))
                                {
                                    disabledPathData.Add(mergeNode, new List<Path>());
                                }

                                for (var i = 0; i < paths.Count; i++)
                                {
                                    var connected = HasConnectedPath(mergeNode, node, paths[i].ConnectedLaneIndex, true);

                                    SwitchPathEnabled(owner, paths[i], connected);

                                    if (!connected)
                                        disabledPathData[mergeNode].Add(paths[i]);
                                }
                            }
                        }
                        else
                        {
                            if (!mergeNode.IsEndOfOneWay)
                            {
                                // In opposite direction nodes
                                disabledNodes.Add(mergeNode);

                                var entities = sceneTrafficNodeBindingRight[mergeNode];

                                for (int i = 0; i < node.GetLaneCount(false); i++)
                                {
                                    var entity = entities[i];
                                    EntityManager.SetComponentEnabled<TrafficNodeComponent>(entity, false);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (disabledPathData.TryGetValue(mergeNode, out var paths))
                    {
                        for (var i = 0; i < paths.Count; i++)
                        {
                            SwitchPathEnabled(owner, paths[i], true);
                        }

                        disabledPathData.Remove(mergeNode);
                    }
                }
            }
        }

        private void SwitchPathEnabled(RuntimeSegment segment, Path path, bool isEnabled)
        {
            var pathIndex = GetGlobalPathIndex(segment, path);

#if UNITY_EDITOR
            path.HasPathCustomColor = !isEnabled;
            path.HighlightColor = isEnabled ? Color.white : Color.red;
#endif

            SwitchPathEnabled(pathIndex, isEnabled);
        }

        private void SwitchPathEnabled(int pathIndex, bool isEnabled)
        {
            graph.SetGroup(pathIndex, isEnabled ? TrafficGroupType.Default : default);
        }

        private bool DifferentNodes(TrafficNode srcNode, TrafficNode dstNode)
        {
            if (!srcNode.IsOneWay && !dstNode.IsOneWay)
            {
                return srcNode.LaneCount != dstNode.LaneCount;
            }

            if (srcNode.IsOneWay != dstNode.IsOneWay)
            {
                return true;
            }

            if (srcNode.IsEndOfOneWay != !dstNode.IsEndOfOneWay || srcNode.TotalLaneCount != dstNode.TotalLaneCount)
            {
                return true;
            }

            return false;
        }

        private bool HasConnectedPath(TrafficNode srcNode, TrafficNode dstNode, int laneIndex, bool byEnd)
        {
            if (!srcNode.IsOneWay && !dstNode.IsOneWay)
            {
                return laneIndex < dstNode.GetLaneCount(!byEnd);
            }

            if (srcNode.IsOneWay != dstNode.IsOneWay)
            {
                if (dstNode.IsOneWay)
                {
                    // All lanes in opposite direction
                    if (dstNode.IsEndOfOneWay && byEnd)
                    {
                        return false;
                    }
                    else
                    {
                        var laneDiff = srcNode.TotalLaneCount - dstNode.TotalLaneCount;

                        return laneIndex + srcNode.GetLaneCount(!byEnd) - laneDiff < dstNode.TotalLaneCount;
                    }
                }
                else
                {
                    if (srcNode.IsEndOfOneWay && !byEnd || !srcNode.IsEndOfOneWay && byEnd)
                    {
                        return laneIndex < dstNode.GetLaneCount(!byEnd);
                    }
                    else
                    {
                        return srcNode.TotalLaneCount - laneIndex - 1 < dstNode.GetLaneCount(!byEnd);
                    }
                }
            }
            else
            {
                return srcNode.IsEndOfOneWay == !dstNode.IsEndOfOneWay && laneIndex < dstNode.TotalLaneCount;
            }
        }

        private void GenerateConnectedPaths(RuntimeSegment runtimeSegment, TrafficLightCrossroad crossroad, TrafficNode node)
        {
            node.IterateAllPaths((path, sourceLaneIndex) =>
            {
                if (!path.HasConnection) return;

                var external = path.IsExternal;
                var sourceEntity = TryToGetEntity(node, !external, sourceLaneIndex);

                var connectedTrafficNode = path.ConnectedTrafficNode;

                var connectedEntity = TryToGetEntity(connectedTrafficNode, external, path.ConnectedLaneIndex);

                var sourcePathIndex = GetGlobalPathIndex(runtimeSegment, path);

                if (connectedEntity == Entity.Null)
                {
                    var owner = GetOwnerSegment(path.ConnectedTrafficNode);

                    if (!DifferentNodes(path.ConnectedTrafficNode, connectedTrafficNode))
                    {
                        Debug.Log($"RuntimeSegment {owner.name}. Crossroad {connectedTrafficNode.CrossroadName} Broken traffic node {connectedTrafficNode.name} found. Ensure that all segments in this segment are correctly located.");

#if UNITY_EDITOR
                        brokenNodes.Add(connectedTrafficNode);
#endif
                    }

                    return;
                }

                var connectedPathBuffer = EntityManager.GetBuffer<PathConnectionElement>(connectedEntity, true);

                for (int i = 0; i < connectedPathBuffer.Length; i++)
                {
                    var connectedPathIndex = connectedPathBuffer[i].GlobalPathIndex;
                    graph.AddConnectedByPath(connectedPathIndex, sourcePathIndex);
                    graph.AddConnectedPath(sourcePathIndex, connectedPathIndex);
                }

                if (nodeMerge2.ContainsKey(node))
                {
                    var connectedNode = nodeMerge2[node].Node1;
                    if (connectedNode != null)
                    {
                        var connectedSegment = GetOwnerSegment(connectedNode);
                        connectedSegment.GetInnerPathsConnectedToNodeNonAlloc(connectedNode, sourceLaneIndex, ref tempConnectedPaths);

                        for (int i = 0; i < tempConnectedPaths?.Count; i++)
                        {
                            Path connectedPath = tempConnectedPaths[i];
                            var connectedPathIndex = GetGlobalPathIndex(connectedSegment, connectedPath);

                            graph.AddConnectedByPath(sourcePathIndex, connectedPathIndex);
                            graph.AddConnectedPath(connectedPathIndex, sourcePathIndex);
                        }
                    }
                }

            }, true);
        }

        private void RemovePath(int currentPathIndex)
        {
            nodeResolver.RemovePath(currentPathIndex);
        }

        private int GetGlobalPathIndex(RuntimeSegment runtimeSegment, Path path)
        {
            return runtimeSegment.GetGlobalPathIndex(path);
        }

        private void CreateAllPedNodes(RuntimeSegment runtimeSegment, ref EntityCommandBuffer commandBuffer)
        {
            for (int i = 0; i < runtimeSegment.pedestrianNodes.Length; i++)
            {
                var node = runtimeSegment.pedestrianNodes[i];
                CreatePedNode(runtimeSegment, node, ref commandBuffer);
            }
        }

        private RuntimeSegment GetOwnerSegment(TrafficNode node)
        {
            if (node.TryGetComponent<NodeOwnerComponent>(out var owner))
            {
                if (!owner.OwnerSegment)
                {
                    Debug.LogError($"RuntimeRoadManager. Crossroad {node.TrafficLightCrossroad.name} TrafficNode {node.name} <b>NodeOwnerComponent</b> owner reference is null.");
                }

                return owner.OwnerSegment;
            }

            Debug.LogError($"RuntimeRoadManager. Crossroad {node.TrafficLightCrossroad.name} TrafficNode {node.name} doesn't have <b>NodeOwnerComponent</b>. Rebake your prefabs");

            return null;
        }

        private RuntimeSegment GetOwnerSegment(PedestrianNode node)
        {
            if (node.TryGetComponent<NodeOwnerComponent>(out var owner))
            {
                return owner.OwnerSegment;
            }

            string parentName = string.Empty;

            var parent = node.GetComponentInParent<RuntimeSegment>();

            parentName = parent ? parent.name : "NaN";

            Debug.LogError($"RuntimeRoadManager. Crossroad {parentName} PedestrianNode {node.name} doesn't have <b>NodeOwnerComponent</b>. Rebake your prefabs");

            return null;
        }

        private void ConnectPedNodes(RuntimeSegment runtimeSegment)
        {
            for (int i = 0; i < runtimeSegment.pedestrianNodes.Length; i++)
            {
                var pedestrianNode = runtimeSegment.pedestrianNodes[i];
                var entity = TryToGetEntity(pedestrianNode);

                if (entity == Entity.Null)
                {
                    var crossroad = pedestrianNode.GetComponentInParent<RoadSegment>();

                    var name = crossroad ? crossroad.name : "Unknown";

                    Debug.Log($"RuntimeSegment {runtimeSegment.name} Crossroad {name} PedestrianNode {pedestrianNode.name} not created pedestrian node entity found. Ensure that all segments in this segment are correctly located.");
                    continue;
                }

                var buffer = EntityManager.GetBuffer<NodeConnectionDataElement>(entity);
                buffer.EnsureCapacity(pedestrianNode.DefaultConnectedPedestrianNodes.Count);

                bool emptyBuffer = buffer.Length == 0;

                var settings = EntityManager.GetComponentData<NodeSettingsComponent>(entity);

                for (int j = 0; j < pedestrianNode.DefaultConnectedPedestrianNodes.Count; j++)
                {
                    var connectedNode = pedestrianNode.DefaultConnectedPedestrianNodes[j];

                    if (connectedNode == null || !connectedNode.gameObject.activeInHierarchy)
                        continue;

                    if (!connectedNode.CheckConnection(pedestrianNode))
                        continue;

                    if (!connectedNode.HasConnection(pedestrianNode))
                        continue;

                    var connectedEntity = TryToGetEntity(connectedNode);

                    bool exist = false;

                    if (!emptyBuffer)
                    {
                        exist = buffer.IsConnectedTo(connectedEntity);
                    }

                    if (!exist)
                    {
                        settings.SumWeight += connectedNode.PriorityWeight;

                        buffer.Add(new NodeConnectionDataElement()
                        {
                            SumWeight = settings.SumWeight,
                            ConnectedEntity = connectedEntity
                        });
                    }
                }

                EntityManager.SetComponentData(entity, settings);
            }

            for (int i = 0; i < runtimeSegment.connectionEdges?.Length; i++)
            {
                var edge = runtimeSegment.connectionEdges[i];
                var node1 = edge.Node1;
                var node2 = edge.Node2;
                var pos1 = node1.transform.position;
                var pos2 = node2.transform.position;
                var castDir = Vector3.Normalize(pos2 - pos1);

                // To avoid self casting
                const float radius = 0.6f;

                pos1 += castDir * radius;
                pos2 -= castDir * radius;

                var entity1 = TryToGetEntity(node1);
                var entity2 = TryToGetEntity(node2);

                var distance = Vector3.Distance(pos1, pos2);

                var count = Physics.RaycastNonAlloc(pos1, castDir, hitsBuffer, distance, 1 << LayerMask.NameToLayer(ProjectConstants.PEDESTRIAN_NODE_LAYER_NAME), QueryTriggerInteraction.Collide);

                tempNodes.Clear();

                for (int j = 0; j < count; j++)
                {
                    var connectedNode = hitsBuffer[j];

                    if (connectedNode.collider.gameObject == node1.gameObject || connectedNode.collider.gameObject == node2.gameObject)
                        continue;

                    var node = connectedNode.collider.GetComponent<PedestrianNode>();

                    var hitNodeEntity = TryToGetEntity(node);

                    if (hitNodeEntity != entity1 && hitNodeEntity != entity2 && hitNodeEntity != Entity.Null)
                        tempNodes.Add(node);
                }

                if (tempNodes.Count > 0)
                {
                    PedestrianNode sourceNode = node1;

                    for (int j = 0; j < tempNodes.Count; j++)
                    {
                        JoinRuntimeConnection(sourceNode, node2, tempNodes[j]);
                        sourceNode = tempNodes[j];
                    }
                }
            }

            for (int i = 0; i < runtimeSegment.nodeCasters?.Length; i++)
            {
                var caster = runtimeSegment.nodeCasters[i];

                tempNodes.Clear();
                tempNodes2.Clear();
                caster.Cast(tempNodes);

                if (tempNodes.Count == 0)
                    continue;

                for (int index1 = 0; index1 < tempNodes.Count; index1++)
                {
                    var node1 = tempNodes[index1];

                    for (int index2 = 0; index2 < tempNodes.Count; index2++)
                    {
                        var node2 = tempNodes[index2];

                        if (node1 == node2) continue;

                        if (tempNodes2.Contains(node1) && tempNodes2.Contains(node2)) continue;

                        var nodeEntity1 = TryToGetEntity(node1);
                        var nodeEntity2 = TryToGetEntity(node2);

                        var connected = PedestrianNodeEntityUtils.IsConnectedTo(EntityManager, nodeEntity1, nodeEntity2);

                        if (connected)
                        {
                            tempNodes2.TryToAdd(node1);
                            tempNodes2.TryToAdd(node2);

                            JoinRuntimeConnection(node1, node2, caster.OwnerNode);
                        }
                    }
                }

                if (tempNodes2.Count > 0)
                {
                    foreach (var node in tempNodes2)
                    {
                        tempNodes.Remove(node);
                    }
                }

                for (int j = 0; j < tempNodes.Count; j++)
                {
                    AddRuntimeConnection(tempNodes[j], caster.OwnerNode);
                }
            }
        }

        private void AddConnection(Entity sourceNode, Entity connectedNode)
        {
            if (!runtimeConnection.ContainsKey(sourceNode))
            {
                runtimeConnection.Add(sourceNode, new List<Entity>());
            }

            runtimeConnection[sourceNode].Add(connectedNode);

            if (!runtimeConnectionInversed.ContainsKey(connectedNode))
            {
                runtimeConnectionInversed.Add(connectedNode, new List<Entity>());
            }

            runtimeConnectionInversed[connectedNode].Add(sourceNode);
        }

        private bool RemoveRuntimeConnection(Entity entity, bool sourceDestroyedNode = false)
        {
            if (!entityInnerToConnectionLink.ContainsKey(entity) && !entityPairToConnectionLink.ContainsKey(entity))
            {
                Dictionary<Entity, List<Entity>> data1 = null;
                Dictionary<Entity, List<Entity>> data2 = null;

                if (runtimeConnection.ContainsKey(entity))
                {
                    data1 = runtimeConnection;
                    data2 = runtimeConnectionInversed;
                }

                if (runtimeConnectionInversed.ContainsKey(entity))
                {
                    data1 = runtimeConnectionInversed;
                    data2 = runtimeConnection;
                }

                if (data1 == null)
                    return false;

                var list = data1[entity];

                for (int i = 0; i < list.Count; i++)
                {
                    var connectedEntity = list[i];

                    PedestrianNodeEntityUtils.RemoveConnectedEntity(EntityManager, connectedEntity, entity);

                    if (!sourceDestroyedNode)
                    {
                        PedestrianNodeEntityUtils.RemoveConnectedEntity(EntityManager, entity, connectedEntity);
                    }

                    data2[connectedEntity].TryToRemove(entity);

                    if (data2[connectedEntity].Count == 0)
                    {
                        data2.Remove(connectedEntity);
                    }
                }

                data1.Remove(entity);
                return true;
            }
            else
            {
                var isPair = entityPairToConnectionLink.ContainsKey(entity);

                if (!isPair)
                {
                    var pair = entityInnerToConnectionLink[entity];

                    var list = connections[pair].Nodes;

                    var index = list.IndexOf(entity);

                    var previousEntity = list[index - 1];

                    var nextEntity = list[index + 1];

                    bool reconnect = true;

                    list.TryToRemove(entity);

                    entityInnerToConnectionLink.Remove(entity);

                    if (list.Count <= 2)
                    {
                        reconnect = connections[pair].InitiallyConnected;
                        RemovePair(pair);

                        if (entityPairToConnectionLink.ContainsKey(pair.A))
                        {
                            var pairs = entityPairToConnectionLink[pair.A];
                            pairs.TryToRemove(pair);

                            if (pairs.Count == 0)
                            {
                                entityPairToConnectionLink.Remove(pair.A);
                            }
                        }

                        if (entityPairToConnectionLink.ContainsKey(pair.B))
                        {
                            var pairs = entityPairToConnectionLink[pair.B];
                            pairs.TryToRemove(pair);

                            if (pairs.Count == 0)
                            {
                                entityPairToConnectionLink.Remove(pair.B);
                            }
                        }
                    }

                    PedestrianNodeEntityUtils.UnjoinEntityAndReconnect(EntityManager, previousEntity, nextEntity, entity, reconnect);
                }
                else
                {
                    var pairs = entityPairToConnectionLink[entity];

                    for (int i = 0; i < pairs.Count; i++)
                    {
                        EntityPair pair = pairs[i];
                        TryToRemovePairConnection(pair);
                    }

                    entityPairToConnectionLink.Remove(entity);
                }

                return true;
            }
        }

        private bool TryToRemovePairConnection(EntityPair pair)
        {
            if (!connections.ContainsKey(pair)) return false;

            var list = connections[pair].Nodes;

            if (list.Count > 2)
            {
                PedestrianNodeEntityUtils.RemoveConnection(EntityManager, list[0], list[1]);
                PedestrianNodeEntityUtils.RemoveConnection(EntityManager, list[list.Count - 2], list[list.Count - 1]);
            }
            else
            {
                PedestrianNodeEntityUtils.RemoveConnection(EntityManager, pair.A, pair.B);
            }

            RemovePair(pair);
            return true;
        }

        private void RemovePair(EntityPair pair)
        {
            var list = connections[pair].Nodes;

            for (int i = 0; i < list.Count; i++)
            {
                entityInnerToConnectionLink.Remove(list[i]);
            }

            connections.Remove(pair);
        }

        private void RemoveConnectedEntity(Entity sourceEntity, Entity removeEntity)
        {
            PedestrianNodeEntityUtils.RemoveConnectedEntity(EntityManager, sourceEntity, removeEntity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Entity CreatePedNode(RuntimeSegment runtimeSegment, PedestrianNode pedestrianNode, ref EntityCommandBuffer commandBuffer)
        {
            var entity = Entity.Null;

            PedestrianNode crosswalkMergeNode = null;

            var connectedTrafficNode = pedestrianNode.ConnectedTrafficNode;

            var mergeType = GetMergeType(connectedTrafficNode);

            if (mergeType != MergeType.None)
            {
                crosswalkMergeNode = TryToGetMergeNode(pedestrianNode);
            }

            if (mergeType != MergeType.Merged || crosswalkMergeNode == null || TryToGetEntity(crosswalkMergeNode) == Entity.Null)
            {
                entity = CreatePedestrianNode(pedestrianNode, ref commandBuffer);
            }
            else
            {
                entity = TryToGetEntity(crosswalkMergeNode);

                if (entity == Entity.Null)
                {
                    return Entity.Null;
                }

                var settings = EntityManager.GetComponentData<NodeLightSettingsComponent>(entity);

                var lightEntity = TryToGetEntity(pedestrianNode.RelatedTrafficLightHandler);

                if (lightEntity == Entity.Null)
                {
                    lightEntity = TryToGetEntity(crosswalkMergeNode.RelatedTrafficLightHandler);
                }

                if (settings.LightEntity == Entity.Null && lightEntity != Entity.Null)
                {
                    settings.LightEntity = lightEntity;

                    if (settings.CrosswalkIndex == -1)
                    {
                        if (pedestrianNode.ConnectedTrafficNode != null)
                        {
                            crosswalkBinding.TryGetValue(pedestrianNode.ConnectedTrafficNode, out crosswalkIndex);
                            settings.CrosswalkIndex = crosswalkIndex;
                        }
                    }

                    commandBuffer.SetComponent(entity, settings);
                }

                AddLightLink(lightEntity, entity);
            }
            if (!scenePedestrianNodeBinding.ContainsKey(pedestrianNode))
            {
                scenePedestrianNodeBinding.Add(pedestrianNode, entity);
            }

            return entity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Entity CreatePedestrianNode(PedestrianNode pedestrianNode, ref EntityCommandBuffer commandBuffer)
        {
            Entity entity;
            var lightEntity = TryToGetEntity(pedestrianNode.RelatedTrafficLightHandler);

            entity = EntityManager.CreateEntity(
                typeof(NodeCapacityComponent),
                typeof(NodeLightSettingsComponent),
                typeof(NodeSettingsComponent),
                typeof(NodeConnectionDataElement),
                typeof(LocalTransform),
                typeof(LocalToWorld));

            if (addCullingComponents)
                commandBuffer.AddComponent(entity, CullComponentsExtension.GetComponentSet());

            if (pedestrianNode.CanSpawnInView)
            {
                commandBuffer.AddComponent(entity, typeof(NodeCanSpawnInVisionTag));
            }

            int capacity = pedestrianNode.Capacity;

            int maxCapacity = pedestrianNode.PedestrianNodeType == PedestrianNodeType.CarParking ? 1 : capacity;

            var connectedTrafficNode = pedestrianNode.ConnectedTrafficNode;

            if (connectedTrafficNode != null && IsLinkedTrafficNode(connectedTrafficNode))
            {
                var trafficNodeEntity = TryToGetEntity(connectedTrafficNode, false);
                var trafficNodeCapacityComponent = EntityManager.GetComponentData<TrafficNodeCapacityComponent>(trafficNodeEntity);

                trafficNodeCapacityComponent.PedestrianNodeEntity = entity;

                commandBuffer.SetComponent(trafficNodeEntity, trafficNodeCapacityComponent);
                commandBuffer.AddComponent(entity, new NodeLinkedTrafficNodeComponent() { LinkedEntity = trafficNodeEntity });

                commandBuffer.AppendToBuffer(trafficNodeEntity, new ConnectedPedestrianNodeElement()
                {
                    PedestrianNodeEntity = entity
                });
            }

            commandBuffer.SetComponent(entity, new NodeCapacityComponent { MaxAvailableCount = maxCapacity, CurrentCount = capacity });

            if (capacity >= 0)
            {
                commandBuffer.AddComponent(entity, typeof(NodeHasCapacityOptionTag));
            }

            var connectionBuffer = commandBuffer.AddBuffer<NodeConnectionDataElement>(entity);

            int connectionCapacity = pedestrianNode.AutoConnectedPedestrianNodes.Count + pedestrianNode.DefaultConnectedPedestrianNodes.Count;
            connectionBuffer.EnsureCapacity(connectionCapacity);

            var nodeSettingsComponent = new NodeSettingsComponent()
            {
                NodeType = pedestrianNode.PedestrianNodeType,
                NodeShapeType = pedestrianNode.PedestrianNodeShapeType,
                Weight = pedestrianNode.PriorityWeight,
                CustomAchieveDistance = pedestrianNode.CustomAchieveDistance,
                CanSpawnInVision = pedestrianNode.CanSpawnInView ? 1 : 0,
                ChanceToSpawn = pedestrianNode.ChanceToSpawn,
                MaxPathWidth = pedestrianNode.MaxPathWidth,
                Height = pedestrianNode.Height,
                HasMovementRandomOffset = pedestrianNode.HasMovementRandomOffset ? 1 : 0,
                SumWeight = 0
            };

            EntityManager.SetComponentData(entity, nodeSettingsComponent);
            ProcessAdditionalSettings(ref commandBuffer, pedestrianNode, entity);
            var pos = pedestrianNode.transform.position;

            commandBuffer.SetComponent(entity, LocalTransform.FromPosition(pos));

            var crosswalkIndex = -1;

            if (pedestrianNode.ConnectedTrafficNode != null)
            {
                crosswalkBinding.TryGetValue(pedestrianNode.ConnectedTrafficNode, out crosswalkIndex);
            }

            commandBuffer.SetComponent(entity, new NodeLightSettingsComponent()
            {
                HasCrosswalk = true,
                LightEntity = lightEntity,
                CrosswalkIndex = crosswalkIndex,
            });

            return entity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessAdditionalSettings(ref EntityCommandBuffer commandBuffer, PedestrianNode pedestrianNode, Entity entity)
        {
            int capacity = pedestrianNode.Capacity;

            switch (pedestrianNode.PedestrianNodeType)
            {
                case PedestrianNodeType.Sit:
                    {
                        PedestrianNodeSeatSettings pedestrianNodeSeatSettings = pedestrianNode.GetComponent<PedestrianNodeSeatSettings>();

                        if (!pedestrianNodeSeatSettings)
                        {
                            pedestrianNodeSeatSettings = pedestrianNode.gameObject.AddComponent<PedestrianNodeSeatSettings>();
                        }

                        commandBuffer.AddComponent(entity,
                            new NodeSeatSettingsComponent
                            {
                                InitialPosition = pedestrianNode.transform.position,
                                InitialRotation = pedestrianNode.transform.rotation,
                                SeatsCount = capacity,
                                BaseOffset = pedestrianNodeSeatSettings.BaseOffset,
                                SeatOffset = pedestrianNodeSeatSettings.SeatOffset,
                                EnterSeatOffset = pedestrianNodeSeatSettings.EnterSeatOffset,
                                SeatHeight = pedestrianNodeSeatSettings.SeatHeight,
                            });

                        var seats = commandBuffer.AddBuffer<BenchSeatElement>(entity);
                        seats.EnsureCapacity(capacity);

                        for (int i = 0; i < capacity; i++)
                        {
                            seats.Add(new BenchSeatElement());
                        }

                        break;
                    }
                case PedestrianNodeType.TalkArea:
                    {
                        commandBuffer.AddComponent(entity, typeof(CustomSpawnerTag));

                        commandBuffer.AddComponent(entity,
                            new SpawnAreaComponent
                            {
                                AreaType = pedestrianNode.CurrentPedestrianNodeAreaSettings.areaShapeType,
                                AreaSize = pedestrianNode.CurrentPedestrianNodeAreaSettings.areaSize,
                                MinSpawnCount = pedestrianNode.CurrentPedestrianNodeAreaSettings.minSpawnCount,
                                MaxSpawnCount = pedestrianNode.CurrentPedestrianNodeAreaSettings.maxSpawnCount,
                            });

                        commandBuffer.AddComponent(entity,
                            new TalkAreaSettingsComponent
                            {
                                UnlimitedTalkTime = pedestrianNode.CurrentPedestrianNodeAreaSettings.unlimitedTalkTime ? 1 : 0,
                                MinTalkTime = pedestrianNode.CurrentPedestrianNodeAreaSettings.minTalkTime,
                                MaxTalkTime = pedestrianNode.CurrentPedestrianNodeAreaSettings.maxTalkTime,
                            });

                        commandBuffer.AddComponent(entity, typeof(NodeTalkAreaTag));

                        commandBuffer.AddComponent(entity, typeof(NodeAreaSpawnedTag));
                        commandBuffer.AddComponent(entity, typeof(NodeAreaSpawnRequestedTag));

                        commandBuffer.SetComponentEnabled<NodeAreaSpawnedTag>(entity, false);
                        commandBuffer.SetComponentEnabled<NodeAreaSpawnRequestedTag>(entity, false);

                        break;
                    }
                case PedestrianNodeType.TrafficPublicStopStation:
                    {
                        commandBuffer.AddBuffer<WaitQueueElement>(entity);
                        commandBuffer.AddComponent(entity, typeof(WaitQueueComponent));
                        commandBuffer.AddComponent(entity, typeof(NodeProcessWaitQueueTag));
                        commandBuffer.SetComponentEnabled<NodeProcessWaitQueueTag>(entity, false);

                        float minIdleTime = PedestrianNodeStopStationSettings.MIN_IDLE_TIME;
                        float maxIdleTime = PedestrianNodeStopStationSettings.MAX_IDLE_TIME;

                        if (pedestrianNode.TryGetComponent<PedestrianNodeStopStationSettings>(out var pedestrianNodeStopStationSettings))
                        {
                            minIdleTime = pedestrianNodeStopStationSettings.IdleDuration.x;
                            maxIdleTime = pedestrianNodeStopStationSettings.IdleDuration.y;
                        }

                        commandBuffer.AddComponent(entity, new NodeIdleComponent()
                        {
                            MinIdleTime = minIdleTime,
                            MaxIdleTime = maxIdleTime
                        });

                        break;
                    }
                case PedestrianNodeType.Idle:
                    {
                        float minIdleTime = PedestrianNodeIdleSettings.MIN_IDLE_TIME;
                        float maxIdleTime = PedestrianNodeIdleSettings.MAX_IDLE_TIME;

                        if (pedestrianNode.TryGetComponent<PedestrianNodeIdleSettings>(out var pedestrianNodeIdleAuthoring))
                        {
                            minIdleTime = pedestrianNodeIdleAuthoring.IdleDuration.x;
                            maxIdleTime = pedestrianNodeIdleAuthoring.IdleDuration.y;
                        }

                        commandBuffer.AddComponent(entity, new NodeIdleComponent()
                        {
                            MinIdleTime = minIdleTime,
                            MaxIdleTime = maxIdleTime
                        });

                        break;
                    }
            }
        }

        private bool AddLightLink(Entity lightEntity, Entity entity)
        {
            if (lightEntity == Entity.Null)
                return false;

            if (!lightLinkedEntities.ContainsKey(lightEntity))
            {
                lightLinkedEntities.Add(lightEntity, new List<Entity>());
            }

            lightLinkedEntities[lightEntity].Add(entity);
            return true;
        }

        private void AddLightEntity(TrafficLightHandler value, Entity lightEntity)
        {
            lightEntities.Add(value, lightEntity);
        }

        private MergeType GetMergeType(TrafficNode trafficNode)
        {
            if (!trafficNode)
                return MergeType.None;

            if (nodeMerge1.TryGetValue(trafficNode, out var data))
                return data.ConnectionType;

            if (nodeMerge2.TryGetValue(trafficNode, out data))
                return data.ConnectionType;

            return MergeType.None;
        }

        private void PostAddAction()
        {
            RecalculateNavigation();
        }

        private void PostRemoveAction()
        {
            RecalculateNavigation();
        }

        private void RecalculateNavigation()
        {
            var trafficCustomPathService = TrafficCustomPathService.Instance;

            if (trafficCustomPathService)
            {
                trafficCustomPathService.RecalculateAllTargets();
            }

            var pedestrianCustomPathService = PedestrianCustomPathService.Instance;

            if (pedestrianCustomPathService)
            {
                pedestrianCustomPathService.RecalculateAllTargets();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IterateAllLanes(TrafficNode trafficNode, ref EntityCommandBuffer commandBuffer, Action<EntityCommandBuffer, int, bool> callback, bool includeExternal = true)
        {
            if (trafficNode.HasRightLanes)
            {
                for (int laneIndex = 0; laneIndex < trafficNode.GetLaneCount(); laneIndex++)
                {
                    if (trafficNode.LaneExist(laneIndex, false))
                        callback?.Invoke(commandBuffer, laneIndex, false);
                }
            }

            if (includeExternal)
            {
                IterateExternalLanes(trafficNode, ref commandBuffer, callback);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IterateExternalLanes(TrafficNode trafficNode, ref EntityCommandBuffer entityCommand, Action<EntityCommandBuffer, int, bool> callback)
        {
            if (!trafficNode.HasLeftLanes)
                return;

            for (int laneIndex = 0; laneIndex < trafficNode.GetLaneCount(true); laneIndex++)
            {
                callback?.Invoke(entityCommand, laneIndex, true);
            }
        }

#endif
    }
}
