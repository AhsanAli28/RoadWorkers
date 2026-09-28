using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Sound;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car.Sound
{
    [UpdateInGroup(typeof(PreEarlyJobGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CarEnableSoundSystem : ISystem
    {
        private EntityQuery enableSoundQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            enableSoundQuery = SystemAPI.QueryBuilder()
                .WithDisabled<HasSoundTag>()
                .WithAll<InViewOfCameraTag, HasDriverTag, CarSoundData>()
                .Build();

            state.RequireForUpdate(enableSoundQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var enableSoundJob = new EnableSoundJob()
            {
            };

            enableSoundJob.Schedule();
        }

        [WithDisabled(typeof(HasSoundTag))]
        [WithAll(typeof(InViewOfCameraTag), typeof(HasDriverTag))]
        [BurstCompile]
        public partial struct EnableSoundJob : IJobEntity
        {
            void Execute(
                EnabledRefRW<HasSoundTag> hasSoundTagRW)
            {
                hasSoundTagRW.ValueRW = true;
            }
        }
    }
}
