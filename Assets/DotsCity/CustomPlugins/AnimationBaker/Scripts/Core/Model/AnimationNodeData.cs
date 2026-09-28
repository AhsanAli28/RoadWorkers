using Spirit604.AnimationBaker.EditorInternal;
using Spirit604.AnimationBaker.Utils;
using UnityEngine;

namespace Spirit604.AnimationBaker
{
    public class AnimationNodeData : NodeData
    {
        public string AnimName;

        [SerializeField] private bool uniqueAnimation;
        [ReadOnly] public int AnimHash;

        public bool UniqueAnimation
        {
            get => uniqueAnimation;
            set
            {
                if (uniqueAnimation != value)
                {
                    uniqueAnimation = value;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        public void UpdateHash()
        {
            AnimHash = Animator.StringToHash(AnimName);
        }

        public void SetAnimName(string newAnimName)
        {
            if (AnimName != newAnimName)
            {
                AnimName = newAnimName;
                UpdateHash();

                EditorSaver.SetObjectDirty(this);
            }
        }

        public void CopyHash()
        {
#if UNITY_EDITOR
            GUIUtility.systemCopyBuffer = AnimHash.ToString();
#endif
        }
    }
}
