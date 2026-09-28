using UnityEditor;
using UnityEngine;

namespace Spirit604.AnimationBaker
{
    [CustomEditor(typeof(AnimationNodeData))]
    public class AnimationNodeDataEditor : Editor
    {
        private AnimationNodeData animationNodeData;

        private void OnEnable()
        {
            animationNodeData = target as AnimationNodeData;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Copy Hash"))
            {
                animationNodeData.CopyHash();
            }
        }
    }
}
