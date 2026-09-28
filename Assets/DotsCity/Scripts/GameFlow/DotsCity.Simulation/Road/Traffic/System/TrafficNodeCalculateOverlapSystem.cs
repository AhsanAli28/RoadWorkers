using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Road
{
    [UpdateAfter(typeof(CarHashMapSystem))]
    [UpdateInGroup(typeof(HashMapGroup))]
    [BurstCompile]
    public partial struct TrafficNodeCalculateOverlapSystem : ISystem
    {
        public enum CalculateMethod { LowAccuracy, HighAccuracy }

        private SystemHandle carHashMapSystem;
        private SystemHandle calcCullingSystem;
        private EntityQuery updateQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            carHashMapSystem = state.WorldUnmanaged.GetExistingUnmanagedSystem<CarHashMapSystem>();
            calcCullingSystem = state.WorldUnmanaged.GetExistingUnmanagedSystem<CalcCullingSystem>();

            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<CulledEventTag>()
                .WithAll<TrafficNodeAvailableComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
            state.RequireForUpdate<CarHashMapSystem.Singleton>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();

            ref var carHashMapSystemRef = ref state.WorldUnmanaged.ResolveSystemStateRef(carHashMapSystem);
            ref var calcCullingSystemRef = ref state.WorldUnmanaged.ResolveSystemStateRef(calcCullingSystem);

            var depJob = JobHandle.CombineDependencies(carHashMapSystemRef.Dependency, calcCullingSystemRef.Dependency);

            var overlapNodeJob = new OverlapNodeJob()
            {
                CarHashMapSingleton = SystemAPI.GetSingleton<CarHashMapSystem.Singleton>(),
                TrafficRoadConfigReference = SystemAPI.GetSingleton<TrafficRoadOverlapConfigReference>()
            };

            state.Dependency = overlapNodeJob.ScheduleParallel(depJob);
        }

        [WithNone(typeof(CulledEventTag))]
        [BurstCompile]
        public partial struct OverlapNodeJob : IJobEntity
        {
            [ReadOnly]
            public CarHashMapSystem.Singleton CarHashMapSingleton;

            [ReadOnly]
            public TrafficRoadOverlapConfigReference TrafficRoadConfigReference;

            void Execute(
                ref TrafficNodeAvailableComponent trafficNodeAvailableComponent,
                in LocalToWorld worldTransform)
            {
                bool isAvailable = true;

                var position = worldTransform.Position;
                var keys = HashMapHelper.GetHashMapPosition9Cells(position.Flat());

                for (int i = 0; i < keys.Length; i++)
                {
                    if (CarHashMapSingleton.CarHashMap.TryGetFirstValue(keys[i], out var carHashEntity, out var nativeMultiHashMapIterator))
                    {
                        do
                        {
                            switch (TrafficRoadConfigReference.Config.Value.CalculateMethod)
                            {
                                case CalculateMethod.LowAccuracy:
                                    {
                                        var maxDistance = carHashEntity.BoundsSize.z * TrafficRoadConfigReference.Config.Value.SizeMultiplier;

                                        float3 carPosition = carHashEntity.Position;
                                        float distance = math.distance(carPosition, worldTransform.Position);

                                        if (distance < maxDistance)
                                        {
                                            isAvailable = false;
                                        }

                                        break;
                                    }
                                case CalculateMethod.HighAccuracy:
                                    {
                                        var toCarDirection = math.normalize(carHashEntity.Position - position).Flat();
                                        var nodeForward = worldTransform.Forward;

                                        var lerpDot = math.abs(math.dot(toCarDirection, nodeForward));

                                        var relativeSize = math.lerp(carHashEntity.BoundsSize.x, carHashEntity.BoundsSize.z, lerpDot);
                                        relativeSize *= TrafficRoadConfigReference.Config.Value.SizeMultiplier;

                                        float3 carPosition = carHashEntity.Position;
                                        float distance = math.distance(carPosition, position);

                                        if (distance < relativeSize)
                                        {
                                            isAvailable = false;
                                        }

                                        break;
                                    }
                            }

                            if (!isAvailable)
                            {
                                break;
                            }

                        } while (CarHashMapSingleton.CarHashMap.TryGetNextValue(out carHashEntity, ref nativeMultiHashMapIterator));
                    }

                    if (!isAvailable)
                    {
                        break;
                    }
                }

                keys.Dispose();

                if (trafficNodeAvailableComponent.IsAvailable != isAvailable)
                {
                    trafficNodeAvailableComponent.IsAvailable = isAvailable;
                }
            }
        }
    }
}