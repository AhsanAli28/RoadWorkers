using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Npc.Navigation
{
    public struct NavAgentConfig
    {
        public float UpdateFrequency;
        public float MaxDistanceToTargetNode;
        public float MaxCollisionTime;
        public float RecalcFrequency;
        public float MaxDistanceToObstacleSQ;
        public float MaxHeightDiff;
        public bool RevertTargetSupport;
        public float RevertSteeringTargetDistance;
        public float RevertEndTargetRemainingDistance;
    }

    public struct NavAgentConfigReference : IComponentData
    {
        public BlobAssetReference<NavAgentConfig> Config;
    }
}
