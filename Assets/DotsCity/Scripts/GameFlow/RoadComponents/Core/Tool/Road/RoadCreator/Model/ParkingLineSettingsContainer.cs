using Spirit604.Gameplay.Road;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public class ParkingLineSettingsContainer : ScriptableObject
    {
        [SerializeField]
        private ParkingLineSettings parkingLineSettings = new ParkingLineSettings();

        public TrafficNodeType PlaceTrafficNodeType { get => parkingLineSettings.PlaceTrafficNodeType; set => parkingLineSettings.PlaceTrafficNodeType = value; }
        public float ParkingTrafficNodeWeight { get => parkingLineSettings.ParkingTrafficNodeWeight; set => parkingLineSettings.ParkingTrafficNodeWeight = value; }
        public float ParkingTrafficNodeCustomAchieveDistance { get => parkingLineSettings.NodeCustomAchieveDistance; set => parkingLineSettings.NodeCustomAchieveDistance = value; }
        public int PlaceCount { get => parkingLineSettings.PlaceCount; set => parkingLineSettings.PlaceCount = value; }
        public Vector3 PlaceSize { get => parkingLineSettings.PlaceSize; set => parkingLineSettings.PlaceSize = value; }
        public float ParkingPlaceStartOffset { get => parkingLineSettings.ParkingPlaceStartOffset; set => parkingLineSettings.ParkingPlaceStartOffset = value; }
        public float ParkingPlaceSpacingOffset { get => parkingLineSettings.ParkingPlaceSpacingOffset; set => parkingLineSettings.ParkingPlaceSpacingOffset = value; }
        public Vector3 LineStartPointLocal { get => parkingLineSettings.LineStartPointLocal; set => parkingLineSettings.LineStartPointLocal = value; }
        public Vector3 LineDirection { get => parkingLineSettings.LineDirection; set => parkingLineSettings.LineDirection = value; }
        public Vector3 NodeDirection { get => parkingLineSettings.NodeDirection != Vector3.zero ? parkingLineSettings.NodeDirection : Vector3.forward; set => parkingLineSettings.NodeDirection = value; }
        public bool AddParkingPedestrianNodes { get => parkingLineSettings.AddParkingPedestrianNodes; set => parkingLineSettings.AddParkingPedestrianNodes = value; }
        public PedestrianNodeType ParkingPedestrianNodeType { get => parkingLineSettings.ParkingPedestrianNodeType; set => parkingLineSettings.ParkingPedestrianNodeType = value; }
        public bool AutoConnectNodes { get => parkingLineSettings.AutoConnectNodes; set => parkingLineSettings.AutoConnectNodes = value; }
        public float ParkingPedestrianNodeWeight { get => parkingLineSettings.ParkingPedestrianNodeWeight; set => parkingLineSettings.ParkingPedestrianNodeWeight = value; }
        public Vector3 ParkingNodeOffset { get => parkingLineSettings.ParkingNodeOffset; set => parkingLineSettings.ParkingNodeOffset = value; }
        public Vector3 ParkingEnterNodeOffset { get => parkingLineSettings.ParkingEnterNodeOffset; set => parkingLineSettings.ParkingEnterNodeOffset = value; }
        public float InitialPathSpeedLimit { get => parkingLineSettings.InitialPathSpeedLimit; set => parkingLineSettings.InitialPathSpeedLimit = value; }
        public int NodeCloneCount { get => parkingLineSettings.NodeCloneCount; set => parkingLineSettings.NodeCloneCount = value; }
        public int NodeSkipLastCount { get => parkingLineSettings.NodeSkipLastCount; set => parkingLineSettings.NodeSkipLastCount = value; }
        public ParkingLineSettings ParkingLineSettings { get => parkingLineSettings; }

        public void InstallSettings(ParkingLineSettings newParkingLineSettings)
        {
            parkingLineSettings = newParkingLineSettings.Clone() as ParkingLineSettings;
        }

        public ParkingLineSettings GetSettingsClone() => parkingLineSettings.Clone() as ParkingLineSettings;
    }
}