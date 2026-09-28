using UnityEditor;
using UnityEngine;

namespace Spirit604.Attributes.Editor
{
    [CustomPropertyDrawer(typeof(DocLinkerAttribute))]
    public class DocLinkerAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = attribute as DocLinkerAttribute;

            DocumentationLinkerUtils.ShowButton(attr.Link, attr.Offset);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0;
        }
    }
}
