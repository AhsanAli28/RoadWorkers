namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [System.Flags]
    public enum ActionState
    {
        Default = 0,
        AchievedTarget = 1 << 0,
        Idle = 1 << 1,
        MovingToNextTargetPoint = 1 << 2,
        WaitForGreenLight = 1 << 3,
        CrossingTheRoad = 1 << 4,
        ScaryRunning = 1 << 5,
        Sitting = 1 << 6,
        Talking = 1 << 7,
        Reset = 1 << 8,
    }
}