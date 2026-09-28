#if RUNTIME_ROAD
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(HashMapGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct PedestrianPathHashMapSystem : ISystem, ISystemStartStop
    {
        public struct PedestrianHashEntity
        {
            public Entity Entity;
        }

        public struct Singleton : IComponentData
        {
            public NativeParallelMultiHashMap<EntityPair, PedestrianHashEntity> HashMap;

            public bool IsCreated => HashMap.IsCreated;
            public bool IsEmpty => HashMap.IsEmpty;
        }

        private NativeParallelMultiHashMap<EntityPair, PedestrianHashEntity> hashMap;
        private EntityQuery updateQuery;
        private EntityQuery pedestrianQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<PedestrianSpawnSettingsReference>()
                .Build();

            pedestrianQuery = SystemAPI.QueryBuilder()
                .WithAll<PedestrianMovementSettings>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            if (hashMap.IsCreated)
            {
                hashMap.Dispose();
            }
        }

        void ISystemStartStop.OnStartRunning(ref SystemState state)
        {
            if (!hashMap.IsCreated)
            {
                var config = SystemAPI.GetSingleton<PedestrianSpawnSettingsReference>().Config.Value;
                hashMap = new NativeParallelMultiHashMap<EntityPair, PedestrianHashEntity>(100 + config.MinPedestrianCount * 2, Allocator.Persistent);

                state.EntityManager.AddComponentData(state.SystemHandle, new Singleton()
                {
                    HashMap = hashMap
                });
            }
        }

        void ISystemStartStop.OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            hashMap.Clear();

            var entityCount = pedestrianQuery.CalculateEntityCount();

            if (hashMap.Capacity < entityCount)
            {
                hashMap.Capacity = entityCount * 2;
            }

            var fillHashMapJob = new FillHashMapJob()
            {
                HashMapParallel = hashMap.AsParallelWriter(),
            };

            fillHashMapJob.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct FillHashMapJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            public NativeParallelMultiHashMap<EntityPair, PedestrianHashEntity>.ParallelWriter HashMapParallel;

            void Execute(
                Entity entity,
                in DestinationComponent destinationComponent)
            {
                var pair = new EntityPair(destinationComponent.PreviuosDestinationNode, destinationComponent.DestinationNode);

                var hashEntity = new PedestrianHashEntity()
                {
                    Entity = entity,
                };

                HashMapParallel.Add(pair, hashEntity);
            }
        }
    }
}
#endif