using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Sound.Authoring
{
    public class TrafficCollisionConfigAuthoring : SyncConfigBase
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficCarConfigs.html#traffic-collision-config")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(Sync))]
        [ShowIf(nameof(DotsSimulation))]
        [Tooltip("Idling time after collision with other vehicle")]
        [SerializeField][Range(0f, 20f)] private float idleDuration = 3f;

        [OnValueChanged(nameof(Sync))]
        [Tooltip("Attempt to avoid a stuck collision vehicle")]
        [SerializeField] private bool avoidStuckedCollision = true;

        [OnValueChanged(nameof(Sync))]
        [ShowIf(nameof(AvoidStuckedCollisionDots))]
        [Tooltip("Collision time with vehicle to start avoidance")]
        [SerializeField][Range(0f, 20f)] private float collisionDuration = 1f;

        [OnValueChanged(nameof(Sync))]
        [ShowIf(nameof(AvoidStuckedCollisionDots))]
        [Tooltip("Duration of collision ignoring since last event")]
        [SerializeField][Range(0f, 20f)] private float ignoreCollisionDuration = 2f;

        [OnValueChanged(nameof(Sync))]
        [ShowIf(nameof(AvoidStuckedCollisionDots))]
        [Tooltip("Calculation frequency of the avoidance")]
        [SerializeField][Range(0f, 20f)] private float calculationCollisionFrequency = 1f;

        [OnValueChanged(nameof(Sync))]
        [ShowIf(nameof(AvoidStuckedCollisionDots))]
        [Tooltip("Frequency of collision avoidance attempts")]
        [SerializeField][Range(0f, 30f)] private float repeatAvoidanceFrequency = 5f;

        [OnValueChanged(nameof(Sync))]
        [ShowIf(nameof(AvoidStuckedCollisionDots))]
        [Tooltip("Dot value of the direction of the colliding vehicles along the Z axis")]
        [SerializeField][Range(0f, 1f)] private float forwardDirectionValue = 0.6f;

        [OnValueChanged(nameof(Sync))]
        [ShowIf(nameof(AvoidStuckedCollisionDots))]
        [Tooltip("Dot value of the direction of the colliding vehicles along the X axis")]
        [SerializeField][Range(0f, 1f)] private float sideDirectionValue = 0.8f;

        [OnValueChanged(nameof(Sync))]
        [ShowIf(nameof(AvoidStuckedCollisionMono))]
        [Tooltip("Distance after which the timer is reset")]
        [SerializeField][Range(0f, 5f)] private float stuckDistance = 1f;

        [OnValueChanged(nameof(Sync))]
        [ShowIf(nameof(AvoidStuckedCollisionMono))]
        [SerializeField][Range(0f, 10f)] private float stuckDuration = 2f;

        [OnValueChanged(nameof(Sync))]
        [ShowIf(nameof(AvoidStuckedCollisionMono))]
        [Tooltip("Delay after end of avoidance if car can do collision check again")]
        [SerializeField][Range(0f, 10f)] private float postActivationDelay = 3f;

        [OnValueChanged(nameof(Sync))]
        [ShowIf(nameof(avoidStuckedCollision))]
        [Tooltip("How far back to drive")]
        [SerializeField][Range(0f, 5f)] private float avoidanceDistance = 1f;

        [OnValueChanged(nameof(Sync))]
        [ShowIf(nameof(AvoidStuckedCollisionMono))]
        [SerializeField][Range(0f, 10f)] private float reverseDriveMaxDuration = 2f;

        private CitySettingsInitializerBase CitySettingsInitializer => CitySettingsInitializerBase.EditorInstance;
        private bool HasSettings => CitySettingsInitializer != null;
        private bool DotsSimulation => HasSettings ? CitySettingsInitializer.GetSettings<GeneralSettingDataCore>().DOTSSimulation : true;
        private bool AvoidStuckedCollisionDots => avoidStuckedCollision && DotsSimulation || !HasSettings;
        private bool AvoidStuckedCollisionMono => avoidStuckedCollision && !DotsSimulation || !HasSettings;

        class TrafficCollisionConfigAuthoringBaker : Baker<TrafficCollisionConfigAuthoring>
        {
            public override void Bake(TrafficCollisionConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<TrafficCollisionConfig>();

                    root.IdleDuration = authoring.idleDuration;
                    root.AvoidStuckedCollision = authoring.avoidStuckedCollision;
                    root.CollisionDuration = authoring.collisionDuration;
                    root.IgnoreCollisionDuration = authoring.ignoreCollisionDuration;
                    root.CalculationCollisionFrequency = authoring.calculationCollisionFrequency;
                    root.RepeatAvoidanceFrequency = authoring.repeatAvoidanceFrequency;
                    root.ForwardDirectionValue = authoring.forwardDirectionValue;
                    root.SideDirectionValue = authoring.sideDirectionValue;

                    root.StuckDistance = authoring.stuckDistance;
                    root.StuckDuration = authoring.stuckDuration;
                    root.PostActivationDelay = authoring.postActivationDelay;
                    root.AvoidanceDistance = authoring.avoidanceDistance;
                    root.ReverseDriveMaxDuration = authoring.reverseDriveMaxDuration;

                    var blobRef = builder.CreateBlobAssetReference<TrafficCollisionConfig>(Unity.Collections.Allocator.Persistent);

                    AddBlobAsset(ref blobRef, out var hash);

                    AddComponent(entity, new TrafficCollisionConfigReference() { Config = blobRef });
                }
            }
        }
    }
}