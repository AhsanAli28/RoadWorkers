using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Sound;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Car.Sound
{
    [UpdateInGroup(typeof(HashMapGroup), OrderLast = true)]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CarSoundSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<CarCustomEnginePitchTag>()
                .WithDisabled<CarUpdateSound>()
                .WithAll<CarEngineStartedTag, HasSoundTag, AliveTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var soundJob = new SoundJob()
            {
                SoundVolumeLookup = SystemAPI.GetComponentLookup<SoundVolume>(false),
                CarSharedDataConfigReference = SystemAPI.GetSingleton<CarSharedDataConfigReference>(),
            };

            soundJob.Schedule();
        }

        [WithNone(typeof(CarCustomEnginePitchTag))]
        [WithDisabled(typeof(CarUpdateSound))]
        [WithAll(typeof(CarEngineStartedTag), typeof(HasSoundTag), typeof(AliveTag))]
        [BurstCompile]
        public partial struct SoundJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<SoundVolume> SoundVolumeLookup;

            [ReadOnly]
            public CarSharedDataConfigReference CarSharedDataConfigReference;

            void Execute(
                ref CarSoundData carSoundData,
                EnabledRefRW<CarUpdateSound> carUpdateSoundRw,
                in SpeedComponent speedComponent,
                in CarModelComponent carModelComponent)
            {
                var newCarSoundType = CarSoundType.Driving;

                var updateSound = newCarSoundType != carSoundData.CarSoundType || carSoundData.SoundEntity == Entity.Null;

                if (updateSound)
                {
                    carSoundData.NewCarSoundType = newCarSoundType;
                    carUpdateSoundRw.ValueRW = true;
                }

                var engineData = CarSharedDataConfigReference.GetEngineData(carModelComponent.Value);

                float t = math.clamp(speedComponent.ValueAbs / engineData.MaxLoadSpeed, 0, 1);
                float t2 = math.clamp(speedComponent.ValueAbs / engineData.MaxVolumeSpeed, 0, 1);

                float pitch = math.lerp(engineData.MinPitch, engineData.MaxPitch, t);
                float currentVolume = math.lerp(engineData.MinVolume, 1f, t2);

                if (carSoundData.CarSoundType == CarSoundType.Driving && SoundVolumeLookup.HasComponent(carSoundData.SoundEntity))
                {
                    var soundVolume = SoundVolumeLookup[carSoundData.SoundEntity];

                    soundVolume.Pitch = pitch;
                    soundVolume.Volume = currentVolume;

                    SoundVolumeLookup[carSoundData.SoundEntity] = soundVolume;
                }
            }
        }
    }
}
