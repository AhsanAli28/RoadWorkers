using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Authoring;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Common.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class StaticPhysicsBakingSystem : SystemBase
    {
        private EntityQuery bakingQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<BakingStaticPhysicsData>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate(bakingQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
            .ForEach((
                Entity entity, in BakingStaticPhysicsData bakingStaticPhysicsData) =>
            {
                if (!bakingStaticPhysicsData.PreinitEnabling)
                {
                    commandBuffer.AddComponent(entity, CullComponentsExtension.GetComponentSet());
                }
                else
                {
                    commandBuffer.AddComponent(entity, CullComponentsExtension.GetComponentSet(CullStateList.PreInit));
                }

            }).Run();

            CompleteDependency();
            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}