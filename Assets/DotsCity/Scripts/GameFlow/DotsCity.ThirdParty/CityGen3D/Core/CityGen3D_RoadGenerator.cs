using CityGen3D;
using Spirit604.CityEditor.Road;
using Spirit604.CityEditor.Utils;
using Spirit604.DotsCity.Core;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadGeneration
{
    public class CityGen3D_RoadGenerator : MonoBehaviour
    {
        private const string MinorRoad = "Minor Road";

        [Serializable]
        class IntersectionPairData
        {
            public int Hash;
            public Vector3 Position;
            public Vector2Int Indexes;
            public (bool, bool) Edges;
            public (RoadSegmentCreator, RoadSegmentCreator) Pair;
        }

        [Serializable]
        public class WarningData
        {
            public RoadSegmentCreator Segment;
            public string Message;
        }

        [Serializable]
        public class Pair
        {
            public RoadSegmentCreator Item1;
            public RoadSegmentCreator Item2;
        }

        class IntersectPointData
        {
            public int Hash;
            public RoadSegmentCreator Creator;
            public Vector3 Position;
            public int Index;
            public bool Edge;
        }

        class BrokenSplineData
        {
            public bool Broken;
            public RoadSegmentCreator Spline;
        }

        [Tooltip("Hash cell size of intersecting roads, used to generate split points for splines")]
        public float crossingHashSize = 5;

        [Tooltip("Max distance between edges of spline roads to ensure that they are connected by edges")]
        public float edgeDistance = 0.2f;

        [Tooltip("Max distance to continue edges of spline road to ensure that close edges of spline roads are intersected")]
        public float maxConnectionDistance = 1f;

        [Tooltip("Max distance between neighboring intersection points to merge them if they are close enough")]
        public float maxIntersectionDistance = 5f;

        [Tooltip("Additional offset of split spline roads at the edges")]
        [Range(-5f, 5f)] public float cutOffset;

        [Tooltip("Intersecting straight roads are automatically split at the intersection points")]
        public bool splitRoads = true;

        [Tooltip("Crossings are automatically generated at intersection points")]
        public bool generateCrossings = true;

        [Tooltip("Automatically offset traffic nodes that overlap with other traffic nodes in intersections")]
        public bool fixOverlapCrossings = true;

        [Tooltip("Max offset for overlapping traffic nodes")]
        [Range(0.1f, 20f)] public float maxOffsetOverlap = 8f;

        [Tooltip("Adds distance to width of traffic nodes to check for overlap")]
        [Range(-1f, 5f)] public float additionalOverlapDistance = 1f;

        [Tooltip("Distance multiplier of calculated offset for overlapping traffic nodes")]
        [Range(0.1f, 3f)] public float overlapDistanceRate = 1f;

        [Tooltip("Minimum distance for path, otherwise it will be merged in the center of the map with traffic nodes of neighboring intersections")]
        [Range(0.1f, 10f)]
        public float minPathLength = 3f;

        public bool addMinorRoads = true;
        public bool addUTurns = true;

        [Tooltip("Roads outside the map will be deleted")]
        public bool cleanupOutOfTheMap;

        [Tooltip("Auto-bake generated roads")]
        public bool autoBakeRoad = true;

        [Tooltip("Only streets with these names will be created")]
        public List<string> targetRoadNames = new List<string>()
        {
            "Main Road",
            "Dual",
            "MotorWay",
            MinorRoad
        };

        public List<GameObject> tempObjects = new List<GameObject>();

        [Tooltip("Automatically created dead U-turns of road segments")]
        public List<TrafficNode> uTurns = new List<TrafficNode>();

        [Tooltip("Dead ends of the road segments")]
        public List<TrafficNode> deadEnds = new List<TrafficNode>();

        [Tooltip("Possible failed road segments")]
        public List<WarningData> warningCreators = new List<WarningData>();

        [Tooltip("Automatically fixed overllaped traffic nodes")]
        public List<RoadSegmentCreator> fixedOverlapCrossings = new List<RoadSegmentCreator>();

        [Tooltip("Segment for which automatic offset for overlapping doesn't work & user should fix these cases manually")]
        public List<RoadSegmentCreator> notFixedOverlapCrossings = new List<RoadSegmentCreator>();

        [Tooltip("Segment for which automatic offset for overlapping partially work & user should fix these cases manually")]
        public List<RoadSegmentCreator> partiallyFixedOverlapCrossings = new List<RoadSegmentCreator>();

        public List<MapRoad> ignoredRoads = new List<MapRoad>();

        [Tooltip("Show select buttons of all roads")]
        public bool showSelectButtons;
        public bool drawIntersectPoints;
        public bool drawIntersectAveragePoint = true;
        public bool drawIntersectLocalPoints;
        public bool drawIntersectHashes;

#if UNITY_EDITOR

        private Dictionary<RoadSegmentCreator, List<RoadSegmentCreator>> intersectedRoads = new Dictionary<RoadSegmentCreator, List<RoadSegmentCreator>>();

        // Source spline road creator / new spline road created split by intersection points
        private Dictionary<RoadSegmentCreator, List<RoadSegmentCreator>> roadSplits = new Dictionary<RoadSegmentCreator, List<RoadSegmentCreator>>();

        private Dictionary<RoadSegmentCreator, List<IntersectPointData>> roadIntersectPoints = new Dictionary<RoadSegmentCreator, List<IntersectPointData>>();

        // Hash / points
        private Dictionary<int, List<IntersectionPairData>> intersections = new Dictionary<int, List<IntersectionPairData>>();

        private List<RoadSegmentCreator> splineRoads = new List<RoadSegmentCreator>();
        private List<GameObject> destroyList = new List<GameObject>();
        private List<RoadSegmentCreator> createdIntersections = new List<RoadSegmentCreator>();
        private List<RoadSegmentCreator> allRoads = new List<RoadSegmentCreator>();

        public RoadSegmentCreator[] SceneRoads { get; private set; }

        private RoadParent roadParent;

        public void Generate()
        {
            if (roadParent == null)
                roadParent = ObjectUtils.FindObjectOfType<RoadParent>();

            if (!roadParent)
            {
                Debug.Log("RoadParent not found.");
                return;
            }

            Clear();

            var roads = Map.Instance.mapRoads.GetMapRoads();

            var filteredRoads = new List<MapRoad>();

            foreach (MapRoad road in roads)
            {
                if (!ValidRoad(road))
                {
                    ignoredRoads.Add(road);
                    continue;
                }
                else
                {
                    if (road.name.Contains(MinorRoad) && !addMinorRoads)
                    {
                        ignoredRoads.Add(road);
                        continue;
                    }
                }

                filteredRoads.Add(road);
            }

            var segmentCounter = new Dictionary<string, int>();
            var originSegment = new Dictionary<string, GameObject>();

            foreach (MapRoad road in filteredRoads)
            {
                var poses = road.GetPositions().ToArray();
                var lanes = road.GetLanes();

                for (int i = 0; i < poses.Length; i++)
                {
                    if (lanes.Count == 0) Selection.activeGameObject = road.gameObject;
                    poses[i].y = lanes[0][i].y;
                }

                var laneCount = 0;
                var oneway = road.oneWay;

                if (!oneway)
                {
                    laneCount = road.lanes / 2;
                }
                else
                {
                    laneCount = road.lanes;
                }

                if (laneCount == 0)
                {
                    laneCount = 1;
                    oneway = true;
                }

                laneCount = Mathf.Clamp(laneCount, 1, int.MaxValue);

                var creator = RoadSceneUtils.CreateStraightSegment(poses.ToList(), new RoadSceneUtils.StraightSegmentSettings()
                {
                    IsOneWay = oneway,
                    LaneCount = laneCount,
                    LaneWidth = road.laneWidth,
                    AddPedestrianNodes = false
                }, segmentName: $"{road.name}_segment");

                if (!segmentCounter.ContainsKey(creator.name))
                {
                    segmentCounter.Add(creator.name, 1);
                    originSegment.Add(creator.name, creator.gameObject);
                }
                else
                {
                    segmentCounter[creator.name]++;

                    if (segmentCounter[creator.name] == 2)
                    {
                        originSegment[creator.name].name += "_1";
                    }

                    creator.name += $"_{segmentCounter[creator.name]}";
                }

                creator.transform.SetParent(roadParent.transform, true);

                splineRoads.Add(creator);
                allRoads.Add(creator);
            }

            IterateCrossings();

            GenerateCrossings();
            DestroyBrokenSplines();
            CompleteGeneration();
            CheckOverllapedCrossings();
            DestroyBrokenSplines();
            DestroyOutOfMap();
            FindDeadEnds();
            ProcessFinalStep();
        }

        public void UpdateSceneRoads()
        {
            SceneRoads = ObjectUtils.FindObjectsOfType<RoadSegmentCreator>();
        }

        private void DestroyOutOfMap()
        {
            if (!cleanupOutOfTheMap) return;

            destroyList.Clear();

            foreach (var road in allRoads)
            {
                if (!road) continue;

                bool destroy = true;

                road.IterateAllNodes(node =>
                {
                    var landScape1 = CityGen3D.EditorExtension.Generator.Instance.GetLandscapeAtWorldPosition(node.transform.position);

                    if (landScape1 != null)
                    {
                        destroy = false;
                    }
                });

                if (destroy)
                {
                    road.IterateAllNodes(node =>
                    {
                        var mergeNode = node.MergeNode;

                        if (mergeNode)
                            mergeNode.UnselectMerge(makeDefaultConnection: true);
                    });

                    destroyList.Add(road.gameObject);
                }
            }

            foreach (var item in destroyList)
            {
                allRoads.Remove(item.GetComponent<RoadSegmentCreator>());
            }

            destroyList.DestroyGameObjects();
        }

        private void FindDeadEnds()
        {
            if (!generateCrossings)
                return;

            foreach (var road in allRoads)
            {
                if (!road) continue;

                road.IterateAllNodes(node =>
                {
                    CheckDeadEnd(node);
                });
            }
        }

        private void CheckDeadEnd(TrafficNode node)
        {
            if (!node || !node.HasLeftLanes || node.MergeNode)
                return;

            var path = node.GetAnyPath(true);

            if (!path)
            {
                var landScape1 = CityGen3D.EditorExtension.Generator.Instance.GetLandscapeAtWorldPosition(node.transform.position);

                if (landScape1 == null || !addUTurns)
                {
                    deadEnds.Add(node);
                    node.TrafficNodeType = TrafficNodeType.DestroyVehicle;
                }
                else
                {
                    uTurns.Add(node);
                    RoadSceneUtils.CreateUTurnPaths(node);
                }
            }
        }

        private void DestroyBrokenSplines()
        {
            int index = 0;

            while (index < splineRoads.Count)
            {
                if (roadSplits.ContainsKey(splineRoads[index]))
                {
                    var list = roadSplits[splineRoads[index]];

                    splineRoads.RemoveAt(index);
                    splineRoads.InsertRange(index, list);
                }

                index++;
            }

            var destroy = new List<BrokenSplineData>();

            foreach (var splineRoad in splineRoads)
            {
                if (!splineRoad) continue;

                var path = splineRoad.GetTempPath(0);

                var length = path.GetPathLength();

                if (length >= 20)
                    continue;

                var point = splineRoad.GetCenterPoint();

                var broken1 = BrokenNode(splineRoad.TryToGetNode(0), point);
                var broken2 = BrokenNode(splineRoad.TryToGetNode(1), point);

                bool added = false;

                if (broken1 || broken2 || length < minPathLength)
                {
                    destroy.Add(new BrokenSplineData()
                    {
                        Broken = broken1 || broken2,
                        Spline = splineRoad
                    });

                    added = true;
                }
                else
                {
                    var node1 = path.Nodes[0].transform.position;
                    var node2 = path.Nodes.Last().transform.position;

                    var dir2 = Vector3.Normalize(node2 - node1);

                    var dir1 = Vector3.Normalize(splineRoad.TryToGetNode(1).transform.position - splineRoad.TryToGetNode(0).transform.position);
                    var dot1 = Vector3.Dot(dir1, dir2);

                    if (dot1 < 0)
                    {
                        destroy.Add(new BrokenSplineData()
                        {
                            Broken = true,
                            Spline = splineRoad
                        });

                        added = true;
                    }
                }

                if (!added)
                {
                    var node1 = splineRoad.TryToGetNode(0);
                    var node2 = splineRoad.TryToGetNode(1);

                    if (node1.MergeNode != null && node2.MergeNode != null)
                    {
                        var crossing1 = node1.MergeNode.TrafficLightCrossroad;
                        var crossing2 = node2.MergeNode.TrafficLightCrossroad;

                        if (crossing1 == crossing2)
                        {
                            added = true;

                            var creator = crossing1.GetComponent<RoadSegmentCreator>();

                            creator.DestroyNode(node1.MergeNode, false, false);
                            creator.DestroyNode(node2.MergeNode, false, false);
                            creator.CreateAutoCrossroad();

                            destroy.Add(new BrokenSplineData()
                            {
                                Spline = splineRoad
                            });
                        }
                    }
                }
            }

            foreach (var splineRoadData in destroy)
            {
                var splineRoad = splineRoadData.Spline;
                var point = splineRoad.GetCenterPoint();

                var node1 = splineRoad.TryToGetNode(0).MergeNode;

                if (node1 != null)
                {
                    node1.transform.rotation = node1.transform.rotation;

                    if (splineRoadData.Broken)
                    {
                        node1.transform.rotation *= Quaternion.Euler(0, 180, 0);

                        if (node1.IsOneWay)
                        {
                            node1.IsEndOfOneWay = !node1.IsEndOfOneWay;
                        }
                    }

                    node1.transform.position = point;

                    CreateCrossing(node1);
                }

                var node2 = splineRoad.TryToGetNode(1).MergeNode;

                if (node2 != null)
                {
                    node2.transform.rotation = node2.transform.rotation;
                    node2.transform.position = point;

                    if (splineRoadData.Broken)
                    {
                        node2.transform.rotation *= Quaternion.Euler(0, 180, 0);

                        if (node2.IsOneWay)
                        {
                            node2.IsEndOfOneWay = !node2.IsEndOfOneWay;
                        }
                    }

                    CreateCrossing(node2);
                }

                if (node1 && node2)
                {
                    node1.SetMerge(node2);
                }
                else
                {
                    if (node1)
                    {
                        node1.ExternalConnection = TrafficNode.ExternalConnectionType.Path;
                    }

                    if (node2)
                    {
                        node2.ExternalConnection = TrafficNode.ExternalConnectionType.Path;
                    }
                }
            }

            var destroyList = destroy.Select(a => a.Spline).ToList();
            destroyList.DestroyGameObjects();
        }

        private void CheckOverllapedCrossings()
        {
            if (!fixOverlapCrossings) return;

            foreach (var creator in createdIntersections)
            {
                if (creator.TrafficNodeCount == 2) continue;

                bool changed = false;
                bool overlaped = false;
                bool partial = false;

                for (int i = 0; i < creator.CreatedTrafficNodes.Count; i++)
                {
                    var sourceNode = creator.CreatedTrafficNodes[i];
                    var sourceWidth = sourceNode.CalculatedRouteWidth;

                    for (int j = i + 1; j < creator.CreatedTrafficNodes.Count; j++)
                    {
                        var targetNode = creator.CreatedTrafficNodes[j];
                        var targetWidth = sourceNode.CalculatedRouteWidth;

                        var dist = Vector3.Distance(sourceNode.transform.position, targetNode.transform.position);

                        var byDistance = dist < sourceWidth + targetWidth + additionalOverlapDistance;

                        if (byDistance)
                        {
                            overlaped = true;
                            var angle = Vector3.SignedAngle(sourceNode.transform.forward, targetNode.transform.forward, Vector3.up);
                            var absAngle = Mathf.Abs(angle);

                            if (absAngle > 90)
                            {
                                absAngle = 180 - absAngle;
                            }

                            float overlapDistance = (sourceWidth + targetWidth - dist) * 2;

                            var side = angle > 0 ? 1 : -1;

                            RoadSegmentCreator creator1 = null;
                            RoadSegmentCreator creator2 = null;

                            if (sourceNode.MergeNode)
                            {
                                creator1 = sourceNode.MergeNode.TrafficLightCrossroad.GetComponent<RoadSegmentCreator>();
                            }

                            if (targetNode.MergeNode)
                            {
                                creator2 = targetNode.MergeNode.TrafficLightCrossroad.GetComponent<RoadSegmentCreator>();
                            }

                            float cutDistance = overlapDistance * Mathf.Cos(Mathf.Deg2Rad * absAngle / 2) * overlapDistanceRate;

                            cutDistance = Mathf.Min(maxOffsetOverlap, cutDistance);

                            // Almost right angle
                            if (absAngle >= 70 && cutDistance < 1f)
                            {
                                cutDistance = Mathf.Max(targetWidth, sourceWidth);
                                cutDistance = Mathf.Clamp(cutDistance, 0, 4);
                            }

                            var straight1 = creator1 != null && creator1.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomStraightRoad;
                            var custom1 = creator1 != null && creator1.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomSegment && creator1.TrafficNodeCount == 2;

                            var straight2 = creator2 != null && creator2.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomStraightRoad;
                            var custom2 = creator2 != null && creator2.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomSegment && creator2.TrafficNodeCount == 2;

                            var localChanged = false;

                            if (straight1 || custom1)
                            {
                                changed = true;
                                localChanged = true;

                                if (straight1)
                                {
                                    creator1.CutSegment(sourceNode.MergeNode, cutDistance);
                                    sourceNode.transform.position = sourceNode.MergeNode.transform.position;
                                    sourceNode.transform.rotation = Quaternion.LookRotation(-sourceNode.MergeNode.transform.forward);
                                }
                                else
                                {
                                    sourceNode.transform.position += sourceNode.transform.forward * cutDistance;
                                    sourceNode.MergeNode.transform.position += sourceNode.transform.forward * cutDistance;
                                    creator1.CreateAutoCrossroad();
                                }
                            }

                            if (straight2 || custom2)
                            {
                                changed = true;
                                localChanged = true;

                                if (straight2)
                                {
                                    creator2.CutSegment(targetNode.MergeNode, cutDistance);
                                    targetNode.transform.position = targetNode.MergeNode.transform.position;
                                    targetNode.transform.rotation = Quaternion.LookRotation(-targetNode.MergeNode.transform.forward);
                                }
                                else
                                {
                                    targetNode.transform.position += targetNode.transform.forward * cutDistance;
                                    targetNode.MergeNode.transform.position += targetNode.transform.forward * cutDistance;
                                    creator2.CreateAutoCrossroad();
                                }
                            }

                            if (!localChanged)
                            {
                                partial = true;
                            }
                        }
                    }
                }

                if (changed)
                {
                    creator.CreateAutoCrossroad();
                    fixedOverlapCrossings.TryToAdd(creator);

                    if (partial)
                    {
                        partiallyFixedOverlapCrossings.TryToAdd(creator);
                    }
                }
                else if (overlaped)
                {
                    notFixedOverlapCrossings.TryToAdd(creator);
                }
            }
        }

        private void CreateCrossing(TrafficNode node1)
        {
            var creator = node1.TrafficLightCrossroad.GetComponent<RoadSegmentCreator>();
            creator.CreateAutoCrossroad();

            createdIntersections.Add(creator);
            allRoads.Add(creator);
        }

        private void CheckIntersection(RoadSegmentCreator creator)
        {
            if (creator.TrafficNodeCount > 2)
            {
                if (!ValidateCrossing(creator))
                    return;

                creator.InverseCleanup = true;
                creator.CreateAutoCrossroad();

                if (!ValidateCrossing(creator))
                    return;

                creator.CleanupSelfIntersection = false;
                creator.InverseCleanup = false;
                creator.CreateAutoCrossroad();

                warningCreators.Add(new WarningData()
                {
                    Segment = creator,
                    Message = "Possible wrong connected paths",
                });
            }
            else
            {
                var node1 = creator.TryToGetNode(0);
                var node2 = creator.TryToGetNode(1);

                if (node1.IsOneWay && node2.IsOneWay && node1.IsEndOfOneWay == node2.IsEndOfOneWay)
                {
                    if (node1.MergeNode)
                    {
                        node1.MergeNode.UnselectMerge(makeDefaultConnection: true);
                    }

                    if (node2.MergeNode)
                    {
                        node2.MergeNode.UnselectMerge(makeDefaultConnection: true);
                    }

                    DestroyImmediate(creator.gameObject);
                }
            }
        }

        private static bool ValidateCrossing(RoadSegmentCreator creator)
        {
            bool error = false;

            creator.IterateAllNodes(node =>
            {
                if (node.HasRightLanes)
                {
                    node.IterateAllLanes((laneIndex, external) =>
                    {
                        var laneData = node.TryToGetLaneData(laneIndex, external);

                        if (laneData == null || laneData.paths.Count == 0)
                        {
                            error = true;
                        }

                    }, false, false);
                }
            });

            return error;
        }

        private static bool BrokenNode(TrafficNode node, Vector3 point)
        {
            var dir1 = point - node.transform.position;
            var dot1 = Vector3.Dot(dir1, node.transform.forward);

            return dot1 > 0;
        }

        private void CompleteGeneration()
        {
            destroyList.DestroyGameObjects();

            foreach (var intersection in createdIntersections)
            {
                if (!intersection) continue;

                CheckIntersection(intersection);
            }
        }

        private void GenerateCrossings()
        {
            if (!splitRoads || !generateCrossings)
                return;

            var nodes = new List<TrafficNode>();
            var creators = new List<TrafficLightCrossroad>();
            var temp = new HashSet<TrafficLightCrossroad>();

            foreach (var item in intersections)
            {
                var list = item.Value;
                nodes.Clear();
                creators.Clear();

                int count = list.Count;

                foreach (var item2 in list)
                {
                    if (item2.Edges.Item1 && item2.Edges.Item2)
                    {
                        count++;
                    }
                    else if (!item2.Edges.Item1 && item2.Edges.Item2 || item2.Edges.Item1 && !item2.Edges.Item2)
                    {
                        count += 2;
                    }
                    else
                    {
                        count += 3;
                    }
                }

                foreach (var item2 in list)
                {
                    var node1 = GetClosestNode(item2.Position, item2.Pair.Item1);
                    var node2 = GetClosestNode(item2.Position, item2.Pair.Item2);

                    AddNode(nodes, node1.Item1);
                    AddNode(nodes, node1.Item2);
                    AddNode(nodes, node2.Item1);
                    AddNode(nodes, node2.Item2);
                }

                var intersection = RoadSceneUtils.CreateIntersection(nodes, autoMerge: true);

                intersection.additionalSettings = RoadSegmentCreator.AdditionalPathSettingsType.AutoCrossroad;
                EditorSaver.SetObjectDirty(intersection);

                createdIntersections.Add(intersection);
                allRoads.Add(intersection);

                if (intersection)
                {
                    intersection.SetAllPedestrianEnabledState(false);
                    intersection.transform.SetParent(roadParent.transform, true);
                }
            }
        }

        private void AddNode(List<TrafficNode> nodes, TrafficNode node)
        {
            if (node && !destroyList.Contains(node.TrafficLightCrossroad.gameObject))
                nodes.TryToAdd(node);
        }

        protected void IterateCrossings()
        {
            if (!splitRoads)
                return;

            intersectedRoads.Clear();
            roadIntersectPoints.Clear();

            var edgeDistanceSq = edgeDistance * edgeDistance;

            for (int i = 0; i < splineRoads.Count; i++)
            {
                RoadSegmentCreator splineRoad = splineRoads[i];

                var sourcePath = splineRoad.GetTempPath(0);

                var points1 = sourcePath.WaypointPositions.ToArray();

                var startPoint1 = points1[0];
                var endPoint1 = points1[points1.Length - 1];

                points1[0] = startPoint1 + (startPoint1 - points1[1]).normalized * maxConnectionDistance;
                points1[points1.Length - 1] = endPoint1 + (endPoint1 - points1[points1.Length - 2]).normalized * maxConnectionDistance;

                for (int j = i + 1; j < splineRoads.Count; j++)
                {
                    RoadSegmentCreator intersectedSplineRoad = splineRoads[j];

                    if (splineRoad == intersectedSplineRoad) continue;

                    var targetPath = intersectedSplineRoad.GetTempPath(0);

                    Vector3[] points2 = targetPath.WaypointPositions.ToArray();

                    var startPoint2 = points2[0];
                    var endPoint2 = points2[points2.Length - 1];

                    points2[0] = startPoint2 + (startPoint2 - points2[1]).normalized * maxConnectionDistance;
                    points2[points2.Length - 1] = endPoint2 + (endPoint2 - points2[points2.Length - 2]).normalized * maxConnectionDistance;

                    Vector3 intersectPoint = default;

                    bool edgeConnection = false;

                    (bool, bool) edges = default;

                    var index1 = -1;
                    var index2 = -1;

                    if (Vector3.SqrMagnitude(endPoint1 - endPoint2) < edgeDistanceSq)
                    {
                        edgeConnection = true;
                        intersectPoint = (endPoint1 + endPoint2) / 2;

                        index1 = points1.Length - 2;
                        index2 = points2.Length - 2;
                    }

                    if (!edgeConnection && Vector3.SqrMagnitude(endPoint1 - startPoint2) < edgeDistanceSq)
                    {
                        edgeConnection = true;
                        intersectPoint = (endPoint1 + startPoint2) / 2;

                        index1 = points1.Length - 2;
                        index2 = 0;
                    }

                    if (!edgeConnection && Vector3.SqrMagnitude(startPoint1 - endPoint2) < edgeDistanceSq)
                    {
                        edgeConnection = true;
                        intersectPoint = (startPoint1 + endPoint2) / 2;

                        index1 = 0;
                        index2 = points2.Length - 2;
                    }

                    if (!edgeConnection)
                    {
                        intersectPoint = PathHelper.GetPathIntersection(points1, points2, out index1, out index2);
                    }

                    if (intersectPoint == Vector3.zero) continue;

                    var hash = GetHash(intersectPoint, crossingHashSize);

                    if (!intersectedRoads.ContainsKey(splineRoad))
                    {
                        intersectedRoads.Add(splineRoad, new List<RoadSegmentCreator>());
                        roadIntersectPoints.Add(splineRoad, new List<IntersectPointData>());
                    }

                    if (!intersectedRoads.ContainsKey(intersectedSplineRoad))
                    {
                        intersectedRoads.Add(intersectedSplineRoad, new List<RoadSegmentCreator>());
                        roadIntersectPoints.Add(intersectedSplineRoad, new List<IntersectPointData>());
                    }

                    float distSq = maxConnectionDistance * maxConnectionDistance;

                    if (index1 <= 0)
                    {
                        var dist = Vector3.SqrMagnitude(intersectPoint - startPoint1);

                        if (dist < distSq)
                        {
                            edges.Item1 = true;
                        }
                    }

                    if (index1 >= points1.Length - 2)
                    {
                        var dist = Vector3.SqrMagnitude(intersectPoint - endPoint1);

                        if (dist < distSq)
                        {
                            edges.Item1 = true;
                        }
                    }

                    if (index2 == 0)
                    {
                        var dist = Vector3.SqrMagnitude(intersectPoint - startPoint2);

                        if (dist < distSq)
                        {
                            edges.Item2 = true;
                        }
                    }

                    if (index2 >= points2.Length - 2)
                    {
                        var dist = Vector3.SqrMagnitude(intersectPoint - endPoint2);

                        if (dist < distSq)
                        {
                            edges.Item2 = true;
                        }
                    }

                    var hashes = HashMapHelper.GetHashMapPosition9Cells(intersectPoint.Flat(), crossingHashSize, crossingHashSize, allocator: Allocator.Temp);

                    for (var k = 0; k < hashes.Length; k++)
                    {
                        var hash2 = hashes[k];

                        if (hash2 == hash) continue;

                        if (intersections.ContainsKey(hash2))
                        {
                            var point = intersections[hash2][0];

                            var distance = Vector3.Distance(point.Position, intersectPoint);

                            if (distance < maxIntersectionDistance)
                            {
                                hash = hash2;
                                break;
                            }
                        }
                    }

                    hashes.Dispose();

                    intersectedRoads[splineRoad].TryToAdd(intersectedSplineRoad);

                    roadIntersectPoints[splineRoad].Add(new IntersectPointData()
                    {
                        Hash = hash,
                        Position = intersectPoint,
                        Creator = intersectedSplineRoad,
                        Edge = edges.Item1,
                        Index = index1,
                    });

                    intersectedRoads[intersectedSplineRoad].TryToAdd(splineRoad);

                    roadIntersectPoints[intersectedSplineRoad].Add(new IntersectPointData()
                    {
                        Hash = hash,
                        Position = intersectPoint,
                        Creator = splineRoad,
                        Index = index2,
                        Edge = edges.Item2,
                    });

                    if (!intersections.ContainsKey(hash))
                    {
                        intersections.Add(hash, new List<IntersectionPairData>());
                    }

                    intersections[hash].Add(new IntersectionPairData()
                    {
                        Hash = hash,
                        Indexes = new Vector2Int(index1, index2),
                        Edges = edges,
                        Pair = (intersectedSplineRoad, splineRoad),
                        Position = intersectPoint,
                    });
                }
            }

            var connectionBinding2Sorted = new Dictionary<RoadSegmentCreator, List<IntersectPointData>>();

            foreach (var item in roadIntersectPoints)
            {
                var list = item.Value;
                var lastPos = item.Key.TryToGetNode(1).transform.position;

                list = list.OrderBy(a => a.Index).ThenByDescending(a => Vector3.SqrMagnitude(a.Position - lastPos)).ToList();

                connectionBinding2Sorted.Add(item.Key, list);
            }

            roadIntersectPoints = connectionBinding2Sorted;

            var addedHashes = new HashSet<int>();

            foreach (var item in roadIntersectPoints)
            {
                var source = item.Key;
                var sourcePath2 = source.GetTempPath(0);
                var list = item.Value;
                addedHashes.Clear();

                int part = 1;

                for (int j = 0; j <= list.Count; j++)
                {
                    var index = j;
                    var last = index == list.Count;

                    if (last)
                    {
                        index--;
                    }

                    IntersectPointData item2 = list[index];

                    if (addedHashes.Contains(item2.Hash) && !last)
                    {
                        continue;
                    }

                    addedHashes.Add(item2.Hash);

                    var intersectedSegment = item2.Creator;

                    if (item2.Index == 0 && item2.Edge)
                    {
                        continue;
                    }

                    if (last || !last && !destroyList.Contains(source.gameObject))
                    {
                        if (list[index].Index >= sourcePath2.Nodes.Count - 2)
                            continue;
                    }

                    destroyList.TryToAdd(source.gameObject);

                    var partialSegment = Instantiate(source, source.transform.parent);

                    partialSegment.name = partialSegment.name.Replace("(Clone)", $"_part{part}");

                    var copyPath = partialSegment.GetTempPath(0);

                    part++;

#if UNITY_EDITOR
                    partialSegment.RegenerateAllIds();
#endif

                    if (!roadSplits.ContainsKey(source))
                    {
                        roadSplits.Add(source, new List<RoadSegmentCreator>());
                    }

                    roadSplits[source].Add(partialSegment);

                    var nodes = copyPath.WaypointPositions.ToList();

                    int index1 = -1;
                    int index2 = -1;

                    if (!last)
                    {
                        if (j - 1 >= 0)
                        {
                            GetOrInsertPoint(copyPath, list[j - 1].Position, out index1);
                        }

                        GetOrInsertPoint(copyPath, list[index].Position, out index2);
                    }
                    else
                    {
                        GetOrInsertPoint(copyPath, list[index].Position, out index1);
                    }

                    var deleteCount = copyPath.Nodes.Count - index2 - 1;

                    if (index2 != -1)
                    {
                        while (deleteCount > 0)
                        {
                            copyPath.RemoveNodeAt(copyPath.Nodes.Count - 1, false, false);
                            deleteCount--;
                        }
                    }

                    if (index1 != -1)
                    {
                        while (index1 > 0)
                        {
                            copyPath.RemoveNodeAt(0, false, false);
                            index1--;
                        }
                    }

                    partialSegment.CutSegment(0.001f, 0.001f);

                    float cutDistance1, cutDistance2;

                    GetCutDistance(partialSegment, intersectedSegment, item2.Position, out cutDistance1, out cutDistance2);

                    if (index - 1 >= 0)
                    {
                        intersectedSegment = list[index - 1].Creator;

                        GetCutDistance(partialSegment, intersectedSegment, item2.Position, out cutDistance1, out var cutDistance22);
                    }

                    partialSegment.CutSegment(cutDistance1, cutDistance2);
                    allRoads.Add(partialSegment);
                }

                if (!destroyList.Contains(source.gameObject))
                {
                    if (list.Count > 0)
                    {
                        float cutDistance1 = 0;
                        float cutDistance2 = 0;

                        if (list[0].Index == 0)
                        {
                            var intersectedSegment = list[0].Creator;
                            GetCutDistance(source, intersectedSegment, list[0].Position, out cutDistance1, out cutDistance2);
                        }

                        if (list.Last().Index >= sourcePath2.Nodes.Count - 2)
                        {
                            var intersectedSegment = list.Last().Creator;

                            GetCutDistance(source, intersectedSegment, list.Last().Position, out cutDistance1, out cutDistance2);
                        }

                        if (cutDistance1 > 0 || cutDistance2 > 0)
                        {
                            source.CutSegment(cutDistance1, cutDistance2);
                        }
                    }
                }
                else
                {
                    allRoads.Remove(source);
                }
            }
        }

        private void GetCutDistance(RoadSegmentCreator source, RoadSegmentCreator intersectedSegment, Vector3 point, out float cutDistance1, out float cutDistance2)
        {
            var node = intersectedSegment.TryToGetNode(0);

            var laneCount = Mathf.Max(2, (float)node.TotalLaneCount);
            cutDistance1 = (float)laneCount * node.LaneWidth + cutOffset;
            cutDistance2 = (float)laneCount * node.LaneWidth + cutOffset;
        }

        private (TrafficNode, TrafficNode) GetClosestNode(Vector3 pos, RoadSegmentCreator creator)
        {
            if (roadSplits.ContainsKey(creator))
            {
                var nodes = new List<TrafficNode>();

                foreach (var splitCreator in roadSplits[creator])
                {
                    nodes.Add(splitCreator.TryToGetNode(0));
                    nodes.Add(splitCreator.TryToGetNode(1));
                }

                nodes = nodes.OrderBy(a => Vector3.SqrMagnitude(a.transform.position - pos)).ToList();

                (TrafficNode, TrafficNode) result = default;

                result.Item1 = nodes[0];

                for (int i = 1; i < nodes.Count; i++)
                {
                    if (nodes[i].TrafficLightCrossroad != nodes[0].TrafficLightCrossroad)
                    {
                        result.Item2 = nodes[i];
                        break;
                    }
                }

                if (result.Item1 && result.Item2)
                {
                    var dir1 = (result.Item1.transform.position - pos).normalized;
                    var dir2 = (result.Item2.transform.position - pos).normalized;

                    var dot = Vector3.Dot(dir1, dir2);

                    // Next segment not related to current intersection
                    if (dot > 0)
                    {
                        result.Item2 = null;
                    }
                }

                return result;
            }
            else
            {
                var node = creator.TryToGetClosestNode(pos);
                return (node, node);
            }
        }

        private Transform GetOrInsertPoint(Path tempCustomPath1, Vector3 point, out int index, float nodePrecision = 1f)
        {
            var closestPoint = PathHelper.GetAttachPoint(tempCustomPath1, point, out index);

            if (Vector3.Distance(point, closestPoint) > nodePrecision)
            {
                closestPoint = point;
                index = -1;
            }

            var distance = float.MaxValue;

            Transform closestNode = null;
            bool cornerNode = false;

            for (int i = 0; i < tempCustomPath1.Nodes.Count; i++)
            {
                var node = tempCustomPath1.Nodes[i];

                var dist = Vector3.SqrMagnitude(node.transform.position - closestPoint);

                if (dist < distance)
                {
                    distance = dist;
                    closestNode = node;
                    index = i;
                    cornerNode = i == 0 || i == tempCustomPath1.Nodes.Count - 1;
                }
            }

            if (closestNode != null)
                distance = Vector3.Distance(closestNode.transform.position, closestPoint);

            if ((distance > nodePrecision && !cornerNode) || closestNode == null)
            {
                closestNode = tempCustomPath1.InsertNode(closestPoint, index, false);
            }
            else
            {
                closestNode.transform.position = closestPoint;
            }

            return closestNode;
        }

        private bool ValidRoad(MapRoad road)
        {
            bool match = false;

            for (int i = 0; i < targetRoadNames.Count; i++)
            {
                if (road.name.Contains(targetRoadNames[i], StringComparison.InvariantCultureIgnoreCase))
                {
                    match = true;
                    break;
                }
            }

            if (!match)
                return false;

            var surface = road.GetSurface();

            if (surface == null) return false;

            var poses = road.GetPositions();

            if (poses == null) return false;

            return true;
        }

        private void Clear()
        {
            var creators = roadParent.GetComponentsInChildren<RoadSegmentCreator>().ToList();
            creators.DestroyGameObjects();

            splineRoads.Clear();
            intersections.Clear();
            roadSplits.Clear();
            tempObjects.DestroyGameObjects();
            destroyList.Clear();
            tempObjects.Clear();
            createdIntersections.Clear();
            notFixedOverlapCrossings.Clear();
            partiallyFixedOverlapCrossings.Clear();
            fixedOverlapCrossings.Clear();

            warningCreators.Clear();
            deadEnds.Clear();
            uTurns.Clear();
            allRoads.Clear();
            ignoredRoads.Clear();
        }

        private int GetHash(Vector3 item, float hashSize) => HashMapHelper.GetHashMapPosition(item.Flat(), hashSize);

        private void ProcessFinalStep()
        {
            UpdateSceneRoads();

            if (cleanupOutOfTheMap)
                ClearNullReports();

            if (autoBakeRoad)
                roadParent.BakePathData();
        }

        private void ClearNullReports()
        {
            ClearNulls(uTurns);
            ClearNulls(deadEnds);
            ClearNulls(fixedOverlapCrossings);
            ClearNulls(notFixedOverlapCrossings);
            ClearNulls(partiallyFixedOverlapCrossings);
            ClearNulls(warningCreators);
        }

        private void ClearNulls<T>(List<T> source) where T : Component
        {
            int index = 0;

            while (source.Count > index)
            {
                if (IsNullOrDestroyed(source[index]))
                {
                    source.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }
        }

        private void ClearNulls(List<WarningData> source)
        {
            int index = 0;

            while (source.Count > index)
            {
                if (IsNullOrDestroyed(source[index].Segment))
                {
                    source.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }
        }

        private bool IsNullOrDestroyed(System.Object obj)
        {
            if (object.ReferenceEquals(obj, null)) return true;

            if (obj is UnityEngine.Object) return (obj as UnityEngine.Object) == null;

            return false;
        }

        private void OnDrawGizmos()
        {
            if (!drawIntersectPoints) return;

            var sourceColor = Gizmos.color;

            foreach (var item in intersections)
            {
                Vector3 pos = default;

                for (int i = 0; i < item.Value.Count; i++)
                {
                    IntersectionPairData pair = item.Value[i];

                    pos += pair.Position;

                    if (drawIntersectLocalPoints)
                    {
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawWireSphere(pos, 1f);
                    }
                }

                Gizmos.color = sourceColor;
                pos /= item.Value.Count;

                if (drawIntersectAveragePoint)
                    Gizmos.DrawCube(pos, Vector3.one * crossingHashSize);

                if (drawIntersectHashes)
                    EditorExtension.DrawWorldString($"Hash {item.Key}", pos + new Vector3(2, 0, 2));
            }
        }

#endif
    }
}
