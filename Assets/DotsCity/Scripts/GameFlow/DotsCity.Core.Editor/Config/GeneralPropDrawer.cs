using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public static class GeneralPropDrawer
    {
        public static void DrawProp(string propName)
        {
            var instance = CitySettingsInitializerBase.EditorInstance;

            if (instance == null) return;

            var config = instance.GetSettings<GeneralSettingDataCore>();

            if (config == null) return;

            var so = new SerializedObject(config);
            so.Update();

            var prop = so.FindProperty(propName);

            if (prop == null) return;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(prop);

            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
            }
        }

        public static void DrawProp(string propName, Rect rect)
        {
            var instance = CitySettingsInitializerBase.EditorInstance;

            if (instance == null) return;

            var config = instance.GetSettings<GeneralSettingDataCore>();

            if (config == null) return;

            var so = new SerializedObject(config);
            so.Update();

            var prop = so.FindProperty(propName);

            if (prop == null) return;

            EditorGUI.BeginChangeCheck();

            EditorGUI.PropertyField(rect, prop);

            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
            }
        }
    }
}