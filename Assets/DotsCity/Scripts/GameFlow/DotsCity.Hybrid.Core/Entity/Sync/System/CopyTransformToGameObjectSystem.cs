using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Jobs;

namespace Spirit604.DotsCity.Hybrid.Core
{
    [UpdateInGroup(typeof(InitGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct CopyTransformToGameObjectSystem : ISystem
    {
        private EntityQuery m_Query;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            m_Query = SystemAPI.QueryBuilder()
                .WithPresent<CopyTransformToGameObject>()
                .WithAll<LocalToWorld>()
                .WithAllRW<Transform>()
                .Build();

            state.RequireForUpdate(m_Query);
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            var localToWorlds = m_Query.ToComponentDataListAsync<LocalToWorld>(Allocator.TempJob, state.Dependency, out var jobHandle1);
            var entities = m_Query.ToEntityListAsync(Allocator.TempJob, state.Dependency, out var jobHandle2);

            var depJob = JobHandle.CombineDependencies(jobHandle1, jobHandle2);

            state.Dependency = new SyncTransformsJob
            {
                LocalToWorlds = localToWorlds,
                Entities = entities,
                CopyTransformToGameObjectLookup = SystemAPI.GetComponentLookup<CopyTransformToGameObject>(true),
                HybridPhysicsObjectTagLookup = SystemAPI.GetComponentLookup<HybridPhysicsObjectTag>(true),
            }.Schedule(m_Query.GetTransformAccessArray(), depJob);

            localToWorlds.Dispose(state.Dependency);
            entities.Dispose(state.Dependency);
        }

        [BurstCompile]
        struct SyncTransformsJob : IJobParallelForTransform
        {
            [ReadOnly] public NativeList<LocalToWorld> LocalToWorlds;
            [ReadOnly] public NativeList<Entity> Entities;
            [ReadOnly] public ComponentLookup<CopyTransformToGameObject> CopyTransformToGameObjectLookup;
            [ReadOnly] public ComponentLookup<HybridPhysicsObjectTag> HybridPhysicsObjectTagLookup;

            public void Execute(int index, TransformAccess transform)
            {
                var entity = Entities[index];

                if (CopyTransformToGameObjectLookup.IsComponentEnabled(entity))
                {
                    var pos = LocalToWorlds[index].Position;

                    if (HybridPhysicsObjectTagLookup.HasComponent(entity))
                        pos.y = transform.position.y;

                    transform.position = pos;
                    transform.rotation = LocalToWorlds[index].Rotation;
                }
            }
        }
    }
}