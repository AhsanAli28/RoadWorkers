using Spirit604.DotsCity.Simulation.Traffic;
using System.Text;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficObstacleDistanceDebug : TrafficDebugBase
    {
        public TrafficObstacleDistanceDebug(EntityManager entityManager) : base(entityManager, false)
        {
        }

        public override Color GetBoundsColor(Entity entity)
        {
            var carObstacleComponent = EntityManager.GetComponentData<TrafficObstacleComponent>(entity);
            Color color = carObstacleComponent.HasObstacle ? Color.green : Color.red;

            return color;
        }

        public override StringBuilder GetDescriptionText(Entity entity)
        {
            var carObstacleComponent = EntityManager.GetComponentData<TrafficObstacleComponent>(entity);

            StringBuilder sb = new StringBuilder();

            sb.Append("ApproachSpeed: ");
            sb.Append(carObstacleComponent.ApproachSpeed).Append("\n");

            if (carObstacleComponent.HasObstacle)
            {
                sb.Append("IntersectInfo: ");
                sb.Append($"{carObstacleComponent.ObstacleEntity.Index} ");
                sb.Append($"{carObstacleComponent.ObstacleType}");
                sb.Append("\n");
            }

            return sb;
        }
    }
}