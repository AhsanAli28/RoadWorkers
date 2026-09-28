using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficNpcObstacleDebug : TrafficObstacleDistanceDebug
    {

        public TrafficNpcObstacleDebug(EntityManager entityManager) : base(entityManager)
        {
        }

        public override void Tick(Entity entity)
        {
            base.Tick(entity);

            if (EntityManager.HasComponent(entity, typeof(TrafficNpcObstacleComponent)))
            {
#if UNITY_EDITOR
                TrafficNpcObstacleDebugger.DrawDebug(entity, Color.white, false);
#endif
            }
        }

        public override Color GetBoundsColor(Entity entity)
        {
            if (EntityManager.HasComponent(entity, typeof(TrafficNpcObstacleComponent)))
            {
                var npcObstacle = EntityManager.GetComponentData<TrafficNpcObstacleComponent>(entity);

                var color = npcObstacle.HasObstacle ? Color.red : Color.green;
                return color;
            }
            else return default;
        }
    }
}