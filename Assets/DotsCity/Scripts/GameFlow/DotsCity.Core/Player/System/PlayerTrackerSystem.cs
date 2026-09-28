using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Core
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [BurstCompile]
    public partial struct PlayerTrackerSystem : ISystem
    {
        private EntityQuery playerQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerTag, LocalToWorld>().Build();
            state.RequireForUpdate(playerQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            if (playerQuery.CalculateEntityCount() != 1)
                return;

            var transform = playerQuery.GetSingleton<LocalToWorld>();

            foreach (var (localTransform, player) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<PlayerTrackerTag>>())
            {
                localTransform.ValueRW.Position = transform.Position;
                localTransform.ValueRW.Rotation = transform.Rotation;
            }
        }
    }
}