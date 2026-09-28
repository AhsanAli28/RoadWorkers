using Spirit604.DotsCity.ThirdParty.RoadGeneration;
using Spirit604.Extensions;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.MVR
{
    [CustomEditor(typeof(MVR_Generator))]
    public class MVR_GeneratorEditor : Editor
    {
        private const string MVR_Generator_Settings = "MVR_Generator_Settings";
        private const string MVR_Generator_Generated = "MVR_Generator_Generated";

        private MVR_Generator m_Generator;

        private void OnEnable()
        {
            m_Generator = (MVR_Generator)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            InspectorExtension.DrawGroupBox("Settings", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("mvr_Config"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("config"));

                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("laneWidth"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("deadEndType"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("autoBakeRoads"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ignoreRoadNames"));

            }, MVR_Generator_Settings);

            RSGeneratorConfigEditorExtension.DrawSettings(m_Generator.Config);

            InspectorExtension.DrawGroupBox("Generated Data", () =>
            {
                DrawOptionalArray("notFound");
                DrawOptionalArray("deadEnds");
                DrawOptionalArray("brokenIntersections");
                DrawOptionalArray("bindingNotFound");
                DrawOptionalArray("ignoreRoads");

            }, MVR_Generator_Generated);

            if (GUILayout.Button("Generate"))
            {
                m_Generator.Generate();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawOptionalArray(string propName)
        {
            var prop = serializedObject.FindProperty(propName);

            if (prop.arraySize > 0)
            {
                EditorGUILayout.PropertyField(prop);
            }
        }
    }
}