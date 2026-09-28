using Spirit604.DotsCity.Simulation.Pedestrian;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class PedestrianNodeTargetDeviationDebugger : EntityDebuggerBase
    {
        public PedestrianNodeTargetDeviationDebugger(EntityManager entityManager) : base(entityManager)
        {
        }

        public override void Tick(Entity entity)
        {
            base.Tick(entity);

            var nodeSettingsComponent = EntityManager.GetComponentData<NodeSettingsComponent>(entity);

            if (nodeSettingsComponent.HasMovementRandomOffset == 1)
            {
                var position = EntityManager.GetComponentData<LocalToWorld>(entity).Position;

                DrawWireSphere(position, Color.yellow, nodeSettingsComponent.MaxPathWidth);
            }
        }
    }
}