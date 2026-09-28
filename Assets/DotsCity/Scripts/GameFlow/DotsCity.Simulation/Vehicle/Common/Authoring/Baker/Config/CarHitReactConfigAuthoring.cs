using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    public struct HitReactionMaterialData : ISharedComponentData, IEquatable<HitReactionMaterialData>
    {
        public Material Material;

        public bool Equals(HitReactionMaterialData other)
        {
            return Material == other.Material;
        }

        public override int GetHashCode()
        {
            return Material?.GetHashCode() ?? 0;
        }
    }

    public class CarHitReactConfigAuthoring : SyncConfigBase
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/carCommonConfigs.html")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(Sync))]
        [SerializeField] private Material hitReactionMaterial;

        [OnValueChanged(nameof(Sync))]
        [SerializeField][Range(1, 100)] private int poolSize = 3;

        [OnValueChanged(nameof(Sync))]
        [SerializeField][Range(0, 5f)] private float effectDuration = 0.2f;

        [OnValueChanged(nameof(Sync))]
        [SerializeField][Range(1, 100f)] private float lerpSpeed = 10f;

        [OnValueChanged(nameof(Sync))]
        [SerializeField][Range(0f, 2f)] private float maxLerp = 1f;

        [OnValueChanged(nameof(Sync))]
        [SerializeField][Range(0, 0.5f)] private float divHorizontalRate = 0.05F;

        [OnValueChanged(nameof(Sync))]
        [SerializeField][Range(0, 0.5f)] private float divVerticalRate = 0.0125f;

        public Material HitReactionMaterial { get => hitReactionMaterial; set => hitReactionMaterial = value; }

        class CarHitReactAuthoringBaker : Baker<CarHitReactConfigAuthoring>
        {
            public override void Bake(CarHitReactConfigAuthoring authoring)
            {
                DependsOn(authoring.hitReactionMaterial);

                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<CarHitReactionConfig>();

                    root.PoolSize = authoring.poolSize;
                    root.EffectDuration = authoring.effectDuration;
                    root.LerpSpeed = authoring.lerpSpeed;
                    root.MaxLerp = authoring.maxLerp;
                    root.DivHorizontalRate = authoring.divHorizontalRate;
                    root.DivVerticalRate = authoring.divVerticalRate;

                    var blobRef = builder.CreateBlobAssetReference<CarHitReactionConfig>(Unity.Collections.Allocator.Persistent);

                    AddBlobAsset(ref blobRef, out var hash);

                    AddComponent(entity, new CarHitReactionConfigReference() { Config = blobRef });
                }

                AddSharedComponentManaged(entity, new HitReactionMaterialData()
                {
                    Material = authoring.hitReactionMaterial
                });
            }
        }
    }
}