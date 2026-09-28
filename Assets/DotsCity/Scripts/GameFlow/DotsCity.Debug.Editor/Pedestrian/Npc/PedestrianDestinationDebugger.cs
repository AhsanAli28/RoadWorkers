#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Pedestrian;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

#if REESE_PATH
using Reese.Path;
#endif

namespace Spirit604.DotsCity.Debug
{
    public class PedestrianDestinationDebugger : EntityDebuggerBase
    {
        private const float DestinationRadius = 0.3f;
        private const float SteeringTargetRadius = 0.2f;

        public PedestrianDestinationDebugger(EntityManager entityManager) : base(entityManager)
        {
        }

        public override bool ShouldDraw(Entity entity)
        {
            return false;
        }

        protected override bool ShouldTick(Entity entity)
        {
            return EntityManager.HasComponent<HasTargetTag>(entity) && EntityManager.IsComponentEnabled<HasTargetTag>(entity);
        }

        public override void Tick(Entity entity, Color fontColor)
        {
            base.Tick(entity, fontColor);

            if (ShouldTick(entity))
            {
                var destination = EntityManager.GetComponentData<DestinationComponent>(entity).Value;
                var position = EntityManager.GetComponentData<LocalToWorld>(entity).Position;

                var oldColor = Gizmos.color;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(destination, DestinationRadius);

                bool drawLine = !destination.Equals(float3.zero);

                if (drawLine)
                {
                    Gizmos.DrawLine(position, destination);
                }

#if REESE_PATH
                if (EntityManager.HasComponent<PathBufferElement>(entity))
                {
                    Gizmos.color = Color.magenta;

                    var buffer = EntityManager.GetBuffer<PathBufferElement>(entity);

                    if (buffer.Length > 0)
                    {
                        if (drawLine)
                        {
                            Gizmos.DrawLine(position, buffer[buffer.Length - 1].Value);
                        }

                        for (int i = 0; i < buffer.Length - 1; i++)
                        {
                            var nextIndex = i + 1;
                            Gizmos.DrawWireSphere(buffer[i].Value, SteeringTargetRadius);

                            if (drawLine)
                            {
                                Gizmos.DrawLine(buffer[i].Value, buffer[nextIndex].Value);
                            }

                            if (i == buffer.Length - 2)
                            {
                                Gizmos.DrawWireSphere(buffer[nextIndex].Value, SteeringTargetRadius);
                            }
                        }
                    }
                }
#endif

                Gizmos.color = oldColor;
            }
        }
    }
}
#endif