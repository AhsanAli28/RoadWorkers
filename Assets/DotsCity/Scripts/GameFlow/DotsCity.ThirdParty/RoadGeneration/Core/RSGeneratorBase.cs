using Spirit604.CityEditor.Road;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Spirit604.DotsCity.ThirdParty.RoadGeneration
{
    [ExecuteInEditMode]
    public abstract class RSGeneratorBase : MonoBehaviour
    {
        #region Helper types

        [Serializable]
        private class CrossingConnectionData
        {
            public ISplineRoad ConnectedSplineRoad;
            public TrafficNode ConnectedNode;
            public bool ByEnd;
        }

        [Serializable]
        private class GeneratedData
        {
            public RSSceneBinding SceneSegment;
            public int Hash;
        }

        [Serializable]
        private class ErrorMessage
        {
            public RSSceneBinding SceneSegment;
            public string Message;
        }

        #endregion

        #region Constans

        private const float MinRecalcNodeDistance = 0.01f;

        #endregion

        #region Serialized fields

#pragma warning disable 0414

        [SerializeField] private RoadParent roadParent;
        [SerializeField] private RSGeneratorConfig config;
        [SerializeField] private List<RSPrefabBinding> customPrefabs = new List<RSPrefabBinding>();

        [Tooltip("Min length of spline to create road segment")]
        [SerializeField][Range(1f, 40f)] private float minRoadLength = 12f;

        [Tooltip("Offset between the custom straight road and the crossroads")]
        [SerializeField][Range(1f, 20f)] private float segmentOffset = 5f;

        [SerializeField] private bool autoLaneCount = true;

        [SerializeField][Range(1f, 20f)] private float laneWidth = 4f;

        [Tooltip("Threshold rounding of lane count depending on road width")]
        [SerializeField][Range(0f, 4f)] private float laneThreshold = 1f;

        [Tooltip("Min lane width of spline to create road segment")]
        [SerializeField][Range(0f, 4f)] private float minLaneWidth = 0.01f;

        [Tooltip("Road segment is recalculated if road spline is changed")]
        [SerializeField] private bool autoRecalculateRoads = true;

        [Tooltip("Roads that contain this text will be ignored")]
        [SerializeField] private List<string> ignoreRoadNames = new List<string>();

        [Tooltip("Segments locked for generator rebuild")]
        [SerializeField] private List<RSSceneBinding> lockedSegments = new List<RSSceneBinding>();

        [Tooltip("Segments that can't find a hash for the source scene road")]
        [SerializeField] private List<ErrorMessage> failedSegments = new List<ErrorMessage>();

        [Tooltip("Segments that have collision for the hash. Make sure your road object wrapper has a unique position for it")]
        [SerializeField] private List<RSSceneBinding> duplicateHashSegments = new List<RSSceneBinding>();

        [SerializeField] private List<GameObject> notFoundObjects = new List<GameObject>();
        [SerializeField] private List<GameObject> ignoredObjects = new List<GameObject>();
        [SerializeField] private List<TrafficNode> deadEndNodes = new List<TrafficNode>();
        [SerializeField] private List<GeneratedData> generatedSegments = new List<GeneratedData>();

#pragma warning restore 0414

        #endregion

        #region Variables

        private List<RoadSegmentCreator> splineRoads = new List<RoadSegmentCreator>();
        private Dictionary<ICrossingRoad, List<CrossingConnectionData>> crossings = new Dictionary<ICrossingRoad, List<CrossingConnectionData>>();
        private Dictionary<GameObject, RoadSegmentCreator> prefabBinding = new Dictionary<GameObject, RoadSegmentCreator>();
        private Dictionary<int, RSSceneBinding> sceneRSObjects = new Dictionary<int, RSSceneBinding>();
        private Dictionary<int, IRoadObject> sceneSourceRoadObjects = new Dictionary<int, IRoadObject>();
        private Dictionary<GameObject, RSSceneBinding> objectToRSBinding = new Dictionary<GameObject, RSSceneBinding>();

        private Scene sourceRoadScene;
        private bool moveToSubscene;
        private Transform sourceRoadParent;
        private RSSceneBinding lastUpdatedBinding;

        #endregion

        #region Private properties

        private RoadSegmentCreator RoadPrefab => config.RoadSegmentCreatorPrefab;
        private bool AutoRecalculateRoads => autoRecalculateRoads;

        #endregion

        #region Abstract & virtual properties

        public abstract Type RoadType { get; }
        public virtual Type CustomPrefabType { get; }
        public virtual Type CustomPrefabIgnoreType { get; }

        #endregion

#if UNITY_EDITOR

        #region Unity lifecycle

        protected virtual void OnEnable()
        {
            UnityEditor.Undo.undoRedoPerformed -= Undo_undoRedoPerformed;
            UnityEditor.Undo.undoRedoPerformed += Undo_undoRedoPerformed;
        }

        protected virtual void OnDisable()
        {
            UnityEditor.Undo.undoRedoPerformed -= Undo_undoRedoPerformed;
        }

        #endregion

        #region Public methods

        public void Generate()
        {
            if (!roadParent)
            {
                roadParent = ObjectUtils.FindObjectOfType<RoadParent>();
            }

            if (!roadParent)
            {
                Debug.Log($"Roadparent not found. Make sure you have created a city base or subscene that is open & contains a road parent.");
                return;
            }

            if (!config)
            {
                Debug.Log($"Config not assigned.");
                return;
            }

            if (!config.RoadSegmentCreatorPrefab)
            {
                Debug.Log($"Base prefab not assigned in the config.");
                return;
            }

            sourceRoadScene = default;
            moveToSubscene = false;

            if (roadParent.gameObject.scene.isSubScene)
            {
                moveToSubscene = true;
                sourceRoadScene = roadParent.gameObject.scene;
                sourceRoadParent = roadParent.transform.parent;
                UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene(roadParent.gameObject, gameObject.scene);
            }

            ClearScene();

            InitCustomPrefabs();

            PreProcessCreation();

            IterateAllObjects();

            PostProccessCreation();
        }

        public void ClearScene()
        {
            Clear();

            int index = 0;

            FindSceneObjects();

            while (index < generatedSegments.Count)
            {
                bool remove = true;

                RSSceneBinding sceneObj = null;

                if (sceneRSObjects.ContainsKey(generatedSegments[index].Hash))
                {
                    sceneObj = sceneRSObjects[generatedSegments[index].Hash];
                }

                if (sceneObj)
                {
                    if (generatedSegments[index].SceneSegment == null)
                    {
                        generatedSegments[index].SceneSegment = sceneObj;
                    }

                    if (sceneObj.LockAutoRecreation)
                    {
                        if (sceneObj.RestoreSceneObject(sceneSourceRoadObjects))
                        {
                            lockedSegments.Add(sceneObj);
                            objectToRSBinding.Add(sceneObj.SelectedSceneObject, sceneObj);
                        }
                        else
                        {
                            failedSegments.Add(new ErrorMessage()
                            {
                                SceneSegment = sceneObj,
                                Message = "RestoreSceneObject failed. Source scene object not found. Hash not found"
                            });
                        }

                        remove = false;
                    }
                    else
                    {
                        DestroyImmediate(sceneObj.gameObject);
                    }
                }

                if (remove)
                {
                    generatedSegments.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }
        }

        public void UpdateSegment(RSSceneBinding rsSceneBinding)
        {
            if (rsSceneBinding.SelectedSceneObject == null)
            {
                if (sceneSourceRoadObjects.Count == 0)
                {
                    FindSceneObjects();
                }

                if (!rsSceneBinding.RestoreSceneObject(sceneSourceRoadObjects, true))
                {
                    Debug.Log($"RsSceneBinding {rsSceneBinding.RoadSegmentCreator.name} scene object restore failed.");
                    return;
                }
            }

            var component = GetSplineComponentByObject(rsSceneBinding.SelectedSceneObject);
            UpdateSegment(component, rsSceneBinding);
        }

        public void UpdateSegment(Component roadComponent, GameObject sceneObject)
        {
            if (!AutoRecalculateRoads)
                return;

            foreach (var generatedSegment in generatedSegments)
            {
                if (generatedSegment.SceneSegment.SelectedSceneObject == sceneObject)
                {
                    UpdateSegment(roadComponent, generatedSegment.SceneSegment);
                    break;
                }
            }
        }

        #endregion

        #region Protected abstract methods

        protected abstract void AddSceneRoadObjects();

        protected abstract ISplineRoad GetSplineRoad(Component roadComponent);

        protected abstract ICrossingRoad GetCrossingRoad(Component crossingComponent);

        protected abstract ICustomPrefabRoad GetCustomRoadPrefab(Component customPrefabComponent);

        protected abstract Component GetSplineComponentByObject(GameObject sceneObject);

        #endregion

        #region Protected virtual methods

        protected virtual void IterateAllObjects()
        {
            IterateSplineRoads();

            IterateCrossingPrefabs();

            IterateCrossings();
        }

        protected virtual void PreProcessCreation() { }

        protected virtual void IterateSplineRoads()
        {
            var roads = FindObjects(RoadType);

            foreach (var roadComponent in roads)
            {
                var road = GetSplineRoad(roadComponent);
                var rsSplineRoad = Generate(road);

                if (rsSplineRoad != null)
                {
                    splineRoads.Add(rsSplineRoad);
                    AddCrossing(road, rsSplineRoad);
                }
            }
        }

        protected virtual void IterateCrossingPrefabs()
        {
            if (CustomPrefabType == null)
                return;

            var crossingPrefabs = FindObjects(CustomPrefabType, CustomPrefabIgnoreType);

            foreach (var crossingPrefab in crossingPrefabs)
            {
                var crossingPrefabWrapper = GetCustomRoadPrefab(crossingPrefab);
                GenerateCustomPrefab(crossingPrefabWrapper);
            }
        }

        protected virtual void IterateCrossings()
        {
            foreach (var item in crossings)
            {
                GenerateCrossing(item);
            }
        }

        protected virtual void IterateDeadEnds()
        {
            for (int i = 0; i < splineRoads.Count; i++)
            {
                RoadSegmentCreator splineRoad = splineRoads[i];

                splineRoad.IterateAllNodes(node =>
                {
                    if (node.HasLeftLanes && node.ExternalLanes.Count == 0)
                    {
                        node.TrafficNodeType = TrafficNodeType.DestroyVehicle;
                        EditorSaver.SetObjectDirty(node);
                        deadEndNodes.Add(node);
                    }

                    if (splineRoad.addPedestrianNodes)
                    {
                        var connnectedNode = node.CastConnectedNode(false, true);

                        if (connnectedNode != null && connnectedNode.TrafficNodeCrosswalk.Enabled)
                        {
                            node.TrafficNodeCrosswalk.Connect(connnectedNode.TrafficNodeCrosswalk);
                        }
                    }
                });
            }
        }

        protected virtual void PostProccessCreation()
        {
            roadParent.ClearUnattachedPaths(false, false);
            roadParent.ResetSegments();
            roadParent.ConnectSegments(0, true);

            IterateDeadEnds();

            if (moveToSubscene)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene(roadParent.gameObject, sourceRoadScene);
                roadParent.transform.SetParent(sourceRoadParent);
            }

            EditorSaver.SetObjectDirty(this);
        }

        protected virtual int GetObjectHash(IRoadObject roadObject)
        {
            return RSGeneratorExtension.GetHash(roadObject.Position);
        }

        #endregion

        #region Protected methods

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TSourceMono">Source object on the scene. </typeparam>
        /// <typeparam name="TInstance">The class that wraps this scene object. </typeparam>
        protected void RegisterSceneRoadObject<TSourceMono, TInstance>()
            where TSourceMono : MonoBehaviour
            where TInstance : class
        {
            var objs = FindObjects<TSourceMono>();

            foreach (var obj in objs)
            {
                IRoadObject cast = (IRoadObject)CreateInstance<TSourceMono, TInstance>(obj);

                var hash = GetObjectHash(cast);

                if (!sceneSourceRoadObjects.ContainsKey(hash))
                {
                    sceneSourceRoadObjects.Add(hash, cast);
                }
            }
        }

        protected object CreateInstance<TSourceMono, TInstance>(TSourceMono arg)
            where TSourceMono : MonoBehaviour
            where TInstance : class
        {
            return Activator.CreateInstance(typeof(TInstance), new object[] { arg });
        }

        #endregion

        #region Private methods

        private void InitCustomPrefabs()
        {
            foreach (var customPrefab in customPrefabs)
            {
                if (!customPrefab.BindSourcePrefab)
                    continue;

                prefabBinding.Add(customPrefab.BindSourcePrefab, customPrefab.RoadSegmentCreator);
            }
        }

        private Component[] FindObjects(Type type)
        {
            return ObjectUtils.FindObjectsOfType(type);
        }

        private Component[] FindObjects(Type type, Type ignoreType)
        {
            if (ignoreType == null)
                return FindObjects(type);

            return ObjectUtils.FindObjectsOfType(type).Where(a => a.GetComponent(ignoreType) == null).ToArray();
        }

        private T[] FindObjects<T>() where T : MonoBehaviour
        {
            return ObjectUtils.FindObjectsOfType<T>();
        }

        private T[] FindObjects<T, ExcludeT>() where T : MonoBehaviour where ExcludeT : MonoBehaviour
        {
            return ObjectUtils.FindObjectsOfType<T>().Where(a => a.GetComponent<ExcludeT>() == null).ToArray();
        }

        private void AddCrossing(ISplineRoad road, RoadSegmentCreator connectedRoad)
        {
            if (road.StartConnectionObject != null)
            {
                var crossing = GetCrossingRoad(road.StartConnectionObject);

                if (!crossings.ContainsKey(crossing))
                {
                    crossings.Add(crossing, new List<CrossingConnectionData>());
                }

                crossings[crossing].Add(new CrossingConnectionData()
                {
                    ConnectedSplineRoad = road,
                    ConnectedNode = connectedRoad.TryToGetNode(0),
                    ByEnd = false,
                });
            }

            if (road.EndConnectionObject != null)
            {
                var crossing = GetCrossingRoad(road.EndConnectionObject);

                if (!crossings.ContainsKey(crossing))
                {
                    crossings.Add(crossing, new List<CrossingConnectionData>());
                }

                crossings[crossing].Add(new CrossingConnectionData()
                {
                    ConnectedSplineRoad = road,
                    ConnectedNode = connectedRoad.TryToGetNode(1),
                    ByEnd = true
                });
            }
        }

        private void GenerateCrossing(KeyValuePair<ICrossingRoad, List<CrossingConnectionData>> crossingData)
        {
            var crossing = crossingData.Key;

            if (IsCreated(crossing))
                return;

            var position = crossing.Position;

            RoadSegmentCreator creator = CreateSegment(position, Quaternion.identity, crossing.SceneObject, $"{crossing.Name}_segment", crossing, RSGenType.Crossing);

            creator.laneCount = 1;
            creator.Create();
            creator.Create(RoadSegmentCreator.RoadSegmentType.CustomSegment);

            ClearInternalPaths(creator);

            var roads = crossingData.Value;

            var nodeToCreate = roads.Count - creator.TrafficNodeCount;

            if (nodeToCreate > 0)
            {
                for (int i = 0; i < nodeToCreate; i++)
                {
                    creator.AddTrafficNode();
                }
            }

            for (int i = 0; i < creator.TrafficNodeCount; i++)
            {
                if (roads.Count <= i)
                {
                    failedSegments.Add(new ErrorMessage()
                    {
                        SceneSegment = creator.GetComponent<RSSceneBinding>(),
                        Message = "one connection for crossing, seems to ignore some connected straight road"
                    });

                    continue;
                }

                var road = roads[i].ConnectedSplineRoad;
                var connectedNode = roads[i].ConnectedNode;
                var node = creator.TryToGetNode(i);

                var byEnd = roads[i].ByEnd;
                SetCrossNodeTransform(node, road.Points, byEnd);

                node.LaneCount = connectedNode.LaneCount;
                node.LaneWidth = connectedNode.LaneWidth;
                node.IsOneWay = connectedNode.IsOneWay;
                node.IsEndOfOneWay = !connectedNode.IsEndOfOneWay;
                node.Resize();
            }

            creator.SnapNodes(false, true);
            creator.CreateAutoPaths(false);
            creator.SnapNodes(false, false);

            if (creator.addPedestrianNodes)
            {
                creator.connectCrosswalks = true;
                creator.OnCrosswalkSettingsChanged();
            }
        }

        private void SetCrossNodeTransform(TrafficNode node, List<Vector3> points, bool byEnd)
        {
            var index = byEnd ? points.Count - 1 : 0;
            var point = points[index];
            node.transform.position = point;
            node.transform.rotation = GetRotation(points, index, !byEnd);
        }

        private void ClearInternalPaths(RoadSegmentCreator creator)
        {
            foreach (var node in creator.CreatedTrafficNodes)
            {
                node.IterateAllPaths(path => path.DestroyPath(false));
            }
        }

        private RoadSegmentCreator GenerateCustomPrefab(ICustomPrefabRoad crossingPrefab)
        {
            if (IsCreated(crossingPrefab))
                return null;

            RoadSegmentCreator newCrossingPrefabCreator = null;

#if UNITY_EDITOR
            var prefab = PrefabExtension.FindPrefabByName(crossingPrefab.Name);

            if (!prefab)
            {
                notFoundObjects.Add(crossingPrefab.SceneObject);
                Debug.Log($"Prefab for '{crossingPrefab.Name}' not found.");
                return null;
            }

            if (!prefabBinding.ContainsKey(prefab))
            {
                Debug.Log($"Road segment for '{crossingPrefab.Name}' not created or prefab not assigned.");
                return null;
            }

            var creatorPrefab = prefabBinding[prefab];
            var position = crossingPrefab.Position;
            var rotation = crossingPrefab.Rotation;

            newCrossingPrefabCreator = CreateSegment(creatorPrefab.gameObject, position, rotation, crossingPrefab.SceneObject, creatorPrefab.name, crossingPrefab, RSGenType.CustomPrefab);

            newCrossingPrefabCreator.SnapNodes(false, customSnapObject: crossingPrefab.SceneObject);

            for (int i = 0; i < newCrossingPrefabCreator.TrafficNodeCount; i++)
            {
                var node = newCrossingPrefabCreator.TryToGetNode(i);

                var flipDirection = node.IsOneWay && !node.IsEndOfOneWay;
                var connectedNode = node.CastConnectedNode(false, true);

                if (connectedNode)
                {
                    var connectedCreator = connectedNode.TrafficLightCrossroad.GetComponent<RoadSegmentCreator>();

                    if (connectedCreator.GetComponent<RSPrefabBinding>())
                        continue;

                    if (node.IsOneWay)
                    {
                        if (!connectedNode.IsOneWay)
                        {
                            connectedCreator.oneWay = true;
                            connectedCreator.laneCount = node.LaneCount;
                            connectedCreator.Create();
                        }

                        if (connectedNode.IsEndOfOneWay == node.IsEndOfOneWay)
                        {
                            connectedCreator.shouldRevertDirection = !connectedCreator.shouldRevertDirection;
                        }
                    }

                    if (connectedCreator.LaneWidth != node.LaneWidth)
                    {
                        connectedCreator.LaneWidth = node.LaneWidth;
                    }

                    connectedCreator.dividerWidth = node.DividerWidth;
                    connectedCreator.Create();

                    if (config.GenerateSpawnNodes)
                    {
                        connectedCreator.GeneratePathInnerSpawnNodes(config.MinNodeOffsetDistance);
                    }
                }
                else
                {
                    Debug.Log($"{newCrossingPrefabCreator.name} connected node not found.");
                }
            }

#endif

            return newCrossingPrefabCreator;
        }

        private RoadSegmentCreator Generate(ISplineRoad splineRoad)
        {
            if (!IsAvailable(splineRoad))
                return null;

            var createdSegment = TryToGetSegment(splineRoad);

            if (createdSegment != null)
                return createdSegment.RoadSegmentCreator;

            var points = splineRoad.Points;

            RoadSegmentCreator creator = CreateSegment(splineRoad.Position, Quaternion.identity, splineRoad.SceneObject, $"{splineRoad.Name}_segment", splineRoad, RSGenType.SplineRoad);

            float currentLaneWidth = 0;

            if (autoLaneCount)
            {
                currentLaneWidth = laneWidth;
                var totalLaneCount = Mathf.FloorToInt((splineRoad.Width + laneThreshold + 0.01f) / laneWidth);

                bool isOneway = false;

                if (totalLaneCount == 1)
                {
                    isOneway = true;
                    creator.laneCount = 1;
                }
                else
                {
                    creator.laneCount = totalLaneCount / 2;
                }

                creator.oneWay = isOneway;
            }
            else
            {
                creator.laneCount = splineRoad.LaneCount;
                currentLaneWidth = splineRoad.Width;
            }

            config.ApplyPreCreationSplineSettings(creator, splineRoad.Name);

            creator.LaneWidth = currentLaneWidth;

            creator.Create(RoadSegmentCreator.RoadSegmentType.CustomStraightRoad);

            GenerateSpline(creator, splineRoad.SceneObject, points);

            return creator;
        }

        private void GenerateSpline(RoadSegmentCreator creator, GameObject sceneObject, List<Vector3> points, bool recreate = false)
        {
            var lastIndex = points.Count - 1;
            var node1 = creator.TryToGetNode(0);
            var node2 = creator.TryToGetNode(1);

            var sourcePos1 = node1.transform.position;
            var sourcePos2 = node2.transform.position;

            node1.transform.position = points[0];
            node1.transform.rotation = GetRotation(points, 0);

            float totalDistance = 0;
            float remainOffset = segmentOffset;

            int skippedFirst = 0;

            for (int i = 0; i < points.Count - 1; i++)
            {
                var distance = Vector3.Distance(points[i], points[i + 1]);
                totalDistance += distance;

                if (totalDistance >= segmentOffset)
                {
                    var sourcePoint = points[i] + (points[i + 1] - points[i]).normalized * remainOffset;
                    node1.transform.position = sourcePoint;
                    node1.transform.rotation = GetRotation(points, i, false);
                    break;
                }
                else
                {
                    skippedFirst++;
                    remainOffset -= distance;
                }
            }

            totalDistance = 0;
            remainOffset = segmentOffset;

            int skippedLast = 0;

            for (int i = points.Count - 1; i > 0; i--)
            {
                var distance = Vector3.Distance(points[i - 1], points[i]);
                totalDistance += distance;

                if (totalDistance >= segmentOffset)
                {
                    var sourcePoint = points[i] + (points[i - 1] - points[i]).normalized * remainOffset;
                    node2.transform.position = sourcePoint;
                    node2.transform.rotation = GetRotation(points, i, true);
                    lastIndex = i - 1;
                    break;
                }
                else
                {
                    remainOffset -= distance;
                    skippedLast++;
                }
            }

            var tempPath = creator.GetTempPath(0);

            tempPath.Nodes[0].transform.rotation = node1.transform.rotation;
            tempPath.Nodes[tempPath.Nodes.Count - 1].transform.rotation = GetRotation(points, lastIndex);

            for (int i = 1; i < points.Count - 1; i++)
            {
                var pathNode = tempPath.InsertNode(points[i], i, false);

                pathNode.transform.rotation = GetRotation(points, i, true);
            }

            for (int i = 0; i < skippedFirst; i++)
            {
                tempPath.RemoveNodeAt(1, false, false);
            }

            for (int i = 0; i < skippedLast; i++)
            {
                tempPath.RemoveNodeAt(tempPath.Nodes.Count - 1, false, false);
            }

            tempPath.CreatePath(true, false);

            tempPath.RenameNodes();

            creator.SnapToSurfaceCustomPath(sceneObject);

            if (recreate)
            {
                creator.InitOuter();
            }

            creator.Recalculate(false);

            config.ApplyPostCreationSplineSettings(creator);

            if (recreate)
            {
                if (ShouldRecalc(sourcePos1, node1))
                {
                    RecalcConnected(creator, node1, points, false);
                }

                if (ShouldRecalc(sourcePos2, node2))
                {
                    RecalcConnected(creator, node2, points, true);
                }
            }
        }

        private bool ShouldRecalc(Vector3 sourcePos, TrafficNode trafficNode)
        {
            return Vector3.Distance(trafficNode.transform.position, sourcePos) > MinRecalcNodeDistance;
        }

        private void RecalcConnected(RoadSegmentCreator creator, TrafficNode trafficNode, List<Vector3> points, bool byEnd)
        {
            RSSceneBinding rsSceneBinding = null;
            TrafficNode connectedNode = null;

            if (creator.AllConnectedOuterPaths.ContainsKey(trafficNode) && creator.AllConnectedOuterPaths[trafficNode].Count > 0)
            {
                var outerPath = creator.AllConnectedOuterPaths[trafficNode][0];
                connectedNode = outerPath.SourceTrafficNode;
                rsSceneBinding = connectedNode.TrafficLightCrossroad.GetComponent<RSSceneBinding>();
            }

            if (rsSceneBinding == null)
                return;

            var connectedCreator = rsSceneBinding.GetComponent<RoadSegmentCreator>();

            if (rsSceneBinding.RSGenType == RSGenType.Crossing)
            {
                connectedCreator.InitOuter();

                ClearInternalPaths(connectedCreator);

                SetCrossNodeTransform(connectedNode, points, byEnd);

                connectedCreator.SnapNodes(false, true);
                connectedCreator.CreateAutoPaths(false);
                connectedCreator.SnapNodes(false, false);

                connectedCreator.UpdatePaths(connectedNode, false);
                connectedCreator.RecalculateAllOuterConnectedPaths(false);
            }
            else
            {
                connectedCreator.AttachInnerExternalPaths(connectedNode, false);
            }
        }

        private RoadSegmentCreator CreateSegment(Vector3 position, Quaternion rotation, GameObject sceneObject, string name, IRoadObject roadObject, RSGenType rsGenType)
        {
            return CreateSegment(RoadPrefab.gameObject, position, rotation, sceneObject, name, roadObject, rsGenType);
        }

        private RoadSegmentCreator CreateSegment(GameObject prefab, Vector3 position, Quaternion rotation, GameObject sceneObject, string name, IRoadObject roadObject, RSGenType rsGenType)
        {
            GameObject createdObject = UnityEditor.PrefabUtility.InstantiatePrefab(prefab, roadParent.transform) as GameObject;

            UnityEditor.PrefabUtility.UnpackPrefabInstance(createdObject, UnityEditor.PrefabUnpackMode.OutermostRoot, UnityEditor.InteractionMode.AutomatedAction);

            var creator = createdObject.GetComponent<RoadSegmentCreator>();

            creator.transform.position = position;
            creator.transform.rotation = rotation;

            var roadSegmentSceneBinding = createdObject.AddComponent<RSSceneBinding>();
            var hash = GetObjectHash(roadObject);

            roadSegmentSceneBinding.RSGenType = rsGenType;
            roadSegmentSceneBinding.SetBinding(sceneObject, hash);
            creator.name = name;

            config.ApplySettings(creator, rsGenType, name);

            generatedSegments.Add(new GeneratedData()
            {
                SceneSegment = roadSegmentSceneBinding,
                Hash = roadSegmentSceneBinding.BindingHash
            });

            return creator;
        }

        private Quaternion GetRotation(List<Vector3> points, int index, bool opposite = false)
        {
            if (points.Count < 2)
                return Quaternion.identity;

            Quaternion rot = default;

            if (index + 1 < points.Count)
            {
                rot = Quaternion.LookRotation((points[index] - points[index + 1]).normalized);
            }
            else
            {
                rot = Quaternion.LookRotation((points[index - 1] - points[index]).normalized, Vector3.up);
            }

            if (opposite)
            {
                rot = rot * Quaternion.Euler(0, 180, 0);
            }

            return rot;
        }

        private void Clear()
        {
            crossings.Clear();
            prefabBinding.Clear();
            objectToRSBinding.Clear();
            lockedSegments.Clear();
            failedSegments.Clear();
            duplicateHashSegments.Clear();
            notFoundObjects.Clear();
            ignoredObjects.Clear();
            deadEndNodes.Clear();
            splineRoads.Clear();
            lastUpdatedBinding = null;
        }

        private void FindSceneObjects()
        {
            sceneRSObjects.Clear();

            var objs = FindObjects<RSSceneBinding>();

            foreach (var obj in objs)
            {
                if (!sceneRSObjects.ContainsKey(obj.BindingHash))
                {
                    sceneRSObjects.Add(obj.BindingHash, obj);
                }
                else
                {
                    duplicateHashSegments.Add(obj);
                }
            }

            sceneSourceRoadObjects.Clear();

            AddSceneRoadObjects();
        }

        private RSSceneBinding TryToGetSegment(IRoadObject roadObject)
        {
            var hash = GetObjectHash(roadObject);

            if (sceneRSObjects.ContainsKey(hash))
                return sceneRSObjects[hash];

            return null;
        }

        private void UpdateSegment(Component roadComponent, RSSceneBinding rsSceneBinding)
        {
            lastUpdatedBinding = rsSceneBinding;
            var creator = rsSceneBinding.RoadSegmentCreator;
            creator.DestroyTempPath();
            creator.GenerateCustomTempPath();

            var splineRoad = GetSplineRoad(roadComponent);
            GenerateSpline(creator, rsSceneBinding.SelectedSceneObject, splineRoad.Points, true);

            var hash = GetObjectHash(splineRoad);

            var generatedData = generatedSegments.Where(a => a.Hash == rsSceneBinding.BindingHash).FirstOrDefault();

            if (generatedData != null && generatedData.Hash != hash)
            {
                generatedData.Hash = hash;
                EditorSaver.SetObjectDirty(this);
            }

            rsSceneBinding.SetBinding(rsSceneBinding.SelectedSceneObject, hash);
        }

        private bool IsAvailable(ISplineRoad splineRoad)
        {
            if (splineRoad.Width < minLaneWidth)
                return false;

            if (splineRoad.Points.Count < 2)
                return false;

            var roadLength = Vector3.Distance(splineRoad.Points[0], splineRoad.Points[splineRoad.Points.Count - 1]);

            if (roadLength <= minRoadLength)
                return false;

            if (!NotIgnored(splineRoad))
                return false;

            return splineRoad.IsAvailable;
        }

        private bool NotIgnored(IRoadObject roadObject)
        {
            if (!CheckIgnoreName(roadObject.SceneObject.name))
            {
                ignoredObjects.Add(roadObject.SceneObject);
                return false;
            }

            return true;
        }

        private bool CheckIgnoreName(string name)
        {
            for (int i = 0; i < ignoreRoadNames.Count; i++)
            {
                if (name.Contains(ignoreRoadNames[i], StringComparison.InvariantCultureIgnoreCase))
                    return false;
            }

            return true;
        }

        private bool IsCreated(IRoadObject roadObject)
        {
            return TryToGetSegment(roadObject) != null;
        }

        private void Undo_undoRedoPerformed()
        {
            if (lastUpdatedBinding != null)
                UpdateSegment(lastUpdatedBinding, lastUpdatedBinding.SelectedSceneObject);
        }

        #endregion
#endif
    }
}
