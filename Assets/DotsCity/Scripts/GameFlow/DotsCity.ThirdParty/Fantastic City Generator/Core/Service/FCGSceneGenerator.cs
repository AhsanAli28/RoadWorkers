#if FCG
using FCG;
using Spirit604.Attributes;
using Spirit604.CityEditor.Road;
using Spirit604.CityEditor.Utils;
using Spirit604.DotsCity.RuntimeRoad;
using Spirit604.DotsCity.Simulation.Road.Authoring;
using Spirit604.DotsCity.ThirdParty.RoadGeneration;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using RaycastHit = UnityEngine.RaycastHit;

namespace Spirit604.DotsCity.ThirdParty.FCG
{
    public class FCGSceneGenerator : MonoBehaviour
    {
        public enum Mode { GenerateEditorWays, CreateOrConnectEditorBlocks, GeneratePrefabBlocks, ImportTemplateRoadBlocks }

        [Serializable]
        public class BrokenData
        {
            public GameObject BlockPrefab;
            public string BrokenWay;
        }

        private const string TrafficLightName = "Traffic_Light";
        private const string RoadSegmentsName = "RoadSegments";

        [SerializeField] private Mode mode;
        [SerializeField] private FCGPrefabContainer FCGPrefabContainer;
        [SerializeField] private List<GameObject> fcgPrefabBlocks = new List<GameObject>();
        [SerializeField] private FCGRoadTemplateContainer templateContainer;
        [SerializeField] private List<BrokenData> brokenBlocks = new List<BrokenData>();
        [SerializeField] private List<FCGWaypointsContainer> brokenWays = new List<FCGWaypointsContainer>();

        [Tooltip("Offset between source scene intersection entry point & created intersection traffic node entry")]
        [SerializeField] private float crossingOffset = 5f;

        [Tooltip("Offset between source scene straigh road entry point & created straight road traffic node entry")]
        [SerializeField] private float roadOffset = 5f;

        [SerializeField][Range(1, 4)] private int laneCount = 2;
        [SerializeField][Range(1, 6f)] private float laneWidth = 3.7f;
        [SerializeField] private bool autoConnectSegments = true;
        [SerializeField] private RSGeneratorConfig config;

        [SerializeField] private List<RoadSegmentCreator> segmentCreators = new List<RoadSegmentCreator>();

        private HashSet<Crossing> crossings = new HashSet<Crossing>();
        private RaycastHit[] hits = new RaycastHit[30];

        private Dictionary<FCGWaypointsContainer, List<FCGWaypointsContainer>> nextWays1 = new Dictionary<FCGWaypointsContainer, List<FCGWaypointsContainer>>();
        private Dictionary<FCGWaypointsContainer, List<FCGWaypointsContainer>> nextWays2 = new Dictionary<FCGWaypointsContainer, List<FCGWaypointsContainer>>();
        private HashSet<FCGWaypointsContainer> ignoreWays = new HashSet<FCGWaypointsContainer>();

        private List<string> colliderBindings = new List<string>();
        private RoadParent roadParent;

        public bool EditorMode => mode == Mode.GenerateEditorWays;
        public bool ConnectEditorBlocks => mode == Mode.CreateOrConnectEditorBlocks;
        public bool GeneratePrefabBlocks => mode == Mode.GeneratePrefabBlocks;
        public bool ImportTemplate => mode == Mode.ImportTemplateRoadBlocks;

        public RSGeneratorConfig Config { get => config; set => config = value; }
        public Mode CurrentMode { get => mode; set => mode = value; }

