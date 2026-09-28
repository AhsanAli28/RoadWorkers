using Spirit604.Extensions;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    public partial struct PedestrianSpawnQueueSystem : ISystem
    {
        public struct Singleton : IComponentData/*, IEnableableComponent*/
        {
            internal NativeQueue<EventData> eventQueue;


            public void AddSpawnEvent(Entity nodeEntity, int skinIndex = -1)
            {
                eventQueue.Enqueue(new EventData { NodeEntity = nodeEntity, SkinIndex = skinIndex });
            }
        }

        public struct EventData
        {
            public Entity NodeEntity;
            public int SkinIndex;
        }

        private NativeQueue<EventData> eventQueue;

        void ISystem.OnCreate(ref SystemState state)
        {
            eventQueue = new NativeQueue<EventData>(Allocator.Persistent);

            state.EntityManager.AddComponentData(state.SystemHandle, new Singleton()
            {
                eventQueue = eventQueue
            });

            state.RequireForUpdate<Singleton>();
        }

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            eventQueue.Dispose();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            if (eventQueue.Count == 0)
                return;
            var queue = eventQueue;

            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var currentTime = (float)SystemAPI.Time.ElapsedTime;

            var nodeConnectionDataLookup = SystemAPI.GetBufferLookup<NodeConnectionDataElement>(true);
            var worldTransformLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);

            var pedestrianSettingsRef = SystemAPI.GetSingleton<PedestrianSettingsReference>();
            var prefabContainer = SystemAPI.GetSingletonBuffer<PedestrianEntityPrefabContainer>();

            while (queue.Count > 0)
            {
                var data = queue.Dequeue();

                var nodeEntity = data.NodeEntity;
                var skinIndex = data.SkinIndex;

                var rndGen = UnityMathematicsExtension.GetRandomGen(currentTime, nodeEntity);

                Entity prefabEntity = default;

                if (skinIndex == -1)
                {
                    prefabEntity = prefabContainer.GetRandomPrefab(rndGen, out skinIndex);
                }
                else
                {
                    if (prefabContainer.HasPrefabVariants())
                    {
                        prefabEntity = prefabContainer[skinIndex].PrefabEntity;
                    }
                    else
                    {
                        prefabEntity = prefabContainer[0].PrefabEntity;
                    }
                }

                if (prefabEntity == Entity.Null)
                    continue;

                var pedestrianSettings = pedestrianSettingsRef.Config;

                uint seed = UnityMathematicsExtension.GetSeed(currentTime, nodeEntity.Index);
                var nodeConnectionData = nodeConnectionDataLookup[nodeEntity];

                var parkingData = new ParkingSpawnHelper.ParkingSpawnData()
                {
                    Seed = seed,
                    PedestrianEntityPrefab = prefabEntity,
                    SpawnPointEntity = nodeEntity,
                    NodeConnectionData = nodeConnectionData,
                    WorldTransformLookup = worldTransformLookup,
                    SkinIndex = skinIndex,
                };

                ParkingSpawnHelper.Spawn(ref commandBuffer, in parkingData, in pedestrianSettings);
            }
        }
    }
}