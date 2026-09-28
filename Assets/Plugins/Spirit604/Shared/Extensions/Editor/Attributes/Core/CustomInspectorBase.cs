using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Spirit604.Attributes.Editor
{
    [CanEditMultipleObjects]
#if !ODIN_INSPECTOR && !CUSTOM_INSPECTOR
    [CustomEditor(typeof(UnityEngine.Object), true)]
#endif
    public class CustomInspectorBase : UnityEditor.Editor
    {
        class PropDataBase
        {
            public Func<bool> IsEnabledFunc;
            public Func<bool> IsVisibleFunc;

            public bool IsEnabled => IsEnabledFunc != null ? IsEnabledFunc() : true;
            public bool IsVisible => IsVisibleFunc != null ? IsVisibleFunc() : true;
        }

        class PropData : PropDataBase
        {
            public MethodInfo OnChange;
        }

        class ButtonData : PropDataBase
        {
            public string Label;
            public MethodInfo Method;
        }

        private Dictionary<string, PropData> customPropData = new Dictionary<string, PropData>();
        private List<MethodInfo> onInspectorEnableMethods = new List<MethodInfo>();
        private List<MethodInfo> onInspectorDisableMethods = new List<MethodInfo>();
        private List<ButtonData> buttons = new List<ButtonData>();
        private static HashSet<int> inspectorIds = new HashSet<int>();

        protected virtual void OnEnable()
        {
            Init();

            if (inspectorIds.Contains(target.GetInstanceID()))
                return;

            inspectorIds.Add(target.GetInstanceID());
            OnInspectorEnabled();
        }

        protected virtual void OnDisable()
        {
            if (!inspectorIds.Contains(target.GetInstanceID()))
                return;

            inspectorIds.Remove(target.GetInstanceID());
            OnInspectorDisabled();
        }

        public override void OnInspectorGUI()
        {
            if (customPropData.Count == 0)
            {
                DrawDefaultInspector();
            }
            else
            {
                DrawCustomInspector();
            }

            DrawButtons();
        }

        protected void DrawCustomInspector()
        {
            serializedObject.Update();

            using (var property = serializedObject.GetIterator())
            {
                if (property.NextVisible(true))
                {
                    do
                    {
                        if (property.name.Equals("m_Script", System.StringComparison.Ordinal))
                        {
                            using (new EditorGUI.DisabledScope(disabled: true))
                            {
                                EditorGUILayout.PropertyField(property);
                            }
                        }
                        else
                        {
                            DrawProp(property);
                        }
                    }
                    while (property.NextVisible(false));
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected void DrawButtons()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                if (!buttons[i].IsVisible)
                    continue;

                using (new EditorGUI.DisabledScope(disabled: !buttons[i].IsEnabled))
                {
                    if (GUILayout.Button(buttons[i].Label))
                    {
                        var method = buttons[i].Method;
                        InvokeMethod(method);
                    }
                }
            }
        }

        protected void Init()
        {
            try { if (serializedObject == null) return; }
            catch { return; }

            InitProps();
            InitButtons();
            InitInspectorMethods();
        }

        private void InitProps()
        {
            customPropData.Clear();

            using (var iterator = serializedObject.GetIterator())
            {
                if (iterator.NextVisible(true))
                {
                    do
                    {
                        var prop = serializedObject.FindProperty(iterator.name);

                        if (prop == null) continue;

                        var hasCustomAttrs = AttributeExtensionEditor.GetAttributes<ICustomAttribute>(prop).Any();

                        if (hasCustomAttrs)
                        {
                            var data = new PropData();

                            InitData(data, prop);

                            var onChangeAttr = AttributeExtensionEditor.GetAttribute<OnValueChangedAttribute>(prop);

                            if (onChangeAttr != null)
                            {
                                data.OnChange = target.GetType().GetMethod(onChangeAttr.Condition, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            }

                            customPropData.Add(prop.name, data);
                        }
                    }
                    while (iterator.NextVisible(false));
                }
            }
        }

        private void DrawProp(SerializedProperty property)
        {
            if (!customPropData.ContainsKey(property.name))
            {
                DrawPropField(property);
            }
            else
            {
                var propData = customPropData[property.name];

                if (!propData.IsVisible)
                    return;

                using (new EditorGUI.DisabledScope(disabled: !propData.IsEnabled))
                {
                    EditorGUI.BeginChangeCheck();

                    UnityEngine.Object prevObjectRef = null;

                    var refProp = property.propertyType == SerializedPropertyType.ObjectReference;

                    if (refProp)
                    {
                        prevObjectRef = property.objectReferenceValue;
                    }

                    DrawPropField(property);

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (propData.OnChange != null)
                        {
                            serializedObject.ApplyModifiedProperties();

                            if (!refProp || refProp && property.objectReferenceValue != prevObjectRef)
                                InvokeMethod(propData.OnChange);
                        }
                    }
                }
            }
        }

        private void DrawPropField(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                // Weird popup bug fix for LayerMask
                case SerializedPropertyType.LayerMask:
                    EditorGUILayout.PropertyField(property.serializedObject.FindProperty(property.name));
                    break;
                default:
                    EditorGUILayout.PropertyField(property);
                    break;
            }

            FieldUtility.TryToDrawHelpbox(property);
        }

        private void InvokeMethod(MethodInfo method)
        {
            var parameters = method.GetParameters();
            object[] args = new object[parameters.Length];

            for (int i = 0; i < args.Length; i++)
            {
                args[i] = parameters[i].DefaultValue;
            }

            method.Invoke(target, args);
        }

        private void InitButtons()
        {
            buttons.Clear();

            if (target == null)
            {
                Debug.LogError("The target object is null. Check for missing scripts.");
                return;
            }

            IEnumerable<MethodInfo> methodInfos = GetAllMethods<ButtonAttribute>();

            foreach (var method in methodInfos)
            {
                var btnAttr = AttributeExtension.GetAttribute<ButtonAttribute>(method);

                var label = !string.IsNullOrEmpty(btnAttr.Name) ? btnAttr.Name : ObjectNames.NicifyVariableName(method.Name);

                var button = new ButtonData()
                {
                    Method = method,
                    Label = label,
                };

                InitData(button, method);

                buttons.Add(button);
            }
        }

        private void InitInspectorMethods()
        {
            onInspectorEnableMethods = GetAllMethods<OnInspectorEnableAttribute>().ToList();
            onInspectorDisableMethods = GetAllMethods<OnInspectorDisableAttribute>().ToList();
        }

        private IEnumerable<MethodInfo> GetAllMethods<T>() where T : Attribute
        {
            return ReflectionUtility.GetAllMethods(target, a => a.GetCustomAttributes<T>().Count() > 0);
        }

        private void InitData(PropDataBase data, SerializedProperty prop)
        {
            data.IsVisibleFunc = () => FieldUtility.IsVisible(target, prop);
            data.IsEnabledFunc = () => FieldUtility.IsEnabled(target, prop);
        }

        private void InitData(PropDataBase data, MethodInfo method)
        {
            data.IsVisibleFunc = () => FieldUtility.IsVisible(target, method);
            data.IsEnabledFunc = () => FieldUtility.IsEnabled(target, method);
        }

        private void OnInspectorEnabled()
        {
            foreach (var method in onInspectorEnableMethods)
            {
                InvokeMethod(method);
            }
        }

        private void OnInspectorDisabled()
        {
            foreach (var method in onInspectorDisableMethods)
            {
                InvokeMethod(method);
            }
        }
    }
}
