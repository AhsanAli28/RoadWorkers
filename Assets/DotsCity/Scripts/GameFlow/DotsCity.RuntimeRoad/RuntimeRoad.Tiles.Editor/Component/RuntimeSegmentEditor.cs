using Spirit604.Attributes;
using Spirit604.Extensions;
using System;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [CustomEditor(typeof(RuntimeSegment))]
    public class RuntimeSegmentEditor : SharedSettingsEditorBase<RuntimeSegmentEditor.EditorSettings>
    {
        [Serializable]
        public class EditorSettings
        {
            public bool SettingsFlag = true;
            public bool BakedFlag;
            public bool Utils;
            public bool Buttons = true;
        }

        private RuntimeSegment runtimeSegment;

        protected override string SaveKey => "RuntimeSegmentEditor";

        protected override void OnEnable()
        {
            base.OnEnable();
            runtimeSegment = target as RuntimeSegment;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            InspectorExtension.DrawGroupBox("Settings", () =>
            {
                DocumentationLinkerUtils.ShowButton("https://dotstrafficcity.readthedocs.io/en/latest/runtimeRoad.html#tile-prefab", -17);

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(runtimeSegment.addOnAwake)));

            }, ref SharedSettings.SettingsFlag);

            InspectorExtension.DrawGroupBox("Baked Data", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(runtimeSegment.bakedData)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(runtimeSegment.crossroadData)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(runtimeSegment.allConnectedPaths)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(runtimeSegment.casterTrafficNodes)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(runtimeSegment.pedestrianNodes)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(runtimeSegment.paths)));

                if (!runtimeSegment.IsChunk)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(runtimeSegment.connectionEdges)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(runtimeSegment.nodeCasters)));
                }

            }, ref SharedSettings.BakedFlag);

#if DOTSCITY_DEV
            if (runtimeSegment.gameObject.scene != null)
            {
                InspectorExtension.DrawDefaultInspectorGroupBlock("Utils", () =>
                {
                    GUI.enabled = Application.isPlaying;

                    if (!runtimeSegment.Placed)
                    {
                        if (GUILayout.Button("Place Segment"))
                        {
                            runtimeSegment.PlaceSegment();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Remove Segment"))
                        {
                            runtimeSegment.RemoveSegment();
                        }
                    }

                    GUI.enabled = true;

                }, ref SharedSettings.Utils);
            }
#endif

            InspectorExtension.DrawGroupBox("Buttons", () =>
            {
                GUI.enabled = !Application.isPlaying;

                if (GUILayout.Button("Bake"))
                {
                    runtimeSegment.Bake();
                }

                GUI.enabled = true;

            }, ref SharedSettings.Buttons);

            serializedObject.ApplyModifiedProperties();
        }

        protected override EditorSettings GetDefaultSettings() => new EditorSettings();
    }
}