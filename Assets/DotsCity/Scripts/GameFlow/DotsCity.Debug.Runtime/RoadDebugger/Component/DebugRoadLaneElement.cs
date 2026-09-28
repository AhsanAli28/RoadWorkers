using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    public struct DebugRoadLaneElement : IBufferElementData
    {
        public bool IdleCar;
        public float NormalizedPathPosition;
        public float SpawnDelay;
        public int SpawnCarModel;

        public Entity TrafficNodeEntity;
        public int LocalPathIndex;
    }
}
