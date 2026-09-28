#if UNITY_EDITOR
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.TrafficPublic.Authoring
{
    [CustomEditor(typeof(TrafficRoute))]
    public class TrafficRouteEditor : Editor
    {
        private Path[] paths;
        private GUIStyle guiStyle;
        private GUIStyle labelGuiStyle;
        private ReorderableList reordableList;

        protected virtual void OnEnable()
        {
            paths = ObjectUtils.FindObjectsOfType<Path>();

            var trafficRoute = target as TrafficRoute;

            var lineHeight = EditorGUIUtility.singleLineHeight;

            reordableList = new ReorderableList(serializedObject, serializedObject.FindProperty("routes"), true, true, true, true);

            reordableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, new GUIContent("Routes"));
            };

            reordableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty element = reordableList.serializedProperty.GetArrayElementAtIndex(index);

                EditorGUI.PropertyField(new Rect(40, rect.y, rect.width, lineHeight), element);

                serializedObject.ApplyModifiedProperties();
            };

            reordableList.onRemoveCallback = (ReorderableList list) =>
            {
                int i = reordableList.index;
                trafficRoute.RemovePath(trafficRoute.Routes[i]);
                serializedObject.ApplyModifiedProperties();
            };

            reordableList.onReorderCallback = (ReorderableList list) =>
            {
            };

            guiStyle = new GUIStyle("button");
            guiStyle.fontSize = 24;
            guiStyle.normal.textColor = Color.black;

            labelGuiStyle = new GUIStyle();
            labelGuiStyle.fontSize = 24;
            labelGuiStyle.normal.textColor = Color.white;
        }

        protected virtual void OnDisable()
        {
            var trafficRoute = target as TrafficRoute;

            DisableHightLight(trafficRoute);
        }

        public override void OnInspectorGUI()
        {
            var trafficRoute = target as TrafficRoute;

            serializedObject.Update();

            DrawDefaultInspector(trafficRoute);

            serializedObject.ApplyModifiedProperties();
        }

        protected void DrawDefaultInspector(TrafficRoute trafficRoute)
        {
            DrawTransitionSettings();
            DrawVisualSceneSettings();
            DrawRouteData(trafficRoute);
            DrawButtons(trafficRoute);
        }

        protected void DrawTransitionSettings()
        {
            InspectorExtension.DrawDefaultInspectorGroupBlock("Transition Settings", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("targetOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("distanceBeetweenParallelNodes"));
            }, serializedObject.FindProperty("transitionSettingsFoldout"));
        }

        protected void DrawVisualSceneSettings()
        {
            InspectorExtension.DrawDefaultInspectorGroupBlock("Scene Settings", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hightlightRoute"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("showPathSelectionButtons"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("showSwapButtons"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("showOnlyRelatedNodes"));
            }, serializedObject.FindProperty("sceneSettingsFoldout"));
        }

        protected void DrawRouteData(TrafficRoute trafficRoute)
        {
            InspectorExtension.DrawDefaultInspectorGroupBlock("Route Data", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("trafficNodeRouteData"));

                EditorGUILayout.PropertyField(serializedObject.FindProperty("routeChangeLaneTransitions"));

                trafficRoute.routesFoldout = EditorGUILayout.Foldout(trafficRoute.routesFoldout, "Routes");

                if (trafficRoute.routesFoldout)
                {
                    reordableList.DoLayoutList();
                }
            }, serializedObject.FindProperty("routeDataFoldout"));
        }

        protected void DrawButtons(TrafficRoute trafficRoute)
        {
            if (GUILayout.Button("Update Transitions"))
            {
                trafficRoute.UpdateTransitions();
            }

            if (GUILayout.Button("Clear Route"))
            {
                trafficRoute.ClearRoute();
            }

            if (GUILayout.Button("Refresh Related Nodes"))
            {
                trafficRoute.RefreshRelatedNodes();
            }
        }

        protected virtual void OnSceneGUI()
        {
            var trafficRoute = target as TrafficRoute;

            if (trafficRoute.showPathSelectionButtons)
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    if (paths[i].WayPoints.Count <= 1)
                    {
                        continue;
                    }

                    bool shouldShow = trafficRoute.ShouldShowPath(paths[i]);

                    if (!shouldShow)
                    {
                        continue;
                    }

                    var worldPosition = GetMiddlePosition(paths[i]);

                    var guiPosition = HandleUtility.WorldToGUIPoint(worldPosition);
                    Rect rect = new Rect(guiPosition, new Vector2(100, 100));

                    Handles.BeginGUI();
                    GUILayout.BeginArea(rect);
                    GUILayout.BeginHorizontal();

                    int width = 50;

                    bool contains = trafficRoute.Routes.Contains(paths[i]);

                    if (!contains)
                    {
                        if (GUILayout.Button("+", guiStyle, GUILayout.Width(width)))
                        {
                            trafficRoute.AddPath(paths[i]);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("-", guiStyle, GUILayout.Width(width)))
                        {
                            trafficRoute.RemovePath(paths[i]);
                        }
                    }

                    GUILayout.EndHorizontal();
                    GUILayout.EndArea();
                    Handles.EndGUI();
                }
            }

            if (trafficRoute.showSwapButtons)
            {
                for (int i = 0; i < trafficRoute.RouteChangeLaneTransitions.Count; i++)
                {
                    var transition = trafficRoute.RouteChangeLaneTransitions[i];

                    if (transition.SourcePath && transition.TargetPath)
                    {
                        var point1 = GetMiddlePosition(transition.SourcePath);
                        var point2 = GetMiddlePosition(transition.TargetPath);

                        var point = (point1 + point2) / 2;

                        Action callback = () => trafficRoute.SwapTransition(transition);

                        EditorExtension.DrawButton("<->", point, 50f, callback);
                    }
                }
            }

            if (trafficRoute.hightlightRoute)
            {
                for (int i = 0; i < trafficRoute.Routes?.Count; i++)
                {
                    var path = trafficRoute.Routes[i];
                    var labelPosition = Vector3.zero;

                    var transition = trafficRoute.GetTransition(path);

                    if (transition != null)
                    {
                        labelPosition = trafficRoute.GetSourceTransitionPoint(transition, path);
                    }
                    else
                    {
                        labelPosition = GetMiddlePosition(trafficRoute.Routes[i]);
                    }

                    var labelText = (i).ToString();
                    Handles.Label(labelPosition, labelText, labelGuiStyle);
                    trafficRoute.Routes[i].Highlighted = true;
                }

                for (int i = 0; i < trafficRoute.RouteChangeLaneTransitions?.Count; i++)
                {
                    var transition = trafficRoute.RouteChangeLaneTransitions[i];

                    var sourcePath = transition.SourcePath;
                    var targetPath = transition.TargetPath;

                    var pathLength1 = sourcePath.GetPathLength();
                    var pathLength2 = targetPath.GetPathLength();

                    var normLength1 = trafficRoute.RouteChangeLaneTransitions[i].SourceOffset / pathLength1;
                    sourcePath.HightlightNormalizedLength = normLength1;

                    var normLength2 = trafficRoute.RouteChangeLaneTransitions[i].TargetOffset / pathLength2;
                    targetPath.HightlightNormalizedLength = normLength2 - 1;

                    var point1 = trafficRoute.GetSourceTransitionPoint(sourcePath, true);
                    var point2 = trafficRoute.GetSourceTransitionPoint(targetPath, false);

                    DebugLine.DrawThickLine(point1, point2, 1f, Color.green);
                }
            }
            else
            {
                DisableHightLight(trafficRoute);
            }
        }

        private void DisableHightLight(TrafficRoute trafficRoute)
        {
            for (int i = 0; i < trafficRoute.Routes?.Count; i++)
            {
                trafficRoute.Routes[i].Highlighted = false;
                trafficRoute.Routes[i].HightlightNormalizedLength = 1f;
            }
        }

        private Vector3 GetMiddlePosition(Path path)
        {
            var worldPosition = path.GetMiddlePosition();

            return worldPosition;
        }
    }
}
#endif
