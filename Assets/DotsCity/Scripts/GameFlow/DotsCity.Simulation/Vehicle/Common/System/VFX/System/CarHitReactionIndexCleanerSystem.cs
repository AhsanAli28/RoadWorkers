using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car
{
    [UpdateInGroup(typeof(CleanupGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CarHitReactionIndexCleanerSystem : ISystem, ISystemStartStop
    {
        private EntityQuery updateQuery;
        private NativeHashSet<int> takenIndexesLocalRef;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<VehicleAnimatedHullTag>()
                .WithAll<CarHitReactionTakenIndex>()
                .Build();

            state.RequireForUpdate(updateQuery);
            state.RequireForUpdate<CarHitReactProviderSystem.FactoryCreatedEventTag>();
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            takenIndexesLocalRef = default;
        }

        void ISystemStartStop.OnStartRunning(ref SystemState state)
        {
            if (!takenIndexesLocalRef.IsCreated)
            {
                takenIndexesLocalRef = CarHitReactProviderSystem.TakenIndexes;
            }
        }

        void ISystemStartStop.OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var cleanIndexJob = new CleanIndexJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                TakenIndexes = takenIndexesLocalRef,
            };

            cleanIndexJob.Schedule();
        }

        [WithNone(typeof(VehicleAnimatedHullTag))]
        [BurstCompile]
        public partial struct CleanIndexJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [NativeDisableContainerSafetyRestriction]
            public NativeHashSet<int> TakenIndexes;

            void Execute(
                Entity entity,
                in CarHitReactionTakenIndex carHitReactionTakenIndex)
            {
                if (TakenIndexes.Contains(carHitReactionTakenIndex.TakenIndex))
                {
                    TakenIndexes.Remove(carHitReactionTakenIndex.TakenIndex);
                }

                CommandBuffer.RemoveComponent<CarHitReactionTakenIndex>(entity);
            }
        }
    }
}