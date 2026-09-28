using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Spirit604.DotsCity.Hybrid.Core
{
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    [UpdateBefore(typeof(ExportPhysicsWorld))]
    [BurstCompile]
    public partial struct InterpolateStateRecorderSystem : ISystem
    {
        private EntityQuery m_Query;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            m_Query = SystemAPI.QueryBuilder()
                .WithAllRW<InterpolateTransformData>()
                .WithAll<PhysicsWorldIndex, PhysicsVelocity, LocalTransform>()
                .Build();

            m_Query.SetSharedComponentFilter(new PhysicsWorldIndex() { Value = 0 });

            state.RequireForUpdate(m_Query);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            new SaveTransformJob
            {
            }.Schedule(m_Query);
        }

        [WithAll(typeof(PhysicsWorldIndex))]
        [BurstCompile]
        partial struct SaveTransformJob : IJobEntity
        {
            public void Execute(
                ref InterpolateTransformData interpolateData,
                in PhysicsVelocity physicsVelocity,
                in LocalTransform localTransform)
            {
                interpolateData.PreviousTransform = new RigidTransform(localTransform.Rotation, localTransform.Position);
                interpolateData.PreviousVelocity = physicsVelocity;
            }
        }
    }
}