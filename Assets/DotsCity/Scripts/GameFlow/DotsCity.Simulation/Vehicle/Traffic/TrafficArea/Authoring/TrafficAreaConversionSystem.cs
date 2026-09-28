using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Road.Authoring;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.TrafficArea.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateAfter(typeof(TrafficNodeConversionSystem))]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class TrafficAreaConversionSystem : SystemBase
    {
        private TrafficNodeConversionSystem trafficNodeConversionSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            trafficNodeConversionSystem = World.GetOrCreateSystemManaged<TrafficNodeConversionSystem>();
        }

        protected override void OnUpdate()
        {
            trafficNodeConversionSystem.GetDependency().Complete();

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((Entity areaEntity, in TrafficAreaBakerTempBakingData trafficAreaBakerTempBakingData) =>
            {
                var enterNodes = trafficAreaBakerTempBakingData.EnterNodes;
                var queueNodes = trafficAreaBakerTempBakingData.QueueNodes;
                var exitNodes = trafficAreaBakerTempBakingData.ExitNodes;
                var defaultNodes = trafficAreaBakerTempBakingData.DefaultNodes;

                InitTrafficNode(ref commandBuffer, in defaultNodes, areaEntity, TrafficAreaNodeType.Default, trafficAreaBakerTempBakingData.PlaceNodeTypes[0]);
                InitTrafficNode(ref commandBuffer, in enterNodes, areaEntity, TrafficAreaNodeType.Enter, trafficAreaBakerTempBakingData.PlaceNodeTypes[1]);
                InitTrafficNode(ref commandBuffer, in queueNodes, areaEntity, TrafficAreaNodeType.Queue, trafficAreaBakerTempBakingData.PlaceNodeTypes[2]);
                InitTrafficNode(ref commandBuffer, in exitNodes, areaEntity, TrafficAreaNodeType.Exit, trafficAreaBakerTempBakingData.PlaceNodeTypes[3]);

                commandBuffer.SetComponentEnabled<TrafficAreaProcessingEnterQueueTag>(areaEntity, false);
                commandBuffer.SetComponentEnabled<TrafficAreaProcessingExitQueueTag>(areaEntity, false);
                commandBuffer.SetComponentEnabled<TrafficAreaUpdateLockStateTag>(areaEntity, false);
                commandBuffer.SetComponentEnabled<TrafficAreaCarObserverEnabledTag>(areaEntity, false);

                if (trafficAreaBakerTempBakingData.RelatedSegment != Entity.Null)
                {
                    var segment = BakerExtension.GetEntity(EntityManager, trafficAreaBakerTempBakingData.RelatedSegment);
                    var sceneSection = EntityManager.GetSharedComponent<SceneSection>(segment);
                    commandBuffer.AddSharedComponent(areaEntity, sceneSection);
                }

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }

        private void InitTrafficNode(
            ref EntityCommandBuffer commandBuffer,
            in NativeArray<Entity> linkedNodes,
            Entity areaEntity,
            TrafficAreaNodeType trafficAreaNodeType,
            TrafficAreaAuthoring.NodePlaceType placeType)
        {
            for (int i = 0; i < linkedNodes.Length; i++)
            {
                var scopeEntity = linkedNodes[i];

                if (scopeEntity == Entity.Null)
                {
                    continue;
                }

                var scopeData = EntityManager.GetComponentData<TrafficNodeScopeBakingData>(scopeEntity);

                NativeArray<TrafficNodeTempData> lanes = default;

                switch (placeType)
                {
                    case TrafficAreaAuthoring.NodePlaceType.Default:
                        lanes = scopeData.GetMainLaneEntities();
                        break;
                    case TrafficAreaAuthoring.NodePlaceType.ForceRightLane:
                        lanes = scopeData.RightLaneEntities;
                        break;
                    case TrafficAreaAuthoring.NodePlaceType.ForceLeftLane:
                        lanes = scopeData.LeftLaneEntities;
                        break;
                }

                for (int laneIndex = 0; laneIndex < lanes.Length; laneIndex++)
                {
                    var nodeEntity = lanes[laneIndex].Entity;

                    switch (trafficAreaNodeType)
                    {
                        case TrafficAreaNodeType.Queue:
                            {
                                var queueBufferNodes = EntityManager.GetBuffer<TrafficAreaQueueNodeElement>(areaEntity);

                                queueBufferNodes.Add(new TrafficAreaQueueNodeElement()
                                {
                                    NodeEntity = nodeEntity
                                });

                                break;
                            }

                        case TrafficAreaNodeType.Enter:
                            {
                                var enterBufferNodes = EntityManager.GetBuffer<TrafficAreaEnterNodeElement>(areaEntity);

                                enterBufferNodes.Add(new TrafficAreaEnterNodeElement()
                                {
                                    NodeEntity = nodeEntity
                                });

                                break;
                            }
                    }

                    commandBuffer.AddComponent(nodeEntity,
                       new TrafficAreaNode()
                       {
                           TrafficAreaNodeType = trafficAreaNodeType,
                           AreaEntity = areaEntity
                       });

                    commandBuffer.AddComponent<TrafficAreaEntryNodeComponent>(nodeEntity);
                    commandBuffer.AddComponent<TrafficAreaProcessEnteredNodeTag>(nodeEntity);
                    commandBuffer.SetComponentEnabled<TrafficAreaProcessEnteredNodeTag>(nodeEntity, false);
                }
            }
        }
    }
}