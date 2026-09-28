#if UNITY_EDITOR
using Spirit604.Gameplay.Road;
using UnityEditor;

namespace Spirit604.CityEditor.Road
{
    [CustomEditor(typeof(TrafficGroupMaskSettings))]
    public class TrafficGroupMaskSettingsEditor : Editor
    {
        private TrafficGroupMaskSettings trafficGroupMaskSettings;

        private void OnEnable()
        {
            trafficGroupMaskSettings = target as TrafficGroupMaskSettings;
        }

        public override void OnInspectorGUI()
        {
            DrawSettings(trafficGroupMaskSettings, serializedObject);
        }

        public static void DrawSettings(TrafficGroupMaskSettings trafficGroupMaskSettings, SerializedObject so)
        {
            so.Update();

            EditorGUILayout.PropertyField(so.FindProperty(nameof(trafficGroupMaskSettings.DefaultGroup)));

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(so.FindProperty(nameof(trafficGroupMaskSettings.CustomGroups)));

            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
                TrafficGroupMaskSettings.MakeHeaderCache();
            }

            so.ApplyModifiedProperties();
        }
    }
}
#endif