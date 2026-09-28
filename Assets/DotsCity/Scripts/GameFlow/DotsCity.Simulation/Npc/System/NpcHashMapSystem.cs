using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Common;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Npc
{
    [UpdateInGroup(typeof(HashMapGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct NpcHashMapSystem : ISystem, ISystemStartStop
    {
        public struct Singleton : IComponentData
        {
            public NativeParallelMultiHashMap<int, NpcHashEntityComponent> NpcMultiHashMap;
        }

        public struct NpcHashEntityComponent
        {
            public Entity Entity;
            public float3 Position;
            public FactionType FactionType;
            public float ColliderRadius;
            public bool IsObstacle;
        }

        private EntityQuery npcQuery;
        private bool isInitialized;
        private NativeParallelMultiHashMap<int, NpcHashEntityComponent> npcMultiHashMap;

        void ISystem.OnCreate(ref SystemState state)
        {
            npcQuery = SystemAPI.QueryBuilder()
                .WithAll<NpcTypeComponent>()
                .Build();

            var configQuery = SystemAPI.QueryBuilder()
                .WithAll<NpcCommonConfigReference>()
                .Build();

            state.RequireForUpdate(configQuery);
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            if (npcMultiHashMap.IsCreated)
            {
                npcMultiHashMap.Dispose();
            }

            isInitialized = false;
        }

        void ISystemStartStop.OnStartRunning(ref SystemState state)
        {
            if (!isInitialized)
            {
                isInitialized = true;
                var npcConfig = SystemAPI.GetSingleton<NpcCommonConfigReference>().Config;

                npcMultiHashMap = new NativeParallelMultiHashMap<int, NpcHashEntityComponent>(npcConfig.Value.NpcHashMapCapacity, Allocator.Persistent);

                state.EntityManager.AddComponentData(state.SystemHandle, new Singleton()
                {
                    NpcMultiHashMap = npcMultiHashMap
                });
            }
        }

        void ISystemStartStop.OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var npcCount = npcQuery.CalculateEntityCount();

            if (npcMultiHashMap.Capacity < npcCount)
            {
                npcMultiHashMap.Capacity = npcCount;
            }

            npcMultiHashMap.Clear();

            if (npcCount == 0)
                return;

            var hashMapJob = new HashMapJob()
            {
                npcNativeMultiHashMapLocalParallel = npcMultiHashMap.AsParallelWriter(),
                CircleColliderLookup = SystemAPI.GetComponentLookup<CircleColliderComponent>(true),
                PedestrianStateLookup = SystemAPI.GetComponentLookup<StateComponent>(true),
                FactionLookup = SystemAPI.GetComponentLookup<FactionTypeComponent>(true),
                TrafficNpcObstacleConfigReference = SystemAPI.GetSingleton<TrafficNpcObstacleConfigReference>()
            };

            hashMapJob.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct HashMapJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            public NativeParallelMultiHashMap<int, NpcHashEntityComponent>.ParallelWriter npcNativeMultiHashMapLocalParallel;

            [ReadOnly]
            public ComponentLookup<CircleColliderComponent> CircleColliderLookup;

            [ReadOnly]
            public ComponentLookup<StateComponent> PedestrianStateLookup;

            [ReadOnly]
            public ComponentLookup<FactionTypeComponent> FactionLookup;

            [ReadOnly]
            public TrafficNpcObstacleConfigReference TrafficNpcObstacleConfigReference;

            void Execute(
                Entity entity,
                in NpcTypeComponent npcTypeComponent,
                in LocalTransform transform)
            {
                bool isObstacle = false;

                if (npcTypeComponent.Type == NpcType.Player ||
                    npcTypeComponent.Type == NpcType.Npc)
                {
                    isObstacle = true;
                }

                if (PedestrianStateLookup.HasComponent(entity))
                {
                    var stateComponent = PedestrianStateLookup[entity];

                    var pedestrianObstacleState = (int)TrafficNpcObstacleConfigReference.Config.Value.ObstacleActionStates;
                    var actionState = (int)stateComponent.ActionState;
                    var isObstacleState = (pedestrianObstacleState & actionState) != 0;

                    if (isObstacleState)
                    {
                        isObstacle = true;
                    }
                }

                var position = transform.Position;
                var flatPosition = position.Flat();
                int key = HashMapHelper.GetHashMapPosition(flatPosition);

                float colliderRadius = 0;

                if (CircleColliderLookup.HasComponent(entity))
                {
                    colliderRadius = CircleColliderLookup[entity].Radius;
                }

                var factionType = FactionType.All;

                if (FactionLookup.HasComponent(entity))
                {
                    factionType = FactionLookup[entity].Value;
                }

                var npcHashEntityComponent = new NpcHashEntityComponent()
                {
                    Entity = entity,
                    Position = position,
                    ColliderRadius = colliderRadius,
                    IsObstacle = isObstacle,
                    FactionType = factionType
                };

                npcNativeMultiHashMapLocalParallel.Add(key, npcHashEntityComponent);
            }
        }
    }
}