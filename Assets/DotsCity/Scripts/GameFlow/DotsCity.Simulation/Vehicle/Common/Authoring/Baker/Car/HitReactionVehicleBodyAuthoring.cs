using Spirit604.DotsCity.Core;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Spirit604.DotsCity.Simulation.Car
{
    public class HitReactionVehicleBodyAuthoring : MonoBehaviour
    {
        class HitReactionVehicleBodyAuthoringBaker : Baker<HitReactionVehicleBodyAuthoring>
        {
            public override void Bake(HitReactionVehicleBodyAuthoring authoring)
            {
                var hitMeshEntity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<VehicleAnimatedHullTag>(hitMeshEntity);
                AddComponent<HitReactionMaterialDataComponent>(hitMeshEntity);
                AddComponent<HitReactionStateComponent>(hitMeshEntity);
                AddComponent<CarShaderDeviationData>(hitMeshEntity);
                AddComponent<CarShaderLerpData>(hitMeshEntity);

                AddComponent<AnimateHitReactionTag>(hitMeshEntity);
                AddComponent<HitReactionInitComponent>(hitMeshEntity);
                AddComponent<MaterialMeshInfo>(hitMeshEntity);
                AddComponent<WorldRenderBounds>(hitMeshEntity);

                AddComponent<RenderBounds>(hitMeshEntity);

                AddComponent<WorldToLocal_Tag>(hitMeshEntity);
                AddComponent<PerInstanceCullingTag>(hitMeshEntity);
                AddComponent<BlendProbeTag>(hitMeshEntity);

                AddComponent<EntityTrackerComponent>(hitMeshEntity);
                AddComponent<CarRelatedHullComponent>(hitMeshEntity);

                PoolEntityUtils.AddPoolComponents(this, hitMeshEntity, EntityWorldType.PureEntity);

                var filterSettings = RenderFilterSettings.Default;
                filterSettings.ShadowCastingMode = ShadowCastingMode.On;
                filterSettings.ReceiveShadows = true;

                filterSettings.Layer = 0;
                filterSettings.RenderingLayerMask = 1;

                AddSharedComponentManaged(hitMeshEntity, filterSettings);
                AddComponent<RenderMeshArray>(hitMeshEntity);

                AddComponent<HitReactionVehicleBodyTag>(hitMeshEntity);
            }
        }
    }
}