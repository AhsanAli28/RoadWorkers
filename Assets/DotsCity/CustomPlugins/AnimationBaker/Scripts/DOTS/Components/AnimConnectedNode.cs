using Unity.Entities;

namespace Spirit604.AnimationBaker.Entities
{
    public struct AnimConnectedNode : IBufferElementData
    {
        public Entity NextState;
        public Entity TransitionState;
    }
}
