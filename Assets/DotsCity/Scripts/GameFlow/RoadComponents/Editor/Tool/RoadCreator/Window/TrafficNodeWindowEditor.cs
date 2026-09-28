#if UNITY_EDITOR
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public class TrafficNodeWindowEditor : EditorWindowBase
    {
        private RoadSegmentCreator roadSegmentCreator;
        public List<TrafficNode> trafficNodes;

        private GUIStyle trafficNodeHeaderGuiStyle;
        private Vector3 defaultCrossWalkOffset;
        private Vector2 scrollPosition;
        private TrafficNode destroyTrafficNode;

        //[MenuItem("Spirit604/TrafficNode Window")]
        public static TrafficNodeWindowEditor ShowWindow()
        {
            TrafficNodeWindowEditor trafficNodeWindow = (TrafficNodeWindowEditor)GetWindow(typeof(TrafficNodeWindowEditor));
            trafficNodeWindow.titleContent = new GUIContent("TrafficNode Window");

            return trafficNodeWindow;
        }

        protected override Vector2 GetDefaultWindowSize()
        {
            return new Vector2(600, 500);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            trafficNodeHeaderGuiStyle = new GUIStyle();
            trafficNodeHeaderGuiStyle.fontStyle = FontStyle.Bold;
            trafficNodeHeaderGuiStyle.fontSize = 16;
            trafficNodeHeaderGuiStyle.normal.textColor = EditorStyles.label.normal.textColor;

            Undo.undoRedoPerformed += Undo_undoRedoPerformed;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Undo.undoRedoPerformed -= Undo_undoRedoPerformed;
        }

        private void OnGUI()
        {
            SerializedObject so = new SerializedObject(this);
            SerializedProperty stringsProperty = so.FindProperty(nameof(trafficNodes));

            EditorGUILayout.PropertyField(stringsProperty, true);
            EditorGUILayout.Separator();

            if (trafficNodes != null)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                for (int index = 0; index < trafficNodes.Count; index++)
                {
                    if (trafficNodes[index] == null)
                    {
                        continue;
                    }

                    var trafficNode = this.trafficNodes[index];

                    DrawTrafficNodeHeader(trafficNode, index);
                    DrawNodeSettings(trafficNode, index);
                    DrawLaneData(trafficNode);

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(20f);

                if (GUILayout.Button("Set Settings", GUILayout.Height(30f)))
                {
                    UpdateSettings();
                }
            }

            if (destroyTrafficNode)
            {
                Undo.RecordObject(this, "TrafficNode Destroy");
                this.trafficNodes.Remove(destroyTrafficNode);
                roadSegmentCreator.DestroyNode(destroyTrafficNode, true, true);
                destroyTrafficNode = null;
            }
        }

        private void DrawTrafficNodeHeader(TrafficNode trafficNode, int index)
        {
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("TrafficNode " + (index + 1).ToString(), trafficNodeHeaderGuiStyle);

            if (GUILayout.Button("x", GUILayout.Width(25)))
            {
                destroyTrafficNode = trafficNode;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawNodeSettings(TrafficNode trafficNode, int index)
        {
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            var laneCountType = (LaneCountType)EditorGUILayout.EnumPopup("Lane Count Type", trafficNode.LaneCountType);

            var laneCustom = laneCountType == LaneCountType.Custom;
            var laneCount = EditorGUILayout.IntSlider("Lane Count", trafficNode.LaneCount, 1, 10);
            var externalLaneCount = 0;

            if (laneCustom)
            {
                externalLaneCount = EditorGUILayout.IntSlider("External Lane Count", trafficNode.ExternalLaneCount, 1, 10);
            }

            var laneOffset = EditorGUILayout.Slider("Lane Width", trafficNode.LaneWidth, 0f, 20f);
            var dividerWidth = EditorGUILayout.Slider("Divider Width", trafficNode.DividerWidth, 0f, 20f);

            var trafficNodeType = (TrafficNodeType)EditorGUILayout.EnumPopup("Traffic Node Type", trafficNode.TrafficNodeType);

            var hasPedestrianNodes = EditorGUILayout.Toggle("Has Pedestrian Nodes", roadSegmentCreator.GetCustomPedestrianNodeEnabledState(index));

            GUI.enabled = hasPedestrianNodes;

            var hasCrosswalk = EditorGUILayout.Toggle("Has Crosswalk", trafficNode.HasCrosswalk);

            GUI.enabled = true;

            Vector3 crossWalkOffset = default;
            float width = 0;
            float height = 0;
            NodeShapeType pedestrianNodeShapeType = default;
            PedestrianNode crosswalkNode = null;

            var crosswalk = trafficNode.TrafficNodeCrosswalk;

            if (crosswalk)
            {
                crossWalkOffset = EditorGUILayout.Vector3Field("Crosswalk Offset", crosswalk.CrossWalkOffset);

                crosswalkNode = crosswalk.GetCrosswalkNode(0);

                if (crosswalkNode != null)
                {
                    pedestrianNodeShapeType = (NodeShapeType)EditorGUILayout.EnumPopup("Crosswalk Node Shape", crosswalkNode.PedestrianNodeShapeType);

                    width = EditorGUILayout.Slider("Crosswalk Width", crosswalkNode.MaxPathWidth, 0.1f, 10f);

                    if (pedestrianNodeShapeType == NodeShapeType.Rectangle)
                    {
                        height = EditorGUILayout.Slider("Crosswalk Node Height", crosswalkNode.Height, 0.1f, 10f);
                    }
                }
            }

            var isOneWay = EditorGUILayout.Toggle("Is One Way", trafficNode.IsOneWay);

            var isEndOfOneWay = false;

            if (trafficNode.IsOneWay)
            {
                isEndOfOneWay = EditorGUILayout.Toggle("Is End Of OneWay", trafficNode.IsEndOfOneWay);
            }

            var lockPathAutoCreation = EditorGUILayout.Toggle("Lock Path Auto Creation", trafficNode.LockPathAutoCreation);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(trafficNode, "Undo Traffic Node Settings");

                if (crosswalk)
                    Undo.RecordObject(crosswalk, "Undo Croswalk Offset");

                trafficNode.LaneCountType = laneCountType;
                trafficNode.LaneCount = laneCount;

                if (laneCustom)
                {
                    trafficNode.ExternalLaneCount = externalLaneCount;
                }

                trafficNode.LaneWidth = laneOffset;
                trafficNode.TrafficNodeType = trafficNodeType;
                roadSegmentCreator.SetPedestrianEnabledState(index, hasPedestrianNodes);
                trafficNode.HasCrosswalk = hasCrosswalk;
                trafficNode.IsOneWay = isOneWay;
                trafficNode.DividerWidth = !isOneWay ? dividerWidth : 0;

                if (crosswalk)
                {
                    crosswalk.CrossWalkOffset = crossWalkOffset;
                    crosswalk.SwitchEnabledState(hasPedestrianNodes);
                    crosswalk.SetCrosswalkPosition(trafficNode, true);

                    if (crosswalkNode != null)
                    {
                        crosswalk.SetType(pedestrianNodeShapeType, true);
                        crosswalk.SetCustomWidth(width, height);
                    }
                }

                if (trafficNode.IsOneWay)
                {
                    trafficNode.IsEndOfOneWay = isEndOfOneWay;
                }

                trafficNode.LockPathAutoCreation = lockPathAutoCreation;

                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

                EditorSaver.SetObjectDirty(trafficNode);
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawLaneData(TrafficNode trafficNode)
        {
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUILayout.LabelField("Lanes", EditorStyles.boldLabel);

            for (int laneIndex = 0; laneIndex < trafficNode.Lanes?.Count; laneIndex++)
            {
                for (int j = 0; j < trafficNode.Lanes[laneIndex].paths.Count; j++)
                {
                    var path = trafficNode.Lanes[laneIndex].paths[j];

                    if (path == null) continue;

                    EditorGUILayout.BeginHorizontal();

                    StringBuilder sb = new StringBuilder();
                    sb.Append("Lane: ");
                    sb.Append(laneIndex.ToString());
                    sb.Append(" Connected Lane: ");
                    sb.Append(path.ConnectedLaneIndex.ToString());
                    sb.Append(" Connected: ");

                    switch (path.PathConnectionType)
                    {
                        case PathConnectionType.TrafficNode:
                            {
                                if (path.ConnectedTrafficNode)
                                {
                                    sb.Append($"'{path.ConnectedTrafficNode.gameObject.name}' node");
                                }
                                else
                                {
                                    sb.Append("ConnectedTrafficNode is null");
                                }

                                break;
                            }
                        case PathConnectionType.PathPoint:
                            {
                                if (path.ConnectedPath)
                                {
                                    sb.Append($"'{path.ConnectedPath.gameObject.name}' path");
                                }
                                else
                                {
                                    sb.Append("ConnectedPath is null");
                                }

                                break;
                            }
                    }

                    EditorGUILayout.LabelField(sb.ToString());

                    if (GUILayout.Button("Select"))
                    {
                        SelectPath(path);
                    }

                    if (GUILayout.Button("x", GUILayout.Width(25)))
                    {
                        DestroyPath(trafficNode, path);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void SelectPath(Path path)
        {
            var pathEditor = PathSettingsWindowEditor.ShowWindow();
            pathEditor.Initialize(path);
            roadSegmentCreator.ForceSelectPath(path.SourceTrafficNode, path);
            SceneView.RepaintAll();
        }

        private void DestroyPath(TrafficNode trafficNode, Path path)
        {
            path.DestroyPath(true);
        }

        public void Initialize(RoadSegmentCreator roadSegmentCreator, TrafficNode[] trafficNodes, Vector3 defaultCrossWalkOffset)
        {
            this.roadSegmentCreator = roadSegmentCreator;
            this.trafficNodes = trafficNodes.ToList();
            this.defaultCrossWalkOffset = defaultCrossWalkOffset;
            Repaint();
        }

        private void UpdateSettings()
        {
            for (int i = 0; i < trafficNodes?.Count; i++)
            {
                var trafficNode = this.trafficNodes[i];

                if (trafficNode.TrafficNodeCrosswalk)
                    trafficNode.TrafficNodeCrosswalk.SwitchConnectionState(this.trafficNodes[i].HasCrosswalk, true);

                trafficNode.Resize(true);

                EditorSaver.SetObjectDirty(this.trafficNodes[i]);
            }

            roadSegmentCreator.UpdatePaths(true, true);

            EditorExtension.CollapseUndoCurrentOperations();
        }

        private void Undo_undoRedoPerformed()
        {
            Repaint();
        }
    }
}
#endif