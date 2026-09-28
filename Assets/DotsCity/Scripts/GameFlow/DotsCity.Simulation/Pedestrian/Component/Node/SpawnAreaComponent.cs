using Spirit604.Gameplay.Road;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct SpawnAreaComponent : IComponentData
    {
        public PedestrianAreaShapeType AreaType;
        public float AreaSize;
        public int MinSpawnCount;
        public int MaxSpawnCount;
    }

    public struct TalkAreaSettingsComponent : IComponentData
    {
        public int UnlimitedTalkTime;
        public float MinTalkTime;
        public float MaxTalkTime;
    }

    public struct NodeAreaSpawnRequestedTag : IComponentData, IEnableableComponent { }

    public struct NodeAreaSpawnedTag : IComponentData, IEnableableComponent { }

    public struct NodeTalkAreaTag : IComponentData { }
}