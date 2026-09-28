using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Common
{
    /// <summary>
    /// Service for user selection of scene traffic or pedestrian entities by world position & radius.
    /// </summary>
    public class EntitySelectionService : SingletonMonoBehaviour<EntitySelectionService>
    {
        [Flags]
        public enum EntityType
        {
            Pedestrian = 1 << 0,
            Traffic = 1 << 1,
            Any = Pedestrian | Traffic,
        }

        private SystemHandle pedestrianHashMapHandle;
        private SystemHandle trafficHashMapHandle;
        private bool init;

        protected EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        protected override void Awake()
        {
            base.Awake();
            trafficHashMapHandle = World.DefaultGameObjectInjectionWorld.Unmanaged.GetExistingUnmanagedSystem<CarHashMapSystem>();
        }

        /// <summary>
        /// Select the nearest entity to the position, Entity.Null if no nearest entity is available.
        /// </summary>
        public Entity SelectEntity(Vector3 position, EntityType entityType, float maxSelectionRadius = 0)
        {
            var keys = HashMapHelper.GetHashMapPosition9Cells(position.Flat());

            if (maxSelectionRadius > 0)
            {
                maxSelectionRadius *= maxSelectionRadius;
            }

            Entity closestEntity = default;

            var maxDistance = float.MaxValue;

            if (entityType.HasFlag(EntityType.Pedestrian))
            {
                closestEntity = SelectPedestrianEntityInternal(position, maxSelectionRadius, ref keys, ref maxDistance);
            }

            if (entityType.HasFlag(EntityType.Traffic))
            {
                var newClosestEntity = SelectTrafficEntityInternal(position, maxSelectionRadius, ref keys, ref maxDistance);

                if (newClosestEntity != Entity.Null)
                {
                    closestEntity = newClosestEntity;
                }
            }

            keys.Dispose();

            return closestEntity;
        }

        /// <summary>
        /// Select entities by radius, if null then no entities selected.
        /// </summary>
        public List<Entity> SelectEntities(Vector3 position, EntityType entityType, float selectionDistance)
        {
            List<Entity> list = null;

            SelectEntitiesNonAlloc(position, entityType, selectionDistance, ref list);

            return list;
        }

        /// <summary>
        /// Select entities by radius, if null then no entities selected. Non-allocation version, the list should be provided by the user.
        /// </summary>
        public void SelectEntitiesNonAlloc(Vector3 position, EntityType entityType, float selectionDistance, ref List<Entity> entities)
        {
            var keys = HashMapHelper.GetHashMapPosition9Cells(position.Flat());

            selectionDistance *= selectionDistance;

            if (entities != null)
            {
                entities.Clear();
            }

            if (entityType.HasFlag(EntityType.Pedestrian))
            {
                SelectPedestrianEntitiesInternal(position, selectionDistance, ref keys, ref entities);
            }

            if (entityType.HasFlag(EntityType.Traffic))
            {
                SelectTrafficEntitiesInternal(position, selectionDistance, ref keys, ref entities);
            }

            keys.Dispose();
        }

        private Entity SelectPedestrianEntityInternal(Vector3 position, float maxSelectionRadiusSQ, ref NativeList<int> keys, ref float maxDistance)
        {
            Entity closestEntity = Entity.Null;

            NpcHashMapSystem.Singleton hashMapRef = GetPedestrianHashMap();

            for (int i = 0; i < keys.Length; i++)
            {
                if (hashMapRef.NpcMultiHashMap.TryGetFirstValue(keys[i], out var hashData, out var iter))
                {
                    do
                    {
                        var distance = math.distancesq(hashData.Position, position);

                        if (distance < maxDistance && (maxSelectionRadiusSQ == 0 || distance < maxSelectionRadiusSQ))
                        {
                            maxDistance = distance;
                            closestEntity = hashData.Entity;
                        }

                    } while (hashMapRef.NpcMultiHashMap.TryGetNextValue(out hashData, ref iter));
                }
            }

            return closestEntity;
        }

        private Entity SelectTrafficEntityInternal(Vector3 position, float maxSelectionRadiusSQ, ref NativeList<int> keys, ref float maxDistance)
        {
            Entity closestEntity = Entity.Null;

            var hashMapRef = GetTrafficHashMap();

            for (int i = 0; i < keys.Length; i++)
            {
                if (hashMapRef.CarHashMap.TryGetFirstValue(keys[i], out var hashData, out var iter))
                {
                    do
                    {
                        var distance = math.distancesq(hashData.Position, position);

                        if (distance < maxDistance && (maxSelectionRadiusSQ == 0 || distance < maxSelectionRadiusSQ))
                        {
                            maxDistance = distance;
                            closestEntity = hashData.Entity;
                        }

                    } while (hashMapRef.CarHashMap.TryGetNextValue(out hashData, ref iter));
                }
            }

            return closestEntity;
        }

        private void SelectPedestrianEntitiesInternal(Vector3 position, float selectionDistanceSQ, ref NativeList<int> keys, ref List<Entity> entities)
        {
            var hashMapRef = GetPedestrianHashMap();

            for (int i = 0; i < keys.Length; i++)
            {
                if (hashMapRef.NpcMultiHashMap.TryGetFirstValue(keys[i], out var hashData, out var iter))
                {
                    do
                    {
                        var distance = math.distancesq(hashData.Position, position);

                        if (distance < selectionDistanceSQ)
                        {
                            if (entities == null)
                            {
                                entities = new List<Entity>();
                            }

                            entities.Add(hashData.Entity);
                        }
                    } while (hashMapRef.NpcMultiHashMap.TryGetNextValue(out hashData, ref iter));
                }
            }
        }

        private void SelectTrafficEntitiesInternal(Vector3 position, float selectionDistanceSQ, ref NativeList<int> keys, ref List<Entity> entities)
        {
            var hashMapRef = GetTrafficHashMap();

            for (int i = 0; i < keys.Length; i++)
            {
                if (hashMapRef.CarHashMap.TryGetFirstValue(keys[i], out var hashData, out var iter))
                {
                    do
                    {
                        var distance = math.distancesq(hashData.Position, position);

                        if (distance < selectionDistanceSQ)
                        {
                            if (entities == null)
                            {
                                entities = new List<Entity>();
                            }

                            entities.Add(hashData.Entity);
                        }

                    } while (hashMapRef.CarHashMap.TryGetNextValue(out hashData, ref iter));
                }
            }
        }

        private NpcHashMapSystem.Singleton GetPedestrianHashMap()
        {
            if (!init)
            {
                init = true;
                pedestrianHashMapHandle = World.DefaultGameObjectInjectionWorld.Unmanaged.GetExistingUnmanagedSystem<NpcHashMapSystem>();
            }

            return EntityManager.GetComponentData<NpcHashMapSystem.Singleton>(pedestrianHashMapHandle);
        }

        private CarHashMapSystem.Singleton GetTrafficHashMap()
        {
            return EntityManager.GetComponentData<CarHashMapSystem.Singleton>(trafficHashMapHandle);
        }
    }
}
