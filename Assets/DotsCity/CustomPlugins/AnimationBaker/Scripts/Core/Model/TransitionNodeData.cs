using UnityEditor;
using UnityEngine;

namespace Spirit604.AnimationBaker
{
    public class TransitionNodeData : NodeData
    {
        [SerializeField] private float transitionDuration;
        [SerializeField] private AnimationTransitionType animationTransitionType;

        public float TransitionDuration
        {
            get => transitionDuration;
            set
            {
                if (transitionDuration != value)
                {
                    transitionDuration = value;

#if UNITY_EDITOR
                    EditorUtility.SetDirty(this);
#endif
                }
            }
        }

        public AnimationTransitionType AnimationTransitionType
        {
            get => animationTransitionType;
            set
            {
                if (animationTransitionType != value)
                {
                    animationTransitionType = value;

#if UNITY_EDITOR
                    EditorUtility.SetDirty(this);
#endif
                }
            }
        }
    }
}
