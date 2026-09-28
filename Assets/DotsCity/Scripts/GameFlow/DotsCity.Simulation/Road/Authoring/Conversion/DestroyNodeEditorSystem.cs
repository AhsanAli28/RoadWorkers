using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup), OrderLast = true)]
    public partial class DestroyNodeEditorSystem : SimpleSystemBase
    {
        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .WithAll<DestroyNodeEditorTag>()
            .ForEach((Entity entity) =>
            {
                commandBuffer.DestroyEntity(entity);
            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}