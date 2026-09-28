using Spirit604.DotsCity.Simulation.Road.Authoring;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateAfter(typeof(TrafficNodeConversionSystem))]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class TrafficRoadDebuggerBakingSystem : SystemBase
    {
        private TrafficNodeConversionSystem trafficNodeConversionSystem;
        private EntityQuery query;

        protected override void OnCreate()
        {
            base.OnCreate();

            trafficNodeConversionSystem = World.GetOrCreateSystemManaged<TrafficNodeConversionSystem>();

            query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TrafficRoadDebuggerInfoBakingData>()
                .Build(this);

            RequireForUpdate(query);
        }

        protected override void OnUpdate()
        {
            if (trafficNodeConversionSystem.NodeIndex == 0)
            {
                return;
            }

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .ForEach((
                Entity entity,
                ref TrafficRoadDebuggerInfoBakingData trafficRoadDebuggerInfoBakingData) =>
            {
                var dataOfLanes = trafficRoadDebuggerInfoBakingData.DataOfLanes;

                var buffer = commandBuffer.AddBuffer<DebugRoadLaneElement>(entity);

                for (int i = 0; i < dataOfLanes.Length; i++)
                {
                    var trafficNodeScopeData = EntityManager.GetComponentData<TrafficNodeScopeBakingData>(dataOfLanes[i].NodeScopeEntity);

                    var trafficNode = trafficNodeScopeData.RightLaneEntities[dataOfLanes[i].LaneIndex];

                    buffer.Add(new DebugRoadLaneElement()
                    {
                        IdleCar = dataOfLanes[i].IdleCar,
                        SpawnDelay = dataOfLanes[i].SpawnDelay,
                        SpawnCarModel = dataOfLanes[i].SpawnCarModel,
                        NormalizedPathPosition = dataOfLanes[i].NormalizedPathPosition,
                        TrafficNodeEntity = trafficNode.Entity,
                        LocalPathIndex = dataOfLanes[i].LocalPathIndex,
                    });

                    if (i == 0)
                    {
                        var sceneSection = EntityManager.GetSharedComponent<SceneSection>(trafficNode.Entity);

                        commandBuffer.AddSharedComponent(entity, sceneSection);
                    }
                }
            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}
