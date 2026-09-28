using Spirit604.DotsCity.Simulation.Traffic;

namespace Spirit604.DotsCity.Simulation.Car
{
    public interface ICarLight
    {
        void UpdateState(in VehicleLightComponent trafficBlinkerComponent);
    }
}