namespace Spirit604.DotsCity.Simulation.Traffic
{
    /// <summary>
    /// If a traffic car has a component that implements an ICustomTrafficCar interface & CustomHandling is true, then this car will not be spawned by default in TrafficSpawnerSystem.
    /// </summary>
    public interface ICustomTrafficCar
    {
        public bool CustomHandling { get; }
    }
}