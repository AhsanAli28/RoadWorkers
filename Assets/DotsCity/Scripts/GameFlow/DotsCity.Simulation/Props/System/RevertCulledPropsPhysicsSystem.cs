using Spirit604.DotsCity.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.GraphicsIntegration;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Level.Props
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct RevertCulledPropsPhysicsSystem : ISystem
    {
        private EntityQuery revertQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            revertQuery = SystemAPI.QueryBuilder()
                .WithNone<InPermittedRangeTag, CulledEventTag>()
                .WithAny<InViewOfCameraTag, PreInitInCameraTag>()
                .WithAll<CullPhysicsTag, PhysicsWorldIndex, PropsComponent, CustomCullPhysicsTag>()
                .Build();

            revertQuery.SetSharedComponentFilter(new PhysicsWorldIndex() { Value = ProjectConstants.NoPhysicsWorldIndex });

            state.RequireForUpdate(revertQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var revertPhysicsJob = new RevertPhysicsJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                VelocityLookup = SystemAPI.GetComponentLookup<VelocityComponent>(true),
                PhysicsVelocityLookup = SystemAPI.GetComponentLookup<PhysicsVelocity>(true),
                StaticLookup = SystemAPI.GetComponentLookup<Static>(true),
                PhysicsGraphicalInterpolationLookup = SystemAPI.GetComponentLookup<PhysicsGraphicalInterpolationBuffer>(true)
            };

            revertPhysicsJob.Run(revertQuery);
        }
    }
}