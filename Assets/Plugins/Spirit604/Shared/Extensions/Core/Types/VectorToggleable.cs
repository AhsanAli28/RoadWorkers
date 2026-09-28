using Spirit604.Attributes;
using System;
using UnityEditor;
using UnityEngine;

namespace Spirit604.Extensions
{
    [Serializable]
    public struct VectorToggleable
    {
        public Vector3 Value;
        public bool Enabled;
        public string Label;

        public VectorToggleable(Vector3 value)
        {
            this.Value = value;
            this.Enabled = false;
            this.Label = String.Empty;
        }

        public VectorToggleable(string label)
        {
            this.Label = label;
            this.Value = default;
            this.Enabled = false;
        }

        public VectorToggleable(string label, Vector3 value) : this(value)
        {
            this.Label = label;
            this.Enabled = false;
        }

        public VectorToggleable(string label, Vector3 value, bool enabled) : this(label, value)
        {
            this.Enabled = enabled;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(VectorToggleable))]
    internal class VectorToggleableDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var valueField = property.FindPropertyRelative("Value");

            var toggleField = property.FindPropertyRelative("Enabled");

            var labelField = property.FindPropertyRelative("Label");
            var labelValue = labelField.stringValue;

            GUI.enabled = toggleField.boolValue;

            const float toggleWidth = 20f;
            const float spacing = 5f;

            var r1 = position;

            r1.width -= (toggleWidth);

            Vector3 newValue = default;

            var wideMode = EditorGUIUtility.wideMode;
            var previousLabelWidth = EditorGUIUtility.labelWidth;

            if (!EditorGUIUtility.wideMode)
            {
                EditorGUIUtility.wideMode = true;
                EditorGUIUtility.labelWidth = 150f;
            }

            if (!string.IsNullOrEmpty(labelValue))
            {
                string toolTip = string.Empty;

                var tt = AttributeExtension.GetAttribute<TooltipAttribute>(fieldInfo);

                if (tt != null) toolTip = tt.tooltip;

                var labelContent = new GUIContent(labelValue, toolTip);

                newValue = EditorGUI.Vector3Field(r1, labelContent, valueField.vector3Value);
            }
            else
            {
                newValue = EditorGUI.Vector3Field(r1, string.Empty, valueField.vector3Value);
            }

            if (valueField.vector3Value != newValue)
            {
                valueField.vector3Value = newValue;
            }

            EditorGUIUtility.wideMode = wideMode;
            EditorGUIUtility.labelWidth = previousLabelWidth;

            GUI.enabled = true;

            var r2 = r1;
            r2.x += r2.width + spacing;
            r2.width = toggleWidth;

            // Draw the toggle.
            var enabled = EditorGUI.Toggle(r2, toggleField.boolValue);

            if (toggleField.boolValue != enabled)
            {
                toggleField.boolValue = enabled;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
#endif
}
