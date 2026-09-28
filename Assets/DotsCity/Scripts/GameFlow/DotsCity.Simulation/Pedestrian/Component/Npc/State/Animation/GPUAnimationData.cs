using Spirit604.AnimationBaker;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [Serializable]
    public class GPUAnimationData
    {
        public string AnimationGUID;
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(GPUAnimationData))]
    public class GPUAnimationDataDrawer : PropertyDrawer
    {
        private static CrowdSkinFactory crowdSkinFactory;
        private static List<AnimationCollectionContainer.AnimationData> animations;
        private static string[] animationGuids;
        private static string[] animationNames;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (crowdSkinFactory == null)
            {
                crowdSkinFactory = ObjectUtils.FindObjectOfType<CrowdSkinFactory>();

                if (crowdSkinFactory)
                {
                    animations = crowdSkinFactory.AnimationCollectionContainer.GetAnimations();
                    animationGuids = animations.Select(a => a.Guid).ToArray();
                    animationNames = animations.Select(a => a.Name).ToArray();
                }
            }

            EditorGUI.BeginProperty(position, label, property);

            var r1 = position;
            r1.height = GetHeight();

            AnimationCollectionContainer container = null;

            var defaultWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100;

            if (crowdSkinFactory)
                container = crowdSkinFactory.AnimationCollectionContainer;

            var animationGUIDProp = property.FindPropertyRelative("AnimationGUID");

            if (container == null)
            {
                EditorGUI.PropertyField(r1, animationGUIDProp);
            }
            else
            {
                var sourceIndex = Array.IndexOf(animationGuids, animationGUIDProp.stringValue);

                var newIndex = EditorGUI.Popup(r1, "Animation", sourceIndex, animationNames);

                if (sourceIndex != newIndex)
                {
                    animationGUIDProp.stringValue = animationGuids[newIndex];
                }
            }

            EditorGUIUtility.labelWidth = defaultWidth;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int fieldCount = 1;

            if (crowdSkinFactory != null)
            {
                //fieldCount++;
            }

            return fieldCount * GetRow();
        }

        private float GetRow() => GetHeight() + 2;
        private float GetHeight() => EditorGUIUtility.singleLineHeight;
    }
#endif
}