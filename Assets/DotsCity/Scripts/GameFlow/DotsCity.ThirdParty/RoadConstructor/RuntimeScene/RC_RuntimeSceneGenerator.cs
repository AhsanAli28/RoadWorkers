using PampelGames.RoadConstructor;
using Spirit604.CityEditor.Utils;
using Spirit604.DotsCity.RuntimeRoad;
using Spirit604.DotsCity.RuntimeRoad.Custom;
using Spirit604.DotsCity.Simulation.Road.Authoring;
using Spirit604.Extensions;
using Spirit604.Gameplay.Config.Road;
using UnityEngine;

#if RUNTIME_ROAD
using System.Collections.Generic;
#endif

namespace Spirit604.DotsCity.ThirdParty.RoadConstructor
{
    public class RC_RuntimeSceneGenerator :
#if RUNTIME_ROAD
        RC_EditorSceneGeneratorBase<RuntimeSegmentCustom>
#else
        MonoBehaviour
#endif
    {
        [SerializeField] private SharedLightStateContainer lightStateContainer;
        [SerializeField] private PampelGames.RoadConstructor.RoadConstructor roadConstructor;
        [SerializeField] private RuntimeRoadManagerCustom runtimeRoadManagerCustom;

#if RUNTIME_ROAD

        private List<RuntimeSegmentCustom> segments = new List<RuntimeSegmentCustom>();
        private List<Waypoint> waypointCache = new List<Waypoint>();

        private void Awake()
        {
            if (roadConstructor == null)
            {
                Debug.LogError("RC_RuntimeSceneGenerator. <b>RoadConstructor</b> is null. Assign RoadConstructor in the inspector.");
                return;
            }

            if (runtimeRoadManagerCustom == null)
            {
                Debug.LogError("RC_RuntimeSceneGenerator. <b>RuntimeRoadManagerCustom</b> is null. Create & assign RuntimeRoadManagerCustom in the inspector.");
                return;
            }

            roadConstructor.OnRoadsAdded += AddRoads;
            roadConstructor.OnRoadsRemoved += RemoveRoads;

            if (lightStateContainer == null)
            {
                Debug.LogError("RC_RuntimeSceneGenerator. <b>LightStateContainer</b> is null. Assign any light state container.");
            }
        }

#if UNITY_EDITOR
        private void Start()
        {
            ValidateTrafficLight();
        }
#endif

        public void AddRoads(List<RoadObject> roadObjects, List<IntersectionObject> intersections)
        {
            if (roadObjects.Count == 0 && intersections.Count == 0)
                return;

            segments.Clear();

            for (int i = 0; i < intersections.Count; i++)
            {
                if (roadBinding.ContainsKey(intersections[i]))
                {
                    var segment = GetBinding(intersections[i]);
                    segments.Add(segment);
                    RemoveBinding(intersections[i]);
                }
            }

            if (segments.Count > 0)
            {
                runtimeRoadManagerCustom.RemoveSegments(segments);
            }

            segments.Clear();

            for (int i = 0; i < roadObjects.Count; i++)
            {
                CreateSpline(roadObjects[i]);
            }

            for (int i = 0; i < intersections.Count; i++)
            {
                CreateIntersection(intersections[i]);
            }

            runtimeRoadManagerCustom.AddSegments(segments);

            AddLightObjects(roadObjects);
        }

        private void AddLightObjects(List<RoadObject> roadObjects)
        {
            for (int i = 0; i < roadObjects.Count; i++)
            {
                RC_TrafficLightConverter[] lights = null;
                var intersectionConnections = roadObjects[i].IntersectionConnections;

                if (intersectionConnections.Count == 0)
                    continue;

                var intersection1 = intersectionConnections[0];

                if (intersection1.RoadConnections.Count >= 3)
                {
                    if (lights == null)
                        lights = roadObjects[i].GetComponentsInChildren<RC_TrafficLightConverter>();

                    for (int j = 0; j < lights?.Length; j++)
                    {
                        RC_TrafficLightConverter light = lights[j];
                        AddLightObject(roadObjects[i], intersection1, light);
                    }
                }

                if (intersectionConnections.Count <= 1)
                    continue;

                var intersection2 = intersectionConnections[1];

                if (intersection2.RoadConnections.Count >= 3)
                {
                    if (lights == null)
                        lights = roadObjects[i].GetComponentsInChildren<RC_TrafficLightConverter>();

                    for (int j = 0; j < lights?.Length; j++)
                    {
                        RC_TrafficLightConverter light = lights[j];
                        AddLightObject(roadObjects[i], intersection2, light, false);
                    }
                }
            }
        }

        private void AddLightObject(RoadObject roadObject, IntersectionObject intersectionObject, RC_TrafficLightConverter light, bool start = true)
        {
            const float sqDist = 100;

            var index = start ? 0 : 1;

            var pos2 = light.transform.position;

            var intersectionSegment = GetBinding(intersectionObject);
            var node = intersectionSegment.GetClosestNode(pos2);
            var pos1 = node.Position;

            var dist = Vector3.SqrMagnitude(pos1 - pos2);

            if (dist < sqDist)
            {
                var lightIndex = node.LightIndex;
                var pedLightIndex = (node.LightIndex + 1) % 2;

                light.trafficLight.ConnectedId = intersectionSegment.CrossroadID;
                light.trafficLight.SetFrameIndex(lightIndex);
                light.trafficLight.GetComponent<TrafficLightObjectAuthoring>().RegisterFrames();

                light.pedestrianLight.ConnectedId = intersectionSegment.CrossroadID;
                light.pedestrianLight.SetFrameIndex(pedLightIndex);
                light.pedestrianLight.GetComponent<TrafficLightObjectAuthoring>().RegisterFrames();
            }
        }

        public void RemoveRoads(List<RoadObject> roadObjects, List<IntersectionObject> intersections)
        {
            if (roadObjects.Count == 0 && intersections.Count == 0)
                return;

            segments.Clear();

            for (int i = 0; i < roadObjects.Count; i++)
            {
                RoadObject roadObject = roadObjects[i];
                var source = GetBinding(roadObject);

                if (!source)
                    continue;

                segments.Add(source);
                RemoveBinding(roadObject);
            }

            for (int i = 0; i < intersections.Count; i++)
            {
                IntersectionObject intersection = intersections[i];
                var source = GetBinding(intersection);

                if (!source)
                    continue;

                segments.Add(source);
                RemoveBinding(intersection);
            }

            runtimeRoadManagerCustom.RemoveSegments(segments);
        }

        protected override RuntimeSegmentCustom CreateSpline(RoadObject roadObject, List<RoadSceneUtils.NodeSettings> nodeList, RoadSceneUtils.StraightSegmentSettings settings)
        {
            var runtimeSegment = roadObject.gameObject.AddComponent<RuntimeSegmentCustom>();

            runtimeSegment.Position = roadObject.splineContainer.EvaluatePosition(0.5f);
            runtimeSegment.AddNodes(nodeList);

            var intersectionConnections = roadObject.IntersectionConnections;

            for (int i = 0; i < intersectionConnections.Count; i++)
            {
                var connection = intersectionConnections[i];
                var binding = GetBinding(connection);

                if (binding != null)
                {
                    var newCreatedNode = runtimeSegment.GetClosestNode(binding.Position);
                    ConnectSegments(newCreatedNode, binding);
                }
            }

            var roadConnections = roadObject.RoadConnections;

            for (int i = 0; i < roadConnections.Count; i++)
            {
                var connection = roadConnections[i];
                var binding = GetBinding(connection);

                if (binding != null)
                {
                    var newCreatedNode = runtimeSegment.GetClosestNode(binding.Position);
                    ConnectSegments(newCreatedNode, binding);
                }
            }

            for (int i = 0; i < roadObject.traffic.trafficLanes.Count; i++)
            {
                TrafficLane trafficLane = roadObject.traffic.trafficLanes[i];

                if (!AddPedestrianNodes(trafficLane))
                    continue;

                waypointCache.Clear();
                waypointCache.AddRange(trafficLane.waypoints);

                var prev = trafficLane.waypoints[0].prev;

                // Straight road with no edge nodes, so add the edges of the previous/next paths
                if (prev.Count > 0)
                    waypointCache.Insert(0, prev[0]);

                var next = trafficLane.waypoints[trafficLane.waypoints.Count - 1].next;

                if (next.Count > 0)
                    waypointCache.Add(next[0]);

                runtimeSegment.AddPedestrianNodes(waypointCache, trafficLane.crossing, CrosswalkWidth, CrosswalkHeight, CrosswalkShapeType);
            }

            segments.Add(runtimeSegment);

            return runtimeSegment;
        }

        protected override RuntimeSegmentCustom CreateIntersection(IntersectionObject intersection, List<RoadSceneUtils.NodeSettings> nodeList, RoadSceneUtils.SegmentSettings segmentSettings)
        {
            var runtimeSegment = intersection.gameObject.AddComponent<RuntimeSegmentCustom>();

            runtimeSegment.Position = intersection.centerPosition;
            runtimeSegment.AddNodes(nodeList);
            runtimeSegment.AddLightContainer(lightStateContainer);

            var connections = intersection.RoadConnections;

            for (int i = 0; i < connections.Count; i++)
            {
                RoadObject connection = connections[i];
                var binding = GetBinding(connection);
                var currentNode = runtimeSegment.GetNode(i);

                ConnectSegments(currentNode, binding);
            }

            for (int i = 0; i < intersection.traffic.trafficLanes.Count; i++)
            {
                TrafficLane trafficLane = intersection.traffic.trafficLanes[i];

                if (AddPedestrianNodes(trafficLane))
                {
                    runtimeSegment.AddPedestrianNodes(trafficLane.waypoints, trafficLane.crossing);
                }
            }

            segments.Add(runtimeSegment);

            return runtimeSegment;
        }

        private void ConnectSegments(TrafficNodeData currentNode, RuntimeSegmentCustom connectedSegment)
        {
            var closestNode = connectedSegment.GetClosestNode(currentNode.Position);
            closestNode.ConnectedExternalNode = currentNode;
            currentNode.ConnectedExternalNode = closestNode;

#if UNITY_EDITOR
            closestNode.ConnectedExternalSegment = currentNode.Owner;
            currentNode.ConnectedExternalSegment = closestNode.Owner;
#endif
        }

        protected override RoadSceneUtils.NodeSettings TryToGetClosestNode(RuntimeSegmentCustom binding, Vector3 intersectionPos)
        {
            return binding.GetClosestNode(intersectionPos).GetSettings();
        }

#endif

        private void Reset()
        {
            roadConstructor = ObjectUtils.FindObjectOfType<PampelGames.RoadConstructor.RoadConstructor>();
            runtimeRoadManagerCustom = ObjectUtils.FindObjectOfType<RuntimeRoadManagerCustom>();
            EditorSaver.SetObjectDirty(this);
        }
    }
}
