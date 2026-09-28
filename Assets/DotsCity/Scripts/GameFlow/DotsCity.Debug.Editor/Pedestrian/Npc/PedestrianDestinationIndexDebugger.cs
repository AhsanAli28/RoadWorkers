#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using System.Text;
using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    public class PedestrianDestinationIndexDebugger : EntityDebuggerBase
    {
        private StringBuilder sb = new StringBuilder();

        public PedestrianDestinationIndexDebugger(EntityManager entityManager) : base(entityManager)
        {
        }

        public override bool ShouldDraw(Entity entity)
        {
            return false;
        }

        protected override bool ShouldTick(Entity entity)
        {
            return EntityManager.HasComponent(entity, typeof(StateComponent));
        }

        public override StringBuilder GetDescriptionText(Entity entity)
        {
            var destinationComponent = EntityManager.GetComponentData<DestinationComponent>(entity);

            sb.Clear();
            sb.Append("Previous index: ");
            sb.Append(destinationComponent.PreviuosDestinationNode.Index).Append("\n");
            sb.Append("Destination Index: ");
            sb.Append(destinationComponent.DestinationNode.Index).Append("\n");

            return sb;
        }
    }
}
#endif