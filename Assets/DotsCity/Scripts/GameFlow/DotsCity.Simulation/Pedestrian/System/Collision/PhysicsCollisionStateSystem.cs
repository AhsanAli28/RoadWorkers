using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(SimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct PhysicsCollisionStateSystem : ISystem
    {
        private const float MinCollisionTime = 0.3f;

        private EntityQuery npcQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            npcQuery = SystemAPI.QueryBuilder()
                .WithAll<HasCollisionTag, CollisionComponent>()
                .Build();

            state.RequireForUpdate(npcQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var stateCollisionJob = new StateCollisionJob()
            {
                Timestamp = (float)SystemAPI.Time.ElapsedTime,
            };

            stateCollisionJob.Schedule();
        }

        [WithAll(typeof(HasCollisionTag))]
        [BurstCompile]
        public partial struct StateCollisionJob : IJobEntity
        {
            [ReadOnly]
            public float Timestamp;

            public void Execute(ref CollisionComponent collisionComponent, EnabledRefRW<HasCollisionTag> hasCollisionTagRW)
            {
                if (collisionComponent.LastCollisionTimestamp != 0 && Timestamp - collisionComponent.LastCollisionTimestamp >= MinCollisionTime)
                {
                    collisionComponent.FirstCollisionTime = 0;
                    collisionComponent.LastCollisionTimestamp = 0;
                    hasCollisionTagRW.ValueRW = false;
                }
            }
        }
    }
}