#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Car;
using System.Text;
using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficInputDebugger : ITrafficDebugger
    {
        private StringBuilder sb = new StringBuilder();
        private EntityManager entityManager;

        public TrafficInputDebugger(EntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public string Tick(Entity entity)
        {
            if (entityManager.HasComponent(entity, typeof(VehicleInputReader)))
            {
                var input = entityManager.GetComponentData<VehicleInputReader>(entity);

                sb.Clear();
                sb.Append("Throttle: ").Append(input.Throttle).Append(" \n");
                sb.Append("Handbrake: ").Append(input.HandbrakeInput).Append(" \n");
                sb.Append("Steering: ").Append(input.SteeringInput).Append(" \n");

                return sb.ToString();
            }

            return null;
        }
    }
}
#endif