using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public class TrafficParkingConfigAuthoring : RuntimeConfigUpdater<TrafficParkingConfigReference, TrafficParkingConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficCarConfigs.html#traffic-parking-config")]
        [SerializeField] private string link;

        [ShowIfNull]
        [SerializeField] private CitySettingsInitializerBase citySettingsInitializer;

        [GeneralOption("trafficParkingSupport")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("" +
            "<b>WaitForPedestrian</b> : the car will only start moving when the pedestrian gets in\r\n\r\n" +
            "<b>TempIdleAndStart</b> : the car starts moving after the set idle time")]
        [SerializeField] private ParkingIdleType parkingIdleType = ParkingIdleType.WaitForPedestrian;

        [ShowIf(nameof(Idle))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][MinMaxSlider(0, 100)] private Vector2 idleDuration = new Vector2(10, 15f);

        [ShowIf(nameof(PreciseAligmentAtNodeAvailable))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("On/off precise positioning of the car's parking space")]
        [SerializeField] private bool preciseAligmentAtNode = true;

        [ShowIf(nameof(PreciseAligmentAtNode))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Rotating speed")]
        [SerializeField][Range(0.1f, 25f)] private float rotationSpeed = 3f;

        [ShowIf(nameof(PreciseAligmentAtNode))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Angle at which the rotation is complete")]
        [SerializeField][Range(0.1f, 25f)] private float completeAngle = 2f;

        [ShowIf(nameof(PreciseAligmentAtNode))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("On/Off minor driving correction speed to parking point")]
        [SerializeField] private bool precisePosition = true;

        [ShowIf(nameof(PrecisePosition))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Movement speed to the parking point")]
        [SerializeField][Range(0.1f, 25f)] private float movementSpeed = 1f;

        [ShowIf(nameof(PrecisePosition))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Achieve distance at which the movement is complete")]
        [SerializeField][Range(0.1f, 25f)] private float achieveDistance = 0.2f;

        private bool Idle => parkingIdleType == ParkingIdleType.TempIdleAndStart;
        private bool PreciseAligmentAtNodeAvailable => DotsSimulation;
        private bool PreciseAligmentAtNode => preciseAligmentAtNode && PreciseAligmentAtNodeAvailable;
        private bool PrecisePosition => PreciseAligmentAtNode && precisePosition;
        private bool DotsSimulation => CitySettingsInitializerBase.DotsSimulationStatic;

        protected override bool UpdateAvailableByDefault => false;

        public override TrafficParkingConfigReference CreateConfig(BlobAssetReference<TrafficParkingConfig> blobRef)
        {
            return new TrafficParkingConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<TrafficParkingConfig> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<TrafficParkingConfig>();

                if (!citySettingsInitializer || citySettingsInitializer.DOTSSimulation)
                    root.AligmentAtNode = preciseAligmentAtNode;

                root.ParkingIdleType = parkingIdleType;
                root.IdleDuration = idleDuration;
                root.RotationSpeed = rotationSpeed;
                root.CompleteAngle = completeAngle;
                root.PrecisePosition = precisePosition;
                root.MovementSpeed = movementSpeed;
                root.AchieveDistanceSQ = achieveDistance * achieveDistance;

                return builder.CreateBlobAssetReference<TrafficParkingConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class TrafficParkingConfigAuthoringBaker : Baker<TrafficParkingConfigAuthoring>
        {
            public override void Bake(TrafficParkingConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.CreateConfig(this));
            }
        }
    }
}