using UnityEditor;
using UnityEngine;

namespace Spirit604.AnimationBaker
{
    [CustomEditor(typeof(CharacterAnimationContainer))]
    public class CharacterAnimationContainerEditor : Editor
    {
        private CharacterAnimationContainer characterAnimationContainer;

        private void OnEnable()
        {
            characterAnimationContainer = target as CharacterAnimationContainer;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Clear"))
            {
                characterAnimationContainer.Clear(true);
            }

            EditorGUILayout.HelpBox("Character data is only set at the NPC factory.", MessageType.Info);
        }
    }
}