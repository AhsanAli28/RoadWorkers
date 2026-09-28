#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficChangeLaneCommonDebugger : ITrafficDebugger
    {
        private EntityManager entityManager;

        public TrafficChangeLaneCommonDebugger(EntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public string Tick(Entity entity)
        {
            if (entityManager.HasComponent(entity, typeof(TrafficChangeLaneComponent)))
            {
                var trafficChangeLaneComponent = entityManager.GetComponentData<TrafficChangeLaneComponent>(entity);

                if (entityManager.HasComponent<TrafficChangingLaneEventTag>(entity) && entityManager.IsComponentEnabled<TrafficChangingLaneEventTag>(entity))
                {
                    var destinationComponent = entityManager.GetComponentData<TrafficDestinationComponent>(entity);

                    Handles.color = Color.green;
                    Handles.DrawWireDisc(destinationComponent.Destination, Vector3.up, 1f);
                }
            }

            return null;
        }
    }
}
#endif