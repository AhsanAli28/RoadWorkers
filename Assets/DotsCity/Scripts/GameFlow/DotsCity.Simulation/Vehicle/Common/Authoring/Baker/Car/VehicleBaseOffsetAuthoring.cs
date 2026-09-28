using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    [DisallowMultipleComponent]
    public class VehicleBaseOffsetAuthoring : MonoBehaviour
    {
        [field: SerializeField]
        public float BaseOffset;

        class VehicleBaseOffsetAuthoringBaker : Baker<VehicleBaseOffsetAuthoring>
        {
            public override void Bake(VehicleBaseOffsetAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(entity, new VehicleBaseOffsetComponent()
                {
                    Offset = authoring.BaseOffset
                });
            }
        }
    }
}