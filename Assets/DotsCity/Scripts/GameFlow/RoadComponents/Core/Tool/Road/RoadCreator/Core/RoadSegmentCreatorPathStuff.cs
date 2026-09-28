using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreator : MonoBehaviour
    {
        private const float MinTurnAngle = 20f;
        private const float MinBezierStraightAngle = 5f;

        public void InitializePathHeaders()
        {
#if UNITY_EDITOR
            if (selectedTrafficNodeIndex != -1)
            {
                selectedPathIndex = -1;
                selectedPath = null;

                if (selectedTrafficNodeIndex >= createdTrafficNodes.Count)
                {
                    selectedTrafficNodeIndex = -1;
                    pathHeaders = null;
                    pathIndexBinding = null;
                    return;
                }

                var targetTrafficNode = createdTrafficNodes[selectedTrafficNodeIndex];
                var tempHeaders = new List<string>();

                pathIndexBinding = new Dictionary<int, Path>();

                tempHeaders.Add("All");

                int index = 0;

                if (PathDirection.HasFlag(TrafficNodeDirectionType.Right))
                {
                    targetTrafficNode.IterateAllPaths((path =>
                    {
                        pathIndexBinding.Add(index, path);
                        tempHeaders.Add(path.name);
                        index++;
                    }));
                }

                if (PathDirection.HasFlag(TrafficNodeDirectionType.Left))
                {
                    targetTrafficNode.IterateExternalPaths((path =>
                    {
                        pathIndexBinding.Add(index, path);
                        tempHeaders.Add(path.name);
                        index++;
                    }));
                }

                pathHeaders = tempHeaders.ToArray();
            }
            else
            {
                pathHeaders = null;
                pathIndexBinding = null;
            }
#endif
        }

        public Path CreatePath(PathDirectionType pathDirectionType, TrafficNode sourceTrafficNode, TrafficNode targetTrafficNode, bool arc = false)
        {
            return CreatePath(pathDirectionType, sourceTrafficNode, targetTrafficNode, turnCurveType, arc);
        }

        public Path CreatePath(PathDirectionType pathDirectionType, TrafficNode sourceTrafficNode, TrafficNode targetTrafficNode, int sourceLaneIndex, int targetLaneIndex, bool arc = false, int side = -1)
        {
            var path = CreatePath(pathDirectionType, sourceTrafficNode, targetTrafficNode, turnCurveType, arc);

            SetPathConnection(sourceTrafficNode, targetTrafficNode, sourceLaneIndex, targetLaneIndex, path, pathDirectionType != PathDirectionType.Forward, arc, side);
            return path;
        }

        public Path CreatePath(PathDirectionType pathDirectionType, TrafficNode sourceTrafficNode, TrafficNode targetTrafficNode, TurnCurveType turnCurveType, int sourceLaneIndex, int targetLaneIndex, bool arc = false, int side = -1)
        {
            var path = CreatePath(pathDirectionType, sourceTrafficNode, targetTrafficNode, turnCurveType, arc);

            SetPathConnection(sourceTrafficNode, targetTrafficNode, sourceLaneIndex, targetLaneIndex, path, turnCurveType, pathDirectionType != PathDirectionType.Forward, arc, side);
            return path;
        }

        public Path CreatePath(PathDirectionType pathDirectionType, TrafficNode sourceTrafficNode, TrafficNode targetTrafficNode, TurnCurveType turnCurveType, bool arc = false)
        {
            Path path = Instantiate(roadSegmentCreatorConfig.PathPrefab);

            path.PathRoadType = PathRoadType.TurnRoad;

            var currentCurveType = turnCurveType;

            if (arc)
            {
                currentCurveType = TurnCurveType.BezierQuad;
            }

            int nodeCount = 0;
            switch (pathDirectionType)
            {
                case PathDirectionType.Left:
                    {
                        path.transform.name = "PathLeftTurn";
                        path.WayPointsCountPerCurve = wayPointTurnCurveCount;
                        path.Priority = turnRoadPriority;

                        if (currentCurveType == TurnCurveType.BezierCube)
                        {
                            path.PathCurveType = PathCurveType.BezierCube;
                            nodeCount = 3;
                        }
                        else if (currentCurveType == TurnCurveType.BezierQuad)
                        {
                            path.PathCurveType = PathCurveType.BezierQuad;
                            nodeCount = 4;
                        }
                        break;
                    }
                case PathDirectionType.Forward:
                    {
                        path.transform.name = "PathStraight";
                        path.PathCurveType = PathCurveType.StraightLine;
                        path.PathRoadType = PathRoadType.StraightRoad;
                        path.WayPointsCountPerCurve = wayPointStraightRoadCount;
                        path.Priority = straightRoadPriority;
                        nodeCount = 2;
                        break;
                    }
                case PathDirectionType.Right:
                    {
                        path.transform.name = "PathRightTurn";
                        path.WayPointsCountPerCurve = wayPointTurnCurveCount;
                        path.Priority = turnRoadPriority;

                        if (currentCurveType == TurnCurveType.BezierCube)
                        {
                            path.PathCurveType = PathCurveType.BezierCube;
                            nodeCount = 3;
                        }
                        else if (currentCurveType == TurnCurveType.BezierQuad)
                        {
                            path.PathCurveType = PathCurveType.BezierQuad;
                            nodeCount = 4;
                        }
                        break;
                    }
            }

            path.transform.parent = sourceTrafficNode.PathParent;
            path.transform.localPosition = Vector3.zero;
            path.transform.localRotation = Quaternion.identity;

            path.SourceTrafficNode = sourceTrafficNode;
            path.ConnectedTrafficNode = targetTrafficNode;

            List<Transform> nodes = new List<Transform>();

            for (int i = 0; i < nodeCount; i++)
            {
                nodes.Add(new GameObject("Node" + (i + 1).ToString()).transform);
            }

            path.Nodes = nodes;

            return path;
        }

        private List<Path> TryToCreateLeftTurnPath(int currentNodeIndex, int laneIndex, int customNextIndex = -1)
        {
            var dynamicGeneration = customNextIndex != -1;

            int nextNodeIndex = customNextIndex == -1 ? (currentNodeIndex + 1) % CreateTrafficNodeCount : customNextIndex;

            var currentNode = createdTrafficNodes[currentNodeIndex];
            var nextNode = createdTrafficNodes[nextNodeIndex];

            if (IsStraightRoad())
                return null;

            if (!dynamicGeneration)
            {
                if (!CanConnectTurnLaneIndex(currentNodeIndex, laneIndex, currentNode.LaneCount, true) && CreateTrafficNodeCount != 2)
                    return null;
            }
            else
            {
                if (laneIndex != 0)
                {
                    for (int i = 0; i < currentNodeTempPaths.Count; i++)
                    {
                        // Avoid intersection in the left lane
                        if (currentNodeTempPaths[i].PathRoadType == PathRoadType.StraightRoad)
                        {
                            return null;
                        }
                    }
                }
            }

            if (!ConnectionIsAvailable(nextNode))
                return null;

            float signedAngle = GetSignedAngleRelativeNextTrafficNode(currentNodeIndex, nextNodeIndex);

            var hasLeftTurn = (signedAngle < -MinTurnAngle && signedAngle > -120);

            if (hasLeftTurn && nextNode.IsOneWay)
            {
                hasLeftTurn = nextNode.IsEndOfOneWay;
            }

            if (roadSegmentType == RoadSegmentType.TurnRoad)
            {
                hasLeftTurn = true;
            }

            if (!hasLeftTurn)
                return null;

            var leftPaths = new List<Path>();

            int maxConnection = 1;

            if (!dynamicGeneration)
            {
                var turnData = GetTurnData(currentNodeIndex);
                var laneLeftTurnConnectionCount = turnData.LaneLeftTurnConnectionCount;
                maxConnection = Mathf.Clamp(laneLeftTurnConnectionCount, 0, nextNode.LaneCount);
            }

            int laneDiff = ProjectConstants.LaneHandDirection == 1 ? 0 : nextNode.GetLaneCount(true) - currentNode.LaneCount;

            for (int connectionIndex = 0; connectionIndex < maxConnection; connectionIndex++)
            {
                int nextLaneIndex = 0;

                if (!nextNode.IsOneWay)
                {
                    nextLaneIndex = laneIndex + connectionIndex + laneDiff;
                }
                else
                {
                    nextLaneIndex = laneIndex + connectionIndex * ProjectConstants.LaneHandDirection + laneDiff;
                }

                if (nextLaneIndex >= nextNode.GetLaneCount(true) || nextLaneIndex < 0)
                    continue;

                if (!AvailableConnection(currentNodeIndex, nextNodeIndex, laneIndex, nextLaneIndex))
                    continue;

                if (dynamicGeneration)
                {
                    if (IsConnectedToLane(currentNodeIndex, nextNodeIndex, nextLaneIndex))
                        continue;
                }

                var leftPath = CreatePath(PathDirectionType.Left, currentNode, nextNode);
                leftPath.name = $"PathLeftTurn_{laneIndex}";
                leftPath.PathSpeedLimit = turnRoadPathSpeedLimit;

                SetPathConnection(currentNodeIndex, nextNodeIndex, laneIndex, nextLaneIndex, leftPath, true);
                leftPaths.Add(leftPath);
            }

            return leftPaths;
        }

        private List<Path> TryToCreateStraightPath(int currentNodeIndex, int laneIndex, int customNextIndex = -1)
        {
            var dynamicGeneration = customNextIndex != -1;

            if (CreateTrafficNodeCount < 3 && !IsStraightRoad() && !IsCustom())
                return null;

            int nextNodeIndex = (currentNodeIndex + 2) % CreateTrafficNodeCount;

            if (CreateTrafficNodeCount == 3 && currentNodeIndex == CreateTrafficNodeCount - 1)
            {
                nextNodeIndex = 0;
            }

            bool hasStraight = true;
            bool bezier = false;
            bool isRightTurn = false;

            if (!IsStraightRoad() && (CreateTrafficNodeCount != 2 || customNextIndex != -1))
            {
                nextNodeIndex = customNextIndex != -1 ? customNextIndex : nextNodeIndex;

                var signedAngle = GetSignedAngleRelativeNextTrafficNode(currentNodeIndex, nextNodeIndex);

                hasStraight = (Mathf.Abs(signedAngle) < MinBezierStraightAngle);

                if (!hasStraight)
                {
                    hasStraight = (Mathf.Abs(signedAngle) >= MinBezierStraightAngle && Mathf.Abs(signedAngle) < MinTurnAngle);
                    bezier = true;
                    isRightTurn = signedAngle > 0;
                }
            }
            else
            {
                nextNodeIndex = (currentNodeIndex + 1) % 2;
            }

            var currentNode = createdTrafficNodes[currentNodeIndex];
            var nextNode = createdTrafficNodes[nextNodeIndex];

            if (!ConnectionIsAvailable(nextNode))
                return null;

            if (!hasStraight)
                return null;

            var straightPaths = new List<Path>();

            int maxConnectionCount = 1;

            if (!dynamicGeneration)
                maxConnectionCount = roadSegmentType != RoadSegmentType.MergeStraightRoad ? 1 : nextNode.LaneCount;

            for (int connectionIndex = 0; connectionIndex < maxConnectionCount; connectionIndex++)
            {
                int nextLaneIndex = laneIndex + connectionIndex;

                if (bezier && dynamicGeneration)
                {
                    if (isRightTurn)
                    {
#pragma warning disable CS0162
                        var currentLaneCount = currentNode.LaneCount;

                        if (ProjectConstants.LaneHandDirection == 1)
                        {
                            nextLaneIndex = laneIndex + nextNode.LaneCount - currentLaneCount;
                        }
#pragma warning restore CS0162

                        if (nextLaneIndex < 0 || nextLaneIndex >= nextNode.GetLaneCount(true))
                            return null;

#pragma warning disable CS0162
                        if (ProjectConstants.LaneHandDirection == 1)
                        {
                            if (IsSubLane(nextNodeIndex))
                            {
                                nextLaneIndex = Mathf.Clamp(nextLaneIndex, 0, nextNode.GetLaneCount(true) - 1);
                            }
                            else if (IsSubLane(currentNodeIndex))
                            {
                                if (!dynamicGeneration)
                                {
                                    nextLaneIndex = nextNode.LaneCount - connectionIndex - 1;
                                }
                            }
                            else
                            {
                                nextLaneIndex -= connectionIndex;
                            }
                        }
                        else
                        {
                            nextLaneIndex += connectionIndex;
                        }

#pragma warning restore CS0162
                    }
                    else
                    {
                        int laneDiff = ProjectConstants.LaneHandDirection == 1 ? 0 : nextNode.GetLaneCount(true) - currentNode.LaneCount;

                        if (!nextNode.IsOneWay)
                        {
                            nextLaneIndex = laneIndex + connectionIndex + laneDiff;
                        }
                        else
                        {
                            nextLaneIndex = laneIndex + connectionIndex * ProjectConstants.LaneHandDirection + laneDiff;
                        }
                    }
                }

                //int nextLaneIndex = laneIndex + connectionIndex + nextNode.LaneCount - currentNode.LaneCount;

                if (!AvailableConnection(currentNodeIndex, nextNodeIndex, laneIndex, nextLaneIndex))
                    continue;

                if (!dynamicGeneration)
                {
                    nextLaneIndex = Mathf.Clamp(nextLaneIndex, 0, nextNode.GetLaneCount(true) - 1);
                }
                else
                {
                    if (nextLaneIndex < 0 || nextLaneIndex >= nextNode.GetLaneCount(true))
                        continue;

                    if (IsConnectedToLane(currentNodeIndex, nextNodeIndex, nextLaneIndex))
                        continue;
                }

                var type = !bezier ? PathDirectionType.Forward : PathDirectionType.Right;

                var straightPath = CreatePath(type, currentNode, nextNode);

                straightPath.name = $"PathStraight_{laneIndex}";
                straightPath.PathSpeedLimit = straightRoadPathSpeedLimit;
                straightPath.PathRoadType = PathRoadType.StraightRoad;

                SetPathConnection(currentNodeIndex, nextNodeIndex, laneIndex, nextLaneIndex, straightPath, bezier);

                straightPaths.Add(straightPath);
            }

            return straightPaths;
        }

        private List<Path> TryToCreateRightPath(int currentNodeIndex, int laneCount, int laneIndex, int customNextIndex = -1)
        {
            var dynamicGeneration = customNextIndex != -1;

            if (IsStraightRoad())
                return null;

            int nextNodeIndex = (currentNodeIndex + CreateTrafficNodeCount - 1) % CreateTrafficNodeCount;

            nextNodeIndex = customNextIndex != -1 ? customNextIndex : nextNodeIndex;

            var currentNode = createdTrafficNodes[currentNodeIndex];
            var nextNode = createdTrafficNodes[nextNodeIndex];

            if (!ConnectionIsAvailable(nextNode))
                return null;

            var nextLaneIndex = laneIndex;
            var currentLaneCount = currentNode.LaneCount;

            if (!dynamicGeneration)
            {
                if (!CanConnectTurnLaneIndex(currentNodeIndex, nextLaneIndex, currentLaneCount, false) && CreateTrafficNodeCount != 2)
                    return null;
            }
            else
            {
                // This lane is empty
                if (laneIndex != currentLaneCount - 1 && currentLaneTempPaths.Count != 0)
                {
                    return null;
                }
            }

            var signedAngle = GetSignedAngleRelativeNextTrafficNode(currentNodeIndex, nextNodeIndex);

            var hasRightTurn = (signedAngle > MinTurnAngle && signedAngle < 120);

            if (hasRightTurn && nextNode.IsOneWay)
            {
                hasRightTurn = nextNode.IsEndOfOneWay;
            }

            if (roadSegmentType == RoadSegmentType.TurnRoad)
            {
                hasRightTurn = false;
            }

            if (!hasRightTurn)
                return null;

            var rightPaths = new List<Path>();

            int maxConnection = 1;

            if (!dynamicGeneration)
            {
                var turnData = GetTurnData(currentNodeIndex);
                var laneRightTurnConnectionCount = turnData.LaneRightTurnConnectionCount;
                maxConnection = Mathf.Clamp(laneRightTurnConnectionCount, 0, nextNode.LaneCount);
            }

#pragma warning disable CS0162
            if (ProjectConstants.LaneHandDirection == 1)
            {
                nextLaneIndex = laneIndex + nextNode.LaneCount - currentLaneCount;
            }
#pragma warning restore CS0162

            if (nextLaneIndex < 0 || nextLaneIndex >= nextNode.GetLaneCount(true))
                return null;

            for (int connectionIndex = 0; connectionIndex < maxConnection; connectionIndex++)
            {
#pragma warning disable CS0162
                if (ProjectConstants.LaneHandDirection == 1)
                {
                    if (IsSubLane(nextNodeIndex))
                    {
                        nextLaneIndex = Mathf.Clamp(nextLaneIndex, 0, nextNode.GetLaneCount(true) - 1);
                    }
                    else if (IsSubLane(currentNodeIndex))
                    {
                        if (!dynamicGeneration)
                        {
                            nextLaneIndex = nextNode.LaneCount - connectionIndex - 1;
                        }
                    }
                    else
                    {
                        nextLaneIndex -= connectionIndex;
                    }
                }
                else
                {
                    nextLaneIndex += connectionIndex;
                }

#pragma warning restore CS0162

                if (nextLaneIndex < 0 || nextLaneIndex >= nextNode.GetLaneCount(true))
                    continue;

                if (!AvailableConnection(currentNodeIndex, nextNodeIndex, laneIndex, nextLaneIndex))
                    continue;

                if (dynamicGeneration)
                {
                    if (IsConnectedToLane(currentNodeIndex, nextNodeIndex, nextLaneIndex))
                        continue;
                }

                var rightPath = CreatePath(PathDirectionType.Right, currentNode, nextNode);
                rightPath.name = $"PathRightTurn_{laneIndex}";
                rightPath.PathSpeedLimit = turnRoadPathSpeedLimit;

                SetPathConnection(currentNodeIndex, nextNodeIndex, laneIndex, nextLaneIndex, rightPath, true);
                rightPaths.Add(rightPath);
            }

            return rightPaths;
        }

        private List<Path> TryToCreateUTurns(int currentNodeIndex, int laneCount, int laneIndex)
        {
            if (!CustomTurnSupport()) return null;

            if (!UTurnIsAvailable(currentNodeIndex)) return null;

            if (laneIndex > 0) return null;

            bool isUTurn = GetUTurnData(currentNodeIndex);

            if (isUTurn)
            {
                var currentNode = TryToGetNode(currentNodeIndex);
                var nextNode = currentNode;
                var nextNodeIndex = currentNodeIndex;
                var nextLaneIndex = laneIndex;

                var path = CreatePath(PathDirectionType.Left, currentNode, nextNode, true);
                path.name = $"PathUTurn_{laneIndex}";
                path.PathSpeedLimit = turnRoadPathSpeedLimit;

                SetPathConnection(currentNodeIndex, nextNodeIndex, laneIndex, nextLaneIndex, path, true, true);

                return new List<Path> { path };
            }

            return null;
        }

        private bool CanConnectTurnLaneIndex(int currentNodeIndex, int laneIndex, int currentLaneCount, bool isLeft)
        {
            var turnData = GetTurnData(currentNodeIndex);
            var turnCount = isLeft ? turnData.LeftTurnCount : turnData.RightTurnCount;

            isLeft = ProjectConstants.LaneHandDirection == 1 ? isLeft : !isLeft;

            if (isLeft)
            {
                return laneIndex < turnCount;
            }
            else
            {
                return laneIndex >= currentLaneCount - turnCount;
            }
        }

        private bool ConnectionIsAvailable(TrafficNode trafficNode) => trafficNode.AvailableForInnerConnection();

        private Vector3 GetStartOneWayPoint(int currentTrafficNodeIndex, int laneIndex)
        {
            return GetOneWayLanePosition(createdTrafficNodes[currentTrafficNodeIndex], laneIndex);
        }

        private Vector3 GetTargetOneWayPoint(int connectedLaneIndex, int nextTrafficNodeIndex)
        {
            return GetOneWayLanePosition(createdTrafficNodes[nextTrafficNodeIndex], connectedLaneIndex, -1);
        }

        private Vector3 GetLanePosition(TrafficNode trafficNode, int laneIndex, int side = 1)
        {
            return trafficNode.GetLanePosition(laneIndex, side == -1);
        }

        private Vector3 GetOneWayLanePosition(TrafficNode trafficNode, int laneIndex, int side = 1)
        {
            return trafficNode.GetOneWaypoint(laneIndex, side == -1);
        }

        private float GetSignedAngleRelativeNextTrafficNode(int i, int nextIndex)
        {
            var dir1 = -createdTrafficNodes[i].transform.forward;
            var dir2 = (createdTrafficNodes[nextIndex].transform.position - createdTrafficNodes[i].transform.position).normalized;
            var dot = Vector3.Dot(createdTrafficNodes[nextIndex].transform.forward, dir2);

            var angle = Vector3.SignedAngle(dir1, dir2, Vector3.up);

            if (dot < 0)
            {
                if (angle < 0)
                {
                    angle = -180 - angle;
                }
                else
                {
                    angle = 180 - angle;
                }
            }

            return angle;
        }

        public void ClearOffsetPaths()
        {
            for (int i = 0; i < createdTrafficNodes.Count; i++)
            {
                var createdNode = createdTrafficNodes[i];

                if (createdNode.Lanes?.Count > 0)
                {
                    var lanes = createdNode.Lanes;

                    for (int laneIndex = 0; laneIndex < lanes.Count; laneIndex++)
                    {
                        var paths = lanes[laneIndex].paths;

                        for (int n = 0; n < paths?.Count; n++)
                        {
                            var path = paths[n];

                            if (path)
                                DestroyImmediate(path.gameObject);
                        }
                    }
                }

                createdTrafficNodes[i].Lanes = null;
            }
        }

        private void CreatePathNodes(int currentIndex, int nextIndex)
        {
            LaneArray[] pathArray = new LaneArray[laneCount];

            var tempCustomPath = GetTempPath(0);

            for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
            {
                var path = Instantiate(roadSegmentCreatorConfig.PathPrefab, createdTrafficNodes[currentIndex].transform.parent);

                path.PathCurveType = tempCustomPath.PathCurveType;

                path.WayPointsCountPerCurve = path.PathCurveType == PathCurveType.StraightLine ? wayPointStraightRoadCount : wayPointTurnCurveCount;
                path.PathSpeedLimit = path.PathCurveType == PathCurveType.StraightLine ? straightRoadPathSpeedLimit : turnRoadPathSpeedLimit;

                List<Transform> nodes = new List<Transform>();

                for (int i = 0; i < tempCustomPath.Nodes.Count; i++)
                {
                    var node = new GameObject("Node" + (i + 1).ToString()).transform;

                    if (currentIndex == 0)
                    {
                        nodes.Add(node);
                    }
                    else
                    {
                        nodes.Insert(0, node);
                    }
                }

                path.Nodes = nodes;

                Path[] paths = new Path[1];
                paths[0] = path;

                pathArray[laneIndex].paths = paths.ToList();
            }

            createdTrafficNodes[currentIndex].Lanes = pathArray.ToList();
        }

        private bool AvailableConnection(int sourceNodeIndex, int targetNodeIndex, int source, int target)
        {
            var nodeIndexes = new Vector2Int(sourceNodeIndex, targetNodeIndex);

#if UNITY_EDITOR
            if (ignoreConnections.Contains(nodeIndexes))
            {
                return false;
            }
#endif

            var connectionIndexes = new Vector2Int(source, target);

            if (!addedConnections.ContainsKey(nodeIndexes))
            {
                addedConnections.Add(nodeIndexes, new HashSet<Vector2Int>());
            }

            if (!addedConnections[nodeIndexes].Contains(connectionIndexes))
            {
                addedConnections[nodeIndexes].Add(connectionIndexes);
                return true;
            }

            return false;
        }

        private bool IsConnectedToLane(int sourceNodeIndex, int targetNodeIndex, int targetLaneIndex)
        {
            var node1 = TryToGetNode(sourceNodeIndex);
            var node2 = TryToGetNode(targetNodeIndex);

            for (int i = 0; i < currentNodeTempPaths?.Count; i++)
            {
                var path = currentNodeTempPaths[i];

                if (path.SourceTrafficNode == node1 && path.ConnectedTrafficNode == node2 && path.ConnectedLaneIndex == targetLaneIndex)
                    return true;
            }

            return false;
        }

        private List<(Path, Path)> GetSelfIntersections(TrafficNode trafficNode)
        {
            var list = new List<(Path, Path)>();

            var laneArray = trafficNode.Lanes;

            for (int laneIndex1 = 0; laneIndex1 < laneArray.Count; laneIndex1++)
            {
                for (int laneIndex2 = laneIndex1 + 1; laneIndex2 < laneArray.Count; laneIndex2++)
                {
                    CheckIntersections(list, laneArray[laneIndex1].paths, laneArray[laneIndex2].paths);
                }
            }

            return list;
        }

        private void CheckIntersections(List<(Path, Path)> list, List<Path> paths1, List<Path> paths2)
        {
            for (int i = 0; i < paths1.Count; i++)
            {
                Vector3[] points1 = paths1[i].WaypointPositionArray;

                for (int j = 0; j < paths2.Count; j++)
                {
                    if (paths1[i].ConnectedTrafficNode == paths2[j].ConnectedTrafficNode && paths1[i].ConnectedLaneIndex == paths2[j].ConnectedLaneIndex) continue;

                    Vector3[] points2 = paths2[j].WaypointPositionArray;

                    var intersectPoint = PathHelper.GetPathIntersection(points1, points2, out var index1, out var index2);

                    if (intersectPoint == Vector3.zero) continue;

                    Path path1 = null;
                    Path path2 = null;

                    if (paths1[i].PathRoadType == PathRoadType.StraightRoad || paths1[i].PathRoadType != PathRoadType.StraightRoad && paths1[i].name.Contains("Left"))
                    {
                        path1 = paths2[j];
                        path2 = paths1[i];
                    }
                    else
                    {
                        path1 = paths1[i];
                        path2 = paths2[j];
                    }

                    var pair = (path1, path2);

                    bool added = false;

                    foreach (var item in list)
                    {
                        if (item.Item1 == pair.path1 && item.Item2 == pair.path2)
                        {
                            added = true;
                            break;
                        }
                    }

                    if (!added)
                        list.Add(pair);
                }
            }
        }

        private void SetPathConnection(TrafficNode currentNode, TrafficNode nextNode, int sourceLaneIndex, int connectedLaneIndex, Path path, bool bezier = true, bool arc = false, int side = -1)
        {
            var index1 = createdTrafficNodes.IndexOf(currentNode);
            var index2 = createdTrafficNodes.IndexOf(nextNode);

            SetPathConnection(index1, index2, sourceLaneIndex, connectedLaneIndex, path, turnCurveType, bezier, arc, side);
        }

        private void SetPathConnection(TrafficNode currentNode, TrafficNode nextNode, int sourceLaneIndex, int connectedLaneIndex, Path path, TurnCurveType turnCurveType, bool bezier = true, bool arc = false, int side = -1)
        {
            var index1 = createdTrafficNodes.IndexOf(currentNode);
            var index2 = createdTrafficNodes.IndexOf(nextNode);

            SetPathConnection(index1, index2, sourceLaneIndex, connectedLaneIndex, path, turnCurveType, bezier, arc, side);
        }

        private void SetPathConnection(int currentNodeIndex, int nextNodeIndex, int sourceLaneIndex, int connectedLaneIndex, Path path, bool bezier = true, bool arc = false, int side = -1)
        {
            SetPathConnection(currentNodeIndex, nextNodeIndex, sourceLaneIndex, connectedLaneIndex, path, turnCurveType, bezier, arc, side);
        }

        private void SetPathConnection(int currentNodeIndex, int nextNodeIndex, int sourceLaneIndex, int connectedLaneIndex, Path path, TurnCurveType turnCurveType, bool bezier = true, bool arc = false, int side = -1)
        {
            Vector3 point1, point2;
            GetLaneConnectionPoints(currentNodeIndex, nextNodeIndex, sourceLaneIndex, connectedLaneIndex, out point1, out point2, side);

            path.Nodes[0].transform.position = point1;
            path.Nodes[path.Nodes.Count - 1].transform.position = point2;
            path.ConnectedLaneIndex = connectedLaneIndex;

            var currentTurnCurveType = turnCurveType;

            if (arc)
            {
                bezier = true;
                currentTurnCurveType = TurnCurveType.BezierQuad;
            }

            if (bezier)
            {
                int relativeTrafficNodeIndex = CreateTrafficNodeCount == 2 ? 0 : currentNodeIndex;

                if (currentTurnCurveType == TurnCurveType.BezierCube)
                {
                    var cornerPoint = GetSplineCornerPoint(point1, point2, currentNodeIndex, nextNodeIndex, true);

                    path.Nodes[1].transform.position = cornerPoint + createdTrafficNodes[relativeTrafficNodeIndex].transform.rotation * PathCorner1Offset;
                }
                else
                {
                    if (!arc)
                    {
                        var cornerPoints = GetSplineTwoCornerPoint(point1, point2, currentNodeIndex, nextNodeIndex, true);
                        path.Nodes[1].transform.position = cornerPoints.Item1 + createdTrafficNodes[relativeTrafficNodeIndex].transform.rotation * PathCorner1Offset;
                        path.Nodes[2].transform.position = cornerPoints.Item2 + createdTrafficNodes[relativeTrafficNodeIndex].transform.rotation * PathCorner2Offset;
                    }
                    else
                    {
                        var cornerPoints = GetArcControlPoints(point1, point2, stretchFactor, stretchAngle);
                        path.Nodes[1].transform.position = cornerPoints.Item1;
                        path.Nodes[2].transform.position = cornerPoints.Item2;
                    }
                }
            }

            path.ConnectedTrafficNode = createdTrafficNodes[nextNodeIndex];
            path.ConnectedLaneIndex = connectedLaneIndex;
            path.CreatePath(true);

            EditorSaver.SetObjectDirty(path);
        }

        private void GetLaneConnectionPoints(int currentNodeIndex, int nextNodeIndex, int sourceLaneIndex, int nextLaneIndex, out Vector3 point1, out Vector3 point2, int side = -1)
        {
            point1 = Vector3.zero;

            if (!createdTrafficNodes[currentNodeIndex].IsOneWay)
            {
                point1 = GetLanePosition(createdTrafficNodes[currentNodeIndex], sourceLaneIndex);
            }
            else
            {
                point1 = GetStartOneWayPoint(currentNodeIndex, sourceLaneIndex);
            }

            point2 = Vector3.zero;

            if (!createdTrafficNodes[nextNodeIndex].IsOneWay)
            {
                point2 = GetLanePosition(createdTrafficNodes[nextNodeIndex], nextLaneIndex, -1);
            }
            else
            {
                if (side == -1)
                {
                    point2 = GetTargetOneWayPoint(nextLaneIndex, nextNodeIndex);
                }
                else
                {
                    point2 = GetStartOneWayPoint(nextNodeIndex, nextLaneIndex);
                }
            }
        }

        private Vector3 GetSplineCornerPoint(Vector3 sourcePoint, Vector3 targetPoint, int sourceNodeIndex, int targetNodeIndex, bool allowClosestPoint = false)
        {
#if UNITY_EDITOR
            return PathAttachHelper.GetSplineCornerPoint(sourcePoint, targetPoint, createdTrafficNodes[sourceNodeIndex], createdTrafficNodes[targetNodeIndex], allowClosestPoint);
#else
            return default;
#endif
        }

        private (Vector3, Vector3) GetSplineTwoCornerPoint(Vector3 sourcePoint, Vector3 targetPoint, int sourceNodeIndex, int targetNodeIndex, bool allowClosestPoint = false)
        {
#if UNITY_EDITOR
            return PathAttachHelper.GetSplineTwoCornerPoint(sourcePoint, targetPoint, createdTrafficNodes[sourceNodeIndex], createdTrafficNodes[targetNodeIndex], allowClosestPoint);
#else
            return default;
#endif
        }

        private (Vector3, Vector3) GetArcControlPoints(Vector3 sourcePoint, Vector3 targetPoint, float stretchFactor = 1f, float angle = 0)
        {
#if UNITY_EDITOR
            return PathAttachHelper.GetArcControlPoints(sourcePoint, targetPoint, stretchFactor, angle);
#else
            return default;
#endif
        }

        private Vector3 GetCornerPointRelativeCenter(Vector3 spawnPoint1, Vector3 spawnPoint2)
        {
            Vector3 point1 = new Vector3(spawnPoint1.x, spawnPoint1.y, spawnPoint2.z);
            Vector3 point2 = new Vector3(spawnPoint2.x, spawnPoint2.y, spawnPoint1.z);

            float distance1 = Vector3.SqrMagnitude(transform.position - point1);
            float distance2 = Vector3.SqrMagnitude(transform.position - point2);

            Vector3 spawnPosition = distance1 < distance2 ? point1 : point2;
            return spawnPosition;
        }

        private void SetInnerSpawnNodes(Path path, float minSpawnNodeOffset)
        {
            PathNode lastNode = null;

            path.IterateWaypoints(node =>
            {
                lastNode = node;
                node.SpawnNode = true;
            },
            minSpawnNodeOffset);

            if (lastNode)
            {
                if (Vector3.Distance(lastNode.transform.position, path.EndPosition) < minSpawnNodeOffset / 2)
                {
                    lastNode.SpawnNode = false;
                }
            }
        }

        private Path GetPathByIndex()
        {
#if UNITY_EDITOR
            CheckBinding();

            if (selectedPathIndex == -1)
            {
                return null;
            }

            if (!pathIndexBinding.ContainsKey(selectedPathIndex))
            {
                InitializePathHeaders();
            }

            return pathIndexBinding[selectedPathIndex];
#else
            return null;
#endif
        }

        private void CheckBinding()
        {
#if UNITY_EDITOR
            var headerCount = pathHeaders.Length - 1;

            if (pathIndexBinding == null || pathIndexBinding.Count != headerCount)
            {
                InitializePathHeaders();
            }
#endif
        }

        private string GetPathName(string name, TrafficNode sourceTrafficNode, TrafficNode targetTrafficNode)
        {
            return $"{name}_{GetNodeIndex(sourceTrafficNode) + 1}_{GetNodeIndex(targetTrafficNode) + 1}";
        }
    }
}
