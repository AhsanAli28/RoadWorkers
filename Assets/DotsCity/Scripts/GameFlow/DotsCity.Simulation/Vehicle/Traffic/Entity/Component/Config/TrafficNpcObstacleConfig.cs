using Spirit604.DotsCity.Simulation.Pedestrian;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficNpcObstacleConfig
    {
        public ActionState ObstacleActionStates;
        public float CheckDistanceSQ;
        public float SquareLength;
        public float SideOffsetX;
        public float RateOffsetZ;
        public float MaxYDiff;
    }

#if UNITY_EDITOR
    public struct TrafficNpcObstacleConfigDebug : IComponentData
    {
    }
#endif

    public struct TrafficNpcObstacleConfigReference : IComponentData
    {
        public BlobAssetReference<TrafficNpcObstacleConfig> Config;
    }
}