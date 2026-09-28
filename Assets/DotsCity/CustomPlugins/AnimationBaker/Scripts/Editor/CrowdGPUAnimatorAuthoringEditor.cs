using UnityEditor;
using UnityEngine;

namespace Spirit604.AnimationBaker.EditorInternal
{
    [CustomEditor(typeof(CrowdGPUAnimatorAuthoring))]
    public class CrowdGPUAnimatorAuthoringEditor : Editor
    {
        private CrowdGPUAnimatorAuthoring crowdGPUAnimatorAuthoring;

        private void OnEnable()
        {
            crowdGPUAnimatorAuthoring = target as CrowdGPUAnimatorAuthoring;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUI.enabled = crowdGPUAnimatorAuthoring.AnimatorIsAvailable;

            if (GUILayout.Button("Open Animator"))
            {
                var window = CrowdGPUAnimatorWindow.OpenAnimatorWindow();
                window.Initialize(crowdGPUAnimatorAuthoring.AnimatorContainer, crowdGPUAnimatorAuthoring.AnimationCollectionContainer);
            }

            GUI.enabled = true;
        }
    }
}
