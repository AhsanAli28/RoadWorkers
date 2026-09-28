using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace Spirit604.DotsCity.Common.Authoring
{
    [TemporaryBakingType]
    public struct RemovePhysicsTag : IComponentData { }

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    public partial class RemovePhysicsBakingSystem : SystemBase
    {
        private EntityQuery bakingQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<RemovePhysicsTag>()
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
            .WithAll<RemovePhysicsTag, PhysicsCollider>()
            .ForEach((
                Entity entity) =>
            {
                commandBuffer.RemoveComponent<PhysicsVelocity>(entity);
                commandBuffer.RemoveComponent<PhysicsMass>(entity);
                commandBuffer.RemoveComponent<PhysicsCollider>(entity);
                commandBuffer.RemoveComponent<PhysicsColliderKeyEntityPair>(entity);
                commandBuffer.RemoveComponent<PhysicsDamping>(entity);
                commandBuffer.RemoveComponent<PhysicsWorldIndex>(entity);
            }).Run();


            CompleteDependency();
            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}