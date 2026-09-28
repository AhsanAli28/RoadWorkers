using PampelGames.RoadConstructor;
using Spirit604.CityEditor.Utils;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadConstructor
{
    public abstract class RC_EditorSceneGeneratorBase<T> : MonoBehaviour where T : MonoBehaviour
    {
        private const float Offset = 0;
        private const float LateralWaypointDirection = 0.25f;

        [SerializeField] private NodeShapeType crosswalkShapeType = NodeShapeType.Rectangle;
        [SerializeField][Range(0f, 5f)] private float crosswalkWidth = 1f;
        [SerializeField][Range(0f, 5f)] private float crosswalkHeight = 2f;

        private List<Vector3> firstLineWaypoints;
        private List<TrafficLane> trafficLanes = new List<TrafficLane>();
        protected Dictionary<SceneObject, T> roadBinding = new Dictionary<SceneObject, T>();

        public NodeShapeType CrosswalkShapeType { get => crosswalkShapeType; set => crosswalkShapeType = value; }
        public float CrosswalkWidth { get => crosswalkWidth; set => crosswalkWidth = value; }
        public float CrosswalkHeight { get => crosswalkHeight; set => crosswalkHeight = value; }

        protected abstract T CreateSpline(RoadObject roadObject, List<RoadSceneUtils.NodeSettings> nodeList, RoadSceneUtils.StraightSegmentSettings settings);

        protected abstract T CreateIntersection(IntersectionObject intersection, List<RoadSceneUtils.NodeSettings> nodeList, RoadSceneUtils.SegmentSettings segmentSettings);

        protected abstract RoadSceneUtils.NodeSettings TryToGetClosestNode(T binding, Vector3 intersectionPos);

        protected virtual void RegisterSpline(SceneObject sceneObject, T obj)
        {
            RegisterObject(sceneObject, obj);
        }

        protected virtual void RegisterIntersection(SceneObject sceneObject, T obj)
        {
            RegisterObject(sceneObject, obj);
        }

        protected T CreateSpline(RoadObject road)
        {
            if (roadBinding.ContainsKey(road))
                return roadBinding[road];

            firstLineWaypoints = null;
            var splineContainer = road.splineContainer;
            var spline = road.splineContainer.Spline;

            trafficLanes.Clear();

            var sourceTrafficLanes = road.traffic.trafficLanes;

            bool anyPedestrian = false;

            for (int j = 0; j < sourceTrafficLanes.Count; j++)
            {
                var laneData = sourceTrafficLanes[j];

                if (laneData.trafficLaneType == TrafficLaneType.Car)
                {
                    trafficLanes.Add(laneData);
                }
                else
                {
                    anyPedestrian = true;
                }
            }

            var laneCount = trafficLanes.Count;
            var firstLane = trafficLanes[0];

            var oppositeLane = trafficLanes[laneCount / 2];

            var dividerWidth = 0f;
            var laneWidth = trafficLanes[0].waypoints[0].laneWidth;

            bool oneway = false;

            if (firstLane.direction != oppositeLane.direction)
            {
                var firstWaitpoint = trafficLanes[0].waypoints[0];
                var lastpoint = oppositeLane.waypoints.Last();

                dividerWidth = Vector3.Distance(firstWaitpoint.transform.position, lastpoint.transform.position) - laneWidth;
            }
            else
            {
                oneway = true;
            }

            if (!oneway)
            {
                laneCount /= 2;
            }

            var list = new List<RoadSceneUtils.NodeSettings>();

            AddSplineNode(road, list, dividerWidth, oneway, laneWidth, laneCount, anyPedestrian);
            AddSplineNode(road, list, dividerWidth, oneway, laneWidth, laneCount, anyPedestrian, laneCount, true);

            Vector3 crosswalkOffset = default;

            if (anyPedestrian)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    crosswalkOffset = GetCrosswalkOffset(road, list[i], sourceTrafficLanes, ShouldAddEdges(road, oneway, i));
                    list[i].Settings.CrosswalkOffset = crosswalkOffset;
                }
            }

            var segmentSettings = new RoadSceneUtils.StraightSegmentSettings()
            {
                StartOffset = Offset,
                EndOffset = Offset,
                DividerWidth = dividerWidth,
                LaneCount = laneCount,
                IsOneWay = oneway,
                LaneWidth = laneWidth,
                GenerateInnerTrafficSpawnNodes = true,
                AddPedestrianAlongLine = true,
                AddCrosswalk = false,
                AddPedestrianNodes = anyPedestrian,
                CrosswalkOffset = crosswalkOffset,
            };

            var splineObj = CreateSpline(road, list, segmentSettings);

            RegisterSpline(road, splineObj);
            return splineObj;
        }

        private static bool ShouldAddEdges(RoadObject road, bool oneway, int index)
        {
            if (road.RoadConnections.Count != 2)
            {
                return true;
            }
            else
            {
                Vector3 point = default;

                var trafficLanes = road.traffic.trafficLanes.Where(a => a.trafficLaneType == TrafficLaneType.Car).ToList();

                var knot = (Vector3)road.splineContainer.Spline.Knots.ElementAt(index).Position;

                if (index == 0)
                {
                    point = trafficLanes[0].waypoints[0].transform.position;

                    if (!oneway)
                    {
                        if (trafficLanes.Count >= 2)
                        {
                            var oppositeLane = trafficLanes[trafficLanes.Count / 2];
                            point = (point + oppositeLane.waypoints.Last().transform.position) / 2;
                        }
                    }
                    else
                    {
                        for (int i = 1; i < trafficLanes.Count; i++)
                        {
                            point += trafficLanes[i].waypoints[0].transform.position;
                        }

                        point /= trafficLanes.Count;
                    }
                }
                else
                {
                    point = trafficLanes[0].waypoints.Last().transform.position;

                    if (!oneway)
                    {
                        if (trafficLanes.Count >= 2)
                        {
                            var oppositeLane = trafficLanes[trafficLanes.Count / 2];
                            point = (point + oppositeLane.waypoints[0].transform.position) / 2;
                        }
                    }
                    else
                    {
                        for (int i = 1; i < trafficLanes.Count; i++)
                        {
                            point += trafficLanes[i].waypoints.Last().transform.position;
                        }

                        point /= trafficLanes.Count;
                    }
                }

                var dist = Vector3.SqrMagnitude(point - knot);

                const float maxDistancesq = 1f;

                if (dist > maxDistancesq)
                    return true;
            }

            return false;
        }

        protected T CreateIntersection(IntersectionObject intersection)
        {
            var connections = intersection.RoadConnections;
            var list = new List<RoadSceneUtils.NodeSettings>();

            var sourceTrafficLanes = intersection.traffic.trafficLanes;

            for (int i = 0; i < connections.Count; i++)
            {
                var connectedRoad = connections[i];

                var spline = connectedRoad.splineContainer.Splines[0];

                var connectedNode = GetClosestNode(connectedRoad, intersection.centerPosition);

                var pos1 = (Vector3)spline.Knots.ElementAt(0).Position;
                var pos2 = (Vector3)spline.Knots.ElementAt(1).Position;

                var dist1 = Vector3.SqrMagnitude(pos1 - connectedNode.Position);
                var dist2 = Vector3.SqrMagnitude(pos2 - connectedNode.Position);
                var nodePosition = dist1 < dist2 ? pos1 : pos2;
                var nodeRotation = connectedNode.Rotation * Quaternion.Euler(0, 180, 0);

                trafficLanes.Clear();

                float laneCount = connectedNode.Settings.LaneCount;

                if (connectedNode.Settings.IsOneWay)
                {
                    laneCount /= 2;
                }

                var pathData = new List<RoadSceneUtils.PathData>();

                if (connections.Count != 1 || connections.Count == 1 && !connectedNode.Settings.IsOneWay)
                {
                    var dist = laneCount * connectedNode.Settings.LaneWidth + connectedNode.Settings.DividerWidth / 2;

                    dist *= dist;

                    var forward = nodeRotation * Vector3.forward;

                    for (int j = 0; j < sourceTrafficLanes.Count; j++)
                    {
                        var laneData = sourceTrafficLanes[j];

                        if (laneData.trafficLaneType == TrafficLaneType.Car)
                        {
                            var waypoint = laneData.waypoints[0].transform.position;

                            bool hasPrev = laneData.waypoints[0].prev.Count > 0;

                            var sqDist = Vector3.SqrMagnitude(nodePosition - waypoint);

                            if (sqDist < dist)
                            {
                                var dir = Vector3.Normalize(nodePosition - waypoint);
                                var dot = Mathf.Abs(Vector3.Dot(forward, dir));

                                if (dot < LateralWaypointDirection || !hasPrev)
                                {
                                    trafficLanes.Add(laneData);
                                }
                            }
                        }
                    }

                    for (int j = 0; j < trafficLanes.Count; j++)
                    {
                        var localPathData = CreatePathData(trafficLanes[j]);
                        pathData.Add(localPathData);
                    }
                }

                var crosswalk = connectedNode.Settings.CrosswalkOffset;
                crosswalk.z = -crosswalk.z;

                list.Add(new RoadSceneUtils.NodeSettings()
                {
                    Position = nodePosition,
                    Rotation = nodeRotation,

                    Settings = new RoadSceneUtils.BaseSettings()
                    {
                        AddCrosswalk = HasCrosswalk(nodePosition, sourceTrafficLanes),
                        AddPedestrianNodes = connectedNode.Settings.AddPedestrianNodes,
                        DividerWidth = connectedNode.Settings.DividerWidth,
                        IsOneWay = connectedNode.Settings.IsOneWay,
                        IsEndOfOneWay = connectedNode.Settings.IsOneWay && !connectedNode.Settings.IsEndOfOneWay,
                        LaneCount = connectedNode.Settings.LaneCount,
                        LaneWidth = connectedNode.Settings.LaneWidth,
                        Offset = Offset,
                        CrosswalkOffset = crosswalk,
                        CrosswalkShapeType = connectedNode.Settings.CrosswalkShapeType,
                        CrosswalkHeight = connectedNode.Settings.CrosswalkHeight,
                        CrosswalkWidth = connectedNode.Settings.CrosswalkWidth,
                    },

                    Paths = pathData
                });
            }

            var segmentSettings = RoadSceneUtils.SegmentSettings.GetDefault();

            segmentSettings.AddCrosswalk = connections.Count >= 3;
            segmentSettings.ConnectCrosswalks = connections.Count >= 3;

            var sceneIntersection = CreateIntersection(intersection, list, segmentSettings);

            RegisterIntersection(intersection, sceneIntersection);

            return sceneIntersection;
        }

        protected void RegisterObject(SceneObject sceneObject, T obj)
        {
            roadBinding.Add(sceneObject, obj);
        }

        protected T GetBinding(SceneObject roadObject)
        {
            if (roadBinding.TryGetValue(roadObject, out var binding))
                return binding;

            return null;
        }

        protected void RemoveBinding(SceneObject roadObject)
        {
            roadBinding.Remove(roadObject);
        }

        protected void ValidateTrafficLight()
        {
#if UNITY_EDITOR
            var trafficLightHybridService = Simulation.Road.TrafficLightHybridService.Instance;
            var isActive = trafficLightHybridService.IsActive;

            if (!isActive)
            {
                Debug.LogError("<b>TrafficLightHybridService</b> is not active");
                UnityEditor.EditorGUIUtility.PingObject(trafficLightHybridService);
            }
#endif
        }

        protected bool AddPedestrianNodes(TrafficLane trafficLane)
        {
            return trafficLane.trafficLaneType == TrafficLaneType.Pedestrian &&
                (trafficLane.direction == TrafficLaneDirection.Forward || trafficLane.direction == TrafficLaneDirection.Both); // Avoid node duplication
        }

        private Vector3 GetCrosswalkOffset(RoadObject roadObject, RoadSceneUtils.NodeSettings connectedNode, List<TrafficLane> sourceTrafficLanes, bool addOfset)
        {
            float maxDistance = float.MaxValue;

            Waypoint closestWaypoint = null;

            var nodePosition = connectedNode.Position;
            var nodeRotation = connectedNode.Rotation;

            for (int j = 0; j < sourceTrafficLanes.Count; j++)
            {
                if (!AddPedestrianNodes(sourceTrafficLanes[j]))
                    continue;

                var waypoint1 = sourceTrafficLanes[j].waypoints[0];
                var waypoint2 = sourceTrafficLanes[j].waypoints.Last();

                // Straight road with no edge nodes, so add the edges of the previous/next paths (v1.5.0)
                if (addOfset)
                {
                    if (waypoint1.startPoint)
                    {
                        if (waypoint1.prev.Count > 0)
                            waypoint1 = waypoint1.prev[0];
                    }
                    else
                    {
                        if (waypoint1.next.Count > 0)
                            waypoint1 = waypoint1.next[0];
                    }

                    if (waypoint2.startPoint)
                    {
                        if (waypoint2.prev.Count > 0)
                            waypoint2 = waypoint2.prev[0];
                    }
                    else
                    {
                        if (waypoint2.next.Count > 0)
                            waypoint2 = waypoint2.next[0];
                    }
                }

                var nodeDist1 = Vector3.SqrMagnitude(waypoint1.transform.position - nodePosition);

                if (nodeDist1 < maxDistance)
                {
                    maxDistance = nodeDist1;
                    closestWaypoint = waypoint1;
                }

                var nodeDist2 = Vector3.SqrMagnitude(waypoint2.transform.position - nodePosition);

                if (nodeDist2 < maxDistance)
                {
                    maxDistance = nodeDist2;
                    closestWaypoint = waypoint2;
                }
            }

            Vector3 crosswalkOffset = default;

            if (closestWaypoint != null)
            {
                var point1 = closestWaypoint.transform.position;

                crosswalkOffset = TrafficNodeExtension.CalculateCrosswalkOffset(
                    connectedNode.Position,
                    connectedNode.Rotation,
                    connectedNode.Settings.LaneCount,
                    connectedNode.Settings.LaneWidth,
                    connectedNode.Settings.DividerWidth,
                    connectedNode.Settings.IsOneWay,
                    point1);
            }

            return crosswalkOffset;
        }

        private bool HasCrosswalk(Vector3 nodePosition, List<TrafficLane> sourceTrafficLanes)
        {
            for (int j = 0; j < sourceTrafficLanes.Count; j++)
            {
                if (sourceTrafficLanes[j].trafficLaneType != TrafficLaneType.Pedestrian)
                    continue;

                if (!sourceTrafficLanes[j].crossing)
                    continue;

                var waypoint1 = sourceTrafficLanes[j].waypoints[0];
                var waypoint2 = sourceTrafficLanes[j].waypoints.Last();

                var midpoint = (waypoint1.transform.position + waypoint2.transform.position) / 2;

                float distance = Vector3.SqrMagnitude(midpoint - nodePosition);

                if (distance < 9f)
                    return true;
            }

            return false;
        }

        private void AddSplineNode(SceneObject road, List<RoadSceneUtils.NodeSettings> list, float dividerWidth, bool oneway, float laneWidth, int laneCount, bool addPedestrianNodes, int startIndex = 0, bool isEnd = false)
        {
            var pathData = new List<RoadSceneUtils.PathData>();

            int laneIndex = 0;

            var sourceRoad = road as RoadObject;

            if (!oneway || oneway && !isEnd)
            {
                for (int j = startIndex; j < laneCount + startIndex; j++)
                {
                    var laneData = trafficLanes[j];
                    var localPathData = CreatePathData(laneData, laneIndex, laneIndex, !isEnd ? 1 : 0);

                    // Straight road with no edge points
                    if (ShouldAddEdges(sourceRoad, oneway, 0))
                    {
                        AddEdges(laneData, localPathData, true);
                    }

                    if (ShouldAddEdges(sourceRoad, oneway, 1))
                    {
                        AddEdges(laneData, localPathData, false);
                    }

                    laneIndex++;

                    pathData.Add(localPathData);

                    if (firstLineWaypoints == null)
                    {
                        firstLineWaypoints = new List<Vector3>(localPathData.Waypoints);
                    }
                }
            }

            var index = !isEnd ? 0 : 1;
            var position = road.splineContainer.Spline.Knots.ElementAt(index).Position;

            var rotation = Quaternion.identity;

            if (trafficLanes.Count >= 2)
            {
                if (!isEnd)
                {
                    var lane1 = trafficLanes[0].waypoints[0];

                    // Neighbor lane
                    Waypoint lane2;
                    int side = 1;

                    if (trafficLanes.Count != 2 || oneway)
                    {
                        lane2 = trafficLanes[1].waypoints[0];
                    }
                    else
                    {
                        lane2 = trafficLanes[1].waypoints.Last();
                        side = -1;
                    }

                    var dir = Vector3.Normalize(lane1.transform.position - lane2.transform.position);

                    rotation = Quaternion.LookRotation(Vector3.Cross(dir, Vector3.up * side));
                }
                else
                {
                    var lane1 = trafficLanes[0].waypoints.Last();

                    // Neighbor lane
                    Waypoint lane2;
                    int side = 1;

                    if (trafficLanes.Count != 2 || oneway)
                    {
                        lane2 = trafficLanes[1].waypoints.Last();
                    }
                    else
                    {
                        lane2 = trafficLanes[1].waypoints[0];
                        side = -1;
                    }

                    var dir = Vector3.Normalize(lane1.transform.position - lane2.transform.position);

                    rotation = Quaternion.LookRotation(Vector3.Cross(dir, Vector3.down * side));
                }
            }
            else
            {
                rotation = RoadSceneUtils.GetNodeRotation(firstLineWaypoints, isEnd);
            }

            list.Add(new RoadSceneUtils.NodeSettings()
            {
                Position = position,
                Rotation = rotation,
                Settings = new RoadSceneUtils.BaseSettings()
                {
                    Offset = Offset,
                    LaneWidth = laneWidth,
                    LaneCount = laneCount,
                    DividerWidth = dividerWidth,
                    IsOneWay = oneway,
                    IsEndOfOneWay = oneway && index == 1,
                    AddCrosswalk = false,
                    CrosswalkHeight = crosswalkHeight,
                    CrosswalkWidth = crosswalkWidth,
                    CrosswalkShapeType = crosswalkShapeType,
                    AddPedestrianNodes = addPedestrianNodes
                },
                Paths = pathData
            });
        }

        private static void AddEdges(TrafficLane currentLaneData, RoadSceneUtils.PathData localPathData, bool prev)
        {
            var data = prev ? currentLaneData.waypoints[0].prev : currentLaneData.waypoints[currentLaneData.waypoints.Count - 1].next;

            if (data.Count == 0)
                return;

            var nextEdgeNode = data[0];

            if (prev)
            {
                localPathData.Waypoints.Insert(0, nextEdgeNode.transform.position);
            }
            else
            {
                localPathData.Waypoints.Add(nextEdgeNode.transform.position);
            }
        }

        private RoadSceneUtils.PathData CreatePathData(TrafficLane laneData, int laneIndex = -1, int connectedLaneIndex = -1, int connectedNodeIndex = -1)
        {
            var localPathData = new RoadSceneUtils.PathData()
            {
                SpeedLimit = laneData.maxSpeed,
                StartLaneIndex = laneIndex,
                ConnectedLaneIndex = connectedLaneIndex,
                ConnectedNodeIndex = connectedNodeIndex,
                Waypoints = new List<Vector3>()
            };

            foreach (var waypoint in laneData.waypoints)
            {
                localPathData.Waypoints.Add(waypoint.transform.position);
            }

            return localPathData;
        }

        private RoadSceneUtils.NodeSettings GetClosestNode(RoadObject roadObject, Vector3 intersectionPos)
        {
            var creator = GetBinding(roadObject);

            return TryToGetClosestNode(creator, intersectionPos);
        }
    }
}
