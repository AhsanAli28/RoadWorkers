#if UNITY_EDITOR
using Spirit604.Extensions;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Binding
{
    [CustomPropertyDrawer(typeof(EntityWeakRef))]
    class EntityWeakRefDrawer : PropertyDrawer
    {
        private Dictionary<string, EntityWeakRef> cache = new Dictionary<string, EntityWeakRef>();

        private bool Selected(EntityWeakRef targetProperty) => EntityRefEditorBinder.SelectedEntityRef != null && EntityRefEditorBinder.SelectedEntityRef == targetProperty;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var targetProperty = GetProperty(property, label);

            EditorGUI.BeginProperty(position, label, property);

            var idField = property.FindPropertyRelative("Id");

            var r1 = position;
            r1.height = GetRowHeight() - 2;

            var idR1 = r1;
            var idR2 = r1;

            idR1.width -= 30f;

            idR2.x += idR1.width + 3f;
            idR2.width = 25f;

            GUI.enabled = false;

            string text = $"{label} ";

            if (Application.isPlaying)
            {
                text += $"[Entity:{targetProperty.EntityIndex}]";
            }
            else
            {
                text += $"[EntityRef ID]";
            }

            var newValue = EditorGUI.IntField(idR1, text, idField.intValue);

            GUI.enabled = true;

            if (idField.intValue != newValue)
            {
                idField.intValue = newValue;
            }

            if (!Selected(targetProperty))
            {
                if (GUI.Button(idR2, "+"))
                {
                    EntityRefEditorBinder.SelectedEntityRef = targetProperty;
                    EntityRefEditorBinder.SourceObject = property.serializedObject.targetObject;
                }
            }
            else
            {
                if (GUI.Button(idR2, "-"))
                {
                    EntityRefEditorBinder.SelectedEntityRef = null;
                    EntityRefEditorBinder.SourceObject = null;
                }
            }

            if (Selected(targetProperty))
            {
                r1.y += GetRowHeight();

                var newEntityBindingType = (EntityRefEditorBinder.SceneEntityBindingType)EditorGUI.EnumPopup(r1, "Entity Binding Type", EntityRefEditorBinder.CurrentSceneEntityBindingType);

                if (EntityRefEditorBinder.CurrentSceneEntityBindingType != newEntityBindingType)
                {
                    EntityRefEditorBinder.CurrentSceneEntityBindingType = newEntityBindingType;
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int fieldCount = 1;

            var targetProperty = GetProperty(property, label);

            if (Selected(targetProperty))
            {
                fieldCount += 1;
            }

            return (GetRowHeight()) * fieldCount;
        }

        private float GetRowHeight() => EditorGUIUtility.singleLineHeight + 2;

        private EntityWeakRef GetProperty(SerializedProperty property, GUIContent label)
        {
            var labelText = label.text;

            if (cache.ContainsKey(labelText))
            {
                return cache[labelText];
            }
            else
            {
                var prop = PropertyExtension.GetPropertyInstance<EntityWeakRef>(property);

                cache.Add(labelText, prop);

                return prop;
            }
        }
    }
}
#endif
