using Spirit604.DotsCity.Core;
using Unity.Burst;
using Unity.Entities;

#if REESE_PATH
using Reese.Path;
using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Spirit604.Extensions;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
#endif

namespace Spirit604.DotsCity.Gameplay.Npc
{
    [UpdateInGroup(typeof(SimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct NpcInputSystem : ISystem
    {
#if REESE_PATH
        private const float minimumDistanceToTarget = 2f;
        private const float shootDistance = 6f;

        private const float AIM_POINT_LENGTH = 5f;
        private const float minAchieveDistance = 1f;
#endif

        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<NpcCustomDestinationComponent, NpcShouldEnterCarTag, NpcCustomReachComponent>()
                .WithAll<AliveTag, NpcTargetComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);

#if !REESE_PATH
            state.Enabled = false;
#endif
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
#if REESE_PATH
            var npcInputJob = new NpcInputJob()
            {
                PathPlanningLookup = SystemAPI.GetComponentLookup<PathPlanning>(true),
                UpdateNavTargetLookup = SystemAPI.GetComponentLookup<UpdateNavTargetTag>(false),
                timestamp = (float)SystemAPI.Time.ElapsedTime,
            };

            npcInputJob.Schedule();
#endif
        }

#if REESE_PATH
        [WithNone(typeof(NpcCustomDestinationComponent), typeof(NpcShouldEnterCarTag), typeof(NpcCustomReachComponent))]
        [WithAll(typeof(AliveTag))]
        [BurstCompile]
        public partial struct NpcInputJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<PathPlanning> PathPlanningLookup;

            public ComponentLookup<UpdateNavTargetTag> UpdateNavTargetLookup;

            [ReadOnly]
            public float timestamp;

            void Execute(
                Entity entity,
                ref InputComponent inputComponent,
                ref NpcNavAgentComponent npcNavAgentComponent,
                ref NavAgentComponent navAgentComponent,
                in NavAgentSteeringComponent navAgentSteeringComponent,
                in NpcTargetComponent npcTargetComponent,
                in LocalToWorld worldTransform)
            {
                float3 npcPosition = worldTransform.Position.Flat();

                if (npcTargetComponent.HasMovementTarget)
                {
                    float distanceToTarget = npcTargetComponent.MovementTargetDistance;

                    if (distanceToTarget > minimumDistanceToTarget && npcNavAgentComponent.PathCalculated == 0)
                    {
                        npcNavAgentComponent.PathCalculated = 1;
                        var targetPosition = npcTargetComponent.MovementTargetPosition;

                        bool pathPlanning = PathPlanningLookup.HasComponent(entity) && PathPlanningLookup.IsComponentEnabled(entity);

                        if (!pathPlanning && !navAgentComponent.PathEndPosition.IsEqual(targetPosition, 0.1f))
                        {
                            navAgentComponent.PathEndPosition = targetPosition;
                            UpdateNavTargetLookup.SetComponentEnabled(entity, true);
                        }
                    }
                    else if (navAgentComponent.RemainingDistance < minAchieveDistance)
                    {
                        inputComponent.MovingInput = float2.zero;
                        npcNavAgentComponent.PathCalculated = 0;
                    }
                    else
                    {
                        inputComponent.MovingInput = math.normalize(navAgentSteeringComponent.SteeringTargetValue.Flat() - npcPosition).To2DSpace();
                    }
                }
                else
                {
                    inputComponent.MovingInput = float2.zero;
                    npcNavAgentComponent.PathCalculated = 0;
                }

                HandleShooting(ref inputComponent, npcTargetComponent, worldTransform.Position);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HandleShooting(ref InputComponent inputComponent, in NpcTargetComponent npcTargetComponent, Vector3 npcPosition)
        {
            if (ShouldShoot(npcTargetComponent))
            {
                inputComponent.MovingInput = float2.zero;
                inputComponent.ShootDirection = GetShootDirection(npcTargetComponent.ShootingTargetPosition, npcPosition);
            }
            else
            {
                inputComponent.ShootDirection = float3.zero;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float3 GetShootDirection(float3 targetPoint, float3 npcPosition)
        {
            var shootDirection = math.normalize(targetPoint - npcPosition);
            return shootDirection;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldShoot(in NpcTargetComponent npcTargetComponent)
        {
            return npcTargetComponent.HasShootingTarget && npcTargetComponent.ShootingTargetDistance < shootDistance;
        }
#endif
    }
}