        [Button]
        public void Generate()
        {
            if (!GeneratePrefabBlocks && !ImportTemplate)
            {
                if (!roadParent)
                {
                    roadParent = ObjectUtils.FindObjectOfType<RoadParent>();
                }

                if (!roadParent)
                {
                    UnityEngine.Debug.LogError("RoadParent not found");
                    return;
                }
            }

            Clear();

            if (!config)
            {
                UnityEngine.Debug.LogError("Config should be assigned");
                return;
            }

            switch (mode)
            {
                case Mode.GenerateEditorWays:
                    {
                        FCGWaypointsContainer[] ways = GetWays();
                        GenerateInternal(ways);
                        break;
                    }
                case Mode.CreateOrConnectEditorBlocks:
                    {
                        var roads = ObjectUtils.FindObjectsOfType<RuntimeSegment>();

                        if (roads.Length == 0)
                        {
                            var cityMakerParent = GameObject.Find("City-Maker");

                            if (cityMakerParent == null)
                            {
                                UnityEngine.Debug.LogError("FCG blocks not found, make sure you have generated a city");
                                return;
                            }

                            if (templateContainer == null)
                            {
                                UnityEngine.Debug.LogError("Template container not assigned");
                                return;
                            }

                            for (int i = 0; i < cityMakerParent.transform.childCount; i++)
                            {
                                var blockRoot = cityMakerParent.transform.GetChild(i);

                                var blockName = blockRoot.name.Replace("(Clone)", "");
                                var templatePrefab = FindPrefab(blockName);

                                if (templatePrefab != null)
                                {
#if UNITY_EDITOR
                                    var templatePrefabInstance = UnityEditor.PrefabUtility.InstantiatePrefab(templatePrefab) as GameObject;
                                    UnityEditor.PrefabUtility.UnpackPrefabInstance(templatePrefabInstance, UnityEditor.PrefabUnpackMode.OutermostRoot, UnityEditor.InteractionMode.AutomatedAction);

                                    InitTemplateBlock(blockRoot.gameObject, templatePrefabInstance);
#endif
                                }
                                else
                                {
                                    UnityEngine.Debug.LogError($"Template for <b>{blockName}</b> block not found");
                                }
                            }

                            roads = ObjectUtils.FindObjectsOfType<RuntimeSegment>();
                        }

                        if (roads.Length == 0)
                        {
                            UnityEngine.Debug.LogError($"Blocks not found, make sure that you generated block with <b>RuntimeBlocks</b> mode then switch back to <b>EditorBlocks</b> after generation");
                            return;
                        }

                        bool anyCrossroad = false;

                        foreach (var road in roads)
                        {
                            var crossroads = road.crossroadData;

                            foreach (var crossroadData in crossroads)
                            {
                                var crossroad = crossroadData.Key;

                                if (!crossroad) continue;

                                crossroad.transform.SetParent(roadParent.transform, true);

                                if (crossroad.HasLights)
                                {
                                    GenerateLight(crossroad);
                                }

                                anyCrossroad = true;
                            }
                        }

                        roads.ToList().DestroyGameObjects();

                        if (!anyCrossroad)
                        {
                            Generate();
                            return;
                        }

                        roadParent.AddCrossroads();
                        ConnnectSegments();

                        break;
                    }
                case Mode.GeneratePrefabBlocks:
                    {
                        InitBindings();

                        foreach (var prefabRef in fcgPrefabBlocks)
                        {
#if UNITY_EDITOR
                            PrefabExtension.EditPrefab(prefabRef, prefab =>
                            {
                                FCGWaypointsContainer[] ways2 = GetWays(prefab);
                                GenerateInternal(ways2, prefabRef, prefab);
                            });
#endif
                        }
                        break;
                    }
                case Mode.ImportTemplateRoadBlocks:
                    {
                        if (templateContainer == null)
                        {
                            UnityEngine.Debug.LogError("Template container not assigned");
                            return;
                        }

                        foreach (var block in fcgPrefabBlocks)
                        {
#if UNITY_EDITOR
                            GameObject templateRoadPrefab = FindPrefab(block);

                            if (templateRoadPrefab == null)
                            {
                                brokenBlocks.Add(new BrokenData()
                                {
                                    BlockPrefab = block,
                                    BrokenWay = "Template not found"
                                });

                                continue;
                            }

                            PrefabExtension.EditPrefab(block.gameObject, (prefab) =>
                            {
                                var runtimeSegment = prefab.GetComponentInChildren<RuntimeSegment>();

                                if (runtimeSegment != null)
                                {
                                    DestroyImmediate(runtimeSegment.gameObject);
                                }

                                var templateInstance = UnityEditor.PrefabUtility.InstantiatePrefab(templateRoadPrefab, prefab.scene) as GameObject;

                                InitTemplateBlock(prefab, templateInstance);

                            });
#endif
                        }

                        break;
                    }
            }
        }

