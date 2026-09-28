using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public static class LightStateDrawer
    {
        public static ReorderableList DrawList(string propertyName, SerializedObject serializedObject, List<LightStateInfo> sourceLightStates, GenericMenu.MenuFunction2 addItemCallback, string headerName = "", ReorderableList.RemoveCallbackDelegate removeCallback = null)
        {
            var property = serializedObject.FindProperty(propertyName);

            return DrawList(property, serializedObject, sourceLightStates, addItemCallback, headerName, removeCallback);
        }

        public static ReorderableList DrawList(SerializedProperty serializedProperty, SerializedObject serializedObject, List<LightStateInfo> sourceLightStates, GenericMenu.MenuFunction2 addItemCallback, string headerName = "", ReorderableList.RemoveCallbackDelegate removeCallback = null)
        {
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var lineHeightSpace = lineHeight + 10;

            var reordableList = new ReorderableList(serializedObject, serializedProperty, true, true, true, true);

            reordableList.drawHeaderCallback = (Rect rect) =>
            {
                string localHeaderName = string.IsNullOrEmpty(headerName) ? "Light States" : headerName;
                EditorGUI.LabelField(rect, new GUIContent(localHeaderName));
            };

            reordableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty element = reordableList.serializedProperty.GetArrayElementAtIndex(index);

                EditorGUI.PropertyField(new Rect(40, rect.y, 80, lineHeight), element);

                serializedObject.ApplyModifiedProperties();
            };

            reordableList.onAddDropdownCallback = (Rect rect, ReorderableList list) =>
            {
                GenericMenu dropdownMenu = new GenericMenu();

                var lightStates = Enum.GetValues(typeof(LightState)).Cast<LightState>().ToList();

                for (int i = 0; i < lightStates.Count; i++)
                {
                    var lightStateAddData = new LightStateAddData()
                    {
                        ReorderableList = reordableList,
                        LightState = lightStates[i]
                    };

                    dropdownMenu.AddItem(new GUIContent(lightStates[i].ToString()), false, addItemCallback, lightStateAddData);
                }

                dropdownMenu.ShowAsContext();
            };

            if (removeCallback != null)
            {
                reordableList.onRemoveCallback += removeCallback;
            }
            else
            {
                reordableList.onRemoveCallback = (ReorderableList list) =>
                {
                    int i = reordableList.index;
                    sourceLightStates.RemoveAt(i);
                };
            }

            return reordableList;
        }
    }
}