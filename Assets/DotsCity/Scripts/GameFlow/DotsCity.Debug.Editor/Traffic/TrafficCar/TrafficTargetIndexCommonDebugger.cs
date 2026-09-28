#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Traffic;
using System.Text;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficTargetIndexCommonDebugger : ITrafficDebugger
    {
        private StringBuilder sb = new StringBuilder();
        private EntityManager entityManager;

        public TrafficTargetIndexCommonDebugger(EntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public string Tick(Entity entity)
        {
            sb.Clear();

            if (entityManager.HasComponent(entity, typeof(TrafficDestinationComponent)))
            {
                var targetIndex = entityManager.GetComponentData<TrafficDestinationComponent>(entity).DestinationNode.Index;

                sb.Append("Target: ").Append(targetIndex);

                var targetPosition = entityManager.GetComponentData<TrafficPathComponent>(entity).DestinationWayPoint;

                Handles.DrawWireDisc(targetPosition, Vector3.up, 1f);
            }

            return sb.ToString();
        }
    }
}
#endif