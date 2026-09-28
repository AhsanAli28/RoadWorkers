#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Extensions;
using System;
using System.Text;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficNodeCapacityDebugger : ITrafficNodeDebugger
    {
        private EntityManager entityManager;

        public TrafficNodeCapacityDebugger(EntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public void Tick(Entity entity)
        {
            var trafficNodeCapacity = entityManager.GetComponentData<TrafficNodeCapacityComponent>(entity);
            var pos = entityManager.GetComponentData<LocalToWorld>(entity).Position;
            var textPosition = pos + new Unity.Mathematics.float3(0, 0.5f, 0);

            StringBuilder sb = new StringBuilder();

            sb.Append($"Capacity: {trafficNodeCapacity.Capacity}");
            sb.Append(Environment.NewLine);
            sb.Append($"Related: {trafficNodeCapacity.CarEntity}");
            sb.Append(Environment.NewLine);

            var text = sb.ToString();

            EditorExtension.DrawWorldString(text, textPosition);

            var color = trafficNodeCapacity.HasSlots() ? Color.green : Color.red;

            Gizmos.color = color;
            Gizmos.DrawWireSphere(pos, 1f);
        }
    }
}
#endif