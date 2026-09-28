using Spirit604.Attributes;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficExplodeDebugger : MonoBehaviourBase
    {
        [SerializeField] private bool enableDebug;

        [ShowIf(nameof(enableDebug))]
        [SerializeField] private int trafficIndex;

#if UNITY_EDITOR

        private TrafficDebuggerSystem trafficDebuggerSystem;
        private EntityManager entityManager;

        private void Awake()
        {
            trafficDebuggerSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TrafficDebuggerSystem>();
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        [Button]
        public void Explode()
        {
            if (!enableDebug || !Application.isPlaying)
            {
                return;
            }

            if (trafficDebuggerSystem.TryToGetEntity(trafficIndex, out var trafficEntity))
            {
                if (!entityManager.HasComponent(trafficEntity, typeof(CarExplodeRequestedTag)))
                {
                    entityManager.AddComponent(trafficEntity, typeof(CarExplodeRequestedTag));
                }
            }
        }
#endif
    }
}
