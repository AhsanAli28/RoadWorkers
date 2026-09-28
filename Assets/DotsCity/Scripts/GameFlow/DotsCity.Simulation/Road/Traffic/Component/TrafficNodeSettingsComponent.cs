using Spirit604.Gameplay.Road;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    public struct TrafficNodeSettingsComponent : IComponentData
    {
        public TrafficNodeType TrafficNodeType;
        public int LaneDirectionSide;
        public int LaneIndex;
        public float ChanceToSpawn;
        public float Weight;
        public float CustomAchieveDistance;
        public bool HasCrosswalk;
        public bool AllowedRouteRandomizeSpawning;
        public bool IsAvailableForSpawn;
        public bool IsAvailableForSpawnTarget;

        public int TrafficNodeTypeFlag => 1 << (int)TrafficNodeType;
    }
}