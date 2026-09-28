using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Gameplay.Player
{
    [UpdateAfter(typeof(CarHashMapSystem))]
    [UpdateInGroup(typeof(HashMapGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct PlayerGetAvailableCarForEnterSystem : ISystem, ISystemStartStop
    {
        private const float SizeMultiplier = 2.7f;

        private SystemHandle carHashMapSystem;
        private EntityQuery updateQuery;
        private NativeParallelHashSet<int> availablePlayerPoolCars;
        private NativeList<int> keys;
        private NativeArray<int> previousKey;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            carHashMapSystem = state.WorldUnmanaged.GetExistingUnmanagedSystem<CarHashMapSystem>();

            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<PlayerNpcComponent, LocalToWorld>()
                .Build();

            state.RequireForUpdate<CarHashMapSystem.Singleton>();
            state.RequireForUpdate<PlayerCarCollectionReference>();

            state.RequireForUpdate(updateQuery);
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            if (availablePlayerPoolCars.IsCreated)
            {
                availablePlayerPoolCars.Dispose();
            }

            if (keys.IsCreated)
            {
                keys.Dispose();
            }

            if (previousKey.IsCreated)
            {
                previousKey.Dispose();
            }
        }

        void ISystemStartStop.OnStartRunning(ref SystemState state)
        {
            if (availablePlayerPoolCars.IsCreated)
                return;

            var playerCarCollectionReference = SystemAPI.GetSingleton<PlayerCarCollectionReference>();
            ref var collection = ref playerCarCollectionReference.Config.Value.AvailableIds;

            availablePlayerPoolCars = new NativeParallelHashSet<int>(collection.Length, Allocator.Persistent);

            for (int i = 0; i < collection.Length; i++)
            {
                availablePlayerPoolCars.Add(collection[i]);
            }

            keys = new NativeList<int>(9, Allocator.Persistent);
            previousKey = new NativeArray<int>(1, Allocator.Persistent);
        }

        void ISystemStartStop.OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            ref var carHashMapSystemRef = ref state.WorldUnmanaged.ResolveSystemStateRef(carHashMapSystem);
            var depJob = carHashMapSystemRef.Dependency;

            var availableJob = new AvailableJob()
            {
                CarHashMapSingleton = SystemAPI.GetSingleton<CarHashMapSystem.Singleton>(),
                AvailablePlayerPoolCars = availablePlayerPoolCars,
                Keys = keys,
                PreviousKey = previousKey
            };

            state.Dependency = availableJob.Schedule(depJob);
        }

        [BurstCompile]
        public partial struct AvailableJob : IJobEntity
        {
            [ReadOnly]
            public CarHashMapSystem.Singleton CarHashMapSingleton;

            [ReadOnly]
            public NativeParallelHashSet<int> AvailablePlayerPoolCars;

            public NativeList<int> Keys;
            public NativeArray<int> PreviousKey;

            void Execute(
               ref PlayerNpcComponent playerNpcComponent,
               in LocalToWorld worldTransform)
            {
                int carEntityIndex = -1;
                float cachedMinDistance = float.MaxValue;

                var playerPos = worldTransform.Position;
                var playerPosFlatted = playerPos.Flat();

                var currentKey = HashMapHelper.GetHashMapPosition(playerPosFlatted);

                if (PreviousKey[0] != currentKey)
                {
                    PreviousKey[0] = currentKey;
                    Keys.Clear();
                    HashMapHelper.GetHashMapPosition9Cells(ref Keys, playerPosFlatted);
                }

                for (int i = 0; i < Keys.Length; i++)
                {
                    if (CarHashMapSingleton.CarHashMap.TryGetFirstValue(Keys[i], out var carHashEntity, out var nativeMultiHashMapIterator))
                    {
                        do
                        {
                            var isAvailable = AvailablePlayerPoolCars.Contains(carHashEntity.CarModel);

                            if (!isAvailable)
                                continue;

                            isAvailable = carHashEntity.Health != 0;

                            if (!isAvailable)
                                continue;

                            float distance = math.distance(carHashEntity.Position, playerPos);

                            float sizeX = carHashEntity.BoundsExtents.x * SizeMultiplier;

                            if (distance < sizeX && distance < cachedMinDistance)
                            {
                                carEntityIndex = carHashEntity.Entity.Index;
                                cachedMinDistance = distance;
                            }

                        } while (CarHashMapSingleton.CarHashMap.TryGetNextValue(out carHashEntity, ref nativeMultiHashMapIterator));
                    }
                }

                if (playerNpcComponent.AvailableCarEntityIndex != carEntityIndex)
                {
                    playerNpcComponent.AvailableCarEntityIndex = carEntityIndex;
                }
            }
        }
    }
}