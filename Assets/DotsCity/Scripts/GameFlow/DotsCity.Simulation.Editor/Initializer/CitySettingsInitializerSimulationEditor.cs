#if UNITY_EDITOR
using Spirit604.Attributes;
using UnityEditor;

namespace Spirit604.DotsCity.Simulation.Initialization
{
    [CustomEditor(typeof(CitySettingsSimulationInitializer))]
    public class CitySettingsSimulationInitializerEditor : Editor
    {
        private const string link = "https://dotstrafficcity.readthedocs.io/en/latest/commonConfigs.html#general-settings-config";

        private GeneralSettingDataSimulationEditor.GeneralSettingDataEditorSettings editorSettings;
        private SerializedProperty settingsProp;
        private SerializedObject settingsSo;

        private void OnEnable()
        {
            editorSettings = GeneralSettingDataSimulationEditor.LoadSettings();
            settingsProp = serializedObject.FindProperty("settings");
            InitSettings();
        }

        private void OnDisable()
        {
            GeneralSettingDataSimulationEditor.SaveSettings(editorSettings);
        }

        public override void OnInspectorGUI()
        {
            DocumentationLinkerUtils.ShowButtonAndHeader(target, link);

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginVertical("HelpBox");

            EditorGUILayout.PropertyField(settingsProp);

            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                InitSettings();
            }

            if (settingsSo != null)
            {
                GeneralSettingDataSimulationEditor.Draw(settingsSo, editorSettings);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void InitSettings()
        {
            settingsSo = null;

            if (settingsProp.objectReferenceValue != null)
            {
                settingsSo = new SerializedObject(settingsProp.objectReferenceValue);
            }
        }
    }
}
#endif
