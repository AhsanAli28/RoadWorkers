using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.Gameplay.Road;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateAfter(typeof(TrafficSectionConversionSystem))]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class TrafficLightConversionSystem : SimpleSystemBase
    {
        private int globalCrossroadIndex;

        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<RoadSceneData>();
            Dispose();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Dispose();
        }

        protected override void OnUpdate()
        {
            Dispose();

            InitializeLightForLanes();
        }

        private void Dispose()
        {
            globalCrossroadIndex = 0;
        }

        private void InitializeLightForLanes()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            var roadSceneData = SystemAPI.GetSingleton<RoadSceneData>();
            var roadStreamingConfig = SystemAPI.GetSingleton<RoadStreamingConfigReference>().Config.Value;

            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((
                Entity crossRoadEntity,
                in LightCrossroadBakingData lightCrossroadBakingData,
                in SegmentComponent crossroadComponent) =>
            {
                var lightHandlers = lightCrossroadBakingData.LightHandlerEntities;
                int lightStateCount = 0;

                if (lightCrossroadBakingData.UniqueID != 0)
                {
                    for (int i = 0; i < lightHandlers.Length; i++)
                    {
                        var handlerData = lightHandlers[i];

                        var lightTempLightEntity = handlerData.Entity;
                        var lightHandlerEntity = BakerExtension.GetEntity(EntityManager, lightTempLightEntity);

                        int handlerIndex = handlerData.HandlerIndex;
                        int handlerId = lightCrossroadBakingData.UniqueID + handlerIndex;

                        commandBuffer.AddComponent(lightHandlerEntity, new LightHandlerID()
                        {
                            Value = handlerId
                        });
                    }
                }

                for (int i = 0; i < lightHandlers.Length; i++)
                {
                    var handlerData = lightHandlers[i];

                    var lightTempLightEntity = handlerData.Entity;
                    var lightHandlerEntity = BakerExtension.GetEntity(EntityManager, lightTempLightEntity);

                    LightState initialLightState = LightState.Green;

                    commandBuffer.AddComponent<LightHandlerInitTag>(lightHandlerEntity);
                    commandBuffer.AddComponent<LightHandlerStateUpdateTag>(lightHandlerEntity);

                    var stateBuffer = commandBuffer.AddBuffer<LightHandlerStateElement>(lightHandlerEntity);

                    if (roadStreamingConfig.StreamingIsEnabled)
                    {
                        commandBuffer.AddSharedComponent(lightHandlerEntity, new SceneSection()
                        {
                            SceneGUID = roadSceneData.Hash128,
                            Section = crossroadComponent.SectionIndex
                        });
                    }

                    int startIndex = lightStateCount;
                    int endIndex = startIndex + handlerData.LightStateCount;

                    stateBuffer.EnsureCapacity(handlerData.LightStateCount);

                    float totalDuration = 0;

                    for (int j = startIndex; j < endIndex; j++)
                    {
                        stateBuffer.Add(new LightHandlerStateElement()
                        {
                            LightState = lightCrossroadBakingData.LightStates[j].LightState,
                            Duration = lightCrossroadBakingData.LightStates[j].Duration,
                        });

                        totalDuration += lightCrossroadBakingData.LightStates[j].Duration;
                    }

                    commandBuffer.AddComponent(lightHandlerEntity, new LightHandlerComponent()
                    {
                        CrossRoadIndex = globalCrossroadIndex,
                        State = initialLightState,
                        CycleDuration = totalDuration
                    });

                    lightStateCount += handlerData.LightStateCount;
                }

                globalCrossroadIndex++;

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}