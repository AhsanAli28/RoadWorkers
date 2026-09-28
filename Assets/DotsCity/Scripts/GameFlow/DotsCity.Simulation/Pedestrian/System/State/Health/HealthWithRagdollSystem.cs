using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Spirit604.DotsCity.Simulation.Sound;
using Spirit604.DotsCity.Simulation.Sound.Pedestrian;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct HealthWithRagdollSystem : ISystem
    {
        private EntityQuery updateGroup;

        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithDisabled<AliveTag>()
                .WithDisabledRW<RagdollActivateEventTag>()
                .WithAll<StateComponent, RagdollComponent>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var activateRagdollJob = new ActivateRagdollJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                SoundEventQueue = SystemAPI.GetSingleton<SoundEventPlaybackSystem.Singleton>(),
                InViewOfCameraLookup = SystemAPI.GetComponentLookup<InViewOfCameraTag>(true),
                SoundConfigReference = SystemAPI.GetSingleton<SoundConfigReference>(),
            };

            activateRagdollJob.Run();
        }

        [WithDisabled(typeof(AliveTag), typeof(RagdollActivateEventTag))]
        [BurstCompile]
        public partial struct ActivateRagdollJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [NativeDisableContainerSafetyRestriction]
            public SoundEventPlaybackSystem.Singleton SoundEventQueue;

            [ReadOnly]
            public ComponentLookup<InViewOfCameraTag> InViewOfCameraLookup;

            [ReadOnly]
            public SoundConfigReference SoundConfigReference;

            void Execute(
                Entity entity,
                ref RagdollComponent ragdollComponent,
                ref HealthComponent healthComponent,
                EnabledRefRW<RagdollActivateEventTag> ragdollActivateEventTagRW,
                in LocalTransform transform)
            {
                ragdollComponent.Position = transform.Position;
                ragdollComponent.Rotation = transform.Rotation;
                ragdollComponent.ForceDirection = healthComponent.HitDirection;
                ragdollComponent.ForceMultiplier = healthComponent.ForceMultiplier;
                ragdollComponent.DamageDealerID = healthComponent.DamageDealerID;

                if (math.isnan(ragdollComponent.Position.x))
                    return;

                bool pooled = InViewOfCameraLookup.HasComponent(entity) && !InViewOfCameraLookup.IsComponentEnabled(entity);

                if (pooled)
                {
                    PoolEntityUtils.DestroyEntity(ref CommandBuffer, entity);
                    return;
                }

                ragdollActivateEventTagRW.ValueRW = true;

                var soundId = SoundConfigReference.Config.Value.DeathSoundId;
                SoundEventQueue.PlayOneShot(soundId, transform.Position);
            }
        }
    }
}