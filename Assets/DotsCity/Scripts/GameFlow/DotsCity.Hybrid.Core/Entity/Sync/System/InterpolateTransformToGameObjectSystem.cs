using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.GraphicsIntegration;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Jobs;

namespace Spirit604.DotsCity.Hybrid.Core
{
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateAfter(typeof(LocalToWorldSystem))]
    [UpdateAfter(typeof(ParentSystem))]
    [BurstCompile]
    public partial struct InterpolateTransformToGameObjectSystem : ISystem
    {
        private EntityQuery m_Query;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            m_Query = SystemAPI.QueryBuilder()
                .WithAll<InterpolateTransformData, PhysicsWorldIndex, PhysicsVelocity, PhysicsMass>()
                .WithAllRW<Transform>()
                .Build();

            m_Query.SetSharedComponentFilter(new PhysicsWorldIndex() { Value = 0 });

            state.RequireForUpdate(m_Query);
            state.RequireForUpdate<MostRecentFixedTime>();
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            var mostRecentFixedTime = SystemAPI.GetSingletonBuffer<MostRecentFixedTime>();

            if (mostRecentFixedTime.Length == 0)
            {
                return;
            }

            var recordMostRecentFixedTime = mostRecentFixedTime[0];

            var timeAhead = (float)(SystemAPI.Time.ElapsedTime - recordMostRecentFixedTime.ElapsedTime);
            var timeStep = (float)recordMostRecentFixedTime.DeltaTime;

            if (timeAhead <= 0f || timeStep == 0f)
            {
                return;
            }

            var normalizedTimeAhead = math.clamp(timeAhead / timeStep, 0f, 1f);

            var entities = m_Query.ToEntityListAsync(Allocator.TempJob, state.Dependency, out var jobHandle);

            state.Dependency = new InterpolateJob
            {
                Entities = entities,
                InterpolateTransformDataLookup = SystemAPI.GetComponentLookup<InterpolateTransformData>(true),
                PhysicsVelocityLookup = SystemAPI.GetComponentLookup<PhysicsVelocity>(true),
                PhysicsMassLookup = SystemAPI.GetComponentLookup<PhysicsMass>(true),
                TimeAhead = timeAhead,
                NormalizedTimeAhead = normalizedTimeAhead,

            }.Schedule(m_Query.GetTransformAccessArray(), jobHandle);

            entities.Dispose(state.Dependency);
        }

        [BurstCompile]
        struct InterpolateJob : IJobParallelForTransform
        {
            [ReadOnly] public NativeList<Entity> Entities;
            [ReadOnly] public ComponentLookup<InterpolateTransformData> InterpolateTransformDataLookup;
            [ReadOnly] public ComponentLookup<PhysicsVelocity> PhysicsVelocityLookup;
            [ReadOnly] public ComponentLookup<PhysicsMass> PhysicsMassLookup;
            [ReadOnly] public float TimeAhead;
            [ReadOnly] public float NormalizedTimeAhead;

            public void Execute(int index, TransformAccess transform)
            {
                var entity = Entities[index];
                var interpolateData = InterpolateTransformDataLookup[entity];
                var physicsVelocity = PhysicsVelocityLookup[entity];
                var physicsMass = PhysicsMassLookup[entity];

                var newTransform = GraphicalSmoothingUtility.InterpolateUsingVelocity(interpolateData.PreviousTransform, interpolateData.PreviousVelocity, physicsVelocity, physicsMass, TimeAhead, NormalizedTimeAhead);

                transform.position = newTransform.pos;
                transform.rotation = newTransform.rot;
            }
        }
    }
}