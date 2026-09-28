using Spirit604.Attributes;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using Spirit604.Extensions;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficPublicRouteDebugger : MonoBehaviourBase
    {
        [SerializeField] private bool enableDebug;

        [ShowIf(nameof(enableDebug))]
        [SerializeField] private Color routeColor = Color.magenta;

        [ShowIf(nameof(enableDebug))]
        [SerializeField] private Color availableArrowColor = Color.yellow;

        [ShowIf(nameof(enableDebug))]
        [SerializeField] private Color unavailableArrowColor = Color.red;

#if UNITY_EDITOR

        private EntityManager entityManager;
        private EntityQuery routeQuery;

        private void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            routeQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficPublicRouteCapacityComponent>());
        }

        private void OnDrawGizmos()
        {
            if (!enableDebug || !Application.isPlaying)
            {
                return;
            }

            var routes = routeQuery.ToEntityArray(Unity.Collections.Allocator.TempJob);

            Gizmos.color = routeColor;

            for (int i = 0; i < routes.Length; i++)
            {
                var buffer = entityManager.GetBuffer<FixedRouteNodeElement>(routes[i]);

                for (int j = 0; j < buffer.Length; j++)
                {
                    var point1 = buffer[j].Position;
                    var point2 = buffer[(j + 1) % buffer.Length].Position;

                    Gizmos.DrawLine(point1, point2);

                    var arrowColor = buffer[j].IsAvailable ? availableArrowColor : unavailableArrowColor;

                    DebugLine.DrawArrow(point1, buffer[j].Rotation, arrowColor);
                }
            }

            routes.Dispose();
        }

#endif
    }
}
