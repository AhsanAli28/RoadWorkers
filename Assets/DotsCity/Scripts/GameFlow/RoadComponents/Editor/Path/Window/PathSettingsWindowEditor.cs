#if UNITY_EDITOR
using Spirit604.Attributes;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Spirit604.CityEditor.Road
{
    public class PathSettingsWindowEditor : EditorWindowBase
    {
        #region Constans

        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/path.html#advanced-settings-window";
        private const float MultipleButtonsSize = 40f;
        private const float ButtonSize = 30f;
        private const string SetSpeedLimitButtonText = "Set Speed Limit";

        private const float ArrowWidth = 25f;
        private const float ArrowHeight = 40f;
        private const float ArrowOffset = 115f;
        private const int ArrowFontSize = 25;

        private readonly Color selectWaypointColor = Color.green;
        private readonly List<string> buttonTexts = new List<string> { "S", "E" };

        #endregion

        #region Helper types

        private enum SpeedLimitChangeType { Single, Multiple, AllWay, CustomSection }

        private enum MultiNodeChangeType { Fixed, Interpolate }

        private enum InterpolateType { NodeIndex, NodeDistance }

        private enum PathSectionType { StartOfPath, EndOfPath, AllPath }

        private enum PathSectionCreateType { ClearPathNodes, UseExistNodes }

        #endregion

        #region Serialized variables

        [Tooltip("" +
            "<b>Single</b> - change each waypoint one by one\r\n\r\n" +
            "<b>Multiple</b> - speed limit will be changed on the selected section\r\n\r\n" +
            "<b>All way</b> - all path waypoints will change the speed limit according to the set options\r\n\r\n" +
            "<b>Custom section</b> - section with the custom speed will be automatically generated depending on the parameters")]
        [SerializeField] private SpeedLimitChangeType speedLimitChangeType = SpeedLimitChangeType.Multiple;

        [Tooltip("")]
        [SerializeField] private bool drawSelectButtons;

        [Tooltip("Displays additional settings for each waypoint (Backward Movement, traffic group)")]
        [SerializeField] private bool drawAdditionalSettings;

        [Tooltip("" +
             "<b>Fixed</b> - all waypoints change speed limit\r\n\r\n" +
             "<b>Interpolate</b> - speed will be interpolated from the beginning of the section to the end")]
        [SerializeField] private MultiNodeChangeType multiNodeChangeType;

        [Tooltip("" +
            "<b>Node index</b> : speed is interpolated relative to the waypoint index\r\n\r\n" +
            "<b>Distance</b> : speed is interpolated relative the position of the waypoint")]
        [SerializeField] private InterpolateType interpolateType;

        [Tooltip("Speed limit of all waypoints on the path")]
        [SerializeField][Range(0, 200f)] private float selectedPathSpeedLimit;

        [Tooltip("Initial speed limit of the section")]
        [SerializeField][Range(0, 100f)] private float startSpeedLimit;

        [Tooltip("End speed limit of the section")]
        [SerializeField][Range(0, 100f)] private float endSpeedLimit;

        [Tooltip("" +
            "<b>Start of path</b> : section will be created at the beginning of the path\r\n\r\n" +
            "<b>End of path</b> : section will be created at the end of the path\r\n\r\n" +
            "<b>All path</b> : section will be generated all along the path")]
        [SerializeField] private PathSectionType pathSectionType = PathSectionType.EndOfPath;

        [Tooltip("" +
            "<b>Clear path nodes</b> : waypoints will be generated anew each time a section is created\r\n\r\n" +
            "<b>Use exist nodes</b> : existing waypoints will be used for the section")]
        [SerializeField] private PathSectionCreateType pathSectionCreateType;

        [Tooltip("Length of the created section")]
        [SerializeField][Range(0, 100f)] private float sectionLength = 5f;

        [SerializeField][Range(0, 10f)] private float nearSectionNodeDistance = 0.0001f;

        [Tooltip("Number of waypoints of the created section")]
        [SerializeField][Range(2, 10)] private int sectionWaypoints = 2;

        [SerializeField] private bool commonSettingsFlag = true;
        [SerializeField] private bool customSettingsFlag = true;

        #endregion

        #region Variables

        private GUIStyle arrowButtonStyle;
        private Path selectedPath;
        private Object pathObject;

        private PathNode startPathNode;
        private PathNode endPathNode;
        private Vector2 scrollPosition;
        private bool showAssignError;

        public static Action OnSetSpeedClick = delegate { };

        #endregion

        #region Properties

        private bool DrawAdditionalNodeSettings => drawAdditionalSettings && speedLimitChangeType == SpeedLimitChangeType.Single;

        private bool CurveMessage => speedLimitChangeType == SpeedLimitChangeType.CustomSection && selectedPath.PathCurveType != PathCurveType.StraightLine && pathSectionCreateType == PathSectionCreateType.ClearPathNodes;

        private bool SectionSupported => !CurveMessage;

        private float NearSectionNodeDistance => pathSectionCreateType == PathSectionCreateType.UseExistNodes ? nearSectionNodeDistance : 0.0001f;

        #endregion

        #region Unity lifecycle

        //[MenuItem("Spirit604/Path Settings Window")]
        public static PathSettingsWindowEditor ShowWindow()
        {
            PathSettingsWindowEditor pathWindow = (PathSettingsWindowEditor)GetWindow(typeof(PathSettingsWindowEditor));
            pathWindow.titleContent = new GUIContent("Path Settings");

            return pathWindow;
        }

        protected override Vector2 GetDefaultWindowSize()
        {
            return new Vector2(350, 400);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            LoadData();

            arrowButtonStyle = new GUIStyle("button");
            arrowButtonStyle.fontSize = ArrowFontSize;

            Selection.selectionChanged += Selection_selectionChanged;
            SceneView.duringSceneGui += SceneView_duringSceneGui;
            Undo.undoRedoPerformed += Undo_undoRedoPerformed;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SaveData();
            UnselectPath();

            Selection.selectionChanged -= Selection_selectionChanged;
            SceneView.duringSceneGui -= SceneView_duringSceneGui;
            Undo.undoRedoPerformed -= Undo_undoRedoPerformed;
        }

        private void OnGUI()
        {
            var so = new SerializedObject(this);
            so.Update();

            DocumentationLinkerUtils.ShowButtonFirst(DocLink, -5);

            DrawSettings(so);

            if (selectedPath != null)
            {
                DrawScrollView();

                EditorGUILayout.Separator();

                if (GUILayout.Button("Select"))
                {
                    Selection.activeObject = pathObject;
                }

                if (GUILayout.Button("Recreate"))
                {
                    selectedPath?.RecreateAndSaveUndo();
                }
            }

            so.ApplyModifiedProperties();
        }

        #endregion

        #region Public methods

        public void Initialize(Path path)
        {
            UnselectPath();
            this.pathObject = path;
            SwitchHighlightState(path, true);
            Repaint();
        }

        #endregion

        #region Private methods

        private void DrawSettings(SerializedObject so)
        {
            Action drawSettingsCallback = () =>
            {
                this.selectedPath = EditorGUILayout.ObjectField("Selected Path", pathObject, typeof(Path), true) as Path;

                if (selectedPath != null)
                {
                    var pathSo = new SerializedObject(selectedPath);
                    pathSo.Update();

                    EditorGUI.BeginChangeCheck();

                    var pathCurveType = (PathCurveType)EditorGUILayout.EnumPopup("Path Curve Type", selectedPath.PathCurveType);
                    EditorGUILayout.PropertyField(pathSo.FindProperty("trafficGroupMask"));
                    var wayPointsCountPerCurve = EditorGUILayout.IntSlider("WayPoints Count Per Curve", selectedPath.WayPointsCountPerCurve, 2, 20);
                    var priority = EditorGUILayout.IntSlider("Priority", selectedPath.Priority, -5, 5);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(selectedPath, "Revert path settings");
                        selectedPath.PathCurveType = pathCurveType;
                        selectedPath.WayPointsCountPerCurve = wayPointsCountPerCurve;
                        selectedPath.Priority = priority;

                        pathSo.ApplyModifiedProperties();
                    }

                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.PropertyField(so.FindProperty(nameof(drawAdditionalSettings)));

                    if (EditorGUI.EndChangeCheck())
                    {
                        so.ApplyModifiedProperties();

                        if (selectedPath)
                        {
                            selectedPath.ShowAdditionalInfo = drawAdditionalSettings;
                        }

                        SceneView.RepaintAll();
                    }
                }
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Common Settings", drawSettingsCallback, so.FindProperty(nameof(commonSettingsFlag)));

            if (selectedPath == null)
            {
                return;
            }

            Action drawCustomSettingsCallback = () =>
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(speedLimitChangeType)));

                switch (speedLimitChangeType)
                {
                    case SpeedLimitChangeType.Single:
                        {
                            break;
                        }
                    case SpeedLimitChangeType.Multiple:
                        {
                            startPathNode = EditorGUILayout.ObjectField("Start Path Node", startPathNode, typeof(PathNode), true) as PathNode;
                            endPathNode = EditorGUILayout.ObjectField("End Path Node", endPathNode, typeof(PathNode), true) as PathNode;
                            EditorGUILayout.PropertyField(so.FindProperty(nameof(drawSelectButtons)));
                            DrawAdditionalMultipleSettings(so);
                            break;
                        }
                    case SpeedLimitChangeType.AllWay:
                        {
                            DrawAdditionalAllWaySettings(so);
                            break;
                        }
                    case SpeedLimitChangeType.CustomSection:
                        {
                            DrawCustomSectionSettings(so);
                            break;
                        }
                }
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Custom Settings", drawCustomSettingsCallback, so.FindProperty(nameof(customSettingsFlag)));
        }

        private void DrawScrollView()
        {
            EditorGUILayout.BeginVertical("GroupBox");

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < selectedPath.WayPoints.Count; i++)
            {
                var node = selectedPath.WayPoints[i];

                DrawNodeSettings(node, i);

                EditorGUILayout.Separator();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawNodeSettings(PathNode node, int index)
        {
            bool hasSelection = (node == startPathNode || node == endPathNode) && speedLimitChangeType == SpeedLimitChangeType.Multiple;

            if (hasSelection)
            {
                EditorGUILayout.BeginVertical("HelpBox");
            }

            EditorGUILayout.LabelField("Waypoint " + (index + 1).ToString(), EditorStyles.boldLabel);

            GUI.enabled = speedLimitChangeType == SpeedLimitChangeType.Single;

            EditorGUI.BeginChangeCheck();

            float newSpeedLimit = EditorGUILayout.Slider("SpeedLimit", node.SpeedLimit, 0, 200f);

            bool newBackwardMovement = node.BackwardDirection;
            bool newCustomGroup = node.CustomGroup;

            if (DrawAdditionalNodeSettings)
            {
                newBackwardMovement = EditorGUILayout.Toggle("Backward Direction", node.BackwardDirection);
                newCustomGroup = EditorGUILayout.Toggle("Custom Group", node.CustomGroup);

                if (node.CustomGroup)
                {
                    var pathNodeSo = new SerializedObject(node);
                    pathNodeSo.Update();

                    EditorGUILayout.PropertyField(pathNodeSo.FindProperty("trafficGroupMask"));

                    pathNodeSo.ApplyModifiedProperties();
                }
            }
            else if (drawAdditionalSettings)
            {
                EditorGUILayout.Toggle("Backward Direction", node.BackwardDirection);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(node, "Revert node settings");
                node.SpeedLimit = newSpeedLimit;
                node.BackwardDirection = newBackwardMovement;
                node.CustomGroup = newCustomGroup;
                EditorSaver.SetObjectDirty(node);
            }

            GUI.enabled = true;

            if (speedLimitChangeType == SpeedLimitChangeType.Multiple)
            {
                EditorGUILayout.BeginHorizontal();

                bool startSelected = node == startPathNode;
                bool endSelected = node == endPathNode;

                GUI.enabled = !startSelected;
                var startButtonText = !startSelected ? "Select Start" : "Selected Start";

                if (GUILayout.Button(startButtonText))
                {
                    SelectStartNode(node);
                }

                GUI.enabled = !endSelected;

                var endButtonText = !endSelected ? "Select End" : "Selected End";

                if (GUILayout.Button(endButtonText))
                {
                    SelectEndNode(node);
                }

                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
            }

            if (hasSelection)
            {
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawAdditionalMultipleSettings(SerializedObject so)
        {
            if (this.selectedPath == null)
            {
                return;
            }

            if (speedLimitChangeType == SpeedLimitChangeType.Multiple)
            {
                ShowMultipleNodeSpeedSettings(so);

                if (GUILayout.Button(SetSpeedLimitButtonText))
                {
                    if (startPathNode != null && endPathNode != null)
                    {
                        showAssignError = false;
                        var startIndex = selectedPath.WayPoints.IndexOf(startPathNode);
                        var endIndex = selectedPath.WayPoints.IndexOf(endPathNode);

                        if (startIndex > endIndex)
                        {
                            var temp = endIndex;
                            endIndex = startIndex;
                            startIndex = temp;
                        }

                        SetupSelectedIndexSpeedLimit(startIndex, endIndex);
                    }
                    else
                    {
                        showAssignError = true;
                    }
                }

                if (showAssignError)
                {
                    EditorGUILayout.HelpBox("Assign Nodes!", MessageType.Error);
                }
            }
        }

        private void SetupSelectedIndexSpeedLimit(int startIndex, int endIndex)
        {
            switch (multiNodeChangeType)
            {
                case MultiNodeChangeType.Fixed:
                    {
                        for (int i = startIndex; i <= endIndex; i++)
                        {
                            PathNode node = selectedPath.WayPoints[i];
                            Undo.RecordObject(node, "Revert speed limit");
                            node.SpeedLimit = selectedPathSpeedLimit;
                            EditorSaver.SetObjectDirty(node);
                        }

                        break;
                    }
                case MultiNodeChangeType.Interpolate:
                    {
                        float pathLength = 0;
                        int indexLength = 0;

                        switch (interpolateType)
                        {
                            case InterpolateType.NodeIndex:
                                {
                                    indexLength = endIndex - startIndex + 1;
                                    break;
                                }
                            case InterpolateType.NodeDistance:
                                {
                                    pathLength = selectedPath.GetPathLength(startIndex, endIndex);
                                    break;
                                }
                        }

                        int index = 0;

                        for (int i = startIndex; i <= endIndex; i++)
                        {
                            PathNode node = selectedPath.WayPoints[i];
                            Undo.RecordObject(node, "Revert speed limit");

                            float speedLimit = 0;

                            switch (interpolateType)
                            {
                                case InterpolateType.NodeIndex:
                                    {
                                        float t = (float)index / (indexLength - 1);
                                        speedLimit = RoundSpeed(Mathf.Lerp(startSpeedLimit, endSpeedLimit, t));
                                        break;
                                    }
                                case InterpolateType.NodeDistance:
                                    {
                                        float t = 0;

                                        if (i == endIndex)
                                        {
                                            t = 1;
                                        }
                                        else if (i > startIndex)
                                        {
                                            var distanceToNode = selectedPath.GetPathLength(startIndex, i);
                                            t = distanceToNode / pathLength;
                                        }

                                        speedLimit = RoundSpeed(Mathf.Lerp(startSpeedLimit, endSpeedLimit, t));
                                        break;
                                    }
                            }

                            node.SpeedLimit = speedLimit;
                            EditorSaver.SetObjectDirty(node);
                            index++;
                        }

                        break;
                    }
            }

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            OnSetSpeedClick();
        }

        private void DrawAdditionalAllWaySettings(SerializedObject so)
        {
            if (this.selectedPath == null)
            {
                return;
            }

            ShowMultipleNodeSpeedSettings(so);

            if (GUILayout.Button(SetSpeedLimitButtonText))
            {
                int startIndex = 0;
                int endIndex = selectedPath.WayPoints.Count - 1;

                if (multiNodeChangeType == MultiNodeChangeType.Fixed)
                {
                    Undo.RecordObject(selectedPath, "Changed Path Speed Limit");
                    selectedPath.PathSpeedLimit = selectedPathSpeedLimit;
                }

                SetupSelectedIndexSpeedLimit(startIndex, endIndex);
                EditorSaver.SetObjectDirty(selectedPath);
            }
        }

        private void ShowMultipleNodeSpeedSettings(SerializedObject so)
        {
            EditorGUILayout.PropertyField(so.FindProperty(nameof(multiNodeChangeType)));

            switch (multiNodeChangeType)
            {
                case MultiNodeChangeType.Fixed:
                    {
                        EditorGUILayout.PropertyField(so.FindProperty(nameof(selectedPathSpeedLimit)));
                        break;
                    }
                case MultiNodeChangeType.Interpolate:
                    {
                        EditorGUILayout.PropertyField(so.FindProperty(nameof(interpolateType)));
                        EditorGUILayout.PropertyField(so.FindProperty(nameof(startSpeedLimit)));
                        EditorGUILayout.PropertyField(so.FindProperty(nameof(endSpeedLimit)));
                        break;
                    }
            }
        }

        private void CreateCustomSpeedLimitSegment()
        {
            switch (pathSectionCreateType)
            {
                case PathSectionCreateType.ClearPathNodes:
                    {
                        selectedPath.ClearOnPathNodesAndWaypoints(true);
                        break;
                    }
                case PathSectionCreateType.UseExistNodes:
                    {
                        selectedPath.SaveMovementUndo();
                        break;
                    }
            }

            var wayPoints = selectedPath.WayPoints;
            float totalDistance = 0;
            int targetIndex = 0;
            int existSectionNodesCount = 1;
            int addNodeIndex = 0;
            var startLineIndex = 0;
            var endLineIndex = 0;

            var pathLength = selectedPath.GetPathLength();
            float currentSegmentDistance = 0;

            switch (pathSectionType)
            {
                case PathSectionType.StartOfPath:
                    {
                        currentSegmentDistance = MathF.Min(pathLength, sectionLength);
                        targetIndex = 1;

                        if (pathSectionCreateType != PathSectionCreateType.ClearPathNodes)
                        {
                            for (int i = 0; i < wayPoints.Count - 1; i++)
                            {
                                var wayPoint = wayPoints[i];
                                var nextWayPoint = wayPoints[i + 1];

                                float distance = Vector3.Distance(wayPoint.transform.position, nextWayPoint.transform.position);

                                totalDistance += distance;

                                if (totalDistance < sectionLength)
                                {
                                    targetIndex = i + 1;
                                    existSectionNodesCount++;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }

                        startLineIndex = 0;
                        endLineIndex = sectionWaypoints - 1;
                        addNodeIndex = startLineIndex + 1;
                        break;
                    }
                case PathSectionType.EndOfPath:
                    {
                        currentSegmentDistance = MathF.Min(pathLength, sectionLength);

                        targetIndex = wayPoints.Count - 1;

                        if (pathSectionCreateType != PathSectionCreateType.ClearPathNodes)
                        {
                            for (int i = wayPoints.Count - 1; i >= 1; i--)
                            {
                                var wayPoint = wayPoints[i];
                                var nextWayPoint = wayPoints[i - 1];

                                float distance = Vector3.Distance(wayPoint.transform.position, nextWayPoint.transform.position);

                                totalDistance += distance;

                                if (totalDistance <= sectionLength + NearSectionNodeDistance)
                                {
                                    targetIndex = i - 1;
                                    existSectionNodesCount++;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }

                        startLineIndex = targetIndex;
                        endLineIndex = wayPoints.Count - 1 + sectionWaypoints - existSectionNodesCount;
                        addNodeIndex = startLineIndex;
                        break;
                    }
                case PathSectionType.AllPath:
                    {
                        currentSegmentDistance = pathLength;
                        startLineIndex = 0;
                        existSectionNodesCount = wayPoints.Count;
                        endLineIndex = wayPoints.Count - 1 + sectionWaypoints - existSectionNodesCount;
                        addNodeIndex = startLineIndex + 1;
                        break;
                    }
            }

            var nodeDiff = sectionWaypoints - existSectionNodesCount;
            var nodes = selectedPath.Nodes;

            if (nodeDiff > 0)
            {
                while (nodeDiff > 0)
                {
                    selectedPath.InsertNode(default, addNodeIndex);
                    nodeDiff--;
                }
            }
            else if (nodeDiff < 0)
            {
                nodeDiff = -nodeDiff;

                while (nodeDiff > 0)
                {
                    selectedPath.RemoveNodeAt(startLineIndex);
                    nodeDiff--;
                }
            }

            var startNode = nodes[startLineIndex];
            var endNode = nodes[endLineIndex];

            switch (pathSectionType)
            {
                case PathSectionType.StartOfPath:
                    {
                        endNode.transform.position = startNode.transform.position + Vector3.Normalize(nodes[nodes.Count - 1].transform.position - startNode.transform.position) * currentSegmentDistance;
                        wayPoints[endLineIndex].transform.position = endNode.transform.position;
                        break;
                    }
                case PathSectionType.EndOfPath:
                    {
                        startNode.transform.position = endNode.transform.position + Vector3.Normalize(nodes[0].transform.position - endNode.transform.position) * currentSegmentDistance;
                        wayPoints[startLineIndex].transform.position = startNode.transform.position;
                        break;
                    }
            }

            var index = 1;

            for (int i = startLineIndex + 1; i < endLineIndex; i++)
            {
                var t = (float)index / (sectionWaypoints - 1);

                var nodePosition = Vector3.Lerp(startNode.transform.position, endNode.transform.position, t);
                var speedLimit = RoundSpeed(Mathf.Lerp(startSpeedLimit, endSpeedLimit, t));
                nodes[i].transform.position = nodePosition;
                wayPoints[i].transform.position = nodePosition;
                wayPoints[i].SpeedLimit = speedLimit;

                EditorSaver.SetObjectDirty(wayPoints[i]);
                index++;
            }

            wayPoints[startLineIndex].SpeedLimit = startSpeedLimit;
            wayPoints[endLineIndex].SpeedLimit = endSpeedLimit;

            EditorSaver.SetObjectDirty(wayPoints[startLineIndex]);
            EditorSaver.SetObjectDirty(wayPoints[endLineIndex]);
            EditorSaver.SetObjectDirty(selectedPath);

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }

        private void DrawCustomSectionSettings(SerializedObject so)
        {
            if (this.selectedPath == null)
            {
                return;
            }

            EditorGUILayout.PropertyField(so.FindProperty(nameof(pathSectionType)));
            EditorGUILayout.PropertyField(so.FindProperty(nameof(pathSectionCreateType)));

            if (pathSectionType != PathSectionType.AllPath)
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(sectionLength)));
            }

            if (pathSectionCreateType == PathSectionCreateType.UseExistNodes)
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(nearSectionNodeDistance)));
            }

            EditorGUILayout.PropertyField(so.FindProperty(nameof(sectionWaypoints)));
            EditorGUILayout.PropertyField(so.FindProperty(nameof(startSpeedLimit)));

            if (InspectorExtension.DrawVerticalArrowButton(arrowButtonStyle, ArrowOffset, ArrowFontSize, ArrowWidth, ArrowHeight))
            {
                var start = startSpeedLimit;
                startSpeedLimit = endSpeedLimit;
                endSpeedLimit = start;
            }

            EditorGUILayout.PropertyField(so.FindProperty(nameof(endSpeedLimit)));

            GUI.enabled = SectionSupported;

            if (GUILayout.Button("Create SpeedLimit Segment"))
            {
                CreateCustomSpeedLimitSegment();
            }

            GUI.enabled = true;

            if (CurveMessage)
            {
                EditorGUILayout.HelpBox("Curved paths not supported", MessageType.Error);
            }
        }

        private void SelectStartNode(PathNode node)
        {
            if (startPathNode != null)
            {
                startPathNode.HasSelectCustomColor = false;
            }

            startPathNode = node;

            if (node != null)
            {
                startPathNode.HasSelectCustomColor = true;
                startPathNode.SelectCustomColor = selectWaypointColor;
            }

            Repaint();
            SceneView.RepaintAll();
        }

        private void SelectEndNode(PathNode node)
        {
            if (endPathNode != null)
            {
                endPathNode.HasSelectCustomColor = false;
            }

            endPathNode = node;

            if (node != null)
            {
                endPathNode.HasSelectCustomColor = true;
                endPathNode.SelectCustomColor = selectWaypointColor;
            }

            Repaint();
            SceneView.RepaintAll();
        }

        private void SwitchHighlightState(Object pathObject, bool isActive)
        {
            if (pathObject == null)
            {
                return;
            }

            var path = (Path)pathObject;

            path.ShowInfoWaypoints = isActive;
            path.ShowAdditionalInfo = drawAdditionalSettings;
            path.Highlighted = isActive;
            SceneView.RepaintAll();
        }

        private void UnselectWaypoints()
        {
            if (startPathNode != null)
            {
                startPathNode.HasSelectCustomColor = false;
                startPathNode = null;
            }

            if (endPathNode != null)
            {
                endPathNode.HasSelectCustomColor = false;
                endPathNode = null;
            }
        }

        private void UnselectPath()
        {
            SwitchHighlightState(pathObject, false);
            UnselectWaypoints();
        }

        private float RoundSpeed(float sourceValue)
        {
            return UnityEngine.Mathf.RoundToInt(sourceValue);
        }

        private void Selection_selectionChanged()
        {
            if (Selection.activeGameObject != null)
            {
                var path = Selection.activeGameObject.GetComponent<Path>();

                if (path != null)
                {
                    Initialize(path);
                }
            }
        }

        #endregion

        #region Event handlers

        private void SceneView_duringSceneGui(SceneView obj)
        {
            if (Selection.activeObject != selectedPath && selectedPath != null)
            {
                if (selectedPath.ShowInfoWaypoints)
                {
                    PathEditorExtension.DrawWaypointInfo(selectedPath, null, selectedPath.ShowAdditionalInfo);
                }
            }

            if (speedLimitChangeType == SpeedLimitChangeType.Multiple && drawSelectButtons && selectedPath != null)
            {
                foreach (var node in selectedPath.WayPoints)
                {
                    var position = node.transform.position;

                    Action selectStartCallback = () =>
                    {
                        SelectStartNode(node);
                    };

                    Action selectEndCallback = () =>
                    {
                        SelectEndNode(node);
                    };

                    if (node != startPathNode && node != endPathNode)
                    {
                        List<Action> actions = new List<Action>()
                    {
                        selectStartCallback,
                        selectEndCallback
                    };

                        EditorExtension.DrawButtons(buttonTexts, position, MultipleButtonsSize, actions);
                    }
                    else if (node != startPathNode)
                    {
                        EditorExtension.DrawButton(buttonTexts[0], position, ButtonSize, selectStartCallback);
                    }
                    else if (node != endPathNode)
                    {
                        EditorExtension.DrawButton(buttonTexts[1], position, ButtonSize, selectEndCallback);
                    }
                }
            }
        }

        private void Undo_undoRedoPerformed()
        {
            Repaint();
        }

        #endregion
    }
}
#endif