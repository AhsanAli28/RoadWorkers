using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.DotsCity.Simulation.Traffic.Obstacle;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public abstract class RuntimeRoadManagerBase : MonoBehaviourBase
    {
#if RUNTIME_ROAD

        private EntityQuery graphQuery;
        private EntityQuery nodeResolverQuery;
        private Entity graphEntity;

        protected PathGraphSystem.Singleton graph;

        public bool Init { get; private set; }

        public TrafficNodeResolverSystem.RuntimePathDataRef nodeResolver { get; private set; }

        public TrafficObstacleSystem.Singleton CarHashMap
        {
            get
            {
                var trafficObstacleSystem = World.DefaultGameObjectInjectionWorld.Unmanaged.GetExistingUnmanagedSystem<TrafficObstacleSystem>();

                if (EntityManager.HasComponent<TrafficObstacleSystem.Singleton>(trafficObstacleSystem))
                {
                    return EntityManager.GetComponentData<TrafficObstacleSystem.Singleton>(trafficObstacleSystem);
                }

                return default;
            }
        }

        public PedestrianPathHashMapSystem.Singleton PedestrianHashMap
        {
            get
            {
                var pedestrianPathHashMapSystem = World.DefaultGameObjectInjectionWorld.Unmanaged.GetExistingUnmanagedSystem<PedestrianPathHashMapSystem>();

                if (EntityManager.HasComponent<PedestrianPathHashMapSystem.Singleton>(pedestrianPathHashMapSystem))
                {
                    return EntityManager.GetComponentData<PedestrianPathHashMapSystem.Singleton>(pedestrianPathHashMapSystem);
                }

                return default;
            }
        }

        public TrafficNextPathHashMapSystem.Singleton TrafficNextMap
        {
            get
            {
                var pedestrianPathHashMapSystem = World.DefaultGameObjectInjectionWorld.Unmanaged.GetExistingUnmanagedSystem<TrafficNextPathHashMapSystem>();

                if (EntityManager.HasComponent<TrafficNextPathHashMapSystem.Singleton>(pedestrianPathHashMapSystem))
                {
                    return EntityManager.GetComponentData<TrafficNextPathHashMapSystem.Singleton>(pedestrianPathHashMapSystem);
                }

                return default;
            }
        }

        protected EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

#endif

        protected virtual void Awake()
        {
#if !RUNTIME_ROAD
            Debug.Log($"RuntimeRoadManager. Make sure you have added the scripting define 'RUNTIME_ROAD' to the project's player settings for the current platform.\r\n" +
                "For more info read the <a href=\"https://dotstrafficcity.readthedocs.io/en/latest/runtimeRoad.html#installation\">https://dotstrafficcity.readthedocs.io/en/latest/runtimeRoad.html#installation</a>\r\n\r\n\r\n\r\n\r\n");
#else
            graphQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PathGraphSystem.Singleton>());
            nodeResolverQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficNodeResolverSystem.RuntimePathDataRef>());
#endif
        }

