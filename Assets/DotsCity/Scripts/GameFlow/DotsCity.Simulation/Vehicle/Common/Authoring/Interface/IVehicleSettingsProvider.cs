namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    public interface IVehicleSettingsProvider
    {
        /// <summary> Max vehicle speed in km/h.</summary>
        float MaxSpeed { get; }

        /// <summary> Max steering angle of the vehicle in degree.</summary>
        float MaxSteeringAngle { get; }
    }
}