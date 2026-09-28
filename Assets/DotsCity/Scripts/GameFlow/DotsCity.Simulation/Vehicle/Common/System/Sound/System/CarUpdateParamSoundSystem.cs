#if FMOD
using Spirit604.DotsCity.Simulation.Sound;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car.Sound
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CarUpdateParamSoundSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<CarUpdateParamSound>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var updateParamSoundJob = new UpdateParamSoundJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                FMODFloatParameterLookup = SystemAPI.GetBufferLookup<FMODFloatParameter>(true),
            };

            updateParamSoundJob.Schedule();
        }

        [WithAll(typeof(CarUpdateParamSound))]
        [BurstCompile]
        public partial struct UpdateParamSoundJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public BufferLookup<FMODFloatParameter> FMODFloatParameterLookup;

            void Execute(
                Entity entity,
                ref CarSoundData carSoundData,
                in CarUpdateParamSound carUpdateParamSound)
            {
                if (FMODFloatParameterLookup.HasBuffer(carSoundData.SoundEntity))
                {
                    DynamicBuffer<FMODFloatParameter> parameters = FMODFloatParameterLookup[carSoundData.SoundEntity];

                    if (parameters.Length > carUpdateParamSound.ParamId)
                    {
                        var floatParam = parameters[carUpdateParamSound.ParamId];
                        floatParam.CurrentValue = carUpdateParamSound.ParamValue;
                        parameters[carUpdateParamSound.ParamId] = floatParam;
                    }
                }

                CommandBuffer.RemoveComponent<CarUpdateParamSound>(entity);
            }
        }
    }
}
#endif