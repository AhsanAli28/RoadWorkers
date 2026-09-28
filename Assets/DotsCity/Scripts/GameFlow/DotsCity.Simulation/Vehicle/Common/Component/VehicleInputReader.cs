using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car
{
    public struct VehicleInputReader : IComponentData
    {
        public float Throttle;
        public float SteeringInput;
        public int HandbrakeInput;

        public bool Handbrake => HandbrakeInput == 1;

        public static VehicleInputReader GetBrake() => new VehicleInputReader()
        {
            Throttle = 0,
            SteeringInput = default,
            HandbrakeInput = 1,
        };
    }
}