using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Spirit604.DotsCity.Simulation.Sound;
using Spirit604.DotsCity.Simulation.Sound.Pedestrian;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct ProcessTrafficEntryNodeSystem : ISystem
    {
        // If the vehicle has departed from the stop station
        private const float MaxDiffDistanceSQ = 0.0625f; // 0.25f * 0.25f;

        private EntityQuery npcQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            npcQuery = SystemAPI.QueryBuilder()
                .WithAll<ProcessEnterTrafficEntryNodeTag>()
                .Build();

            state.RequireForUpdate(npcQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var enteredTrafficEntryNodeJob = new EnteredTrafficEntryNodeJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                SoundEventQueue = SystemAPI.GetSingleton<SoundEventPlaybackSystem.Singleton>(),
                CarCapacityLookup = SystemAPI.GetComponentLookup<CarCapacityComponent>(false),
                VehicleLinkLookup = SystemAPI.GetComponentLookup<VehicleLinkComponent>(true),
                WorldTransformLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                SoundConfigReference = SystemAPI.GetSingleton<SoundConfigReference>()
            };

            enteredTrafficEntryNodeJob.Run();
        }

        [WithAll(typeof(ProcessEnterTrafficEntryNodeTag))]
        [BurstCompile]
        public partial struct EnteredTrafficEntryNodeJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [NativeDisableContainerSafetyRestriction]
            public SoundEventPlaybackSystem.Singleton SoundEventQueue;

            public ComponentLookup<CarCapacityComponent> CarCapacityLookup;

            [ReadOnly]
            public ComponentLookup<VehicleLinkComponent> VehicleLinkLookup;

            [ReadOnly]
            public ComponentLookup<LocalToWorld> WorldTransformLookup;

            [ReadOnly]
            public SoundConfigReference SoundConfigReference;

            void Execute(
                Entity pedestrianEntity,
                ref DestinationComponent destinationComponent,
                in LocalTransform transform)
            {
                bool carEntered = false;

                if (VehicleLinkLookup.HasComponent(destinationComponent.DestinationNode))
                {
                    var carEntity = VehicleLinkLookup[destinationComponent.DestinationNode].LinkedVehicle;

                    if (CarCapacityLookup.HasComponent(carEntity))
                    {
                        var currentPosition = WorldTransformLookup[destinationComponent.DestinationNode].Position;
                        var destinationPosition = destinationComponent.Value;

                        var distance = math.distancesq(currentPosition, destinationPosition);

                        if (distance < MaxDiffDistanceSQ)
                        {
                            var carCapacity = CarCapacityLookup[carEntity];

                            if (carCapacity.AvailableCapacity > 0)
                            {
                                carCapacity.AvailableCapacity -= 1;
                                carEntered = true;
                            }

                            CarCapacityLookup[carEntity] = carCapacity;
                        }
                    }
                }

                if (carEntered)
                {
                    var soundId = SoundConfigReference.Config.Value.EnterTramSoundId;
                    SoundEventQueue.PlayOneShot(soundId, transform.Position);

                    PoolEntityUtils.DestroyEntity(ref CommandBuffer, pedestrianEntity);
                }
                else
                {
                    destinationComponent = destinationComponent.SwapBack();

                    CommandBuffer.SetComponentEnabled<HasTargetTag>(pedestrianEntity, true);
                    CommandBuffer.RemoveComponent<ProcessEnterTrafficEntryNodeTag>(pedestrianEntity);
                }
            }
        }
    }
}
