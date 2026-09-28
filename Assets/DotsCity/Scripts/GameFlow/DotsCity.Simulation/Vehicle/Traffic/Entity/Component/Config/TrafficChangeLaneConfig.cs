using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficChangeLaneConfig
    {
        public int CanChangeLane;
        public float MinChangeLaneOffset;
        public float MaxChangeLaneOffset;
        public float MaxDistanceToEndOfPath;
        public float MinDistanceToLastCarInLane;
        public float MinTargetLaneCarDistance;
        public float MaxTargetLaneCarDistance;
        public bool CheckTheIntersectedPaths;
        public bool IgnoreEmptyIntersects;
        public float MaxDistanceToIntersectedPathSQ;
        public float MinCheckFrequency;
        public float MaxCheckFrequency;
        public float BlockDurationAfterChangeLane;
        public float AchieveDistanceSQ;
        public int MinCarsToChangeLane;
        public int MinCarDiffToChangeLane;
        public int ChangeLaneHashMapCapacity;
        public float MaxSpeedDiff;
        public float RandomChangeLaneFrequency;
        public float RandomChangeLaneChance;
    }

    public struct TrafficChangeLaneConfigReference : IComponentData
    {
        public BlobAssetReference<TrafficChangeLaneConfig> Config;
    }
}
