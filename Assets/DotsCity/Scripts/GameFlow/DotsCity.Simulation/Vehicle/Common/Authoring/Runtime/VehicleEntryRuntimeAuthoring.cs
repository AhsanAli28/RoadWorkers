using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    [RequireComponent(typeof(HybridEntityRuntimeAuthoring))]
    public class VehicleEntryRuntimeAuthoring : MonoBehaviour, IRuntimeEntityComponentSetProvider, IRuntimeInitEntity
    {
        [SerializeField] private Transform parent;

        ComponentType[] IRuntimeEntityComponentSetProvider.GetComponentSet()
        {
            return new ComponentType[] {
                ComponentType.ReadOnly<VehicleLinkComponent>(),
                ComponentType.ReadOnly<CopyTransformFromGameObject>(),
                ComponentType.ReadOnly<NodeSettingsComponent>(),
            };
        }

        void IRuntimeInitEntity.Initialize(EntityManager entityManager, GameObject root, Entity entity)
        {
            var entityRef = parent.GetComponent<IHybridEntityRef>();

            entityManager.SetComponentData(entity, new VehicleLinkComponent()
            {
                LinkedVehicle = entityRef.RelatedEntity
            });

            entityManager.AddComponentObject(entity, root.transform);

            entityManager.SetComponentData(entity, new NodeSettingsComponent()
            {
                NodeType = Gameplay.Road.PedestrianNodeType.TrafficPublicEntry,
                SumWeight = 1,
            });
        }
    }
}