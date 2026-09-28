using Pathfinding.ECS;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.ThirdParty.Astar
{
    [UpdateInGroup(typeof(InitGroup))]
    [DisableAutoCreation]
    [DisableAutoTypeRegistration]
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    public partial struct AstarManagedStateInitSystem : ISystem
    {
        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            bool updated = false;

            foreach (var (managedStateInit, entity)
               in SystemAPI.Query<RefRO<ManagedStateInit>>()
               .WithAll<Prefab>()
               .WithOptions(EntityQueryOptions.IncludePrefab)
               .WithEntityAccess())
            {
                updated = true;

                commandBuffer.RemoveComponent<ManagedStateInit>(entity);
                commandBuffer.AddComponent(entity, new ManagedState());
            }

            if (updated)
                state.Enabled = false;
        }
    }
}
