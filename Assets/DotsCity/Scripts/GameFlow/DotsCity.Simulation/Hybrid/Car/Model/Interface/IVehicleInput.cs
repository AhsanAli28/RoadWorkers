namespace Spirit604.DotsCity.Simulation.Car
{
    public interface IVehicleInput
    {
        float Throttle { get; set; }
        float Steering { get; set; }
        bool Handbrake { get; set; }

        void SwitchEnabledState(bool isEnabled);
    }
}
