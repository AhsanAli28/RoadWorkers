using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Road.Authoring;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup), OrderLast = true)]
    public partial class PedestrianNodeConversionSystem : SystemBase
    {
        private HashSet<Entity> bufferBinding = new HashSet<Entity>();
        private EntityQuery subNodeQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            subNodeQuery = EntityManager.CreateEntityQuery(typeof(PedestrianSubNodeBakingData));
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var roadStatConfigEntity = SystemAPI.GetSingletonEntity<RoadStatConfig>();
            var roadStatConfig = SystemAPI.GetSingleton<RoadStatConfig>();
            var citySpawnConfig = SystemAPI.GetSingleton<CitySpawnConfigReference>();
            roadStatConfig.PedestrianNodeTotal = subNodeQuery.CalculateEntityCount();

            bufferBinding.Clear();

            Entities
            .WithoutBurst()
            .WithNone<PedestrianSubNodeBakingData, DestroyNodeEditorTag>()
            .ForEach((
                Entity entity,
                ref DynamicBuffer<NodeConnectionDataElement> nodeConnectionData,
                in PedestrianNodeBakingData pedestrianNodeBakingData,
                in NodeSettingsComponent nodeSettingsComponent) =>
            {
                roadStatConfig.PedestrianNodeTotal++;

                bool hasCrosswalk = true;
                int crossWalkIndex = -1;

                if (pedestrianNodeBakingData.TrafficNodeScopeEntity != Entity.Null)
                {
                    var trafficNodeScopeData = EntityManager.GetComponentData<TrafficNodeScopeBakingData>(pedestrianNodeBakingData.TrafficNodeScopeEntity);

                    var nodeEntities = trafficNodeScopeData.GetCurrentLaneEntities(EntityManager);

                    var trafficNodeEntity = Entity.Null;

                    if (nodeEntities.Length > 0)
                        trafficNodeEntity = nodeEntities[nodeEntities.Length - 1].Entity;

                    if (trafficNodeEntity != Entity.Null)
                    {
                        if (!bufferBinding.Contains(trafficNodeEntity))
                        {
                            bufferBinding.Add(trafficNodeEntity);
                            commandBuffer.AddBuffer<ConnectedPedestrianNodeElement>(trafficNodeEntity);
                        }

                        var trafficNodeCapacityComponent = EntityManager.GetComponentData<TrafficNodeCapacityComponent>(trafficNodeEntity);
                        var trafficNodeSettingsComponent = EntityManager.GetComponentData<TrafficNodeSettingsComponent>(trafficNodeEntity);

                        trafficNodeCapacityComponent.PedestrianNodeEntity = entity;

                        commandBuffer.SetComponent(trafficNodeEntity, trafficNodeCapacityComponent);
                        commandBuffer.AddComponent(entity, new NodeLinkedTrafficNodeComponent() { LinkedEntity = trafficNodeEntity });

                        commandBuffer.AppendToBuffer(trafficNodeEntity, new ConnectedPedestrianNodeElement()
                        {
                            PedestrianNodeEntity = entity
                        });

                        hasCrosswalk = trafficNodeSettingsComponent.HasCrosswalk;
                        crossWalkIndex = trafficNodeScopeData.CrossWalkIndex;
                    }
                }
                else
                {
                    if (nodeSettingsComponent.NodeType == PedestrianNodeType.CarParking || nodeSettingsComponent.NodeType == PedestrianNodeType.TrafficPublicStopStation)
                    {
                        UnityEngine.Debug.Log($"PedestrianNode InstanceId {pedestrianNodeBakingData.PedestrianNodeInstanceId} Entity Index '{entity.Index}' PedestrianNodeType '{nodeSettingsComponent.NodeType}' doesn't have linked traffic node{TrafficObjectFinderMessage.GetMessage()}");
                    }
                }

                for (int i = 0; i < nodeConnectionData.Length; i++)
                {
                    var pedestrianNodeConnectionDataElement = nodeConnectionData[i];

                    var realConnectedEntity = BakerExtension.GetEntity(EntityManager, pedestrianNodeConnectionDataElement.ConnectedEntity);
                    pedestrianNodeConnectionDataElement.ConnectedEntity = realConnectedEntity;
                    nodeConnectionData[i] = pedestrianNodeConnectionDataElement;
                }

                var lightEntity = Entity.Null;

                if (pedestrianNodeBakingData.LightEntity != Entity.Null)
                {
                    lightEntity = BakerExtension.GetEntity(EntityManager, pedestrianNodeBakingData.LightEntity);
                }

                commandBuffer.SetComponent(entity,
                    new NodeLightSettingsComponent()
                    {
                        HasCrosswalk = hasCrosswalk,
                        LightEntity = lightEntity,
                        CrosswalkIndex = crossWalkIndex,
                    });


                commandBuffer.AddComponent(entity, CullComponentsExtension.GetComponentSet(citySpawnConfig.Config.Value.PedestrianNodeStateList));

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            EntityManager.SetComponentData(roadStatConfigEntity, roadStatConfig);
        }
    }
}