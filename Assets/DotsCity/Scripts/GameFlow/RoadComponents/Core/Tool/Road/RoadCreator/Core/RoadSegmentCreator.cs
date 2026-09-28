using Spirit604.Attributes;
using Spirit604.CityEditor.Utils;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using UnityEngine;
using Path = Spirit604.Gameplay.Road.Path;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spirit604.CityEditor.Road
{
    [ExecuteInEditMode]
    public partial class RoadSegmentCreator : MonoBehaviour
    {
        #region Variables

        #region Constans

        public const int MinCrossRoadDirectionCount = 3;
        public const int MaxCrossRoadDirectionCount = 4;

        #endregion

        #region Helper types

        public enum RoadSegmentType
        {
            DefaultCrossRoad = 0,
            TurnRoad = 1,
            StraightRoad = 2,
            MergeCrossRoad = 3,
            MergeStraightRoad = 4,
            MergeCrossRoadToOneWayRoad = 5,
            OneWayStraight = 6,
            OneWayTurn = 7,
            Roundabout = 10,
            CustomStraightRoad = 8,
            CustomSegment = 9
        }

        public enum TurnCurveType { BezierCube, BezierQuad }

        public enum LightLocation
        {
            Right,
            Left,
            RightLeft
        }

        public enum SnapObjectType
        {
            All = TrafficNode | PathNode,
            TrafficNode = 1 << 0,
            PathNode = 1 << 1,
        }

        public enum PathDirectionType { Left, Forward, Right }

        public enum PedestrianCornerConnectionType { Disabled, Corner, Straight }

        public enum NewNodeSettingsType { Prefab, Unique, CopyLast, CopySelected }

        public enum ViewType { Toolbar, Tabs }

        public enum ParkingConfigType { Temp, Selected }

        public enum ParkingConnectionSourceType { Path, Node, SingleNode }

        public enum HandleType
        {
            None = 0,
            Position = 1,
            Rotation = 2,
        }

        public enum LineHandleObjectType { ParkingLine, ParkingPlace }

        public enum ParkingPositionSnapType { Disabled, Custom }

        public enum ParkingRotationSnapType { Disabled, RightCorner, Custom }

        public enum ParkingPathRailType
        {
            None,
            EnterOnly = 1 << 0,
            ExitOnly = 1 << 1,
            EnterAndExit = EnterOnly | ExitOnly
        }

        [Flags]
        public enum LightType { Traffic = 1 << 0, Pedestrian = 1 << 1 }

        public enum TabType { CommonSettings, PathSettings, SnapSettins, LightSettings, SegmentSettings, OtherSettings }

        public enum AdditionalPathSettingsType { Default, ExtrudeLane, AutoCrossroad, IntersectionCreation }

        public enum AdditionalStraightRoadSettings { Default, StripNodes, GenerateSpawnNodes }

        public enum ExtrudeState { Default, WaitingForDrag, IsDrag, Creating }

        public const float StartDragDistance = 1f;

        #endregion

        #region Cached transforms & prefabs

        public Transform trafficNodeParent;
        public Transform cornerNodes;
        public Transform trafficLights;
        public Transform pedestrianLights;
        public TrafficLightCrossroad trafficLightCrossroad;
        public RoadSegmentCreatorConfig roadSegmentCreatorConfig;
        public int selectedTabIndex;

        #endregion

        #region General settings

        public RoadSegmentType roadSegmentType = RoadSegmentType.DefaultCrossRoad;

        public LaneCountType laneCountType;
        [Range(1, 10)] public int laneCount = 1;
        [Range(1, 10)] public int backwardLaneCount = 1;
        [Range(1, 10)] public int subLaneCount = 1;

        [Tooltip("Divider line width")]
        [Range(0, 20)] public float dividerWidth = 0f;
        [Range(0.1f, 20f)] public float customLaneWidth = 4f;

        [SerializeField] private bool customSubLaneWidth;
        [SerializeField][Range(0.1f, 20f)] private float subLaneWidth = 3.8f;

        [Tooltip("Swap regular node with sub-node")]
        [SerializeField] private bool flipSubLaneNode;

        [Tooltip("If the paths are connected from one node and intersect, the extra path is destroyed")]
        [SerializeField] private bool cleanupSelfIntersection = true;

        [SerializeField] private bool inverseCleanup;

        [Obsolete("No longer in use.")]
        public bool customLaneWidthEnabled;

        [Range(MinCrossRoadDirectionCount, MaxCrossRoadDirectionCount)] public int directionCount = 4;
        [Range(0, 100)] public float crossroadWidth = 12f;
        [Range(0, 100)] public float subTrafficNodeDistanceFromCenter = 14f;
        [Range(-100, 100)] public float trafficNodeOffset1;
        [Range(-100, 100)] public float trafficNodeOffset2;
        [Range(-180, 180)] public float additionalLocalAngle1;
        [Range(-180, 180)] public float additionalLocalAngle2;

        public Vector3 pathCorner1Offset = new Vector3(1f, 0, -1f);
        public Vector3 pathCorner2Offset = new Vector3(-1f, 0, 1f);

        [Obsolete("No longer in use.")]
        public bool uniqueCrossWalkOffset;

        public Vector3 customCrossWalkOffset = new Vector3(1, 0, 0);

        public bool customTrafficNodeSettings;
        public NewNodeSettingsType newNodeSettingsType;
        public int copySelectedIndex;

        #endregion

        #region Pedestrian node settings

        [Tooltip("Can pedestrian cross the road")]
        public bool addPedestrianNodes = true;

        [Range(0.1f, 10f)] public float pedestrianRouteWidth = 1f;

        public bool addAlongLine;
        [Range(-10f, 10f)] public float lineNodeOffset;
        [Range(0, 40f)] public float nodeSpacing = 10f;

        public NodeShapeType crossWalkNodeShape;
        [Range(0.1f, 10f)] public float crosswalkNodeWidth = 1f;
        [Range(0.1f, 10f)] public float crosswalkNodeHeight = 1f;

        public bool customCrossWalk;
        public bool connectCrosswalks;

        public List<bool> customPedestrianNodesData = new List<bool>() { true, true, true, true };

        public List<bool> customCrossWalksData = new List<bool>() { true, true, true, true };

        public PedestrianCornerConnectionType pedestrianCornerConnectionType;
        public Vector3 cornerOffset;
        public bool loopPedestrianConnection = true;
        public List<PedestrianNode> cornerPedestrianNodes = new List<PedestrianNode>();
        public List<int> cornerPedestrianNodesBinding = new List<int>();

        #endregion

        #region Additional settings

        public List<bool> isEnterOfOneWay = new List<bool>() { true, false };
        public bool shouldRevertDirection;
        public bool generateSpawnNodes;

        public List<CustomTurnData> customTurnDatas = new List<CustomTurnData>()
        {
            new CustomTurnData(), new CustomTurnData(), new CustomTurnData(), new CustomTurnData()
        };

        public bool[] customUTurnDatas = new bool[4]
        {
            false, false, false, false
        };

        public bool customNodeUTurnSettings;
        [Range(0, 4f)] public float stretchFactor = 1f;
        [Range(0, 45f)] public float stretchAngle = 0f;
        public bool customNodeTurnSettings;
        [Range(-10f, 20f)] public float trafficNodeHeight1;
        [Range(-10f, 20f)] public float trafficNodeHeight2;

        #region Attachments settings

        public bool parkingBuilderMode;
        public int parkingSettingsTabIndex = 0;
        public int parkingHandleTabIndex = 0;
        public ParkingConfigType parkingConfigType;
        public int selectedPathToolbarOption;

        [Tooltip("" +
            "<b>Path</b> : paths will be connected to the Parking source path\r\n\r\n" +
            "<b>Node</b> : paths will be connected to the selected TrafficNodes\r\n\r\n" +
            "<b>Single Node</b> : paths will be connected to the selected single TrafficNode (same node for enter & exit paths)")]
        public ParkingConnectionSourceType parkingConnectionSourceType;

        public Path parkingSourcePath;
        public TrafficNode sourceTrafficNode;
        public TrafficNode targetTrafficNode;
        [Range(0, 10)] public int connectionLaneIndex;
        public bool showSelectPathButtons;
        public bool autoRecalculateParkingPaths = true;
        public int handleTypeTabIndex = 1;

        [Tooltip("Show position handles, edit (add & remove) buttons of the parking paths")]
        public bool showEditPathParkingButtons = true;

        public bool showSaveParkingConfigSettings;
        public string configName;
        public ParkingLineSettingsContainer tempParkingLineSettings;
        public string[] parkingConfigNames;
        public Transform tempStartParkingPoint;
        public List<ParkingLineData> lineDatas = new List<ParkingLineData>();
        public List<TempParkingPathOffsetData> enterPathOffsets = new List<TempParkingPathOffsetData>();
        public List<TempParkingPathOffsetData> exitPathOffsets = new List<TempParkingPathOffsetData>();
        public int selectedParkingOffsetPathIndex;

        public bool ShowPathParkingHandles => handleTypeTabIndex == 1;
        public bool ShowPathParkingOffsetHandles => handleTypeTabIndex == 2;
        public bool ParkingBuilderMode => parkingBuilderMode && ParkingBuilderModeSupported;
        public bool ParkingBuilderModeSupported => roadSegmentType == RoadSegmentType.CustomSegment;

        public ParkingLineSettingsContainer CurrentParkingLineSettings
        {
            get
            {
                switch (parkingConfigType)
                {
                    case ParkingConfigType.Temp:
                        {
                            if (tempParkingLineSettings == null)
                            {
                                tempParkingLineSettings = ScriptableObject.CreateInstance<ParkingLineSettingsContainer>();
                            }

                            return tempParkingLineSettings;
                        }
                    case ParkingConfigType.Selected:
                        {
                            return roadSegmentCreatorConfig.GetParkingLineConfig;
                        }
                }

                return null;
            }
        }

        public Vector3 LineStartPointWorld { get; set; }

        public int SelectedParkingOffsetPathIndex => selectedParkingOffsetPathIndex - 2;

        public TabType SelectedTab => (TabType)selectedTabIndex;

        #endregion

        #region Custom snap straight road settings

        public int snapToolbarIndex;
        public int straightRoadSelectedNodeIndex;
        public readonly string[] customStraightRoadNodesNames = new string[] { "All", "Node1", "Node2" };

        #endregion

        #endregion

        #region Path Settings

        [Range(0, 200)] public int wayPointStraightRoadCount = 2;

        [Range(0, 200f)] public float straightRoadPathSpeedLimit = 60f;

        [Range(-10, 10)] public int straightRoadPriority = 0;

        public TurnCurveType turnCurveType = TurnCurveType.BezierCube;

        [Range(3, 20)] public int wayPointTurnCurveCount = 10;

        [Range(0, 200f)] public float turnRoadPathSpeedLimit = 30f;

        [Range(-10, 10)] public int turnRoadPriority = -1;

        [Tooltip("Show position handles of the TrafficNode")]
        public bool showTrafficNodeHandles = true;

        [Tooltip("Show of the forward of the TrafficNode")]
        public bool showTrafficNodeForward = true;

        [Tooltip("Show position handles of the path")]
        public bool showPathHandles;

        public int selectedTrafficNodeIndex = -1;
        public int selectedPathIndex = -1;
        public Path selectedPath;

        [Tooltip("Show edit (add & remove) buttons of the path")]
        public bool showEditButtonsPathNodes;
        public bool showWaypoints;
        public bool showWaypointsInfo;

        [Tooltip("<b>Extrude lane</b> : extrude new lane & TrafficNode from the selected lane point")]
        public AdditionalPathSettingsType additionalSettings;

        public bool Extrude => GetRoadSegmentType == RoadSegmentType.CustomSegment && additionalSettings == AdditionalPathSettingsType.ExtrudeLane;

        #endregion

        #region Light Settings

        [Range(2, 4)] public int minTrafficNodesCountForAddLight = 3;

        public int selectedLightNodeIndex = -1;
        public bool showLightIndexes;
        public bool addPedestrianLights = true;
        public bool addTrafficLights = true;

        public HandleType lightHandleType;
        public LightType lightType = LightType.Traffic;

        public bool lightSnapPosition = true;
        public bool lightAddHalfOffset = true;

        [Range(0, 20f)] public float lightSnapCustomSize = 1f;

        public bool lightAutoRoundRotation = true;

        [Range(0, 90)] public int lightRoundAngle = 10;

        public LightObjectData commonLightObjectData = new LightObjectData();
        public LightObjectData[] lightObjectDatas;

        #endregion

        #region Other Settings

        public bool oneWay;
        public SnapObjectType snapObjectType = SnapObjectType.All;
        public bool autoSnapPosition = true;
        public bool addHalfOffset = true;

        [Range(0, 20f)] public float autoSnapCustomSize = 1f;

        public bool autoRoundRotation = true;

        [Range(0, 90)] public int roundAngle = 10;

        public bool lockYAxisMove = true;
        public bool showYPosition = false;
        public bool roundYPosition = true;

        [Range(0, 5f)] public float roundYValue = 0.25f;

        [Tooltip("Minimum angle between normal faces to create new path nodes")]
        [Range(0.01f, 10f)] public float angleThreshold = 2f;

        [Tooltip("Min offset between generated path nodes")]
        [Range(0.1f, 5f)] public float minWaypointOffset = 1f;

        [Space]
        public List<Path> tempCustomPaths = new List<Path>();

        public string[] trafficNodeHeaders;
        public string[] pathHeaders;

        [SerializeField] private List<TrafficNode> createdTrafficNodes = new List<TrafficNode>();
        [SerializeField] private List<TrafficNode> createdSubLaneTrafficNodes = new List<TrafficNode>();

        public List<TrafficLightObject> createdLights = new List<TrafficLightObject>();
        public List<LightObjectBindingData> lightBinding = new List<LightObjectBindingData>();

        [SerializeField] private int createTrafficNodeCount;
        [SerializeField] private bool initialInit;

        private bool customRoadSegmentCreated;
        private RoadSegment roadSegment;
        private Dictionary<Vector2Int, HashSet<Vector2Int>> addedConnections;

#if UNITY_EDITOR

        [NonSerialized] public int sourceIgnoreIndex;
        [NonSerialized] public int targetIgnoreIndex = 1;
        [NonSerialized] public List<Vector2Int> ignoreConnections = new List<Vector2Int>();

        public event Action<List<TrafficNode>> ParkingLineDestroyed = delegate { };
        public event Action<List<TrafficNode>> ParkingLineCreated = delegate { };
        public event Action OnInspectorRepaintRequested = delegate { };

        private Transform currentLightParent;
        private Dictionary<int, Path> pathIndexBinding;

        /// <summary> All paths connected from other segments to current traffic node. </summary>
        private Dictionary<TrafficNode, List<Path>> allConnectedOuterPaths;
#endif

        #endregion

        #endregion

        #region Events

        public event Action<TrafficNode> OnTrafficNodeAdd = delegate { };
        public event Action<TrafficNode> OnTrafficNodeRemove = delegate { };
        public event Action<Path> OnPathSelectionChangedEvent = delegate { };

        #endregion

        #region Properties

        public bool ShowPathHandles { get => showPathHandles; set => showPathHandles = value; }
        public List<TrafficNode> CreatedTrafficNodes { get => createdTrafficNodes; set => createdTrafficNodes = value; }

        public List<TrafficNode> CreatedSubLaneTrafficNodes { get => createdSubLaneTrafficNodes; set => createdSubLaneTrafficNodes = value; }
        public LaneCountType LaneCountType => CustomLaneTypeSupport ? laneCountType : LaneCountType.Same;
        public bool CustomLaneTypeSupport => !oneWay;
        public RoadSegmentType GetRoadSegmentType { get => roadSegmentType; }
        public float LaneWidth { get => customLaneWidth; set => customLaneWidth = value; }
        public float DividerWidth => IsCustom() ? dividerWidth : 0;
        public float SubLaneWidth => !customSubLaneWidth ? LaneWidth : subLaneWidth;
        public bool FlipSubLaneNodeAvailable => roadSegmentType == RoadSegmentType.MergeCrossRoadToOneWayRoad;
        public bool FlipSubLaneNode => flipSubLaneNode && FlipSubLaneNodeAvailable;
        public float AdditionalLocalAngle1 => AdditionalLocalAngleSupported ? additionalLocalAngle1 : 0;
        public float AdditionalLocalAngle2 => AdditionalLocalAngleSupported ? additionalLocalAngle2 : 0;
        public Vector3 CrossWalkOffset => customCrossWalkOffset;
        public Vector3 PathCorner1Offset => pathCorner1Offset;
        public Vector3 PathCorner2Offset => pathCorner2Offset;
        public int CreateTrafficNodeCount { get => !IsCustom() ? createTrafficNodeCount : createdTrafficNodes.Count; set => createTrafficNodeCount = value; }
        private bool ShouldCreateLight { get => CreateTrafficNodeCount >= minTrafficNodesCountForAddLight; }
        public bool AttachmentSupported => IsStraightRoad() && !SubLaneSupport() && !IsCustom();
        public bool TrafficLightPlacingSupported => !IsCustom();
        public bool AdditionalLocalAngleSupported => IsTurnRoad();
        public bool PedestrianLoopConnectionSupported => CreateTrafficNodeCount == 3;
        public bool ShowSegmentPositionHandle => roadSegmentCreatorConfig?.ShowSegmentPositionHandle ?? false;
        public bool SnapSegmentPosition => roadSegmentCreatorConfig?.SnapSegmentPosition ?? false;
        public bool AddHalfOffset => roadSegmentCreatorConfig?.AddHalfOffset ?? false;
        public float CustomSnapSize => roadSegmentCreatorConfig?.CustomSnapSize ?? 0;
        public float SnapSurfaceOffset => roadSegmentCreatorConfig?.SnapSurfaceOffset ?? 0;
        public LayerMask SnapLayerMask => roadSegmentCreatorConfig?.SnapLayerMask ?? ~0;
        public bool DebugCast => roadSegmentCreatorConfig?.DebugCast ?? false;
        public int TrafficNodeCount => createdTrafficNodes.Count;
        public TrafficNodeDirectionType PathDirection => roadSegmentCreatorConfig.PathDirection;

        public RoadSegment RoadSegment
        {
            get
            {
                if (roadSegment)
                {
                    return roadSegment;
                }

                roadSegment = GetComponent<RoadSegment>();

                return roadSegment;
            }
        }

        public bool AddAlongLine => addAlongLine && addPedestrianNodes && IsCustomStraight();

#if UNITY_EDITOR

        public Dictionary<TrafficNode, List<Path>> AllConnectedOuterPaths => allConnectedOuterPaths;
        public Dictionary<TrafficNode, RoadSegmentCreator> MergedCrossings = new Dictionary<TrafficNode, RoadSegmentCreator>();

#endif

        #region Extrude lane settings 

        public Vector3 StartDragPosition { get; set; }
        public Vector3 CurrentDragPosition { get; set; }
        public ExtrudeState CurrentExtrudeState { get; set; }
        public bool ExtrudeExternal { get; private set; }
        public TrafficNode SourceExtrudeNode { get; set; }
        public Quaternion ExtrudeNodeRotation { get; set; }
        public Vector3 HandleOffset { get; set; }
        public Vector3 ExtrudeNodeForward => ExtrudeNodeRotation * Vector3.forward;
        public int SourceExtrudeLaneIndex { get; set; }
        public int ExtrudeLaneCount { get => roadSegmentCreatorConfig.ExtrudeLaneCount; set => roadSegmentCreatorConfig.ExtrudeLaneCount = value; }
        public int CurrentExtrudeCount => Mathf.Min(MaxExtrudeLaneCount, ExtrudeLaneCount);

        public int MaxExtrudeLaneCount
        {
            get
            {
                if (SourceExtrudeNode != null)
                {
                    return SourceExtrudeLaneIndex + 1;
                }

                return 1;
            }
        }

        public bool CleanupSelfIntersection { get => cleanupSelfIntersection; set => cleanupSelfIntersection = value; }

        public bool InverseCleanup { get => inverseCleanup; set => inverseCleanup = value; }

        #endregion

        #endregion

        #region Unity lifecycle

#if UNITY_EDITOR

        private void OnDestroy()
        {
            RecordAllOuterConnectedPathUndo(false, true);
            ClearLights(true);
            EditorExtension.CollapseUndoCurrentOperations();
        }

#endif

        #endregion

        public void Create(RoadSegmentType segmentType)
        {
            this.roadSegmentType = segmentType;
            Create();
        }

        [Button]
        public void Create()
        {
            if (ObjectIsPrefab())
                return;

            var currentRotation = transform.transform.rotation;
            transform.transform.rotation = Quaternion.identity;

            if (roadSegmentType != RoadSegmentType.CustomStraightRoad)
            {
                CreateDefaultSegment();
            }
            else
            {
                CreateCustomSegment();
            }

            transform.transform.rotation = currentRotation;

            EditorSaver.SetObjectDirty(this);
        }

        public void Recalculate(bool recordUndo = true)
        {
            bool recreate = true;

            if (roadSegmentType == RoadSegmentType.CustomStraightRoad)
            {
                var tempCustomPath = GetTempPath(0);

                if (createdTrafficNodes.Count == 0 || tempCustomPath == null)
                {
                    customRoadSegmentCreated = false;
                }
                else
                {
                    recreate = false;
                    tempCustomPath.CreatePath(false);

                    SetTrafficNodeSettings(createdTrafficNodes[0].TempCreatorLocalIndex, createdTrafficNodes[0], false, recordUndo);
                    SetTrafficNodeSettings(createdTrafficNodes[1].TempCreatorLocalIndex, createdTrafficNodes[1], false, recordUndo);

                    if (oneWay || LaneCountType == LaneCountType.Same)
                    {
                        createdTrafficNodes[0].LaneCount = laneCount;
                        createdTrafficNodes[1].LaneCount = laneCount;

                        if (createdTrafficNodes[0].LaneCountType != LaneCountType.Same)
                        {
                            createdTrafficNodes[0].LaneCountType = LaneCountType.Same;
                        }

                        if (createdTrafficNodes[1].LaneCountType != LaneCountType.Same)
                        {
                            createdTrafficNodes[1].LaneCountType = LaneCountType.Same;
                        }
                    }
                    else
                    {
                        createdTrafficNodes[0].LaneCount = laneCount;
                        createdTrafficNodes[0].ExternalLaneCount = backwardLaneCount;

                        createdTrafficNodes[1].LaneCount = backwardLaneCount;
                        createdTrafficNodes[1].ExternalLaneCount = laneCount;

                        if (createdTrafficNodes[0].LaneCountType != LaneCountType.Custom)
                        {
                            createdTrafficNodes[0].LaneCountType = LaneCountType.Custom;
                        }

                        if (createdTrafficNodes[1].LaneCountType != LaneCountType.Custom)
                        {
                            createdTrafficNodes[1].LaneCountType = LaneCountType.Custom;
                        }
                    }

                    CreateOffsetPaths();
                    AttachAllInnerOuterPaths(recordUndo);

                    if (recordUndo)
                    {
#if UNITY_EDITOR
                        EditorExtension.CollapseUndoCurrentOperations();
#endif
                    }
                }
            }
            else if (roadSegmentType == RoadSegmentType.CustomSegment)
            {
                recreate = false;

                IterateAllNodes(node =>
                {
                    SetTrafficNodeSettings(node.TempCreatorLocalIndex, node, false, true);
                });

                UpdatePaths(true, true);

#if UNITY_EDITOR
                EditorExtension.CollapseUndoCurrentOperations();
#endif
            }

            if (recreate)
            {
                Create();
            }
        }

        [Button]
        public void Clear()
        {
            if (ObjectIsPrefab())
                return;

            Clear(trafficNodeParent);
            Clear(cornerNodes);
            ClearLights();
            ClearCornerNodes();
            createdTrafficNodes.Clear();
            createdSubLaneTrafficNodes.Clear();
            ClearParkingLine();

            if (trafficLightCrossroad)
            {
                trafficLightCrossroad.ClearNodes();
                trafficLightCrossroad.ClearHandlers();
            }

            DestroyTempPath();

            customRoadSegmentCreated = false;
        }

        public bool IsSubLane(int nodeIndex)
        {
            if (IsCustom(false) && createdTrafficNodes.Count > nodeIndex)
            {
                var node = createdTrafficNodes[nodeIndex];

                int max = 0;

                for (int i = 0; i < createdTrafficNodes.Count; i++)
                {
                    max = Mathf.Max(max, createdTrafficNodes[i].LaneCount);
                }

                return max > node.LaneCount;
            }

            if (roadSegmentType == RoadSegmentType.MergeCrossRoad)
                return nodeIndex % 2 != 0;

            if (roadSegmentType == RoadSegmentType.MergeCrossRoadToOneWayRoad)
            {
                if (!flipSubLaneNode)
                    return nodeIndex % 2 != 0;
                else
                    return nodeIndex % 2 == 0;
            }

            if (roadSegmentType == RoadSegmentType.MergeStraightRoad)
                return nodeIndex == 0;

            return false;
        }

        public bool IsCrossRoad()
        {
            bool isCrossRoad = roadSegmentType == RoadSegmentType.DefaultCrossRoad
                || roadSegmentType == RoadSegmentType.MergeCrossRoad
                || roadSegmentType == RoadSegmentType.MergeCrossRoadToOneWayRoad;

            return isCrossRoad;
        }

        public bool IsStraightRoad()
        {
            return roadSegmentType == RoadSegmentType.StraightRoad ||
                roadSegmentType == RoadSegmentType.MergeStraightRoad ||
                roadSegmentType == RoadSegmentType.OneWayStraight ||
                roadSegmentType == RoadSegmentType.CustomStraightRoad;
        }

        public bool IsTurnRoad()
        {
            return roadSegmentType == RoadSegmentType.TurnRoad ||
                roadSegmentType == RoadSegmentType.OneWayTurn;
        }

        public bool IsOneWayRoad(int index)
        {
            if (roadSegmentType == RoadSegmentType.MergeCrossRoadToOneWayRoad)
            {
                if (!flipSubLaneNode)
                    return index % 2 != 0;
                else
                    return index % 2 == 0;
            }

            return (roadSegmentType == RoadSegmentType.OneWayStraight ||
                roadSegmentType == RoadSegmentType.OneWayTurn) ||
                (roadSegmentType == RoadSegmentType.CustomStraightRoad && oneWay);
        }

        public bool CheckMissing() =>
            roadSegmentType == RoadSegmentType.DefaultCrossRoad ||
            roadSegmentType == RoadSegmentType.MergeCrossRoadToOneWayRoad ||
            roadSegmentType == RoadSegmentType.MergeStraightRoad ||
            roadSegmentType == RoadSegmentType.MergeCrossRoad;

        public bool IsCustom(bool includeStraight = true)
        {
            return (roadSegmentType == RoadSegmentType.CustomSegment ||
               IsCustomStraight() && includeStraight);
        }

        public bool IsCustomStraight() => roadSegmentType == RoadSegmentType.CustomStraightRoad;

        public bool SubLaneSupport()
        {
            bool hasSubLane = roadSegmentType == RoadSegmentCreator.RoadSegmentType.MergeCrossRoad
                || roadSegmentType == RoadSegmentCreator.RoadSegmentType.MergeCrossRoadToOneWayRoad
                || roadSegmentType == RoadSegmentCreator.RoadSegmentType.MergeStraightRoad;

            return hasSubLane;
        }

        public bool SubLaneTrafficNode()
        {
            bool hasSubLane = roadSegmentType == RoadSegmentCreator.RoadSegmentType.MergeCrossRoad
                || roadSegmentType == RoadSegmentCreator.RoadSegmentType.MergeCrossRoadToOneWayRoad;

            return hasSubLane;
        }

        public bool IsSubLaneTrafficNode(TrafficNode trafficNode)
        {
            return createdSubLaneTrafficNodes.Contains(trafficNode);
        }

        public bool RevertDirectionSupport()
        {
            return (roadSegmentType == RoadSegmentCreator.RoadSegmentType.OneWayStraight
                || roadSegmentType == RoadSegmentCreator.RoadSegmentType.OneWayTurn);
        }

        public bool CustomTurnSupport()
        {
            return (roadSegmentType == RoadSegmentCreator.RoadSegmentType.DefaultCrossRoad
                || roadSegmentType == RoadSegmentCreator.RoadSegmentType.MergeCrossRoad
                || roadSegmentType == RoadSegmentCreator.RoadSegmentType.MergeCrossRoadToOneWayRoad);
        }

        public void ConvertToCustom()
        {
#if UNITY_EDITOR
            Undo.RegisterCompleteObjectUndo(this, "Undo convert");

            UninitializeType(roadSegmentType, true);

            if (IsCustomStraight())
            {
                var tempPath = GetTempPath(0);

                if (tempPath != null)
                {
                    Undo.DestroyObjectImmediate(tempPath.gameObject);
                    tempCustomPaths.Clear();
                }
            }

            EditorExtension.CollapseUndoCurrentOperations();
#endif

            roadSegmentType = RoadSegmentType.CustomSegment;
            customTrafficNodeSettings = true;
            InitCustomSegment();
            EditorSaver.SetObjectDirty(this);
        }

        public void Rotate(float angle)
        {
            transform.Rotate(new Vector3(0, angle, 0));
            DirtyAllPaths();
        }

        public void SaveToPrefab()
        {
            string name = gameObject.name;

            GameObject createdRoad = null;

#if UNITY_EDITOR
            roadSegmentCreatorConfig.CheckForSavePath();
            var savePath = AssetDatabaseExtension.GetPrefabSavePath(roadSegmentCreatorConfig.SavePrefabPath, name);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, savePath);
            createdRoad = PrefabUtility.InstantiatePrefab(prefab, transform.parent) as GameObject;

            Debug.Log($"RoadSegmentCreator. Prefab successfully saved to '{savePath}'");
#endif

            createdRoad.transform.position = transform.position;
            createdRoad.transform.rotation = transform.rotation;

#if UNITY_EDITOR
            Selection.activeObject = createdRoad;
#endif

            RoadSegment sourceRoadSegment = gameObject.GetComponent<RoadSegment>();
            RoadSegmentPlacer roadSegmentPlacer = sourceRoadSegment.RoadSegmentPlacer;

#if UNITY_EDITOR
            sourceRoadSegment.RemoveFromCreator();
#endif

            if (roadSegmentPlacer)
            {
                roadSegmentPlacer.AddRoadSegment(createdRoad.GetComponent<RoadSegment>());
                roadSegmentPlacer.LoadAssets();
            }

            DestroyImmediate(gameObject);
        }

        public void DestroyTempPath(int index = -1)
        {
            if (index <= -1)
            {
                for (int i = 0; i < tempCustomPaths.Count; i++)
                {
                    Path tempCustomPath = tempCustomPaths[i];

                    if (tempCustomPath != null)
                    {
                        DestroyImmediate(tempCustomPath.gameObject);
                    }
                }

                tempCustomPaths.Clear();
            }
            else
            {
                var path = GetTempPath(index);

                if (path)
                {
                    DestroyImmediate(path.gameObject);
                }
            }
        }

        public CustomTurnData GetTurnData(int index)
        {
            if (CustomTurnSupport() && index < customTurnDatas.Count)
            {
                if (customNodeTurnSettings)
                {
                    return customTurnDatas[index];
                }
                else
                {
                    return customTurnDatas[0];
                }
            }

            return new CustomTurnData();
        }

        public bool GetUTurnData(int index)
        {
            if (CustomTurnSupport() && index < customUTurnDatas.Length)
            {
                if (customNodeUTurnSettings)
                {
                    return customUTurnDatas[index];
                }
                else
                {
                    return customUTurnDatas[0];
                }
            }

            return false;
        }

        public void SetUTurnData(int index, bool isEnabled)
        {
            if (CustomTurnSupport() && index < customUTurnDatas.Length)
            {
                customUTurnDatas[index] = isEnabled;
            }
        }

        public bool UTurnIsAvailable(int index)
        {
            var node = TryToGetNode(index);

            if (node && !node.IsOneWay) return true;

            return false;
        }

        public bool AnyUTurn()
        {
            if (CustomTurnSupport())
            {
                if (!customNodeUTurnSettings)
                {
                    return GetUTurnData(0);
                }
                else
                {
                    for (int i = 0; i < TrafficNodeCount; i++)
                    {
                        if (GetUTurnData(i))
                            return true;
                    }
                }
            }

            return false;
        }

        public bool GetCustomPedestrianNodeEnabledState(int index)
        {
            if (index >= 0)
            {
                CheckForSizeCollection(customPedestrianNodesData, index);
                return customPedestrianNodesData[index];
            }

            return true;
        }

        public void SetAllPedestrianEnabledState(bool isEnabled)
        {
            SetPedestrianEnabledState(-1, isEnabled);
        }

        public void SetPedestrianEnabledState(int index, bool isEnabled)
        {
            if (index != -1)
            {
                CheckForSizeCollection(customPedestrianNodesData, index);
                customPedestrianNodesData[index] = isEnabled;
            }
            else
            {
                for (int i = 0; i < createdTrafficNodes.Count; i++)
                {
                    SetPedestrianEnabledState(i, isEnabled);
                }
            }
        }

        public bool GetCrosswalkEnabledState(int index, bool relativePedestrianNodes = true)
        {
            if (index >= 0)
            {
                if (relativePedestrianNodes)
                {
                    if (!addPedestrianNodes)
                    {
                        return false;
                    }

                    CheckForSizeCollection(customPedestrianNodesData, index);
                    CheckForSizeCollection(customCrossWalksData, index);

                    if (!customCrossWalk)
                    {
                        return customCrossWalksData[0];
                    }
                    else
                    {
                        return customCrossWalksData[index] && customPedestrianNodesData[index];
                    }
                }
                else
                {
                    CheckForSizeCollection(customCrossWalksData, index);

                    if (!customCrossWalk)
                    {
                        return customCrossWalksData[0];
                    }
                    else
                    {
                        return customCrossWalksData[index];
                    }
                }
            }

            return true;
        }

        public void SetAllCrosswalkEnabled(bool isEnabled)
        {
            SetCrosswalkEnabledState(-1, isEnabled);
        }

        public void SetCrosswalkEnabledState(TrafficNode trafficNode, bool isEnabled)
        {
            var index = GetNodeIndex(trafficNode);

            if (index >= 0)
            {
                SetCrosswalkEnabledState(index, isEnabled);
            }
        }

        public void SetCrosswalkEnabledState(int index, bool isEnabled)
        {
            if (index != -1)
            {
                CheckForSizeCollection(customCrossWalksData, index);
                customCrossWalksData[index] = isEnabled;
            }
            else
            {
                for (int i = 0; i < createdTrafficNodes.Count; i++)
                {
                    SetCrosswalkEnabledState(i, isEnabled);
                }
            }
        }

        public void CheckForSizeCollection(List<bool> collection, int index)
        {
            if (index >= collection.Count)
            {
                int itemsToAdd = index - collection.Count + 1;

                for (int i = 0; i < itemsToAdd; i++)
                {
                    bool initialValue = true;

                    if (collection.Count > 0)
                    {
                        initialValue = collection[0];
                    }

                    collection.Add(initialValue);
                }
            }
        }

        public void SetCustomPedestrianWidth(float width, bool includeCrosswalk = false)
        {
            if (!addPedestrianNodes) return;

            var nodes = this.GetComponentsInChildren<PedestrianNode>();

            foreach (var node in nodes)
            {
                if (node.ConnectedTrafficNode != null && !includeCrosswalk) continue;

                node.MaxPathWidth = width;
                EditorSaver.SetObjectDirty(node);
            }
        }

        public void TryToSnapSegmentPosition()
        {
            if (SnapSegmentPosition)
            {
                SnapObject(transform, AddHalfOffset, CustomSnapSize);
            }
        }

        public void SnapObject(Transform objectToSnap, bool evenSizeSnapping, float customSnapSize)
        {
            Vector2Int size = GetSegmentSize(evenSizeSnapping);

            PositionHelper.RoundObjectPositionToTile(objectToSnap, size, customSnapSize);
        }

        public Vector3 SnapPosition(Vector3 objectToSnap, bool evenSizeSnapping, float customSnapSize)
        {
            Vector2Int size = GetSegmentSize(evenSizeSnapping);

            return PositionHelper.RoundPositionToTile(objectToSnap, size, customSnapSize);
        }

        public void SnapNodes(bool recordUndo = true, bool includeRotation = false, GameObject customSnapObject = null)
        {
            if (recordUndo)
            {
                RecordAllPathUndo(true, true);
            }

            IterateAllNodes(trafficNode =>
            {
                SnapToSurface(trafficNode, recordUndo: recordUndo, includeRotation: includeRotation, customSnapObject: customSnapObject);
            });

#if UNITY_EDITOR
            var paths = new List<Path>();
            int pathPointCount = 0;
#endif

            IterateAllNodes(trafficNode =>
            {
#if UNITY_EDITOR
                trafficNode.IterateAllPaths((path) =>
                {
                    if (pathPointCount == 0 && path.PathConnectionType == PathConnectionType.TrafficNode || path.PathConnectionType == PathConnectionType.PathPoint)
                    {
                        paths.Add(path);
                    }
                    else
                    {
                        paths.Insert(paths.Count - pathPointCount, path);
                    }

                    if (path.PathConnectionType == PathConnectionType.PathPoint)
                    {
                        pathPointCount++;
                    }

                }, true);
#endif
            });

#if UNITY_EDITOR
            foreach (Path path in paths)
                path.SnapToSurface(SnapLayerMask, SnapSurfaceOffset, recordUndo: recordUndo, customSnapObject: customSnapObject);
#endif

            float yCenter = 0;

            foreach (var trafficNode in createdTrafficNodes)
            {
                yCenter += trafficNode.transform.position.y;
            }

            yCenter /= createdTrafficNodes.Count;

            var offset = yCenter - transform.position.y;

            if (recordUndo)
            {
#if UNITY_EDITOR
                Undo.RecordObject(transform, "Edited Object Position");
#endif
            }

            transform.position = new Vector3(transform.position.x, yCenter, transform.position.z);

            foreach (var trafficNode in createdTrafficNodes)
            {
                trafficNode.transform.localPosition -= new Vector3(0, offset, 0);
            }

            if (recordUndo)
            {
#if UNITY_EDITOR
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif
            }
        }

        public void SnapToSurface(TrafficNode trafficNode, bool recordUndo = true, bool includeRotation = false, GameObject customSnapObject = null)
        {
#if UNITY_EDITOR

            SnapToSurface(trafficNode.transform, recordUndo: recordUndo, includeRotation: includeRotation, customSnapObject: customSnapObject);
            RecalculateNodeOuterConnections(trafficNode, false);

            if (recordUndo)
            {
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            }
#endif
        }

        public void SnapSegment()
        {
#if UNITY_EDITOR
            Undo.RecordObject(transform, "Edited Object Position");

            RecordAllPathUndo(true, true);
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            SnapToSurface(transform, SnapSurfaceOffset, false);

            UpdatePaths(false, true);
#endif
        }

        public void SnapToSurface(Transform sourceTransform, float surfaceOffset = 0, bool recordUndo = true, bool includeRotation = false, GameObject customSnapObject = null)
        {
            SnapUtils.SnapToSurface(sourceTransform, SnapLayerMask, surfaceOffset, recordUndo, includeRotation, customSnapObject);
        }

        public Path GetSelectedPath()
        {
            if (selectedTrafficNodeIndex >= 0 && selectedTrafficNodeIndex < CreatedTrafficNodes.Count)
            {
                return selectedPath;
            }

            return null;
        }

        public int GetInternalIndex(int orderIndex)
        {
            if (createdTrafficNodes.Count > orderIndex)
            {
                if (createdTrafficNodes[orderIndex] != null)
                {
                    return createdTrafficNodes[orderIndex].TempCreatorLocalIndex;
                }
            }

            return -1;
        }

        public void ForceSelectPath(TrafficNode sourceTrafficNode, Path path)
        {
#if UNITY_EDITOR
            var newSelectedTrafficNodeIndex = createdTrafficNodes.IndexOf(sourceTrafficNode);

            if (newSelectedTrafficNodeIndex != selectedTrafficNodeIndex)
            {
                selectedTrafficNodeIndex = newSelectedTrafficNodeIndex;
                InitializePathHeaders();
            }

            CheckBinding();

            foreach (var item in pathIndexBinding)
            {
                if (item.Value == path)
                {
                    selectedPathIndex = item.Key;
                }
            }

            OnPathSelectionChanged();
#endif
        }

        public Path AddTempPath(PathDirectionType pathDirectionType, TrafficNode sourceNode, TrafficNode targetNode, int index = -1, bool addNulls = false)
        {
            var path = CreatePath(pathDirectionType, sourceNode, targetNode);

            if (addNulls && tempCustomPaths.Count <= index)
            {
                int addCount = index - tempCustomPaths.Count + 1;

                for (int i = 0; i < addCount; i++)
                {
                    tempCustomPaths.Add(null);
                }
            }

            if (index == -1 || tempCustomPaths.Count <= index)
            {
                tempCustomPaths.Add(path);
            }
            else
            {
                if (tempCustomPaths[index] != null)
                {
                    DestroyImmediate(tempCustomPaths[index].gameObject);
                }

                tempCustomPaths[index] = path;
            }

            return path;
        }

        public Path GetTempPath(int index)
        {
            if (tempCustomPaths.Count > index && index >= 0)
            {
                return tempCustomPaths[index];
            }

            return null;
        }

        public bool HasTempPath(int index)
        {
            return tempCustomPaths.Count > index && tempCustomPaths[index] != null;
        }

        public void SwitchTempEnabledState(bool isActive, int index = -1)
        {
            if (index == -1)
            {
                for (int i = 0; i < tempCustomPaths.Count; i++)
                {
                    if (tempCustomPaths[i] && tempCustomPaths[i].gameObject)
                    {
                        tempCustomPaths[i].gameObject.SetActive(isActive);
                    }
                }
            }
            else
            {
                var tempPath = GetTempPath(index);

                if (tempPath && tempPath.gameObject)
                {
                    tempPath.gameObject.SetActive(isActive);
                }
            }
        }

#if UNITY_EDITOR
        public void RepaintInspector()
        {
            OnInspectorRepaintRequested();
        }
#endif

        public void StartLaneExtrude(TrafficNode node, int laneIndex, bool external = false)
        {
            StartDragPosition = node.GetLanePosition(laneIndex, external);
            CurrentExtrudeState = ExtrudeState.WaitingForDrag;
            ExtrudeExternal = external;
            SourceExtrudeNode = node;
            SourceExtrudeLaneIndex = laneIndex;

            ExtrudeNodeRotation = SourceExtrudeNode.transform.rotation;

            if (!external)
            {
                ExtrudeNodeRotation = ExtrudeNodeRotation * Quaternion.Euler(0, 180, 0);
            }

            var s = CurrentExtrudeCount;
        }

        public void ResetLaneExtrude()
        {
#if UNITY_EDITOR
            if (CurrentExtrudeState != ExtrudeState.Default)
            {
                CurrentExtrudeState = ExtrudeState.Default;
                SourceExtrudeNode = null;
                StartDragPosition = default;
                HandleOffset = default;
                OnInspectorRepaintRequested();
            }
#endif
        }

        public void CreateLaneExtrude()
        {
#if UNITY_EDITOR
            if (SourceExtrudeNode == null)
                return;

            Undo.RegisterCompleteObjectUndo(this, "Undo segment");

            var node = AddTrafficNode();

            Undo.RegisterCreatedObjectUndo(node.gameObject, "Undo segment");

            var currentExtrudeCount = CurrentExtrudeCount;
            node.LaneCount = currentExtrudeCount;
            node.LaneWidth = SourceExtrudeNode.LaneWidth;
            node.IsOneWay = true;
            node.Resize();

            node.transform.position = CurrentDragPosition;
            node.transform.rotation = ExtrudeNodeRotation;

            var dir1 = node.transform.position - SourceExtrudeNode.transform.position;
            var dir2 = SourceExtrudeNode.transform.forward;
            var dot = Vector3.Dot(dir1, dir2);

            var extrudeExternal = ExtrudeExternal;

            var flipConnection = extrudeExternal ? dot < 0 : dot > 0;

            node.IsEndOfOneWay = !flipConnection;

            var selectedNode = SourceExtrudeNode;

            if (flipConnection)
            {
                node.transform.rotation *= Quaternion.Euler(0, 180, 0);
                extrudeExternal = false;
                selectedNode = node;
            }

            Path path = null;

            for (int i = 0; i < currentExtrudeCount; i++)
            {
                path = CreatePath(PathDirectionType.Right, SourceExtrudeNode, node, TurnCurveType.BezierCube);

                Undo.RegisterCreatedObjectUndo(path.gameObject, "Undo segment");

                var pathName = !ExtrudeExternal ? "Path" : "PathExt";
                path.name = GetPathName(pathName, SourceExtrudeNode, node);

                var connectedLaneIndex = 0;

                if (!flipConnection)
                {
                    path.SourceTrafficNode = SourceExtrudeNode;
                    path.ConnectedTrafficNode = node;
                    SourceExtrudeNode.AddPath(path, SourceExtrudeLaneIndex - i, reparent: true, isExternalLane: extrudeExternal, lockAutoPath: extrudeExternal);
                    connectedLaneIndex = currentExtrudeCount - 1 - i;
                }
                else
                {
                    path.SourceTrafficNode = node;
                    path.ConnectedTrafficNode = SourceExtrudeNode;
                    node.AddPath(path, i, reparent: true, isExternalLane: extrudeExternal, lockAutoPath: extrudeExternal);
                    connectedLaneIndex = SourceExtrudeLaneIndex - currentExtrudeCount + 1 + i;
                }

                path.ConnectedLaneIndex = connectedLaneIndex;
                path.AutoAttachToTrafficNodes();

                var source = path.Nodes[0].transform.position;
                var target = path.Nodes[2].transform.position;

                if (flipConnection)
                {
                    var temp = source;
                    source = target;
                    target = temp;
                }

                path.Nodes[1].transform.position = GetExtrudeSplinePoint(source, target, true);
                path.CreatePath(false);
            }

            InitializePathHeaders();

            if (roadSegmentCreatorConfig.SelectAfterCreation)
            {
                EditorGUIUtility.PingObject(path.gameObject);
                ForceSelectPath(selectedNode, path);
            }

            EditorSaver.SetObjectDirty(node);

            ResetLaneExtrude();

            EditorExtension.CollapseUndoCurrentOperations();
#endif
        }

        public void AddAutoIgnore()
        {
#if UNITY_EDITOR
            if (sourceIgnoreIndex != targetIgnoreIndex)
            {
                var ignoreIndexes = new Vector2Int(sourceIgnoreIndex, targetIgnoreIndex);

                if (!ignoreConnections.Contains(ignoreIndexes))
                {
                    ignoreConnections.Add(ignoreIndexes);
                }
            }
#endif
        }

        public void RemoveIgnore(Vector2Int ignoreIndex)
        {
#if UNITY_EDITOR
            ignoreConnections.Remove(ignoreIndex);
#endif
        }

        public void ClearAutoPaths()
        {
            List<Path> paths = new List<Path>();
            IterateAllTrafficNodesPath(path => paths.Add(path));

            paths.DestroyGameObjects();

            IterateAllNodes(node => node.ClearEmptyLanes());
        }

        public void CreateAutoCrossroad()
        {
#if UNITY_EDITOR
            ClearAutoPaths();
            CreateAutoPaths(false);
#endif
        }

        public Vector3 GetExtrudeSplinePoint(Vector3 source, Vector3 target, bool includeOffset = false)
        {
#if UNITY_EDITOR
            var p2 = PathAttachHelper.GetSplineCornerPoint(source, target, SourceExtrudeNode.transform.forward, ExtrudeNodeForward);

            if (includeOffset)
            {
                p2 += HandleOffset;
            }

            return p2;
#else
            return default;
#endif
        }

        public Vector3 GetCenterPoint()
        {
            if (createdTrafficNodes.Count >= 2)
            {
                Vector3 point = default;

                for (int i = 0; i < createdTrafficNodes.Count; i++)
                {
                    point += createdTrafficNodes[i].transform.position;
                }

                point /= createdTrafficNodes.Count;

                return point;
            }

            return transform.position;
        }

        private void CreateDefaultSegment()
        {
            Clear();

            if (!trafficLightCrossroad)
            {
                trafficLightCrossroad = GetComponent<TrafficLightCrossroad>();
            }

            UpdateCreateNodeCount();

            CreateCrossroad();

            CreateAutoPaths();

            UpdateCrosswalk();

            TryToAddLights();

            InitializeCrossroad();

            InitializeTrafficNodeHeaders();
            InitializePathHeaders();
            InitCustomSegment();
        }

        private void InitCustomSegment()
        {
            if (!IsCustom())
            {
                return;
            }

            InitializeTempStartParkingPoint();
        }

        private void UpdateCreateNodeCount()
        {
            CreateTrafficNodeCount = GetDirectionCount();
        }

        private void CreateCrossroad()
        {
            int index = 0;

            int createTrafficNodeCount = CreateTrafficNodeCount;

            if (IsCustom())
            {
                createTrafficNodeCount = 2;
            }

            for (int i = 0; i < createTrafficNodeCount; i++)
            {
                CreateTrafficNode(index);

                if (!IsStraightRoad() && !IsCustom())
                {
                    index = (index + 1) % 4;
                }
                else
                {
                    index = (index + 2) % 4;
                }
            }
        }

        private void UpdateCrosswalk()
        {
            for (int i = 0; i < createdTrafficNodes.Count; i++)
            {
                TrafficNode trafficNode = createdTrafficNodes[i];
                var index = trafficNode.TempCreatorLocalIndex;
                var hasCrossWalkValue = GetCrosswalkEnabledState(index);
                var crossWalkNodesIsEnabled = GetPedestrianNodeEnabledState(index);
                trafficNode.TrafficNodeCrosswalk.SwitchConnectionState(hasCrossWalkValue);
                trafficNode.TrafficNodeCrosswalk.SwitchEnabledState(crossWalkNodesIsEnabled);
                trafficNode.TrafficNodeCrosswalk.SetCustomWidth(crosswalkNodeWidth, crosswalkNodeHeight);
                trafficNode.TrafficNodeCrosswalk.CrossWalkOffset = CrossWalkOffset;
                trafficNode.TrafficNodeCrosswalk.SetType(crossWalkNodeShape);
                trafficNode.Resize();
            }

            ProcessCrosswalkConnections();
        }

        public void ProcessCrosswalkConnections()
        {
            ClearCornerNodes();

            if (!IsStraightRoad() && !IsCustom())
            {
                if (addPedestrianNodes)
                {
                    if (pedestrianCornerConnectionType != PedestrianCornerConnectionType.Disabled)
                    {
                        CreateCornerNodes();
                    }

                    TryToCreatePedestrianLoopedConnection();
                }
            }
            else
            {
                if (!IsCustom())
                {
                    ConnectStraightRoadCrosswalks();
                }
            }

            if (IsCustom(false))
            {
                IterateAllNodes(node =>
                {
                    ConnectClosestNode(node, node.GetCrosswalkNode(0), node.GetCrosswalkNode(1));
                    ConnectClosestNode(node, node.GetCrosswalkNode(1), node.GetCrosswalkNode(0));
                });
            }

            CreateOffsetPedestrianNodes();
        }

        private void ConnectClosestNode(TrafficNode sourceNode, PedestrianNode node1, PedestrianNode node2)
        {
            PedestrianNode closestNode = null;
            float minDistance = float.MaxValue;

            IterateAllNodes(connectedNode =>
            {
                if (sourceNode == connectedNode)
                    return;

                var crosswalkNode1 = connectedNode.GetCrosswalkNode(0);
                var crosswalkNode2 = connectedNode.GetCrosswalkNode(1);

                var distance = Vector3.SqrMagnitude(node1.transform.position - crosswalkNode1.transform.position);
                var distance2 = Vector3.SqrMagnitude(node2.transform.position - crosswalkNode1.transform.position);

                if (distance < minDistance && distance < distance2)
                {
                    minDistance = distance;
                    closestNode = crosswalkNode1;
                }

                distance = Vector3.SqrMagnitude(node1.transform.position - crosswalkNode2.transform.position);
                distance2 = Vector3.SqrMagnitude(node2.transform.position - crosswalkNode2.transform.position);

                if (distance < minDistance && distance < distance2)
                {
                    minDistance = distance;
                    closestNode = crosswalkNode2;
                }
            });

            if (closestNode)
            {
                if (connectCrosswalks)
                {
                    node1.AddConnection(closestNode);
                }
                else
                {
                    node1.RemoveConnection(closestNode);
                }
            }
        }

        private List<Path> currentNodeTempPaths;
        private List<Path> currentLaneTempPaths;

        public void CreateAutoPaths(bool templateGeneration = true)
        {
            if (addedConnections == null)
            {
                addedConnections = new Dictionary<Vector2Int, HashSet<Vector2Int>>();
            }

            if (!templateGeneration)
            {
                pathCorner1Offset = default;
                pathCorner2Offset = default;
            }

            addedConnections.Clear();

            for (int currentNodeIndex = 0; currentNodeIndex < CreateTrafficNodeCount; currentNodeIndex++)
            {
                if (IsOneWayRoad(currentNodeIndex) && CreateTrafficNodeCount == 1)
                {
                    if (shouldRevertDirection)
                    {
                        currentNodeIndex += 1;
                    }
                }

                if (!createdTrafficNodes[currentNodeIndex].HasRightLanes)
                    continue;

                var currentLaneCount = createdTrafficNodes[currentNodeIndex].LaneCount;
                var laneArray = new List<LaneArray>(currentLaneCount);

                currentNodeTempPaths = new List<Path>();

                for (int laneIndex = 0; laneIndex < currentLaneCount; laneIndex++)
                {
                    var currentLaneIndex = laneIndex;
                    int tempIndex = -1;

                    currentLaneTempPaths = new List<Path>();

                    while (true)
                    {
                        tempIndex++;

                        if (tempIndex == currentNodeIndex)
                            continue;

                        if (tempIndex >= CreateTrafficNodeCount)
                            break;

                        int nextNodeIndex = -1;

                        if (!templateGeneration)
                        {
                            nextNodeIndex = tempIndex;
                        }

                        var straightPaths = TryToCreateStraightPath(currentNodeIndex, currentLaneIndex, nextNodeIndex);

                        if (straightPaths?.Count > 0)
                        {
                            currentLaneTempPaths.AddRange(straightPaths);
                            currentNodeTempPaths.AddRange(straightPaths);
                            UpdateSourceConnection(currentNodeIndex, straightPaths);
                        }

                        var leftPaths = TryToCreateLeftTurnPath(currentNodeIndex, currentLaneIndex, nextNodeIndex);

                        if (leftPaths?.Count > 0)
                        {
                            currentLaneTempPaths.AddRange(leftPaths);
                            currentNodeTempPaths.AddRange(leftPaths);
                            UpdateSourceConnection(currentNodeIndex, leftPaths);
                        }

                        var rightPaths = TryToCreateRightPath(currentNodeIndex, currentLaneCount, currentLaneIndex, nextNodeIndex);

                        if (rightPaths?.Count > 0)
                        {
                            currentLaneTempPaths.AddRange(rightPaths);
                            currentNodeTempPaths.AddRange(rightPaths);
                            UpdateSourceConnection(currentNodeIndex, rightPaths);
                        }

                        var arcPaths = TryToCreateUTurns(currentNodeIndex, currentLaneCount, currentLaneIndex);

                        if (arcPaths?.Count > 0)
                        {
                            currentLaneTempPaths.AddRange(arcPaths);
                            UpdateSourceConnection(currentNodeIndex, arcPaths);
                        }

                        if (templateGeneration)
                        {
                            break;
                        }
                    }

                    laneArray.Add(new LaneArray()
                    {
                        paths = currentLaneTempPaths
                    });
                }

                createdTrafficNodes[currentNodeIndex].Lanes = laneArray;

                EditorSaver.SetObjectDirty(createdTrafficNodes[currentNodeIndex]);
            }

            if (!templateGeneration || CheckMissing())
            {
                ReconnectMissingLanes();
                ClearSelfIntersection();
            }
        }

        private void ReconnectMissingLanes()
        {
            var data = GetAllPathsConnectedToNodes();

            IterateAllNodes(node =>
            {
                var found = false;

                node.IterateExternalLanes((connectedLaneIndex, external) =>
                {
                    if (!node.HasLeftLanes || data.ContainsKey(node) && data[node].ContainsKey(connectedLaneIndex))
                    {
                        found = true;
                        return;
                    }

                    IterateAllNodes(node2 =>
                    {
                        if (node == node2 || !node2.HasRightLanes) return;

                        found = true;
                        ConnectLane(node2, node, connectedLaneIndex, true);
                    });

                }, false);

                if (!found && node.HasLeftLanes && node.HasRightLanes)
                {
                    node.IterateAllLanes((sourceLaneIndex, external) =>
                    {
                        ConnectLane(node, node, sourceLaneIndex, false);
                    }, false, false);
                }
            });

            IterateAllNodes(node =>
            {
                if (!node.HasRightLanes) return;

                bool found = false;

                node.IterateAllLanes((sourceLaneIndex, external) =>
                {
                    var laneExist = node.LaneExist(sourceLaneIndex, external, true);

                    if (!laneExist)
                    {
                        IterateAllNodes(node2 =>
                        {
                            if (node == node2 || !node2.HasLeftLanes) return;

                            ConnectLane(node, node2, sourceLaneIndex, false);
                            found = true;
                        });
                    }
                    else
                    {
                        found = true;
                    }

                }, false, false);

                if (!found && node.HasLeftLanes)
                {
                    node.IterateAllLanes((sourceLaneIndex, external) =>
                    {
                        ConnectLane(node, node, sourceLaneIndex, false);
                    }, false, false);
                }
            });
        }

        private Dictionary<TrafficNode, Dictionary<int, List<Path>>> GetAllPathsConnectedToNodes()
        {
            var data = new Dictionary<TrafficNode, Dictionary<int, List<Path>>>();

            IterateAllNodes(node =>
            {
                node.IterateAllPaths(path =>
                {
                    var connectedNode = path.ConnectedTrafficNode;

                    if (!connectedNode)
                        return;

                    if (!data.ContainsKey(connectedNode))
                    {
                        data.Add(connectedNode, new Dictionary<int, List<Path>>());
                    }

                    if (!data[connectedNode].ContainsKey(path.ConnectedLaneIndex))
                    {
                        data[connectedNode].Add(path.ConnectedLaneIndex, new List<Path>());
                    }

                    var paths = data[connectedNode][path.ConnectedLaneIndex];
                    paths.TryToAdd(path);
                });
            });

            return data;
        }

        private void ConnectLane(TrafficNode sourceNode, TrafficNode targetNode, int connectedLaneIndex, bool inverse = false)
        {
            var dir = -targetNode.transform.forward;
            var dir2 = (sourceNode.transform.position - targetNode.transform.position).normalized;

            var angle = Vector3.SignedAngle(dir, dir2, Vector3.up);

            const float baseAngle = 30f;

            int index = 0;

            if (angle > baseAngle)
            {
                index = -1 * ProjectConstants.LaneHandDirection;
            }
            else if (angle < -baseAngle)
            {
                index = 1 * ProjectConstants.LaneHandDirection;
            }

            bool arc = false;

            if (sourceNode == targetNode)
            {
                arc = true;
                index = -1;
            }

            Path path = null;

            int sourceLaneIndex = 0;

            switch (index)
            {
                case -1:
                    {
                        sourceLaneIndex = Mathf.Clamp(connectedLaneIndex, 0, sourceNode.GetLaneCount() - 1);

                        if (sourceLaneIndex == -1)
                            return;

                        if (!inverse)
                        {
                            connectedLaneIndex = Mathf.Clamp(connectedLaneIndex, 0, targetNode.GetLaneCount(true) - 1);
                        }

                        path = CreatePath(PathDirectionType.Left, sourceNode, targetNode, !arc ? turnCurveType : TurnCurveType.BezierQuad);

                        SetPathConnection(sourceNode, targetNode, sourceLaneIndex, connectedLaneIndex, path, true, arc);

                        break;
                    }
                case 0:
                    {
                        sourceLaneIndex = Mathf.Clamp(connectedLaneIndex, 0, sourceNode.GetLaneCount() - 1);
                        connectedLaneIndex = Mathf.Clamp(connectedLaneIndex, 0, targetNode.GetLaneCount(true) - 1);

                        if (sourceLaneIndex == -1)
                            return;

                        path = CreatePath(PathDirectionType.Forward, sourceNode, targetNode, turnCurveType);

                        SetPathConnection(sourceNode, targetNode, sourceLaneIndex, connectedLaneIndex, path, false);

                        break;
                    }
                case 1:
                    {
                        if (inverse)
                        {
                            sourceLaneIndex = Mathf.Clamp(sourceNode.GetLaneCount() - targetNode.GetLaneCount(true) + connectedLaneIndex, 0, sourceNode.GetLaneCount() - 1);
                        }
                        else
                        {
                            sourceLaneIndex = Mathf.Clamp(connectedLaneIndex, 0, sourceNode.GetLaneCount() - 1);
                            connectedLaneIndex = Mathf.Clamp(targetNode.GetLaneCount(true) - sourceNode.GetLaneCount() + connectedLaneIndex, 0, targetNode.GetLaneCount(true) - 1);
                        }

                        if (sourceLaneIndex == -1)
                            return;

                        path = CreatePath(PathDirectionType.Right, sourceNode, targetNode, turnCurveType);

                        SetPathConnection(sourceNode, targetNode, sourceLaneIndex, connectedLaneIndex, path);

                        break;
                    }
            }

            if (path != null)
            {
                path.SourceTrafficNode.AddPath(path, sourceLaneIndex);
                path.name += $"_{sourceLaneIndex}";
            }
        }

        private void ClearSelfIntersection()
        {
            if (!cleanupSelfIntersection)
                return;

            var data = GetAllPathsConnectedToNodes();

            IterateAllNodes(node =>
            {
                node.ClearEmptyLanes();

                var list = GetSelfIntersections(node);

                if (list.Count > 0)
                {
                    foreach (var item in list)
                    {
                        if (item.Item1 == null || item.Item2 == null) continue;

                        var path1 = item.Item1;
                        var path2 = item.Item2;

                        if (inverseCleanup)
                        {
                            var temp = path1;
                            path1 = path2;
                            path2 = temp;
                        }

                        var connectedData = data[path1.ConnectedTrafficNode];
                        var laneCount = connectedData[path1.ConnectedLaneIndex].Count;

                        Path destroyPath = null;

                        if (laneCount > 1)
                        {
                            destroyPath = path1;
                        }
                        else
                        {
                            var connectedData2 = data[path2.ConnectedTrafficNode];

                            if (connectedData2.ContainsKey(path2.ConnectedLaneIndex))
                            {
                                var laneCount2 = connectedData2[path2.ConnectedLaneIndex].Count;

                                if (laneCount2 > 1)
                                    destroyPath = path2;
                            }
                        }

                        if (destroyPath)
                        {
                            data[destroyPath.ConnectedTrafficNode][destroyPath.ConnectedLaneIndex].Remove(destroyPath);
                            destroyPath.DestroyPath(false);
                        }
                    }
                }
            });
        }

        private void UpdateSourceConnection(int currentNodeIndex, List<Path> paths)
        {
            for (int i = 0; i < paths.Count; i++)
            {
                paths[i].SourceTrafficNode = createdTrafficNodes[currentNodeIndex];
            }
        }

        public void ChangeOffsetParentRelative(Vector3 targetPos, Quaternion targetRot, bool recordUndo = true)
        {
            var rotationOffset = Quaternion.Inverse(transform.rotation) * targetRot;
            var offset = transform.position - targetPos;

#if UNITY_EDITOR
            if (recordUndo)
            {
                Undo.RegisterCompleteObjectUndo(this, "Undo offset");
            }
#endif

            ChangeOffsetParent(offset, rotationOffset, recordUndo);
        }

        public void ChangeOffsetParent(Vector3 offset, Quaternion rotationOffset, bool recordUndo = true)
        {
            if (offset == Vector3.zero && rotationOffset == Quaternion.identity) return;

#if UNITY_EDITOR

            if (recordUndo)
            {
                Undo.RecordObject(transform, "Undo position");
            }

            List<Transform> transforms = new List<Transform>();
            List<Vector3> positions = new List<Vector3>();
            List<Quaternion> rotations = new List<Quaternion>();

            IterateAllNodes(trafficNode =>
            {
                SaveOffset(trafficNode.transform, recordUndo);
            });

            var pedestrianNodes = cornerNodes.GetComponentsInChildren<PedestrianNode>();

            foreach (var pedestrianNode in pedestrianNodes)
            {
                SaveOffset(pedestrianNode.transform, recordUndo);
            }

            var trafficLightObjects = transform.GetComponentsInChildren<TrafficLightObject>();

            foreach (var trafficLightObject in trafficLightObjects)
            {
                SaveOffset(trafficLightObject.transform, recordUndo);
            }

            for (int i = 0; i < tempCustomPaths?.Count; i++)
            {
                if (tempCustomPaths[i] != null)
                {
                    SaveOffset(tempCustomPaths[i].transform, recordUndo);
                }
            }

            transform.position += -offset;
            transform.rotation *= rotationOffset;

            void SaveOffset(Transform tr, bool recordUndo)
            {
                if (recordUndo)
                {
                    Undo.RegisterCompleteObjectUndo(tr, "Undo position");
                }

                transforms.Add(tr);
                positions.Add(tr.transform.position);
                rotations.Add(tr.transform.rotation);
            }

            for (int i = 0; i < transforms.Count; i++)
            {
                Transform tr = transforms[i];

                tr.position = positions[i];
                tr.rotation = rotations[i];
            }
#endif
        }

        public void ChangeCreatorPosition(Vector3 offset, bool single = true, bool allowMerge = false)
        {
#if UNITY_EDITOR
            Undo.RecordObject(transform, "Position changed");

            if (allowMerge)
            {
                foreach (var crossing in MergedCrossings)
                {
                    var mergedCrossing = crossing.Value;
                    var mergeNode = crossing.Key.MergeNode;

                    if (!mergedCrossing.IsCustomStraight())
                    {
                        mergedCrossing.RecordAllPathUndo(true, true);
                    }
                    else
                    {
                        mergedCrossing.RecordAllOuterPathUndo(mergeNode, true);
                    }

                    crossing.Key.MergeNode.transform.position += offset;

                    if (mergedCrossing.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomSegment)
                    {
                        mergedCrossing.UpdatePaths();
                    }

                    if (mergedCrossing.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomStraightRoad)
                    {
                        mergedCrossing.AttachInnerExternalPaths(mergeNode);
                        mergedCrossing.RecalculateCustomPath();
                    }
                }
            }
#endif

            transform.position += offset;

            DirtyAllPaths();

            if (single)
            {
                TryToSnapSegmentPosition();
                RecalculateConnection();
            }
        }

        public void DirtyAllPaths()
        {
#if UNITY_EDITOR
            IterateAllTrafficNodesPath(path =>
            {
                path.Dirty = true;
            }, true);

            IterateAllOuterPaths(path =>
            {
                path.Dirty = true;
            });
#endif
        }

        public void RecalculateConnection()
        {
#if UNITY_EDITOR
            AttachAllInnerOuterPaths(true);
#endif
        }

        public void ClearNullNodes()
        {
            int index = 0;
            while (index < createdTrafficNodes.Count)
            {
                if (createdTrafficNodes[index] == null)
                {
                    createdTrafficNodes.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            index = 0;

            while (index < trafficLightCrossroad.TrafficNodes.Count)
            {
                if (trafficLightCrossroad.TrafficNodes[index] == null)
                {
                    trafficLightCrossroad.TrafficNodes.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            InitializeTrafficNodeHeaders();
            InitializePathHeaders();
            EditorSaver.SetObjectDirty(this);
        }

        private void TryToAddLights()
        {
            if (!ShouldCreateLight)
            {
                return;
            }

            trafficLightCrossroad.ClearLights(false);

            if (addPedestrianLights)
            {
                CreatePedestrianLights();
            }

            if (addTrafficLights && TrafficLightPlacingSupported)
            {
                CreateTrafficLights();
            }
        }

        private void InitializeCrossroad()
        {
            var hasLights = !IsStraightRoad() && !IsTurnRoad();
            trafficLightCrossroad.HasLights = hasLights;
            trafficLightCrossroad.Initialize(createdTrafficNodes);
        }

        private int GetDirectionCount()
        {
            int currentDirectionCount = 2;

            if (IsCrossRoad())
            {
                currentDirectionCount = Mathf.Clamp(directionCount, MinCrossRoadDirectionCount, MaxCrossRoadDirectionCount);
            }

            return currentDirectionCount;
        }

        private bool GetPedestrianNodeEnabledState(int crossWalkIndex)
        {
            CheckForSizeCollection(customPedestrianNodesData, crossWalkIndex);
            return !CustomCrosswalkSettings(customCrossWalk) ? addPedestrianNodes : addPedestrianNodes && customPedestrianNodesData[crossWalkIndex];
        }

        private Vector3 GetCrosswalkOffset(int crossWalkIndex)
        {
            if (!customCrossWalk)
            {
                return CrossWalkOffset;
            }
            else
            {
                var node = TryToGetNode(crossWalkIndex);

                return node.TrafficNodeCrosswalk.CrossWalkOffset;
            }
        }

        private bool CustomCrosswalkSettings(bool initialValue)
        {
            return (initialValue && roadSegmentType != RoadSegmentType.CustomSegment) || (roadSegmentType == RoadSegmentType.CustomSegment && customTrafficNodeSettings);
        }

        private void Clear(Transform parentToClear)
        {
            while (parentToClear.transform.childCount > 0)
            {
                DestroyImmediate(parentToClear.transform.GetChild(0).gameObject);
            }
        }

        private void ClearList<T>(List<T> list, bool recordUndo = false) where T : Component
        {
            while (list.Count > 0)
            {
                var obj = list[0];

                if (obj != null)
                {
                    if (!recordUndo)
                    {
                        DestroyImmediate(obj.gameObject);
                    }
                    else
                    {
#if UNITY_EDITOR
                        Undo.DestroyObjectImmediate(obj.gameObject);
#endif
                    }
                }

                list.RemoveAt(0);
            }
        }

        private void ClearLights(bool recordUndo = false)
        {
            ClearList(createdLights, recordUndo);
            Clear(trafficLights);
            Clear(pedestrianLights);
            lightBinding.Clear();
        }

        private bool ObjectIsPrefab()
        {
#if UNITY_EDITOR
            var prefabStage = EditorExtension.GetCurrentPrefabStage();

            if (prefabStage == null && PrefabUtility.GetPrefabInstanceHandle(gameObject) != null)
            {
                Debug.Log("Enter prefab mode to edit road or unpack prefab");
                return true;
            }
#endif

            return false;
        }

        private Vector2Int GetSegmentSize(bool evenSizeSnapping)
        {
            return evenSizeSnapping ? Vector2Int.one * 2 : Vector2Int.one;
        }

        private void RecordUndoSegment()
        {
#if UNITY_EDITOR
            if (trafficLightCrossroad != null)
            {
                Undo.RegisterCompleteObjectUndo(trafficLightCrossroad, "Undo Crossroad");
            }

            Undo.RegisterCompleteObjectUndo(this, "Undo Segment");
#endif
        }
    }
}