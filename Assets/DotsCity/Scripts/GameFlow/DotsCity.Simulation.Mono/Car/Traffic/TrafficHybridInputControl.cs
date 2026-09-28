using Spirit604.DotsCity.Simulation.Car;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Mono
{
    public class TrafficHybridInputControl : MonoBehaviour, IVehicleInput
    {
        public float Throttle { get; set; }
        public float Steering { get; set; }
        public bool Handbrake { get; set; }

        public void SwitchEnabledState(bool isEnabled)
        {
            enabled = isEnabled;
        }
    }
}