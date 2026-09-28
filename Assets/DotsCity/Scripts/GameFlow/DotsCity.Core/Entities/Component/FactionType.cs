namespace Spirit604.DotsCity.Core
{
    [System.Flags]
    public enum FactionType
    {
        All = Player | City | Mafia,
        Player = 1 << 0,
        City = 1 << 1,
        Mafia = 1 << 2
    }
}
