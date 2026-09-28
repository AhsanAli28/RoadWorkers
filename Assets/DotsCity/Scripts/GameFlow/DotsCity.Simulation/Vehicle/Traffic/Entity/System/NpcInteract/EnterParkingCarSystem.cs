using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Sound;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct EnterParkingCarSystem : ISystem
    {
        private EntityQuery carQuery;
        void ISystem.OnCreate(ref SystemState state)
        {
            carQuery = SystemAPI.QueryBuilder()
                .WithAllRW<TrafficStateComponent>()
                .WithPresentRW<TrafficIdleTag>()
                .WithAll<ParkingDriverRequestTag, CarModelComponent, LocalTransform>()
                .Build();

            state.RequireForUpdate(carQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var hasIgnition = false;

            if (SystemAPI.HasSingleton<CarIgnitionConfigReference>())
            {
                hasIgnition = SystemAPI.GetSingleton<CarIgnitionConfigReference>().Config.Value.HasIgnition;
            }

            var enterCarJob = new EnterCarJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                SoundEventQueue = SystemAPI.GetSingleton<SoundEventPlaybackSystem.Singleton>(),
                InViewOfCameraLookup = SystemAPI.GetComponentLookup<InViewOfCameraTag>(true),
                CarSharedDataConfigReference = SystemAPI.GetSingleton<CarSharedDataConfigReference>(),
                HasIgnition = hasIgnition,
            };

            enterCarJob.Run(carQuery);
        }

        [WithAll(typeof(ParkingDriverRequestTag))]
        [BurstCompile]
        public partial struct EnterCarJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            public SoundEventPlaybackSystem.Singleton SoundEventQueue;

            [ReadOnly]
            public ComponentLookup<InViewOfCameraTag> InViewOfCameraLookup;

            [ReadOnly]
            public CarSharedDataConfigReference CarSharedDataConfigReference;

            [ReadOnly]
            public bool HasIgnition;

            void Execute(
                Entity entity,
                ref TrafficStateComponent trafficStateComponent,
                EnabledRefRW<TrafficIdleTag> trafficIdleTagRW,
                in CarModelComponent carModelComponent,
                in LocalTransform transform)
            {
                var inViewOfCamera = InViewOfCameraLookup.IsComponentEnabled(entity);
                var ignite = HasIgnition && inViewOfCamera;

                ref var soundConfig = ref CarSharedDataConfigReference.Config;

                InteractCarUtils.EnterCar(ref CommandBuffer, ref soundConfig, ref SoundEventQueue, entity, inViewOfCamera, ignite, carModelComponent.Value, transform.Position);
                TrafficStateExtension.RemoveIdleState(ref trafficStateComponent, ref trafficIdleTagRW, TrafficIdleState.Parking);

                CommandBuffer.RemoveComponent<TrafficNodeLinkedComponent>(entity);
                CommandBuffer.RemoveComponent<ParkingDriverRequestTag>(entity);
            }
        }
    }
}