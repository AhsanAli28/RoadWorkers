using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Chaser
{
    public static class ChaserCarSpawnHelper
    {
        private static TrafficSpawnerSystem trafficSpawnerSystem;
        private static Camera mainCamera;

        static ChaserCarSpawnHelper()
        {
            trafficSpawnerSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TrafficSpawnerSystem>();
            mainCamera = Camera.main;
        }

        public static bool GetSpawnPosition(Vector3 targetPosition, out Vector3 spawnPosition, out Quaternion spawnRotation, float maxDistance = -1)
        {
            spawnPosition = Vector3.zero;
            spawnRotation = Quaternion.identity;

            var trafficNodeComponents = trafficSpawnerSystem.PermittedTrafficNodeGroup.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);

            if (trafficNodeComponents.Length > 0)
            {
                var localSpawnIndex = UnityEngine.Random.Range(0, trafficNodeComponents.Length);

                spawnPosition = trafficNodeComponents[localSpawnIndex].Position;

                Vector3 directionToTarget = (targetPosition.Flat() - (Vector3)spawnPosition.Flat()).normalized;

                spawnRotation = Quaternion.LookRotation(directionToTarget);

                trafficNodeComponents.Dispose();
                return true;
            }

            trafficNodeComponents.Dispose();
            return false;
        }

        public static RigidTransform GetSpawnPoint(Vector3 target)
        {
            Vector3 spawnPosition;
            Quaternion spawnRotation;
            var found = GetSpawnPosition(target, out spawnPosition, out spawnRotation, 32f);

            if (found)
            {
                RigidTransform transform = new RigidTransform(spawnRotation, spawnPosition);

                return transform;
            }

            return new RigidTransform();
        }
    }
}