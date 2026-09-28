namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    /// <summary>
    /// Used to transfer settings between Traffic & Player cars after conversion.
    /// </summary>
    public interface IVehicleSettingsSync
    {
        public void Sync(object sourceObject);
    }
}