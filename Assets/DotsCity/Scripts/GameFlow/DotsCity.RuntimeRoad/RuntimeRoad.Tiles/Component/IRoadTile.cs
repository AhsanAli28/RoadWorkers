namespace Spirit604.DotsCity.RuntimeRoad
{
    public interface IRoadTile
    {
        ConnectDirection CurrentFlags { get; }
        RecalculationType Type { get; }
        int Page { get; }
    }
}