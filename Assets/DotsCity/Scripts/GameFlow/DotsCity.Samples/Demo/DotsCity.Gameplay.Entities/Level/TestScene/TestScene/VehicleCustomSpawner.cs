using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.TestScene
{
    public class VehicleCustomSpawner : MonoBehaviourBase
    {
        [Expandable]
        [SerializeField]
        private VehicleCustomSceneSettings vehicleCustomSceneSettings;

        [SerializeField]
        private List<Transform> spawnpoints = new List<Transform>();

        private Entity spawnedEntity;
        private EntityManager entityManager;
        private EntityQuery vehicleQuery;
        private EntityQuery prefabQuery;
        private int currentIndex;
        private int vehicleModelIndex = 0;
        private bool spawned;

        public event Action<bool> OnSpawned = delegate { };

        private void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            vehicleQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<VehicleInputReader>());
            CreateQuery();
        }

        private void Update()
        {
            if (!spawned && prefabQuery.CalculateEntityCount() > 0)
            {
                spawned = true;
                InitialSpawn();
            }
        }

        public void SpawnNext(int direction = 1)
        {
            currentIndex += direction;

            if (currentIndex < 0)
            {
                currentIndex = spawnpoints.Count - 1;
            }

            if (currentIndex >= spawnpoints.Count)
            {
                currentIndex = 0;
            }

            if (!EntityExist(spawnedEntity))
            {
                Spawn(currentIndex);
            }
            else
            {
                SetPosition(currentIndex);
            }
        }

        public void ResetPosition()
        {
            SpawnNext(0);
        }

        public void SpawnNextVehicle()
        {
            vehicleModelIndex = ++vehicleModelIndex % prefabQuery.CalculateEntityCount();
            Spawn(currentIndex);
        }

        private void InitialSpawn()
        {
            if (vehicleQuery.CalculateEntityCount() == 0)
            {
                Spawn(0);
                OnSpawned(false);
            }
            else
            {
                var entities = vehicleQuery.ToEntityArray(Allocator.TempJob);

                spawnedEntity = entities[0];

                var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

                VehicleComponentCleanerBakingSystem.Clean(spawnedEntity, ref entityManager, ref commandBuffer, false);
                PrefabContainerBakingSystem.AddInputComponents(ref commandBuffer, spawnedEntity);

                PoolEntityUtils.AddPoolComponents(ref commandBuffer, spawnedEntity, EntityWorldType.PureEntity);

                commandBuffer.Playback(entityManager);
                commandBuffer.Dispose();

                entities.Dispose();
                OnSpawned(true);
            }
        }

        private void Spawn(int index)
        {
            DestroyEntity(spawnedEntity);

            var prefabContainers = prefabQuery.ToComponentDataArray<PrefabContainer>(Allocator.TempJob);

            var prefabEntity = prefabContainers[vehicleModelIndex].Entity;

            spawnedEntity = entityManager.Instantiate(prefabEntity);
            SetPosition(index);

            prefabContainers.Dispose();
        }

        private void SetPosition(int index)
        {
            var pos = spawnpoints[index].position;
            var rot = spawnpoints[index].rotation;

            entityManager.SetComponentData<LocalTransform>(spawnedEntity, LocalTransform.FromPositionRotation(pos, rot));
        }

        private void DestroyEntity(Entity entity)
        {
            if (EntityExist(entity))
            {
                PoolEntityUtils.DestroyEntity(ref entityManager, entity);
            }
        }

        private bool EntityExist(Entity entity) => entityManager.HasComponent<LocalTransform>(entity);

        private void CreateQuery()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            prefabQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PrefabContainer>(), ComponentType.ReadOnly<PrefabContainerSort>());

            prefabQuery.SetSharedComponentFilter(new PrefabContainerSort()
            {
                OwnerType = vehicleCustomSceneSettings.SpawnType
            });
        }
    }
}