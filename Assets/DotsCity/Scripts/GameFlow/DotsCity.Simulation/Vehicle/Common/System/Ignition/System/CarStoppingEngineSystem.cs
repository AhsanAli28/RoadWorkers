using Spirit604.DotsCity.Simulation.Car.Sound;
using Spirit604.DotsCity.Simulation.Sound;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Car
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CarStoppingEngineSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<CarStoppingEngineStartedTag, CarIgnitionData>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var stoppingEngineJob = new StoppingEngineJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                CarRemoveDriverAfterStopEngineLookup = SystemAPI.GetComponentLookup<CarRemoveDriverAfterStopEngineTag>(true),
                SoundVolumeLookup = SystemAPI.GetComponentLookup<SoundVolume>(false),
                SoundLevelConfigReference = SystemAPI.GetSingleton<SoundLevelConfigReference>(),
                CarSharedDataConfigReference = SystemAPI.GetSingleton<CarSharedDataConfigReference>(),
                CarStopEngineConfigReference = SystemAPI.GetSingleton<CarStopEngineConfigReference>(),
                CurrentTime = (float)SystemAPI.Time.ElapsedTime,
            };

            stoppingEngineJob.Run();
        }

        [WithAll(typeof(CarStoppingEngineStartedTag))]
        [BurstCompile]
        public partial struct StoppingEngineJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public ComponentLookup<CarRemoveDriverAfterStopEngineTag> CarRemoveDriverAfterStopEngineLookup;

            public ComponentLookup<SoundVolume> SoundVolumeLookup;

            [ReadOnly]
            public SoundLevelConfigReference SoundLevelConfigReference;

            [ReadOnly]
            public CarSharedDataConfigReference CarSharedDataConfigReference;

            [ReadOnly]
            public CarStopEngineConfigReference CarStopEngineConfigReference;

            [ReadOnly]
            public float CurrentTime;

            void Execute(
                Entity entity,
                ref CarIgnitionData carIgnitionData,
                ref CarSoundData carSoundData,
                in CarModelComponent carModelComponent)
            {
                var updateSound = SoundLevelConfigReference.Config.Value.TrafficHasSounds && (carSoundData.CarSoundType != CarSoundType.Driving || carSoundData.SoundEntity == Entity.Null);

                if (updateSound)
                {
                    carSoundData.NewCarSoundType = CarSoundType.Driving;

                    CommandBuffer.SetComponentEnabled<CarUpdateSound>(entity, true);
                }

                float stoppingDuration = CarStopEngineConfigReference.Config.Value.StoppingDuration;
                float idleAfterStopping = CarStopEngineConfigReference.Config.Value.IdleAfterStopping;

                if (carIgnitionData.EndTime == 0)
                {
                    carIgnitionData.EndTime = CurrentTime + stoppingDuration + idleAfterStopping;
                    carIgnitionData.IgnitionState = IgnitionState.EngineStopping;
                }

                float remainTime = carIgnitionData.EndTime - CurrentTime;

                if (SoundLevelConfigReference.Config.Value.TrafficHasSounds && carSoundData.CarSoundType == CarSoundType.Driving && SoundVolumeLookup.HasComponent(carSoundData.SoundEntity))
                {
                    var soundVolume = SoundVolumeLookup[carSoundData.SoundEntity];

                    if (!carIgnitionData.EngineStarted)
                    {
                        float t = (idleAfterStopping + stoppingDuration - remainTime) / (stoppingDuration);

                        float minPitch = CarStopEngineConfigReference.Config.Value.TargetMinPitch;
                        float minVolume = CarStopEngineConfigReference.Config.Value.TargetMinVolume;

                        var engineData = CarSharedDataConfigReference.GetEngineData(carModelComponent.Value);

                        var pitch = math.lerp(engineData.MinPitch, minPitch, t);
                        var currentVolume = math.lerp(engineData.MinVolume, minVolume, t);

                        soundVolume.Pitch = pitch;
                        soundVolume.Volume = currentVolume;

                        if (remainTime <= idleAfterStopping)
                        {
                            carIgnitionData.EngineStarted = true;
                            soundVolume.Volume = 0;
                        }

                        SoundVolumeLookup[carSoundData.SoundEntity] = soundVolume;
                    }
                }

                if (CurrentTime >= carIgnitionData.EndTime || carIgnitionData.IgnitionState != IgnitionState.EngineStopping)
                {
                    carIgnitionData.EngineStarted = false;
                    CommandBuffer.RemoveComponent<CarStoppingEngineStartedTag>(entity);

                    if (carIgnitionData.IgnitionState == IgnitionState.EngineStopping)
                    {
                        carIgnitionData.EndTime = 0;
                        carIgnitionData.IgnitionState = IgnitionState.Default;

                        if (!CarRemoveDriverAfterStopEngineLookup.HasComponent(entity))
                        {
                            CommandBuffer.SetComponentEnabled<TrafficEnteredTriggerNodeTag>(entity, true);
                        }
                        else
                        {
                            CommandBuffer.RemoveComponent<CarEngineStartedTag>(entity);
                            CommandBuffer.RemoveComponent<CarCustomEnginePitchTag>(entity);
                        }
                    }
                    else if (carIgnitionData.IgnitionState != IgnitionState.EngineStarted)
                    {
                        CommandBuffer.RemoveComponent<CarEngineStartedTag>(entity);
                    }
                }
            }
        }
    }
}