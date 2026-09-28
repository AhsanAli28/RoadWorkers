using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public abstract partial class PedestrianEntityCustomBakingSystemBase : SystemBase
    {
        private EntityQuery npcBakingQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            npcBakingQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CustomBakingTag>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate(npcBakingQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            OnUpdateBegin();

            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
            .WithAll<CustomBakingTag>()
            .ForEach((
                Entity entity) =>
            {
                Bake(ref commandBuffer, entity);

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }

        protected virtual void OnUpdateBegin() { }
        protected abstract void Bake(ref EntityCommandBuffer commandBuffer, Entity entity);
    }
}