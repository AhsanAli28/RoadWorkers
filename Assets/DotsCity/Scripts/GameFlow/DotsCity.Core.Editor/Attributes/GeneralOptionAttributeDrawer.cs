using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    [CustomPropertyDrawer(typeof(GeneralOptionAttribute))]
    public class GeneralOptionAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (GeneralOptionAttribute)attribute;

            GeneralPropDrawer.DrawProp(attr.PropName);
            EditorGUILayout.PropertyField(property, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0;
        }
    }
}