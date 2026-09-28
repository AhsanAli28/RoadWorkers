using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Sound;
using Spirit604.DotsCity.Simulation.Sound.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car.Sound
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CarUpdateSoundSystem : ISystem
    {
        private EntityQuery soundPrefabQuery;
        private EntityQuery updateQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            soundPrefabQuery = SoundExtension.GetSoundQuery(state.EntityManager, SoundType.TrackingVehicle);

            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<CarUpdateSound, HasSoundTag, CarSoundData>()
                .Build();

            state.RequireForUpdate(updateQuery);
            state.RequireForUpdate<CarSharedDataConfigReference>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var updateSoundJob = new UpdateSoundJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                CarSharedDataConfigReference = SystemAPI.GetSingleton<CarSharedDataConfigReference>(),
                SoundEntityPrefab = soundPrefabQuery.GetSingletonEntity(),
            };

            updateSoundJob.Run();
        }

        [WithAll(typeof(CarUpdateSound), typeof(HasSoundTag))]
        [BurstCompile]
        public partial struct UpdateSoundJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public CarSharedDataConfigReference CarSharedDataConfigReference;

            [ReadOnly]
            public Entity SoundEntityPrefab;

            void Execute(
                Entity entity,
                ref CarSoundData carSoundData,
                in CarModelComponent carModelComponent)
            {
                if ((carSoundData.SoundEntity == Entity.Null || carSoundData.CarSoundType != carSoundData.NewCarSoundType) && !carSoundData.WaitForInit)
                {
                    carSoundData.CarSoundType = carSoundData.NewCarSoundType;

                    if (carSoundData.SoundEntity != Entity.Null)
                    {
                        PoolEntityUtils.DestroyEntity(ref CommandBuffer, carSoundData.SoundEntity);
                    }

                    var localId = (int)carSoundData.NewCarSoundType;
                    var soundId = CarSharedDataConfigReference.Config.GetSoundID(carModelComponent.Value, localId);

                    var soundEntity = CommandBuffer.CreateTrackedSoundEntity(SoundEntityPrefab, soundId, entity);

                    CommandBuffer.SetComponent(soundEntity, new CarInitSoundEntity()
                    {
                        VehicleEntity = entity,
                    });

                    carSoundData.WaitForInit = true;
                }

                CommandBuffer.SetComponentEnabled<CarUpdateSound>(entity, false);
            }
        }
    }
}
