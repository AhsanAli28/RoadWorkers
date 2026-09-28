using Pathfinding.ECS;
using Spirit604.DotsCity.Simulation.Navigation;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using AutoRepathPolicy = Pathfinding.ECS.AutoRepathPolicy;

namespace Spirit604.DotsCity.ThirdParty.Astar
{
    [UpdateInGroup(typeof(MainThreadEventPlaybackGroup), OrderFirst = true)]
    [DisableAutoCreation]
    [DisableAutoTypeRegistration]
    [BurstCompile]
    public partial struct AstarPedestrianStructuralSystem : ISystem
    {
        public struct Singleton : IComponentData
        {
            internal NativeQueue<EventData> eventQueue;

            public void AddComponents(Entity entity)
            {
                eventQueue.Enqueue(new EventData { Entity = entity, Type = EventType.AddComponents });
            }

            public void RemoveComponents(Entity entity)
            {
                eventQueue.Enqueue(new EventData { Entity = entity, Type = EventType.RemoveComponents });
            }

            public void RemoveComponentsNonStructural(Entity entity)
            {
                eventQueue.Enqueue(new EventData { Entity = entity, Type = EventType.RemoveComponentsNoStructural });
            }
        }

        internal enum EventType { AddComponents, RemoveComponents, RemoveComponentsNoStructural }

        internal struct EventData
        {
            public Entity Entity;
            public EventType Type;
        }

        private NativeQueue<EventData> eventQueue;

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

        void ISystem.OnUpdate(ref SystemState state)
        {
            if (eventQueue.Count == 0)
                return;

            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            while (eventQueue.Count > 0)
            {
                EventData eventData = eventQueue.Dequeue();
                var entity = eventData.Entity;

                switch (eventData.Type)
                {
                    case EventType.AddComponents:
                        {
                            commandBuffer.AddComponent<CustomReachTargetTag>(entity);
                            commandBuffer.AddComponent<PedestrianPathfindingSystem.PathRequest>(entity);
                            commandBuffer.SetComponentEnabled<PedestrianPathfindingSystem.PathRequest>(entity, false);

                            commandBuffer.AddComponent<PathAgentComponent>(entity);

                            commandBuffer.AddComponent<PathAgentUpdateState>(entity);
                            commandBuffer.SetComponentEnabled<PathAgentUpdateState>(entity, false);

                            commandBuffer.AddBuffer<NodeSequenceElement>(entity);

                            commandBuffer.AddComponent(entity, new AutoRepathPolicy()
                            {
                                mode = Pathfinding.AutoRepathPolicy.Mode.Never,
                            });

                            commandBuffer.AddComponent<DestinationPoint>(entity);
                            commandBuffer.AddComponent(entity, new AgentMovementPlane()
                            {
                                value = new Pathfinding.Util.NativeMovementPlane(quaternion.identity)
                            });

                            commandBuffer.AddComponent(entity, new ManagedState());

                            break;
                        }
                    case EventType.RemoveComponents:
                        {
                            var managedState = state.EntityManager.GetComponentData<ManagedState>(entity);
                            managedState.ClearPath();
                            managedState.CancelCurrentPathRequest();

                            commandBuffer.RemoveComponent<CustomReachTargetTag>(entity);
                            commandBuffer.RemoveComponent<NodeSequenceElement>(entity);
                            commandBuffer.RemoveComponent<PathAgentComponent>(entity);
                            commandBuffer.RemoveComponent<PathAgentUpdateState>(entity);
                            commandBuffer.RemoveComponent<PedestrianPathfindingSystem.PathRequest>(entity);

                            commandBuffer.SetComponentEnabled<ReachTargetTag>(entity, true);

                            commandBuffer.RemoveComponent<DestinationPoint>(entity);
                            commandBuffer.RemoveComponent<AutoRepathPolicy>(entity);
                            commandBuffer.RemoveComponent<AgentMovementPlane>(entity);
                            commandBuffer.RemoveComponent<ManagedState>(entity);
                            break;
                        }
                    case EventType.RemoveComponentsNoStructural:
                        {
                            var managedState = state.EntityManager.GetComponentData<ManagedState>(entity);
                            managedState.ClearPath();
                            managedState.CancelCurrentPathRequest();

                            commandBuffer.SetComponentEnabled<CustomReachTargetTag>(entity, false);
                            break;
                        }
                }
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
    }
}
