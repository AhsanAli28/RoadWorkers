using Pathfinding;
using Pathfinding.ECS;
using Spirit604.DotsCity.Simulation.Navigation;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using AutoRepathPolicy = Pathfinding.ECS.AutoRepathPolicy;

namespace Spirit604.DotsCity.ThirdParty.Astar
{
    public abstract partial class AstarPathEventSystemBase : SystemBase
    {
        private enum EventType { FindPath, NotFound, Found }

        private struct EventData
        {
            public Entity Entity;
            public Entity TargetEntity;
            public GraphMask GraphMask;
            public EventType Type;
        }

        protected AstarEntityGraphBase astarGraph;

        private Dictionary<Entity, Entity> targets = new Dictionary<Entity, Entity>();
        private Dictionary<Path, Entity> callbacks = new Dictionary<Path, Entity>();
        private Dictionary<Entity, Path> binding = new Dictionary<Entity, Path>();

        private NativeQueue<EventData> eventQueue;

        public Dictionary<Entity, Entity> Targets { get => targets; }

        protected override void OnCreate()
        {
            base.OnCreate();
            eventQueue = new NativeQueue<EventData>(Allocator.Persistent);
            Enabled = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            eventQueue.Dispose();
        }

        protected override void OnUpdate()
        {
            var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var destinationPointLookup = SystemAPI.GetComponentLookup<DestinationPoint>(false);
            var autoRepathPolicyLookup = SystemAPI.GetComponentLookup<AutoRepathPolicy>(false);
            var pathAgentComponentLookup = SystemAPI.GetComponentLookup<PathAgentComponent>(false);
            var pathAgentUpdateStateLookup = SystemAPI.GetComponentLookup<PathAgentUpdateState>(false);

            OnStartUpdate();

            while (eventQueue.Count > 0)
            {
                EventData eventData = eventQueue.Dequeue();
                var entity = eventData.Entity;
                var type = eventData.Type;
                var endNode = eventData.TargetEntity;

                switch (type)
                {
                    case EventType.FindPath:
                        {
                            if (!targets.ContainsKey(entity))
                                targets.Add(entity, endNode);

                            if (binding.ContainsKey(entity))
                                binding.Remove(entity);

                            if (localTransformLookup.HasComponent(entity) && localTransformLookup.HasComponent(endNode))
                            {
                                targets[entity] = endNode;

                                float3 position = GetStartPosition(entity, in localTransformLookup);
                                var dest = localTransformLookup[endNode].Position;
                                var autoRepathPolicy = autoRepathPolicyLookup[entity];
                                var movementPlane = new AgentMovementPlane(quaternion.identity);

                                var managedState = EntityManager.GetComponentData<ManagedState>(entity);

                                var destination = new DestinationPoint()
                                {
                                    destination = dest
                                };

                                var path = ABPath.Construct(position, dest, Path_OnComplete);

                                path.UseSettings(managedState.pathfindingSettings);

                                path.nnConstraint.graphMask = eventData.GraphMask;

                                callbacks.Add(path, entity);
                                ManagedState.SetPath(path, managedState, in movementPlane, ref destination);
                                //autoRepathPolicy.DidRecalculatePath(destination.destination, (float)World.DefaultGameObjectInjectionWorld.Time.ElapsedTime);

                                destinationPointLookup[entity] = destination;
                                autoRepathPolicyLookup[entity] = autoRepathPolicy;

                                UpdateStatus(ref pathAgentComponentLookup, ref pathAgentUpdateStateLookup, entity, PathStatus.WaitingForPath);
                            }
                            else
                            {
                                UpdateStatus(ref pathAgentComponentLookup, ref pathAgentUpdateStateLookup, entity, PathStatus.TargetNotExist);
                            }

                            break;
                        }
                    case EventType.NotFound:
                        {
                            if (pathAgentComponentLookup.HasComponent(entity))
                            {
                                UpdateStatus(ref pathAgentComponentLookup, ref pathAgentUpdateStateLookup, entity, PathStatus.NotFound);
                            }

                            break;
                        }
                    case EventType.Found:
                        {
                            if (binding.TryGetValue(entity, out var path))
                            {
                                binding.Remove(entity);

                                if (pathAgentComponentLookup.HasComponent(entity))
                                {
                                    PathOnCompleteInternal(path, entity, in localTransformLookup);

                                    UpdateStatus(ref pathAgentComponentLookup, ref pathAgentUpdateStateLookup, entity, PathStatus.Found);
                                }
                            }

                            break;
                        }
                }
            }

            Enabled = false;
        }

        protected abstract void PathOnCompleteInternal(Path path, Entity entity, in ComponentLookup<LocalTransform> localTransformLookup);

        protected virtual void OnStartUpdate() { }

        private void UpdateStatus(
            ref ComponentLookup<PathAgentComponent> pathAgentComponentLookup,
            ref ComponentLookup<PathAgentUpdateState> pathAgentUpdateStateLookup,
            Entity entity,
            PathStatus status)
        {
            pathAgentComponentLookup[entity] = new PathAgentComponent()
            {
                Status = status
            };

            pathAgentUpdateStateLookup.SetComponentEnabled(entity, true);
        }

        public void Initialize(AstarEntityGraphBase astarGraph)
        {
            this.astarGraph = astarGraph;
        }

        public void SetTarget(Entity entity, Entity endNode, GraphMask graphMask)
        {
            eventQueue.Enqueue(new EventData()
            {
                Entity = entity,
                TargetEntity = endNode,
                GraphMask = graphMask,
            });

            Enabled = true;
        }

        public void RemoveEntity(Entity entity)
        {
            if (targets.ContainsKey(entity))
                targets.Remove(entity);
        }

        protected virtual float3 GetStartPosition(Entity entity, in ComponentLookup<LocalTransform> componentLookup)
        {
            return componentLookup[entity].Position;
        }

        private void Path_OnComplete(Path path)
        {
            path.callback -= Path_OnComplete;

            var entity = callbacks[path];
            callbacks.Remove(path);
            Enabled = true;

            if (path.error)
            {
                eventQueue.Enqueue(new EventData { Entity = entity, Type = EventType.NotFound });
                return;
            }

            binding.Add(entity, path);

            eventQueue.Enqueue(new EventData { Entity = entity, Type = EventType.Found });
        }
    }
}
