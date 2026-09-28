using Spirit604.DotsCity.Core;
using Spirit604.Gameplay.Road;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup), OrderLast = true)]
    public partial class TrafficNodeLightTriggerConversionSystem : SimpleSystemBase
    {
        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .ForEach((
                Entity entity,
                ref TrafficNodeLightTriggerBakingData trafficNodeLightTriggerBakingData) =>
            {
                var sourceLightEntity = BakerExtension.GetEntity(EntityManager, trafficNodeLightTriggerBakingData.SourceLightEntity);

                if (EntityManager.HasComponent<TrafficNodeScopeBakingData>(entity))
                {
                    var trafficNodeScopeBakingData = EntityManager.GetComponentData<TrafficNodeScopeBakingData>(entity);
                    var entities = trafficNodeScopeBakingData.GetMainLaneEntities();

                    for (int i = 0; i < entities.Length; i++)
                    {
                        var trafficNodeEntity = entities[i].Entity;

                        commandBuffer.AddComponent<LightTriggerBinding>(trafficNodeEntity);
                        commandBuffer.AddComponent<LightTriggerEnabledTag>(trafficNodeEntity);
                        commandBuffer.SetComponentEnabled<LightTriggerEnabledTag>(trafficNodeEntity, false);

                        commandBuffer.AddComponent(trafficNodeEntity, new LightTrigger()
                        {
                            TriggerDistanceSQ = trafficNodeLightTriggerBakingData.TriggerDistance * trafficNodeLightTriggerBakingData.TriggerDistance,
                            SourceLightEntity = sourceLightEntity,
                        });

                        var buffer = commandBuffer.AddBuffer<LightTriggerRelatedEntity>(trafficNodeEntity);

                        for (int j = 0; j < trafficNodeLightTriggerBakingData.TargetLightEntities.Length; j++)
                        {
                            var relatedLightEntity = BakerExtension.GetEntity(EntityManager, trafficNodeLightTriggerBakingData.TargetLightEntities[j]);

                            buffer.Add(new LightTriggerRelatedEntity()
                            {
                                LightEntity = relatedLightEntity
                            });

                            var relatedLightHandlerComponent = EntityManager.GetComponentData<LightHandlerComponent>(sourceLightEntity);
                            relatedLightHandlerComponent = relatedLightHandlerComponent.SetState(LightState.Green);

                            commandBuffer.AddComponent<LightHandlerOverrideStateTag>(relatedLightEntity);

                            commandBuffer.SetComponent(relatedLightEntity, relatedLightHandlerComponent);
                        }
                    }
                }

                var lightHandlerComponent = EntityManager.GetComponentData<LightHandlerComponent>(sourceLightEntity);
                lightHandlerComponent = lightHandlerComponent.SetState(LightState.Red);

                commandBuffer.SetComponent(sourceLightEntity, lightHandlerComponent);

                commandBuffer.AddComponent<LightHandlerOverrideStateTag>(sourceLightEntity);

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}
