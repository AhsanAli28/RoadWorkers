using Spirit604.DotsCity.Simulation.Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.ThirdParty.Astar
{
    [UpdateInGroup(typeof(MainThreadEventPlaybackGroup))]
    [DisableAutoCreation]
    [DisableAutoTypeRegistration]
    [BurstCompile]
    public partial struct AstarUpdateStateEventSystem : ISystem
    {
        public struct Singleton : IComponentData
        {
            public NativeQueue<EventData> eventQueue;

            public void UpdateState(Entity entity, PathStatus status)
            {
                eventQueue.Enqueue(new EventData { Entity = entity, Status = status });
            }
        }

        public struct EventData
        {
            public Entity Entity;
            public PathStatus Status;
        }

        private NativeQueue<EventData> eventQueue;
        private EntityQuery npcQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            eventQueue = new NativeQueue<EventData>(Allocator.Persistent);

            state.EntityManager.AddComponentData(state.SystemHandle, new Singleton()
            {
                eventQueue = eventQueue
            });
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

            var pathAgentComponentLookup = SystemAPI.GetComponentLookup<PathAgentComponent>(false);
            var pathAgentUpdateStateLookup = SystemAPI.GetComponentLookup<PathAgentUpdateState>(false);

            while (eventQueue.Count > 0)
            {
                EventData eventData = eventQueue.Dequeue();
                var entity = eventData.Entity;
                var status = eventData.Status;

                pathAgentComponentLookup[entity] = new PathAgentComponent()
                {
                    Status = status,
                };

                pathAgentUpdateStateLookup.SetComponentEnabled(entity, true);
            }
        }
    }
}
