using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Custom.Authoring
{
    [System.Serializable]
    public class WheelData
    {
        public const float BrakeRateDefaultValue = 1f;
        public const float HandBrakeRateDefaultValue = 1.2f;

        public GameObject Wheel;
        public bool Driving = true;
        public bool Brake = true;

        [Range(0.01f, 3f)]
        public float BrakeRate = BrakeRateDefaultValue;

        [Range(0.01f, 3f)]
        public float HandBrakeRate = HandBrakeRateDefaultValue;

        public WheelData()
        {
            this.Brake = true;
            this.Driving = true;
            this.BrakeRate = WheelData.BrakeRateDefaultValue;
            this.HandBrakeRate = WheelData.HandBrakeRateDefaultValue;
        }

        public WheelData(GameObject wheel) : this()
        {
            this.Wheel = wheel;
        }

        public float BrakeValue => Brake ? BrakeRate : 0;
        public float HandBrakeValue => Brake ? HandBrakeRate : 0;
        public float DrivingValue => Driving ? 1 : 0;
    }
}