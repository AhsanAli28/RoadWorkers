#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Spirit604.DotsCity.Simulation.Pedestrian;
using System.Text;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class PedestrianNavigationDebugger : EntityDebuggerBase
    {
        private const float SphereRadius = 0.6f;

        private StringBuilder sb = new StringBuilder();

        public PedestrianNavigationDebugger(EntityManager entityManager) : base(entityManager)
        {
        }

        public override bool ShouldDraw(Entity entity)
        {
            return false;
        }

        public override bool HasCustomColor()
        {
            return true;
        }

        public override Color GetBoundsColor(Entity entity)
        {
            if (!EntityManager.HasComponent<NavAgentComponent>(entity))
            {
                return Color.white;
            }

            var navigationIsEnabled = EntityManager.HasComponent<EnabledNavigationTag>(entity) && EntityManager.IsComponentEnabled<EnabledNavigationTag>(entity);

            return navigationIsEnabled ? Color.green : Color.red;
        }

        protected override bool ShouldTick(Entity entity)
        {
            return base.ShouldTick(entity) && EntityManager.HasComponent(entity, typeof(NavAgentComponent));
        }

        public override StringBuilder GetDescriptionText(Entity entity)
        {
            //var navAgentComponent = EntityManager.GetComponentData<NavAgentComponent>(entity);

            //sb.Clear();
            //sb.Append("AgentIsEnabled: ");
            //sb.Append((navAgentComponent.AgentIsEnabled == 1).ToString() + "\n");
            //sb.Append("NavigationIsEnabled: ");
            //sb.Append((navAgentComponent.NavigationIsEnabled == 1).ToString() + "\n");
            //sb.Append("PathStatus: ");
            //sb.Append((navAgentComponent.PathStatus).ToString() + "\n");

            return sb;
        }

        public override void Tick(Entity entity, Color fontColor)
        {
            base.Tick(entity, fontColor);

            if (!EntityManager.HasComponent<NavAgentComponent>(entity))
            {
                return;
            }

            var navigationIsEnabled = EntityManager.HasComponent<EnabledNavigationTag>(entity) && EntityManager.IsComponentEnabled<EnabledNavigationTag>(entity);

            if (navigationIsEnabled)
            {
                var navAgentSteeringComponent = EntityManager.GetComponentData<NavAgentSteeringComponent>(entity);

                if (navAgentSteeringComponent.HasSteeringTarget)
                {
                    var steeringTarget = navAgentSteeringComponent.SteeringTargetValue;

                    var oldColor = Gizmos.color;
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(steeringTarget, SphereRadius);

                    var position = EntityManager.GetComponentData<LocalToWorld>(entity).Position;

                    Gizmos.DrawLine(position, steeringTarget);
                    Gizmos.color = oldColor;
                }

                var hasAvoidanceBuffer = EntityManager.HasComponent<PathPointAvoidanceElement>(entity);

                if (hasAvoidanceBuffer)
                {
                    var avoidanceBuffer = EntityManager.GetBuffer<PathPointAvoidanceElement>(entity);

                    for (int i = 0; i < avoidanceBuffer.Length; i++)
                    {
                        var target = avoidanceBuffer[i].Point;

                        var oldColor = Gizmos.color;

                        if (i == 0)
                        {
                            Gizmos.color = Color.blue;
                        }
                        else
                        {
                            Gizmos.color = Color.cyan;
                        }

                        Gizmos.DrawWireSphere(target, SphereRadius);

                        Gizmos.color = oldColor;
                    }
                }
            }
        }
    }
}
#endif