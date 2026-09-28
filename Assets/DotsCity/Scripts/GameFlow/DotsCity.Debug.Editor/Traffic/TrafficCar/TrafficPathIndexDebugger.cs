#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Traffic;
using System.Text;
using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficPathIndexDebugger : ITrafficDebugger
    {
        private StringBuilder sb = new StringBuilder();
        private EntityManager entityManager;

        public TrafficPathIndexDebugger(EntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public string Tick(Entity entity)
        {

            if (entityManager.HasComponent(entity, typeof(TrafficPathComponent)))
            {
                var destinationComponent = entityManager.GetComponentData<TrafficPathComponent>(entity);

                sb.Clear();
                sb.Append($"PathIndex: {destinationComponent.CurrentGlobalPathIndex}\n");
                sb.Append($"LocalPathNodeIndex: {destinationComponent.LocalPathNodeIndex}\n");

                return sb.ToString();
            }

            return null;
        }
    }
}
#endif
