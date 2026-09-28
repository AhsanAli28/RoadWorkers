using Spirit604.Extensions;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    [CustomEditor(typeof(Roundabout))]
    public class RoundaboutEditor : Editor
    {
        private const string Roundabout_Entrance = "Roundabout_Entrance";
        private const string Roundabout_Inner = "Roundabout_Inner";
        private const string Roundabout_Common = "Roundabout_Common";
        private const string Roundabout_Nodes = "Roundabout_Nodes";

        private Roundabout roundabout;
        private RoadSegmentCreator roadSegmentCreator;

        private void OnEnable()
        {
            roundabout = (Roundabout)target;
            roadSegmentCreator = roundabout.GetComponent<RoadSegmentCreator>();
        }

        public override void OnInspectorGUI()
        {
            // DrawInspector(serializedObject, roundabout, roadSegmentCreator);
        }

        public static void DrawInspector(SerializedObject serializedObject, Roundabout roundabout, RoadSegmentCreator creator)
        {
            if (creator == null)
            {
                EditorGUILayout.HelpBox("RoadSegmentCreator not found", MessageType.Error);
                return;
            }

            serializedObject.Update();
            var creatorSo = new SerializedObject(creator);

            creatorSo.Update();

            bool changed = false;

            InspectorExtension.DrawGroupBox("Entrance", () =>
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(roundabout.Type)));
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(roundabout.EntranceNodeCount)));

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    roundabout.UpdateStepOffset();
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(roundabout.StepOffset)));

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(roundabout.OuterRadius)));
                EditorGUILayout.PropertyField(creatorSo.FindProperty(nameof(creator.laneCount)), new GUIContent("Entrance Lane Count"));
                EditorGUILayout.PropertyField(creatorSo.FindProperty(nameof(creator.customLaneWidth)), new GUIContent("Entrance Lane Width"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(roundabout.EntranceSpeedLimit)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(roundabout.EntrancePathPriority)));

                if (EditorGUI.EndChangeCheck())
                    changed = true;

            }, Roundabout_Entrance);

            InspectorExtension.DrawGroupBox("Inner", () =>
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(roundabout.InnerRadius)));

                if (roundabout.Type == Roundabout.RoundaboutType.DoubleInnerNodes)
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(roundabout.InnerBaseOffset)));

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(roundabout.InnerLaneCount)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(roundabout.InnerLaneWidth)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(roundabout.InnerSpeedLimit)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(roundabout.InnerPathPriority)));

                if (EditorGUI.EndChangeCheck())
                    changed = true;

            }, Roundabout_Inner);

            InspectorExtension.DrawGroupBox("Common", () =>
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(roundabout.TurnCurveType)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(roundabout.BaseCircularOffset)));
                EditorGUILayout.PropertyField(creatorSo.FindProperty(nameof(creator.dividerWidth)));

                if (EditorGUI.EndChangeCheck())
                    changed = true;

            }, Roundabout_Common);

            InspectorExtension.DrawGroupBox("Nodes Data", () =>
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(roundabout.InnerNodes)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(roundabout.EntranceNodes)));

                if (EditorGUI.EndChangeCheck())
                    changed = true;

            }, Roundabout_Nodes);

            creatorSo.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();

            if (changed)
            {
                roundabout.Create();
            }
        }
    }
}