using Unity.Entities;

namespace Spirit604.AnimationBaker.Entities
{
    public struct GPUSkinTag : IComponentData, IEnableableComponent { }

    public struct HasAnimTransitionTag : IComponentData, IEnableableComponent { }

    public struct SkinAnimatorData : IComponentData
    {
        public int SkinIndex;
        public float StartAnimationTime;
        public int CurrentAnimationHash;
        public bool UniqueAnimation;
        public float LoadSkinTimestamp;
    }

    public struct UpdateSkinTag : IComponentData, IEnableableComponent { }

    public struct SkinUpdateComponent : IComponentData
    {
        public int NewAnimationHash;
        public bool UniqueAnimation;
        public float StartTime;
        public float Timestamp;
    }

    public struct TakenAnimationDataComponent : ICleanupComponentData
    {
        public int SkinIndex;
        public int AnimationHash;
        public int TakenMeshIndex;
    }

    public struct AnimationTransitionData : IComponentData
    {
        public bool IsInitialized;
        public float Speed;
        public int LastAnimHash;

        public Entity CurrentAnimationState;
        public Entity CurrentTransitionState;
        public Entity NextAnimationState;

        public float StartTime;

        public bool HasNextState => NextAnimationState != Entity.Null;
        public bool HasTransitionState => CurrentTransitionState != Entity.Null;
    }
}
