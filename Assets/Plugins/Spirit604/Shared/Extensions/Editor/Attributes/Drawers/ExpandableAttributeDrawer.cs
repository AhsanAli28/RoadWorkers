using UnityEditor;
using UnityEngine;

namespace Spirit604.Attributes.Editor
{
    [CustomPropertyDrawer(typeof(ExpandableAttribute), true)]
    public sealed class ExpandableAttributeDrawer : PropertyDrawer
    {
        private UnityEditor.Editor editor = null;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Draw label
            EditorGUI.PropertyField(position, property, label, true);

            // Draw foldout arrow
            if (property.objectReferenceValue != null)
            {
                property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, GUIContent.none);
            }

            // Draw foldout properties
            if (property.isExpanded)
            {
                if (!editor)
                {
                    UnityEditor.Editor.CreateCachedEditor(property.objectReferenceValue, null, ref editor);
                }

                if (editor)
                {
                    EditorGUI.indentLevel++;

                    var obj = editor.serializedObject;

                    DrawProps(obj);

                    EditorGUI.indentLevel--;
                }
            }
        }

        private static bool DrawProps(SerializedObject obj)
        {
            EditorGUI.BeginChangeCheck();
            obj.UpdateIfRequiredOrScript();
            SerializedProperty iterator = obj.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                {
                    if (iterator.name == "m_Script")
                        continue;

                    if (FieldUtility.IsVisible(iterator))
                    {
                        GUI.enabled = FieldUtility.IsEnabled(iterator);

                        EditorGUI.BeginChangeCheck();

                        EditorGUILayout.PropertyField(iterator, true);

                        if (EditorGUI.EndChangeCheck())
                        {
                            obj.ApplyModifiedProperties();
                            FieldUtility.IsChanged(iterator);
                        }

                        FieldUtility.TryToDrawHelpbox(iterator, true);

                        GUI.enabled = true;
                    }
                }

                enterChildren = false;
            }

            obj.ApplyModifiedProperties();
            return EditorGUI.EndChangeCheck();
        }
    }
}