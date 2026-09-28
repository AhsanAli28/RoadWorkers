using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct TalkSpawnSettings
    {
        public float TalkingPedestrianSpawnChance;
        public float MaxTalkTime;
        public float MinTalkTime;
        public float2 MinMaxSwitchDuration;
        public BlobArray<AnimationState> TalkingStates;
    }

    public struct TalkSpawnSettingsReference : IComponentData
    {
        public BlobAssetReference<TalkSpawnSettings> Config;
    }
}
