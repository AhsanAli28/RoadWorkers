using PampelGames.RoadConstructor;
using Spirit604.Attributes;
using Spirit604.CityEditor.Road;
using Spirit604.CityEditor.Utils;
using Spirit604.Extensions;
using Spirit604.Gameplay.Config.Road;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadConstructor
{
    public class RC_EditorSceneGenerator : RC_EditorSceneGeneratorBase<RoadSegmentCreator>
    {
        [SerializeField] private SharedLightStateContainer lightStateContainer;

        [SerializeField] private bool stripSplineNodes = true;

        [Tooltip("Minimum angle between the previous straight line and the current node, if the angle is less than the specified angle, the node is deleted")]
        [SerializeField][Range(0, 10f)] private float maxStripAngle = 3f;

        [Tooltip("Minimum distance between cutout nodes")]
        [SerializeField][Range(0, 100f)] private int minStripDistance = 20;

        [SerializeField] private bool stripIntersectionNodes = true;

        [Tooltip("Minimum angle between the previous straight line and the current node, if the angle is less than the specified angle, the node is deleted")]
        [SerializeField][Range(0, 10f)] private float maxStripAngleIntersection = 1f;

        [Tooltip("Minimum distance between cutout nodes")]
        [SerializeField][Range(0, 100f)] private int minStripDistanceIntersection = 20;

        [SerializeField] private bool stripPedestrianNodes = true;

        [Tooltip("Minimum angle between the previous straight line and the current node, if the angle is less than the specified angle, the node is deleted")]
        [SerializeField][Range(0, 10f)] private float maxStripAnglePed = 4f;

        [Tooltip("Minimum distance between cutout nodes")]
        [SerializeField][Range(0, 100f)] private float minStripDistancePed = 10f;

        private RoadParent roadParent;

#if UNITY_EDITOR
        private void Start()
        {
            ValidateTrafficLight();
        }
#endif

        [Button]
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
            CreateSplines();
            CreateIntersections();
            ConnectLightObjects();

            roadParent.BakePathData();
        }

        protected override RoadSegmentCreator CreateSpline(RoadObject roadObject, List<RoadSceneUtils.NodeSettings> nodeList, RoadSceneUtils.StraightSegmentSettings segmentSettings)
        {
            var sourceTrafficLanes = roadObject.traffic.trafficLanes;
            var creator = RoadSceneUtils.CreateStraightSegment(nodeList, segmentSettings, $"{roadObject.name}_segment");

            if (stripSplineNodes)
                creator.StripNodes(maxStripAngle, minStripDistance, allowCustomSegment: true);

            if (creator.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomSegment)
            {
                foreach (var trafficLane in sourceTrafficLanes)
                {
                    AddPedestrianNodes(creator, trafficLane, stripPedestrianNodes, true);
                }

                creator.GeneratePathInnerSpawnNodes();
                GenerateTrafficLight(roadObject, creator);
            }

            AddCrossroad(creator);

            return creator;
        }

        protected override RoadSegmentCreator CreateIntersection(IntersectionObject intersection, List<RoadSceneUtils.NodeSettings> list, RoadSceneUtils.SegmentSettings segmentSettings)
        {
            var connections = intersection.RoadConnections;
            var sourceTrafficLanes = intersection.traffic.trafficLanes;
            var creator = RoadSceneUtils.CreateIntersection(list, segmentSettings, segmentName: $"{intersection.name}_segment");

            GenerateTrafficLight(intersection, creator);

            if (stripIntersectionNodes)
                creator.StripNodes(maxStripAngleIntersection, minStripDistanceIntersection, allowCustomSegment: true);

            if (!segmentSettings.ConnectCrosswalks)
            {
                for (var i = 0; i < sourceTrafficLanes.Count; i++)
                {
                    var trafficLane = sourceTrafficLanes[i];

                    AddPedestrianNodes(creator, trafficLane);
                }
            }

            for (int i = 0; i < connections.Count; i++)
            {
                var connectedRoad = connections[i];
                var connectedNode = GetClosestNode(connectedRoad, creator.transform.position);

                var currentNode = creator.TryToGetNode(i);

                currentNode.SetMerge(connectedNode);
            }

            AddCrossroad(creator);
            return creator;
        }

        protected override RoadSceneUtils.NodeSettings TryToGetClosestNode(RoadSegmentCreator creator, Vector3 intersectionPos)
        {
            var node = creator.TryToGetClosestNode(intersectionPos);

            return new RoadSceneUtils.NodeSettings(node);
        }

        private void CreateSplines()
        {
            var roads = ObjectUtils.FindObjectsOfType<RoadObject>().Where(a => a.traffic != null).ToArray();

            for (int i = 0; i < roads.Length; i++)
            {
                var road = roads[i];

                CreateSpline(road);
            }

            for (int i = 0; i < roads.Length; i++)
            {
                var connections = roads[i].RoadConnections;

                var creator = GetBinding(roads[i]);

                for (int j = 0; j < connections.Count; j++)
                {
                    var connectedRoad = connections[j];

                    var connectedNode = GetClosestNode(connectedRoad, creator.transform.position);

                    var connectedCreator = GetBinding(connectedRoad);
                    var currentNode = GetClosestNode(roads[i], connectedCreator.transform.position);

                    currentNode.SetMerge(connectedNode);
                }
            }
        }

        private void CreateIntersections()
        {
            var intersections = ObjectUtils.FindObjectsOfType<IntersectionObject>().Where(a => a.traffic != null).ToArray();

            foreach (var intersection in intersections)
            {
                CreateIntersection(intersection);
            }
        }

        private void GenerateTrafficLight(SceneObject sceneObject, RoadSegmentCreator creator)
        {
            var addLights = creator.TrafficNodeCount >= 3;

            var trafficLightCrossroad = creator.trafficLightCrossroad;
            trafficLightCrossroad.HasLights = addLights;

            if (addLights)
            {
                if (lightStateContainer)
                {
                    trafficLightCrossroad.SharedStateContainer = lightStateContainer;
                }
            }

            EditorSaver.SetObjectDirty(trafficLightCrossroad);
        }

        private void ConnectLightObjects()
        {
            foreach (var item in roadBinding)
            {
                ConnectLightObjects(item.Key, item.Value);
            }
        }

        private void ConnectLightObjects(SceneObject sceneObject, RoadSegmentCreator creator)
        {
            var lights = sceneObject.gameObject.GetComponentsInChildren<RC_TrafficLightConverter>();

            foreach (var light in lights)
            {
                var relatedNode = creator.TryToGetClosestNode(light.transform.position);
                relatedNode = relatedNode.MergeNode;

                var relatedCrossroad = relatedNode.TrafficLightCrossroad;

                var trafficLight = light.trafficLight;
                trafficLight.AssignCrossRoad(relatedCrossroad);

                var nodeLight = relatedNode.TrafficLightHandler;

                trafficLight.SetFrameIndex(nodeLight.RelatedLightIndex);

                var pedestrianLight = light.pedestrianLight;
                pedestrianLight.AssignCrossRoad(relatedCrossroad);

                var pedLight = relatedNode.GetCrosswalkNode(0).RelatedTrafficLightHandler;
                pedestrianLight.SetFrameIndex(pedLight.RelatedLightIndex);
            }
        }

        private void AddCrossroad(RoadSegmentCreator creator)
        {
            roadParent.AddCrossRoad(creator.trafficLightCrossroad);
            creator.trafficLightCrossroad.transform.SetParent(roadParent.transform, true);
        }

        private TrafficNode GetClosestNode(RoadObject roadObject, Vector3 intersectionPos)
        {
            RoadSegmentCreator creator = GetBinding(roadObject);

            return creator.TryToGetClosestNode(intersectionPos);
        }

        private void Clear()
        {
            var creators = roadParent.GetComponentsInChildren<RoadSegmentCreator>().ToList();
            creators.DestroyGameObjects();

            roadBinding.Clear();
        }

        private bool AddPedestrianNodes(RoadSegmentCreator creator, TrafficLane trafficLane, bool stripNodes = false, bool allowSpawn = false)
        {
            if (!AddPedestrianNodes(trafficLane))
                return false;

            List<Vector3> points = null;

            if (trafficLane.waypoints.Count > 2)
            {
                points = trafficLane.waypoints.Skip(1).Take(trafficLane.waypoints.Count - 2).Select(a => a.transform.position).ToList();
            }
            else
            {
                points = trafficLane.waypoints.Select(a => a.transform.position).ToList();
            }

            if (stripNodes && points.Count > 0)
            {
                RoadUtils.StripPoints(points, maxStripAnglePed, minStripDistancePed);
            }

            creator.AddCornerNodes(points, allowSpawn);
            return true;
        }
    }
}
