using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car.Sound;
using Spirit604.DotsCity.Simulation.Sound;
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
    public partial struct CarIgnitionStateSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<CarIgnitionStartedTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var ignitionJob = new IgnitionJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                CarIgnitionConfigReference = SystemAPI.GetSingleton<CarIgnitionConfigReference>(),
                SoundVolumeLookup = SystemAPI.GetComponentLookup<SoundVolume>(false),
                SoundLevelConfigReference = SystemAPI.GetSingleton<SoundLevelConfigReference>(),
                CarSharedDataConfigReference = SystemAPI.GetSingleton<CarSharedDataConfigReference>(),
                CurrentTime = (float)SystemAPI.Time.ElapsedTime,
            };

            ignitionJob.Run();
        }

        [WithAll(typeof(CarIgnitionStartedTag))]
        [BurstCompile]
        public partial struct IgnitionJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public CarIgnitionConfigReference CarIgnitionConfigReference;

            public ComponentLookup<SoundVolume> SoundVolumeLookup;

            [ReadOnly]
            public SoundLevelConfigReference SoundLevelConfigReference;

            [ReadOnly]
            public CarSharedDataConfigReference CarSharedDataConfigReference;

            [ReadOnly]
            public float CurrentTime;

            void Execute(
                Entity entity,
                ref CarIgnitionData carIgnitionData,
                ref CarSoundData carSoundData,
                in CarModelComponent carModelComponent)
            {
                float remainTime = carIgnitionData.EndTime - CurrentTime;
                float startedTimeDuration = CarIgnitionConfigReference.Config.Value.StartedTimeDuration;

                switch (carIgnitionData.IgnitionState)
                {
                    case IgnitionState.Default:
                        {
                            float ignitionDuration = CarIgnitionConfigReference.Config.Value.IdleBeforeStart;
                            carIgnitionData.EndTime = CurrentTime + ignitionDuration;
                            carIgnitionData.IgnitionState = IgnitionState.IdleBeforeIgnite;
                            break;
                        }
                    case IgnitionState.IdleBeforeIgnite:
                        {
                            if (CurrentTime >= carIgnitionData.EndTime)
                            {
                                float ignitionDuration = CarIgnitionConfigReference.Config.Value.IgnitionDuration;
                                carIgnitionData.EndTime = CurrentTime + ignitionDuration;
                                carIgnitionData.IgnitionState = IgnitionState.Ignition;

                                if (SoundLevelConfigReference.Config.Value.TrafficHasSounds)
                                {
                                    carSoundData.NewCarSoundType = CarSoundType.Ignition;
                                    CommandBuffer.SetComponentEnabled<CarUpdateSound>(entity, true);
                                }
                            }

                            break;
                        }
                    case IgnitionState.Ignition:
                        {
                            if (startedTimeDuration > 0)
                            {
                                if (remainTime <= startedTimeDuration)
                                {
                                    carIgnitionData.IgnitionState = IgnitionState.EngineStarted;

                                    if (SoundLevelConfigReference.Config.Value.TrafficHasSounds)
                                    {
                                        carSoundData.NewCarSoundType = CarSoundType.Driving;
                                        CommandBuffer.SetComponentEnabled<CarUpdateSound>(entity, true);
                                    }
                                }
                            }
                            else
                            {
                                carIgnitionData.IgnitionState = IgnitionState.EngineStarted;
                            }

                            break;
                        }
                    case IgnitionState.EngineStarted:
                        {
                            if (SoundLevelConfigReference.Config.Value.TrafficHasSounds && startedTimeDuration > 0 && carSoundData.CarSoundType == CarSoundType.Driving && SoundVolumeLookup.HasComponent(carSoundData.SoundEntity))
                            {
                                float t = (startedTimeDuration - remainTime) / startedTimeDuration;

                                float maxPitch = CarIgnitionConfigReference.Config.Value.MaxPitch;
                                float maxVolume = CarIgnitionConfigReference.Config.Value.MaxVolume;

                                ref var pitchBlob = ref CarIgnitionConfigReference.Config.Value.EngineStartedPitchCurve;
                                ref var volumeblob = ref CarIgnitionConfigReference.Config.Value.EngineStartedVolumeCurve;

                                float t1 = BlobCurveUtils.Evaluate(ref pitchBlob, t);
                                float t2 = BlobCurveUtils.Evaluate(ref volumeblob, t);

                                var engineData = CarSharedDataConfigReference.GetEngineData(carModelComponent.Value);

                                var pitch = math.lerp(engineData.MinPitch, maxPitch, t1);
                                var currentVolume = math.lerp(engineData.MinVolume, maxVolume, t2);

                                var soundVolume = SoundVolumeLookup[carSoundData.SoundEntity];
                                soundVolume.Pitch = pitch;
                                soundVolume.Volume = currentVolume;
                                SoundVolumeLookup[carSoundData.SoundEntity] = soundVolume;
                            }

                            if (CurrentTime >= carIgnitionData.EndTime)
                            {
                                carIgnitionData.EndTime = 0;
                                carIgnitionData.IgnitionState = IgnitionState.Default;

                                CommandBuffer.RemoveComponent<CarIgnitionStartedTag>(entity);
                                CommandBuffer.RemoveComponent<CarCustomEnginePitchTag>(entity);
                                CommandBuffer.AddComponent<CarEngineStartedTag>(entity);
                            }

                            break;
                        }
                }
            }
        }
    }
}