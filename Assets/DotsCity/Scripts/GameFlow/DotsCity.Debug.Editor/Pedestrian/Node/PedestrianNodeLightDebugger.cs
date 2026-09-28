using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Gameplay.Road.Debug;
using System.Text;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class PedestrianNodeLightDebugger : EntityDebuggerBase
    {
        private StringBuilder sb = new StringBuilder();

        public PedestrianNodeLightDebugger(EntityManager entityManager) : base(entityManager)
        {
        }

        public override Color GetBoundsColor(Entity entity)
        {
            var color = Color.blue;
            var nodeSettingsComponent = EntityManager.GetComponentData<NodeLightSettingsComponent>(entity);
            var lightEntity = nodeSettingsComponent.LightEntity;

            if (EntityManager.Exists(lightEntity) && EntityManager.HasComponent<LightHandlerComponent>(lightEntity))
            {
                var lightComponent = EntityManager.GetComponentData<LightHandlerComponent>(lightEntity);
                color = TrafficLightSceneColor.StateToColor(lightComponent.State);
            }

            return color;
        }

        public override StringBuilder GetDescriptionText(Entity entity)
        {
            sb.Clear();
            sb.Append(entity.Index);

            return sb;
        }
    }
}