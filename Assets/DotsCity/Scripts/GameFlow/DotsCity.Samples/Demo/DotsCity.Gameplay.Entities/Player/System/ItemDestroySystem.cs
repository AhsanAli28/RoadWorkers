using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Level;
using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Player
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class ItemDestroySystem : BeginSimulationSystemBase
    {
        protected override void OnUpdate()
        {
            var commandBuffer = GetCommandBuffer();

            Entities
            .WithoutBurst()
            .WithNone<PooledEventTag>()
            .WithAll<ItemComponent>()
            .WithAny<ItemTakenTag>()
            .ForEach((
             Entity entity) =>
            {
                PoolEntityUtils.DestroyEntity(ref commandBuffer, entity);
            }).Schedule();

            AddCommandBufferForProducer();
        }
    }
}