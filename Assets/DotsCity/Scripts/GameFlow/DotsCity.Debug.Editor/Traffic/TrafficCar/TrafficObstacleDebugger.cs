#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Traffic;
using System.Text;
using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficObstacleDebugger : ITrafficDebugger
    {
        private StringBuilder sb = new StringBuilder();
        private EntityManager entityManager;

        public TrafficObstacleDebugger(EntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public string Tick(Entity entity)
        {
            if (!entityManager.HasComponent(entity, typeof(TrafficObstacleComponent)))
            {
                return null;
            }

            var obstacleComponent = entityManager.GetComponentData<TrafficObstacleComponent>(entity);

            sb.Clear();

            if (!entityManager.IsComponentEnabled<TrafficIdleTag>(entity))
            {
                Entity obstacleEntity = obstacleComponent.ObstacleEntity;
                Entity raycastObstacle = Entity.Null;

                bool hasNpcObstacle = false;

                if (entityManager.HasComponent<TrafficRaycastObstacleComponent>(entity))
                {
                    raycastObstacle = entityManager.GetComponentData<TrafficRaycastObstacleComponent>(entity).ObstacleEntity;
                }

                if (entityManager.HasComponent<TrafficNpcObstacleComponent>(entity))
                {
                    hasNpcObstacle = entityManager.GetComponentData<TrafficNpcObstacleComponent>(entity).HasObstacle;
                }

                if (obstacleEntity != Entity.Null)
                {
                    sb.Append("ObstacleEntity: ");
                    sb.Append(obstacleEntity.Index).Append(" \n");
                }
                else
                {

                    if (raycastObstacle != Entity.Null || !hasNpcObstacle)
                    {
                        sb.Append("ObstacleEntity: ");
                        sb.Append(raycastObstacle.Index).Append(" \n");
                    }
                }

                if ((obstacleEntity != Entity.Null || raycastObstacle == Entity.Null) && !hasNpcObstacle)
                {
                    sb.Append(obstacleComponent.ObstacleType).Append(" \n");
                }
                else
                {
                    if (raycastObstacle != Entity.Null || !hasNpcObstacle)
                    {
                        sb.Append("Raycast").Append(" \n");
                    }
                    else
                    {
                        sb.Append("NPC").Append(" \n");
                    }
                }

                if (obstacleComponent.Ignore)
                {
                    sb.Append("Ignore: ").Append(obstacleComponent.IgnoreType).Append(" \n");
                }
            }
            else
            {
                sb.Append("Idling").Append(" \n");
            }

            return sb.ToString();
        }
    }
}
#endif