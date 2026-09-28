using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Npc.Authoring
{
    public class NavAgentConfigAuthoring : RuntimeConfigUpdater<NavAgentConfigReference, NavAgentConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/pedestrianInit.html#navagent-config")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("How often the nav target can be updated")]
        [SerializeField][Range(0f, 10f)] private float updateFrequency = 1f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Distance to nav path node")]
        [SerializeField][Range(0f, 3f)] private float maxDistanceToTargetNode = 0.3f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("If the pedestrian is stuck for more than the collision time, the anti-stuck will be activated")]
        [SerializeField][Range(0f, 2f)] private float maxCollisionTime = 1f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("How often the nav target can be recalculated")]
        [SerializeField][Range(0f, 10f)] private float recalcFrequency = 1f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Distance to obstacle where the nav path is recalculated")]
        [SerializeField][Range(0f, 10f)] private float maxDistanceToObstacle = 10f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Maximum height difference with the obstacle")]
        [SerializeField][Range(0f, 10f)] private float maxHeightDiff = 10f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("If steering target is much further than final target with a given value the target will be reverted")]
        [SerializeField] private bool revertTargetSupport = true;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Distance to steering target logic for target return")]
        [ShowIf(nameof(revertTargetSupport))]
        [SerializeField][Range(0f, 20f)] private float revertSteeringTargetDistance = 15f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Distance to final target logic for target return")]
        [ShowIf(nameof(revertTargetSupport))]
        [SerializeField][Range(0f, 20f)] private float revertEndTargetRemainingDistance = 6f;

        protected override bool UpdateAvailableByDefault => false;

        public override NavAgentConfigReference CreateConfig(BlobAssetReference<NavAgentConfig> blobRef)
        {
            return new NavAgentConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<NavAgentConfig> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<NavAgentConfig>();

                root.UpdateFrequency = updateFrequency;
                root.MaxDistanceToTargetNode = maxDistanceToTargetNode;
                root.MaxCollisionTime = maxCollisionTime;
                root.RecalcFrequency = recalcFrequency;
                root.MaxDistanceToObstacleSQ = maxDistanceToObstacle * maxDistanceToObstacle;
                root.MaxHeightDiff = maxHeightDiff;
                root.RevertTargetSupport = revertTargetSupport;
                root.RevertSteeringTargetDistance = revertSteeringTargetDistance;
                root.RevertEndTargetRemainingDistance = revertEndTargetRemainingDistance;

                return builder.CreateBlobAssetReference<NavAgentConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class NavAgentConfigAuthoringBaker : Baker<NavAgentConfigAuthoring>
        {
            public override void Bake(NavAgentConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.CreateConfig(this));
            }
        }
    }
}