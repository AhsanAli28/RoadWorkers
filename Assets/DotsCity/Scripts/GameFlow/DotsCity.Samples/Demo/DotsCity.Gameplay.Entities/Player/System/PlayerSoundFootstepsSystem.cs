using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Npc;
using Spirit604.DotsCity.Simulation.Sound;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Sound.Player
{
    [UpdateInGroup(typeof(SimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct PlayerSoundFootstepsSystem : ISystem
    {
        private EntityQuery playerQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            playerQuery = SystemAPI.QueryBuilder()
                .WithAll<PlayerTag, PlayerSoundComponent>()
                .Build();

            state.RequireForUpdate(playerQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var playerFootstepsSoundJob = new PlayerFootstepsSoundJob()
            {
                SoundEventLookup = SystemAPI.GetComponentLookup<SoundEventComponent>(false),
                CurrentTimestamp = (float)SystemAPI.Time.ElapsedTime
            };

            playerFootstepsSoundJob.Schedule();
        }

        [WithAll(typeof(PlayerTag))]
        [BurstCompile]
        public partial struct PlayerFootstepsSoundJob : IJobEntity
        {
            public ComponentLookup<SoundEventComponent> SoundEventLookup;

            [ReadOnly]
            public float CurrentTimestamp;

            void Execute(
                ref PlayerSoundComponent playerSoundComponent,
                in AnimatorMovementComponent animatorMovementComponent)
            {
                var shouldPlay = animatorMovementComponent.CurrentForwardLerp > 0.2f;

                if (SoundEventLookup.HasComponent(playerSoundComponent.FootstepsSound))
                {
                    playerSoundComponent.FootstepsPlaying = shouldPlay;
                    var soundEvent = SoundEventLookup[playerSoundComponent.FootstepsSound];

                    bool changed = false;

                    if (shouldPlay)
                    {
                        if (CurrentTimestamp >= playerSoundComponent.FootstepTimestep)
                        {
                            var mult = (2 - animatorMovementComponent.CurrentForwardLerp);
                            playerSoundComponent.FootstepTimestep = CurrentTimestamp + playerSoundComponent.FootstepFrequency * mult;
                            changed = soundEvent.SetEvent(SoundEventType.Play);
                        }
                    }
                    else
                    {
                        changed = soundEvent.SetEvent(SoundEventType.StopFadeout);
                    }

                    if (changed)
                    {
                        SoundEventLookup[playerSoundComponent.FootstepsSound] = soundEvent;
                    }
                }
            }
        }
    }
}
