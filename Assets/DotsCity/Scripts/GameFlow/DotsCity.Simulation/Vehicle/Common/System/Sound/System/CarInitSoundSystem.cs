using Spirit604.DotsCity.Core;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car.Sound
{
    [UpdateInGroup(typeof(EarlyEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CarInitSoundSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithDisabled<PooledEventTag>()
                .WithAll<CarInitSoundEntityTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var initSoundJob = new InitSoundJob()
            {
                CarSoundDataLookup = SystemAPI.GetComponentLookup<CarSoundData>(false),
            };

            initSoundJob.Schedule();
        }

        [WithDisabled(typeof(PooledEventTag))]
        [BurstCompile]
        public partial struct InitSoundJob : IJobEntity
        {
            public ComponentLookup<CarSoundData> CarSoundDataLookup;

            void Execute(
                Entity entity,
                ref CarInitSoundEntity carInitSound,
                EnabledRefRW<CarInitSoundEntityTag> carInitSoundEntityTagRW,
                EnabledRefRW<PooledEventTag> pooledEventTagRW)
            {
                if (carInitSound.Initialized)
                    return;

                carInitSound.Initialized = true;

                if (CarSoundDataLookup.HasComponent(carInitSound.VehicleEntity))
                {
                    var carSoundData = CarSoundDataLookup[carInitSound.VehicleEntity];
                    carSoundData.SoundEntity = entity;
                    carSoundData.WaitForInit = false;
                    CarSoundDataLookup[carInitSound.VehicleEntity] = carSoundData;

                    carInitSoundEntityTagRW.ValueRW = false;
                }
                else
                {
                    PoolEntityUtils.DestroyEntity(ref pooledEventTagRW);
                }
            }
        }
    }
}
