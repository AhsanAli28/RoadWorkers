#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Traffic;
using System;
using System.Text;
using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficApproachSpeedCommonDebugger : ITrafficDebugger
    {
        private StringBuilder sb = new StringBuilder();
        private EntityManager entityManager;

        public TrafficApproachSpeedCommonDebugger(EntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public string Tick(Entity entity)
        {
            sb.Clear();

            var trafficObstacleComponent = entityManager.GetComponentData<TrafficObstacleComponent>(entity);

            sb.Append("Approach Type: ");
            sb.Append(trafficObstacleComponent.ApproachType);

            sb.Append(Environment.NewLine);

            sb.Append("Approach Speed: ");

            var approachSpeed = entityManager.GetComponentData<TrafficApproachDataComponent>(entity).ApproachSpeed;

            sb.Append(approachSpeed);

            return sb.ToString();
        }
    }
}
#endif