        private void InitTemplateBlock(GameObject root, GameObject templateInstance)
        {
            templateInstance.transform.SetParent(root.transform);
            templateInstance.transform.SetAsFirstSibling();
            templateInstance.transform.localPosition = Vector3.zero;
            templateInstance.transform.localRotation = Quaternion.identity;

            var creators = templateInstance.GetComponentsInChildren<RoadSegmentCreator>();

            foreach (var creator in creators)
            {
                if (creator.TrafficNodeCount >= 3)
                {
                    GenerateLight(creator);
                }
            }
        }

        private GameObject FindPrefab(GameObject block)
        {
            return templateContainer.Prefabs.Where(a => FindBlock(a, block.name)).FirstOrDefault();
        }

        private GameObject FindPrefab(string sceneBlockName)
        {
            return templateContainer.Prefabs.Where(a => FindBlock(a, sceneBlockName)).FirstOrDefault();
        }

        private static bool FindBlock(GameObject prefab, string sceneBlockName)
        {
            return prefab.name.Equals(sceneBlockName, StringComparison.OrdinalIgnoreCase);
        }

        private void InitBindings()
        {
            colliderBindings.Clear();

            if (!FCGPrefabContainer) return;

            foreach (var prefab in FCGPrefabContainer.LocalBlockPrefabs)
            {
                if (!prefab) continue;

                if (!colliderBindings.Contains(prefab.BindingName))
                {
                    colliderBindings.Add(prefab.BindingName);
                }
                else
                {
                    UnityEngine.Debug.Log($"PrefabContainer has duplicate '{prefab.BindingName}' binding");
                }
            }
        }

        private void GenerateInternal(FCGWaypointsContainer[] ways, GameObject prefab = null, GameObject parent = null)
        {
            if (!EditorMode)
            {
                crossings.Clear();
                segmentCreators.Clear();
                nextWays1.Clear();
                nextWays2.Clear();
            }

            var prefabParent = parent;
            var currentRoadParent = parent;

            if (parent != null)
            {
                var child = parent.transform.GetChild(0);

                if (child.name != RoadSegmentsName)
                {
                    Clear(parent);
                    var newParent = new GameObject(RoadSegmentsName);

                    newParent.transform.SetParent(parent.transform);
                    newParent.transform.SetAsFirstSibling();
                    newParent.transform.localPosition = Vector3.zero;
                    newParent.transform.localRotation = Quaternion.identity;

                    currentRoadParent = newParent;
                }
                else
                {
                    currentRoadParent = child.gameObject;
                    Clear(currentRoadParent);
                }
            }

            TryToSpawnPrefabs(ways, prefabParent, currentRoadParent);

            foreach (var currentWay in ways)
            {
                Init(currentWay);

                var currentPrevWays = currentWay.nextWay0;
                var currentNextWays = currentWay.nextWay1;

                for (int i = 0; i < currentNextWays?.Length; i++)
                {
                    FCGWaypointsContainer nextWay = currentNextWays[i];
                    Init(nextWay);

                    nextWays2[currentWay].TryToAdd(nextWay);

                    var node = currentWay.waypoints[currentWay.waypoints.Count - 1];

                    var dist1 = Vector3.SqrMagnitude(node.transform.position - nextWay.waypoints[0].transform.position);
                    var dist2 = Vector3.SqrMagnitude(node.transform.position - nextWay.waypoints[nextWay.waypoints.Count - 1].transform.position);

                    var ByEnd = dist2 < dist1;

                    if (!ByEnd)
                    {
                        nextWays1[nextWay].TryToAdd(currentWay);
                    }
                    else
                    {
                        nextWays2[nextWay].TryToAdd(currentWay);
                    }
                }

                for (int i = 0; i < currentPrevWays?.Length; i++)
                {
                    FCGWaypointsContainer prevWay = currentPrevWays[i];
                    Init(prevWay);

                    var node = currentWay.waypoints[0];

                    var dist1 = Vector3.SqrMagnitude(node.transform.position - prevWay.waypoints[0].transform.position);
                    var dist2 = Vector3.SqrMagnitude(node.transform.position - prevWay.waypoints[prevWay.waypoints.Count - 1].transform.position);

                    var ByEnd = dist2 < dist1;

                    if (ByEnd)
                    {
                        nextWays2[prevWay].TryToAdd(currentWay);
                    }
                    else
                    {
                        nextWays1[prevWay].TryToAdd(currentWay);
                    }

                    nextWays1[currentWay].TryToAdd(prevWay);
                }
            }

            GenerateStraight(ways, prefab, currentRoadParent);

            GenerateCrossings(currentRoadParent);

            if (EditorMode)
            {
                ConnnectSegments();
            }
            else
            {
#if UNITY_EDITOR
                TrafficNode.AutoConnectionSettings connectionSettings = GetConnectionSettings();

                var segment = currentRoadParent.GetComponent<RuntimeSegment>();

                if (segment)
                {
                    DestroyImmediate(segment);
                }

                currentRoadParent.AddComponent<RuntimeSegment>();

                foreach (var item in currentRoadParent.GetComponentsInChildren<RoadSegment>())
                {
                    item.ConnectNodes(connectionSettings);
                }

                currentRoadParent.GetComponent<RuntimeSegment>().Bake();
#endif
            }
        }

