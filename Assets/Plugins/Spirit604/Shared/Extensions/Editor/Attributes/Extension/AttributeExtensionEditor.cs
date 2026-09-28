using Spirit604.Attributes.Editor;
using System;
using System.Reflection;
using UnityEditor;

namespace Spirit604.Attributes
{
    public static class AttributeExtensionEditor
    {
        public static T GetAttribute<T>(SerializedProperty serializedProperty) where T : Attribute
        {
            FieldInfo field = GetField(serializedProperty);

            return AttributeExtension.GetAttribute<T>(field);
        }

        public static T[] GetAttributes<T>(SerializedProperty property) where T : class
        {
            FieldInfo fieldInfo = GetField(property);

            if (fieldInfo == null)
            {
                return new T[] { };
            }

            return (T[])fieldInfo.GetCustomAttributes(typeof(T), true);
        }

        private static FieldInfo GetField(SerializedProperty serializedProperty)
        {
            var target = PropertyUtility.GetTargetObjectWithProperty(serializedProperty);

            if (target == null)
                return null;

            var type = target.GetType();

            while (type != null)
            {
                var field = type.GetField(serializedProperty.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            return null;
        }
    }
}
