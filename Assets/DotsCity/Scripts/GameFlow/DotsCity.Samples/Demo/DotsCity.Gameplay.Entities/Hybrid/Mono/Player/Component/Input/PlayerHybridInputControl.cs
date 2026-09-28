using Spirit604.DotsCity.Simulation.Car;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public class PlayerHybridInputControl : MonoBehaviour, IVehicleInput
    {
        private IVehicleEntityRef vehicleEntityRef;

        public float Throttle { get; set; }
        public float Steering { get; set; }
        public bool Handbrake { get; set; }

        private EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        private void Awake()
        {
            if (vehicleEntityRef == null)
                SetEntityRef(GetComponent<IVehicleEntityRef>());
        }

        private void Update()
        {
            ReadInput();
        }

        public void SwitchEnabledState(bool isEnabled)
        {
            enabled = isEnabled;

            if (!isEnabled)
            {
                Throttle = 0;
                Steering = 0;
                Handbrake = true;
            }
        }

        public void SetEntityRef(IVehicleEntityRef hybridEntityRef)
        {
            this.vehicleEntityRef = hybridEntityRef;
        }

        private void ReadInput()
        {
            if (!vehicleEntityRef.HasEntity)
                return;

            var reader = EntityManager.GetComponentData<VehicleInputReader>(vehicleEntityRef.RelatedEntity);
            Throttle = reader.Throttle;
            Steering = reader.SteeringInput;
            Handbrake = reader.Handbrake;
        }
    }
}