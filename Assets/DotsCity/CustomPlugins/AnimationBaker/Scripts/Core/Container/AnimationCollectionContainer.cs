using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.AnimationBaker
{
    [CreateAssetMenu(menuName = Constans.AssetRootPath + "Animation Collection")]
    public class AnimationCollectionContainer : ScriptableObject
    {
        private const int DefaultInstanceCount = 10;

        [Serializable]
        public class AnimationData
        {
            [Tooltip("Animation name")]
            public string Name;

            [Tooltip("A unique animation mesh instance pool will be created for this animation")]
            public bool UniqueAnimation;

            [Tooltip("Is it allowed to take an animation from the pool if it is already being used by another character")]
            public bool AllowDuplicate;

            [Tooltip("Animation pool size")]
            [Range(1, 200)] public int InstanceCount = DefaultInstanceCount;

            [Tooltip("" +
                "<b>Mandatory</b> : each entity of the crowd must have this animation\r\n\r\n" +
                "<b>Optional</b> : each entity of the crowd doesn't necessarily have to have this animation")]
            public AnimationUseType AnimationType = AnimationUseType.Mandatory;

            public string Guid;

            public int GetInstanceCount() => UniqueAnimation ? InstanceCount : 1;

            public int Hash => AnimUtils.StringToHash(Name);
        }

        [SerializeField] private bool showGuids;
        [SerializeField] private List<AnimationData> animations = new List<AnimationData>();

        public void AddGuid(int index)
        {
            animations[index].Guid = GenerateGuid();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public AnimationData GetAnimation(int index) => index >= 0 && index < animations.Count ? animations[index] : null;

        public AnimationData GetAnimation(string guid) => !string.IsNullOrEmpty(guid) ? animations.Where(a => a.Guid == guid).FirstOrDefault() : null;

        public List<AnimationData> GetAnimations() => animations;

        public List<AnimationData> GetAnimations(AnimationUseType animationUseType) => animations.Where(a => a.AnimationType == animationUseType).ToList();

        public IEnumerable<string> GetAnimationNames() => animations.Select(a => a.Name);

        public IEnumerable<string> GetAnimationNames(AnimationUseType animationUseType) => animations.Where(a => a.AnimationType == animationUseType).Select(a => a.Name);

        public int GetAnimationCount() => animations.Count();

        public int GetAnimationCount(AnimationUseType animationUseType) => animations.Where(a => a.AnimationType == animationUseType).Count();

        private string GenerateGuid() => Guid.NewGuid().ToString();
    }
}