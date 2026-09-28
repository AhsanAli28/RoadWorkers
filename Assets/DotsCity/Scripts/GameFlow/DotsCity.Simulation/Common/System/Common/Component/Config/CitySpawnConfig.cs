using Spirit604.DotsCity.Core;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    public struct CitySpawnConfig
    {
        public CullStateList TrafficNodeStateList;
        public CullState TrafficSpawnStateNode;
        public CullStateList PedestrianNodeStateList;
        public CullState PedestrianSpawnStateNode;
    }

    public struct CitySpawnConfigReference : IComponentData
    {
        public BlobAssetReference<CitySpawnConfig> Config;
    }
}