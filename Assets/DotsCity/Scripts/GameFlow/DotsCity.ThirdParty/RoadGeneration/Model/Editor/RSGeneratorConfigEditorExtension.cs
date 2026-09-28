#if UNITY_EDITOR
using Spirit604.Extensions;
using Spirit604.Gameplay.Config.Road;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadGeneration
{
    public static class RSGeneratorConfigEditorExtension
    {
        private const string TrafficNodeKey = "RSGeneratorConfig_TrafficNode";
        private const string PedestrianNodeKey = "RSGeneratorConfig_PedestrianNode";
        private const string TrafficLightKey = "RSGeneratorConfig_TrafficLight";
        private const string SettingsKey = "RSGeneratorConfig_Settings";

        private static List<List<LightStateInfo>> cacheList;

        public static void DrawSettings(RSGeneratorConfig config)
        {
            if (config)
            {
                var so = new SerializedObject(config);
                DrawSettings(so);
            }
        }

        public static void DrawSettings(SerializedObject serializedObject, RSGeneratorConfig config = null)
        {
            serializedObject.Update();

            InspectorExtension.DrawGroupBox("Traffic Node", () =>
            {
                EditorGUILayout.BeginVertical("GroupBox");

                var stripOutNodesProp = serializedObject.FindProperty("stripOutNodes");

                EditorGUILayout.PropertyField(stripOutNodesProp);

                if (stripOutNodesProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("minStripAngle"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("minStripDistance"));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical("GroupBox");

                var generateSpawnNodesProp = serializedObject.FindProperty("generateInnerPathSpawnNodes");

                EditorGUILayout.PropertyField(generateSpawnNodesProp);

                if (generateSpawnNodesProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("minNodeOffsetDistance"));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();

            }, TrafficNodeKey);

            InspectorExtension.DrawGroupBox("Pedestrian Node", () =>
            {
                var addPedestrianNodeProp = serializedObject.FindProperty("addPedestrianNode");

                EditorGUILayout.PropertyField(addPedestrianNodeProp);

                if (addPedestrianNodeProp.boolValue)
                {
                    PedstrianCommon(serializedObject);

                    CrosswalkSettings(serializedObject);

                    PedestrianSplineSettings(serializedObject);
                }

            }, PedestrianNodeKey);

            InspectorExtension.DrawGroupBox("Light Settings", () =>
            {
                var lightTypeProp = serializedObject.FindProperty("crossingLightType");

                EditorGUILayout.PropertyField(lightTypeProp);

                if (lightTypeProp.enumValueIndex == 1)
                {
                    var containerProp = serializedObject.FindProperty("sharedLightStateContainer");

                    EditorGUILayout.PropertyField(containerProp);

                    if (containerProp.objectReferenceValue != null)
                    {
                        if (cacheList == null)
                            cacheList = new List<List<LightStateInfo>>();

                        SharedLightStateContainerEditor.DrawSettings(containerProp.objectReferenceValue as SharedLightStateContainer, cacheList);
                    }
                }

            }, TrafficLightKey);

            InspectorExtension.DrawGroupBox("Settings", () =>
            {
                EditorGUILayout.BeginVertical("HelpBox");

                EditorGUILayout.LabelField("Crossing", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("straightSpeedLimit"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("turnSpeedLimit"));

                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical("HelpBox");

                EditorGUILayout.LabelField("Custom Straight Road", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("splineSpeedLimit"));

                if (serializedObject.FindProperty("customStraightRoadSettings").boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("customDatas"));
                }

                EditorGUILayout.EndVertical();

            }, SettingsKey);

            serializedObject.ApplyModifiedProperties();
        }

        private static void PedestrianSplineSettings(SerializedObject serializedObject)
        {
            EditorGUILayout.BeginVertical("GroupBox");

            EditorGUILayout.LabelField("Spline Road", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("lineNodeOffset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("nodeSpacing"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("ignoreNodeRoads"));

            EditorGUILayout.EndVertical();
        }

        private static void CrosswalkSettings(SerializedObject serializedObject)
        {
            EditorGUILayout.BeginVertical("GroupBox");

            EditorGUILayout.LabelField("Crosswalk", EditorStyles.boldLabel);

            var nodeShapeTypeProp = serializedObject.FindProperty("nodeShapeType");

            EditorGUILayout.PropertyField(nodeShapeTypeProp);

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("crosswalkWidth"), new GUIContent("Width"));

            if (nodeShapeTypeProp.enumValueIndex == 2)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("crosswalkHeight"), new GUIContent("Height"));
            }

            EditorGUI.indentLevel--;

            var crosswalkOffsetTypeProp = serializedObject.FindProperty("crosswalkOffsetType");

            EditorGUILayout.PropertyField(crosswalkOffsetTypeProp);

            if (crosswalkOffsetTypeProp.enumValueIndex == 1)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("crosswalkOffset"));
            }

            EditorGUILayout.EndVertical();
        }

        private static void PedstrianCommon(SerializedObject serializedObject)
        {
            EditorGUILayout.BeginVertical("GroupBox");

            EditorGUILayout.LabelField("Common", EditorStyles.boldLabel);

            var pedestrianPathWidthtypeProp = serializedObject.FindProperty("pedestrianPathWidthType");

            EditorGUILayout.PropertyField(pedestrianPathWidthtypeProp);

            if (pedestrianPathWidthtypeProp.enumValueIndex == 1)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(serializedObject.FindProperty("pedestrianPathWidth"));

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }
    }
}
#endif
