using Spirit604.DotsCity.Simulation.Car.Sound;
using Spirit604.DotsCity.Simulation.Car.Sound.Authoring;
using Spirit604.DotsCity.Simulation.Sound;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class CarSoundBakingSystem : SystemBase
    {
        private EntityQuery bakingCarQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingCarQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CarSoundEntityBakingTag>()
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
            .ForEach((
                Entity prefabEntity,
                ref CarModelComponent carModelComponent,
                in CarSoundEntityBakingTag carSoundEntityBakingData) =>
            {
                commandBuffer.SetComponentEnabled<CarUpdateSound>(prefabEntity, false);
                commandBuffer.SetComponentEnabled<HasSoundTag>(prefabEntity, false);

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}