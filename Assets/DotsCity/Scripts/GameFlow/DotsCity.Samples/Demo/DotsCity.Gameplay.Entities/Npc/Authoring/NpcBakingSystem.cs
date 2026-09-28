using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Npc.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class NpcBakingSystem : SystemBase
    {
        private EntityQuery bakingCarQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingCarQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NpcBakingTag>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate(bakingCarQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .WithAll<NpcBakingTag>()
            .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
            .ForEach((
                Entity entity) =>
            {
                commandBuffer.SetComponentEnabled<AnimatorFallingState>(entity, false);

                if (EntityManager.HasComponent<GroundCasterBakingData>(entity))
                {
                    var groundCasterBakingData = EntityManager.GetComponentData<GroundCasterBakingData>(entity);
                    var casterEntity = EntityManager.GetComponentData<GroundCasterRef>(entity).CasterEntity;

                    commandBuffer.SetComponent(casterEntity, new GroundCasterComponent()
                    {
                        CastingLayer = groundCasterBakingData.CastingLayer
                    });
                }

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}