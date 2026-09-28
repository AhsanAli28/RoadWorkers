#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Traffic;
using System.Text;
using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficIndexDebugger : ITrafficDebugger
    {
        private StringBuilder sb = new StringBuilder();
        private EntityManager entityManager;

        public TrafficIndexDebugger(EntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public string Tick(Entity entity)
        {
            if (entityManager.HasComponent(entity, typeof(TrafficDestinationComponent)))
            {
                var destinationComponent = entityManager.GetComponentData<TrafficDestinationComponent>(entity);
                var trafficLightDataComponent = entityManager.GetComponentData<TrafficLightDataComponent>(entity);

                sb.Clear();
                sb.Append($"PR/C: {destinationComponent.PreviousNode.Index} {destinationComponent.CurrentNode.Index}\n");
                sb.Append($"T/NT: {destinationComponent.DestinationNode.Index} {destinationComponent.NextDestinationNode.Index}\n");
                sb.Append($"L: {trafficLightDataComponent.CurrentLightEntity}\n");

                return sb.ToString();
            }

            return null;
        }
    }
}
#endif