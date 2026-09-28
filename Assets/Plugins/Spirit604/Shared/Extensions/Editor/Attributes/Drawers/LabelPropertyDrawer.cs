#if !ODIN_INSPECTOR
using UnityEditor;
using UnityEngine;

namespace Spirit604.Attributes.Editor
{
    [CustomPropertyDrawer(typeof(LabelAttribute))]
    public class LabelPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var labelAttr = attribute as LabelAttribute;

            EditorGUI.PropertyField(rect, property, new GUIContent(labelAttr.Label), true);
        }
    }
}
#endif
