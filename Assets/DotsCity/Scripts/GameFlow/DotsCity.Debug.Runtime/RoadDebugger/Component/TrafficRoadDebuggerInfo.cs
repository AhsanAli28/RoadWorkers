using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    public struct TrafficRoadDebuggerInfo : IComponentData
    {
        public int InstanceId;
        public int Hash;
        public bool DisableLaneChanging;
    }
}
