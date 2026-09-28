namespace Spirit604.Gameplay.Road
{
    public enum TrafficNodeDirectionType
    {
        Right = 1 << 0,
        Left = 1 << 1,
        RightAndLeft = Right | Left,
    }
}