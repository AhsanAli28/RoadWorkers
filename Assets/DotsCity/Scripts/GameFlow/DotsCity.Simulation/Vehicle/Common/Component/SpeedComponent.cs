using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Car
{
    public struct SpeedComponent : IComponentData
    {
        public float Value;
        public float LaneLimit;
        public float CurrentLimit;

        public float ValueAbs => math.abs(Value);
        public float SpeedKmh => Value * ProjectConstants.KmhToMs_RATE;

        public static SpeedComponent SetSpeedLimitByKmh(float speedKmh)
        {
            return new SpeedComponent()
            {
                CurrentLimit = ToMeterPerSecond(speedKmh)
            };
        }

        public static float ToMeterPerSecond(float speedKmh)
        {
            return speedKmh / ProjectConstants.KmhToMs_RATE;
        }
    }
}