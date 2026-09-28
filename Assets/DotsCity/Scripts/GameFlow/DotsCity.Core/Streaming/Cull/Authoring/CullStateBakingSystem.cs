using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Spirit604.DotsCity.Core.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class CullStateBakingSystem : SystemBase
    {
        private EntityQuery updateQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            updateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CullStateBakingTag>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate<CullSystemConfigReference>();
            RequireForUpdate(updateQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var cullConfig = SystemAPI.GetSingleton<CullSystemConfigReference>();

            Entities
            .WithoutBurst()
            .WithAll<CullStateBakingTag>()
            .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
            .ForEach((
                Entity entity,
                ref CullStateComponent cullComponent) =>
            {
                if (!cullConfig.Config.Value.HasCull)
                {
                    cullComponent.State = CullState.InViewOfCamera;

                    commandBuffer.SetComponentEnabled<InPermittedRangeTag>(entity, false);
                    commandBuffer.SetComponentEnabled<CulledEventTag>(entity, false);

                    if (EntityManager.HasComponent<PreInitInCameraTag>(entity))
                    {
                        commandBuffer.SetComponentEnabled<PreInitInCameraTag>(entity, false);
                    }
                }
                else
                {
                    if (EntityManager.HasComponent<PoolableTag>(entity))
                    {
                        cullComponent.State = CullState.CloseToCamera;

                        commandBuffer.SetComponentEnabled<InViewOfCameraTag>(entity, false);
                        commandBuffer.SetComponentEnabled<CulledEventTag>(entity, false);

                        if (EntityManager.HasComponent<PreInitInCameraTag>(entity))
                        {
                            commandBuffer.SetComponentEnabled<PreInitInCameraTag>(entity, false);
                        }
                    }
                    else
                    {
                        cullComponent.State = CullState.Uninitialized;
                    }
                }

            }).Run();

            CompleteDependency();
            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}