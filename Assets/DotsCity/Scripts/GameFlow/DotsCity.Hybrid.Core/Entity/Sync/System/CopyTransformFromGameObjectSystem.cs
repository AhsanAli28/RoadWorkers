using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Jobs;

namespace Spirit604.DotsCity.Hybrid.Core
{
    [UpdateInGroup(typeof(PreEarlyJobGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct CopyTransformFromGameObjectSystem : ISystem
    {
        private EntityQuery m_Query;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            m_Query = SystemAPI.QueryBuilder()
                .WithAll<CopyTransformFromGameObject, LocalTransform, Transform>()
                .Build();

            state.RequireForUpdate(m_Query);
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            var entities = m_Query.ToEntityListAsync(Allocator.TempJob, state.Dependency, out var jobHandle);

            state.Dependency = new SyncTransformsJob
            {
                Entities = entities,
                LocalTransforms = SystemAPI.GetComponentLookup<LocalTransform>(false),
            }.Schedule(m_Query.GetTransformAccessArray(), jobHandle);

            entities.Dispose(state.Dependency);
        }

        [BurstCompile]
        struct SyncTransformsJob : IJobParallelForTransform
        {
            [ReadOnly] public NativeList<Entity> Entities;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<LocalTransform> LocalTransforms;

            public void Execute(int index, TransformAccess transform)
            {
                var localTransform = LocalTransforms[Entities[index]];

                localTransform.Position = transform.position;
                localTransform.Rotation = transform.rotation;

                LocalTransforms[Entities[index]] = localTransform;
            }
        }
    }
}
