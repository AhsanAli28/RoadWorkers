using Spirit604.Gameplay.Road;
using System;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    [System.Serializable]
    public class ParkingLineSettings : ICloneable
    {
        [Tooltip("" +
            "<b>Parking</b> : for parking nodes\r\n\r\n" +
            "<b>Traffic public stop</b> : for public transport")]
        [SerializeField] private TrafficNodeType placeTrafficNodeType = TrafficNodeType.Parking;

        [Tooltip("Group of the vehicles that allowed on the parking")]
        [SerializeField] private TrafficGroupMask trafficGroupMask = new TrafficGroupMask();

        [Tooltip("Weight of the TrafficNode for route selection by traffic")]
        [SerializeField][Range(0, 1f)] private float parkingTrafficNodeWeight = 1f;

        [Tooltip("Custom distance to achieve a node (if 0 value default value will be taken).")]
        [SerializeField][Range(0, 5f)] private float nodeCustomAchieveDistance = 0f;

        [SerializeField][Range(0, 40)] private int placeCount = 1;

        [SerializeField] private Vector3 placeSize = Vector3.one;

        [SerializeField] private float parkingPlaceStartOffset;

        [Tooltip("Distance between parking slots")]
        [SerializeField][Range(0, 10f)] private float parkingPlaceSpacingOffset;

        [SerializeField] private RoadSegmentCreator.HandleType parkingLineHandleType = RoadSegmentCreator.HandleType.Position;

        [SerializeField] private RoadSegmentCreator.LineHandleObjectType lineHandleObjectType = RoadSegmentCreator.LineHandleObjectType.ParkingLine;

        [SerializeField] private RoadSegmentCreator.ParkingPositionSnapType positionSnapType = RoadSegmentCreator.ParkingPositionSnapType.Disabled;

        [SerializeField] private RoadSegmentCreator.ParkingRotationSnapType rotationSnapType = RoadSegmentCreator.ParkingRotationSnapType.RightCorner;

        [Tooltip("" +
            "<b>None</b> : Rail movement is disabled\r\n\r\n" +
            "<b>Enter Only</b> : the vehicles entering the car park have a Rail Movement\r\n\r\n" +
            "<b>Exit Only</b> : the vehicles entering the car park have a Rail Movement\r\n\r\n" +
            "<b>Enter And Exit</b> : enter & exit paths have a Rail Movement")]
        [SerializeField] private RoadSegmentCreator.ParkingPathRailType railType = RoadSegmentCreator.ParkingPathRailType.None;

        [SerializeField] private Vector3 snapOffset;

        [SerializeField][Range(0, 10f)] private float positionSnap = 1;

        [SerializeField][Range(1, 90)] private int rotationSnapAngle = 10;

        [Tooltip("Local direction of the TrafficNode in the parking place")]
        [SerializeField] private Vector3 lineStartPointLocal;

        [Tooltip("Local direction of the parking line")]
        [SerializeField] private Vector3 lineDirection = new Vector3(0, 0, 1);

        [Tooltip("Local direction of the TrafficNode in the parking place")]
        [SerializeField] private Vector3 nodeDirection = new Vector3(0, 0, 1);

        [Tooltip("Add an entry parking node and a node linking it")]
        [SerializeField] private bool addParkingPedestrianNodes = true;

        [Tooltip("" +
            "<b>Car parking</b> : for parking nodes\r\n\r\n" +
            "<b>Traffic public stop station</b> : for public transport")]
        [SerializeField] private PedestrianNodeType parkingPedestrianNodeType = PedestrianNodeType.CarParking;

        [Tooltip("Auto connect created entry parking node and nearby created node")]
        [SerializeField] private bool autoConnectNodes = true;

        [Tooltip("Weight of the PedestrianNode for route selection by pedestrians")]
        [SerializeField][Range(0, 1f)] private float parkingPedestrianNodeWeight = 0.05f;

        [Tooltip("Entry parking node offset relative to traffic nodes")]
        [SerializeField] private Vector3 parkingNodeOffset;

        [Tooltip("Node that connected to entry parking node relative to traffic nodes")]
        [SerializeField] private Vector3 parkingEnterNodeOffset;

        [SerializeField][Range(0, 100f)] private float initialPathSpeedLimit;

        [Tooltip("Number of nodes in the next paths that are will clone position from source path")]
        [SerializeField][Range(0, 5)] private int nodeCloneCount = 1;

        [Tooltip("Number of last nodes in the next paths that are will clone position the last nodes from source path")]
        [SerializeField][Range(0, 5)] private int nodeSkipLastCount = 0;

        public TrafficNodeType PlaceTrafficNodeType { get => placeTrafficNodeType; set => placeTrafficNodeType = value; }
        public TrafficGroupMask TrafficGroupMask { get => trafficGroupMask; set => trafficGroupMask = value; }
        public float ParkingTrafficNodeWeight { get => parkingTrafficNodeWeight; set => parkingTrafficNodeWeight = value; }
        public float NodeCustomAchieveDistance { get => nodeCustomAchieveDistance; set => nodeCustomAchieveDistance = value; }
        public int PlaceCount { get => placeCount; set => placeCount = value; }
        public Vector3 PlaceSize { get => placeSize; set => placeSize = value; }
        public float ParkingPlaceStartOffset { get => parkingPlaceStartOffset; set => parkingPlaceStartOffset = value; }
        public float ParkingPlaceSpacingOffset { get => parkingPlaceSpacingOffset; set => parkingPlaceSpacingOffset = value; }
        public RoadSegmentCreator.HandleType ParkingLineHandleType { get => parkingLineHandleType; set => parkingLineHandleType = value; }
        public RoadSegmentCreator.LineHandleObjectType LineHandleObjectType { get => lineHandleObjectType; set => lineHandleObjectType = value; }
        public RoadSegmentCreator.ParkingPositionSnapType PositionSnapType { get => positionSnapType; set => positionSnapType = value; }
        public RoadSegmentCreator.ParkingRotationSnapType RotationSnapType { get => rotationSnapType; set => rotationSnapType = value; }
        public RoadSegmentCreator.ParkingPathRailType RailType { get => railType; set => railType = value; }
        public Vector3 SnapOffset { get => snapOffset; set => snapOffset = value; }
        public float PositionSnap { get => positionSnap; set => positionSnap = value; }
        public int RotationSnapAngle { get => rotationSnapAngle; set => rotationSnapAngle = value; }
        public Vector3 LineStartPointLocal { get => lineStartPointLocal; set => lineStartPointLocal = value; }
        public Vector3 LineDirection { get => lineDirection; set => lineDirection = value; }
        public Vector3 NodeDirection { get => nodeDirection; set => nodeDirection = value; }
        public bool AddParkingPedestrianNodes { get => addParkingPedestrianNodes; set => addParkingPedestrianNodes = value; }
        public PedestrianNodeType ParkingPedestrianNodeType { get => parkingPedestrianNodeType; set => parkingPedestrianNodeType = value; }
        public bool AutoConnectNodes { get => autoConnectNodes; set => autoConnectNodes = value; }
        public float ParkingPedestrianNodeWeight { get => parkingPedestrianNodeWeight; set => parkingPedestrianNodeWeight = value; }
        public Vector3 ParkingNodeOffset { get => parkingNodeOffset; set => parkingNodeOffset = value; }
        public Vector3 ParkingEnterNodeOffset { get => parkingEnterNodeOffset; set => parkingEnterNodeOffset = value; }
        public float InitialPathSpeedLimit { get => initialPathSpeedLimit; set => initialPathSpeedLimit = value; }
        public int NodeCloneCount { get => nodeCloneCount; set => nodeCloneCount = value; }
        public int NodeSkipLastCount { get => nodeSkipLastCount; set => nodeSkipLastCount = value; }

        public bool IsRail(bool exitPath) => IsRail(!exitPath ? 0 : 1);

        public bool IsRail(int index) => index == 0 && RailType.HasFlag(RoadSegmentCreator.ParkingPathRailType.EnterOnly) || index == 1 && RailType.HasFlag(RoadSegmentCreator.ParkingPathRailType.ExitOnly);

        public object Clone()
        {
            var settings = this.MemberwiseClone() as ParkingLineSettings;
            settings.TrafficGroupMask = this.TrafficGroupMask.GetClone();
            return settings;
        }
    }
}