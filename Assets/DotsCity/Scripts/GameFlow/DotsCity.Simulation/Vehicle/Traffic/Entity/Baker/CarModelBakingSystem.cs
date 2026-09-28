using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup), OrderFirst = true)]
    public partial class CarModelBakingSystem : SystemBase
    {
        private EntityQuery bakingCarQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingCarQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CarModelBakingData>()
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
                in CarModelBakingData carModelBakingData) =>
            {
                var vehicleEntity = carModelBakingData.VehicleEntity;

                if (!EntityManager.HasComponent<CarModelComponent>(vehicleEntity))
                {
                    UnityEngine.Debug.Log("CarModelBakingSystem. The vehicle entity doesn't have CarModelComponent component. Make sure that the entity has the authoring component derived from CarEntityAuthoringBase.");
                    return;
                }

                var carModelComponent = EntityManager.GetComponentData<CarModelComponent>(vehicleEntity);

                carModelComponent.Value = carModelBakingData.CarModel;
                carModelComponent.LocalIndex = carModelBakingData.LocalIndex;

                commandBuffer.SetComponent(vehicleEntity, carModelComponent);

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}