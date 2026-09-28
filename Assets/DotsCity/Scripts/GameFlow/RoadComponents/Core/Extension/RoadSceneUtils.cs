using Spirit604.CityEditor.Road;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_SPLINE
using UnityEngine.Splines;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spirit604.CityEditor.Utils
{
    public static class RoadSceneUtils
    {
        public class StraightSegmentSettings : BaseSettings
        {
            public float StartOffset;
            public float EndOffset;

            public bool GenerateInnerTrafficSpawnNodes;
            public bool AddPedestrianAlongLine;

            public bool AutoStripWaypoints;
            public float StripAngle = 5f;
            public float StripDistance = 30f;

            public static StraightSegmentSettings GetDefault() => new StraightSegmentSettings();
        }

        public class BaseSettings
        {
            public int LaneCount = 1;
            public Vector2Int CustomLaneCount;
            public float LaneWidth = 4f;
            public float Offset;
            public float DividerWidth;
            public bool IsOneWay;
            public bool IsEndOfOneWay;
            public bool InverseNodeRotation;
            public bool AddPedestrianNodes = true;
            public bool AddCrosswalk = true;

            public bool HasCrosswalk => AddPedestrianNodes && AddCrosswalk;
            public Vector3 CrosswalkOffset = new Vector3(1, 0, 0);
            public NodeShapeType CrosswalkShapeType = NodeShapeType.Circle;
            public float CrosswalkWidth = 1f;
            public float CrosswalkHeight = 2f;

            public BaseSettings()
            {
            }

            public BaseSettings(TrafficNode trafficNode)
            {
                var crosswalk = trafficNode.TrafficNodeCrosswalk;
                LaneCount = trafficNode.LaneCount;
                LaneWidth = trafficNode.LaneWidth;
                DividerWidth = trafficNode.DividerWidth;
                IsOneWay = trafficNode.IsOneWay;
                IsEndOfOneWay = trafficNode.IsEndOfOneWay;
                AddPedestrianNodes = crosswalk.Enabled;
                AddCrosswalk = crosswalk.HasConnection;
                CrosswalkOffset = crosswalk.CrossWalkOffset;
                CrosswalkShapeType = crosswalk.GetShapeType();
                CrosswalkWidth = crosswalk.GetSize().x;
                CrosswalkHeight = crosswalk.GetSize().y;

                if (trafficNode.LaneCountType == LaneCountType.Custom)
                {
                    CustomLaneCount = new Vector2Int(trafficNode.GetLaneCount(false), trafficNode.GetLaneCount(true));
                }
            }
        }

        public class SegmentSettings : BaseSettings
        {
            public bool ConnectCrosswalks;

            public SegmentSettings() { }

            public SegmentSettings(StraightSegmentSettings straightSegmentSettings)
            {
                this.LaneCount = straightSegmentSettings.LaneCount;
                this.LaneWidth = straightSegmentSettings.LaneWidth;
                this.Offset = straightSegmentSettings.Offset;
                this.DividerWidth = straightSegmentSettings.DividerWidth;
                this.IsOneWay = straightSegmentSettings.IsOneWay;
                this.IsEndOfOneWay = straightSegmentSettings.IsEndOfOneWay;
                this.InverseNodeRotation = straightSegmentSettings.InverseNodeRotation;
                this.AddPedestrianNodes = straightSegmentSettings.AddPedestrianNodes;
                this.AddCrosswalk = straightSegmentSettings.AddCrosswalk;
                this.CrosswalkOffset = straightSegmentSettings.CrosswalkOffset;
                this.CrosswalkShapeType = straightSegmentSettings.CrosswalkShapeType;
                this.CrosswalkWidth = straightSegmentSettings.CrosswalkWidth;
                this.CrosswalkHeight = straightSegmentSettings.CrosswalkHeight;
            }

            public SegmentSettings(bool connectCrosswalks)
            {
                ConnectCrosswalks = connectCrosswalks;
            }

            public static SegmentSettings GetDefault() => new SegmentSettings();
        }

        public class NodeSettings
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public BaseSettings Settings;

            /// <summary> User defined paths, otherwise it will be generated automatically.</summary>
            public List<PathData> Paths;

            public NodeSettings()
            {
            }

            public NodeSettings(Vector3 position, Quaternion rotation, BaseSettings settings, List<PathData> paths = null)
            {
                Position = position;
                Rotation = rotation;
                Settings = settings;
                Paths = paths;
            }

            public NodeSettings(TrafficNode trafficNode, bool inverse = false)
            {
                Position = trafficNode.transform.position;
                Rotation = trafficNode.GetNodeRotation(!inverse ? -1 : 1);
                Settings = new BaseSettings(trafficNode);

                if (inverse)
                {
                    if (trafficNode.IsOneWay)
                    {
                        Settings.IsEndOfOneWay = !trafficNode.IsEndOfOneWay;
                    }

                    if (Settings.CustomLaneCount != default)
                    {
                        var temp = Settings.CustomLaneCount.x;
                        Settings.CustomLaneCount.x = Settings.CustomLaneCount.y;
                        Settings.CustomLaneCount.y = temp;
                    }
                }
            }

            public static NodeSettings GetDefault() => new NodeSettings();
        }

        public class PathData
        {
            public const int EmptyPriority = 10;

            public List<Vector3> Waypoints = new List<Vector3>();

            /// <summary> Start lane index, if value is -1 it will be detected automatically.</summary>
            public int StartLaneIndex = -1;

            /// <summary> Connected lane index, if value is -1 it will be detected automatically.</summary>
            public int ConnectedLaneIndex = -1;

            /// <summary> Connected traffic node index, if value is -1 it will be detected automatically.</summary>
            public int ConnectedNodeIndex = -1;

            /// <summary> Speed limit in metres per second.</summary>
            public float SpeedLimit;

            /// <summary> Order of crossing intersected paths (vehicle with the higher priority gets through first). If value is 10, default value is generated.</summary>
            public int Priority = EmptyPriority;

            public bool DefaultPriority => Priority == EmptyPriority;
        }

        public static RoadSegmentCreator CreateIntersection(List<NodeSettings> nodeList, SegmentSettings segmentSettings = null, string segmentName = "")
        {
            var creator = CreateSegmentInternal(RoadSegmentCreator.RoadSegmentType.CustomSegment, nodeList, segmentName);
            CreateIntersection(creator, nodeList, segmentSettings: segmentSettings);
            return creator;
        }

        public static void CreateIntersection(RoadSegmentCreator creator, List<NodeSettings> nodeList, SegmentSettings segmentSettings = null)
        {
            CreateIntersectionInternal(creator, nodeList, segmentSettings: segmentSettings);
        }

        public static RoadSegmentCreator CreateStraightSegment(List<NodeSettings> nodeList, StraightSegmentSettings straightSegmentSettings = null, string segmentName = "")
        {
            var creator = CreateSegmentInternal(RoadSegmentCreator.RoadSegmentType.CustomStraightRoad, nodeList, segmentName);
            CreateStraightSegment(creator, nodeList, straightSegmentSettings);
            return creator;
        }

        public static RoadSegmentCreator CreateStraightSegment(List<Vector3> middlePoints, StraightSegmentSettings straightSegmentSettings, List<NodeSettings> nodeList = null, string segmentName = "")
        {
            var position = (middlePoints[0] + middlePoints[middlePoints.Count - 1]) / 2;
            var creator = CreateSegmentInternal(position, segmentName);
            CreateStraightSegment(creator, middlePoints, straightSegmentSettings, nodeList, true);
            return creator;
        }

        public static void CreateStraightSegment(RoadSegmentCreator creator, List<NodeSettings> nodeList, StraightSegmentSettings straightSegmentSettings = null)
        {
            if (nodeList == null)
            {
                throw new ArgumentException($"CreateStraightSegment. Segment '{creator.name}' node list is null");
            }

            if (nodeList.Count != 2)
            {
                throw new ArgumentException($"CreateStraightSegment. Segment '{creator.name}' node list should have exactly 2 nodes");
            }

            var oneway = false;
            int laneCount;

            var node1 = nodeList[0];
            var node2 = nodeList[1];

            if (straightSegmentSettings != null)
            {
                oneway = straightSegmentSettings.IsOneWay;
                laneCount = straightSegmentSettings.LaneCount;
            }
            else
            {
                oneway = node1.Settings.IsOneWay;
                laneCount = node1.Settings.LaneCount;

                straightSegmentSettings.IsOneWay = oneway;
                straightSegmentSettings.LaneCount = laneCount;

                if (node1.Settings == null)
                {
                    throw new ArgumentException($"CreateStraightSegment. Segment '{creator.name}' Node1 <b>NodeSettings</b> is null");
                }

                if (node2.Settings == null)
                {
                    throw new ArgumentException($"CreateStraightSegment. Segment '{creator.name}' Node2 <b>NodeSettings</b> is null");
                }
            }

            bool matched = true;

            for (int i = 0; i < node1.Paths.Count; i++)
            {
                var waypoints = node1.Paths[i].Waypoints;

                for (int j = 0; j < node2.Paths.Count; j++)
                {
                    var waypoints2 = node2.Paths[j].Waypoints;

                    if (waypoints.Count != waypoints2.Count)
                    {
                        matched = false;
                        break;
                    }
                }

                if (!matched)
                    break;
            }

            if (matched)
            {
                for (int i = 0; i < node1.Paths.Count; i++)
                {
                    var waypoints = node1.Paths[i].Waypoints;

                    for (int j = 0; j < node1.Paths.Count; j++)
                    {
                        if (i == j) continue;

                        var waypoints2 = node1.Paths[j].Waypoints;

                        if (waypoints.Count != waypoints2.Count)
                        {
                            matched = false;
                            break;
                        }
                    }

                    if (!matched)
                        break;
                }
            }

            if (!matched)
            {
                var settings = new SegmentSettings(straightSegmentSettings);

                for (int i = 0; i < nodeList.Count; i++)
                {
                    NodeSettings item = nodeList[i];
                    item.Settings.Offset = -1 * item.Settings.Offset;
                }

                if (nodeList[0].Settings.IsOneWay)
                {
                    nodeList[1].Settings.IsEndOfOneWay = true;
                }

                CreateIntersection(creator, nodeList, settings);
                return;
            }

            var middlePoints = new List<Vector3>();

            List<Vector3> lane1 = null;
            List<Vector3> lane2 = null;

            bool avgLane = false;
            bool opposite = true;

            if (!oneway)
            {
                lane1 = node1.Paths[0].Waypoints;
                lane2 = node2.Paths[0].Waypoints;
                avgLane = true;
            }
            else
            {
                if (laneCount % 2 != 0)
                {
                    foreach (var waypoint in nodeList[0].Paths[laneCount / 2].Waypoints)
                    {
                        middlePoints.Add(waypoint);
                    }
                }
                else
                {
                    opposite = false;
                    avgLane = true;

                    lane1 = node1.Paths[laneCount / 2 - 1].Waypoints;
                    lane2 = node1.Paths[laneCount / 2].Waypoints;
                }
            }

            if (avgLane)
            {
                var min = MathF.Min(lane1.Count, lane2.Count);

                for (int i = 0; i < min; i++)
                {
                    var currentWaypoint = lane1[i];
                    int laneOpposite = opposite ? lane2.Count - 1 - i : i;

                    var oppositeWaypoint = lane2[laneOpposite];

                    var point = (currentWaypoint + oppositeWaypoint) / 2;

                    middlePoints.Add(point);
                }
            }

            CreateStraightSegment(creator, middlePoints, straightSegmentSettings, nodeList, true);
        }

        public static RoadSegmentCreator CreateIntersection(List<RoadSegmentCreator> straightRoadCreators, SegmentSettings segmentSettings = null, string segmentName = "")
        {
            if (straightRoadCreators.Count < 2)
                throw new ArgumentException($"CreateIntersection. The number of segments should be 2 or more.");

            var nodeIndexes = new int[straightRoadCreators.Count];
            var tempConnectionNodes = new TrafficNode[straightRoadCreators.Count];
            var connectionNodes = new TrafficNode[straightRoadCreators.Count];

            float maxDistance = float.MaxValue;

            int count = 0;
            int maxCount = straightRoadCreators[0].CreatedTrafficNodes.Count;

            for (int i = 1; i < straightRoadCreators.Count; i++)
            {
                maxCount *= straightRoadCreators[i].CreatedTrafficNodes.Count;
            }

            while (count <= maxCount)
            {
                for (int i = 0; i < straightRoadCreators.Count; i++)
                {
                    tempConnectionNodes[i] = straightRoadCreators[i].TryToGetNode(nodeIndexes[i]);
                }

                Vector3 avgPoint = default;

                for (int n = 0; n < tempConnectionNodes.Length; n++)
                {
                    avgPoint += tempConnectionNodes[n].transform.position;
                }

                avgPoint /= tempConnectionNodes.Length;
                float currentDistance = 0;

                for (int n = 0; n < tempConnectionNodes.Length; n++)
                {
                    currentDistance += Vector3.SqrMagnitude(avgPoint - tempConnectionNodes[n].transform.position);
                }

                if (currentDistance < maxDistance)
                {
                    maxDistance = currentDistance;

                    for (int n = 0; n < connectionNodes.Length; n++)
                    {
                        connectionNodes[n] = tempConnectionNodes[n];
                    }
                }

                IterateNodes(straightRoadCreators, nodeIndexes);
                count++;
            }

            return CreateIntersection(connectionNodes, segmentSettings, true, segmentName);
        }

        public static RoadSegmentCreator CreateIntersection(IEnumerable<TrafficNode> connectionNodes, SegmentSettings segmentSettings = null, bool autoMerge = true, string segmentName = "")
        {
            var nodeList = new List<NodeSettings>();

            foreach (var item in connectionNodes)
            {
                var settings = new NodeSettings(item, true);
                settings.Settings.AddCrosswalk = true;
                nodeList.Add(settings);
            }

            if (segmentSettings == null)
            {
                segmentSettings = SegmentSettings.GetDefault();
                segmentSettings.ConnectCrosswalks = true;
            }

            var creator = CreateIntersection(nodeList, segmentSettings, segmentName);

            if (autoMerge)
            {
                int index = 0;

                foreach (var sourceNode in connectionNodes)
                {
                    var connectedNode = creator.TryToGetNode(index);

                    sourceNode.SetMerge(connectedNode);
                    index++;
                }
            }

            return creator;
        }

#if UNITY_SPLINE

        public static RoadSegmentCreator CreateIntersection(List<SplineContainer> splineContainers, List<NodeSettings> nodeSettingsList = null, SegmentSettings segmentSettings = null, string segmentName = "")
        {
            if (nodeSettingsList != null && nodeSettingsList.Count != splineContainers.Count)
                throw new ArgumentException($"CreateIntersection. Node settings list should match number with splines list.");

            var nodeIndexes = new int[splineContainers.Count];
            var tempKnotes = new int[splineContainers.Count];
            var closestKnotIndexes = new int[splineContainers.Count];

            float maxDistance = float.MaxValue;

            int count = 0;
            int maxCount = Mathf.RoundToInt(Mathf.Pow(2, splineContainers.Count));

            while (count <= maxCount)
            {
                for (int i = 0; i < splineContainers.Count; i++)
                {
                    tempKnotes[i] = (nodeIndexes[i] == 0 ? 0 : splineContainers[i].Spline.Knots.Count() - 1);
                }

                Vector3 avgPoint = default;

                for (int n = 0; n < tempKnotes.Length; n++)
                {
                    avgPoint += GetKnotPosition(splineContainers, tempKnotes, n);
                }

                avgPoint /= tempKnotes.Length;
                float currentDistance = 0;

                for (int n = 0; n < tempKnotes.Length; n++)
                {
                    currentDistance += Vector3.SqrMagnitude(avgPoint - GetKnotPosition(splineContainers, tempKnotes, n));
                }

                if (currentDistance < maxDistance)
                {
                    maxDistance = currentDistance;

                    for (int n = 0; n < closestKnotIndexes.Length; n++)
                    {
                        closestKnotIndexes[n] = tempKnotes[n];
                    }
                }

                IterateKnots(nodeIndexes);
                count++;
            }

            var nodeList = new List<RoadSceneUtils.NodeSettings>();

            for (int i = 0; i < closestKnotIndexes.Length; i++)
            {
                var settings = new NodeSettings();

                var t = closestKnotIndexes[i] == 0 ? 0f : 1f;
                Vector3 tangent = GetTangent(splineContainers[i], t);

                settings.Position = GetKnotPosition(splineContainers, closestKnotIndexes, i);
                settings.Rotation = Quaternion.LookRotation(tangent, Vector3.up);

                if (t != 0)
                {
                    settings.Rotation = settings.Rotation * Quaternion.Euler(0, 180, 0);
                }

                if (nodeSettingsList != null)
                {
                    settings.Settings = nodeSettingsList[i].Settings;
                }

                nodeList.Add(settings);
            }

            var intersection = CreateIntersection(nodeList, segmentSettings, segmentName);
            TrafficNode.AutoConnectionSettings connectionSettings = GetMergeSettings();

            intersection.IterateAllNodes(node =>
            {
                node.ConnectSegments(connectionSettings);
            });

            return intersection;
        }

        private static Vector3 GetTangent(SplineContainer splineContainer, float t)
        {
            return splineContainer.transform.TransformDirection(SplineUtility.EvaluateTangent(splineContainer.Spline, t));
        }

        public static RoadSegmentCreator CreateStraightSegment(SplineContainer splineContainer, StraightSegmentSettings segmentSettings, List<NodeSettings> nodeList = null, float resolution = 2f, string segmentName = "")
        {
            var positions = new List<Vector3>();

            var length = splineContainer.CalculateLength();
            var splineSegmentCount = Mathf.Max(2, Mathf.RoundToInt(length * resolution / 10f));

            for (int i = 0; i <= splineSegmentCount; i++)
            {
                float t = (float)i / splineSegmentCount;

                var pos = SplineUtility.EvaluatePosition(splineContainer[0], t);

                pos = splineContainer.transform.TransformPoint(pos);
                positions.Add(pos);
            }

            if (nodeList == null)
            {
                nodeList = new List<NodeSettings>();
                nodeList.Add(new NodeSettings());
                nodeList.Add(new NodeSettings());
            }

            nodeList[0].Rotation = Quaternion.LookRotation(-SplineUtility.EvaluateTangent(splineContainer.Spline, 0));
            nodeList[1].Rotation = Quaternion.LookRotation(SplineUtility.EvaluateTangent(splineContainer.Spline, 1));

            var straighRoad = CreateStraightSegment(positions, segmentSettings, nodeList, segmentName: segmentName);

            TrafficNode.AutoConnectionSettings settings = GetMergeSettings();

            straighRoad.IterateAllNodes(node =>
                    {
                        node.ConnectSegments(settings);
                    });

            return straighRoad;
        }

        private static Vector3 GetKnotPosition(List<SplineContainer> splines, int[] tempConnectionNodes, int n)
        {
            var pos = (Vector3)splines[n].Spline.Knots.ElementAt(tempConnectionNodes[n]).Position;

            return splines[n].transform.TransformPoint(pos);
        }

        private static void IterateKnots(int[] nodeIndexes)
        {
            var index = nodeIndexes.Length - 1;

            nodeIndexes[index]++;

            while (true)
            {
                if (nodeIndexes[0] >= 2)
                {
                    nodeIndexes[0] = 0;
                    break;
                }

                if (nodeIndexes[index] >= 2)
                {
                    while (index - 1 >= 0)
                    {
                        nodeIndexes[index] = 0;
                        index = index - 1;
                        nodeIndexes[index]++;

                        if (nodeIndexes[index] < 2)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    break;
                }
            }
        }

#endif

        public static void CreateStraightSegment(RoadSegmentCreator creator, List<Vector3> middlePoints, StraightSegmentSettings segmentSettings = null, List<NodeSettings> nodeList = null, bool insertPoints = false)
        {
            if (middlePoints.Count < 2)
                throw new ArgumentException("The number of points in the array should be greater than or equal to 2.");

            if (nodeList != null && nodeList.Count != 2)
                throw new ArgumentException("Node list should contain only 2 elements");

            if (segmentSettings == null)
                segmentSettings = StraightSegmentSettings.GetDefault();

            if (segmentSettings.AutoStripWaypoints)
            {
                RoadUtils.StripPoints(middlePoints, segmentSettings.StripAngle, segmentSettings.StripDistance);
            }

            var pos = creator.transform.position;

            creator.laneCount = segmentSettings.LaneCount;

            if (segmentSettings.CustomLaneCount != default)
            {
                creator.laneCount = segmentSettings.CustomLaneCount.x;
                creator.backwardLaneCount = segmentSettings.CustomLaneCount.y;
                creator.laneCountType = LaneCountType.Custom;
            }

            creator.LaneWidth = segmentSettings.LaneWidth;
            creator.dividerWidth = segmentSettings.DividerWidth;
            creator.oneWay = segmentSettings.IsOneWay;
            creator.shouldRevertDirection = segmentSettings.IsEndOfOneWay;
            creator.SetAllCrosswalkEnabled(segmentSettings.HasCrosswalk);

            creator.Create(RoadSegmentCreator.RoadSegmentType.StraightRoad);
            creator.Create(RoadSegmentCreator.RoadSegmentType.CustomStraightRoad);

            creator.transform.position = pos;

            var lastIndex = middlePoints.Count - 1;

            int skipped = 0;
            int skippedLast = 0;

            var node1 = creator.TryToGetNode(0);
            var node2 = creator.TryToGetNode(1);

            ApplyNodeSettings(creator, node1, segmentSettings, true);
            ApplyNodeSettings(creator, node2, segmentSettings, true);

            if (node2.IsOneWay)
            {
                node2.IsEndOfOneWay = true;
            }

            EditorSaver.SetObjectDirty(node1);
            EditorSaver.SetObjectDirty(node2);

            var distance = GetDistance(middlePoints);

            var startCutOffset = segmentSettings.StartOffset;
            var endCutOffset = segmentSettings.EndOffset;

            const float multiplier = 1.5f;

            bool cutAvailable = distance > (startCutOffset * multiplier + endCutOffset * multiplier);

            if (!cutAvailable)
            {
                startCutOffset /= 2;
                endCutOffset /= 2;

                cutAvailable = distance > (startCutOffset * multiplier + endCutOffset * multiplier);
            }

            if (startCutOffset > 0 && cutAvailable)
            {
                RoadSceneUtils.CutLine(middlePoints, false, startCutOffset, out var nodePos1, out var nodeRot2, out skipped);
                node1.transform.SetPositionAndRotation(nodePos1, nodeRot2);
            }
            else
            {
                if (startCutOffset < 0)
                {
                    middlePoints[0] = middlePoints[0] - startCutOffset * Vector3.Normalize(middlePoints[0] - middlePoints[1]);
                }

                Quaternion rotation = nodeList != null && nodeList[0].Rotation != default ? nodeList[0].Rotation : GetNodeRotation(middlePoints, false);

                node1.transform.SetPositionAndRotation(middlePoints[0], rotation);
            }

            if (endCutOffset > 0 && cutAvailable)
            {
                RoadSceneUtils.CutLine(middlePoints, true, endCutOffset, out var nodePos2, out var nodeRot2, out skippedLast);
                node2.transform.SetPositionAndRotation(nodePos2, nodeRot2);
            }
            else
            {
                if (endCutOffset < 0)
                {
                    middlePoints[middlePoints.Count - 1] = middlePoints[middlePoints.Count - 1] - endCutOffset * Vector3.Normalize(middlePoints[middlePoints.Count - 1] - middlePoints[middlePoints.Count - 2]);
                }

                Quaternion rotation = nodeList != null && nodeList[1].Rotation != default ? nodeList[1].Rotation : GetNodeRotation(middlePoints, true);

                node2.transform.SetPositionAndRotation(middlePoints[middlePoints.Count - 1], rotation);
            }

            lastIndex -= skippedLast;

            var tempPath = creator.GetTempPath(0);

            tempPath.Nodes[0].transform.rotation = node1.transform.rotation;
            tempPath.Nodes[tempPath.Nodes.Count - 1].transform.rotation = RoadSceneUtils.GetNodeRotation(middlePoints, lastIndex);

            if (insertPoints)
            {
                int index = 1;

                for (int i = 1 + skipped; i < middlePoints.Count - 1 - skippedLast; i++)
                {
                    var pathNode = tempPath.InsertNode(middlePoints[i], index, false);

                    pathNode.transform.rotation = RoadSceneUtils.GetNodeRotation(middlePoints, i, true);
                    index++;
                }
            }

            for (int i = 0; i < skipped; i++)
            {
                tempPath.RemoveNodeAt(1, false, false);
            }

            for (int i = 0; i < skippedLast; i++)
            {
                tempPath.RemoveNodeAt(tempPath.Nodes.Count - 1, false, false);
            }

            creator.addPedestrianNodes = segmentSettings.AddPedestrianNodes;
            creator.customCrossWalkOffset = segmentSettings.CrosswalkOffset;
            creator.crossWalkNodeShape = segmentSettings.CrosswalkShapeType;
            creator.crossroadWidth = segmentSettings.CrosswalkWidth;
            creator.crosswalkNodeHeight = segmentSettings.CrosswalkHeight;

            creator.generateSpawnNodes = segmentSettings.GenerateInnerTrafficSpawnNodes;
            creator.addAlongLine = segmentSettings.AddPedestrianAlongLine;

#if UNITY_EDITOR
            creator.OnCrosswalkSettingsChanged();
#endif

            creator.RecalculateCustomPath(true, false);
            creator.Recalculate(false);

            tempPath.RenameNodes();
        }

        public static void CutLine(List<Vector3> points, bool reverseDirection, float segmentOffset, out Vector3 cornerNodePos, out Quaternion cornerNodeRot, out int cutPointCount)
        {
            var totalDistance = 0f;
            var remainOffset = segmentOffset;
            cutPointCount = 0;

            if (!reverseDirection)
            {
                cornerNodePos = points[0];
                cornerNodeRot = GetNodeRotation(points, 0, reverseDirection);

                for (int i = 0; i < points.Count - 1; i++)
                {
                    var distance = Vector3.Distance(points[i], points[i + 1]);
                    totalDistance += distance;

                    if (totalDistance >= segmentOffset)
                    {
                        var sourcePoint = points[i] + (points[i + 1] - points[i]).normalized * remainOffset;
                        cornerNodePos = sourcePoint;
                        cornerNodeRot = GetNodeRotation(points, i, reverseDirection);
                        break;
                    }
                    else
                    {
                        cutPointCount++;
                        remainOffset -= distance;
                    }
                }
            }
            else
            {
                cornerNodePos = points[points.Count - 1];
                cornerNodeRot = GetNodeRotation(points, points.Count - 2, reverseDirection);

                for (int i = points.Count - 1; i > 0; i--)
                {
                    var distance = Vector3.Distance(points[i - 1], points[i]);
                    totalDistance += distance;

                    if (totalDistance >= segmentOffset)
                    {
                        var sourcePoint = points[i] + (points[i - 1] - points[i]).normalized * remainOffset;
                        cornerNodePos = sourcePoint;
                        cornerNodeRot = GetNodeRotation(points, i, reverseDirection);
                        break;
                    }
                    else
                    {
                        remainOffset -= distance;
                        cutPointCount++;
                    }
                }
            }
        }

        public static Quaternion GetNodeRotation(List<Vector3> points, bool lastNodeSpline, bool inverse = false)
        {
            var index = !lastNodeSpline ? 0 : points.Count - 1;

            if (inverse)
            {
                lastNodeSpline = !lastNodeSpline;
            }

            return GetNodeRotation(points, index, lastNodeSpline);
        }

        public static Quaternion GetNodeRotation(List<Vector3> points, int index, bool inverse = false)
        {
            if (points.Count < 2)
                return Quaternion.identity;

            Quaternion rot = Quaternion.identity;

            if (index + 1 < points.Count)
            {
                rot = Quaternion.LookRotation((points[index] - points[index + 1]).normalized);
            }
            else
            {
                rot = Quaternion.LookRotation((points[index - 1] - points[index]).normalized, Vector3.up);
            }

            if (inverse)
            {
                rot = rot * Quaternion.Euler(0, 180, 0);
            }

            return rot;
        }

        public static Path CreatePath(RoadSegmentCreator roadSegmentCreator, TrafficNode sourceNode, PathData pathData)
        {
            var pathPrefab = roadSegmentCreator.roadSegmentCreatorConfig.PathPrefab;
            var path = GameObject.Instantiate(pathPrefab, sourceNode.transform.position, sourceNode.transform.rotation);

            path.SourceTrafficNode = sourceNode;

            var startPos = pathData.Waypoints[0];
            var endPos = pathData.Waypoints[pathData.Waypoints.Count - 1];

            var startClosestLane = path.SourceTrafficNode.GetClosestLane(startPos);

            if (pathData.StartLaneIndex == -1)
            {
                pathData.StartLaneIndex = startClosestLane.Item1;
            }

            if (pathData.ConnectedNodeIndex != -1)
            {
                path.ConnectedTrafficNode = roadSegmentCreator.TryToGetNode(pathData.ConnectedNodeIndex);
            }
            else
            {
                path.ConnectedTrafficNode = roadSegmentCreator.TryToGetClosestNode(endPos, true);
            }

            var endClosestLane = path.ConnectedTrafficNode.GetClosestLane(endPos);

            if (pathData.ConnectedLaneIndex != -1)
            {
                path.ConnectedLaneIndex = pathData.ConnectedLaneIndex;
            }
            else
            {
                path.ConnectedLaneIndex = endClosestLane.Item1;
                pathData.ConnectedLaneIndex = endClosestLane.Item1;
            }

            pathData.Waypoints[0] = path.SourceTrafficNode.GetLanePosition(startClosestLane.Item1, startClosestLane.Item2);
            pathData.Waypoints[pathData.Waypoints.Count - 1] = path.ConnectedTrafficNode.GetLanePosition(endClosestLane.Item1, endClosestLane.Item2);

            CreatePathInternal(path, pathData.Waypoints);

            path.name = $"Path_{pathData.StartLaneIndex}_{pathData.ConnectedLaneIndex}";
            path.PathSpeedLimit = pathData.SpeedLimit;

            if (!pathData.DefaultPriority)
                path.Priority = pathData.Priority;

            path.ResetSpeedLimit(false);

            EditorSaver.SetObjectDirty(path);

            return path;
        }

        public static Path CreatePath(List<Vector3> waypoints)
        {
            var path = GameObject.Instantiate(GetPathPrefab());

            CreatePathInternal(path, waypoints);
            return path;
        }

        public static Path CreatePathBezierCube(TrafficNode sourceNode, TrafficNode targetNode, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var path = CreatePath(PathCurveType.BezierCube, sourceNode, targetNode, p1, p3);

            path.Nodes[1].transform.position = p2;
            return path;
        }

        public static Path CreatePathBezierQuad(TrafficNode sourceNode, TrafficNode targetNode, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            var path = CreatePath(PathCurveType.BezierQuad, sourceNode, targetNode, p1, p4);
            path.Nodes[1].transform.position = p2;
            path.Nodes[2].transform.position = p3;

            return path;
        }

        public static void CreateUTurnPaths(TrafficNode sourceNode, float stretchFactor = 1f, float angle = 0f, int direction = 1)
        {
            if (sourceNode.IsOneWay)
            {
                sourceNode.TrafficNodeType = TrafficNodeType.DestroyVehicle;
                EditorSaver.SetObjectDirty(sourceNode);
                return;
            }

            for (var laneIndex = 0; laneIndex < sourceNode.GetLaneCount(true); laneIndex++)
            {
                RoadSceneUtils.CreateUTurnPath(sourceNode, laneIndex);
            }
        }

        public static Path CreateUTurnPath(TrafficNode sourceNode, int laneIndex, float stretchFactor = 1f, float angle = 0f, int direction = 1)
        {
#if UNITY_EDITOR

            var deadEnd = direction == 1;
            var pointA = sourceNode.GetLanePosition(laneIndex, deadEnd);

            var connectedLaneIndex = Mathf.Clamp(laneIndex, 0, sourceNode.GetLaneCount(!deadEnd) - 1);
            var pointB = sourceNode.GetLanePosition(connectedLaneIndex, !deadEnd);

            var controlPoints = PathAttachHelper.GetArcControlPoints(pointA, pointB, stretchFactor, angle, direction);

            var path = RoadSceneUtils.CreatePathBezierQuad(sourceNode, sourceNode, pointA, controlPoints.Item1, controlPoints.Item2, pointB);

            path.ConnectedLaneIndex = connectedLaneIndex;
            path.CreatePath(true);

            sourceNode.AddPath(path, laneIndex, false, deadEnd);

            EditorSaver.SetObjectDirty(sourceNode);
            return path;
#else
            return null;
#endif
        }

        public static Path CreatePath(PathCurveType pathCurveType, TrafficNode sourceNode, TrafficNode targetNode, Vector3 sourcePosition, Vector3 targetPosition)
        {
            var path = GameObject.Instantiate(GetPathPrefab());

            path.transform.parent = sourceNode.PathParent;
            path.transform.localPosition = Vector3.zero;
            path.transform.localRotation = Quaternion.identity;

            path.SourceTrafficNode = sourceNode;
            path.ConnectedTrafficNode = targetNode;
            path.PathCurveType = pathCurveType;

            Transform[] nodes = null;

            switch (pathCurveType)
            {
                case PathCurveType.StraightLine:
                    {
                        nodes = new Transform[2];
                        nodes[0] = new GameObject("Node1").transform;
                        nodes[1] = new GameObject("Node2").transform;

                        nodes[0].transform.position = sourcePosition;
                        nodes[1].transform.position = targetPosition;
                        break;
                    }
                case PathCurveType.BezierCube:
                    {
                        path.WayPointsCountPerCurve = 10;

                        nodes = new Transform[3];
                        nodes[0] = new GameObject("Node1").transform;
                        nodes[1] = new GameObject("Node2").transform;
                        nodes[2] = new GameObject("Node3").transform;

                        nodes[0].transform.position = sourcePosition;
                        nodes[2].transform.position = targetPosition;

                        break;
                    }
                case PathCurveType.BezierQuad:
                    {
                        path.WayPointsCountPerCurve = 10;

                        nodes = new Transform[4];
                        nodes[0] = new GameObject("Node1").transform;
                        nodes[1] = new GameObject("Node2").transform;
                        nodes[2] = new GameObject("Node3").transform;
                        nodes[3] = new GameObject("Node4").transform;

                        nodes[0].transform.position = sourcePosition;
                        nodes[3].transform.position = targetPosition;
                        break;
                    }
            }

            path.Nodes = nodes.ToList();

            return path;
        }

        public static Path GetPathPrefab()
        {
#if UNITY_EDITOR
            return (AssetDatabase.LoadAssetAtPath(CityEditorBookmarks.PATH_PREFAB_PATH, typeof(GameObject)) as GameObject).GetComponent<Path>();
#else
            return null;
#endif
        }

        private static void IterateNodes(List<RoadSegmentCreator> straightRoadCreators, int[] nodeIndexes)
        {
            var index = nodeIndexes.Length - 1;

            nodeIndexes[index]++;

            while (true)
            {
                if (nodeIndexes[0] >= straightRoadCreators[0].TrafficNodeCount)
                {
                    nodeIndexes[0] = 0;
                    break;
                }

                if (nodeIndexes[index] >= straightRoadCreators[index].TrafficNodeCount)
                {
                    while (index - 1 >= 0)
                    {
                        nodeIndexes[index] = 0;
                        index = index - 1;
                        nodeIndexes[index]++;

                        if (nodeIndexes[index] < straightRoadCreators[index].TrafficNodeCount)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private static void CreateIntersectionInternal(RoadSegmentCreator creator, List<NodeSettings> nodeList, SegmentSettings segmentSettings = null)
        {
            if (segmentSettings == null)
                segmentSettings = SegmentSettings.GetDefault();

            creator.Create(RoadSegmentCreator.RoadSegmentType.CustomSegment);

            if (nodeList?.Count > 0 && nodeList[0].Settings != null)
            {
                var addCrosswalk = nodeList[0].Settings.AddCrosswalk;

                for (int i = 0; i < nodeList.Count; i++)
                {
                    if (nodeList[i].Settings != null)
                    {
                        creator.SetCrosswalkEnabledState(i, nodeList[i].Settings.AddCrosswalk);

                        if (nodeList[i].Settings.AddCrosswalk != addCrosswalk)
                        {
                            creator.customCrossWalk = true;
                        }
                    }
                }
            }

            creator.connectCrosswalks = segmentSettings.ConnectCrosswalks;
            creator.customTrafficNodeSettings = true;

            creator.EnsureTrafficNodes(nodeList.Count);

            if (creator.TrafficNodeCount == 2)
            {
                creator.trafficLightCrossroad.HasLights = false;
                EditorSaver.SetObjectDirty(creator.trafficLightCrossroad);
            }

            for (int i = 0; i < nodeList.Count; i++)
            {
                var node = creator.TryToGetNode(i);

                node.transform.rotation = nodeList[i].Rotation;

                BaseSettings currentSettings = segmentSettings;

                if (nodeList[i].Settings != null)
                {
                    currentSettings = nodeList[i].Settings;
                }

                if (currentSettings.InverseNodeRotation)
                {
                    node.transform.rotation *= Quaternion.Euler(0f, 180f, 0f);
                }

                node.transform.position = nodeList[i].Position + node.transform.forward * currentSettings.Offset;
                ApplyNodeSettings(creator, node, currentSettings);
            }

            if (creator.TrafficNodeCount > 2)
            {
                var crossroad = creator.trafficLightCrossroad;
                var node1 = creator.TryToGetNode(0);
                var lightIndex = node1.TrafficLightHandler.RelatedLightIndex;

                for (int i = 1; i < creator.TrafficNodeCount; i++)
                {
                    var node2 = creator.TryToGetNode(i);

                    var dir = Vector3.Normalize(node1.transform.position - node2.transform.position);
                    var angle = Vector3.Angle(node1.transform.forward, dir);

                    if (angle > 180)
                    {
                        angle -= 180;
                    }

                    const float angleOffset = 20;

                    var currentIndex = angle < angleOffset || angle > 180 - angleOffset ? lightIndex : (lightIndex + 1) % 2;
                    var nodeIndex = node2.TrafficLightHandler.RelatedLightIndex;

                    if (nodeIndex != currentIndex)
                    {
                        crossroad.RemoveNode(node2);
                        crossroad.AddLight(node2, currentIndex);

                        var revertedIndex = (currentIndex + 1) % 2;

                        var pedNodeLight = crossroad.GetTrafficLightHandler(revertedIndex);
                        node2.TrafficNodeCrosswalk.SetLight(pedNodeLight);
                    }
                }
            }

            bool hashPathData = false;

            for (int i = 0; i < nodeList.Count; i++)
            {
                var node = creator.TryToGetNode(i);

                if (nodeList[i].Paths?.Count > 0)
                {
                    node.ClearPaths(false, false, true);
                    var pathDatas = nodeList[i].Paths;

                    foreach (var pathData in pathDatas)
                    {
                        Path path = CreatePath(creator, node, pathData);

                        node.AddPath(path, pathData.StartLaneIndex, true);
                    }

                    hashPathData = true;
                }
                else
                {
                    if (hashPathData && (!node.IsOneWay || !node.IsEndOfOneWay))
                    {
                        Debug.Log($"CreateIntersectionInternal. Segment '{creator.name}' {node.name} missing path data");
                    }
                }
            }

            if (!hashPathData)
            {
                creator.CreateAutoCrossroad();
            }

            if (segmentSettings.ConnectCrosswalks)
            {
#if UNITY_EDITOR
                creator.ProcessCrosswalkConnections();
#endif
            }
        }

        private static void ApplyNodeSettings(RoadSegmentCreator creator, TrafficNode node, BaseSettings currentSettings, bool straight = false)
        {
            node.LaneCount = currentSettings.LaneCount;
            node.LaneWidth = currentSettings.LaneWidth;
            node.IsOneWay = currentSettings.IsOneWay;

            if (currentSettings.CustomLaneCount != default && !straight)
            {
                node.LaneCountType = LaneCountType.Custom;
                node.LaneCount = currentSettings.CustomLaneCount.x;
                node.ExternalLaneCount = currentSettings.CustomLaneCount.y;
            }

            if (currentSettings.IsOneWay)
            {
                node.IsEndOfOneWay = currentSettings.IsEndOfOneWay;

                if (node.IsEndOfOneWay)
                {
                    node.ClearPaths(false, false, true);
                }
            }

            node.DividerWidth = currentSettings.DividerWidth > 0.01f ? currentSettings.DividerWidth : 0;

            var crosswalk = node.TrafficNodeCrosswalk;
            crosswalk.SwitchEnabledState(currentSettings.AddPedestrianNodes);
            crosswalk.CrossWalkOffset = currentSettings.CrosswalkOffset;
            crosswalk.SetType(currentSettings.CrosswalkShapeType);
            crosswalk.SetCustomWidth(currentSettings.CrosswalkWidth, currentSettings.CrosswalkHeight);

            if (creator.customCrossWalk)
            {
                creator.customCrossWalkOffset = currentSettings.CrosswalkOffset;
            }

            creator.SetCrosswalkEnabledState(node, currentSettings.HasCrosswalk);
            crosswalk.SetCrosswalkPosition();

            crosswalk.SwitchConnectionState(currentSettings.HasCrosswalk);
            node.Resize();
        }

        private static void CreatePathInternal(Path path, List<Vector3> waypoints)
        {
            for (int i = 0; i < waypoints.Count; i++)
            {
                path.AddNode(waypoints[i], false);
            }

            path.CreatePath(true, false);
        }

        private static RoadSegmentCreator CreateSegmentInternal(RoadSegmentCreator.RoadSegmentType segmentType, List<NodeSettings> points, string name = "")
        {
#if UNITY_EDITOR
            Vector3 position = default;

            if (segmentType == RoadSegmentCreator.RoadSegmentType.CustomSegment)
            {
                position = GetInstersectionPos(points);
            }
            else
            {
                position = GetSplinePos(points);
            }

            return CreateSegmentInternal(position, name);
#else
            return null;
#endif
        }

        private static RoadSegmentCreator CreateSegmentInternal(Vector3 position, string name = "")
        {
#if UNITY_EDITOR
            var segment = CreateRoadSegmentCreator(position, name).GetComponent<RoadSegmentCreator>();

            return segment;
#else
            return null;
#endif
        }

        private static Vector3 GetInstersectionPos(List<NodeSettings> nodeList)
        {
            Vector3 averagePosition = Vector3.zero;

            foreach (var point in nodeList)
            {
                averagePosition += point.Position;
            }

            averagePosition /= nodeList.Count;

            return averagePosition;
        }

        private static Vector3 GetSplinePos(List<NodeSettings> nodeList)
        {
            return (nodeList[0].Position + nodeList[nodeList.Count - 1].Position) / 2;
        }

        private static float GetDistance(List<Vector3> middlePoints)
        {
            float distance = 0;

            for (int i = 0; i < middlePoints.Count - 1; i++)
            {
                Vector3 item = middlePoints[i];
                Vector3 item2 = middlePoints[i + 1];

                distance += Vector3.Distance(item, item2);
            }

            return distance;
        }

        private static TrafficNode.AutoConnectionSettings GetMergeSettings()
        {
            return new TrafficNode.AutoConnectionSettings()
            {
                AllowMerge = true,
                MaxConnectionLength = 3f,
                MergeDistance = 2
            };
        }

#if UNITY_EDITOR
        private static GameObject CreateRoadSegmentCreator(Vector3 position, string name = "")
        {
            var roadSegment = CreatePrefab(CityEditorBookmarks.ROAD_SEGMENT_PREFAB_PATH, position, true,
                onCreate: (obj) =>
                {
                    var selectedObject = Selection.activeGameObject;

                    if (selectedObject != null && selectedObject.TryGetComponent<RoadSegmentCreator>(out var oldCreator))
                    {
                        oldCreator.OnInspectorDisabled();
                    }

                    var creator = obj.GetComponent<RoadSegmentCreator>();
                    creator.OnCreateSegment();
                    creator.Create();
                });

            if (roadSegment)
            {
                if (string.IsNullOrEmpty(name))
                {
                    var count = ObjectUtils.FindObjectsOfType<RoadSegment>().Length;
                    var indexText = count.ToString();
                    name = $"{roadSegment.name}{indexText}";
                }

                roadSegment.name = name;
            }

            return roadSegment;
        }

        private static GameObject CreatePrefab(string prefabPath, Vector3 position, bool unpackPrefab = false, bool allowSearchRoot = true, Action<GameObject> onCreate = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));

            if (prefab == null && allowSearchRoot)
            {
                CityEditorStartup.ForceUpdateRoot();
                return CreatePrefab(prefabPath, position, unpackPrefab, false);
            }

            if (prefab == null)
            {
                Debug.LogError("Prefab not found!");
                return null;
            }

            GameObject createdObject = PrefabExtension.InstantiatePrefab(prefab, unpackPrefab);

            createdObject.transform.position = position;
            createdObject.transform.SetAsLastSibling();

            onCreate?.Invoke(createdObject.gameObject);

            return createdObject;
        }
#endif
    }
}
