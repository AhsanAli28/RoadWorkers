namespace Spirit604.Gameplay.Road
{
    [System.Flags]
    public enum TrafficGroupType
    {
        Default = 1 << 0,
        Tram = 1 << 1,
        PublicTransport = 1 << 2,
        Taxi = 1 << 3,
        Truck = 1 << 4,
        Police = 1 << 5,
        Ambulance = 1 << 6,
        RoadService = 1 << 7,
    }
}