using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    public class PedestrianObstacleLocalAvoidanceSettingsAuthoring : RuntimeConfigUpdater<ObstacleAvoidanceSettingsReference, ObstacleAvoidanceSettingsData>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/pedestrianInit.html#obstacle-local-avoidance-config")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField] private ObstacleAvoidanceMethod obstacleAvoidanceMethod = ObstacleAvoidanceMethod.FindNeighbors;

        [Tooltip("Maximum surface tilt angle at which the avoidance is calculated")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0, 15f)] private float maxSurfaceAngle = 5f;

        [Tooltip("Offset between an obstacle and avoidance waypoints.")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0, 5f)] private float obstacleAvoidanceOffset = 1;

        [Tooltip("Distance to achieve the avoidance waypoint")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0, 3f)] private float achieveDistance = 0.5f;

        [Tooltip("Check if destination can be reached, if not and can't be found new, destination returns.")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField] private bool checkTargetAvailability;

        [Tooltip("Number of attempts to find new destination if destination not available")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [ShowIf(nameof(checkTargetAvailability))]
        [SerializeField][Range(1, 30)] private int searchNewTargetAttemptCount = 10;

        protected override bool UpdateAvailableByDefault => false;

        public override ObstacleAvoidanceSettingsReference CreateConfig(BlobAssetReference<ObstacleAvoidanceSettingsData> blobRef)
        {
            return new ObstacleAvoidanceSettingsReference() { SettingsReference = blobRef };
        }

        protected override BlobAssetReference<ObstacleAvoidanceSettingsData> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<ObstacleAvoidanceSettingsData>();

                root.ObstacleAvoidanceMethod = obstacleAvoidanceMethod;
                root.MaxSurfaceAngle = maxSurfaceAngle;
                root.ObstacleAvoidanceOffset = obstacleAvoidanceOffset;
                root.AchieveDistanceSQ = achieveDistance * achieveDistance;
                root.CheckTargetAvailability = checkTargetAvailability;
                root.SearchNewTargetAttemptCount = searchNewTargetAttemptCount;

                return builder.CreateBlobAssetReference<ObstacleAvoidanceSettingsData>(Unity.Collections.Allocator.Persistent);
            }
        }

        class PedestrianObstacleLocalAvoidanceSettingsAuthoringBaker : Baker<PedestrianObstacleLocalAvoidanceSettingsAuthoring>
        {
            public override void Bake(PedestrianObstacleLocalAvoidanceSettingsAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.CreateConfig(this));
            }
        }
    }
}