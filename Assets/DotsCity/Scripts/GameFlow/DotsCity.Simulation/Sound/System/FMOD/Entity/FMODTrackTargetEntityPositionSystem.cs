#if FMOD
using FMODUnity;
using Spirit604.DotsCity.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Sound
{
    [UpdateInGroup(typeof(LateSimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct FMODTrackTargetEntityPositionSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<FMODSound, TrackSoundComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var trackTargetFMODSoundJob = new TrackTargetFMODSoundJob()
            {
                WorldTransformLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true)
            };

            trackTargetFMODSoundJob.Schedule();
        }

        [WithDisabled(typeof(PooledEventTag))]
        [BurstCompile]
        public partial struct TrackTargetFMODSoundJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<LocalToWorld> WorldTransformLookup;

            void Execute(
                EnabledRefRW<PooledEventTag> pooledEventTagRW,
                in TrackSoundComponent trackSoundComponent,
                in FMODSound fMODSound)
            {
                if (WorldTransformLookup.HasComponent(trackSoundComponent.TargetEntity))
                {
                    var position = WorldTransformLookup[trackSoundComponent.TargetEntity].Position;
                    var posAttributes = ((Vector3)position).To3DAttributes();
                    fMODSound.Event.set3DAttributes(posAttributes);
                }
                else
                {
                    PoolEntityUtils.DestroyEntity(ref pooledEventTagRW);
                }
            }
        }
    }
}
#endif