#if RUNTIME_ROAD
        protected IEnumerator InitRoutine()
        {
            while (true)
            {
                if (nodeResolverQuery.CalculateEntityCount() != 0 && graphQuery.CalculateEntityCount() != 0) break;

                yield return null;
            }

            Init = true;

            graphEntity = graphQuery.GetSingletonEntity();
            graph = graphQuery.GetSingleton<PathGraphSystem.Singleton>();
            nodeResolver = nodeResolverQuery.GetSingleton<TrafficNodeResolverSystem.RuntimePathDataRef>();
        }

        protected void DestroyPedestrian(ref EntityCommandBuffer commandBuffer, Entity sourceEntity, ref NativeList<Entity> connectedEntities, bool destroyEntityImmediate = false)
        {
            connectedEntities.Clear();
            PedestrianNodeEntityUtils.GetConnectedEntities(EntityManager, sourceEntity, ref connectedEntities);

            for (int n = 0; n < connectedEntities.Length; n++)
            {
                var connectedEntity = connectedEntities[n];
                DestroyPedestrian(ref commandBuffer, sourceEntity, connectedEntity, destroyEntityImmediate);
            }
        }

        protected void DestroyPedestrian(ref EntityCommandBuffer commandBuffer, Entity sourceNodeEntity, Entity connectedNodeEntity, bool destroyEntityImmediate = false)
        {
            var hashMap = this.PedestrianHashMap.HashMap;

            if (!hashMap.IsCreated)
                return;

            var pair = new EntityPair(sourceNodeEntity, connectedNodeEntity);

            if (hashMap.TryGetFirstValue(pair, out var hashData, out var nativeMultiHashMapIterator))
            {
                do
                {
                    if (!EntityManager.HasComponent<PooledEventTag>(hashData.Entity))
                        continue;

                    if (!destroyEntityImmediate)
                    {
                        commandBuffer.SetComponentEnabled<PooledEventTag>(hashData.Entity, true);
                    }
                    else
                    {
                        commandBuffer.DestroyEntity(hashData.Entity);
                    }

                } while (hashMap.TryGetNextValue(out hashData, ref nativeMultiHashMapIterator));
            }
        }

        protected void UnselectPaths(ref EntityCommandBuffer commandBuffer, NativeList<int> destroyPaths, NativeHashSet<Entity> ignoreEntities = default)
        {
            var trafficNextMap = this.TrafficNextMap.HashMap;

            if (!trafficNextMap.IsCreated)
                return;

            for (int i = 0; i < destroyPaths.Length; i++)
            {
                var pathIndex = destroyPaths[i];

                UnselectPath(ref commandBuffer, pathIndex, ignoreEntities);
            }
        }

        protected void UnselectPath(ref EntityCommandBuffer commandBuffer, int pathIndex, NativeHashSet<Entity> ignoreEntities = default)
        {
            if (this.TrafficNextMap.HashMap.TryGetFirstValue(pathIndex, out var carHashData2, out var nativeMultiHashMapIterator2))
            {
                do
                {
                    if (!EntityManager.HasComponent<TrafficDestinationComponent>(carHashData2.Entity))
                        continue;

                    if (ignoreEntities.IsCreated && ignoreEntities.Contains(carHashData2.Entity))
                        continue;

                    var dest = EntityManager.GetComponentData<TrafficDestinationComponent>(carHashData2.Entity);

                    dest.NextDestinationNode = Entity.Null;
                    dest.NextGlobalPathIndex = -1;

                    commandBuffer.SetComponent(carHashData2.Entity, dest);

                } while (this.TrafficNextMap.HashMap.TryGetNextValue(out carHashData2, ref nativeMultiHashMapIterator2));
            }
        }

        protected void DestroyTraffic(ref EntityCommandBuffer commandBuffer, NativeList<int> destroyPaths, bool uncludeUnselection = true, bool immediate = false)
        {
            var carHashMap = this.CarHashMap.CarHashMap;

            if (!carHashMap.IsCreated)
                return;

            for (int i = 0; i < destroyPaths.Length; i++)
            {
                var pathIndex = destroyPaths[i];

                if (uncludeUnselection)
                {
                    UnselectPath(ref commandBuffer, pathIndex);
                }

                if (carHashMap.TryGetFirstValue(pathIndex, out var carHashData, out var nativeMultiHashMapIterator))
                {
                    do
                    {
                        if (!EntityManager.HasComponent<PooledEventTag>(carHashData.Entity))
                            continue;

                        if (!immediate)
                        {
                            commandBuffer.SetComponentEnabled<PooledEventTag>(carHashData.Entity, true);
                        }
                        else
                        {
                            if (EntityManager.HasComponent<Transform>(carHashData.Entity))
                            {
                                var transform = EntityManager.GetComponentObject<Transform>(carHashData.Entity);
                                transform.gameObject.ReturnToPool();
                            }

                            commandBuffer.DestroyEntity(carHashData.Entity);
                        }

                    } while (carHashMap.TryGetNextValue(out carHashData, ref nativeMultiHashMapIterator));
                }
            }
        }

        protected void DestroyTrafficRecord(NativeList<int> destroyPaths, ref NativeHashSet<Entity> destroyEntities)
        {
            for (int i = 0; i < destroyPaths.Length; i++)
            {
                DestroyTrafficRecord(destroyPaths[i], ref destroyEntities);
            }
        }

        protected void DestroyTrafficRecord(int pathIndex, ref NativeHashSet<Entity> destroyEntities)
        {
            var carHashMap = this.CarHashMap.CarHashMap;

            if (!carHashMap.IsCreated)
                return;

            if (carHashMap.TryGetFirstValue(pathIndex, out var carHashData, out var nativeMultiHashMapIterator))
            {
                do
                {
                    if (!EntityManager.HasComponent<PooledEventTag>(carHashData.Entity))
                        continue;

                    if (EntityManager.HasComponent<Transform>(carHashData.Entity))
                    {
                        var transform = EntityManager.GetComponentObject<Transform>(carHashData.Entity);
                        transform.gameObject.ReturnToPool();
                    }

                    destroyEntities.Add(carHashData.Entity);

                } while (carHashMap.TryGetNextValue(out carHashData, ref nativeMultiHashMapIterator));
            }
        }

        protected Entity CreateLightEntity(ref EntityCommandBuffer commandBuffer, List<LightStateInfo> lightStates, int crossroadIndex, int crossroadID, int localLightIndex)
        {
            var lightEntity = EntityManager.CreateEntity(
                typeof(LightHandlerInitTag),
                typeof(LightHandlerStateUpdateTag),
                typeof(LightHandlerComponent),
                typeof(LightHandlerID),
                typeof(LightHandlerStateElement));

            var buffer = commandBuffer.AddBuffer<LightHandlerStateElement>(lightEntity);

            buffer.EnsureCapacity(lightStates.Count);

            float totalDuration = 0;

            for (int i = 0; i < lightStates.Count; i++)
            {
                var lightState = lightStates[i];

                buffer.Add(new LightHandlerStateElement()
                {
                    LightState = lightState.LightState,
                    Duration = lightState.Duration,
                });

                totalDuration += lightState.Duration;
            }

            commandBuffer.SetComponent(lightEntity, new LightHandlerComponent()
            {
                CycleDuration = totalDuration,
                CrossRoadIndex = crossroadIndex
            });

            var handlerId = crossroadID + localLightIndex;

            commandBuffer.SetComponent(lightEntity, new LightHandlerID()
            {
                Value = handlerId
            });

            return lightEntity;
        }

        protected void SaveGraph()
        {
            if (!graph.IsCreated()) return;

            EntityManager.SetComponentData(graphEntity, graph);
        }
#endif
    }
}