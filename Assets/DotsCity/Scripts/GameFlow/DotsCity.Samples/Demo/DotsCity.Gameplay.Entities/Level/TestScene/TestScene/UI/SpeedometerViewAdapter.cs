using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.TestScene.UI
{
    public class SpeedometerViewAdapter : MonoBehaviour
    {
        [SerializeField]
        private SpeedometerView speedometerView;

        private EntityManager entityManager;
        private EntityQuery query;

        private Entity trackerEntity;
        private int previousSpeed = -1;

        private void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<HasDriverTag>());
        }

        private void Update()
        {
            if (entityManager.Exists(trackerEntity) && entityManager.HasComponent<SpeedComponent>(trackerEntity))
            {
                var currentSpeed = entityManager.GetComponentData<SpeedComponent>(trackerEntity).SpeedKmh;
                var currentSpeedRounded = Mathf.FloorToInt(currentSpeed);

                if (previousSpeed != currentSpeedRounded)
                {
                    previousSpeed = currentSpeedRounded;
                    speedometerView.UpdateSpeed(previousSpeed);
                }
            }
            else
            {
                if (query.CalculateEntityCount() == 1)
                {
                    trackerEntity = query.GetSingletonEntity();
                }
            }
        }
    }
}
