using UnityEngine;
using UnityEngine.Serialization;

namespace Spirit604.AnimationBaker
{
    public class CrowdGPUAnimatorAuthoring : MonoBehaviour
    {
        [SerializeField] private AnimatorDataContainer animatorContainer;

        [FormerlySerializedAs("bakedAnimationCollection")]
        [SerializeField] private AnimationCollectionContainer animationCollection;

        public bool AnimatorIsAvailable => animatorContainer;

        public AnimatorDataContainer AnimatorContainer { get => animatorContainer; }

        public AnimationCollectionContainer AnimationCollectionContainer { get => animationCollection; }
    }
}
