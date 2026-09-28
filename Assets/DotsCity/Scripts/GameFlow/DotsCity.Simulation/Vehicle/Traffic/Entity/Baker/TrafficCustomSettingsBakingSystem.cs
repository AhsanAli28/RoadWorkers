using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup), OrderFirst = true)]
    public partial class TrafficCustomSettingsBakingSystem : SystemBase
    {
        private EntityQuery bakingCarQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingCarQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CustomSettingsBakingData>()
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
                in CustomSettingsBakingData сustomSettingsBakingData) =>
            {
                var vehicleEntity = сustomSettingsBakingData.VehicleEntity;

                var trafficSettingsComponent = EntityManager.GetComponentData<TrafficSettingsComponent>(vehicleEntity);

                trafficSettingsComponent.MaxSpeed = сustomSettingsBakingData.MaxCarSpeed;
                trafficSettingsComponent.MaxSteerAngle = сustomSettingsBakingData.MaxSteeringAngle;

                commandBuffer.SetComponent(vehicleEntity, trafficSettingsComponent);

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}