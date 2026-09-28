#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Config;
using Spirit604.Extensions;
using System;
using Unity.Physics;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity
{
    [CustomEditor(typeof(GeneralSettingDataSimulation))]
    public class GeneralSettingDataSimulationEditor : Editor
    {
        public const string GeneralSettingDataEditorKey = "GeneralSettingDataEditorSettings";

        private const string CAR_GROUP_NAME = "Common Car Settings";
        private const string TRAFFIC_CAR_GROUP_NAME = "Traffic Car Settings";
        private const string PEDESTRIAN_GROUP_NAME = "Pedestrian Settings";
        private const string Other_GROUP_NAME = "Other Settings";

        [Serializable]
        public class GeneralSettingDataEditorSettings
        {
            public bool CarFlag = true;
            public bool TrafficFlag = true;
            public bool PedestrianFlag = true;
            public bool OtherFlag = true;
        }

        private GeneralSettingDataEditorSettings editorSettings;

        private void OnEnable()
        {
            editorSettings = LoadSettings();
        }

        private void OnDisable()
        {
            SaveSettings(editorSettings);
        }

        public override void OnInspectorGUI()
        {
            InspectorExtension.DrawDefaultHeaderScript(target);

            Draw(serializedObject, editorSettings);
        }

        public static void Draw(SerializedObject so, GeneralSettingDataEditorSettings editorSettings)
        {
            so.Update();

            var worldSimulationTypeProp = so.FindProperty("worldSimulationType");

            InspectorExtension.DrawGroupBox(CAR_GROUP_NAME, () =>
            {
                EditorGUILayout.PropertyField(so.FindProperty("carVisualDamageSystemSupport"));

            }, ref editorSettings.CarFlag);

            InspectorExtension.DrawGroupBox(TRAFFIC_CAR_GROUP_NAME, () =>
            {
                DrawTrafficSettings(so, worldSimulationTypeProp);

            }, ref editorSettings.TrafficFlag);

            InspectorExtension.DrawGroupBox(PEDESTRIAN_GROUP_NAME, () =>
            {
                DrawPedestrianSettings(so);

            }, ref editorSettings.PedestrianFlag);

            InspectorExtension.DrawGroupBox(Other_GROUP_NAME, () =>
            {
                EditorGUILayout.PropertyField(so.FindProperty("worldSimulationType"));
                EditorGUILayout.PropertyField(so.FindProperty("physicsSimulationType"));
                EditorGUILayout.PropertyField(so.FindProperty("cullPhysics"));
                EditorGUILayout.PropertyField(so.FindProperty("cullStaticPhysics"));
                EditorGUILayout.PropertyField(so.FindProperty("forceLegacyPhysics"));
                EditorGUILayout.PropertyField(so.FindProperty("propsPhysics"));
                EditorGUILayout.PropertyField(so.FindProperty("propsDamageSystemSupport"));
                EditorGUILayout.PropertyField(so.FindProperty("healthSystemSupport"));

            }, ref editorSettings.OtherFlag);

            so.ApplyModifiedProperties();
        }

        public static void DrawTrafficSettings(SerializedObject so, SerializedProperty worldSimulationTypeProp)
        {
            EditorGUILayout.PropertyField(so.FindProperty("hasTraffic"));
            EditorGUILayout.PropertyField(so.FindProperty("trafficBakingType"));
            EditorGUILayout.PropertyField(so.FindProperty("changeLaneSupport"));
            EditorGUILayout.PropertyField(so.FindProperty("trafficPublicSupport"));
            EditorGUILayout.PropertyField(so.FindProperty("trainSupport"));
            EditorGUILayout.PropertyField(so.FindProperty("trafficParkingSupport"));
            EditorGUILayout.PropertyField(so.FindProperty("antiStuckSupport"));
            EditorGUILayout.PropertyField(so.FindProperty("avoidanceSupport"));

            if (worldSimulationTypeProp.enumValueIndex == 0)
            {
                EditorGUILayout.PropertyField(so.FindProperty("railMovementSupport"));
                EditorGUILayout.PropertyField(so.FindProperty("carHitCollisionReaction"));
                EditorGUILayout.PropertyField(so.FindProperty("wheelSystemSupport"));
            }
            else
            {
                EditorGUILayout.PropertyField(so.FindProperty("vehicleLightSupport"));
            }
        }

        public static void DrawPedestrianSettings(SerializedObject so)
        {
            EditorGUILayout.PropertyField(so.FindProperty("hasPedestrian"));
            EditorGUILayout.PropertyField(so.FindProperty("pedestrianBakingType"));
            EditorGUILayout.PropertyField(so.FindProperty("pedestrianTriggerSystemSupport"));
            EditorGUILayout.PropertyField(so.FindProperty("navigationSupport"));
            EditorGUILayout.PropertyField(so.FindProperty("talkingSupport"));
            EditorGUILayout.PropertyField(so.FindProperty("benchSystemSupport"));
        }

        public static void DrawOtherSettings(SerializedObject so, SerializedProperty worldSimulationTypeProp)
        {
            EditorGUILayout.PropertyField(worldSimulationTypeProp);

            if (worldSimulationTypeProp.enumValueIndex == 0)
            {
                EditorGUILayout.PropertyField(so.FindProperty("physicsSimulationType"));
            }
            else
            {
                using (new EditorGUI.DisabledScope(disabled: true))
                {
                    EditorGUILayout.EnumPopup("DOTS Physics Type", SimulationType.NoPhysics);
                }
            }

            if (worldSimulationTypeProp.enumValueIndex == 0)
            {
                EditorGUILayout.PropertyField(so.FindProperty("cullPhysics"));
                EditorGUILayout.PropertyField(so.FindProperty("cullStaticPhysics"));
                EditorGUILayout.PropertyField(so.FindProperty("forceLegacyPhysics"));
                EditorGUILayout.PropertyField(so.FindProperty("propsPhysics"));
                EditorGUILayout.PropertyField(so.FindProperty("propsDamageSystemSupport"));
                EditorGUILayout.PropertyField(so.FindProperty("healthSystemSupport"));
            }
        }

        public static GeneralSettingDataEditorSettings LoadSettings()
        {
            GeneralSettingDataEditorSettings editorSettings = null;

            if (EditorPrefs.HasKey(GeneralSettingDataEditorKey))
            {
                try
                {
                    editorSettings = JsonUtility.FromJson<GeneralSettingDataEditorSettings>(EditorPrefs.GetString(GeneralSettingDataEditorKey));
                }
                catch
                {
                    editorSettings = new GeneralSettingDataEditorSettings();
                    SaveSettings(editorSettings);
                }
            }
            else
            {
                editorSettings = new GeneralSettingDataEditorSettings();
                SaveSettings(editorSettings);
            }

            return editorSettings;
        }

        public static void SaveSettings(GeneralSettingDataEditorSettings editorSettings)
        {
            if (editorSettings != null)
            {
                EditorPrefs.SetString(GeneralSettingDataEditorKey, JsonUtility.ToJson(editorSettings));
            }
        }
    }
}
#endif