using Spirit604.Extensions;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.Integration
{
    [CustomEditor(typeof(IntegrationDataContainer))]
    public class IntegrationDataContainerEditor : Editor
    {
        private IntegrationDataContainer container;

        private void OnEnable()
        {
            container = target as IntegrationDataContainer;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            InspectorExtension.DrawGroupBox("Settings", () =>
            {
                var addOffsetProp = serializedObject.FindProperty(nameof(container.AddOffset));
                EditorGUILayout.PropertyField(addOffsetProp);

                if (addOffsetProp.boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.GlobalOffset)));
                }

                if (GUILayout.Button("Integrate"))
                {
                    container.Integrate();
                }

            }, serializedObject.FindProperty(nameof(container.SettingsFlag)));

            InspectorExtension.DrawGroupBox("Generated Cache", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.OverrideMaterial)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.subsceneRelativeAssetPath)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.subsceneRelativeFolderPath)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.reparentAssets)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.reparentVehicles)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.replaceLight)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.convertMaterials)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.qualityIndex)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.cleanVehiclePhrase)));

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.cacheMaterialName)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.cacheContainer)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.SkinDatas)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.Materials)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.ClonedMaterials)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.Prefabs)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.Presets)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.LegacySkins)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.Ragdolls)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.CacheContainerPrefabs)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.ContentScene)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(container.Subscene)));

            }, serializedObject.FindProperty(nameof(container.GeneratedCacheFlag)));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
