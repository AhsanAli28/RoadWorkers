using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Spirit604.Attributes.Editor
{
    public static class FieldUtility
    {
        public static bool TryToDrawHelpbox(SerializedProperty property, bool expandable = false)
        {
            var target = PropertyUtility.GetTargetObjectWithProperty(property);
            return TryToDrawHelpbox(target, property, expandable);
        }

        public static bool TryToDrawHelpbox(object target, SerializedProperty property, bool expandable = false)
        {
            var helpBoxAttr = AttributeExtensionEditor.GetAttribute<HelpboxAttribute>(property);

            if (helpBoxAttr == null) return false;

            var isEnabled = IsEnabled(target, helpBoxAttr.Condition);

            if (!isEnabled) return false;

            if (string.IsNullOrEmpty(helpBoxAttr.Url))
            {
                EditorGUILayout.HelpBox(helpBoxAttr.Text, (MessageType)(int)helpBoxAttr.MessageType);
            }
            else
            {
                var style = new GUIStyle(EditorStyles.helpBox);
                style.richText = true;

                var minHeightLayout = GUILayout.MinHeight((EditorGUIUtility.singleLineHeight) * 2);

                if (GUILayout.Button(helpBoxAttr.Text + $" <a href={helpBoxAttr.Url}>{helpBoxAttr.Url}</a>", style, minHeightLayout))
                {
                    Application.OpenURL(helpBoxAttr.Url);
                }

                var r = GUILayoutUtility.GetLastRect();

                if (expandable)
                {
                    r.x += r.width - 37f;
                    r.y -= 5;
                    r.width = 35f;
                    r.height = 35f;
                }
                else
                {
                    r.x += r.width - 26f;
                    r.y += 2;
                    r.width = 25f;
                    r.height = 25f;
                }

                GUIContent content = null;

                switch (helpBoxAttr.MessageType)
                {
                    case MessageBoxType.Info:
                        content = EditorGUIUtility.IconContent("console.infoicon");
                        break;
                    case MessageBoxType.Warning:
                        content = EditorGUIUtility.IconContent("console.warnicon");
                        break;
                    case MessageBoxType.Error:
                        content = EditorGUIUtility.IconContent("console.erroricon");
                        break;
                }

                if (content != null)
                {
                    EditorGUI.LabelField(r, content);
                }
            }

            return true;
        }

        public static bool IsEnabled(SerializedProperty property)
        {
            var target = PropertyUtility.GetTargetObjectWithProperty(property);
            return IsEnabled(target, property);
        }

        public static bool IsEnabled(object target, SerializedProperty property)
        {
            var readOnlyAttr = AttributeExtensionEditor.GetAttribute<ReadOnlyAttribute>(property);

            if (readOnlyAttr != null)
                return false;

            var enableAttr = AttributeExtensionEditor.GetAttribute<EnableFieldBaseAttribute>(property);

            if (enableAttr != null)
            {
                return IsEnabled(target, enableAttr);
            }

            return true;
        }

        public static bool IsEnabled(object target, MethodInfo method)
        {
            var readOnlyAttr = AttributeExtension.GetAttribute<ReadOnlyAttribute>(method);

            if (readOnlyAttr != null)
                return false;

            var enableAttr = AttributeExtension.GetAttribute<EnableFieldBaseAttribute>(method);

            if (enableAttr != null)
            {
                return IsEnabled(target, enableAttr);
            }

            return true;
        }

        public static bool IsEnabled(SerializedProperty property, EnableFieldBaseAttribute attr)
        {
            var target = PropertyUtility.GetTargetObjectWithProperty(property);

            return IsEnabled(target, attr);
        }

        public static bool IsEnabled(object target, EnableFieldBaseAttribute attr)
        {
            return IsEnabled(target, attr.Condition, attr.Inverted);
        }

        public static bool IsEnabled(object target, string condition, bool inverted = false)
        {
            var show = GetValue<bool>(target, condition);

            if (inverted)
                show = !show;

            return show;
        }

        public static bool IsVisible(SerializedProperty property)
        {
            var target = PropertyUtility.GetTargetObjectWithProperty(property);
            return IsVisible(target, property);
        }

        public static bool IsVisible(object target, SerializedProperty property)
        {
            var visibleAttr = AttributeExtensionEditor.GetAttribute<VisibleFieldBaseAttribute>(property);

            if (visibleAttr != null)
            {
                return IsVisible(target, visibleAttr);
            }

            var showIfNullAttribute = AttributeExtensionEditor.GetAttribute<ShowIfNullAttribute>(property);

            if (showIfNullAttribute != null)
            {
                return property.objectReferenceValue == null;
            }

            return true;
        }

        public static bool IsVisible(object target, MethodInfo method)
        {
            var visibleAttr = AttributeExtension.GetAttribute<VisibleFieldBaseAttribute>(method);

            if (visibleAttr != null)
            {
                return IsVisible(target, visibleAttr);
            }

            return true;
        }

        public static bool IsVisible(SerializedProperty property, VisibleFieldBaseAttribute attr)
        {
            var target = PropertyUtility.GetTargetObjectWithProperty(property);

            return IsVisible(target, attr);
        }

        public static bool IsVisible(object target, VisibleFieldBaseAttribute attr)
        {
            var show = GetValue<bool>(target, attr.Condition);

            if (attr.Inverted)
                show = !show;

            return show;
        }

        public static void IsChanged(SerializedProperty property)
        {
            var target = PropertyUtility.GetTargetObjectWithProperty(property);
            IsChanged(target, property);
        }

        private static T GetValue<T>(object target, string fieldName)
        {
            var type = target.GetType();

            while (type != null)
            {
                var conditionProperty = type.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (conditionProperty != null)
                {
                    return (T)conditionProperty.GetValue(target, null);
                }

                var conditionField = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (conditionField != null)
                {
                    return (T)conditionField.GetValue(target);
                }

                type = type.BaseType;
            }

            return default(T);
        }

        private static void IsChanged(object target, SerializedProperty property)
        {
            var onChangeAttr = AttributeExtensionEditor.GetAttribute<OnValueChangedAttribute>(property);

            if (onChangeAttr != null)
            {
                var method = target.GetType().GetMethod(onChangeAttr.Condition, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                method.Invoke(target, null);
            }
        }
    }
}