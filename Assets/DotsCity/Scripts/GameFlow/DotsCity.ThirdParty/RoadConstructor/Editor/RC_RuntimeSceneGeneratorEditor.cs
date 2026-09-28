using Spirit604.Extensions;
using Spirit604.Gameplay.Config.Road;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadConstructor
{
    [CustomEditor(typeof(RC_RuntimeSceneGenerator))]
    public class RC_RuntimeSceneGeneratorEditor : SharedSettingsEditorBase<RC_RuntimeSceneGeneratorEditor.EditorSettings>
    {
        [Serializable]
        public class EditorSettings
        {
            public bool SceneFoldout = true;
            public bool LightFoldout = true;
            public bool CrosswalkFoldout = true;
        }

        private RC_RuntimeSceneGenerator sceneGenerator;
        private List<List<LightStateInfo>> cacheList = new List<List<LightStateInfo>>();

        protected override string SaveKey => "RC_RuntimeSceneGeneratorEditor_Key";

        protected override void OnEnable()
        {
            base.OnEnable();
            sceneGenerator = (RC_RuntimeSceneGenerator)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            InspectorExtension.DrawGroupBox("Scene Refs", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("roadConstructor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("runtimeRoadManagerCustom"));

            }, ref SharedSettings.SceneFoldout);

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

            serializedObject.ApplyModifiedProperties();
        }

        protected override EditorSettings GetDefaultSettings()
        {
            return new EditorSettings();
        }
    }
}
