using Spirit604.DotsCity.Simulation.Pedestrian;
using System.Text;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class PedestrianNodeOtherSettingsDebugger : EntityDebuggerBase
    {
        private StringBuilder sb = new StringBuilder();

        public PedestrianNodeOtherSettingsDebugger(EntityManager entityManager) : base(entityManager)
        {
        }

        public override Color GetBoundsColor(Entity entity)
        {
            return Color.blue;
        }

        public override StringBuilder GetDescriptionText(Entity entity)
        {
            var nodeSettingsComponent = EntityManager.GetComponentData<NodeSettingsComponent>(entity);
            var nodeCapacityComponent = EntityManager.GetComponentData<NodeCapacityComponent>(entity);

            sb.Clear();
            sb.Append("ChanceToSpawn: ").Append(nodeSettingsComponent.ChanceToSpawn).Append("\n");
            sb.Append("Weight: ").Append(nodeSettingsComponent.Weight).Append("\n");
            sb.Append("CustomAchieveDistance: ").Append(nodeSettingsComponent.CustomAchieveDistance).Append("\n");
            sb.Append("CanSpawnInVision: ").Append(nodeSettingsComponent.CanSpawnInVision == 1).Append("\n");
            sb.Append("Capacity: ").Append(nodeCapacityComponent.CurrentCount).Append("\n");

            return sb;
        }
    }
}

