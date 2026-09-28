using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    [UpdateInGroup(typeof(SimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct EnemyNpcTargetSystem : ISystem
    {
        private const float targetOffset = 1f;
        private const float minDistanceToMove = 1f;

        private EntityQuery enemyQuery;
        private EntityQuery targetQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            enemyQuery = SystemAPI.QueryBuilder()
                .WithAll<EnemyNpcTag, EnemyNpcCombatStateTag>()
                .Build();

            targetQuery = SystemAPI.QueryBuilder()
               .WithAll<AliveTag, PlayerMobTag, LocalToWorld>()
                .Build();

            state.RequireForUpdate(enemyQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var targetPositions = targetQuery.ToComponentDataListAsync<LocalToWorld>(Allocator.TempJob, out var depList1);
            var targetEntities = targetQuery.ToEntityListAsync(Allocator.TempJob, out var depList2);

            var dep = JobHandle.CombineDependencies(depList1, depList2);

            var targetJob = new TargetJob()
            {
                TargetPositions = targetPositions,
                TargetEntities = targetEntities,
            };

            state.Dependency = targetJob.Schedule(dep);

            targetPositions.Dispose(state.Dependency);
            targetEntities.Dispose(state.Dependency);
        }

        [WithAll(typeof(EnemyNpcTag), typeof(EnemyNpcCombatStateTag))]
        [BurstCompile]
        public partial struct TargetJob : IJobEntity
        {
            [ReadOnly]
            public NativeList<LocalToWorld> TargetPositions;

            [ReadOnly]
            public NativeList<Entity> TargetEntities;

            void Execute(
                ref EnemyNpcTargetComponent enemyNpcTargetComponent,
                ref NpcTargetComponent npcTargetComponent,
                in LocalToWorld worldTransform)
            {
                float maxDistance = float.MaxValue;

                for (int i = 0; i < TargetPositions.Length; i++)
                {
                    var distance = math.distance(worldTransform.Position, TargetPositions[i].Position);

                    if (distance < maxDistance)
                    {
                        maxDistance = distance;
                        enemyNpcTargetComponent.Target = TargetEntities[i];

                        npcTargetComponent.ShootingTargetPosition = TargetPositions[i].Position;
                        npcTargetComponent.ShootingTargetDistance = distance;
                        npcTargetComponent.HasShootingTarget = true;

                        var movementTarget = GetMovementTargetPosition(npcTargetComponent.ShootingTargetPosition, worldTransform.Position);

                        npcTargetComponent.MovementTargetPosition = movementTarget;
                        var movementDistance = math.distance(worldTransform.Position, movementTarget);

                        npcTargetComponent.MovementTargetDistance = movementDistance;

                        npcTargetComponent.HasMovementTarget = movementDistance >= minDistanceToMove;
                    }
                }
            }

            private static float3 GetMovementTargetPosition(float3 target, float3 npcPosition)
            {
                var directionToTarget = math.normalize(target - npcPosition);

                var targetPosition = target - directionToTarget * targetOffset;

                return targetPosition;
            }
        }
    }
}