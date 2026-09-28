using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    [DisallowMultipleComponent]
    public class VehicleOverrideTypeAuthoring : MonoBehaviour
    {
        [field: SerializeField]
        public EntityType EntityType { get; set; }

        class VehicleOverrideTypeBaker : Baker<VehicleOverrideTypeAuthoring>
        {
            public override void Bake(VehicleOverrideTypeAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(entity, new VehicleOverrideTypeComponent()
                {
                    EntityType = authoring.EntityType
                });
            }
        }
    }
}