        private void ConnnectSegments()
        {
            TrafficNode.AutoConnectionSettings connectionSettings = GetConnectionSettings();

            if (autoConnectSegments)
            {
                roadParent.ForceConnectSegments(connectionSettings);
                roadParent.BakePathData();
            }
        }

        private void TryToSpawnPrefabs(FCGWaypointsContainer[] ways, GameObject prefabParent, GameObject roadParent)
        {
            ignoreWays.Clear();

            if (colliderBindings.Count == 0) return;

            IEnumerable<Collider> colliders = null;

            Func<Collider, bool> predicate = a => colliderBindings.Contains(a.name);

            if (prefabParent)
            {
                colliders = prefabParent.GetComponentsInChildren<Collider>().Where(predicate);
            }
            else
            {
                colliders = ObjectUtils.FindObjectsOfType<Collider>().Where(predicate);
            }

            if (colliders.Count() == 0) return;

            foreach (var way in ways)
            {
                foreach (var collider in colliders)
                {
                    var bounds = collider.bounds;

                    bounds.size = new Vector3(bounds.size.x, 2, bounds.size.z);

                    if (bounds.Contains(way.waypoints[0].transform.position) || bounds.Contains(way.waypoints[way.waypoints.Count - 1].transform.position))
                    {
                        if (!ignoreWays.Contains(way))
                            ignoreWays.Add(way);
                    }
                }
            }

#if UNITY_EDITOR
            foreach (var collider in colliders)
            {
                var prefab = FCGPrefabContainer.GetPrefab(collider.name);

                var newChunk = (UnityEditor.PrefabUtility.InstantiatePrefab(prefab.gameObject) as GameObject).GetComponent<FCGPrefab>();

                newChunk.transform.SetPositionAndRotation(collider.transform.position, collider.transform.rotation);
                SetRoadParent(newChunk.gameObject, roadParent);

                var segments = newChunk.InnerSegments;

                foreach (var creator in segments)
                {
                    segmentCreators.Add(creator);
                }
            }
#endif
        }


        private void Init(FCGWaypointsContainer way)
        {
            if (!nextWays1.ContainsKey(way))
            {
                nextWays1.Add(way, new List<FCGWaypointsContainer>());
            }

            if (!nextWays2.ContainsKey(way))
            {
                nextWays2.Add(way, new List<FCGWaypointsContainer>());
            }
        }

