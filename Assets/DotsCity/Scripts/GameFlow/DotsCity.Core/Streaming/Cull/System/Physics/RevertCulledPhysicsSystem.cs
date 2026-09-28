using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.GraphicsIntegration;
using Unity.Transforms;

namespace Spirit604.DotsCity.Core
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct RevertCulledPhysicsSystem : ISystem
    {
        private EntityQuery revertQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            revertQuery = SystemAPI.QueryBuilder()
                .WithNone<InPermittedRangeTag, CulledEventTag, CustomCullPhysicsTag>()
                .WithAny<InViewOfCameraTag, PreInitInCameraTag>()
                .WithAll<CullPhysicsTag, PhysicsWorldIndex>()
                .Build();

            revertQuery.SetSharedComponentFilter(new PhysicsWorldIndex() { Value = ProjectConstants.NoPhysicsWorldIndex });

            state.RequireForUpdate(revertQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            new RevertPhysicsJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                VelocityLookup = SystemAPI.GetComponentLookup<VelocityComponent>(true),
                PhysicsVelocityLookup = SystemAPI.GetComponentLookup<PhysicsVelocity>(true),
                StaticLookup = SystemAPI.GetComponentLookup<Static>(true),
                PhysicsGraphicalInterpolationLookup = SystemAPI.GetComponentLookup<PhysicsGraphicalInterpolationBuffer>(true)
            }.Run(revertQuery);
        }
    }
}