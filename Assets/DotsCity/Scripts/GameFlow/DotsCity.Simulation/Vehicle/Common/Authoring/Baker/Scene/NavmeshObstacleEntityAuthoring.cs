using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.NavMesh;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    public class NavmeshObstacleEntityAuthoring : MonoBehaviour
    {
        class NavmeshObstacleEntityAuthoringBaker : Baker<NavmeshObstacleEntityAuthoring>
        {
            public override void Bake(NavmeshObstacleEntityAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.ManualOverride);

                AddComponent(entity, typeof(EntityTrackerComponent));
                AddComponent(entity, typeof(CopyTransformToGameObject));
                AddComponent(entity, typeof(LocalTransform));
                AddComponent(entity, typeof(LocalToWorld));
                AddComponent(entity, typeof(NavMeshObstaclePrefabTag));
                AddComponent(entity, typeof(Transform));
                AddComponent(entity, typeof(Prefab));

                PoolEntityUtils.AddPoolComponents(this, entity, EntityWorldType.HybridEntity);
            }
        }
    }
}