namespace Spirit604.DotsCity.Simulation.Traffic.Obstacle
{
    public enum State : byte
    {
        None = 0,
        IsIdle = 1 << 0,
        ChangingLane = 1 << 1,
        HasObstacle = 1 << 2,
        HasCalculatedObstacle = 1 << 3,
        InRangeOfSemaphore = 1 << 4,
        AtTrafficArea = 1 << 5,
        ParkingCar = 1 << 6,
    }
}