        private TrafficNode.AutoConnectionSettings GetConnectionSettings()
        {
            return new TrafficNode.AutoConnectionSettings(40f, false, 0)
            {
                MaxConnectionLength = 5f + (roadOffset + crossingOffset) * 2,
                ConnectCrosswalks = true,
                ExternalSubNodes = true,
            };
        }

        private void GenerateStraight(FCGWaypointsContainer[] ways, GameObject prefab, GameObject parent)
        {
            foreach (var way in ways)
            {
                if (ignoreWays.Contains(way)) continue;

                if (way.nextWay0?.Length > 1)
                {
                    GenerateCrossing(way, way.nextWay0);
                }

                if (way.nextWay1?.Length > 1)
                {
                    GenerateCrossing(way, way.nextWay1, true);
                }
                else
                {
                    if (way.nextWay1?.Length == 1)
                    {
                        var currentLastPoint = way.waypoints[way.waypoints.Count - 1].position;
                        var nextWay = way.nextWay1[0];

                        var nextWaypoints = way.nextWay1[0].waypoints;
                        var nextPoint = nextWaypoints[0].position;
                        var nextPoint2 = nextWaypoints[nextWaypoints.Count - 1].position;

                        const float maxDistanceSq = 100f;

                        if ((Vector3.SqrMagnitude(currentLastPoint - nextPoint) > maxDistanceSq && Vector3.SqrMagnitude(currentLastPoint - nextPoint2) > maxDistanceSq) && nextWays2[nextWay].Count == 1)
                        {
                            if (!EditorMode)
                            {
                                var message = $"{way.name} - {nextWay.name} possible wrong connection";

                                if (AddErrorReport(parent, message, way, nextWay))
                                {
                                    brokenBlocks.Add(new BrokenData()
                                    {
                                        BlockPrefab = prefab,
                                        BrokenWay = message
                                    });
                                }
                            }
                            else
                            {
                                brokenWays.Add(way);
                                brokenWays.Add(nextWay);
                            }
                        }
                    }
                }

                var points = GetPoints(way);

                if (points.Count < 2)
                {
                    if (!EditorMode)
                    {
                        var message = $"{way.name} has {points.Count} points";

                        if (AddErrorReport(parent, message, way))
                        {
                            brokenBlocks.Add(new BrokenData()
                            {
                                BlockPrefab = prefab,
                                BrokenWay = $"{way.name} has {points.Count} points"
                            });
                        }
                    }
                    else
                    {
                        brokenWays.Add(way);
                    }

                    continue;
                }

                var settings = RoadSceneUtils.StraightSegmentSettings.GetDefault();

                if (nextWays1[way].Count > 0)
                    settings.StartOffset = roadOffset + crossingOffset;

                if (nextWays2[way].Count > 0)
                    settings.EndOffset = roadOffset + crossingOffset;

                settings.LaneCount = laneCount;
                settings.LaneWidth = GetLaneWidth(way);

                settings.IsOneWay = way.oneway;

                if (way.oneway)
                {
                    settings.LaneCount *= 2;
                }

                var straight = RoadSceneUtils.CreateStraightSegment(points, settings);

                config.ApplyPostCreationSplineSettings(straight);
                config.ApplySettings(straight, RSGenType.SplineRoad, way.name);

                straight.gameObject.name = $"{way.name}_segment";
                AddSegment(straight, parent);
            }
        }

        private bool AddErrorReport(GameObject parent, string message, FCGWaypointsContainer way1, FCGWaypointsContainer way2 = null)
        {
#if UNITY_EDITOR
            var report = parent.GetOrCreateComponent<FCGErrorReport>();

            return report.TryToAddReport(parent, message, way1, way2);
#else
            return false;
#endif
        }

