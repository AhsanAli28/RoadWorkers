using Spirit604.DotsCity.Simulation.Level.Streaming;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;

namespace Spirit604.DotsCity.Debug
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class TrafficRoadDebuggerInitSystem : EndInitSystemBase
    {
        private TrafficRoadSpawnDebuggerSystem trafficRoadDebuggerSystem;
        private EntityQuery streamingConfigQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            trafficRoadDebuggerSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TrafficRoadSpawnDebuggerSystem>();

            streamingConfigQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<RoadStreamingConfigReference>()
                .Build(this);

            RequireForUpdate<TrafficRoadDebuggerInit>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = GetCommandBuffer();

            bool streaming = false;

            if (streamingConfigQuery.CalculateEntityCount() > 0)
            {
                var streamingConfig = streamingConfigQuery.GetSingleton<RoadStreamingConfigReference>();
                streaming = streamingConfig.Config.Value.StreamingIsEnabled;
            }

            Entities
            .WithoutBurst()
            .WithAll<TrafficRoadDebuggerInit>()
            .ForEach((
                Entity entity,
                DynamicBuffer<DebugRoadLaneElement> debugRoadLaneBuffer,
                in TrafficRoadDebuggerInfo trafficRoadDebuggerInfo) =>
            {
                trafficRoadDebuggerSystem.AddDebugger(entity, streaming);

#if UNITY_EDITOR
                var trafficRoadDebuggerObject = EditorUtility.InstanceIDToObject(trafficRoadDebuggerInfo.InstanceId);

                if (trafficRoadDebuggerObject)
                {
                    var trafficRoadDebugger = trafficRoadDebuggerObject as TrafficRoadDebugger;

                    if (trafficRoadDebugger)
                    {
                        trafficRoadDebugger.Init(entity);
                    }
                }
#endif

                commandBuffer.SetComponentEnabled<TrafficRoadDebuggerInit>(entity, false);
            }).Run();

            AddCommandBufferForProducer();
        }
    }
}
