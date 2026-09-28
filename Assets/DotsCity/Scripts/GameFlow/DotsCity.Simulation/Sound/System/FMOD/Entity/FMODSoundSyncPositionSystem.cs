#if FMOD
using FMODUnity;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Sound
{
    [UpdateInGroup(typeof(PreEarlyJobGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct FMODSoundSyncPositionSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<TrackSoundComponent>()
                .WithAll<FMODSound>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var trackFMODSoundJob = new TrackFMODSoundJob()
            {
            };

            trackFMODSoundJob.Schedule();
        }

        [WithNone(typeof(TrackSoundComponent))]
        [BurstCompile]
        public partial struct TrackFMODSoundJob : IJobEntity
        {
            void Execute(
                in LocalTransform transform,
                in FMODSound fMODSound)
            {
                var posAttributes = ((Vector3)transform.Position).To3DAttributes();
                fMODSound.Event.set3DAttributes(posAttributes);
            }
        }
    }
}
#endif