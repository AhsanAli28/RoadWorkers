using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Sound.Pedestrian
{
    public struct CrowdSoundData : IComponentData
    {
        public int InnerCrowdSoundCount;
        public int OuterCrowdSoundCount;
        public int MinCrowdSoundCount;
        public float OuterMaxVolume;
        public float MaxVolume;
        public float MinVolume;
        public float InnerCellOffset;
        public float OuterCellOffset;
        public float LerpVolumeSpeed;
        public float MinHeightMuting;
        public float MaxHeight;
    }

    public struct CrowdSoundVolume : IComponentData
    {
        public float CurrentVolume;
        public float TargetVolume;
    }
}