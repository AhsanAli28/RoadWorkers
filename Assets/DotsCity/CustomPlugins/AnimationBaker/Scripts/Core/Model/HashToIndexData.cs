
namespace Spirit604.AnimationBaker
{
    public struct HashToIndexData
    {
        /// <summary> Local animation index of the skin's clip array.</summary>
        public readonly int LocalAnimationIndex;

        /// <summary> Index of the animation in the All animations array of all skins.</summary>
        public readonly int CrowdAnimationIndex;

        public HashToIndexData(int localAnimationIndex, int crowdAnimationIndex)
        {
            LocalAnimationIndex = localAnimationIndex;
            CrowdAnimationIndex = crowdAnimationIndex;
        }
    }
}
