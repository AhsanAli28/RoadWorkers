using Spirit604.DotsCity.Simulation.Npc;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    [UpdateInGroup(typeof(SimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct NpcGroundStateSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<NpcStateComponent, AnimatorStateComponent>()
                .WithPresentRW<AnimatorFallingState>()
                .WithAll<NpcTypeComponent, PhysicsMass, GroundCasterRef, LocalTransform>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var stateJob = new StateJob()
            {
                GroundCasterLookup = SystemAPI.GetComponentLookup<GroundCasterComponent>(true),
                NpcGroundConfigReference = SystemAPI.GetSingleton<NpcGroundConfigReference>(),
            };

            stateJob.Schedule(updateQuery);
        }

        [WithAll(typeof(NpcTypeComponent))]
        [BurstCompile]
        public partial struct StateJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<GroundCasterComponent> GroundCasterLookup;

            [ReadOnly]
            public NpcGroundConfigReference NpcGroundConfigReference;

            void Execute(
                ref NpcStateComponent npcStateComponent,
                ref AnimatorStateComponent animatorStateComponent,
                EnabledRefRW<AnimatorFallingState> animatorFallingStateRW,
                in GroundCasterRef groundCasterRef)
            {
                var groundCasterComponent = GroundCasterLookup[groundCasterRef.CasterEntity];

                npcStateComponent.IsLanded = groundCasterComponent.Distance < NpcGroundConfigReference.Config.Value.GroundedDistance;

                if (!npcStateComponent.IsFalling)
                {
                    var isFalling = groundCasterComponent.Distance > NpcGroundConfigReference.Config.Value.FallingDistance;

                    if (!groundCasterComponent.Hit || isFalling)
                    {
                        npcStateComponent.IsGrounded = false;
                        npcStateComponent.IsFalling = true;

                        animatorStateComponent.ShortFalling = groundCasterComponent.Distance < NpcGroundConfigReference.Config.Value.StopFallingDistance;

                        animatorFallingStateRW.ValueRW = true;
                        return;
                    }
                    else
                    {
                        if (npcStateComponent.IsGrounded != npcStateComponent.IsLanded)
                        {
                            npcStateComponent.IsGrounded = npcStateComponent.IsLanded;
                        }
                    }
                }
                else
                {
                    if (groundCasterComponent.Distance < NpcGroundConfigReference.Config.Value.StopFallingDistance)
                    {
                        animatorStateComponent.StartedLanding = true;
                    }

                    if (animatorStateComponent.IsLanded)
                    {
                        npcStateComponent.IsFalling = false;
                        npcStateComponent.IsGrounded = true;
                        animatorStateComponent.IsLanded = false;
                        animatorStateComponent.StartedLanding = false;
                        animatorStateComponent.IsFalling = false;
                        animatorFallingStateRW.ValueRW = false;
                    }
                }
            }
        }
    }
}