using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class VehicleLinkBakingSystem : SystemBase
    {
        private EntityQuery bakingCarQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingCarQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<VehicleLinkBakingComponent>()
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
                in VehicleLinkBakingComponent vehicleLinkBakingComponent) =>
            {
                var entities = vehicleLinkBakingComponent.LinkedEntities;

                for (int i = 0; i < entities.Length; i++)
                {
                    if (entities[i] == Entity.Null)
                    {
                        continue;
                    }

                    commandBuffer.AddComponent(entities[i], new VehicleLinkComponent()
                    {
                        LinkedVehicle = prefabEntity
                    });
                }

            }).Run();

            CompleteDependency();
            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}