#if UNITY_EDITOR && FCG
using Spirit604.Attributes;
using Spirit604.DotsCity.ThirdParty.RoadGeneration;
using Spirit604.Extensions;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.FCG
{
    [CustomEditor(typeof(FCGSceneGenerator))]
    public class FCGSceneGeneratorEditor : Editor
    {
        private const string Link = "https://dotstrafficcity.readthedocs.io/en/latest/fcg.html";

        private const string Key = "FCGSceneGenerator_Common";
        private FCGSceneGenerator fCGSceneGenerator;

        private void OnEnable()
        {
            fCGSceneGenerator = target as FCGSceneGenerator;
            UpdateConfig();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (fCGSceneGenerator.ConnectEditorBlocks || fCGSceneGenerator.ImportTemplate)
            {
                EditorGUILayout.BeginVertical("GroupBox");

                DocumentationLinkerUtils.ShowButtonFirst(Link);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("mode"));

                if (fCGSceneGenerator.ImportTemplate)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("fcgPrefabBlocks"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("templateContainer"));

                    DrawOptional("brokenBlocks");
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("templateContainer"));
                }

                serializedObject.ApplyModifiedProperties();

                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical("GroupBox");

                if (GUILayout.Button("Generate"))
                {
                    fCGSceneGenerator.Generate();
                }

                EditorGUILayout.EndVertical();

                return;
            }

            InspectorExtension.DrawGroupBox("Common Settings", () =>
            {
                var modeProp = serializedObject.FindProperty("mode");

                DocumentationLinkerUtils.ShowButtonFirst(Link);

                EditorGUILayout.PropertyField(modeProp);

                if (fCGSceneGenerator.GeneratePrefabBlocks)
                {
                    InspectorExtension.DrawConfigField<FCGPrefabContainer>(serializedObject, "FCGPrefabContainer", "FCG_PrefabContainer");
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("fcgPrefabBlocks"));
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("crossingOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("roadOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("laneCount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("laneWidth"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("autoConnectSegments"));

                InspectorExtension.DrawConfigField<RSGeneratorConfig>(serializedObject, "config", "FCG_Config", () => UpdateConfig());
            }, Key);

            RSGeneratorConfigEditorExtension.DrawSettings(fCGSceneGenerator.Config);

            InspectorExtension.DrawGroupBox("Generated", () =>
            {
                if (fCGSceneGenerator.EditorMode)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("brokenWays"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("segmentCreators"));
                }
                else
                {
                    DrawOptional("brokenBlocks");
                }
            });

            if (GUILayout.Button("Clear"))
            {
                fCGSceneGenerator.UserClear();
            }

            if (GUILayout.Button("Generate"))
            {
                fCGSceneGenerator.Generate();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void UpdateConfig()
        {
            if (fCGSceneGenerator.Config)
            {
                fCGSceneGenerator.Config.CustomStraightRoadSettings = false;
                EditorSaver.SetObjectDirty(fCGSceneGenerator.Config);
            }
        }

        private void DrawOptional(string arrayPropName)
        {
            var prop = serializedObject.FindProperty(arrayPropName);

            if (prop != null && prop.arraySize > 0)
                EditorGUILayout.PropertyField(prop);
        }
    }
}
#endif
