using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Car.Sound;
using Spirit604.DotsCity.Simulation.Sound;
using Spirit604.DotsCity.Simulation.Sound.Utils;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic.Sound
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficHornSoundSystem : ISystem
    {
        private EntityQuery updateQuery;
        private EntityQuery soundPrefabQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            soundPrefabQuery = SoundExtension.GetSoundQuery(state.EntityManager, SoundType.TrackingAndLoop);

            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<CarHornComponent, InViewOfCameraTag, HasDriverTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
            state.RequireForUpdate<CarSharedDataConfigReference>();
            state.RequireForUpdate<TrafficHornConfigReference>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var hornJob = new HornJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                CarHornConfigReference = SystemAPI.GetSingleton<TrafficHornConfigReference>(),
                TrafficSharedDataConfigReference = SystemAPI.GetSingleton<CarSharedDataConfigReference>(),
                SoundPrefabEntity = soundPrefabQuery.GetSingletonEntity(),
                CurrentTimestamp = (float)SystemAPI.Time.ElapsedTime,
            };

            hornJob.Schedule();
        }

        [WithAll(typeof(InViewOfCameraTag), typeof(HasDriverTag))]
        [BurstCompile]
        public partial struct HornJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public TrafficHornConfigReference CarHornConfigReference;

            [ReadOnly]
            public CarSharedDataConfigReference TrafficSharedDataConfigReference;

            [ReadOnly]
            public Entity SoundPrefabEntity;

            [ReadOnly]
            public float CurrentTimestamp;

            void Execute(
                Entity entity,
                ref CarHornComponent сarHornComponent,
                in TrafficStuckInfoComponent trafficStuckInfoComponent,
                in CarModelComponent carModelComponent)
            {
                float passedTime = CurrentTimestamp - trafficStuckInfoComponent.SavedTimestamp;

                var shouldHourne = passedTime >= CarHornConfigReference.Config.Value.IdleTimeToStart;

                var canHorn = CurrentTimestamp > сarHornComponent.NextHornTime;

                if (shouldHourne && canHorn)
                {
                    var seed = UnityMathematicsExtension.GetSeed(CurrentTimestamp, entity.Index);
                    var rndGen = new Random(seed);

                    var delay = rndGen.NextFloat(CarHornConfigReference.Config.Value.MinDelay, CarHornConfigReference.Config.Value.MaxDelay);
                    сarHornComponent.NextHornTime = CurrentTimestamp + delay;

                    var canStart = UnityMathematicsExtension.ChanceDropped(CarHornConfigReference.Config.Value.ChanceToStart, rndGen);

                    if (canStart)
                    {
                        var soundId = TrafficSharedDataConfigReference.Config.GetSoundID(carModelComponent.Value, CarSoundType.Horn);

                        if (soundId >= 0)
                        {
                            var soundEntity = CommandBuffer.CreateTrackedSoundEntity(SoundPrefabEntity, soundId, entity);

                            var newSeed = MathUtilMethods.ModifySeed(seed, entity.Index);
                            var rnd = new Random(newSeed);

                            float duration = rnd.NextFloat(CarHornConfigReference.Config.Value.MinHornDuration, CarHornConfigReference.Config.Value.MaxHornDuration);

                            CommandBuffer.SetComponent(soundEntity, new LoopSoundData()
                            {
                                Duration = duration
                            });
                        }
                    }
                }
            }
        }
    }
}
