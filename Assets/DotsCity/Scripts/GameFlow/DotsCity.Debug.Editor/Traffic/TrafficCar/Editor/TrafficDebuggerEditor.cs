#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using System.Text;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    [CustomEditor(typeof(TrafficDebugger))]
    public class TrafficDebuggerEditor : Editor
    {
        private static StringBuilder sb = new StringBuilder();
        private TrafficDebugger trafficDebugger;

        private void OnEnable()
        {
            trafficDebugger = target as TrafficDebugger;
            trafficDebugger.OnInspectorEnabled();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            InspectorExtension.DrawDefaultHeaderScript(target);

            var enableDebugProp = serializedObject.FindProperty("enableDebug");

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(enableDebugProp);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                trafficDebugger.EnabledStateChanged();
                trafficDebugger.DebuggerChanged();
            }

            if (enableDebugProp.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("debuggerType"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("textColor"));

                if (trafficDebugger.CustomDebugger == null)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("showObstacleInfo"));
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("showCommonInfo"));
            }

            if (trafficDebugger.CustomDebugger != null)
            {
                var showListProp = serializedObject.FindProperty("showList");

                EditorGUILayout.PropertyField(showListProp);

                if (showListProp.boolValue)
                {
                    EditorGUILayout.BeginVertical("HelpBox");
                    trafficDebugger.CustomDebugger.DrawInspector();
                    EditorGUILayout.EndVertical();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
        static void DrawHandles(TrafficDebugger trafficDebugger, GizmoType gizmoType)
        {
            if (!Application.isPlaying || !trafficDebugger.EnableDebug)
                return;

            var traffics = trafficDebugger.Traffics;

            if (!traffics.IsCreated)
                return;

            var debugger = trafficDebugger.Debugger;
            bool custom = trafficDebugger.CustomDebugger != null;

            for (int i = 0; i < traffics.Length; i++)
            {
                if (!CameraExtension.InViewOfSceneView(traffics[i].Position))
                {
                    continue;
                }

                var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

                if (!entityManager.HasComponent<TrafficTag>(traffics[i].Entity))
                {
                    continue;
                }

                if (!custom)
                {
                    if (trafficDebugger.ShowObstacleInfo)
                    {
                        Color color = !traffics[i].HasObstacle ? Color.green : Color.red;

                        if (!UnityMathematicsExtension.DrawSceneViewRotatedCube(traffics[i].Position, traffics[i].Rotation, traffics[i].Bounds, color))
                        {
                            continue;
                        }
                    }
                }
                else
                {
                    trafficDebugger.CustomDebugger.DrawSceneView(traffics[i].Entity);
                }

                sb.Clear();

                if (trafficDebugger.ShowCommonInfo)
                {
                    sb.Append("Entity Index: ");
                    int index = traffics[i].Entity.Index;
                    sb.Append(index.ToString());
                    sb.Append("\n");
                }

                if (debugger != null)
                {
                    string text = debugger.Tick(traffics[i].Entity);

                    if (text != null)
                    {
                        sb.Append(text);
                    }
                }

                EditorExtension.DrawWorldString(sb.ToString(), traffics[i].Position, trafficDebugger.TextColor);
            }
        }
    }
}
#endif
