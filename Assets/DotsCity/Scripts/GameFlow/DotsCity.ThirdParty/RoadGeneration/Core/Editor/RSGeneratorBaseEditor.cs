#if UNITY_EDITOR
using Spirit604.Extensions;
using System;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadGeneration
{
    [CustomEditor(typeof(RSGeneratorBase), true)]
    public class RSGeneratorBaseEditor : SharedSettingsEditorBase<RSGeneratorBaseEditor.EditorSettings>
    {
        [Serializable]
        public class EditorSettings
        {
            public bool CacheFlag = true;
            public bool SettingsFlag = true;
            public bool GeneratedFlag = true;
        }

        private const string CustomPrefabTip = "CustomPrefabTip";

        private RSGeneratorBase rsGeneratorBase;

        protected override string SaveKey => "RSGeneratorBaseEditorSettings";

        protected override void OnEnable()
        {
            base.OnEnable();
            rsGeneratorBase = target as RSGeneratorBase;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var configProp = serializedObject.FindProperty("config");

            InspectorExtension.DrawGroupBox("Cache", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("roadParent"));
                EditorGUILayout.PropertyField(configProp);

                GUI.enabled = rsGeneratorBase.CustomPrefabType != null;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("customPrefabs"));
                GUI.enabled = true;

                if (rsGeneratorBase.CustomPrefabType == null)
                {
                    EditorTipExtension.TryToShowInspectorTip(CustomPrefabTip, "Override 'CustomPrefabType' if you want to create roads for custom prefabs.");
                }

            }, ref SharedSettings.CacheFlag);

            InspectorExtension.DrawGroupBox("Settings", () =>
            {
                EditorGUILayout.BeginVertical("GroupBox");

                EditorGUILayout.PropertyField(serializedObject.FindProperty("minRoadLength"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("segmentOffset"));
                var autoLaneCountProp = serializedObject.FindProperty("autoLaneCount");

                EditorGUILayout.PropertyField(autoLaneCountProp);

                if (autoLaneCountProp.boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("laneWidth"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("laneThreshold"));
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("minLaneWidth"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("autoRecalculateRoads"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ignoreRoadNames"));

                EditorGUILayout.EndVertical();

                if (configProp.objectReferenceValue != null)
                {
                    var configSo = new SerializedObject(configProp.objectReferenceValue);
                    RSGeneratorConfigEditorExtension.DrawSettings(configSo);
                }

            }, ref SharedSettings.SettingsFlag);

            InspectorExtension.DrawGroupBox("Generated Data", () =>
            {
                DrawOptionalArray("lockedSegments");
                DrawOptionalArray("failedSegments");
                DrawOptionalArray("duplicateHashSegments");
                DrawOptionalArray("notFoundObjects");
                DrawOptionalArray("ignoredObjects");
                DrawOptionalArray("deadEndNodes");

                EditorGUILayout.PropertyField(serializedObject.FindProperty("generatedSegments"));

            }, ref SharedSettings.GeneratedFlag);

            if (GUILayout.Button("Clear"))
            {
                rsGeneratorBase.ClearScene();
            }

            if (GUILayout.Button("Generate"))
            {
                rsGeneratorBase.Generate();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawOptionalArray(string propName)
        {
            var arrayProp = serializedObject.FindProperty(propName);

            if (arrayProp.arraySize > 0)
            {
                EditorGUILayout.PropertyField(arrayProp, true);
            }
        }

        protected override EditorSettings GetDefaultSettings() => new();
    }
}
#endif
