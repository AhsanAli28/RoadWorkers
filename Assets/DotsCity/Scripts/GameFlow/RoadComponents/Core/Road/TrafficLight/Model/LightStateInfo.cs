using UnityEditor;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    [System.Serializable]
    public struct LightStateInfo
    {
        public LightState LightState;
        public float Duration;
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(LightStateInfo))]
    public class LightStateInfoDrawer : PropertyDrawer
    {
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Calculate rects
            var amountLabelRect = new Rect(position.x, position.y, 60, position.height);
            var amountRect = new Rect(position.x + 60, position.y, 50, position.height);
            var stateLabelRect = new Rect(position.x + 120, position.y, 50, position.height);
            var stateRect = new Rect(position.x + 165, position.y, 100, position.height);

            // Draw fields - passs GUIContent.none to each so they are drawn without labels
            EditorGUI.LabelField(amountLabelRect, "Duration");
            EditorGUI.PropertyField(amountRect, property.FindPropertyRelative("Duration"), GUIContent.none);
            EditorGUI.LabelField(stateLabelRect, "State");
            EditorGUI.PropertyField(stateRect, property.FindPropertyRelative("LightState"), GUIContent.none);

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }
#endif
}
