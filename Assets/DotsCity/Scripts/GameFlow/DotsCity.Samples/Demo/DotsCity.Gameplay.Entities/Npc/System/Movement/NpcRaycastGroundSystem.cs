using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    [UpdateInGroup(typeof(RaycastGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct NpcRaycastGroundSystem : ISystem
    {
        private EntityQuery raycastGroupQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            raycastGroupQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<GroundCasterComponent>()
                .Build(state.EntityManager);

            state.RequireForUpdate(raycastGroupQuery);
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var raycastObstacleJob = new RaycastObstacleJob()
            {
                CollisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CollisionWorld,
                NpcGroundConfigReference = SystemAPI.GetSingleton<NpcGroundConfigReference>(),
            };

            raycastObstacleJob.Schedule();
        }

        [BurstCompile]
        public partial struct RaycastObstacleJob : IJobEntity
        {
            [ReadOnly]
            public CollisionWorld CollisionWorld;

            [ReadOnly]
            public NpcGroundConfigReference NpcGroundConfigReference;

            void Execute(
                ref GroundCasterComponent casterComponent,
                in LocalToWorld worldTransform)
            {
                var currentCollisionFilter = new CollisionFilter()
                {
                    CollidesWith = casterComponent.CastingLayer,
                    BelongsTo = casterComponent.CastingLayer,
                    GroupIndex = 0
                };

                var castDistance = NpcGroundConfigReference.Config.Value.CastDistance;
                var origin = worldTransform.Position;
                var end = worldTransform.Position - new float3(0, castDistance, 0);

                var castInput = new RaycastInput()
                {
                    Start = origin,
                    End = end,
                    Filter = currentCollisionFilter
                };

                float distance = float.MaxValue;
                bool hit = false;

                if (CollisionWorld.CastRay(castInput, out var hitInfo))
                {
                    distance = hitInfo.Fraction * castDistance;
                    hit = true;
                }

                casterComponent.Distance = distance;
                casterComponent.Hit = hit;
            }
        }
    }
}