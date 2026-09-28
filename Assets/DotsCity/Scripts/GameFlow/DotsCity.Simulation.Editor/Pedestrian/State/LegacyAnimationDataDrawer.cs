using System;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [CustomPropertyDrawer(typeof(LegacyAnimationData))]
    public class LegacyAnimationDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var defaultWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100;

            var r1 = position;
            r1.height = GetHeight();

            EditorGUI.PropertyField(r1, property.FindPropertyRelative("StateName"));

            r1.y += GetRow();

            EditorGUI.PropertyField(r1, property.FindPropertyRelative("StateLayer"));

            var paramTypeProp1 = property.FindPropertyRelative("ParamType1");
            var paramTypeProp2 = property.FindPropertyRelative("ParamType2");

            var paramNameProp1 = property.FindPropertyRelative("ParamName1");
            var paramNameProp2 = property.FindPropertyRelative("ParamName2");

            var valueProp1 = property.FindPropertyRelative("Value1");
            var valueProp2 = property.FindPropertyRelative("Value2");

            DrawProp(ref r1, property, paramTypeProp1, "ParamName1", valueProp1, () =>
            {
                if (paramTypeProp1.enumValueIndex == 0)
                {
                    valueProp1.floatValue = valueProp2.floatValue;
                    paramTypeProp1.enumValueIndex = paramTypeProp2.enumValueIndex;
                    paramNameProp1.stringValue = paramNameProp2.stringValue;

                    valueProp2.floatValue = 0;
                    paramTypeProp2.enumValueIndex = 0;
                    paramNameProp2.stringValue = string.Empty;
                }
            });

            if (paramTypeProp1.enumValueIndex != 0)
                DrawProp(ref r1, property, paramTypeProp2, "ParamName2", valueProp2);


            DrawProp(ref r1, property, property.FindPropertyRelative("ExitParamType"), "ExitParamName", property.FindPropertyRelative("ExitValue"));

            EditorGUIUtility.labelWidth = defaultWidth;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int fieldCount = 4;

            var paramTypeProp1 = property.FindPropertyRelative("ParamType1");

            if (paramTypeProp1.enumValueIndex != 0)
            {
                fieldCount += 3;

                var paramTypeProp2 = property.FindPropertyRelative("ParamType2");

                if (paramTypeProp2.enumValueIndex != 0)
                {
                    fieldCount += 2;
                }
            }

            var pxitParamTypeProp = property.FindPropertyRelative("ExitParamType");

            if (pxitParamTypeProp.enumValueIndex != 0)
            {
                fieldCount += 2;
            }

            return fieldCount * GetRow();
        }

        private float GetRow() => GetHeight() + 2;
        private float GetHeight() => EditorGUIUtility.singleLineHeight;

        private void DrawProp(ref Rect r1, SerializedProperty property, SerializedProperty paramTypeProp, string paramNameName, SerializedProperty valueProp, Action onChange = null)
        {
            EditorGUI.BeginChangeCheck();

            r1.y += GetRow();

            EditorGUI.PropertyField(r1, paramTypeProp);

            if (EditorGUI.EndChangeCheck())
            {
                paramTypeProp.serializedObject.ApplyModifiedProperties();
                valueProp.floatValue = 0;
                onChange?.Invoke();
            }

            var propType = (AnimParamType)paramTypeProp.enumValueIndex;

            if (propType != AnimParamType.None)
            {
                r1.y += GetRow();
                EditorGUI.PropertyField(r1, property.FindPropertyRelative(paramNameName));
                r1.y += GetRow();
            }

            switch (propType)
            {
                case AnimParamType.Bool:
                    var isToggled = valueProp.floatValue == 1;
                    var newToggled = EditorGUI.Toggle(r1, "Value", isToggled);

                    if (isToggled != newToggled)
                    {
                        valueProp.floatValue = newToggled ? 1 : 0;
                    }

                    break;
                case AnimParamType.Float:
                    valueProp.floatValue = EditorGUI.FloatField(r1, "Value", valueProp.floatValue);
                    break;
                case AnimParamType.Int:
                    valueProp.floatValue = (float)EditorGUI.IntField(r1, "Value", (int)valueProp.floatValue);
                    break;
            }
        }
    }
}