using Spirit604.DotsCity.Core;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(LateEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct DisableSpawnAreaSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<SpawnAreaComponent, NodeAreaSpawnedTag, CulledEventTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var disableSpawnAreaJob = new DisableSpawnAreaJob()
            {
            };

            disableSpawnAreaJob.Schedule();
        }

        [WithAll(typeof(NodeAreaSpawnedTag), typeof(CulledEventTag))]
        [BurstCompile]
        public partial struct DisableSpawnAreaJob : IJobEntity
        {
            void Execute(
                EnabledRefRW<NodeAreaSpawnedTag> nodeAreaSpawnedTagRW)
            {
                nodeAreaSpawnedTagRW.ValueRW = false;
            }
        }
    }
}