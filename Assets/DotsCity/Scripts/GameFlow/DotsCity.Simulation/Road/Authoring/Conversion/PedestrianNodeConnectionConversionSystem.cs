using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Pedestrian.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateAfter(typeof(TrafficSectionConversionSystem))]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class PedestrianNodeConnectionConversionSystem : SimpleSystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<PedestrianNodeBakingData>();
            RequireForUpdate<RoadSceneData>();
            RequireForUpdate<RoadStreamingConfigReference>();
        }

        protected override void OnUpdate()
        {
            var roadSceneData = SystemAPI.GetSingleton<RoadSceneData>();
            var roadStreamingConfig = SystemAPI.GetSingleton<RoadStreamingConfigReference>().Config.Value;

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((
                Entity nodeEntity,
                in PedestrianNodeBakingData pedestrianNodeBakingData,
                in SceneSection currentSection) =>
            {
                var connectionBuffer = EntityManager.GetBuffer<NodeConnectionDataElement>(nodeEntity, true);

                bool sectionConnection = false;

                if (roadStreamingConfig.StreamingIsEnabled)
                {
                    for (int i = 0; i < connectionBuffer.Length; i++)
                    {
                        var connectedBakerEntity = connectionBuffer[i].ConnectedEntity;
                        var connectedEntity = BakerExtension.GetEntity(EntityManager, connectedBakerEntity);
                        var connectedSection = EntityManager.GetSharedComponent<SceneSection>(connectedEntity);

                        if (currentSection.Section != connectedSection.Section)
                        {
                            sectionConnection = true;
                            break;
                        }
                    }
                }

                bool hasMerge = false;
                PedestrianNodeMergeBakingData mergeData = default;
                var realConnectedMergeEntity = Entity.Null;

                if (EntityManager.HasComponent<PedestrianNodeMergeBakingData>(nodeEntity))
                {
                    hasMerge = true;
                    mergeData = EntityManager.GetComponentData<PedestrianNodeMergeBakingData>(nodeEntity);

                    var sourceMergeEntity = mergeData.MergeEntity;
                    mergeData.MergeEntity = BakerExtension.GetEntity(EntityManager, mergeData.MergeEntity);

                    if (!sectionConnection)
                    {
                        var priorityNode = GetPriorityNode(nodeEntity, in pedestrianNodeBakingData, in mergeData);

                        if (nodeEntity != priorityNode)
                        {
                            commandBuffer.AddComponent<DestroyNodeEditorTag>(nodeEntity);
                        }
                        else
                        {
                            var connectedEntities = PedestrianNodeEntityUtils.GetConnectedEntities(EntityManager, mergeData.MergeEntity);

                            for (var i = 0; i < connectedEntities.Length; i++)
                            {
                                PedestrianNodeEntityUtils.RemoveConnectedEntity(EntityManager, mergeData.MergeEntity, connectedEntities[i]);

                                realConnectedMergeEntity = BakerExtension.GetEntity(EntityManager, connectedEntities[i]);

                                PedestrianNodeEntityUtils.RemoveConnectedEntity(EntityManager, realConnectedMergeEntity, sourceMergeEntity);

                                PedestrianNodeEntityUtils.AddConnection(EntityManager, nodeEntity, realConnectedMergeEntity);
                            }
                        }
                    }
                    else
                    {
                        PedestrianNodeEntityUtils.AddConnection(EntityManager, nodeEntity, mergeData.MergeEntity);
                    }
                }

                if (!sectionConnection)
                    return;

                connectionBuffer = EntityManager.GetBuffer<NodeConnectionDataElement>(nodeEntity, true);

                var buffer = EntityManager.GetBuffer<SegmentPedestrianNodeData>(pedestrianNodeBakingData.CrossroadEntity);

                buffer.Add(new SegmentPedestrianNodeData()
                {
                    Entity = nodeEntity
                });

                var position = pedestrianNodeBakingData.Position;
                var hash = HashMapHelper.GetHashMapPosition(position, roadStreamingConfig.NodeCellSize);
                bool hasPriority = true;

                if (hasMerge)
                {
                    var priorityNode = GetPriorityNode(nodeEntity, in pedestrianNodeBakingData, in mergeData);

                    realConnectedMergeEntity = BakerExtension.GetEntity(EntityManager, mergeData.MergeEntity);
                    PedestrianNodeEntityUtils.AddConnection(EntityManager, nodeEntity, realConnectedMergeEntity);

                    if (nodeEntity != priorityNode)
                    {
                        hasPriority = false;
                        hash++;
                    }
                }

                commandBuffer.AddComponent(nodeEntity, new PedestrianSectionData()
                {
                    NodeHash = hash
                });

                var sectionBuffer = commandBuffer.AddBuffer<NodeSectionConnectionDataElement>(nodeEntity);
                sectionBuffer.EnsureCapacity(connectionBuffer.Length);

                for (int i = 0; i < connectionBuffer.Length; i++)
                {
                    var connectedBakerEntity = connectionBuffer[i].ConnectedEntity;
                    var connectedEntity = BakerExtension.GetEntity(EntityManager, connectedBakerEntity);
                    var connectedSection = EntityManager.GetSharedComponent<SceneSection>(connectedEntity);

                    var connectedHash = -1;

                    if (currentSection.Section != connectedSection.Section)
                    {
                        var connectedTransform = EntityManager.GetComponentData<LocalTransform>(connectedEntity);
                        connectedHash = HashMapHelper.GetHashMapPosition(connectedTransform.Position, roadStreamingConfig.NodeCellSize);

                        if (realConnectedMergeEntity == connectedEntity && hasPriority)
                        {
                            connectedHash++;
                        }
                    }

                    sectionBuffer.Add(new NodeSectionConnectionDataElement()
                    {
                        ConnectedHash = connectedHash
                    });
                }

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }

        private Entity GetPriorityNode(Entity nodeEntity, in PedestrianNodeBakingData pedestrianNodeBakingData, in PedestrianNodeMergeBakingData mergeData)
        {
            var lightData1 = EntityManager.GetComponentData<NodeLightSettingsComponent>(nodeEntity);
            var lightData2 = EntityManager.GetComponentData<NodeLightSettingsComponent>(mergeData.MergeEntity);
            var pedestrianNodeBakingData2 = EntityManager.GetComponentData<PedestrianNodeBakingData>(mergeData.MergeEntity);

            if (pedestrianNodeBakingData.LightEntity == Entity.Null && pedestrianNodeBakingData2.LightEntity != Entity.Null)
            {
                return mergeData.MergeEntity;
            }

            if (pedestrianNodeBakingData.LightEntity != Entity.Null && pedestrianNodeBakingData2.LightEntity == Entity.Null)
            {
                return nodeEntity;
            }

            if (!lightData1.HasCrosswalk && lightData2.HasCrosswalk)
            {
                return mergeData.MergeEntity;
            }

            if (lightData1.HasCrosswalk && !lightData2.HasCrosswalk)
            {
                return nodeEntity;
            }

            if (pedestrianNodeBakingData.TrafficNodeScopeEntity != Entity.Null)
            {
                var trafficNodeScopeData = EntityManager.GetComponentData<TrafficNodeScopeBakingData>(pedestrianNodeBakingData.TrafficNodeScopeEntity);

                var trafficNodeScopeData2 = EntityManager.GetComponentData<TrafficNodeScopeBakingData>(pedestrianNodeBakingData2.TrafficNodeScopeEntity);

                return pedestrianNodeBakingData.TrafficNodeScopeEntity.Index < pedestrianNodeBakingData2.TrafficNodeScopeEntity.Index ? nodeEntity : mergeData.MergeEntity;
            }

            return Entity.Null;
        }
    }
}