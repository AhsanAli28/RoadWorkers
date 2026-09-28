using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Core
{
    [UpdateAfter(typeof(CalcCullingSystem))]
    [UpdateInGroup(typeof(CullSimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CullStaticPhysicsSystem : ISystem
    {
        private SystemHandle calcCullingSystem;
        private EntityQuery cullQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            calcCullingSystem = state.WorldUnmanaged.GetExistingUnmanagedSystem<CalcCullingSystem>();

            cullQuery = SystemAPI.QueryBuilder()
                .WithNone<InViewOfCameraTag, PreInitInCameraTag>()
                .WithAny<InPermittedRangeTag, CulledEventTag>()
                .WithAll<PhysicsWorldIndex, CullPhysicsTag, Static>()
                .Build();

            cullQuery.SetSharedComponentFilter(new PhysicsWorldIndex() { Value = 0 });

            state.RequireForUpdate(cullQuery);
            state.Enabled = false;
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            ref var calcCullingSystemRef = ref state.WorldUnmanaged.ResolveSystemStateRef(calcCullingSystem);

            var cullPhysicsJob = new CullStaticPhysicsJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            };

            state.Dependency = cullPhysicsJob.ScheduleParallel(cullQuery, calcCullingSystemRef.Dependency);
        }
    }

    [WithNone(typeof(InViewOfCameraTag), typeof(PreInitInCameraTag))]
    [WithAny(typeof(InPermittedRangeTag), typeof(CulledEventTag))]
    [WithAll(typeof(PhysicsWorldIndex), typeof(CullPhysicsTag), typeof(Static))]
    [BurstCompile]
    public partial struct CullStaticPhysicsJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        void Execute([ChunkIndexInQuery] int entityInQueryIndex, Entity entity)
        {
            CommandBuffer.SetSharedComponent(entityInQueryIndex, entity, new PhysicsWorldIndex()
            {
                Value = ProjectConstants.NoPhysicsWorldIndex
            });
        }
    }
}