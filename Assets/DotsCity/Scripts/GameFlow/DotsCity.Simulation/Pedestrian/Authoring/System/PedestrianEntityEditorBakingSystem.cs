using Spirit604.DotsCity.Events;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class PedestrianEntityEditorBakingSystem : SystemBase
    {
        private EntityQuery bakingQuery;
        private EntityQuery triggerNodeConfigQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PedestrianEntityEditorBakingTag>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            triggerNodeConfigQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TriggerNodeConfigReference>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate(bakingQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            bool hasTriggerNodes = false;

            if (triggerNodeConfigQuery.HasSingleton<TriggerNodeConfigReference>())
            {
                hasTriggerNodes = triggerNodeConfigQuery.GetSingleton<TriggerNodeConfigReference>().Config.Value.IsActivated;
            }

            Entities
            .WithoutBurst()
            .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
            .WithAll<PedestrianEntityEditorBakingTag>()
            .ForEach((
                Entity prefabEntity) =>
            {
                if (hasTriggerNodes)
                {
                    commandBuffer.AddComponent<TriggerNodeTag>(prefabEntity);
                    commandBuffer.SetComponentEnabled<TriggerNodeTag>(prefabEntity, false);
                }

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}