using Spirit604.DotsCity.Simulation.Navigation;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using static Spirit604.DotsCity.Simulation.Traffic.TrafficPathfindingSystem;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(MainThreadEventPlaybackGroup))]
    [DisableAutoCreation]
    [DisableAutoTypeRegistration]
    [BurstCompile]
    public partial struct TrafficCustomPathEventSystem : ISystem
    {
        public struct Singleton : IComponentData
        {
            internal NativeQueue<EventData> eventQueue;

            public void FindPath(Entity entity, Entity targetEntity)
            {
                eventQueue.Enqueue(new EventData { Entity = entity, TargetEntity = targetEntity, Type = EventType.FindPath });
            }

            public void RecalculatePath(Entity entity)
            {
                eventQueue.Enqueue(new EventData { Entity = entity, Type = EventType.RecalculatePath });
            }

            public void StartPath(Entity entity)
            {
                eventQueue.Enqueue(new EventData { Entity = entity, Type = EventType.StartPath });
            }
        }

        internal enum EventType { FindPath, RecalculatePath, StartPath }

        internal struct EventData
        {
            public Entity Entity;
            public Entity TargetEntity;
            public EventType Type;
        }

        private NativeQueue<EventData> eventQueue;
        private EntityQuery npcQuery;
        private int skipped;
        private int maxSkipped;
        private bool skip;

        void ISystem.OnCreate(ref SystemState state)
        {
            maxSkipped = 2;

            eventQueue = new NativeQueue<EventData>(Allocator.Persistent);

            state.EntityManager.AddComponentData(state.SystemHandle, new Singleton()
            {
                eventQueue = eventQueue
            });

            state.RequireForUpdate<TrafficPathAgentConfigReference>();
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

            if (skip)
            {
                if (skipped < maxSkipped)
                {
                    skipped++;
                    return;
                }
                else
                {
                    skip = false;
                }
            }

            var trafficPathComponentLookup = SystemAPI.GetComponentLookup<TrafficPathComponent>(true);
            var pathRequestLookup = SystemAPI.GetComponentLookup<TrafficPathfindingSystem.PathRequest>(false);
            var pathAgentComponentLookup = SystemAPI.GetComponentLookup<PathAgentComponent>(false);
            var pathAgentUpdateStateLookup = SystemAPI.GetComponentLookup<PathAgentUpdateState>(false);
            var trafficDefaultLookup = SystemAPI.GetComponentLookup<TrafficDefaultTag>(false);
            var trafficFixedRouteLookup = SystemAPI.GetComponentLookup<TrafficFixedRouteTag>(false);

            var limitPerFrame = SystemAPI.GetSingleton<TrafficPathAgentConfigReference>().Config.Value.AgentCount;

            int index = 0;

            while (eventQueue.Count > 0)
            {
                EventData eventData = eventQueue.Dequeue();
                var entity = eventData.Entity;
                var type = eventData.Type;

                switch (type)
                {
                    case EventType.FindPath:
                        {
                            if (trafficPathComponentLookup.TryGetComponent(entity, out var pathComponent))
                            {
                                pathRequestLookup[entity] = new PathRequest()
                                {
                                    StartPathIndex = pathComponent.CurrentGlobalPathIndex,
                                    TargetNode = eventData.TargetEntity,
                                    Index = index,
                                };

                                pathAgentComponentLookup[entity] = new PathAgentComponent()
                                {
                                    Status = PathStatus.WaitingForPath
                                };

                                pathRequestLookup.SetComponentEnabled(entity, true);
                                trafficDefaultLookup.SetComponentEnabled(entity, false);
                                trafficFixedRouteLookup.SetComponentEnabled(entity, true);
                            }

                            break;
                        }
                    case EventType.RecalculatePath:
                        {
                            if (pathAgentComponentLookup[entity].Status == PathStatus.Complete)
                                continue;

                            pathAgentComponentLookup[entity] = new PathAgentComponent()
                            {
                                Status = PathStatus.WaitingForPath
                            };

                            pathRequestLookup.SetComponentEnabled(entity, true);
                            trafficDefaultLookup.SetComponentEnabled(entity, false);
                            trafficFixedRouteLookup.SetComponentEnabled(entity, true);
                            break;
                        }
                    case EventType.StartPath:
                        {
                            pathAgentComponentLookup[entity] = new PathAgentComponent()
                            {
                                Status = PathStatus.OnTheWay
                            };

                            pathAgentUpdateStateLookup.SetComponentEnabled(entity, true);

                            break;
                        }
                }

                index++;
                limitPerFrame--;

                if (limitPerFrame == 0) break;
            }

            if (eventQueue.Count != 0 && !skip)
            {
                skip = true;
                skipped = 0;
            }
        }
    }
}
