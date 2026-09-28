#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using System.Text;
using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficIdleStateDebugger : ITrafficDebugger
    {
        private StringBuilder sb = new StringBuilder();
        private EntityManager entityManager;

        public TrafficIdleStateDebugger(EntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public string Tick(Entity entity)
        {
            if (entityManager.HasComponent(entity, typeof(TrafficStateComponent)))
            {
                var trafficStateComponent = entityManager.GetComponentData<TrafficStateComponent>(entity);
                var trafficObstacleComponent = entityManager.GetComponentData<TrafficObstacleComponent>(entity);

                bool hasIdle = false;
                sb.Clear();

                if (trafficStateComponent.TrafficIdleState != TrafficIdleState.Default)
                {
                    hasIdle = true;
                    var flags = trafficStateComponent.TrafficIdleState.GetUniqueFlags();

                    foreach (var flag in flags)
                    {
                        if (flag == TrafficIdleState.Default) continue;

                        sb.Append("IdleState: ").Append(flag).Append(" \n");
                    }
                }

                if (trafficObstacleComponent.HasObstacle)
                {
                    hasIdle = true;
                    sb.Append("Obstacle Car: ").Append(trafficObstacleComponent.ObstacleEntity.Index).Append(" \n");
                }

                if (entityManager.HasComponent<TrafficNpcObstacleComponent>(entity))
                {
                    var trafficNpcObstacleComponent = entityManager.GetComponentData<TrafficNpcObstacleComponent>(entity);

                    if (trafficNpcObstacleComponent.HasObstacle)
                    {
                        hasIdle = true;

                        sb.Append("Has NPC Obstacle").Append(" \n");
                    }
                }

                if (entityManager.HasComponent<TrafficRaycastObstacleComponent>(entity))
                {
                    var trafficRaycastObstacleComponent = entityManager.GetComponentData<TrafficRaycastObstacleComponent>(entity);

                    if (trafficRaycastObstacleComponent.HasObstacle)
                    {
                        hasIdle = true;

                        if (trafficRaycastObstacleComponent.ObstacleEntity != Entity.Null)
                            sb.Append("Raycast Obstacle ").Append(trafficRaycastObstacleComponent.ObstacleEntity).Append(" \n");
                        else
                            sb.Append("Raycast Obstacle").Append(" \n");
                    }
                }

                if (!hasIdle)
                {
                    sb.Append("No idle state");
                }

                return sb.ToString();
            }

            return null;
        }
    }
}
#endif