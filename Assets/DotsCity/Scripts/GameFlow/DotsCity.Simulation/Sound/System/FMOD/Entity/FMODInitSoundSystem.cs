#if FMOD
using FMODUnity;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Sound
{
    [UpdateInGroup(typeof(StructuralInitGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct FMODInitSoundSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<FMODSound>()
                .WithAll<SoundComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
            state.RequireForUpdate<FMODSoundDataProviderSystem.Singleton>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var initializeFMODSoundJob = new InitializeFMODSoundJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                FMODSoundDataProvider = SystemAPI.GetSingleton<FMODSoundDataProviderSystem.Singleton>(),
                SoundDelayLookup = SystemAPI.GetComponentLookup<SoundDelayData>(true),
                SoundVolumeLookup = SystemAPI.GetComponentLookup<SoundVolume>(true),
                OneShotLookup = SystemAPI.GetComponentLookup<OneShot>(true),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            };

            initializeFMODSoundJob.Run();
        }

        [WithNone(typeof(FMODSound))]
        [WithAll(typeof(SoundComponent))]
        [BurstCompile]
        public partial struct InitializeFMODSoundJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public FMODSoundDataProviderSystem.Singleton FMODSoundDataProvider;

            [ReadOnly]
            public ComponentLookup<SoundDelayData> SoundDelayLookup;

            [ReadOnly]
            public ComponentLookup<SoundVolume> SoundVolumeLookup;

            [ReadOnly]
            public ComponentLookup<OneShot> OneShotLookup;

            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransformLookup;

            void Execute(
                Entity entity,
                in SoundComponent sound)
            {
                if (!FMODSoundDataProvider.SoundIdMapping.TryGetValue(sound.Id, out var runtimeSoundIndex))
                {
#if UNITY_EDITOR
                    UnityEngine.Debug.Log($"Sound not found id '{sound.Id}'");
#endif
                    return;
                }

                if (FMODSoundDataProvider.SoundDataArray.Length <= runtimeSoundIndex)
                {
#if UNITY_EDITOR
                    UnityEngine.Debug.Log($"Sound data not found runtime index '{runtimeSoundIndex}' Array length {FMODSoundDataProvider.SoundDataArray.Length}");
#endif
                    return;
                }

                var soundData = FMODSoundDataProvider.SoundDataArray[runtimeSoundIndex];

                ref var eventDescription = ref soundData.EventDescription;

                float volume = 1f;

                if (SoundVolumeLookup.HasComponent(entity))
                {
                    volume = SoundVolumeLookup[entity].Volume;
                }

                if (volume == 0)
                {
                    volume = 0.01f;
                }

                var result = eventDescription.createInstance(out var instance);

                bool delayedSound = SoundDelayLookup.HasComponent(entity);

                if (!delayedSound)
                {
                    result = instance.start();
                }

                instance.setVolume(volume);

                if (LocalTransformLookup.HasComponent(entity))
                {
                    var entityPosition = (Vector3)LocalTransformLookup[entity].Position;

                    if (entityPosition != Vector3.zero)
                    {
                        var posAttributes = entityPosition.To3DAttributes();
                        instance.set3DAttributes(posAttributes);
                    }
                }

                if (!delayedSound)
                {
                    if (OneShotLookup.HasComponent(entity))
                    {
                        instance.release();
                        CommandBuffer.DestroyEntity(entity);
                        return;
                    }
                }

                CommandBuffer.AddComponent(entity, new FMODSound()
                {
                    Event = instance
                });

                if (soundData.ParamCount > 0)
                {
                    int localParamIndex = 0;

                    var floatParameters = CommandBuffer.AddBuffer<FMODFloatParameter>(entity);

                    floatParameters.Capacity = soundData.ParamCount;
                    floatParameters.Length = soundData.ParamCount;

                    for (int paramIndex = soundData.StartParamIndex; paramIndex <= soundData.EndParamIndex; paramIndex++)
                    {
                        if (FMODSoundDataProvider.SoundParamDataArray.Length <= paramIndex)
                        {
#if UNITY_EDITOR
                            UnityEngine.Debug.Log($"FMODSoundSystem. Out of fmod param array size. ArraySize {FMODSoundDataProvider.SoundParamDataArray.Length} ParamIndex {paramIndex}");
#endif
                            continue;
                        }

                        var paramDescription = FMODSoundDataProvider.SoundParamDataArray[paramIndex];

                        floatParameters[localParamIndex] = new FMODFloatParameter()
                        {
                            ParameterId = paramDescription.id
                        };

                        localParamIndex++;
                    }
                }
            }
        }
    }
}
#endif