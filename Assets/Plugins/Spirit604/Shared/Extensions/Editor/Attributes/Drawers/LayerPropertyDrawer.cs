using System;
using UnityEditor;
using UnityEngine;

namespace Spirit604.Attributes.Editor
{
    [CustomPropertyDrawer(typeof(LayerAttribute))]
    public class LayerPropertyDrawer : PropertyDrawer
    {
        private const string TypeWarningMessage = "{0} must be an int or a string";

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            switch (property.propertyType)
            {
                case SerializedPropertyType.String:
                    DrawPropertyForString(rect, property, label, GetLayers());
                    break;
                case SerializedPropertyType.Integer:
                    DrawPropertyForInt(rect, property, label, GetLayers());
                    break;
                default:
                    string message = string.Format(TypeWarningMessage, property.name);
                    Debug.LogWarning(message);
                    break;
            }

            EditorGUI.EndProperty();
        }

        private string[] GetLayers()
        {
            return UnityEditorInternal.InternalEditorUtility.layers;
        }

        private static void DrawPropertyForString(Rect rect, SerializedProperty property, GUIContent label, string[] layers)
        {
            int index = IndexOf(layers, property.stringValue);
            int newIndex = EditorGUI.Popup(rect, label.text, index, layers);
            string newLayer = layers[newIndex];

            if (!property.stringValue.Equals(newLayer, StringComparison.Ordinal))
            {
                property.stringValue = layers[newIndex];
            }
        }

        private static void DrawPropertyForInt(Rect rect, SerializedProperty property, GUIContent label, string[] layers)
        {
            int index = 0;
            string layerName = LayerMask.LayerToName(property.intValue);
            for (int i = 0; i < layers.Length; i++)
            {
                if (layerName.Equals(layers[i], StringComparison.Ordinal))
                {
                    index = i;
                    break;
                }
            }

            int newIndex = EditorGUI.Popup(rect, label.text, index, layers);
            string newLayerName = layers[newIndex];
            int newLayerNumber = LayerMask.NameToLayer(newLayerName);

            if (property.intValue != newLayerNumber)
            {
                property.intValue = newLayerNumber;
            }
        }

        private static int IndexOf(string[] layers, string layer)
        {
            var index = Array.IndexOf(layers, layer);
            return Mathf.Clamp(index, 0, layers.Length - 1);
        }
    }
}
