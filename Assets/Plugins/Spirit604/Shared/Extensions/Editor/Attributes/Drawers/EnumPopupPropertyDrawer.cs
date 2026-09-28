using UnityEditor;
using UnityEngine;

namespace Spirit604.Extensions
{
    [CustomPropertyDrawer(typeof(EnumPopupAttribute), true)]
    public sealed class EnumPopupPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                if (property.propertyType == SerializedPropertyType.Enum)
                {
                    int newIntValue = EditorGUI.Popup(position, property.displayName, property.enumValueIndex, property.enumDisplayNames);

                    if (property.enumValueIndex != newIntValue)
                    {
                        property.enumValueIndex = newIntValue;
                    }
                }
                else
                {
                    EditorGUI.LabelField(position, "Wrong enum type");
                }
            }
        }
    }
}