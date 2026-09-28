using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.TrafficPublic.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class TrafficPublicEntityBakingSystem : SystemBase
    {
        private EntityQuery bakingCarQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingCarQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TrafficPublicEntityBakingTag>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate(bakingCarQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
            .WithAll<TrafficPublicEntityBakingTag>()
            .ForEach((
                Entity prefabEntity
                ) =>
            {
                commandBuffer.SetComponentEnabled<TrafficPublicExitCompleteTag>(prefabEntity, false);
                commandBuffer.SetComponentEnabled<TrafficPublicProccessExitTag>(prefabEntity, false);

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}