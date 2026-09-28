using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    [TemporaryBakingType]
    public struct PedestrianLocalSpawnerBakingData : IComponentData
    {
        public int LocalSpawnerInstanceId;
        public NativeArray<Entity> PedestrianNodeEntities;
    }

    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class PedestrianLocalSpawnerBakingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .ForEach((
                Entity entity,
                in PedestrianLocalSpawnerBakingData pedestrianLocalSpawnerBakingData) =>
            {
                var entities = pedestrianLocalSpawnerBakingData.PedestrianNodeEntities;

                for (int i = 0; i < entities.Length; i++)
                {
                    if (entities[i] != Entity.Null)
                    {
                        var linkedPedestrianEntity = BakerExtension.GetEntity(EntityManager, entities[i]);

                        commandBuffer.AddComponent(linkedPedestrianEntity, new PedestrianLocalSpawnerDataComponent()
                        {
                            LocalSpawnerInstanceId = pedestrianLocalSpawnerBakingData.LocalSpawnerInstanceId,
                            LocalIndex = i
                        });
                    }
                    else
                    {

                    }
                }
            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}
