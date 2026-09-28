using Pathfinding.ECS;
using Spirit604.DotsCity.Simulation.Navigation;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.DotsCity.Simulation.TrafficPublic;
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
    public partial struct AstarTrafficStructuralSystem : ISystem
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

            public void AddComponentsNonStructural(Entity entity)
            {
                eventQueue.Enqueue(new EventData { Entity = entity, Type = EventType.AddComponentsNoStructural });
            }

            public void RemoveComponentsNonStructural(Entity entity)
            {
                eventQueue.Enqueue(new EventData { Entity = entity, Type = EventType.RemoveComponentsNoStructural });
            }
        }

        internal enum EventType { AddComponents, RemoveComponents, AddComponentsNoStructural, RemoveComponentsNoStructural }

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
            var trafficNoTargetTagLookup = SystemAPI.GetComponentLookup<TrafficNoTargetTag>(true);
            var trafficStateComponentLookup = SystemAPI.GetComponentLookup<TrafficStateComponent>(false);

            while (eventQueue.Count > 0)
            {
                EventData eventData = eventQueue.Dequeue();
                var trafficEntity = eventData.Entity;

                switch (eventData.Type)
                {
                    case EventType.AddComponents:
                        {
                            commandBuffer.SetComponentEnabled<TrafficDefaultTag>(trafficEntity, false);
                            commandBuffer.AddComponent<TrafficFixedRouteTag>(trafficEntity);
                            commandBuffer.AddComponent<TrafficFixedRouteComponent>(trafficEntity);
                            commandBuffer.AddComponent<PathAgentComponent>(trafficEntity);

                            commandBuffer.AddComponent<PathAgentUpdateState>(trafficEntity);
                            commandBuffer.SetComponentEnabled<PathAgentUpdateState>(trafficEntity, false);

                            commandBuffer.AddBuffer<FixedRouteNodeElement>(trafficEntity);

                            commandBuffer.AddComponent(trafficEntity, new AutoRepathPolicy()
                            {
                                mode = Pathfinding.AutoRepathPolicy.Mode.Never,
                            });

                            commandBuffer.AddComponent<DestinationPoint>(trafficEntity);
                            commandBuffer.AddComponent(trafficEntity, new AgentMovementPlane()
                            {
                                value = new Pathfinding.Util.NativeMovementPlane(quaternion.identity)
                            });

                            commandBuffer.AddComponent(trafficEntity, new ManagedState());

                            break;
                        }
                    case EventType.RemoveComponents:
                        {
                            var managedState = state.EntityManager.GetComponentData<ManagedState>(trafficEntity);
                            managedState.ClearPath();
                            managedState.CancelCurrentPathRequest();

                            commandBuffer.SetComponentEnabled<TrafficDefaultTag>(trafficEntity, true);
                            commandBuffer.RemoveComponent<TrafficFixedRouteTag>(trafficEntity);
                            commandBuffer.RemoveComponent<TrafficFixedRouteComponent>(trafficEntity);
                            commandBuffer.RemoveComponent<FixedRouteNodeElement>(trafficEntity);
                            commandBuffer.RemoveComponent<PathAgentComponent>(trafficEntity);
                            commandBuffer.RemoveComponent<PathAgentUpdateState>(trafficEntity);

                            if (trafficNoTargetTagLookup.HasComponent(trafficEntity))
                            {
                                commandBuffer.SetComponentEnabled<TrafficNextTrafficNodeRequestTag>(trafficEntity, true);

                                var trafficStateComponent = trafficStateComponentLookup[trafficEntity];

                                if (TrafficStateExtension.RemoveIdleState<TrafficNoTargetTag>(ref commandBuffer, trafficEntity, ref trafficStateComponent, TrafficIdleState.NoTarget))
                                {

                                }

                                trafficStateComponentLookup[trafficEntity] = trafficStateComponent;
                            }

                            commandBuffer.RemoveComponent<DestinationPoint>(trafficEntity);
                            commandBuffer.RemoveComponent<AutoRepathPolicy>(trafficEntity);
                            commandBuffer.RemoveComponent<AgentMovementPlane>(trafficEntity);
                            commandBuffer.RemoveComponent<ManagedState>(trafficEntity);

                            break;
                        }
                    case EventType.AddComponentsNoStructural:
                        {
                            commandBuffer.SetComponentEnabled<TrafficDefaultTag>(trafficEntity, false);
                            commandBuffer.SetComponentEnabled<TrafficFixedRouteTag>(trafficEntity, true);
                            break;
                        }
                    case EventType.RemoveComponentsNoStructural:
                        {
                            var managedState = state.EntityManager.GetComponentData<ManagedState>(trafficEntity);
                            managedState.ClearPath();
                            managedState.CancelCurrentPathRequest();

                            commandBuffer.SetComponentEnabled<TrafficDefaultTag>(trafficEntity, true);
                            commandBuffer.SetComponentEnabled<TrafficFixedRouteTag>(trafficEntity, false);

                            if (trafficNoTargetTagLookup.HasComponent(trafficEntity))
                            {
                                commandBuffer.SetComponentEnabled<TrafficNextTrafficNodeRequestTag>(trafficEntity, true);

                                var trafficStateComponent = trafficStateComponentLookup[trafficEntity];

                                if (TrafficStateExtension.RemoveIdleState<TrafficNoTargetTag>(ref commandBuffer, trafficEntity, ref trafficStateComponent, TrafficIdleState.NoTarget))
                                {

                                }

                                trafficStateComponentLookup[trafficEntity] = trafficStateComponent;
                            }

                            break;
                        }
                }
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
    }
}
