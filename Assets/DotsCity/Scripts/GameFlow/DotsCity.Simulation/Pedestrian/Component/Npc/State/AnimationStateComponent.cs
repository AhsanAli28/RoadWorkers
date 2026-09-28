using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    public struct AnimationStateComponent : IComponentData
    {
        public AnimationState NewAnimationState;
        public float NewStartPlaybacktime;
        public AnimationState AnimationState;
        public AnimationState PreviousAnimationState;
        public float StartTime;
    }
}