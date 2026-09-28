using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    public struct SpawnCarDelayData : IBufferElementData
    {
        public int Index;
        public float SpawnTimestamp;
    }
}
