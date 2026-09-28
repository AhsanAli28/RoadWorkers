using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    [RequireComponent(typeof(PhysicsSwitcher))]
    public class CarEntityAdapter : PhysicsHybridEntityAdapter
    {
        [Tooltip("Reference to the Component with <b>ICarLight</b> component. If the car doesn't use light, then ignore this field")]
        [SerializeField] private Component carLightBase;

        private IVehicleInput input;

        public ICarLight CarLight { get; private set; }

        private void Awake()
        {
            input = GetComponent<IVehicleInput>();

            if (carLightBase != null && carLightBase.TryGetComponent<ICarLight>(out var carLight))
            {
                CarLight = carLight;
            }
        }

        public void UpdateInput(in VehicleInputReader vehicleInputReader)
        {
            if (!Enabled)
                return;

            input.Throttle = vehicleInputReader.Throttle;
            input.Steering = vehicleInputReader.SteeringInput;
            input.Handbrake = vehicleInputReader.Handbrake;
        }
    }
}
