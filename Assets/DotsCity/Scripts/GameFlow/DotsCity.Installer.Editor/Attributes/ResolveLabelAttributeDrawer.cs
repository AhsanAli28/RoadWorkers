#if UNITY_EDITOR && !ZENJECT
using Spirit604.DotsCity.Installer;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Initialization.Installer
{
    [CustomPropertyDrawer(typeof(ResolveLabelAttribute))]
    public class ResolveLabelAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = attribute as ResolveLabelAttribute;

            if (attr.Resolve)
                label.text += " (Resolve)";

            if (attr.Optional)
                label.text += " (Optional)";

            EditorGUI.PropertyField(position, property, label, true);
        }
    }
}
#endif
