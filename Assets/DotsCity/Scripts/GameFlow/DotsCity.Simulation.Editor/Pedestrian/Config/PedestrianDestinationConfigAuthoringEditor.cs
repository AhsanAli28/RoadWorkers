using Spirit604.Extensions;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    [CustomEditor(typeof(PedestrianDestinationConfigAuthoring))]
    public class PedestrianDestinationConfigAuthoringEditor : Editor
    {
        private PedestrianDestinationConfigAuthoring config;

        private void OnEnable()
        {
            config = target as PedestrianDestinationConfigAuthoring;
        }

        public override void OnInspectorGUI()
        {
            InspectorExtension.DrawDefaultHeaderScript(config);

            serializedObject.Update();

            var holderProp = serializedObject.FindProperty("pedestrianSpawnerConfigHolder");

            EditorGUI.BeginChangeCheck();

            if (holderProp.objectReferenceValue != null)
            {
                var holder = (PedestrianSpawnerConfigHolder)holderProp.objectReferenceValue;

                if (holder.PedestrianSettingsConfig)
                {
                    var configSo = new SerializedObject(holder.PedestrianSettingsConfig);
                    configSo.Update();

                    EditorGUILayout.PropertyField(configSo.FindProperty("achieveDistance"));

                    configSo.ApplyModifiedProperties();
                }
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("ignorePreviousDst"));

            if (EditorGUI.EndChangeCheck())
            {
                if (Application.isPlaying)
                {
                    serializedObject.ApplyModifiedProperties();
                    config.ConfigChanged();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}