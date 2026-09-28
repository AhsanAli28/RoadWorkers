using Spirit604.DotsCity.Simulation.Traffic;
using System.Text;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficChangeLaneDebug : TrafficObstacleDistanceDebug
    {
        public TrafficChangeLaneDebug(EntityManager entityManager) : base(entityManager)
        {
        }

        public override void Tick(Entity entity)
        {
            base.Tick(entity);

            if (EntityManager.HasComponent(entity, typeof(TrafficChangeLaneComponent)))
            {
                var trafficChangeLaneComponent = EntityManager.GetComponentData<TrafficChangeLaneComponent>(entity);
                var trafficStateComponent = EntityManager.GetComponentData<TrafficStateComponent>(entity);
                var waitForChangeLane = trafficStateComponent.HasIdleState(TrafficIdleState.WaitForChangeLane);

                if (EntityManager.HasComponent<TrafficChangingLaneEventTag>(entity) && (EntityManager.IsComponentEnabled<TrafficChangingLaneEventTag>(entity) || waitForChangeLane))
                {
                    var destinationComponent = EntityManager.GetComponentData<TrafficDestinationComponent>(entity);

                    Handles.color = !waitForChangeLane ? Color.green : Color.red;
                    Handles.DrawWireDisc(destinationComponent.Destination, Vector3.up, 1f);
                }
            }
        }

        public override StringBuilder GetDescriptionText(Entity entity)
        {
            var text = base.GetDescriptionText(entity);

            if (text == null)
            {
                text = new StringBuilder();
            }

            var trafficChangeLaneComponent = EntityManager.GetComponentData<TrafficChangeLaneComponent>(entity);
            var trafficChangeLaneDebugInfoComponent = EntityManager.GetComponentData<TrafficChangeLaneDebugInfoComponent>(entity);

            //text.Append("RemainDistanceToEndOfPath: ");
            //text.Append(trafficChangeLaneDebugInfoComponent.RemainDistanceToEndOfPath).Append("\n");
            //text.Append("CurrentLaneCarCount: ");
            //text.Append(trafficChangeLaneDebugInfoComponent.CurrentLaneCarCount).Append("\n");
            //text.Append("NeighborLaneCarCount: ");
            //text.Append(trafficChangeLaneDebugInfoComponent.NeighborLaneCarCount).Append("\n");
            //text.Append("ShouldChangeLane: ");
            //text.Append(trafficChangeLaneDebugInfoComponent.ShouldChangeLane).Append("\n");
            //text.Append("NeighborCarDistance: ");
            //text.Append(trafficChangeLaneComponent.DistanceToOtherCarsInNeighborLane).Append("\n");
            //text.Append("TargetCarDirection: ");
            //text.Append(trafficChangeLaneComponent.TargetCarDirection).Append("\n");

            return text;
        }
    }
}