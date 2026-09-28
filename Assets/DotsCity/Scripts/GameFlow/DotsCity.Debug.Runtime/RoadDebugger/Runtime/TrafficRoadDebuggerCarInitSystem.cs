#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial class TrafficRoadDebuggerCarInitSystem : BeginSimulationSystemBase
    {
        private TrafficRoadSpawnDebuggerSystem trafficRoadDebuggerSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            trafficRoadDebuggerSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TrafficRoadSpawnDebuggerSystem>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = GetCommandBuffer();

            Entities
            .WithoutBurst()
            .ForEach((
               Entity entity,
               ref TrafficRoadSegmentInitComponent trafficRoadSegmentInitComponent) =>
            {
                trafficRoadDebuggerSystem.AddCar(ref commandBuffer, entity, trafficRoadSegmentInitComponent.HashID);

                commandBuffer.RemoveComponent<TrafficRoadSegmentInitComponent>(entity);
            }).Run();

            AddCommandBufferForProducer();
        }
    }
}
#endif