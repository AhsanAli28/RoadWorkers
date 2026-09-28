using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Road.Authoring;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    [TemporaryBakingType]
    public struct PedestrianSubNodeBakingData : IComponentData
    {
        public Entity SourceEntity;
        public Entity TargetEntity;
        public bool Last;
        public bool Oneway;
    }

    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateBefore(typeof(TrafficSectionConversionSystem))]
    [UpdateInGroup(typeof(BakingSystemGroup), OrderFirst = true)]
    public partial class PedestrianSubNodeConversionSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var roadStatConfigEntity = SystemAPI.GetSingletonEntity<RoadStatConfig>();
            var roadStatConfig = SystemAPI.GetSingleton<RoadStatConfig>();
            var citySpawnConfig = SystemAPI.GetSingleton<CitySpawnConfigReference>();

            Entities
            .WithoutBurst()
            .ForEach((
                Entity entity,
                in PedestrianSubNodeBakingData pedestrianSubNodeBakingData) =>
            {
                roadStatConfig.PedestrianNodeTotal++;

                if (pedestrianSubNodeBakingData.Last && !pedestrianSubNodeBakingData.Oneway)
                {
                    var connectedEntity = BakerExtension.GetEntity(EntityManager, pedestrianSubNodeBakingData.TargetEntity);
                    var buffer = EntityManager.GetBuffer<NodeConnectionDataElement>(connectedEntity);

                    for (int i = 0; i < buffer.Length; i++)
                    {
                        var connectedEntityLocal = BakerExtension.GetEntity(EntityManager, buffer[i].ConnectedEntity);
                        if (connectedEntityLocal == pedestrianSubNodeBakingData.SourceEntity)
                        {
                            var localElement = buffer[i];
                            localElement.ConnectedEntity = entity;
                            buffer[i] = localElement;
                            break;
                        }
                    }
                }

                var buffer2 = EntityManager.GetBuffer<NodeConnectionDataElement>(entity);

                var element = buffer2[0];
                element.ConnectedEntity = BakerExtension.GetEntity(EntityManager, element.ConnectedEntity);
                buffer2[0] = element;

                commandBuffer.AddComponent(entity, CullComponentsExtension.GetComponentSet(citySpawnConfig.Config.Value.PedestrianNodeStateList));
            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            EntityManager.SetComponentData(roadStatConfigEntity, roadStatConfig);
        }
    }
}