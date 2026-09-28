using Spirit604.DotsCity.Simulation.Pedestrian;
using System.Text;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class PedestrianNodeConnectionDebugger : EntityDebuggerBase
    {
        private StringBuilder sb = new StringBuilder();

        public PedestrianNodeConnectionDebugger(EntityManager entityManager) : base(entityManager)
        {
        }

        public override Color GetBoundsColor(Entity entity)
        {
            return Color.blue;
        }

        public override StringBuilder GetDescriptionText(Entity entity)
        {
            var buffer = EntityManager.GetBuffer<NodeConnectionDataElement>(entity, true);

            sb.Clear();

            sb.Append($"Entity {entity.Index}").Append("\n");

            for (int i = 0; i < buffer.Length; i++)
            {
                sb.Append($"{i + 1}) {buffer[i].ConnectedEntity.Index}").Append("\n");
            }

            return sb;
        }
    }
}

