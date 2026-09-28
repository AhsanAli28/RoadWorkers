#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Traffic;
using System.Text;
using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficStateDebugger : ITrafficDebugger
    {
        private StringBuilder sb = new StringBuilder();
        private EntityManager entityManager;

        public TrafficStateDebugger(EntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public string Tick(Entity entity)
        {
            if (entityManager.HasComponent(entity, typeof(TrafficStateComponent)))
            {
                var trafficStateComponent = entityManager.GetComponentData<TrafficStateComponent>(entity);

                sb.Clear();
                sb.Append("S: ").Append(trafficStateComponent.TrafficState).Append(" \n");
                sb.Append("L: ").Append(trafficStateComponent.TrafficLightCarState).Append(" \n");

                return sb.ToString();
            }

            return null;
        }
    }
}
#endif