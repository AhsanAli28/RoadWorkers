using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Attributes;
using Spirit604.Extensions;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    public class CarModelRuntimeAuthoring : MonoBehaviour, IRuntimeEntityComponentSetProvider, IRuntimeInitEntity
    {
        [SerializeField] private VehicleDataCollection vehicleDataCollection;

        [CarModel(nameof(vehicleDataCollection))]
        [SerializeField] private int carModel;

        public int CarModel { get => carModel; set => carModel = value; }

        public ComponentType[] GetComponentSet() => new ComponentType[] { typeof(CarModelComponent) };

        void IRuntimeInitEntity.Initialize(EntityManager entityManager, GameObject root, Entity entity)
        {
            entityManager.SetComponentData(entity,
                new CarModelComponent()
                {
                    Value = carModel
                });
        }

        private void Reset()
        {
            if (!vehicleDataCollection)
            {
                var holder = ObjectUtils.FindObjectOfType<VehicleDataHolder>();

                if (holder)
                {
                    vehicleDataCollection = holder.VehicleDataCollection;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }
    }
}
