using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Car.Custom
{
    public static class VehicleExtensions
    {
        private const float WheelRotationSpeedToEngineRpm = 60 / (2 * math.PI);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LinearToRotationSpeed(this float speed, float radius)
        {
            return speed / radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RotationToLinearSpeed(this float speed, float radius)
        {
            return speed * radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TorqueToForce(this float torque, float radius)
        {
            return torque / radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ForceToTorque(this float force, float radius)
        {
            return force * radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EvaluateTorque(this VehicleEngine engine, float rpm)
        {
            return engine.Torque.Value.Evaluate(rpm);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TransmitWheelRotationSpeedToEngineRpm(this VehicleEngine engine, float speed)
        {
            return speed * engine.TransmissionRate * WheelRotationSpeedToEngineRpm;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TransmitEngineTorqueToWheelTorque(this VehicleEngine engine, float torque)
        {
            return torque * engine.TransmissionRate;
        }
    }
}