        private void GenerateCrossings(GameObject parent)
        {
            int index = 1;

            foreach (var crossing in crossings)
            {
                var connections = crossing.Connections;

                var nodeList = new List<RoadSceneUtils.NodeSettings>();

                foreach (var connection in connections)
                {
                    var nodeSettings = GetNodeSettings(connection);
                    var local = nodeSettings.Settings;

                    var tr = GetTransform(connection.Road, connection.IsEnd);
                    var nodePosition = tr.transform.position;
                    var points = GetPoints(connection.Road);

                    local.IsOneWay = connection.Road.oneway;

                    if (connection.Road.oneway)
                    {
                        local.LaneCount *= 2;
                        local.IsEndOfOneWay = !connection.IsEnd;
                    }

                    var nodeRotation = RoadSceneUtils.GetNodeRotation(points, connection.IsEnd, true);

                    nodeSettings.Position = nodePosition;
                    nodeSettings.Rotation = nodeRotation;

                    nodeList.Add(nodeSettings);
                }

                var intersection = RoadSceneUtils.CreateIntersection(nodeList);

                if (!EditorMode)
                {
#if UNITY_EDITOR
                    UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene(intersection.gameObject, parent.gameObject.scene);
#endif
                }

                config.ApplySettings(intersection, RSGenType.Crossing);

                UpdateSettings(intersection);
                GenerateLight(intersection);

                intersection.gameObject.name = $"Crossing_{index}_segment";
                AddSegment(intersection, parent);
                index++;
            }
        }

        private FCGWaypointsContainer[] GetWays(GameObject block = null)
        {
            switch (mode)
            {
                case Mode.GenerateEditorWays:
                    return ObjectUtils.FindObjectsOfType<FCGWaypointsContainer>();
                case Mode.GeneratePrefabBlocks:
                    return block.GetComponentsInChildren<FCGWaypointsContainer>();
            }

            return null;
        }

        private void UpdateSettings(RoadSegmentCreator intersection)
        {
            intersection.connectCrosswalks = true;

#if UNITY_EDITOR
            intersection.OnCrosswalkSettingsChanged();
#endif
        }

        private RoadSceneUtils.NodeSettings GetNodeSettings(ConnectionData connection)
        {
            var local = RoadSceneUtils.NodeSettings.GetDefault();

            local.Settings = new RoadSceneUtils.BaseSettings()
            {
                Offset = crossingOffset,
                LaneCount = laneCount,
                LaneWidth = GetLaneWidth(connection.Road)
            };

            return local;
        }

        private void AddSegment(RoadSegmentCreator segment, GameObject parent = null)
        {
            segmentCreators.Add(segment);

            SetRoadParent(segment.gameObject, parent);
        }

        private void SetRoadParent(GameObject segment, GameObject parent)
        {
            if (parent == null)
            {
                segment.transform.SetParent(roadParent.transform);
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene(segment.gameObject, parent.scene);
#endif

                segment.transform.SetParent(parent.transform);
            }
        }

        private List<Vector3> GetPoints(FCGWaypointsContainer way)
        {
            return way.waypoints.Select(a => a.transform.position).ToList();
        }

        private void GenerateCrossing(FCGWaypointsContainer way, FCGWaypointsContainer[] nextWays, bool isEnd = false)
        {
            var crossing = new Crossing();

            crossing.Add(way, isEnd);

            var source = GetTransform(way, isEnd);

            foreach (var connectedWay in nextWays)
            {
                var currentIsEnd = IsEndRoad(source, connectedWay);
                crossing.Add(connectedWay, currentIsEnd);
            }

            if (!crossings.Contains(crossing))
            {
                crossings.Add(crossing);
            }
        }

        private void GenerateLight(RoadSegmentCreator roadSegmentCreator)
        {
            GenerateLight(roadSegmentCreator.trafficLightCrossroad);
        }

