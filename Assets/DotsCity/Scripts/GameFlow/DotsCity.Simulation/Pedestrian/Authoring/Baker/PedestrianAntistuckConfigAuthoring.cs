using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    public class PedestrianAntistuckConfigAuthoring : RuntimeConfigUpdater<AntistuckConfigReference, AntistuckConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/pedestrianInit.html#antistuck-config")]
        [SerializeField] private string link;

        [Tooltip("On/off anti-stuck feature (if disabled previous target will be selected)")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField] private bool antistuckEnabled = true;

        [Tooltip("Dot direction between the pedestrian's forward and the anti-stuck point")]
        [ShowIf(nameof(antistuckEnabled))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0, 1f)] private float targetDirectionDot = 0.95f;

        [Tooltip("Achieve distance to the antistuck target point")]
        [ShowIf(nameof(antistuckEnabled))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0, 4f)] private float achieveDistance = 0.4f;

        [Tooltip("Distance between collision and anti-stuck point")]
        [ShowIf(nameof(antistuckEnabled))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0, 5f)] private float targetPointOffset = 1.5f;

        protected override bool UpdateAvailableByDefault => false;

        public override AntistuckConfigReference CreateConfig(BlobAssetReference<AntistuckConfig> blobRef)
        {
            return new AntistuckConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<AntistuckConfig> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<AntistuckConfig>();

                root.AntistuckEnabled = antistuckEnabled;
                root.TargetDirectionDot = targetDirectionDot;
                root.AchieveDistanceSQ = achieveDistance * achieveDistance;
                root.TargetPointOffset = targetPointOffset;

                return builder.CreateBlobAssetReference<AntistuckConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class PedestrianAntistuckConfigAuthoringBaker : Baker<PedestrianAntistuckConfigAuthoring>
        {
            public override void Bake(PedestrianAntistuckConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.CreateConfig(this));
            }
        }
    }
}