#if RUNTIME_ROAD
using Spirit604.DotsCity.Simulation.Pedestrian;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(HashMapGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct TrafficNextPathHashMapSystem : ISystem, ISystemStartStop
    {
        public struct HashEntity
        {
            public Entity Entity;
        }

        public struct Singleton : IComponentData
        {
            public NativeParallelMultiHashMap<int, HashEntity> HashMap;

            public bool IsCreated => HashMap.IsCreated;
            public bool IsEmpty => HashMap.IsEmpty;
        }

        private NativeParallelMultiHashMap<int, HashEntity> hashMap;
        private EntityQuery updateQuery;
        private EntityQuery entityQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<PedestrianSpawnSettingsReference>()
                .Build();

            entityQuery = SystemAPI.QueryBuilder()
                .WithAll<TrafficDestinationComponent>()
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
                hashMap = new NativeParallelMultiHashMap<int, HashEntity>(100 + config.MinPedestrianCount * 2, Allocator.Persistent);

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

            var entityCount = entityQuery.CalculateEntityCount();

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
            public NativeParallelMultiHashMap<int, HashEntity>.ParallelWriter HashMapParallel;

            void Execute(
                Entity entity,
                in TrafficDestinationComponent destinationComponent)
            {
                var hashEntity = new HashEntity()
                {
                    Entity = entity,
                };

                if (destinationComponent.NextGlobalPathIndex != -1)
                    HashMapParallel.Add(destinationComponent.NextGlobalPathIndex, hashEntity);
            }
        }
    }
}
#endif