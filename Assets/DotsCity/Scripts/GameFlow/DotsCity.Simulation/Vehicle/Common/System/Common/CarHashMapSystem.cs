using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Common;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    [UpdateInGroup(typeof(HashMapGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct CarHashMapSystem : ISystem, ISystemStartStop
    {
        public struct Singleton : IComponentData
        {
            public NativeParallelMultiHashMap<int, CarHashEntityComponent> CarHashMap;

            public bool IsCreated => CarHashMap.IsCreated;
            public bool IsEmpty => CarHashMap.IsEmpty;
        }

        private NativeParallelMultiHashMap<int, CarHashEntityComponent> carHashMap;
        private EntityQuery updateQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<TrafficSpawnerConfigBlobReference>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            if (carHashMap.IsCreated)
            {
                carHashMap.Dispose();
            }
        }

        void ISystemStartStop.OnStartRunning(ref SystemState state)
        {
            if (!carHashMap.IsCreated)
            {
                var trafficSpawnerConfig = SystemAPI.GetSingleton<TrafficSpawnerConfigBlobReference>().Reference.Value;
                carHashMap = new NativeParallelMultiHashMap<int, CarHashEntityComponent>(trafficSpawnerConfig.HashMapCapacity, Allocator.Persistent);

                state.EntityManager.AddComponentData(state.SystemHandle, new Singleton()
                {
                    CarHashMap = carHashMap
                });
            }
        }

        void ISystemStartStop.OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            carHashMap.Clear();

            var fillHashMapJob = new FillHashMapJob()
            {
                CarHashMapParallel = carHashMap.AsParallelWriter(),
                HealthLookup = SystemAPI.GetComponentLookup<HealthComponent>(true),
                FactionLookup = SystemAPI.GetComponentLookup<FactionTypeComponent>(true),
                VelocityComponentLookup = SystemAPI.GetComponentLookup<VelocityComponent>(true),
                CarModelComponentLookup = SystemAPI.GetComponentLookup<CarModelComponent>(true),
            };

            fillHashMapJob.ScheduleParallel();
        }

        [WithAll(typeof(CarTag))]
        [BurstCompile]
        public partial struct FillHashMapJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            public NativeParallelMultiHashMap<int, CarHashEntityComponent>.ParallelWriter CarHashMapParallel;

            [ReadOnly]
            public ComponentLookup<HealthComponent> HealthLookup;

            [ReadOnly]
            public ComponentLookup<FactionTypeComponent> FactionLookup;

            [ReadOnly]
            public ComponentLookup<VelocityComponent> VelocityComponentLookup;

            [ReadOnly]
            public ComponentLookup<CarModelComponent> CarModelComponentLookup;

            void Execute(
                Entity entity,
                in BoundsComponent boundsComponent,
                in LocalToWorld worldTransform)
            {
                int hashKey = HashMapHelper.GetHashMapPosition(worldTransform.Position.Flat());
                int health = -1;
                int carModel = -1;
                var velocity = Vector3.zero;

                if (HealthLookup.HasComponent(entity))
                {
                    health = HealthLookup[entity].Value;
                }

                var faction = FactionType.All;

                if (FactionLookup.HasComponent(entity))
                {
                    faction = FactionLookup[entity].Value;
                }

                if (VelocityComponentLookup.HasComponent(entity))
                {
                    velocity = VelocityComponentLookup[entity].Value;
                }

                if (CarModelComponentLookup.HasComponent(entity))
                {
                    carModel = CarModelComponentLookup[entity].Value;
                }

                var hashEntity = new CarHashEntityComponent()
                {
                    Entity = entity,
                    CarModel = carModel,
                    Position = worldTransform.Position,
                    Rotation = worldTransform.Rotation,
                    Velocity = velocity,
                    BoundsExtents = boundsComponent.Extents,
                    Health = health,
                    FactionType = faction
                };

                CarHashMapParallel.Add(hashKey, hashEntity);
            }
        }
    }
}