using Spirit604.DotsCity.Simulation.Traffic;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    public abstract class CarLightBase : MonoBehaviour, ICarLight
    {
        public abstract void UpdateState(in VehicleLightComponent vehicleLightComponent);
    }
}
