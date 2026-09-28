using Spirit604.DotsCity.Simulation.Traffic;
using System.Text;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficNextPathCalculationDistanceDebug : TrafficDebugBase
    {
        private bool showText;

        public TrafficNextPathCalculationDistanceDebug(EntityManager entityManager) : base(entityManager, false)
        {
        }

        public override Color GetBoundsColor(Entity entity)
        {
            var position = EntityManager.GetComponentData<LocalToWorld>(entity).Position;
            var targetPosition = EntityManager.GetComponentData<TrafficDestinationComponent>(entity).Destination;

            float distanceToTarget = math.distance(position, targetPosition);

            var trafficObstacleConfig = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficObstacleConfigReference>()).GetSingleton<TrafficObstacleConfigReference>();
            float minDistanceToCheckNextConnectedPath = trafficObstacleConfig.Config.Value.MinDistanceToCheckNextPath;

            showText = distanceToTarget < minDistanceToCheckNextConnectedPath;

            return !showText ? Color.green : Color.red;
        }

        public override StringBuilder GetDescriptionText(Entity entity)
        {
            return showText ? new StringBuilder("Checking Next path") : new StringBuilder("Too far to end");
        }
    }
}