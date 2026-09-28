using Spirit604.CityEditor.Road;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    [ExecuteInEditMode]
    public partial class Path : MonoBehaviour
    {
        #region Helper types & constants

        [Serializable]
        public class IntersectPointInfo
        {
            public Path IntersectedPath;
            public Vector3 IntersectPoint;
            public int LocalNodeIndex;
            public bool LocalSpace;

            public Vector3 GetIntersectPoint(Path sourcePath)
            {
                var point = IntersectPoint;

                if (LocalSpace)
                {
                    var parent = sourcePath.GetParent();

                    if (parent != null)
                        point = parent.transform.TransformPoint(point);
                }

                return point;
            }
        }

        public const int CUBE_BEZIER_TYPE = 3;
        public const int QUAD_BEZIER_TYPE = 4;

        #endregion

        #region Serialized Variables

        [SerializeField] private Transform nodesParent;
        [SerializeField] private Transform wayPointsParent;

        [Tooltip("Source node traffic from which the path starts")]
        [SerializeField] private TrafficNode sourceTrafficNode;

        [Tooltip("Type of connection of the TrafficNode or the Path")]
        [SerializeField] private PathConnectionType pathConnectionType = PathConnectionType.TrafficNode;

        [Tooltip("Connected TrafficNode")]
        [SerializeField] private TrafficNode connectedTrafficNode;

        [Tooltip("Connected path in the custom point")]
        [SerializeField] private Path connectedPath;

        [Tooltip("Custom connected traffic light")]
        [SerializeField] private TrafficLightHandler customLightHandler;

        [Tooltip("Key nodes for creating curves (Bezier)")]
        [SerializeField] private List<Transform> nodes = new List<Transform>();

        [Tooltip("Waypoints of path")]
        [SerializeField] private List<PathNode> wayPoints = new List<PathNode>();

        [Tooltip("Intersection points with other paths")]
        [SerializeField] private List<IntersectPointInfo> intersects = new List<IntersectPointInfo>();

        [Tooltip("Path length")]
        [SerializeField] private float pathLength;

        [Tooltip("" +
            "<b>Straight line</b> is default line with nodes connected in series\r\n\r\n" +
            "<b>Bezier</b> generated line based on curved nodes")]
        [SerializeField] private PathCurveType pathCurveType;

        [Tooltip("<b>Straight road</b> : is used to automatically calculate lane changing by traffic")]
        [SerializeField] private PathRoadType pathRoadType;

        [Tooltip("Group types of traffic vehicles that can go on this path")]
        [SerializeField] private TrafficGroupMask trafficGroupMask = new TrafficGroupMask();

        [Tooltip("Order of crossing intersected paths (vehicle with the higher priority gets through first)")]
        [SerializeField][Range(-5, 5)] private int priority;

        [Tooltip("Number of waypoints in the curve segment")]
        [SerializeField][Range(2, 20)] private int wayPointsCountPerCurve = 2;

        [Tooltip("Speed limit of the path. If value == 0, the speed limit is the default value")]
        [SerializeField][Range(0, 200)] private float pathSpeedLimit;

        [Tooltip("Connected lane index")]
        [SerializeField][Range(-1, 10)] private int connectedLaneIndex = -1;

        [Tooltip("Normalized length of the highlighted path (editor only)")]
        [Range(-1f, 1f)] public float HightlightNormalizedLength = 1f;

        [Tooltip("Cars entering this path will move with a rail movement")]
        [SerializeField] private bool rail;

        [Tooltip("Path will be connected to the opposite side of the node")]
        [SerializeField] private bool reversedConnectionSide;

        [Tooltip("On/off tangents on the scene")]
        [SerializeField] private bool drawTangent;

        [Tooltip("Two position handles of tangent will move together")]
        [SerializeField] private bool clampTangent;

        [Tooltip("Auto-attach to connected path (path point only)")]
        [SerializeField] private bool autoAttachPath = true;

        [Tooltip("Show attach buttons (path point only)")]
        [SerializeField] private bool showAttachPathButtons;

        [SerializeField] private bool highlightConnectedPath = true;

        [Tooltip("Show info of waypoints on the scene")]
        [SerializeField] private bool showInfoWaypoints;

        [SerializeField] private bool showAdditionalInfo;

        [Tooltip("Lock Y-axis for position handles of nodes")]
        [SerializeField] private bool lockYAxis = true;

        [Tooltip("Show intersected points on the scene")]
        public bool ShowIntersectedPoints;

#if UNITY_EDITOR
        [Tooltip("Show position handles for nodes")]
        public bool ShowHandles = true;

        [Tooltip("Show edit buttons for path (add/remove nodes)")]
        public bool ShowEditButtons = true;

        [Tooltip("Hightlight color of the path")]
        public Color HighlightColor = Color.white;

        [NonSerialized] public bool Highlighted;
#endif
        #endregion

        #region Variables

        private List<float> oldSpeedLimits = new List<float>();

        #endregion

        #region Properties     

        public List<Transform> Nodes
        {
            get
            {
                return nodes;
            }
            set
            {
                for (int i = 0; i < value.Count; i++)
                {
                    value[i].transform.parent = nodesParent;
                }

                nodes = value;
            }
        }

        public IList<PathNode> WayPoints => wayPoints.AsReadOnly();

        public Vector3[] WaypointPositionArray => wayPoints.Select(a => a.transform.position).ToArray();

        public IList<Vector3> WaypointPositions => wayPoints.Select(a => a.transform.position).ToList().AsReadOnly();

        public IList<IntersectPointInfo> Intersects => intersects.AsReadOnly();

        public Transform[] WaypointTransforms => wayPoints != null ? wayPoints.Select(item => item.transform).ToArray() : null;

        public PathConnectionType PathConnectionType { get => pathConnectionType; set => pathConnectionType = value; }

        public TrafficNode ConnectedTrafficNode
        {
            get
            {
                return connectedTrafficNode;
            }
            set
            {
                if (connectedTrafficNode != value)
                {
                    connectedTrafficNode = value;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        public Path ConnectedPath { get => connectedPath; set => connectedPath = value; }

        public Transform NodesParent { get => nodesParent; }

        public Transform WayPointsParent { get => wayPointsParent; }

        public TrafficNode SourceTrafficNode { get => sourceTrafficNode; set => sourceTrafficNode = value; }

        public int WayPointsCountPerCurve { get => wayPointsCountPerCurve; set => wayPointsCountPerCurve = value; }

        public float PathLength
        {
            get => pathLength;
            set => pathLength = value;
        }

        public PathCurveType PathCurveType { get => pathCurveType; set => pathCurveType = value; }

        public PathRoadType PathRoadType { get => pathRoadType; set => pathRoadType = value; }

        public TrafficGroupMask TrafficGroupMask { get => trafficGroupMask; set => trafficGroupMask = value; }

        public TrafficGroupType TrafficGroup => trafficGroupMask.GetValue();

        public bool ShowInfoWaypoints { get => showInfoWaypoints; set => showInfoWaypoints = value; }

        public bool ShowAdditionalInfo { get => showAdditionalInfo; set => showAdditionalInfo = value; }

        public float PathSpeedLimit { get => pathSpeedLimit; set => pathSpeedLimit = value; }

        public int Priority { get => priority; set => priority = value; }

        public int SourceLaneIndex => sourceTrafficNode?.GetLaneIndexOfPathCheckAll(this) ?? -1;

        public int ConnectedLaneIndex { get => connectedLaneIndex; set => connectedLaneIndex = value; }

        public bool LockYAxis { get => lockYAxis; }

        public bool ReversedConnectionSide
        {
            get => CanUseReverseConnection ? reversedConnectionSide : false;
            set
            {
                if (CanUseReverseConnection)
                {
                    reversedConnectionSide = value;
                }
                else
                {
                    if (reversedConnectionSide)
                    {
                        reversedConnectionSide = false;
                    }
                }
            }
        }

        public bool CanUseReverseConnection => !connectedTrafficNode?.IsOneWay ?? false;

        public bool Rail { get => rail; set => rail = value; }

        public bool HasCustomLight => customLightHandler != null;

        public List<TrafficNode> WorldTrafficNodes { get; private set; }

        public bool ClampTangent { get => clampTangent; set => clampTangent = value; }

        public bool DrawTangent { get => drawTangent; set => drawTangent = value; }

        public bool AutoAttachPath { get => autoAttachPath; set => autoAttachPath = value; }

        public bool ShowAttachButtons { get => showAttachPathButtons; set => showAttachPathButtons = value; }

        public bool HighlightConnectedPath { get => highlightConnectedPath; set => highlightConnectedPath = value; }

        public TrafficLightHandler CustomLightHandler { get => customLightHandler; }

        public bool HasConnection
        {
            get
            {
                bool hasConnection = false;

                switch (pathConnectionType)
                {
                    case PathConnectionType.TrafficNode:
                        hasConnection = connectedTrafficNode != null && sourceTrafficNode;
                        break;
                    case PathConnectionType.PathPoint:
                        hasConnection = connectedPath != null && sourceTrafficNode;
                        break;
                }

                return hasConnection;
            }
        }

        public Vector3 StartPosition => nodes?.Count > 0 && nodes[0] != null ? nodes[0].transform.position : Vector3.zero;

        public Vector3 EndPosition => nodes?.Count > 0 && nodes[nodes.Count - 1] != null ? nodes[nodes.Count - 1].transform.position : Vector3.zero;

        public bool EnterOfCrossroad
        {
            get
            {
                if (SourceTrafficNode && SourceTrafficNode.TrafficLightCrossroad)
                {
                    return !IsExternal && SourceTrafficNode.TrafficLightCrossroad.TrafficNodeDefaultCount() > 2;
                }

                return false;
            }

        }

        public bool IsExternal => SourceTrafficNode.GetLaneIndexOfPath(this) == -1;

        public bool Selected { get; set; }

#if UNITY_EDITOR
        public bool HasPathCustomColor { get; set; }
        internal bool Dirty { get; set; }
#endif

        #endregion

        #region Unity lifecycle

#if UNITY_EDITOR
        private void OnDestroy()
        {
            RemovePathLinking();
        }
#endif

        private void OnDisable()
        {
#if UNITY_EDITOR
            Highlighted = false;
#endif
        }

        private void OnApplicationQuit()
        {
#if UNITY_EDITOR
            Highlighted = false;
#endif
        }

        #endregion

        #region Create & delete methods

        public void CreatePath(bool createNewWaypoints = true, bool recordUndo = false)
        {
            int targetWaypointCount = 0;

            bool shouldRecreate = ShouldRecreateWaypoints(out targetWaypointCount);

            if (shouldRecreate)
            {
                createNewWaypoints = true;
            }

            if (createNewWaypoints)
            {
                SaveOldWayPoints();
                ClearWaypoints(recordUndo);
            }

            if (wayPointsCountPerCurve <= 1)
            {
                return;
            }

            if (pathCurveType == PathCurveType.StraightLine)
            {
                AddWayPoint(nodes[0].transform.position, createNewWaypoints, 0, recordUndo);

                PathHelper.CreateStraightPath(this, nodes, createNewWaypoints, 1, recordUndo);
            }
            else
            {
                PathHelper.CreateCurvedPath(this, createNewWaypoints, targetWaypointCount, recordUndo);
            }

            for (int i = 1; i < wayPoints.Count - 1; i++)
            {
                var current = wayPoints[i];
                var next = wayPoints[i + 1];

                current.transform.rotation = Quaternion.LookRotation((next.transform.position - current.transform.position).normalized);
            }

            if (createNewWaypoints)
            {
                LoadOldWaypoints();
            }

#if UNITY_EDITOR
            Dirty = true;
#endif

            EditorSaver.SetObjectDirty(this);
        }

        public void RecreateAndSaveUndo()
        {
#if UNITY_EDITOR
            this.SavePath();
            this.CreatePath(true, true);
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif
        }

        public Transform AddNode(bool recordUndo = true)
        {
            if (recordUndo)
            {
#if UNITY_EDITOR
                Undo.RegisterCompleteObjectUndo(this, "Added New Node");
#endif
            }

            Transform node = CreateNode(recordUndo);

            nodes.Add(node);

            return node;
        }

        public Transform CreateNode(bool recordUndo)
        {
            int index = nodes.Count;
            var node = new GameObject(GetNodeName(index)).transform;

            if (recordUndo)
            {
#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(node.gameObject, "New node created!");
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif
            }

            node.SetParent(nodesParent);
            node.transform.localPosition = Vector3.zero;
            return node;
        }

        public Transform InsertNode(int index, bool changeSiblingIndex = true, bool recordUndo = true)
        {
            var node = CreateNode(recordUndo);

            nodes.Insert(index, node);

            if (changeSiblingIndex)
            {
                node.SetSiblingIndex(index);
            }

            return node;
        }

        public Transform AddNode(Vector3 position, bool recordUndo = true)
        {
            var node = AddNode(recordUndo);
            node.transform.position = position;

            return node;
        }

        public Transform InsertNode(Vector3 spawnPosition, int index, bool recordUndo = true, bool copyPreviousNodeSettings = false)
        {
            return InsertNode(spawnPosition, index, out var newWaypoint, recordUndo, copyPreviousNodeSettings);
        }

        public Transform InsertNode(Vector3 spawnPosition, int index, out PathNode pathNode, bool recordUndo = true, bool copyPreviousNodeSettings = false)
        {
            pathNode = null;

            if (recordUndo)
            {
#if UNITY_EDITOR
                Undo.RegisterCompleteObjectUndo(this, "Added New Node");
#endif
            }

            var insertWaypoint = nodes.Count == wayPoints.Count && wayPointsCountPerCurve == 2;

            var node = new GameObject(GetNodeName(index)).transform;

            node.SetParent(nodesParent);
            node.SetSiblingIndex(index);
            node.transform.position = spawnPosition;

            if (recordUndo)
            {
#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(node.gameObject, "New node created!");
#endif
            }

            Vector3 dir = default;

            if (index != 0)
            {
                dir = (nodes[index].transform.position - nodes[index - 1].transform.position).normalized;
            }
            else
            {
                dir = (nodes[index + 1].transform.position - nodes[index].transform.position).normalized;
            }

            node.transform.rotation = Quaternion.LookRotation(dir);

            nodes.Insert(index, node);

            if (insertWaypoint)
            {
                pathNode = AddWayPoint(spawnPosition, true, index, insertMode: true, recordUndo: recordUndo);

                if (copyPreviousNodeSettings)
                {
                    if (index - 1 >= 0)
                    {
                        var previousWaypoint = wayPoints[index - 1];
                        var newWaypoint = wayPoints[index];

                        newWaypoint.Copy(previousWaypoint);
                    }
                }
            }
            else
            {
                CreatePath(true, recordUndo);
            }

            RenameNodes();

            if (recordUndo)
            {
#if UNITY_EDITOR
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif
            }

            return node;
        }

        public void RemoveNodeAt(int index, bool recordUndo = true, bool autoRecreatePath = true)
        {
            if (index < 0 || nodes.Count <= index || nodes.Count <= 2)
            {
                return;
            }

            if (recordUndo)
            {
#if UNITY_EDITOR
                Undo.RegisterCompleteObjectUndo(this, "Remove Node");
#endif
            }

            var node = nodes.ElementAt(index);
            nodes.Remove(node);

            if (!recordUndo)
            {
                DestroyImmediate(node.gameObject);
            }
            else
            {
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(node.gameObject);
#endif
            }

            if (autoRecreatePath)
            {
                if (nodes.Count + 1 == wayPoints.Count)
                {
                    var waypoint = wayPoints.ElementAt(index);
                    wayPoints.Remove(waypoint);

                    if (!recordUndo)
                    {
                        DestroyImmediate(waypoint.gameObject);
                    }
                    else
                    {
#if UNITY_EDITOR
                        Undo.DestroyObjectImmediate(waypoint.gameObject);
#endif
                    }

                    RenameNodes(true);
                    RenameWaypoints(true);
                }
                else
                {
                    CreatePath(true, recordUndo);
                }
            }

            if (recordUndo)
            {
#if UNITY_EDITOR
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif
            }
        }

        public PathNode AddWayPoint(
            Vector3 waypointPosition,
            bool createNew = true,
            int wayPointIndex = 0,
            bool insertMode = false,
            bool recordUndo = false)
        {
            if (createNew)
            {
                Transform waypoint = new GameObject(GetWaypointName(wayPointIndex)).transform;
                waypoint.SetParent(wayPointsParent);
                waypoint.position = waypointPosition;
                var pathNode = waypoint.gameObject.AddComponent<PathNode>();
                pathNode.SpeedLimit = pathSpeedLimit;

                if (recordUndo)
                {
#if UNITY_EDITOR
                    Undo.RegisterCreatedObjectUndo(waypoint.gameObject, "Created Waypoint");
#endif
                }

                if (!insertMode)
                {
                    wayPoints.Add(pathNode);
                }
                else
                {
                    waypoint.SetSiblingIndex(wayPointIndex);
                    wayPoints.Insert(wayPointIndex, pathNode);
                }

                EditorSaver.SetObjectDirty(pathNode);

                RenameWaypoints();

                return pathNode;
            }
            else
            {
                if (recordUndo)
                {
#if UNITY_EDITOR
                    Undo.RecordObject(wayPoints[wayPointIndex].transform, "Revert Waypoint Position");
#endif
                }

                wayPoints[wayPointIndex].transform.position = waypointPosition;

                return wayPoints[wayPointIndex];
            }
        }

        public Transform InsertNodeOnLineAtCustomPosition(Vector3 sourceWorldPosition, bool recordUndo = true)
        {
            if (PathCurveType != PathCurveType.StraightLine || nodes.Count != wayPoints.Count)
            {
                return null;
            }

            var points = wayPoints.Select(a => a.transform.position).ToArray();

            if (points?.Length > 1)
            {
                var point = VectorExtensions.FindNearestPointOnLine(points, sourceWorldPosition);
                int index = -1;

                for (int i = 0; i < points.Length - 1; i++)
                {
                    Vector3 a = points[i];
                    Vector3 b = points[i + 1];

                    var isBetween = VectorExtensions.IsBetween3DSpace(a, b, point);

                    if (isBetween)
                    {
                        index = i;
                        break;
                    }
                }

                if (index != -1)
                {
                    return InsertNode(point, index + 1, recordUndo, true);
                }
            }

            return null;
        }

        public void IterateWaypoints(Action<PathNode> action)
        {
            for (int i = 0; i < wayPoints.Count; i++) if (wayPoints[i] != null) action(wayPoints[i]);
        }

        public void IterateWaypoints(Action<PathNode, int> action)
        {
            for (int i = 0; i < wayPoints.Count; i++) if (wayPoints[i] != null) action(wayPoints[i], i);
        }

        public void IterateWaypoints(Action<PathNode> action, float distance, bool resetDistanceOnAction = true)
        {
            float totalDist = 0;

            for (int i = 1; i < wayPoints.Count - 1; i++)
            {
                PathNode wayPoint = wayPoints[i];

                var dist = Vector3.Distance(wayPoints[i].transform.position, wayPoints[i - 1].transform.position);

                totalDist += dist;

                if (totalDist >= distance)
                {
                    if (resetDistanceOnAction)
                    {
                        totalDist = 0;
                    }
                    else
                    {
                        totalDist -= distance;
                    }

                    action(wayPoint);
                }
            }
        }

        public void IterateWaypoints(Action<PathNode, int> action, float distance, bool resetDistanceOnAction = true)
        {
            float totalDist = 0;

            for (int i = 1; i < wayPoints.Count - 1; i++)
            {
                PathNode wayPoint = wayPoints[i];

                var dist = Vector3.Distance(wayPoints[i].transform.position, wayPoints[i - 1].transform.position);

                totalDist += dist;

                if (totalDist >= distance)
                {
                    if (resetDistanceOnAction)
                    {
                        totalDist = 0;
                    }
                    else
                    {
                        totalDist -= distance;
                    }

                    action(wayPoint, i);
                }
            }
        }

        public void RenameNodes(bool recordUndo = false)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                if (node != null)
                {
#if UNITY_EDITOR
                    if (recordUndo)
                    {
                        Undo.RecordObject(node.gameObject, "Undo name");
                    }
#endif

                    node.name = GetNodeName(i);
                }
            }
        }

        public void RenameWaypoints(bool recordUndo = false)
        {
            for (int i = 0; i < wayPoints.Count; i++)
            {
                var wayPoint = wayPoints[i];

                if (wayPoint != null)
                {
#if UNITY_EDITOR
                    if (recordUndo)
                    {
                        Undo.RecordObject(wayPoint.gameObject, "Undo name");
                    }
#endif

                    wayPoint.name = GetWaypointName(i);
                }
            }
        }

        public void ClearOnPathNodesAndWaypoints(bool recordUndo = false)
        {
            if (recordUndo)
            {
#if UNITY_EDITOR
                Undo.RegisterCompleteObjectUndo(this, "Clear path");
#endif
            }

            while (nodes.Count > 2)
            {
                var node = nodes[1];
                nodes.RemoveAt(1);

                if (!recordUndo)
                {
                    DestroyImmediate(node.gameObject);
                }
                else
                {
#if UNITY_EDITOR
                    Undo.DestroyObjectImmediate(node.gameObject);
#endif
                }
            }

            while (wayPoints.Count > 2)
            {
                var waypoint = wayPoints[1];
                wayPoints.RemoveAt(1);

                if (!recordUndo)
                {
                    DestroyImmediate(waypoint.gameObject);
                }
                else
                {
#if UNITY_EDITOR
                    Undo.DestroyObjectImmediate(waypoint.gameObject);
#endif
                }
            }

            if (recordUndo)
            {
#if UNITY_EDITOR
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif
            }
        }

        public void ConvertToStraightLine()
        {
            if (pathCurveType == PathCurveType.StraightLine)
            {
                return;
            }

#if UNITY_EDITOR

            Undo.RegisterCompleteObjectUndo(this, "Path Convert");

            for (int i = 0; i < nodes.Count; i++)
            {
                Undo.RecordObject(nodes[i].gameObject, "Path Convert");
            }

            for (int i = 1; i < nodes.Count - 1; i++)
            {
                Undo.RecordObject(nodes[i].transform, "Path Convert");
            }

            pathCurveType = PathCurveType.StraightLine;
            wayPointsCountPerCurve = 2;

            for (int i = 1; i < wayPoints.Count - 1; i++)
            {
                var index = i;
                Transform node = null;

                var hasExistNode = i < nodes.Count - 1;

                if (hasExistNode)
                {
                    node = nodes[i].transform;
                }
                else
                {
                    node = InsertNode(i);
                }

                node.transform.position = wayPoints[i].transform.position;
                node.transform.rotation = wayPoints[i].transform.rotation;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                var nodeName = GetNodeName(i);
                nodes[i].name = nodeName;
            }

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            EditorSaver.SetObjectDirty(this);
#endif
        }

        private void ClearWaypoints(bool recordUndo = true)
        {
            wayPoints.Clear();

            if (!wayPointsParent)
            {
                return;
            }

            while (wayPointsParent.childCount > 0)
            {
                var go = wayPointsParent.GetChild(0).gameObject;

                if (!recordUndo)
                {
                    DestroyImmediate(go);
                }
                else
                {
#if UNITY_EDITOR
                    Undo.DestroyObjectImmediate(go);
#endif
                }
            }
        }

        private bool ShouldRecreateWaypoints(out int targetWaypointCount)
        {
            bool shouldRecreate = false;

            if (pathCurveType == PathCurveType.StraightLine)
            {
                targetWaypointCount = nodes.Count + (nodes.Count - 1) * (wayPointsCountPerCurve - 2);
                shouldRecreate = targetWaypointCount != wayPoints.Count;
            }
            else
            {
                int curveLength = pathCurveType == PathCurveType.BezierCube ? CUBE_BEZIER_TYPE : QUAD_BEZIER_TYPE;
                int curvesCount = Mathf.FloorToInt((float)nodes.Count / curveLength);

                targetWaypointCount = curvesCount * (wayPointsCountPerCurve - 1) + curvesCount + (nodes.Count - curvesCount * curveLength);
                shouldRecreate = targetWaypointCount != wayPoints.Count;
            }

            return shouldRecreate;
        }

        private void SaveOldWayPoints()
        {
            if (wayPoints?.Count > 0 && wayPoints[0] != null)
            {
                oldSpeedLimits = wayPoints.Select(item => item.SpeedLimit).ToList();
            }
        }

        private void LoadOldWaypoints()
        {
            for (int i = 0; i < wayPoints?.Count; i++)
            {
                if (i < oldSpeedLimits?.Count)
                {
                    var pathNode = wayPoints[i];
                    pathNode.SpeedLimit = oldSpeedLimits[i];
                    EditorSaver.SetObjectDirty(pathNode);
                }
                else
                {
                    break;
                }
            }
        }

        #endregion

        #region Helper methods

        public void AttachToTrafficNodes(int laneIndex, bool sourceExternal = false, bool connectedExternal = false, bool recordUndo = false)
        {
            laneIndex = connectedLaneIndex == -1 ? laneIndex : connectedLaneIndex;

            AttachSourceNode(laneIndex, sourceExternal, false, recordUndo);
            AttachConnectedNode(laneIndex, connectedExternal, false, recordUndo);

            CreatePath(false, recordUndo);
        }

        public void AutoAttachToTrafficNodes(bool recordUndo = false)
        {
            var sourceExternal = false;
            var connectedExternal = false;

            if (sourceTrafficNode != null)
            {
                sourceExternal = sourceTrafficNode.IsExternalPath(this);
            }

            if (connectedTrafficNode != null)
            {
                var dir1 = sourceTrafficNode.transform.position - connectedTrafficNode.transform.position;
                var dir2 = connectedTrafficNode.transform.forward;
                var dot = Vector3.Dot(dir1, dir2);

                connectedExternal = !sourceExternal ? dot > 0 : dot < 0;

                if (sourceTrafficNode && sourceTrafficNode.TrafficLightCrossroad != connectedTrafficNode.TrafficLightCrossroad)
                {
                    connectedExternal = !connectedExternal;
                }

                if (reversedConnectionSide)
                {
                    connectedExternal = !connectedExternal;
                }
            }

            AttachSourceNode(connectedLaneIndex, sourceExternal, true, recordUndo);
            AttachConnectedNode(connectedLaneIndex, connectedExternal, true, recordUndo);

            CreatePath(false, recordUndo);
        }

        public void AttachSourceNode(int laneIndex, bool sourceExternal = false, bool autoRecalcPath = false, bool recordUndo = false)
        {
            if (sourceTrafficNode == null)
            {
                return;
            }

#if UNITY_EDITOR

            laneIndex = sourceTrafficNode.GetLaneIndexOfPath(this, sourceExternal);

            Vector3 position = PathAttachHelper.GetSourceAttachPosition(this, laneIndex, sourceExternal);

            var node = nodes[0].transform;

            if (recordUndo)
            {
                Undo.RecordObject(node, "Undo node");
            }

            node.position = position;

            if (autoRecalcPath)
            {
                CreatePath(false, recordUndo);
            }
#endif
        }

        public void AttachConnectedNode(bool connectedExternal = false, bool autoRecalcPath = false, bool recordUndo = false)
        {
            AttachConnectedNode(connectedLaneIndex, connectedExternal, autoRecalcPath, recordUndo);
        }

        public void AttachConnectedNode(int laneIndex, bool connectedExternal = false, bool autoRecalcPath = false, bool recordUndo = false)
        {
            if (connectedTrafficNode == null)
            {
                return;
            }

#if UNITY_EDITOR
            bool changed = false;

            var position = PathAttachHelper.GetTargetAttachPosition(this, laneIndex, connectedExternal);

            var node = nodes.Last();

            if (recordUndo)
            {
                Undo.RecordObject(node.transform, "Undo node");
            }

            node.transform.position = position;

            if (connectedLaneIndex != laneIndex)
            {
                if (recordUndo)
                {
                    Undo.RecordObject(this, "Undo path");
                }

                this.connectedLaneIndex = laneIndex;
                changed = true;
            }

            if (autoRecalcPath)
            {
                CreatePath(false, recordUndo);
            }
            else
            {
                if (changed)
                {
                    EditorSaver.SetObjectDirty(this);
                }
            }

            if (recordUndo)
            {
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            }
#endif
        }

        public void AttachToNodes(bool sourceExternalLane = false, bool connectedExternal = false, bool recordUndo = false)
        {
            if (connectedLaneIndex != -1)
            {
                AttachToTrafficNodes(connectedLaneIndex, sourceExternalLane, connectedExternal, recordUndo);
            }
        }

        public void ResetSpeedLimit(bool recordUndo = true)
        {
            for (int i = 0; i < wayPoints?.Count; i++)
            {
                if (recordUndo)
                {
#if UNITY_EDITOR
                    Undo.RecordObject(wayPoints[i], "Revert speed limit");
#endif
                }

                wayPoints[i].SpeedLimit = pathSpeedLimit;
                EditorSaver.SetObjectDirty(wayPoints[i]);
            }

            if (recordUndo)
            {
#if UNITY_EDITOR
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif
            }
        }

        public void TryToAddCustomLight()
        {
            if (sourceTrafficNode && sourceTrafficNode.TrafficLightCrossroad)
            {
                var trafficLightHandler = sourceTrafficNode.TrafficLightCrossroad.AddCustomLight(this);

                if (trafficLightHandler != null)
                {
                    customLightHandler = trafficLightHandler;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        public void TryToRemoveCustomLight(bool userClicked = false)
        {
            if (customLightHandler && customLightHandler.TrafficLightCrossroad)
            {
                customLightHandler.TrafficLightCrossroad.RemovePath(this, userClicked);
                customLightHandler = null;
                EditorSaver.SetObjectDirty(this);
            }
        }

        public float GetPathLength(int startIndex = -1, int endIndex = -1)
        {
            if (wayPoints.Count < 2)
                return 0;

            float pathLength = 0;

            var currentStartIndex = startIndex != -1 ? Mathf.Clamp(startIndex, 0, wayPoints.Count - 1) : 0;
            var currentEndIndex = endIndex != -1 ? Mathf.Clamp(endIndex, 0, wayPoints.Count - 1) : wayPoints.Count - 1;

            if (currentStartIndex >= currentEndIndex)
            {
                if (currentStartIndex > 0)
                {
                    currentStartIndex = currentEndIndex - 1;
                }
                else
                {
                    currentEndIndex = currentStartIndex + 1;
                }
            }

            for (int i = currentStartIndex; i < currentEndIndex; i++)
            {
                Vector3 A1point = wayPoints[i].transform.position;
                Vector3 A2point = wayPoints[i + 1].transform.position;

                pathLength += Vector3.Distance(A1point, A2point);
            }

            return pathLength;
        }

        public void FindConnectedLaneForPathPoint()
        {
            if (pathConnectionType != PathConnectionType.PathPoint)
                return;

            int connectedLaneIndex = -1;

            if (connectedPath != null && connectedPath.SourceTrafficNode != null)
            {
                connectedLaneIndex = connectedPath.SourceTrafficNode.GetLaneIndexOfPath(connectedPath);
            }

            if (ConnectedLaneIndex != connectedLaneIndex)
            {
                ConnectedLaneIndex = connectedLaneIndex;
                EditorSaver.SetObjectDirty(this);
            }
        }

        public void BakePathLength()
        {
            var currentPathLength = GetPathLength();
            this.pathLength = currentPathLength;
            EditorSaver.SetObjectDirty(this);
        }

        public void SaveMovementUndo(bool includePathTransform = false)
        {
            if (includePathTransform)
            {
#if UNITY_EDITOR
                Undo.RecordObject(this.transform, "Saved path");
#endif
            }

            if (nodes?.Count > 0)
            {
                foreach (var node in nodes)
                {
#if UNITY_EDITOR
                    Undo.RegisterCompleteObjectUndo(node.transform, "Saved node");
#endif
                }
            }

            var wayPointTransforms = WaypointTransforms;

            if (wayPointTransforms?.Length > 0)
            {
                foreach (var wayPointTransform in wayPointTransforms)
                {
#if UNITY_EDITOR
                    Undo.RegisterCompleteObjectUndo(wayPointTransform, "Saved waypoint");
#endif
                }
            }
        }

        public void SavePath(bool includeMovement = false, bool includeNaming = false)
        {
#if UNITY_EDITOR
            Undo.RegisterCompleteObjectUndo(this, "Revert path");

            if (includeMovement)
            {
                SaveMovementUndo();
            }

            if (includeNaming)
            {
                if (nodes?.Count > 0)
                {
                    foreach (var node in nodes)
                    {
                        if (!node)
                        {
                            continue;
                        }

                        Undo.RegisterCompleteObjectUndo(node.gameObject, "Saved node");
                    }
                }
            }
#endif
        }

        public Vector3 GetMiddlePosition()
        {
            if (wayPoints.Count > 2)
            {
                var midIndex = Mathf.FloorToInt((float)(wayPoints.Count - 1) / 2);
                var middlePosition = (wayPoints[midIndex].transform.position);

                return middlePosition;
            }
            else if (wayPoints.Count == 2)
            {
                return (wayPoints[0].transform.position + wayPoints[1].transform.position) / 2;
            }
            else if (wayPoints.Count == 1)
            {
                return wayPoints[0].transform.position;
            }
            else
            {
                return transform.position;
            }
        }

        public void AddIntersectPoint(IntersectPointInfo intersectPoint)
        {
            intersects.Add(intersectPoint);
            EditorSaver.SetObjectDirty(this);
        }

        public void SortIntersectsByDistance()
        {
            if (intersects?.Count > 1)
            {
                var sourcePoint = Nodes[0].transform.position;
                intersects = intersects.OrderBy(x => Vector3.Distance(x.IntersectPoint, sourcePoint)).ToList();

                EditorSaver.SetObjectDirty(this);
            }
        }

        public void InitializeInstersects(IEnumerable<IntersectPointInfo> newIntersects)
        {
            intersects = newIntersects.ToList();
            EditorSaver.SetObjectDirty(this);
        }

        public int GetTargetWaypointIndexByPoint(Vector3 sourcePoint)
        {
            for (int i = 0; i < wayPoints.Count - 1; i++)
            {
                var wayPoint = wayPoints[i].transform.position;
                var wayPoint2 = wayPoints[i + 1].transform.position;

                if (VectorExtensions.IsBetween3DSpace(wayPoint, wayPoint2, sourcePoint))
                {
                    return i + 1;
                }
            }

            return -1;
        }

        public void SwitchConnectedPathHighlightState(bool isActive)
        {
            if (!highlightConnectedPath && isActive)
            {
                isActive = false;
            }

            if (ConnectedPath && pathConnectionType == PathConnectionType.PathPoint)
            {
#if UNITY_EDITOR
                ConnectedPath.Highlighted = isActive;
#endif
            }
        }

        public void CheckConnection(bool connectedExternal)
        {
#if UNITY_EDITOR
            if (connectedLaneIndex == -1)
            {
                UnityEngine.Debug.Log($"Path InstanceID {this.GetInstanceID()} doesn't have connection index{TrafficObjectFinderMessage.GetMessage()}");
                return;
            }

            if (wayPoints.Count < 2)
            {
                UnityEngine.Debug.Log($"Path InstanceID {this.GetInstanceID()} waypoints not created, create this path{TrafficObjectFinderMessage.GetMessage()}");
                return;
            }

            var firstWaypoint = wayPoints[0];
            var lastWaypoint = wayPoints[wayPoints.Count - 1];

            if (firstWaypoint == null || lastWaypoint == null)
            {
                UnityEngine.Debug.Log($"Path InstanceID {this.GetInstanceID()} has null waypoint, recreate this path{TrafficObjectFinderMessage.GetMessage()}");
            }

            if (nodes.Count > 0 && nodes[0] != null)
            {
                var dist1 = Vector3.SqrMagnitude(firstWaypoint.transform.position.Flat() - nodes[0].position.Flat());

                // 0.5f * 0.5f square
                if (dist1 > 0.25f)
                {
                    UnityEngine.Debug.Log($"Path InstanceID {this.GetInstanceID()} waypoint position not matched to its control nodes, delete this path & re-create this path{TrafficObjectFinderMessage.GetMessage()}");
                    return;
                }
            }

            if (nodes.Count > 0 && nodes[nodes.Count - 1] != null)
            {
                var dist1 = Vector3.SqrMagnitude(lastWaypoint.transform.position.Flat() - nodes[nodes.Count - 1].position.Flat());

                // 0.5f * 0.5f square
                if (dist1 > 0.25f)
                {
                    UnityEngine.Debug.Log($"Path InstanceID {this.GetInstanceID()} waypoint position not matched to its control nodes, delete this path & re-create this path{TrafficObjectFinderMessage.GetMessage()}");
                    return;
                }
            }

            if (pathConnectionType == PathConnectionType.TrafficNode)
            {
                var pos = PathAttachHelper.GetTargetAttachPosition(this, connectedLaneIndex, connectedExternal);

                if (pos != Vector3.zero && nodes.Count > 0 && nodes[nodes.Count - 1] != null)
                {
                    var dist = Vector3.SqrMagnitude(pos - nodes.Last().position);

                    // 0.5f * 0.5f square
                    if (dist > 0.25f)
                    {
                        UnityEngine.Debug.Log($"Path InstanceID {this.GetInstanceID()} appears to have the wrong connection lane index, or was previously connected with a different set of settings. Reattach this path again.{TrafficObjectFinderMessage.GetMessage()}");
                    }
                }
            }
            else
            {
                bool found = false;

                for (int i = 0; i < intersects.Count; i++)
                {
                    if (intersects[i].IntersectedPath == connectedPath)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    if (connectedPath != null)
                    {
                        UnityEngine.Debug.Log($"Path InstanceID {this.GetInstanceID()} not intersects with connected path.{TrafficObjectFinderMessage.GetMessage()}");
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"Path InstanceID {this.GetInstanceID()} connected path is null.{TrafficObjectFinderMessage.GetMessage()}");
                    }
                }

                if (found)
                {
                    if (connectedPath.SourceLaneIndex != connectedLaneIndex)
                    {
                        UnityEngine.Debug.Log($"Path InstanceID {this.GetInstanceID()} appears to have the wrong connection lane index. Reattach this path again.{TrafficObjectFinderMessage.GetMessage()}");
                    }
                }
            }
#endif
        }

        public TrafficLightCrossroad GetParent()
        {
            if (sourceTrafficNode && sourceTrafficNode.TrafficLightCrossroad)
                return sourceTrafficNode.TrafficLightCrossroad;

            return null;
        }

        private string GetNodeName(int index)
        {
            return "Node" + (index + 1).ToString();
        }

        private string GetWaypointName(int index)
        {
            return "Waypoint" + (index + 1).ToString();
        }

        #endregion

        #region Editor events

#if UNITY_EDITOR 
        private void RemovePathLinking()
        {
            if (!Application.isPlaying)
            {
                sourceTrafficNode?.TryToRemovePath(this);
                TryToRemoveCustomLight();
            }
        }
#endif

        #endregion
    }
}