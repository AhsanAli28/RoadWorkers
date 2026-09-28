using Spirit604.DotsCity.Simulation.Car;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.TrafficPublic.Authoring
{
    public class TrafficPublicRoute : TrafficRoute
    {
        [SerializeField] VehicleDataCollection vehicleDataCollection;

        [Tooltip("Maximum number of vehicles on the route")]
        [SerializeField][Range(0, 50)] private int maxVehicleCount = 3;

        [Tooltip("Preferred distance between public transport vehicles")]
        [SerializeField][Range(0, 50)] private float preferedIntervalDistance = 10f;

        [Tooltip("If the camera is ignored, public transport can be spawned in view of the camera")]
        [SerializeField] private bool ignoreCamera;

        [Tooltip("" +
            "<b>Bus</b> : for the default path.\r\n\r\n" +
            "<b>Tram</b> : for the rail path")]
        [SerializeField] private TrafficPublicType trafficPublicType;

        [Tooltip("Car model of the public transport vehicle that will be spawned on the route.")]
        [SerializeField] private int vehicleModel;

        public VehicleDataCollection VehicleDataCollection { get => vehicleDataCollection; }

        public int MaxVehicleCount { get => maxVehicleCount; }

        public float PreferredIntervalDistance { get => preferedIntervalDistance; }

        public bool IgnoreCamera { get => ignoreCamera; }

        public TrafficPublicType TrafficPublicType { get => trafficPublicType; }

        public int VehicleModel { get => vehicleModel; }
    }
}