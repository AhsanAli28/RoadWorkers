using Spirit604.Extensions;
using System;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    [CustomEditor(typeof(PedestrianTriggerConfigAuthoring))]
    public class PedestrianTriggerConfigAuthoringEditor : Editor
    {
        private PedestrianTriggerConfigAuthoring pedestrianTriggerConfig;
        private string[] triggerNames;
        private SerializedProperty triggerProp;

        private void OnEnable()
        {
            pedestrianTriggerConfig = target as PedestrianTriggerConfigAuthoring;
            pedestrianTriggerConfig.ValidateTriggerArray();

            triggerNames = Enum.GetNames(typeof(TriggerAreaType));
            triggerProp = serializedObject.FindProperty("triggerDataConfigs");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();

            System.Action callback = () =>
            {
                float originalValue = EditorGUIUtility.labelWidth;

                EditorGUIUtility.labelWidth = 150;

                for (int i = 0; i < triggerProp.arraySize; i++)
                {
                    if (triggerNames[i] == "Default")
                    {
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(triggerNames[i], EditorStyles.popup, GUILayout.Width(150f));
                    EditorGUILayout.PropertyField(triggerProp.GetArrayElementAtIndex(i).FindPropertyRelative("ImpactTriggerDuration"));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUIUtility.labelWidth = originalValue;
            };

            InspectorExtension.DrawGroupBox("Trigger data", callback, true);

            if (GUILayout.Button("Update Config"))
            {
                pedestrianTriggerConfig.UpdateConfig();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}