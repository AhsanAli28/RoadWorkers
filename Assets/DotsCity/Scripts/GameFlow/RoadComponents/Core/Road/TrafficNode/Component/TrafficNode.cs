using Spirit604.CityEditor.Road;
using Spirit604.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    [ExecuteInEditMode]
    public partial class TrafficNode : MonoBehaviour, IBakeRoad
    {
        public enum ExternalConnectionType { Path, MergeNode }

        private const float MaxMergeDistanceSq = 9f;

        #region Inspector variables

        [HideInInspector][SerializeField] private RoadSegmentCreator roadSegmentCreator;

        [Tooltip("The crossroad to which the node belongs")]
        [SerializeField] private TrafficLightCrossroad trafficLightCrossroad;

        [Tooltip("Traffic light that the traffic node is linked")]
        [SerializeField] private TrafficLightHandler trafficLightHandler;

        [SerializeField] private TrafficNodeCrosswalk trafficNodeCrosswalk;
        [SerializeField] private Path pathPrefab;
        [SerializeField] private Transform pathParent;
        [SerializeField] private ExternalConnectionType externalConnection;
        [SerializeField] private TrafficNode mergeNode;

        [Tooltip("Right side lanes")]
        [SerializeField] private List<LaneArray> lanes;

        [Tooltip("Left side lanes")]
        [SerializeField] private List<LaneArray> externalLanes;

        [SerializeField] private LaneCountType laneCountType;

        [Tooltip("Number of lanes")]
        [SerializeField][Range(1, 10)] private int laneCount = 1;

        [Tooltip("Number of external lanes")]
        [SerializeField][Range(1, 10)] private int externalLaneCount = 1;

        [Tooltip("Lane width")]
        [SerializeField][Range(0.5f, 20f)] private float laneWidth = 3.8f;

        [Tooltip("Divider line width")]
        [SerializeField][Range(0f, 20f)] private float dividerWidth = 0;

        [Tooltip("Chance of the vehicle spawning in the node")]
        [SerializeField][Range(0, 1f)] private float chanceToSpawn = 1f;

        [Tooltip("Weight of the node for route selection by traffic")]
        [SerializeField][Range(0, 1f)] private float weight = 1f;

        [Tooltip("Custom distance to achieve a node (if 0 value default value will be taken)")]
        [SerializeField][Range(0, 5f)] private float customAchieveDistance;

        [Tooltip("" +
            "<b>Right</b> : right-hand lanes have traffic lights\r\n\r\n" +
            "<b>Left</b> : left-hand lanes have traffic lights\r\n\r\n" +
            "<b>Right and left</b> : right and left lanes have traffic lights")]
        [SerializeField] private TrafficNodeDirectionType lightType = TrafficNodeDirectionType.Right;

        [Tooltip("" +
            "<b>Parking</b> : node where cars are parked.\r\n\r\n" +
            "<b>Traffic public stop</b> : node where public traffic stops to pick up passengers\r\n\r\n" +
            "<b>Destroy vehicle</b>: node where the vehicle entity is destroyed (useful for nodes outside the map)\r\n\r\n" +
            "<b>Traffic area</b> : TrafficArea node\r\n\r\n" +
            "<b>Idle</b> : node where the vehicle is idling\r\n\r\n" +
            "<b>Trigger</b> : node to notify the user that the entity has reached the node. TriggerNodeConfig should be activated\r\n\r\n" +
            "<b>Trigger And Destroy</b> : node to notify the user that the entity has reached the node & then destroy the entity. TriggerNodeConfig should be activated")]
        [SerializeField] private TrafficNodeType trafficNodeType = TrafficNodeType.Default;

        [Tooltip("Quick on/off crosswalk option for pedestrians")]
        [SerializeField] private bool hasCrosswalk = true;

        [Tooltip("All lanes are one-way traffic lanes")]
        [SerializeField] private bool isOneWay;

        [Tooltip("Node ends one-way traffic for this RoadSegment")]
        [SerializeField] private bool isEndOfOneWay;

        [Tooltip("On/off prevent auto path creation")]
        [SerializeField] private bool lockPathAutoCreation;

        [Tooltip("Auto path is created")]
        [SerializeField] private bool autoPathIsCreated;

        [SerializeField] private TrafficNodeSubNodeType subNodeType;

        [HideInInspector][SerializeField] private int tempCreatorLocalIndex;

        #endregion

        #region Variables

#if UNITY_EDITOR

        internal List<Path> AllConnectedOuterPaths { get; set; }
#endif

        #endregion

        #region Properties

        public TrafficLightCrossroad TrafficLightCrossroad { get => trafficLightCrossroad; set => trafficLightCrossroad = value; }
        public TrafficLightHandler TrafficLightHandler { get => trafficLightHandler; set => trafficLightHandler = value; }
        public TrafficNodeCrosswalk TrafficNodeCrosswalk { get => trafficNodeCrosswalk; set => trafficNodeCrosswalk = value; }
        public Transform PathParent { get => pathParent; set => pathParent = value; }
        public List<LaneArray> Lanes { get => lanes; set => lanes = value; }
        public List<LaneArray> ExternalLanes { get => externalLanes; set => externalLanes = value; }
        public ExternalConnectionType ExternalConnection { get => externalConnection; set => externalConnection = value; }
        public TrafficNode MergeNode => mergeNode;

        public LaneCountType LaneCountType { get => laneCountType; set => laneCountType = value; }
        public int LaneCount { get => laneCount; set => laneCount = value; }
        public int ExternalLaneCount { get => laneCountType == LaneCountType.Same ? LaneCount : externalLaneCount; set => externalLaneCount = value; }

        public int TotalLaneCount
        {
            get
            {
                int currentLaneCount = 0;

                if (HasRightLanes)
                {
                    currentLaneCount += laneCount;
                }

                if (HasLeftLanes)
                {
                    if (laneCountType == LaneCountType.Same)
                    {
                        currentLaneCount += laneCount;
                    }
                    else
                    {
                        currentLaneCount += externalLaneCount;
                    }
                }

                return currentLaneCount;
            }
        }

        public float LaneWidth { get => laneWidth; set => laneWidth = value; }
        public float DividerWidth { get => !isOneWay ? dividerWidth : 0; set => dividerWidth = value; }
        public float ChanceToSpawn { get => chanceToSpawn; set => chanceToSpawn = value; }
        public float Weight { get => weight; set => weight = value; }
        public float CustomAchieveDistance { get => customAchieveDistance; set => customAchieveDistance = value; }
        public TrafficNodeDirectionType LightType { get => lightType; set => lightType = value; }
        public TrafficNodeType TrafficNodeType { get => trafficNodeType; set => trafficNodeType = value; }
        public bool HasCrosswalk { get => hasCrosswalk; set => hasCrosswalk = value; }
        public bool IsOneWay { get => isOneWay; set => isOneWay = value; }
        public bool IsEndOfOneWay { get => isEndOfOneWay; set => isEndOfOneWay = value; }
        public bool LockPathAutoCreation { get => lockPathAutoCreation; set => lockPathAutoCreation = value; }
        public bool HasRightLanes => !IsOneWay || (IsOneWay && !IsEndOfOneWay);
        public bool HasLeftLanes => !IsOneWay || (IsOneWay && IsEndOfOneWay);
        public bool IgnoreEmptyLanes => trafficNodeType == TrafficNodeType.DestroyVehicle;
        public bool HasSubNodes => subNodeType != TrafficNodeSubNodeType.None;
        public TrafficNodeSubNodeType SubNodeType => subNodeType;
        public string CrossroadName => trafficLightCrossroad ? trafficLightCrossroad.name : "NaN";
        public bool NodeIsMerged => MergeNodeMode && mergeNode != null && mergeNode.MergeNodeMode && mergeNode.MergeNode == this;

        #region Helper Properties

        public float CalculatedRouteWidth => !isOneWay ? ((float)TotalLaneCount / 2) * laneWidth + dividerWidth / 2 : laneCount * (laneWidth / 2);
        public int TempCreatorLocalIndex { get => tempCreatorLocalIndex; set => tempCreatorLocalIndex = value; }
        private bool MergeNodeMode => externalConnection == ExternalConnectionType.MergeNode;

        #endregion

        #endregion

        #region Unity lifecycle

#if !RUNTIME_ROAD
        private void Awake()
        {
            if (Application.isPlaying)
            {
                var collider = GetComponent<Collider>();

                if (collider)
                    collider.enabled = false;
            }
        }
#endif

        private void OnDestroy()
        {
            if (!Application.isPlaying)
            {
                UnselectMerge();
            }
        }

        #endregion

        #region Methods

        #region IBakeRoad interface

        public void Bake()
        {
            bool changed = false;
            var subNodeTypeLocal = TrafficNodeSubNodeType.None;

            this.IterateAllPaths((path) =>
            {
                path.BakePathLength();

                path.IterateWaypoints(pathNode =>
                {
                    if (pathNode.SpawnNode)
                    {
                        if (!path.IsExternal)
                        {
                            subNodeTypeLocal |= TrafficNodeSubNodeType.Inner;
                        }
                        else
                        {
                            subNodeTypeLocal |= TrafficNodeSubNodeType.Outer;
                        }

                        if (pathNode.UniqueID == 0)
                        {
                            pathNode.GenerateId();
                        }
                    }
                });

            }, true);

            if (GenerateIds())
                changed = true;

            CheckForNullPaths();

            bool hasAnyRightPath = false;
            bool hasAnyLeftPath = false;

            if (HasRightLanes)
            {
                bool hasLanes = true;

                if (trafficNodeType != TrafficNodeType.DestroyVehicle)
                {
                    if (lanes == null || lanes.Count == 0)
                    {
                        hasLanes = false;

                        string additionalText = string.Empty;

                        if (trafficNodeType == TrafficNodeType.Default)
                        {
                            additionalText = " Make sure this node has the right-hand paths, otherwise, enable 'DestroyVehicle' type if you want the car to be destroyed in a dead end.";
                        }

                        UnityEngine.Debug.Log($"{GetHeader()} has empty <b>right lanes</b> (green arrow).{additionalText}{TrafficObjectFinderMessage.GetMessage()}");
                    }
                }

                if (hasLanes)
                {
                    this.IterateAllPaths((path) =>
                    {
                        hasAnyRightPath = true;
                        path.CheckConnection(false);
                    }, false);
                }
            }

            if (HasLeftLanes)
            {
#if !RUNTIME_ROAD
                if (trafficNodeType != TrafficNodeType.DestroyVehicle)
                {
                    if (!MergeNodeMode)
                    {
                        if (externalLanes == null || externalLanes.Count == 0)
                        {
                            UnityEngine.Debug.Log($"{GetHeader()} has empty <b>left lanes</b> (violet arrow). Make sure that the traffic node is connected to another segment, otherwise, enable 'DestroyVehicle' type if you want the car to be destroyed in a dead end.{TrafficObjectFinderMessage.GetMessage()}");
                        }
                    }
                }
#endif

                if (!MergeNodeMode)
                {
                    this.IterateExternalPaths((path) =>
                    {
                        hasAnyLeftPath = true;
                        path.CheckConnection(true);
                    });
                }
            }

            if (MergeNodeMode)
            {
                if (mergeNode == null)
                {
                    UnityEngine.Debug.Log($"{GetHeader()} merge node is null.{TrafficObjectFinderMessage.GetMessage()}");
                }
                else
                {
                    var distance = Vector3.SqrMagnitude(transform.position - mergeNode.transform.position);

                    if (distance > MaxMergeDistanceSq)
                    {
                        UnityEngine.Debug.Log($"{GetHeader()} merge nodes is too far away, connect these nodes with external paths.{TrafficObjectFinderMessage.GetMessage()}");
                    }
                }
            }

            this.IterateAllLanes((data, laneIndex, external) =>
            {
                if (hasAnyRightPath && !external || hasAnyLeftPath && external)
                {
                    if (data == null || data.paths.Count == 0)
                    {
                        string laneText = !external ? "right" : "left";
                        UnityEngine.Debug.Log($"{GetHeader()} {laneText} lane '{laneIndex}' null or empty.{TrafficObjectFinderMessage.GetMessage()}");
                    }
                }

            }, HasLeftLanes && !MergeNodeMode, true);

            if (this.subNodeType != subNodeTypeLocal)
            {
                this.subNodeType = subNodeTypeLocal;
                changed = true;
            }

            var paths = GetComponentsInChildren<Path>();

            for (int i = 0; i < paths.Length; i++)
            {
                Path path = paths[i];
                var rightPath = this.HasPath(path);

                bool leftPath = false;

                if (!rightPath)
                {
                    leftPath = this.HasPath(path, true);
                }

                if (!rightPath && !leftPath)
                {
                    UnityEngine.Debug.Log($"{GetHeader()} found not assigned path InstanceID {path.GetInstanceID()} delete this path or assign it in the Inspector in the correct lane field in the TrafficNode.{TrafficObjectFinderMessage.GetMessage()}");
                }
            }

            if (changed)
            {
                EditorSaver.SetObjectDirty(this);
            }
        }

        #endregion

        #region Common methods

        public void ConnectSegments()
        {
            ConnectSegments(default);
        }

        public void ConnectSegments(AutoConnectionSettings autoConnectionSettings)
        {
            if (lockPathAutoCreation)
                return;

            if (!HasLeftLanes)
                return;

            TrafficNode connectedTrafficNode = CastConnectedNode(autoConnectionSettings, false);

            if (connectedTrafficNode == null)
                return;

            if (!TrafficNodeExtension.IsCorrectDirConnection(this, connectedTrafficNode, false, false))
            {
                if (!CanMerge(connectedTrafficNode, autoConnectionSettings))
                    return;
            }

            ConnectNodes(connectedTrafficNode, autoConnectionSettings);
        }

        public bool ConnectNodes(TrafficNode connectedTrafficNode, AutoConnectionSettings autoConnectionSettings, bool force = false, bool doubleConnection = false)
        {
            if (doubleConnection)
            {
                connectedTrafficNode.ConnectNodes(this, autoConnectionSettings, force);
            }

            if (this.trafficLightCrossroad != null && this.trafficLightCrossroad == connectedTrafficNode.trafficLightCrossroad)
                return false;

            bool merged = CanMerge(connectedTrafficNode, autoConnectionSettings);

            if (autoConnectionSettings.ConnectCrosswalks && !merged)
            {
                if (CanConnectCrosswalkTo(connectedTrafficNode))
                    this.trafficNodeCrosswalk.Connect(connectedTrafficNode.TrafficNodeCrosswalk);
            }

            if (!HasLeftLanes)
                return false;

            if (autoPathIsCreated && !force && !merged)
                return false;

#if UNITY_EDITOR
            RecordUndo("Created path");
#endif

            if (merged)
            {
                ClearExternalPaths(true);
                connectedTrafficNode.RecordUndo();
                SetMerge(connectedTrafficNode);

#if UNITY_EDITOR
                UnityEditor.Undo.CollapseUndoOperations(UnityEditor.Undo.GetCurrentGroup());
#endif

                return true;
            }

            var tempExternalPaths = externalLanes;
            externalLanes = null;

            if (externalConnection == ExternalConnectionType.MergeNode)
            {
                externalConnection = ExternalConnectionType.Path;
                mergeNode = null;
            }

            for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
            {
                if (!gameObject)
                    continue;

                var path = Instantiate(pathPrefab);

                path.name = $"PathExt{laneIndex}";

#if UNITY_EDITOR
                UnityEditor.Undo.RegisterCreatedObjectUndo(path.gameObject, "Created path");
#endif

                PathHelper.CreateConnectedStraightPath(path, this, connectedTrafficNode, laneIndex, laneIndex, autoConnectionSettings.WaypointStepOffset);

                if (autoConnectionSettings.WaypointStepOffset > 0 && autoConnectionSettings.ExternalSubNodes)
                {
                    path.IterateWaypoints((pathNode, index) =>
                    {
                        if (index != 0 && index != path.WayPoints.Count - 1)
                        {
                            pathNode.SpawnNode = true;
                        }
                    });
                }

                Path oldPath = null;

                if (tempExternalPaths != null && tempExternalPaths.Count > laneIndex && tempExternalPaths[laneIndex].paths != null && tempExternalPaths[laneIndex].paths.Count > 0 && tempExternalPaths[laneIndex].paths[0])
                {
                    oldPath = tempExternalPaths[laneIndex].paths[0];
                }

                if (!oldPath)
                    continue;

                path.PathSpeedLimit = oldPath.PathSpeedLimit;
                path.TrafficGroupMask = oldPath.TrafficGroupMask.GetClone();
                path.ResetSpeedLimit(false);

                var wayPointCount = Mathf.Min(oldPath.WayPoints.Count, path.WayPoints.Count);

                for (int i = 0; i < wayPointCount; i++)
                {
                    if (!oldPath.WayPoints[i])
                        continue;

                    path.WayPoints[i].CustomGroup = oldPath.WayPoints[i].CustomGroup;

                    if (path.WayPoints[i].CustomGroup)
                    {
                        path.WayPoints[i].TrafficGroupMask = oldPath.WayPoints[i].TrafficGroupMask;
                    }

                    path.WayPoints[i].SpeedLimit = oldPath.WayPoints[i].SpeedLimit;

                    EditorSaver.SetObjectDirty(path.WayPoints[i]);
                }
            }

            ClearPaths(tempExternalPaths, true, false);

#if UNITY_EDITOR
            UnityEditor.Undo.CollapseUndoOperations(UnityEditor.Undo.GetCurrentGroup());
#endif

            if (gameObject)
            {
                autoPathIsCreated = true;

                EditorSaver.SetObjectDirty(this);
            }

            return true;
        }

        public TrafficNode CastConnectedNode(bool flipDirection = false)
        {
            return CastConnectedNode(default, flipDirection);
        }

        public TrafficNode CastConnectedNode(bool flipDirection = false, bool multiRaycast = false, float maxConnectionLength = 0)
        {
            var settings = new AutoConnectionSettings()
            {
                MultiAngleRaycast = multiRaycast,
                MaxConnectionLength = maxConnectionLength
            };

            return CastConnectedNode(settings, flipDirection);
        }

        public TrafficNode CastConnectedNode(AutoConnectionSettings autoConnectionSettings, bool flipDirection = false)
        {
            Vector3 direction = transform.forward;

            if (flipDirection)
            {
                direction = -direction;
            }

            Vector3 origin = TrafficNodeExtension.GetLanePosition(this, 0, true);

            const float yOffset = 0.5f;
            const float colliderSize = 2f;
            const float colliderOffset = colliderSize + 0.1f;
            const int ignoreRaycastLayer = 2;

            origin += new Vector3(0, yOffset);

            bool closeRaycast = autoConnectionSettings.CloseRaycast || autoConnectionSettings.AllowMerge;
            var previousLayer = gameObject.layer;

            if (closeRaycast)
            {
                origin -= direction * (colliderOffset);
                gameObject.layer = ignoreRaycastLayer;
            }

            if (autoConnectionSettings.Angle != 0)
            {
                var q = Quaternion.AngleAxis(autoConnectionSettings.Angle, transform.right);
                direction = q * direction;
            }

            RaycastHit raycastHit;

            if (autoConnectionSettings.MaxConnectionLength == 0)
            {
                autoConnectionSettings.MaxConnectionLength = 9999f;
            }
            else
            {
                if (closeRaycast)
                {
                    autoConnectionSettings.MaxConnectionLength += colliderOffset;
                }
            }

            var physicsScene = PhysicsSceneExtensions.GetPhysicsScene(gameObject.scene);
            physicsScene.Raycast(origin, direction, out raycastHit, autoConnectionSettings.MaxConnectionLength, 1 << LayerMask.NameToLayer(ProjectConstants.TRAFFIC_NODE_LAYER_NAME));

            TrafficNode connectedTrafficNode = null;

            if (raycastHit.collider != null)
            {
                TrafficNode trafficNode = raycastHit.transform.GetComponent<TrafficNode>();

                if (trafficNode != null && trafficNode != this)
                {
                    connectedTrafficNode = trafficNode;
                }
            }

#if UNITY_EDITOR
            float debugRayDistance = Mathf.Min(5f, autoConnectionSettings.MaxConnectionLength);
            UnityEngine.Debug.DrawLine(origin, origin + direction * debugRayDistance, connectedTrafficNode != null ? Color.green : Color.red, 2f);
#endif

            if (!connectedTrafficNode && autoConnectionSettings.MultiAngleRaycast)
            {
                const int raycastCount = 20;

                autoConnectionSettings.MultiAngleRaycast = false;

                for (int i = 0; i < raycastCount; i++)
                {
                    autoConnectionSettings.Angle = i - raycastCount / 2;

                    connectedTrafficNode = CastConnectedNode(autoConnectionSettings, flipDirection);

                    if (connectedTrafficNode)
                        return connectedTrafficNode;
                }
            }

            if (closeRaycast)
            {
                gameObject.layer = previousLayer;
            }

            return connectedTrafficNode;
        }

        public void ForceConnectSegments()
        {
            ForceConnectSegments(true);
        }

        public void ForceConnectSegments(bool report)
        {
            if (!lockPathAutoCreation)
            {
                autoPathIsCreated = false;
                var settings = new AutoConnectionSettings();
                settings.MultiAngleRaycast = true;
                ConnectSegments(settings);
            }
            else
            {
                bool error = CheckExternalConnection();

                if (!error)
                {
                    if (report)
                        UnityEngine.Debug.Log("TrafficNode auto-path creation is locked");
                }
            }
        }

        public bool CheckExternalConnection()
        {
            if (!lockPathAutoCreation)
                return true;

            if (!HasLeftLanes)
                return true;

            var error = false;
            var hasPath = false;

            this.IterateExternalPaths(path =>
            {
                hasPath = true;

                if (!path.HasConnection)
                {
                    error = true;
                    UnityEngine.Debug.Log($"Crossroad '{CrossroadName}'. TrafficNode '{name}' {this.GetInstanceID()} InstanceID. Path '{path.name}' InstanceID {this.GetInstanceID()} connection is null. Fix the connection for this path or uncheck 'Lock path auto creation' in the TrafficNode settings.{TrafficObjectFinderMessage.GetMessage()}");
                }
            });

            if (externalLanes != null && (externalLanes.Count > 0 && !hasPath || externalLanes.Count == 0) && trafficNodeType == TrafficNodeType.Default && !error)
            {
                UnityEngine.Debug.Log($"Crossroad '{CrossroadName}' TrafficNode '{name}' {this.GetInstanceID()} InstanceID. Has no paths, but external connection is locked. Uncheck 'Lock path auto creation' in the TrafficNode settings.{TrafficObjectFinderMessage.GetMessage()}");
                error = true;
            }

            return !error;
        }

        public void ResetNode()
        {
            if (!lockPathAutoCreation)
            {
                autoPathIsCreated = false;
                EditorSaver.SetObjectDirty(this);
            }
        }

        /// <summary> Available connection within road segment. </summary>
        public bool AvailableForInnerConnection() => HasLeftLanes;

        /// <summary> Available connection from another road segment. </summary>
        public bool AvailableForOuterConnection() => HasRightLanes;

        public bool GenerateIds(bool force = false)
        {
            bool changed = false;

            this.IterateAllLanes((laneData, index, external) =>
            {
                if (laneData.UniqueID == 0 || force)
                {
                    var pos = this.GetLanePosition(index, external);
                    laneData.UniqueID = UniqueIdUtils.GetUniqueID(this, pos);
                    changed = true;
                }
            }, true);

            return changed;
        }

        public int GetCrosswalkNodeIndex(PedestrianNode node)
        {
            if (trafficNodeCrosswalk)
            {
                return trafficNodeCrosswalk.GetCrosswalkNodeIndex(node);
            }

            return -1;
        }

        public PedestrianNode GetCrosswalkNode(int index)
        {
            if (trafficNodeCrosswalk)
            {
                return trafficNodeCrosswalk.GetCrosswalkNode(index);
            }

            return null;
        }

        #endregion

        #region Path methods

        public bool AddPath(Path newPath, int laneIndex, bool reparent = false, bool isExternalLane = false, bool saveReparentUndo = false, bool lockAutoPath = false)
        {
            if (IsOneWay)
            {
                if (IsEndOfOneWay && !isExternalLane)
                {
                    var crossRoadName = $"Crossroad {trafficLightCrossroad?.name}" ?? "";
                    UnityEngine.Debug.LogError($"Trying to add 'DefaultLane' type path '{newPath.name}' to an IsOneWay {IsOneWay} & IsEndOfOneWay {IsEndOfOneWay} TrafficNode '{this.name}' InstanceID {this.GetInstanceID()}  {crossRoadName}{TrafficObjectFinderMessage.GetMessage()}");
                    return false;
                }

                if (!IsEndOfOneWay && isExternalLane)
                {
                    var crossRoadName = $"Crossroad {trafficLightCrossroad?.name}" ?? "";
                    UnityEngine.Debug.LogError($"Trying to add 'ExternalLane' type path '{newPath.name}' to an IsOneWay {IsOneWay} & IsEndOfOneWay {IsEndOfOneWay} TrafficNode '{this.name}' InstanceID {this.GetInstanceID()} {crossRoadName}{TrafficObjectFinderMessage.GetMessage()}");
                    return false;
                }
            }

            bool added = false;

            var targetLanes = !isExternalLane ? Lanes : ExternalLanes;

            int laneCount = 0;

            if (targetLanes != null)
            {
                laneCount = targetLanes.Count;
            }

            int newLaneCount = Mathf.Max(laneCount, laneIndex + 1);

            List<LaneArray> lanes = new List<LaneArray>(newLaneCount);

            for (int i = 0; i < newLaneCount; i++)
            {
                lanes.Add(new LaneArray());
            }

            for (int i = 0; i < targetLanes?.Count; i++)
            {
                lanes[i].paths = targetLanes[i].paths;
            }

            int remainLanes = laneIndex - laneCount + 1;

            for (int i = 0; i < remainLanes; i++)
            {
                lanes[laneCount + i].paths = new List<Path>();
            }

            if (!lanes[laneIndex].paths.Contains(newPath))
            {
                added = true;

                lanes[laneIndex].paths.Add(newPath);
            }

            if (!isExternalLane)
            {
                Lanes = lanes;
            }
            else
            {
                ExternalLanes = lanes;
                autoPathIsCreated = true;

                if (lockAutoPath)
                {
                    lockPathAutoCreation = true;
                }
            }

            if (reparent)
            {
                if (!saveReparentUndo)
                {
                    newPath.transform.parent = pathParent;
                }
                else
                {
#if UNITY_EDITOR
                    UnityEditor.Undo.SetTransformParent(newPath.transform, pathParent, "Path Reparent");
#endif
                }
            }

            EditorSaver.SetObjectDirty(this);

            return added;
        }

        public bool TryToRemovePath(Path path, bool recordUndo = false)
        {
            bool removed = false;

            for (int laneIndex = 0; laneIndex < lanes?.Count; laneIndex++)
            {
                var paths = lanes[laneIndex].paths;

                if (paths.Contains(path))
                {
                    if (!removed && recordUndo)
                    {
                        RecordUndo();
                    }

                    paths.Remove(path);

                    if (paths.Count == 0 && laneIndex == lanes.Count - 1)
                    {
                        lanes.RemoveAt(laneIndex);
                    }

                    if (lanes.Count > 0)
                    {
                        var anyPath = GetAnyPath();

                        if (!anyPath)
                        {
                            lanes.Clear();
                        }
                    }

                    removed = true;
                    break;
                }
            }

            for (int laneIndex = 0; laneIndex < externalLanes?.Count; laneIndex++)
            {
                var paths = externalLanes[laneIndex].paths;

                if (paths.Contains(path))
                {
                    if (!removed && recordUndo)
                    {
                        RecordUndo();
                    }

                    paths.Remove(path);

                    if (paths.Count == 0 && laneIndex == externalLanes.Count - 1)
                    {
                        externalLanes.RemoveAt(laneIndex);
                    }

                    if (externalLanes.Count > 0)
                    {
                        var anyPath = GetAnyPath(true);

                        if (!anyPath)
                        {
                            externalLanes.Clear();
                        }
                    }

                    removed = true;
                    break;
                }
            }

            if (removed)
            {
                EditorSaver.SetObjectDirty(this);
            }

            return removed;
        }

        public bool DestroyPath(int sourceLaneIndex, int targetLaneIndex, TrafficNode targetNode, bool externalDirection = false, bool recordNodeUndo = false, bool recordPathUndo = false)
        {
#if UNITY_EDITOR
            if (recordNodeUndo)
            {
                RecordUndo();
            }
#endif

            var path = TryToGetPath(sourceLaneIndex, targetLaneIndex, targetNode, externalDirection);

            if (path != null)
            {
                path.DestroyPath(recordPathUndo);
                return true;
            }

            return false;
        }

        public bool HasPath(Path path, bool externalDirection = false)
        {
            var laneIndex = GetLaneIndexOfPath(path, externalDirection);
            return laneIndex != -1;
        }

        public bool AlreadyHasPath(int sourceLaneIndex, int targetLaneIndex, TrafficNode targetNode, bool externalDirection = false)
        {
            var path = TryToGetPath(sourceLaneIndex, targetLaneIndex, targetNode, externalDirection);
            return path != null;
        }

        public Path TryToGetPath(int sourceLaneIndex, int targetLaneIndex, TrafficNode targetNode, bool externalDirection = false)
        {
            if (!externalDirection)
            {
                if (lanes?.Count > sourceLaneIndex)
                {
                    for (int i = 0; i < lanes[sourceLaneIndex].paths?.Count; i++)
                    {
                        var path = lanes[sourceLaneIndex].paths[i];

                        if (path && path.ConnectedTrafficNode == targetNode && path.ConnectedLaneIndex == targetLaneIndex)
                        {
                            return path;
                        }
                    }
                }
            }
            else
            {
                if (externalLanes?.Count > sourceLaneIndex)
                {
                    for (int i = 0; i < externalLanes[sourceLaneIndex].paths?.Count; i++)
                    {
                        var path = externalLanes[sourceLaneIndex].paths[i];

                        if (path && path.ConnectedTrafficNode == targetNode && path.ConnectedLaneIndex == targetLaneIndex)
                            return path;
                    }
                }
            }

            return null;
        }

        public LaneArray TryToGetLaneData(int laneIndex, bool externalDirection = false)
        {
            if (!externalDirection)
            {
                if (LaneExist(laneIndex))
                    return lanes[laneIndex];
            }
            else
            {
                if (LaneExist(laneIndex, true))
                    return externalLanes[laneIndex];
            }

            return null;
        }

        public bool LaneExist(int laneIndex, bool externalDirection = false, bool checkForEmptyPaths = false)
        {
            if (laneIndex < 0) return false;

            if (!externalDirection)
            {
                if (lanes?.Count > laneIndex)
                {
                    if (!checkForEmptyPaths)
                    {
                        return true;
                    }
                    else
                    {
                        return lanes[laneIndex].paths.Count > 0;
                    }
                }
            }
            else
            {
                if (externalLanes?.Count > laneIndex)
                {
                    if (!checkForEmptyPaths)
                    {
                        return true;
                    }
                    else
                    {
                        return externalLanes[laneIndex].paths.Count > 0;
                    }
                }
            }

            return false;
        }

        public int GetLaneIndexOfPathCheckAll(Path path)
        {
            var laneIndex = GetLaneIndexOfPath(path, false);

            if (laneIndex == -1)
            {
                laneIndex = GetLaneIndexOfPath(path, true);
            }

            return laneIndex;
        }

        public int GetLaneIndexOfPath(Path path, bool externalDirection = false)
        {
            if (!externalDirection)
            {
                for (int laneIndex = 0; laneIndex < lanes?.Count; laneIndex++)
                {
                    if (lanes[laneIndex].paths.Contains(path))
                        return laneIndex;
                }
            }
            else
            {
                for (int laneIndex = 0; laneIndex < externalLanes?.Count; laneIndex++)
                {
                    if (externalLanes[laneIndex].paths.Contains(path))
                        return laneIndex;
                }
            }

            return -1;
        }

        public bool IsExternalPath(Path path) => GetLaneIndexOfPath(path) == -1;

        public int GetLocalLaneIndexOfPath(Path path, bool externalDirection = false)
        {
            if (!externalDirection)
            {
                for (int laneIndex = 0; laneIndex < lanes?.Count; laneIndex++)
                {
                    if (lanes[laneIndex].paths.Contains(path))
                        return lanes[laneIndex].paths.IndexOf(path);
                }
            }
            else
            {
                for (int laneIndex = 0; laneIndex < externalLanes?.Count; laneIndex++)
                {
                    if (externalLanes[laneIndex].paths.Contains(path))
                        return externalLanes[laneIndex].paths.IndexOf(path);
                }
            }

            return -1;
        }

        public void SetPathParent(Transform newParent)
        {
            for (int laneIndex = 0; laneIndex < lanes?.Count; laneIndex++)
            {
                for (int i = 0; i < lanes[laneIndex].paths?.Count; i++)
                {
                    lanes[laneIndex].paths[i].transform.SetParent(newParent);
                }
            }
        }

        public bool HasAtleastOneLane(bool external = false)
        {
            return GetAnyPath(external) != null;
        }

        public Path GetAnyPath(bool external = false)
        {
            Path anyPath = null;

            if (!external)
            {
                this.IterateAllPaths((path) =>
                {
                    if (path)
                    {
                        anyPath = path;
                        return;
                    }
                });
            }
            else
            {
                this.IterateExternalPaths((path) =>
                {
                    if (path)
                    {
                        anyPath = path;
                        return;
                    }
                });
            }

            return anyPath;
        }

        public void CheckForNullPaths()
        {
            CheckForNullPathsInternal(lanes);
            CheckForNullPathsInternal(externalLanes, false);
        }

        public int GetLaneCount(bool externalLanes = false)
        {
            if (!externalLanes)
            {
                if (!HasRightLanes)
                    return 0;

                return laneCount;
            }
            else
            {
                if (HasLeftLanes)
                {
                    return laneCountType == LaneCountType.Same ? laneCount : externalLaneCount;
                }

                return 0;
            }
        }

        public void ClearIntersectionLanes()
        {
            ClearIntersectionLanes(Lanes);
            ClearIntersectionLanes(ExternalLanes);
        }

        public void ClearEmptyLanes()
        {
            bool changed = false;

            if (!HasLeftLanes && ExternalLanes?.Count > 0)
            {
                ExternalLanes.Clear();
                changed = true;
            }

            if (!HasRightLanes && Lanes?.Count > 0)
            {
                Lanes.Clear();
                changed = true;
            }

            if (changed)
            {
                EditorSaver.SetObjectDirty(this);
            }
        }

        public int ClearPaths(bool external = false, bool recordUndo = true, bool allowClearArray = false)
        {
            var currentLanes = !external ? lanes : externalLanes;

            if (currentLanes == null) return 0;

            return ClearPaths(currentLanes, recordUndo, recordUndo, allowClearArray);
        }

        private void CheckForNullPathsInternal(List<LaneArray> lanes, bool isRight = true)
        {
            for (int i = 0; i < lanes?.Count; i++)
            {
                var paths = lanes[i];

                if (paths == null || paths.paths == null || paths.paths.Count == 0)
                {
                    string crossroadName = GetCrossroadName();
                    string laneText = isRight ? "Right" : "Left";
                    UnityEngine.Debug.LogError($"{crossroadName} TrafficNode '{name}' InstanceID {this.GetInstanceID()} {laneText} Lane '{i}' is empty{TrafficObjectFinderMessage.GetMessage()}");
                }

                for (int j = 0; j < paths?.paths?.Count; j++)
                {
                    var path = paths.paths[j];

                    if (!path)
                    {
                        string crossroadName = GetCrossroadName();

                        string laneText = isRight ? "Right" : "Left";
                        UnityEngine.Debug.LogError($"{crossroadName} TrafficNode '{name}' InstanceID {this.GetInstanceID()} {laneText} Lane '{i}' Local index '{j}' is null{TrafficObjectFinderMessage.GetMessage()}");
                    }
                }
            }
        }

        private int ClearPaths(List<LaneArray> lanes, bool recordUndo = true, bool recordSegmentUndo = true, bool allowClearArray = false)
        {
            if (lanes == null || lanes.Count == 0)
                return 0;

            if (recordUndo && recordSegmentUndo)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RegisterCompleteObjectUndo(this, "Clear External Paths");
#endif
            }

            int count = 0;

            for (int laneIndex = 0; laneIndex < lanes?.Count; laneIndex++)
            {
                var paths = lanes[laneIndex].paths;

                if (paths == null)
                    continue;

                while (paths.Count > 0)
                {
                    var path = paths[0];

                    paths.RemoveAt(0);

                    if (path == null)
                        continue;

                    count++;

                    if (!recordUndo)
                    {
                        DestroyImmediate(path.gameObject);
                    }
                    else
                    {
#if UNITY_EDITOR
                        UnityEditor.Undo.DestroyObjectImmediate(path.gameObject);
#endif
                    }
                }
            }

            if (allowClearArray)
            {
                lanes.Clear();
            }

            return count;
        }

        private void ClearIntersectionLanes(List<LaneArray> lanes1)
        {
            for (int lane1 = 0; lane1 < lanes1?.Count; lane1++)
            {
                var paths = lanes1[lane1].paths;

                for (int j = 0; j < paths.Count; j++)
                {
                    if (paths[j])
                    {
                        paths[j].InitializeInstersects(new List<Path.IntersectPointInfo>());
                        EditorSaver.SetObjectDirty(paths[j]);
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"TrafficNode. {name} Instance {GetInstanceID()} lane {lane1} local index {j} path is null{TrafficObjectFinderMessage.GetMessage()}");
                    }
                }
            }
        }

        private void ClearExternalPaths(bool recordUndo = true)
        {
            ClearPaths(externalLanes, recordUndo);
            externalLanes.Clear();
        }

        private string GetCrossroadName() => trafficLightCrossroad?.name ?? "Unknown crossroad";

        private bool CanMerge(TrafficNode connectedTrafficNode, AutoConnectionSettings autoConnectionSettings)
        {
            return autoConnectionSettings.AllowMerge && Vector3.Distance(transform.position, connectedTrafficNode.transform.position) < autoConnectionSettings.MergeDistance;
        }

        #endregion

        #region Other methods

        public void Initialize(RoadSegmentCreator roadSegmentCreator)
        {
            this.roadSegmentCreator = roadSegmentCreator;
        }

        public bool HasLight(int laneDirection)
        {
            if (this.trafficLightHandler)
            {
                if (laneDirection == 1 && LightType.HasFlag(TrafficNodeDirectionType.Right) || (laneDirection == -1 && LightType.HasFlag(TrafficNodeDirectionType.Left)))
                {
                    var hasLight = trafficLightHandler.HasLight(this) && this.trafficLightHandler.gameObject.activeSelf;

                    return hasLight;
                }
            }

            return false;
        }

        public void Resize(bool recordUndo = false)
        {
            var boxCollider = GetComponent<BoxCollider>();

            if (boxCollider)
            {
                var size = boxCollider.size;
                size.x = CalculatedRouteWidth * 2;

                if (boxCollider.size != size)
                {
                    if (recordUndo)
                    {
#if UNITY_EDITOR
                        UnityEditor.Undo.RegisterCompleteObjectUndo(boxCollider, "Undo collider");
#endif
                    }

                    boxCollider.size = size;
                }
            }

            trafficNodeCrosswalk.SetCrosswalkPosition(this, recordUndo);
        }

        public void SetColliderSize(Vector3 size)
        {
            var boxCollider = GetComponent<BoxCollider>();

            if (boxCollider)
            {
                boxCollider.size = size;
            }
        }

        public Vector3 GetColliderSize()
        {
            var boxCollider = GetComponent<BoxCollider>();

            if (boxCollider)
            {
                return boxCollider.size;
            }

            return default;
        }

        public bool CanConnectCrosswalkTo(TrafficNode connectedNode)
        {
            return CanConnectCrosswalk(this) && CanConnectCrosswalk(connectedNode);
        }

        public bool ConnectedToCrosswalk(TrafficNode connectedNode)
        {
            return TrafficNodeCrosswalk.IsConnected(connectedNode.TrafficNodeCrosswalk);
        }

        public void ConnectCrosswalk(TrafficNode connectedNode)
        {
            if (TrafficNodeCrosswalk && connectedNode.TrafficNodeCrosswalk)
                TrafficNodeCrosswalk.Connect(connectedNode.TrafficNodeCrosswalk);
        }

        public void DisconnectCrosswalk(TrafficNode connectedNode)
        {
            if (TrafficNodeCrosswalk && connectedNode.TrafficNodeCrosswalk)
                TrafficNodeCrosswalk.Disconnect(connectedNode.TrafficNodeCrosswalk);
        }

        public TrafficNode TryToGetConnectedExternalNode()
        {
            TrafficNode externalNode = null;

            this.IterateExternalPaths((path) =>
            {
                if (externalNode)
                    return;

                if (path.ConnectedTrafficNode)
                {
                    externalNode = path.ConnectedTrafficNode;
                }
            });

            return externalNode;
        }

        public void SetMerge(TrafficNode mergeNode, bool auto = false)
        {
            if (mergeNode == this || this.mergeNode == mergeNode)
                return;

            UnselectMerge(auto);

            this.mergeNode = mergeNode;
            this.externalConnection = ExternalConnectionType.MergeNode;
            EditorSaver.SetObjectDirty(this);

            if (auto)
                return;

            this.mergeNode.SetMerge(this, true);
        }

        public void UnselectMerge(bool auto = false, bool makeDefaultConnection = false)
        {
            if (mergeNode != null)
            {
                if (auto)
                    mergeNode.UnselectMerge();

                mergeNode = null;

                if (makeDefaultConnection)
                {
                    externalConnection = ExternalConnectionType.Path;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        internal bool CanConnectCrosswalk(TrafficNode node)
        {
            return node && node.trafficNodeCrosswalk && node.trafficNodeCrosswalk.Enabled && node.HasCrosswalk;
        }

        private string GetHeader() => $"TrafficNode InstanceID <b>{this.GetInstanceID()}</b>";

        #endregion

        #region Editor events

#if UNITY_EDITOR

        public void OnInspectorEnabled()
        {
            AllConnectedOuterPaths = this.GetAllConnectedOuterPaths(true);
        }

        public void OnInspectorDisabled()
        {
            AllConnectedOuterPaths = null;
        }

        public void SaveAllPaths()
        {
            this.IterateAllOuterPaths((path) =>
            {
                path.SaveMovementUndo();
            });

            this.IterateAllPaths((path) =>
            {
                if (!path) return;

                path.SaveMovementUndo();
            }, true);

            UnityEditor.Undo.RegisterCompleteObjectUndo(this, "Undo node");
            EditorExtension.CollapseUndoCurrentOperations();
        }

        public void ReattachAllPaths()
        {
            this.IterateAllOuterPaths((path) =>
            {
                path.AutoAttachToTrafficNodes();
            });

            this.IterateAllPaths((path) =>
            {
                path.AutoAttachToTrafficNodes();
            }, false);

            this.IterateExternalPaths((path) =>
            {
                path.AutoAttachToTrafficNodes();
            });
        }

        public void DestroyNode()
        {
            if (!Application.isPlaying)
            {
                roadSegmentCreator?.RemoveTrafficNode(this, recordUndoPaths: true);
                trafficLightCrossroad?.RemoveNode(this, true, true);
                UnityEditor.Undo.DestroyObjectImmediate(this.gameObject);
                EditorExtension.CollapseUndoCurrentOperations();
            }
        }

#endif

        private void RecordUndo(string undoMessage = "Node revert")
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCompleteObjectUndo(this, undoMessage);
#endif
        }

        #endregion
    }
}

#endregion