using UnityEngine;

namespace Spirit604.AnimationBaker
{
    public abstract class AnimationTextureDataContainerBase : ScriptableObject
    {
        public abstract ContainerType CurrentContainerType { get; }
    }
}