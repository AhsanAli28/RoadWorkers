using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

#if CINEMACHINE
#if !CINEMACHINE_V3
using Cinemachine;
#else
using Unity.Cinemachine;
#endif
#endif

namespace Spirit604.DotsCity.TestScene
{
    public class VehicleCustomCameraGroupTracker : MonoBehaviour
    {
        [SerializeField]
        private GameObject startTarget;

        [SerializeField]
        private GameObject endTarget;

#if CINEMACHINE
        [SerializeField]
        private CinemachineTargetGroup cinemachineTargetGroup;
#endif

        private EntityManager entityManager;
        private EntityQuery startEntityQuery;
        private EntityQuery endEntityQuery;

        private Entity startLineEntity;
        private Entity endLineEntity;

        private void Update()
        {
            if (entityManager.Exists(startLineEntity) && entityManager.HasComponent<LocalTransform>(startLineEntity))
            {
                startTarget.transform.position = entityManager.GetComponentData<LocalTransform>(startLineEntity).Position;
                endTarget.transform.position = entityManager.GetComponentData<LocalTransform>(endLineEntity).Position;
            }
            else
            {
                if (startEntityQuery.CalculateEntityCount() == 1)
                {
                    startLineEntity = startEntityQuery.GetSingletonEntity();
                    endLineEntity = endEntityQuery.GetSingletonEntity();
                }
            }
        }

        public void Initialize()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            startEntityQuery = entityManager.CreateEntityQuery(typeof(FirstRowVehicleTag));
            endEntityQuery = entityManager.CreateEntityQuery(typeof(LastRowVehicleTag));
        }

        public void SwitchEnabledState(bool isEnabled)
        {
            this.enabled = isEnabled;
        }
    }
}
