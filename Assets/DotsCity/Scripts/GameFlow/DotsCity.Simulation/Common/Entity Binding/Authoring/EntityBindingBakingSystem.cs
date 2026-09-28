using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Binding.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial class EntityBindingBakingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var config = SystemAPI.GetSingleton<EntityBindingConfigReference>();

            if (!config.Config.Value.IsAvailable)
                return;

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
              .WithoutBurst()
              .ForEach((
                  Entity entity,
                  in EntityIDBakingData entityIDBakingData) =>
              {
                  commandBuffer.AddComponent<EntityIDInitTag>(entity);
                  commandBuffer.AddComponent(entity, new EntityID()
                  {
                      Value = entityIDBakingData.Value
                  });

              }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}