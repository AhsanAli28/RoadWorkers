using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    public class CarExplodeConfigAuthoring : RuntimeConfigUpdater<CarExplodeConfigReference, CarExplodeConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/carCommonConfigs.html")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0.1f, 100000f)] private float initialYForce = 20000f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0.1f, 100000f)] private float initialForwardForce = 1000f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0f, 100f)] private float velocityMultiplier = 10f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(-5f, 5f)] private float applyForceOffset = -1.6f;

        [SerializeField][Range(0.1f, 5000f)] private float sourceMass = 2200F;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField] private bool applyAngularForce = true;

        [ShowIf(nameof(applyAngularForce))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0.1f, 100000)] private float initialAngularForce = 20000f;

        [ShowIf(nameof(applyAngularForce))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0.1f, 10000)] private float constantAngularForce = 150f;

        [ShowIf(nameof(applyAngularForce))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0f, 25f)] private float applyAngularForceTime = 1f;

        public override CarExplodeConfigReference CreateConfig(BlobAssetReference<CarExplodeConfig> blobRef)
        {
            return new CarExplodeConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<CarExplodeConfig> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<CarExplodeConfig>();

                root.InitialYForce = initialYForce;
                root.InitialForwardForce = initialForwardForce;
                root.VelocityMultiplier = velocityMultiplier;
                root.ApplyForceOffset = applyForceOffset;
                root.SourceMass = sourceMass;
                root.ApplyAngularForce = applyAngularForce;
                root.InitialAngularForce = initialAngularForce;
                root.ConstantAngularForce = constantAngularForce;
                root.ApplyAngularForceTime = applyAngularForceTime;

                return builder.CreateBlobAssetReference<CarExplodeConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class CarExplodeConfigAuthoringBaker : Baker<CarExplodeConfigAuthoring>
        {
            public override void Bake(CarExplodeConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.CreateConfig(this));
            }
        }
    }
}