using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.TestScene
{
    public class VehicleControl : MonoBehaviour
    {
        [SerializeField]
        private Transform target;

        private EntityManager entityManager;
        private EntityQuery allEntitiesQuery;
        private EntityQuery targetVehicleQuery;
        private Entity trackedEntity;

        private void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            allEntitiesQuery = entityManager.CreateEntityQuery(typeof(VehicleInputReader));
            targetVehicleQuery = entityManager.CreateEntityQuery(typeof(VehicleInputReader), typeof(PlayerTag));
        }

        private void Update()
        {
            if (entityManager.Exists(trackedEntity) && entityManager.HasComponent<LocalTransform>(trackedEntity))
            {
            }
            else
            {
                if (targetVehicleQuery.CalculateEntityCount() == 1)
                {
                    trackedEntity = targetVehicleQuery.GetSingletonEntity();

                    entityManager.AddComponentObject(trackedEntity, target);
                    entityManager.AddComponent<InterpolateTransformData>(trackedEntity);
                }
            }
        }
    }
}