#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Traffic;
using System;
using System.Text;
using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficCollisionDebugger : ITrafficDebugger
    {
        private StringBuilder sb = new StringBuilder();
        private EntityManager entityManager;

        public TrafficCollisionDebugger(EntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public string Tick(Entity entity)
        {
            if (entityManager.HasComponent(entity, typeof(CarCollisionComponent)))
            {
                var collision = entityManager.GetComponentData<CarCollisionComponent>(entity);

                sb.Clear();
                sb.Append("HasCollision: ").Append(collision.HasCollision).Append(" \n");
                sb.Append("Source: ").Append(collision.SourceCollisionDirectionType).Append(" \n");
                sb.Append("Target: ").Append(collision.TargetCollisionDirectionType).Append(" \n");
                sb.Append("CollisionDuration: ").Append(MathF.Round(collision.CollisionDuration, 2)).Append(" \n");

                return sb.ToString();
            }

            return null;
        }
    }
}
#endif