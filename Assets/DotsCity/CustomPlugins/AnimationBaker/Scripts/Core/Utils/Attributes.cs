using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

[assembly: InternalsVisibleTo("604Spirit.AnimationBaker.Core")]

namespace Spirit604.AnimationBaker.Utils
{
    /// <summary>
    /// Read Only attribute.
    /// Attribute is use only to mark ReadOnly properties.
    /// </summary>
    internal class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
    /// <summary>
    /// This class contain custom drawer for ReadOnly attribute.
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Unity method for drawing GUI in Editor
        /// </summary>
        /// <param name="position">Position.</param>
        /// <param name="property">Property.</param>
        /// <param name="label">Label.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Saving previous GUI enabled value
            var previousGUIState = GUI.enabled;
            // Disabling edit for property
            GUI.enabled = false;
            // Drawing Property
            EditorGUI.PropertyField(position, property, label);
            // Setting old GUI enabled value
            GUI.enabled = previousGUIState;
        }
    }
#endif
}