using UnityEditor;
using UnityEngine;

namespace Spirit604.Collections.Dictionary.Editor
{
    public static class EditorPropertyExtension
    {
        public static object GetPropertyValue(this SerializedProperty property)
        {
            if (property == null)
            {
                return null;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property.intValue;
                case SerializedPropertyType.Boolean:
                    return property.boolValue;
                case SerializedPropertyType.Float:
                    return property.floatValue;
                case SerializedPropertyType.String:
                    return property.stringValue;
                case SerializedPropertyType.Color:
                    return property.colorValue;
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue;
                case SerializedPropertyType.LayerMask:
                    return property.intValue;
                case SerializedPropertyType.Enum:
                    return property.enumValueIndex;
                case SerializedPropertyType.Vector2:
                    return property.vector2Value;
                case SerializedPropertyType.Vector3:
                    return property.vector3Value;
                case SerializedPropertyType.Vector4:
                    return property.vector4Value;
                case SerializedPropertyType.Rect:
                    return property.rectValue;
                case SerializedPropertyType.ArraySize:
                    return property.arraySize;
                case SerializedPropertyType.Character:
                    return property.stringValue;
                case SerializedPropertyType.AnimationCurve:
                    return property.animationCurveValue;
                case SerializedPropertyType.Bounds:
                    return property.boundsValue;
                case SerializedPropertyType.Quaternion:
                    return property.quaternionValue;
                case SerializedPropertyType.Vector2Int:
                    return property.vector2IntValue;
                case SerializedPropertyType.Vector3Int:
                    return property.vector3IntValue;
                case SerializedPropertyType.RectInt:
                    return property.rectIntValue;
                case SerializedPropertyType.BoundsInt:
                    return property.boundsIntValue;
                default:
                    break;
            }

            return null;
        }

        public static void SetPropertyValue(this SerializedProperty property, SerializedProperty targetProperty)
        {
            var value = targetProperty.GetPropertyValue();

            if (value == null)
            {
                SetDefaultPropertyValue(property);
                return;
            }

            SetPropertyValue(property, value);
        }

        public static void SetPropertyValue(this SerializedProperty property, object value)
        {
            if (value == null)
            {
                SetDefaultPropertyValue(property);
                return;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.intValue = (int)value;
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = (bool)value;
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = (float)value;
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = (string)value;
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = (Color)value;
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = (UnityEngine.Object)value;
                    break;
                case SerializedPropertyType.LayerMask:
                    property.intValue = (int)value;
                    break;
                case SerializedPropertyType.Enum:
                    property.enumValueIndex = (int)value;
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = (Vector2)value;
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = (Vector3)value;
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = (Vector4)value;
                    break;
                case SerializedPropertyType.Rect:
                    property.rectValue = (Rect)value;
                    break;
                case SerializedPropertyType.ArraySize:
                    property.arraySize = (int)value;
                    break;
                case SerializedPropertyType.Character:
                    property.stringValue = (string)value;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    property.animationCurveValue = (AnimationCurve)value;
                    break;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = (Bounds)value;
                    break;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = (Quaternion)value;
                    break;
                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = (Vector2Int)value;
                    break;
                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = (Vector3Int)value;
                    break;
                case SerializedPropertyType.RectInt:
                    property.rectIntValue = (RectInt)value;
                    break;
                case SerializedPropertyType.BoundsInt:
                    property.boundsIntValue = (BoundsInt)value;
                    break;
                default:
                    break;
            }
        }

        public static void SetDefaultPropertyValue(this SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.intValue = default;
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = default;
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = default;
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = default;
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = default;
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = null;
                    break;
                case SerializedPropertyType.LayerMask:
                    property.intValue = default;
                    break;
                case SerializedPropertyType.Enum:
                    property.enumValueIndex = default;
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = default;
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = default;
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = default;
                    break;
                case SerializedPropertyType.Rect:
                    property.rectValue = default;
                    break;
                case SerializedPropertyType.ArraySize:
                    property.arraySize = default;
                    break;
                case SerializedPropertyType.Character:
                    property.stringValue = default;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    property.animationCurveValue = default;
                    break;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = default;
                    break;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = default;
                    break;
                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = default;
                    break;
                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = default;
                    break;
                case SerializedPropertyType.RectInt:
                    property.rectIntValue = default;
                    break;
                case SerializedPropertyType.BoundsInt:
                    property.boundsIntValue = default;
                    break;
                default:
                    break;
            }
        }

        public static bool IsEqual(this SerializedProperty property, SerializedProperty targetProperty)
        {
            if (property.propertyType != targetProperty.propertyType)
            {
                return false;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property.intValue == targetProperty.intValue;
                case SerializedPropertyType.Boolean:
                    return property.boolValue == targetProperty.boolValue;
                case SerializedPropertyType.Float:
                    return property.floatValue == targetProperty.floatValue;
                case SerializedPropertyType.String:
                    return property.stringValue == targetProperty.stringValue;
                case SerializedPropertyType.Color:
                    return property.colorValue == targetProperty.colorValue;
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue == targetProperty.objectReferenceValue;
                case SerializedPropertyType.LayerMask:
                    return property.intValue == targetProperty.intValue;
                case SerializedPropertyType.Enum:
                    return property.enumValueIndex == targetProperty.enumValueIndex;
                case SerializedPropertyType.Vector2:
                    return property.vector2Value == targetProperty.vector2Value;
                case SerializedPropertyType.Vector3:
                    return property.vector3Value == targetProperty.vector3Value;
                case SerializedPropertyType.Vector4:
                    return property.vector4Value == targetProperty.vector4Value;
                case SerializedPropertyType.Rect:
                    return property.rectValue == targetProperty.rectValue;
                case SerializedPropertyType.ArraySize:
                    return property.arraySize == targetProperty.arraySize;
                case SerializedPropertyType.Character:
                    return property.stringValue == targetProperty.stringValue;
                case SerializedPropertyType.AnimationCurve:
                    return property.animationCurveValue == targetProperty.animationCurveValue;
                case SerializedPropertyType.Bounds:
                    return property.boundsValue == targetProperty.boundsValue;
                case SerializedPropertyType.Quaternion:
                    return property.quaternionValue == targetProperty.quaternionValue;
                case SerializedPropertyType.Vector2Int:
                    return property.vector2IntValue == targetProperty.vector2IntValue;
                case SerializedPropertyType.Vector3Int:
                    return property.vector3IntValue == targetProperty.vector3IntValue;
                case SerializedPropertyType.RectInt:
                    return property.rectIntValue.Equals(targetProperty.rectIntValue);
                case SerializedPropertyType.BoundsInt:
                    return property.boundsIntValue == targetProperty.boundsIntValue;
                default:
                    break;
            }

            return false;
        }

        public static bool Contains(this SerializedProperty arrayProperty, SerializedProperty targetProperty)
        {
            if (!arrayProperty.isArray)
            {
                return false;
            }

            for (int i = 0; i < arrayProperty.arraySize; i++)
            {
                var arrayElement = arrayProperty.GetArrayElementAtIndex(i);

                if (arrayElement.IsEqual(targetProperty))
                {
                    return true;
                }
            }

            return false;
        }
    }
}