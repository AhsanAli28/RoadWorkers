#if UNITY_EDITOR
using Spirit604.Attributes;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Core.Sound
{
    [CustomEditor(typeof(SoundData))]
    public class SoundDataEditor : Editor
    {
        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/sound.html#sound-data";

        private SoundData soundData;
        private bool added;
        private static SoundData previousSelected;

        private void OnEnable()
        {
            soundData = target as SoundData;

            if (previousSelected != null && previousSelected.Id == soundData.Id && previousSelected != soundData)
            {
                soundData.Reset();
            }

            previousSelected = soundData;

            added = soundData.ContainerContainsSound();
        }

        public override void OnInspectorGUI()
        {
            DocumentationLinkerUtils.ShowButtonAndHeader(target, DocLink);

            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(soundData.Id)));

#if !FMOD
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(soundData.Loop)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(soundData.ClipVolume)));

            var randomSoundProp = serializedObject.FindProperty(nameof(soundData.randomSound));

            EditorGUILayout.PropertyField(randomSoundProp);

            if (!randomSoundProp.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(soundData.AudioClip)));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(soundData.Multiclips)));
            }
#else

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(soundData.Name)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(soundData.Parameters)));
#endif

            serializedObject.ApplyModifiedProperties();

            if (!added)
            {
                if (GUILayout.Button("Add To Container"))
                {
                    added = soundData.AddToContainer();
                }
            }
            else
            {
                if (GUILayout.Button("Remove From Container"))
                {
                    added = !soundData.RemoveFromContainer();
                }
            }
        }
    }
}
#endif
