using System;
using UnityEditor;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    [Serializable]
    public class TrafficGroupMask : ICloneable
    {
        public enum MaskType { Default, Allowed, Forbidden, CustomGroup }

        public MaskType TrafficMaskGroup = MaskType.Default;
        public TrafficGroupType TrafficGroupAllowed = (TrafficGroupType)~0;
        public TrafficGroupType TrafficGroupForbidden;
        public int GroupIndex;

        public TrafficGroupType GetValue()
        {
            switch (TrafficMaskGroup)
            {
                case MaskType.Default:
                    return TrafficGroupMaskSettings.GetDefault();
                case MaskType.Allowed:
                    return TrafficGroupAllowed;
                case MaskType.Forbidden:
                    {
                        return (TrafficGroupType)(~0 & ~(int)TrafficGroupForbidden);
                    }
                case MaskType.CustomGroup:
                    {
                        return TrafficGroupMaskSettings.GetCustomGroup(GroupIndex);
                    }
            }

            return default;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public TrafficGroupMask GetClone() => Clone() as TrafficGroupMask;
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(TrafficGroupMask), true)]
    public class TrafficGroupMaskDrawer : PropertyDrawer
    {
        private const string DefaultText = "Default allowed group of the <b>TrafficGroupType</b> that defined in the <b>TrafficGroupMaskSettings</b>.";
        private const string AllowedText = "User-selected <b>TrafficGroupType</b> types that traffic can spawn & pass through the path.";
        private const string ForbiddenText = "User-selected <b>TrafficGroupType</b> types that are forbidden for traffic spawning & driving (not selected in the list is allowed).";
        private const string CustomText = "Custom allowed group of <b>TrafficGroupType</b> that defined in the <b>TrafficGroupMaskSettings</b>.";
        private const string TotalText =
            "<b>Default.</b> " + DefaultText + "\r\n\r\n" +
            "<b>Allowed.</b> " + AllowedText + "\r\n\r\n" +
            "<b>Forbidden.</b> " + ForbiddenText + "\r\n\r\n" +
            "<b>Custom.</b> " + CustomText;

        private const int spacing = 1;

        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var rect1 = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var rect2 = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + spacing * 2, position.width, EditorGUIUtility.singleLineHeight);

            var trafficMaskGroupProp = property.FindPropertyRelative("TrafficMaskGroup");
            var trafficTypeAllowedProp = property.FindPropertyRelative("TrafficGroupAllowed");
            var trafficTypeForbiddenProp = property.FindPropertyRelative("TrafficGroupForbidden");
            var groupIndexProp = property.FindPropertyRelative("GroupIndex");

            var trafficMaskGroup = (TrafficGroupMask.MaskType)trafficMaskGroupProp.enumValueIndex;

            EditorGUI.PropertyField(rect1, trafficMaskGroupProp, new GUIContent("Traffic Mask Group", TotalText));

            EditorGUI.indentLevel++;

            switch (trafficMaskGroup)
            {
                case TrafficGroupMask.MaskType.Allowed:
                    EditorGUI.PropertyField(rect2, trafficTypeAllowedProp, new GUIContent("Traffic Group", AllowedText));
                    break;
                case TrafficGroupMask.MaskType.Forbidden:
                    EditorGUI.PropertyField(rect2, trafficTypeForbiddenProp, new GUIContent("Traffic Group Forbidden", ForbiddenText));
                    break;
                case TrafficGroupMask.MaskType.CustomGroup:
                    {
                        if (TrafficGroupMaskSettings.GroupCount > 0)
                        {
                            groupIndexProp.intValue = EditorGUI.Popup(rect2, "Custom Group Name", groupIndexProp.intValue, TrafficGroupMaskSettings.GroupHeaders);
                        }
                        else
                        {
                            GUI.enabled = false;
                            EditorGUI.Popup(rect2, "Custom Group Name", 0, new string[1]);
                            GUI.enabled = true;
                        }

                        rect2.y += GetFieldHeight();

                        GUI.enabled = TrafficGroupMaskSettings.GroupCount > 0;

                        var flags = TrafficGroupMaskSettings.GetCustomGroup(groupIndexProp.intValue);

                        var newFlags = (TrafficGroupType)EditorGUI.EnumFlagsField(rect2, new GUIContent("Flags"), flags);

                        if (flags != newFlags)
                        {
                            TrafficGroupMaskSettings.SetCustomGroup(groupIndexProp.intValue, newFlags);
                        }

                        GUI.enabled = true;

                        break;
                    }
            }

            EditorGUI.indentLevel--;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var trafficMaskGroupProp = property.FindPropertyRelative("TrafficMaskGroup");

            var trafficMaskGroup = (TrafficGroupMask.MaskType)trafficMaskGroupProp.enumValueIndex;

            var fieldCount = 1;

            switch (trafficMaskGroup)
            {
                case TrafficGroupMask.MaskType.Allowed:
                    fieldCount = 2;
                    break;
                case TrafficGroupMask.MaskType.Forbidden:
                    fieldCount = 2;
                    break;
                case TrafficGroupMask.MaskType.CustomGroup:
                    fieldCount = 3;
                    break;
            }

            return GetFieldHeight() * fieldCount;
        }

        private static float GetFieldHeight()
        {
            return (EditorGUIUtility.singleLineHeight + spacing);
        }
    }
#endif
}