using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic.Obstacle
{
    public struct ObstacleResult
    {
        public Entity ObstacleEntity;
        public ObstacleType ObstacleType;

        public ObstacleResult(Entity obstacleEntity) : this()
        {
            ObstacleEntity = obstacleEntity;
            ObstacleType = ObstacleType.Undefined;
        }

        public ObstacleResult(Entity obstacleEntity, ObstacleType obstacleType)
        {
            ObstacleEntity = obstacleEntity;
            ObstacleType = obstacleType;
        }

        public bool HasObstacle => ObstacleEntity != Entity.Null;
    }
}
