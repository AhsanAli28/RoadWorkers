#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficNodeStreamingDebugger : ITrafficNodeDebugger
    {
        private EntityManager entityManager;

        public TrafficNodeStreamingDebugger(EntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public void Tick(Entity entity)
        {
            var nodePosition = entityManager.GetComponentData<LocalToWorld>(entity).Position;
            var isAvailable = entityManager.HasComponent<TrafficNodeDynamicConnection>(entity);

            if (isAvailable)
            {
                var trafficNodeDynamicConnection = entityManager.GetComponentData<TrafficNodeDynamicConnection>(entity);
                var prevColor = Gizmos.color;

                var color = Color.clear;

                if (trafficNodeDynamicConnection.ConnectionType.HasFlag(ConnectionType.StreamingConnection) && trafficNodeDynamicConnection.ConnectionType.HasFlag(ConnectionType.ExternalStreamingConnection))
                {
                    color = Color.green;
                }
                else if (trafficNodeDynamicConnection.ConnectionType.HasFlag(ConnectionType.StreamingConnection))
                {
                    color = Color.magenta;
                }
                else
                {
                    color = Color.cyan;
                }

                Gizmos.color = color;

                Gizmos.DrawWireSphere(nodePosition, 1f);
                Gizmos.color = prevColor;
            }
        }
    }
}
#endif