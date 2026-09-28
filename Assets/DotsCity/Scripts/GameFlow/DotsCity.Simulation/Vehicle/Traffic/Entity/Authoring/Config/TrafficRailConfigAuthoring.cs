using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public class TrafficRailConfigAuthoring : SyncConfigBase
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficCarConfigs.html#traffic-rail-config")]
        [SerializeField] private string link;

        [Header("Rail Settings")]

        [OnValueChanged(nameof(Sync))]
        [Tooltip("Maximum distance between the rail and the vehicle")]
        [SerializeField][Range(0.005f, 2f)] private float maxDistanceToRailLine = 0.06f;

        [OnValueChanged(nameof(Sync))]
        [Tooltip("Lateral speed of the vehicle to align with the rail")]
        [SerializeField][Range(0.01f, 4f)] private float lateralSpeed = 0.4f;

        [OnValueChanged(nameof(Sync))]
        [Tooltip("Rotation lerp speed")]
        [SerializeField][Range(0.1f, 20f)] private float rotationLerpSpeed = 1f;

        [OnValueChanged(nameof(Sync))]
        [Tooltip("On/off rotating lerp for default traffic")]
        [SerializeField] private bool lerpRotationTraffic = true;

        [Header("Train Settings")]

        [OnValueChanged(nameof(Sync))]
        [Tooltip("On/off rotating lerp for train")]
        [SerializeField] private bool lerpRotationTram = true;

        [OnValueChanged(nameof(Sync))]
        [Tooltip("Rotation lerp speed for train")]
        [SerializeField][Range(0.1f, 20f)] private float trainRotationLerpSpeed = 10f;

        [OnValueChanged(nameof(Sync))]
        [Tooltip("Relative speed of deceleration/acceleration of the wagon when approaching/departing from the connected wagon")]
        [SerializeField][Range(0f, 5f)] private float convergenceSpeedRate = 0.2f;

        [OnValueChanged(nameof(Sync))]
        [SerializeField][Range(0f, 30f)] private float acceleration = 5f;

        [OnValueChanged(nameof(Sync))]
        [SerializeField][Range(0f, 100f)] private float brakePower = 3f;

        class TrafficRailConfigAuthoringBaker : Baker<TrafficRailConfigAuthoring>
        {
            public override void Bake(TrafficRailConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<TrafficRailConfig>();

                    root.MaxDistanceToRailLine = authoring.maxDistanceToRailLine;
                    root.LateralSpeed = authoring.lateralSpeed;
                    root.RotationLerpSpeed = authoring.rotationLerpSpeed;
                    root.TrainRotationLerpSpeed = authoring.trainRotationLerpSpeed;
                    root.LerpTram = authoring.lerpRotationTram;
                    root.LerpTraffic = authoring.lerpRotationTraffic;
                    root.ConvergenceSpeedRate.x = Mathf.Clamp(1 - authoring.convergenceSpeedRate, 0, float.MaxValue);
                    root.ConvergenceSpeedRate.y = Mathf.Clamp(1 + authoring.convergenceSpeedRate, 0, float.MaxValue);
                    root.Acceleration = authoring.acceleration;
                    root.BrakePower = authoring.brakePower;

                    var blobRef = builder.CreateBlobAssetReference<TrafficRailConfig>(Unity.Collections.Allocator.Persistent);

                    AddBlobAsset(ref blobRef, out var hash);

                    AddComponent(entity, new TrafficRailConfigReference() { Config = blobRef });
                }
            }
        }
    }
}