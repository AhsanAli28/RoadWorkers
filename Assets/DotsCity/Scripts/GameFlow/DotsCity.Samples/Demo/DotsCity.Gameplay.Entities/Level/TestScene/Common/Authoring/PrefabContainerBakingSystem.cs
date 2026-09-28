using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.TestScene
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateBefore(typeof(VehicleComponentCleanerBakingSystem))]
    [UpdateInGroup(typeof(PostBakingSystemGroup), OrderLast = true)]
    public partial class PrefabContainerBakingSystem : SystemBase
    {
        private EntityQuery bakingCarQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingCarQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PrefabContainer>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate(bakingCarQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
            .ForEach((
                in PrefabContainer prefabContainer) =>
            {
                var entity = prefabContainer.Entity;

                commandBuffer.AddComponent(entity, new TrafficComponentCleanerTag()
                {
                    CleanSound = prefabContainer.CleanSound,
                    AddPoolable = prefabContainer.AddPoolable,
                });

                if (prefabContainer.HasInput)
                {
                    AddInputComponents(ref commandBuffer, entity);
                }

            }).Run();

            CompleteDependency();
            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }

        public static void AddInputComponents(ref EntityCommandBuffer commandBuffer, Entity entity)
        {
            commandBuffer.AddComponent<PlayerTag>(entity);
            commandBuffer.AddComponent<HasDriverTag>(entity);
            commandBuffer.AddComponent<CarEngineStartedTag>(entity);
            commandBuffer.AddComponent<VehicleInputReader>(entity);
        }
    }
}