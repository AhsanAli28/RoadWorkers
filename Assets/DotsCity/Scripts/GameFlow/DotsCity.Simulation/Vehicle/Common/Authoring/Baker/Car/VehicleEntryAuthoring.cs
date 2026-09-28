using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.Gameplay.Road;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    public class VehicleEntryAuthoring : MonoBehaviour
    {
        [field: SerializeField]
        public PedestrianNodeType EntryType { get; set; } = PedestrianNodeType.TrafficPublicEntry;

        class VehicleEntryAuthoringBaker : Baker<VehicleEntryAuthoring>
        {
            public override void Bake(VehicleEntryAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(entity, new NodeSettingsComponent()
                {
                    NodeType = authoring.EntryType
                });
            }
        }
    }
}