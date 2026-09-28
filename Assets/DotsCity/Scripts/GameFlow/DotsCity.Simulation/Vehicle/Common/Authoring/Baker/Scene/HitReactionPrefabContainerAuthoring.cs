using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    public struct HitReactionVehicleBodyPrefab : IComponentData
    {
        public Entity PrefabEntity;
    }

    public class HitReactionPrefabContainerAuthoring : MonoBehaviour
    {
        [SerializeField]
        private GameObject prefab;

        class HitReactionPrefabContainerAuthoringBaker : Baker<HitReactionPrefabContainerAuthoring>
        {
            public override void Bake(HitReactionPrefabContainerAuthoring authoring)
            {
                var hitMeshEntity = CreateAdditionalEntity(TransformUsageFlags.None);

                AddComponent(hitMeshEntity, new HitReactionVehicleBodyPrefab()
                {
                    PrefabEntity = GetEntity(authoring.prefab.gameObject, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}