        private void GenerateLight(TrafficLightCrossroad crossroad)
        {
            var physicsScene = PhysicsSceneExtensions.GetPhysicsScene(crossroad.gameObject.scene);
            var count = physicsScene.SphereCast(crossroad.transform.position, 30f, Vector3.up, hits, 30f, ~0, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                RaycastHit collider = hits[i];
                if (collider.transform.name == TrafficLightName)
                {
                    var lightParent = collider.transform.GetComponentInParent<TFShiftHand2>();

#if UNITY_EDITOR
                    var converter = lightParent.gameObject.GetOrCreateComponent<FCGTrafficLightConverter>();

                    converter.CreateInternal(false);

                    var lights = lightParent.gameObject.GetComponentsInChildren<TrafficLightObject>();

                    foreach (var lightObject in lights)
                    {
                        lightObject.AssignCrossRoad(crossroad);
                    }
#endif
                }
            }
        }

        private bool IsEndRoad(Transform sourceTransform, FCGWaypointsContainer way)
        {
            var dist1 = Vector3.Distance(sourceTransform.transform.position, way.waypoints[0].position);
            var dist2 = Vector3.Distance(sourceTransform.transform.position, way.waypoints[way.waypoints.Count - 1].position);

            return dist1 > dist2;
        }

        private float GetLaneWidth(FCGWaypointsContainer way)
        {
            return laneWidth;
        }

        private Transform GetTransform(FCGWaypointsContainer way, bool isEnd)
        {
            return !isEnd ? way.waypoints[0] : way.waypoints[way.waypoints.Count - 1];
        }

        private (TrafficNode, TrafficNode) GetConnectedNodes(RoadSegmentCreator segment1, RoadSegmentCreator segment2)
        {
            float distance = float.MaxValue;

            TrafficNode node12 = null;
            TrafficNode node22 = null;

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    var node1 = segment1.TryToGetNode(i);
                    var node2 = segment2.TryToGetNode(j);

                    var currentDistance = Vector3.SqrMagnitude(node1.transform.position - node2.transform.position);

                    if (distance > currentDistance)
                    {
                        distance = currentDistance;
                        node12 = node1;
                        node22 = node2;
                    }
                }
            }

            return (node12, node22);
        }

        public void UserClear()
        {
            Clear();

            if (!EditorMode)
            {
                foreach (var block in fcgPrefabBlocks)
                {
                    if (block == null) continue;

#if UNITY_EDITOR
                    PrefabExtension.EditPrefab(block, (prefab =>
                    {
                        Clear(prefab);
                    }));
#endif
                }
            }
        }

        private void Clear()
        {
#if UNITY_EDITOR
            var lights = ObjectUtils.FindObjectsOfType<FCGTrafficLightConverter>().Where(a => !UnityEditor.PrefabUtility.IsPartOfPrefabInstance(a));

            foreach (var item in lights)
            {
                item.DestroyInternal(false);
                DestroyImmediate(item);
            }
#endif

            if (roadParent)
            {
                segmentCreators = roadParent.GetComponentsInChildren<RoadSegmentCreator>().ToList();
            }

            var root = ObjectUtils.FindObjectOfType<EntityRoadRoot>();

            if (ConnectEditorBlocks || EditorMode)
            {
                if (root && root.PedestrianNodesRoot)
                {
                    var pedestrianNodes = root.PedestrianNodesRoot.GetComponentsInChildren<PedestrianNode>().ToList();
                    pedestrianNodes.DestroyGameObjects();
                }
            }

            segmentCreators.DestroyGameObjects();

            crossings.Clear();
            brokenBlocks.Clear();
            nextWays1.Clear();
            nextWays2.Clear();
        }

        private void Clear(GameObject block)
        {
            var runtimeSegment = block.transform.GetComponentInChildren<RuntimeSegment>();

            if (runtimeSegment)
            {
                DestroyImmediate(runtimeSegment);
            }

            var segmentCreators = block.transform.GetComponentsInChildren<RoadSegmentCreator>().ToList();
            segmentCreators.DestroyGameObjects();

            var report = block.GetComponent<FCGErrorReport>();

            if (report != null)
            {
                DestroyImmediate(report);
            }

            var fCGPrefabs = block.transform.GetComponentsInChildren<FCGPrefab>().ToList();
            fCGPrefabs.DestroyGameObjects();
        }
    }
}
#endif