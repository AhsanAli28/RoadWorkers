#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Traffic;
using System.Text;
using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficSpeedLimitDebugger : ITrafficDebugger
    {
        private StringBuilder sb = new StringBuilder();
        private EntityManager entityManager;

        public TrafficSpeedLimitDebugger(EntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public string Tick(Entity entity)
        {
            if (entityManager.HasComponent(entity, typeof(TrafficDestinationComponent)))
            {
                var speedComponent = entityManager.GetComponentData<SpeedComponent>(entity);

                sb.Clear();
                sb.Append("LaneLimit: ").Append(speedComponent.LaneLimit * ProjectConstants.KmhToMs_RATE).Append(" \n");
                sb.Append("CurrentLimit: ").Append(speedComponent.CurrentLimit * ProjectConstants.KmhToMs_RATE).Append(" \n");

                float speed = (float)System.Math.Round(speedComponent.SpeedKmh, 2);

                sb.Append("CurrentSpeed: ").Append(speed).Append(" \n");
                return sb.ToString();
            }

            return null;
        }
    }
}
#endif