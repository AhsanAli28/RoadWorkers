using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;
#endif

namespace Spirit604.DotsCity.Core.Authoring
{
    public class CullStateListSourceAttribute : PropertyAttribute
    {
        public string SourceListName;

        public CullStateListSourceAttribute(string listName)
        {
            SourceListName = listName;
        }
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(CullStateListSourceAttribute))]
    public class CullStateListSourceAttributeDrawer : PropertyDrawer
    {
        private List<CullState> values = null;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = attribute as CullStateListSourceAttribute;

            var listProp = property.serializedObject.FindProperty(attr.SourceListName);
            var stateList = (CullStateList)listProp.enumValueIndex;

            if (values == null)
            {
                values = Enum.GetValues(typeof(CullState)).Cast<CullState>().ToList();
            }

            var sourceIndex = property.enumValueIndex - 1;

            if (sourceIndex < 0)
            {
                sourceIndex = values.Count - 1;
            }

            var sourceValue = values[sourceIndex];

            Func<Enum, bool> popupFunc = (enumValue) =>
            {
                return CullComponentsExtension.IsAvailable((CullState)enumValue, stateList);
            };

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.BeginChangeCheck();

            var newValue = (CullState)EditorGUI.EnumPopup(position, label, sourceValue, popupFunc, false);

            if (EditorGUI.EndChangeCheck())
            {
                property.enumValueIndex = (int)newValue + 1;
            }

            EditorGUI.EndProperty();
        }
    }
#endif
}