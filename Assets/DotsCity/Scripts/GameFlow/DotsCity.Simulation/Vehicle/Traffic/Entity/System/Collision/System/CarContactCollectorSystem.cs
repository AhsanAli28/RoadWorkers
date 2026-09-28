using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(EarlyJobGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CarContactCollectorSystem : ISystem
    {
        private const float CollisionFrequency = 0.1f;

        private NativeParallelHashMap<EntityPair, CollisionData> contactHashMap;

        public static NativeParallelHashMap<EntityPair, CollisionData> ContactHashMapStaticRef { get; private set; }

        void ISystem.OnCreate(ref SystemState state)
        {
            contactHashMap = new NativeParallelHashMap<EntityPair, CollisionData>(128, Allocator.Persistent);
            ContactHashMapStaticRef = contactHashMap;
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            if (contactHashMap.IsCreated)
            {
                contactHashMap.Dispose();
                ContactHashMapStaticRef = default;
            }
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();

            if (contactHashMap.IsEmpty)
            {
                return;
            }

            var time = (float)SystemAPI.Time.ElapsedTime;

            var listToRemove = new NativeList<EntityPair>(Allocator.TempJob);

            var contantJob = new ContactJob()
            {
                ContactHashMap = contactHashMap,
                ListToRemove = listToRemove,
                Timestamp = time,
            }.Schedule(state.Dependency);

            state.Dependency = new RemoveJob()
            {
                ContactHashMap = contactHashMap,
                ListToRemove = listToRemove,
            }.Schedule(contantJob);

            listToRemove.Dispose(state.Dependency);
        }

        [BurstCompile]
        public struct ContactJob : IJob
        {
            [ReadOnly]
            public NativeParallelHashMap<EntityPair, CollisionData> ContactHashMap;

            public NativeList<EntityPair> ListToRemove;

            [ReadOnly]
            public float Timestamp;

            public void Execute()
            {
                foreach (var item in ContactHashMap)
                {
                    var enoughTimePassed = Timestamp - item.Value.ActivateTime >= CollisionFrequency;

                    if (enoughTimePassed)
                    {
                        ListToRemove.Add(item.Key);
                    }
                }
            }
        }

        [BurstCompile]
        public struct RemoveJob : IJob
        {
            public NativeParallelHashMap<EntityPair, CollisionData> ContactHashMap;

            [ReadOnly]
            public NativeList<EntityPair> ListToRemove;

            public void Execute()
            {
                foreach (var item in ListToRemove)
                {
                    ContactHashMap.Remove(item);
                }
            }
        }
    }
}