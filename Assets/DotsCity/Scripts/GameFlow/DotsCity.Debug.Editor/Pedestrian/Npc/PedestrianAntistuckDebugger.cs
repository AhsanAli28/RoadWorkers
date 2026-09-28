#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Pedestrian;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class PedestrianAntistuckDebugger : EntityDebuggerBase
    {
        private const float DestinationRadius = 0.3f;
        private const float SteeringTargetRadius = 0.2f;

        public PedestrianAntistuckDebugger(EntityManager entityManager) : base(entityManager)
        {
        }

        public override bool ShouldDraw(Entity entity)
        {
            return false;
        }

        protected override bool ShouldTick(Entity entity)
        {
            return EntityManager.HasComponent<AntistuckDestinationComponent>(entity) && !EntityManager.IsComponentEnabled<AntistuckActivateTag>(entity);
        }

        public override void Tick(Entity entity, Color fontColor)
        {
            base.Tick(entity, fontColor);

            if (ShouldTick(entity))
            {
                var destination = EntityManager.GetComponentData<AntistuckDestinationComponent>(entity).Destination;

                var oldColor = Gizmos.color;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(destination, DestinationRadius);

                Gizmos.color = oldColor;
            }
        }
    }
}
#endif