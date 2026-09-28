namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public static class PedestrianDestinationComponentExtension
    {
        public static void SetCustomAchieveDistance(ref this DestinationComponent destinationComponent, float val)
        {
            destinationComponent.CustomAchieveDistance = val;
            destinationComponent.CustomAchieveDistanceSQ = val * val;
        }
    }
}