#if UNITY_EDITOR
using Spirit604.Extensions;
using System;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Config.Common
{
    [CustomEditor(typeof(GeneralSettingData))]
    public class GeneralSettingDataEditor : Editor
    {
        public const string GeneralSettingDataEditorKey = "GeneralSettingDataEditorSettings";

        private const string PLAYER_GROUP_NAME = "Player Settings";
        private const string PLAYER_TARGET_GROUP_NAME = "Player Target Settings";
        private const string CAR_GROUP_NAME = "Common Car Settings";
        private const string TRAFFIC_CAR_GROUP_NAME = "Traffic Car Settings";
        private const string PEDESTRIAN_GROUP_NAME = "Pedestrian Settings";
        private const string Other_GROUP_NAME = "Other Settings";

        [Serializable]
        public class GeneralSettingDataEditorSettings
        {
            public bool PlayerFlag = true;
            public bool PlayerTargetFlag = true;
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
            var generalSettings = so.targetObject as GeneralSettingData;
            var worldSimulationTypeProp = so.FindProperty("worldSimulationType");

            InspectorExtension.DrawGroupBox(PLAYER_GROUP_NAME, () =>
            {
                EditorGUILayout.PropertyField(so.FindProperty("playerAgentType"));

                if (generalSettings.PlayerSelected)
                {
                    EditorGUILayout.PropertyField(so.FindProperty("playerControllerType"));

                    if (generalSettings.BuiltInInteractionAvailable)
                    {
                        var vehicleInteractionTypeProp = so.FindProperty("vehicleInteractionType");
                        EditorGUILayout.PropertyField(vehicleInteractionTypeProp);

                        if (vehicleInteractionTypeProp.enumValueIndex == 0 && !generalSettings.DOTSSimulation)
                            EditorGUILayout.PropertyField(so.FindProperty("immediateEnterCar"));
                    }

                    if (worldSimulationTypeProp.enumValueIndex == 0)
                    {
                        EditorGUILayout.PropertyField(so.FindProperty("bulletSupport"));
                        EditorGUILayout.PropertyField(so.FindProperty("bulletCollisionType"));
                    }

                    if (generalSettings.BuiltInSolutionOnly)
                    {
                        EditorGUILayout.PropertyField(so.FindProperty("shootDirectionSource"));
                    }
                }

            }, ref editorSettings.PlayerFlag);

            if (generalSettings.PlayerAvailable && generalSettings.BuiltInSolutionOnly)
            {
                InspectorExtension.DrawGroupBox(PLAYER_TARGET_GROUP_NAME, () =>
                {
                    EditorGUILayout.PropertyField(so.FindProperty("maxTargetDistance"));
                    EditorGUILayout.PropertyField(so.FindProperty("maxCaptureAngle"));
                    EditorGUILayout.PropertyField(so.FindProperty("defaultAimPointDistance"));
                    EditorGUILayout.PropertyField(so.FindProperty("defaultAimPointYPosition"));

                }, ref editorSettings.PlayerTargetFlag);
            }

            if (worldSimulationTypeProp.enumValueIndex == 0)
            {
                InspectorExtension.DrawGroupBox(CAR_GROUP_NAME, () =>
                {
                    EditorGUILayout.PropertyField(so.FindProperty("carVisualDamageSystemSupport"));

                }, ref editorSettings.CarFlag);
            }

            InspectorExtension.DrawGroupBox(TRAFFIC_CAR_GROUP_NAME, () =>
            {
                GeneralSettingDataSimulationEditor.DrawTrafficSettings(so, worldSimulationTypeProp);

            }, ref editorSettings.TrafficFlag);

            InspectorExtension.DrawGroupBox(PEDESTRIAN_GROUP_NAME, () =>
            {
                GeneralSettingDataSimulationEditor.DrawPedestrianSettings(so);

            }, ref editorSettings.PedestrianFlag);

            InspectorExtension.DrawGroupBox(Other_GROUP_NAME, () =>
            {
                GeneralSettingDataSimulationEditor.DrawOtherSettings(so, worldSimulationTypeProp);

                if (worldSimulationTypeProp.enumValueIndex == 1)
                    EditorGUILayout.PropertyField(so.FindProperty("chasingCarsSupport"));

                EditorGUILayout.PropertyField(so.FindProperty("hideUI"));
                EditorGUILayout.PropertyField(so.FindProperty("showFps"));

            }, ref editorSettings.OtherFlag);

            so.ApplyModifiedProperties();
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