using Rukhanka;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

namespace Spirit604.DotsCity.ThirdParty.RukhankaEngine
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup), OrderLast = true)]
    public partial class RukhankaRagdollBakingSystem : SystemBase
    {
        private EntityQuery bakingCarQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingCarQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<RukhankaRagdollBakingTag>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate(bakingCarQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .WithAll<RukhankaRagdollBakingTag>()
            .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
            .ForEach((
                Entity entity,
                ref DynamicBuffer<RukhankaRagdollBuffer> buffer,
                ref DynamicBuffer<AnimatorControllerParameterComponent> parameterBuffer,
                in DynamicBuffer<LinkedEntityGroup> group,
                in LocalTransform localTransform) =>
            {
                if (!EntityManager.HasComponent<PoolableTag>(entity))
                    PoolEntityUtils.AddPoolComponents(ref commandBuffer, entity, EntityWorldType.PureEntity);

                parameterBuffer.Clear();

                for (var i = 0; i < group.Length; i++)
                {
                    var jointEntity = group[i].Value;

#if UNITY_PHYSICS
                    if (EntityManager.HasComponent<PhysicsJoint>(jointEntity))
                    {
                        var world = EntityManager.GetComponentData<LocalToWorld>(jointEntity);

                        buffer.Add(new RukhankaRagdollBuffer()
                        {
                            Entity = jointEntity,
                            Joint = true,
                            RelativeParentTransform = new Unity.Mathematics.RigidTransform(world.Rotation, world.Position),
                        });
                    }
#endif
                }

                commandBuffer.AddComponent<RukhankaRagdollData>(entity);
                commandBuffer.AddComponent<RukhankaRagdollActivate>(entity);
                commandBuffer.AddComponent<RagdollComponent>(entity);
                commandBuffer.SetComponentEnabled<RukhankaRagdollActivate>(entity, false);

                for (var i = 0; i < buffer.Length; i++)
                {
                    var boneEntity = buffer[i].Entity;
                    commandBuffer.SetComponentEnabled<Simulate>(boneEntity, false);
                }

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}