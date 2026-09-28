using Spirit604.Attributes;
using Spirit604.CityEditor.Road;
using Spirit604.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    [ExecuteInEditMode]
    public class RoadSegment : MonoBehaviourBase
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/roadSegment.html")]
        [SerializeField] private string link;

        [SerializeField] private RoadSegmentPlacer roadSegmentPlacer;
        [SerializeField] private string shortTitleName = "RoadTitle";

        [OnValueChanged(nameof(ShowIntersectedPaths))]
        [SerializeField] private bool showIntersectedPaths;

        public string ShortTitleName { get => shortTitleName; }
        public RoadSegmentPlacer RoadSegmentPlacer { get => roadSegmentPlacer; }

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                if (roadSegmentPlacer == null)
                {
                    roadSegmentPlacer = ObjectUtils.FindObjectOfType<RoadSegmentPlacer>();

                    if (roadSegmentPlacer)
                    {
                        EditorSaver.SetObjectDirty(this);
                    }
                }

                roadSegmentPlacer?.AddRoadSegment(this);
            }
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying)
            {
                RemoveFromCreator();
            }
        }

        public void RemoveFromCreator()
        {
            roadSegmentPlacer?.RemoveSegment(this);
        }
#endif

        public void Initialize(RoadSegmentPlacer roadCreator)
        {
            if (this.roadSegmentPlacer != roadCreator)
            {
                this.roadSegmentPlacer = roadCreator;
                EditorSaver.SetObjectDirty(this);
            }
        }

        [Button]
        public void ConnectNodes()
        {
            ConnectNodes();
        }

        [Button]
        public void ResetNodes()
        {
            TrafficNode[] trafficNodes = GetComponentsInChildren<TrafficNode>();

            foreach (TrafficNode trafficNode in trafficNodes)
            {
                trafficNode.ResetNode();
            }
        }

        [Button]
        public void BakeData()
        {
            var roadBakers = this.gameObject.GetComponentsInChildren<IBakeRoad>();

            BakeTrafficNodes();

            for (int i = 0; i < roadBakers?.Length; i++)
            {
                roadBakers[i].Bake();
            }
        }

        public void ConnectNodes(TrafficNode.AutoConnectionSettings settings)
        {
            TrafficNode[] trafficNodes = GetComponentsInChildren<TrafficNode>();

            foreach (TrafficNode trafficNode in trafficNodes)
            {
                trafficNode.CheckExternalConnection();
                trafficNode.ConnectSegments(settings);
            }
        }

        public void ShowIntersectedPaths()
        {
            var trafficLightCrossroad = GetComponent<TrafficLightCrossroad>();

            if (trafficLightCrossroad != null)
            {
                var trafficNodes = trafficLightCrossroad.TrafficNodes;

                for (int i = 0; i < trafficNodes?.Count; i++)
                {
                    trafficNodes[i].IterateAllPaths(path => path.ShowIntersectedPoints = showIntersectedPaths);
                }
            }
        }

        private void BakeTrafficNodes()
        {
            TrafficNode[] trafficNodes = GetComponentsInChildren<TrafficNode>();

            for (int i = 0; i < trafficNodes.Length; i++)
            {
                trafficNodes[i].ClearIntersectionLanes();
            }

            for (int indexNode1 = 0; indexNode1 < trafficNodes.Length; indexNode1++)
            {
                for (int indexNode2 = 0; indexNode2 < trafficNodes.Length; indexNode2++)
                {
                    var lanes1 = trafficNodes[indexNode1].Lanes;
                    var lanes2 = trafficNodes[indexNode2].Lanes;
                    var externalLanes1 = trafficNodes[indexNode1].ExternalLanes;
                    var externalLanes2 = trafficNodes[indexNode2].ExternalLanes;

                    IterateIntesectionLanes(transform, indexNode1, indexNode2, lanes1, lanes2);

                    bool advancedCalculation = true;

                    if (advancedCalculation)
                    {
                        IterateIntesectionLanes(transform, indexNode1, indexNode2, lanes1, externalLanes2);
                        IterateIntesectionLanes(transform, indexNode1, indexNode2, lanes2, externalLanes1);
                        IterateIntesectionLanes(transform, indexNode1, indexNode2, externalLanes1, externalLanes2);
                    }
                }
            }
        }

        private void IterateIntesectionLanes(Transform rootTransform, int indexNode1, int indexNode2, List<LaneArray> lanes1, List<LaneArray> lanes2)
        {
            for (int laneIndex1 = 0; laneIndex1 < lanes1?.Count; laneIndex1++)
            {
                var paths1 = lanes1[laneIndex1].paths;

                for (int laneIndex2 = 0; laneIndex2 < lanes2?.Count; laneIndex2++)
                {
                    if (indexNode1 == indexNode2 && laneIndex1 == laneIndex2) continue;

                    var paths2 = lanes2[laneIndex2].paths;

                    CheckIntersections(rootTransform, paths1, paths2);
                }
            }
        }

        private void CheckIntersections(Transform rootTransform, List<Path> paths1, List<Path> paths2)
        {
            for (int i = 0; i < paths1.Count; i++)
            {
                Vector3[] points1 = paths1[i].WaypointPositionArray;

                for (int j = 0; j < paths2.Count; j++)
                {
                    Vector3[] points2 = paths2[j].WaypointPositionArray;

                    var intersectPoint = PathHelper.GetPathIntersection(points1, points2, out var index1, out var index2);

                    if (intersectPoint == Vector3.zero) continue;

                    intersectPoint = rootTransform.transform.InverseTransformPoint(intersectPoint);

                    var intersectedPath = paths1[i].Intersects.Where(item => item.IntersectedPath == paths2[j]).FirstOrDefault();

                    if (intersectedPath != null) continue;

                    paths1[i].AddIntersectPoint(new Path.IntersectPointInfo()
                    {
                        IntersectedPath = paths2[j],
                        IntersectPoint = intersectPoint,
                        LocalNodeIndex = index1,
                        LocalSpace = true
                    });

                    paths2[j].AddIntersectPoint(new Path.IntersectPointInfo()
                    {
                        IntersectedPath = paths1[i],
                        IntersectPoint = intersectPoint,
                        LocalNodeIndex = index2,
                        LocalSpace = true
                    });

                    EditorSaver.SetObjectDirty(paths1[i]);
                    EditorSaver.SetObjectDirty(paths2[j]);
                }
            }

            for (int i = 0; i < paths1.Count; i++)
            {
                paths1[i].SortIntersectsByDistance();
            }
        }
    }
}