using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Npc;
using Spirit604.DotsCity.Gameplay.Weapon;
using Spirit604.Extensions;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace Spirit604.DotsCity.Gameplay.Player
{
    [UpdateAfter(typeof(PlayerTargetSystem))]
    [UpdateInGroup(typeof(LateSimulationGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class PlayerMobTargetSystem : SystemBase
    {
        private const float minumumRandomDistanceForTarget = 1F;
        private const float maximumRandomDistanceForTarget = 1.9F;
        private const float maxPreviousPlayerPositionOffset = 0.2f;

        private const float FIXED_ANGLE = 40F;
        private const float RANDOM_ANGLE = 15F;
        private const float lockRandomTime = 2f;

        private EntityQuery crossHairQuery;
        private EntityQuery playerQuery;
        private EntityQuery playerMobQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            crossHairQuery = new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
                .WithAll<CrossHairComponent, LocalToWorld>()
                .Build(this);

            playerQuery = new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
                .WithAll<PlayerTag, InputComponent, LocalToWorld>()
                .Build(this);

            playerMobQuery = new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
                .WithNone<PlayerTag>()
                .WithAll<PlayerMobNpcComponent, NpcTargetComponent>()
                .Build(this);

            RequireForUpdate(playerQuery);
            RequireForUpdate(playerMobQuery);
        }

        protected override void OnUpdate()
        {
            if (playerQuery.CalculateEntityCount() != 1 || crossHairQuery.CalculateEntityCount() == 0)
                return;

            var playerInputComponent = playerQuery.GetSingleton<InputComponent>();
            var playerIsShooting = !playerInputComponent.ShootInput.Equals(float3.zero);
            var crossHairEntityLocal = crossHairQuery.GetSingletonEntity();

            var playerPosition = playerQuery.GetSingleton<LocalToWorld>().Position;
            float timestamp = (float)SystemAPI.Time.ElapsedTime;
            var seed = MathUtilMethods.GetRandomSeed();

            Entities
            .WithBurst()
            .WithNone<PlayerTag>()
            .ForEach((
                Entity entity,
                ref NpcTargetComponent npcTargetComponent,
                ref PlayerMobNpcComponent mobNpcComponent,
                in LocalTransform transform) =>
            {
                float3 npcPosition = transform.Position.Flat();

                var distanceToPreviousPlayer = math.distance(playerPosition, mobNpcComponent.PreviousPlayerPosition);
                var distanceToPlayer = math.distance(playerPosition, npcPosition);

                bool closeToTarget = distanceToPreviousPlayer <= maxPreviousPlayerPositionOffset && distanceToPlayer <= maximumRandomDistanceForTarget;

                if (!closeToTarget)
                {
                    var targetPosition = GetRandomTargetPosition(ref mobNpcComponent, playerPosition, npcPosition, mobNpcComponent.SideIndex, timestamp, seed);
                    npcTargetComponent.MovementTargetPosition = targetPosition;
                    var movementTargetDistance = math.distance(transform.Position, npcTargetComponent.MovementTargetPosition);
                    npcTargetComponent.MovementTargetDistance = movementTargetDistance;
                }

                npcTargetComponent.HasMovementTarget = !closeToTarget;

                float3 shootTargetPosition = SystemAPI.GetComponent<LocalToWorld>(crossHairEntityLocal).Position;
                var shootingTargetDistance = math.distance(transform.Position, shootTargetPosition);
                npcTargetComponent.ShootingTargetDistance = shootingTargetDistance;
                npcTargetComponent.ShootingTargetPosition = shootTargetPosition;
                npcTargetComponent.HasShootingTarget = playerIsShooting;

            }).Schedule();
        }

        private static float3 GetRandomTargetPosition(ref PlayerMobNpcComponent mobNpcComponent, float3 playerPosition, float3 npcPosition, int sideIndex, float time, uint seed)
        {
            float randomDistanceFromTarget = (minumumRandomDistanceForTarget + maximumRandomDistanceForTarget) / 2;
            float randomAngle = 0;

            if (time > mobNpcComponent.NextUnlockTime)
            {
                Random random = new Random(seed);

                mobNpcComponent.NextUnlockTime = time + lockRandomTime;
                randomDistanceFromTarget = random.NextFloat(minumumRandomDistanceForTarget, maximumRandomDistanceForTarget);
                randomAngle = random.NextFloat(-RANDOM_ANGLE, RANDOM_ANGLE);
                mobNpcComponent.PreviousPlayerPosition = playerPosition;
            }

            var directionToTarget = math.normalize(playerPosition - npcPosition);
            directionToTarget = directionToTarget * randomDistanceFromTarget;

            var yEulerAngle = randomAngle + FIXED_ANGLE * sideIndex;

            var orientation = quaternion.Euler(0, yEulerAngle, 0);
            var targetPosition = playerPosition - math.mul(orientation, directionToTarget);

            return targetPosition;
        }
    }
}
