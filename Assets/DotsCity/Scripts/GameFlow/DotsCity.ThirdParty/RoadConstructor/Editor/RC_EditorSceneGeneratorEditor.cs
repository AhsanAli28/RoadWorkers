using Spirit604.Extensions;
using Spirit604.Gameplay.Config.Road;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadConstructor
{
    [CustomEditor(typeof(RC_EditorSceneGenerator))]
    public class RC_EditorSceneGeneratorEditor : SharedSettingsEditorBase<RC_EditorSceneGeneratorEditor.EditorSettings>
    {
        [Serializable]
        public class EditorSettings
        {
            public bool LightFoldout = true;
            public bool CrosswalkFoldout = true;
            public bool StrippingFoldout = true;
        }

        private RC_EditorSceneGenerator sceneGenerator;
        private List<List<LightStateInfo>> cacheList = new List<List<LightStateInfo>>();

        protected override string SaveKey => "RC_EditorSceneGeneratorEditor_Key";

        protected override void OnEnable()
        {
            base.OnEnable();
            sceneGenerator = (RC_EditorSceneGenerator)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            InspectorExtension.DrawGroupBox("Light Settings", () =>
            {
                var containerProp = serializedObject.FindProperty("lightStateContainer");

                EditorGUILayout.PropertyField(containerProp);

                if (containerProp.objectReferenceValue != null)
                {
                    SharedLightStateContainerEditor.DrawSettings(containerProp.objectReferenceValue as SharedLightStateContainer, cacheList);
                }

            }, ref SharedSettings.LightFoldout);

            InspectorExtension.DrawGroupBox("Crosswalk Settings", () =>
            {
                var crosswalkShapeTypeProp = serializedObject.FindProperty("crosswalkShapeType");

                EditorGUILayout.PropertyField(crosswalkShapeTypeProp);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("crosswalkWidth"), new GUIContent("Width"));

                if (crosswalkShapeTypeProp.enumValueIndex == 2)
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("crosswalkHeight"), new GUIContent("Height"));

            }, ref SharedSettings.CrosswalkFoldout);

            InspectorExtension.DrawGroupBox("Stripping", () =>
            {
                EditorGUILayout.BeginVertical("GroupBox");

                EditorGUILayout.LabelField("Straight Road", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("stripSplineNodes"), new GUIContent("Strip Nodes"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxStripAngle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minStripDistance"));

                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical("GroupBox");

                EditorGUILayout.LabelField("Intersection", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("stripIntersectionNodes"), new GUIContent("Strip Nodes"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxStripAngleIntersection"), new GUIContent("Max Strip Angle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minStripDistanceIntersection"), new GUIContent("Min Strip Distance"));

                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical("GroupBox");

                EditorGUILayout.LabelField("Pedestrian Node", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("stripPedestrianNodes"), new GUIContent("Strip Nodes"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxStripAnglePed"), new GUIContent("Max Strip Angle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minStripDistancePed"), new GUIContent("Min Strip Distance"));

                EditorGUILayout.EndVertical();

            }, ref SharedSettings.StrippingFoldout);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.BeginVertical("GroupBox");

            if (GUILayout.Button("Generate"))
            {
                sceneGenerator.Generate();
            }

            EditorGUILayout.EndVertical();
        }

        protected override EditorSettings GetDefaultSettings()
        {
            return new EditorSettings();
        }
    }
}
