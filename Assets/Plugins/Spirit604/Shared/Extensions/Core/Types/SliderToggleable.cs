using Spirit604.Attributes;
using System;
using UnityEditor;
using UnityEngine;

namespace Spirit604.Extensions
{
    [Serializable]
    public struct SliderToggleable
    {
        public float Value;
        public bool Enabled;
        public string Label;
        public float MinValue;
        public float MaxValue;

        public SliderToggleable(float value = 0)
        {
            this.Value = value;
            this.Enabled = false;
            this.Label = String.Empty;
            this.MinValue = 0;
            this.MaxValue = 0;
        }

        public SliderToggleable(string label, float minValue, float maxValue)
        {
            this.Value = 0;
            this.Enabled = false;
            this.Label = label;
            this.MinValue = minValue;
            this.MaxValue = maxValue;
        }

        public SliderToggleable(string label, float value, float minValue, float maxValue) : this(label, minValue, maxValue)
        {
            this.Value = value;
            this.Enabled = false;
        }

        public SliderToggleable(string label, float value, bool enabled, float minValue, float maxValue) : this(label, value, minValue, maxValue)
        {
            this.Enabled = enabled;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SliderToggleable))]
    internal class SliderToggleableDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var valueField = property.FindPropertyRelative("Value");

            var toggleField = property.FindPropertyRelative("Enabled");

            var labelValue = property.FindPropertyRelative("Label").stringValue;

            // Get the range of the slider.

            var minValue = property.FindPropertyRelative("MinValue").floatValue;
            var maxValue = property.FindPropertyRelative("MaxValue").floatValue;

            GUI.enabled = toggleField.boolValue;

            const float toggleWidth = 20f;
            const float spacing = 5f;

            var r1 = position;

            r1.width -= (toggleWidth);

            float newValue = 0;

            if (!string.IsNullOrEmpty(labelValue))
            {
                string toolTip = string.Empty;

                var tt = AttributeExtension.GetAttribute<TooltipAttribute>(fieldInfo);

                if (tt != null) toolTip = tt.tooltip;

                var labelContent = new GUIContent(labelValue, toolTip);

                // Draw the slider.
                newValue = EditorGUI.Slider(r1, labelContent, valueField.floatValue, minValue, maxValue);
            }
            else
            {
                newValue = EditorGUI.FloatField(r1, valueField.floatValue);
            }

            if (valueField.floatValue != newValue)
            {
                valueField.floatValue = newValue;
            }

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
