using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.Gameplay.Road;
using System.Text;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class DefaultPedestrianNodeDebugger : EntityDebuggerBase
    {
        private StringBuilder sb = new StringBuilder();

        public DefaultPedestrianNodeDebugger(EntityManager entityManager) : base(entityManager)
        {
        }

        public override Color GetBoundsColor(Entity entity)
        {
            return Color.blue;
        }

        public override StringBuilder GetDescriptionText(Entity entity)
        {
            var nodeSettingsComponent = EntityManager.GetComponentData<NodeSettingsComponent>(entity);

            sb.Clear();

            if (nodeSettingsComponent.NodeType == PedestrianNodeType.Sit)
            {
                var nodeCapacityComponent = EntityManager.GetComponentData<NodeCapacityComponent>(entity);

                string text = "Capacity:" + nodeCapacityComponent.CurrentCount.ToString() + "/" + nodeCapacityComponent.MaxAvailableCount.ToString();
                sb.Append(text);
                sb.Append("\n");
            }

            sb.Append("Entity Index: ").Append(entity.Index).Append("\n");

            return sb;
        }
    }
}