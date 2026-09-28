using Spirit604.Attributes;
using Spirit604.CityEditor;
using Spirit604.DotsCity.Core;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Config
{
    [CreateAssetMenu(fileName = "GeneralSettingDataSimulation", menuName = CityEditorBookmarks.CITY_EDITOR_ROOT_PATH + "Game Data/GeneralSettingData Simulation")]
    public class GeneralSettingDataSimulation : GeneralSettingDataCore
    {
        #region Car variables

        [Tooltip("On/off visual hit feature for traffic vehicles by bullets")]
        [SerializeField] private bool carVisualDamageSystemSupport = true;

        #endregion

        #region Traffic variables

        [Tooltip("On/off traffic vehicle in the city")]
        [SerializeField] private bool hasTraffic = true;

        [HideInView]
        [SerializeField] private EntityBakingType trafficBakingType = EntityBakingType.Runtime;

        [ShowIf(nameof(hasTraffic))]
        [Tooltip("On/off feature to change lanes for traffic")]
        [SerializeField] private bool changeLaneSupport = true;

        [ShowIf(nameof(hasTraffic))]
        [Tooltip("On/off public traffic vehicle in the city")]
        [SerializeField] private bool trafficPublicSupport = true;

        [ShowIf(nameof(hasTraffic))]
        [Tooltip("On/off built-in train systems")]
        [SerializeField] private bool trainSupport = true;

        [ShowIf(nameof(hasTraffic))]
        [Tooltip("On/off systems for parking vehicles in the city")]
        [SerializeField] private bool trafficParkingSupport = true;

        [ShowIf(nameof(hasTraffic))]
        [Tooltip("On/off antistuck feature for vehicles")]
        [SerializeField] private bool antiStuckSupport = true;

        [ShowIf(nameof(hasTraffic))]
        [Tooltip("On/off avoidance of the vehicles")]
        [SerializeField] private bool avoidanceSupport = true;

        [ShowIf(nameof(DotsTraffic))]
        [Tooltip("On/off rail movement for traffic on rail paths")]
        [SerializeField] private bool railMovementSupport = true;

        [ShowIf(nameof(DotsTraffic))]
        [Tooltip("On/off traffic collision reaction to other traffic cars")]
        [SerializeField] private bool carHitCollisionReaction = true;

        [ShowIf(nameof(DotsTraffic))]
        [Tooltip("On/off simple wheel system for traffic vehicles")]
        [SerializeField] private bool wheelSystemSupport = true;

        [HideIf(nameof(DotsTraffic))]
        [SerializeField] private bool vehicleLightSupport = true;

        #endregion

        #region Pedestrian variables

        [Tooltip("On/off pedestrians in the city")]
        [SerializeField] private bool hasPedestrian = true;

        [HideInView]
        [SerializeField] private EntityBakingType pedestrianBakingType = EntityBakingType.Runtime;

        [ShowIf(nameof(hasPedestrian))]
        [Tooltip("On/off trigger feature for pedestrians (fear running due bullets etc...)")]
        [SerializeField] private bool pedestrianTriggerSystemSupport = true;

        [Tooltip("On/off navigation systems for pedestrians")]
        [SerializeField] private bool navigationSupport = true;

        [Tooltip("On/off talking systems for pedestrians")]
        [SerializeField] private bool talkingSupport = true;

        [Tooltip("On/off bench systems for pedestrians")]
        [SerializeField] private bool benchSystemSupport = true;
        #endregion

        #region Other

        [ShowIf(nameof(DOTSSimulation))]
        [Tooltip("On/off physics for props")]
        [SerializeField] private bool propsPhysics = true;

        [ShowIf(nameof(DOTSSimulation))]
        [Tooltip("On/off damage systems for props")]
        [SerializeField] private bool propsDamageSystemSupport = true;

        [ShowIf(nameof(DOTSSimulation))]
        [Tooltip("On/off health systems for all entities (vehicles, pedestrians, etc...)")]
        [SerializeField] private bool healthSystemSupport = true;

        #endregion

        public bool HasTraffic { get => hasTraffic; }
        public EntityBakingType TrafficBakingType { get => trafficBakingType; }
        public bool CarVisualDamageSystemSupport { get => carVisualDamageSystemSupport; }
        public bool ChangeLaneSupport { get => changeLaneSupport; set => changeLaneSupport = value; }
        public bool TrafficPublicSupport { get => trafficPublicSupport; }
        public bool TrainSupport { get => trainSupport; }
        public bool TrafficParkingSupport { get => trafficParkingSupport; }
        public bool AntiStuckSupport { get => antiStuckSupport; }
        public bool AvoidanceSupport { get => avoidanceSupport; }
        public bool RailMovementSupport { get => railMovementSupport && DOTSSimulation; }
        public bool CarHitCollisionReaction { get => carHitCollisionReaction; set => carHitCollisionReaction = value; }
        public bool WheelSystemSupport { get => wheelSystemSupport; set => wheelSystemSupport = value; }
        public bool VehicleLightSupport { get => vehicleLightSupport; set => vehicleLightSupport = value; }

        public bool HasPedestrian { get => hasPedestrian; }
        public EntityBakingType PedestrianBakingType { get => pedestrianBakingType; }
        public bool PedestrianTriggerSystemSupport => pedestrianTriggerSystemSupport && HasPedestrian;
        public bool NavigationSupport { get => navigationSupport; set => navigationSupport = value; }
        public bool TalkingSupport { get => talkingSupport; set => talkingSupport = value; }
        public bool BenchSystemSupport { get => benchSystemSupport; set => benchSystemSupport = value; }

        public virtual bool BulletSupport => false;
        public bool PropsPhysics { get => propsPhysics; set => propsPhysics = value; }
        public bool PropsDamageSystemSupport { get => propsDamageSystemSupport && DOTSSimulation; set => propsDamageSystemSupport = value; }
        public bool HealthSystemSupport { get => healthSystemSupport; set => healthSystemSupport = value; }

        private bool DotsTraffic => hasTraffic && DOTSSimulation;
    }
}