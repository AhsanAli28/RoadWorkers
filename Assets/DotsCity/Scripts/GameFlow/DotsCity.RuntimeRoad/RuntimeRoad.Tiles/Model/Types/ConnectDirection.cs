using System;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [Flags]
    public enum ConnectDirection
    {
        None = 0,
        Left = 1 << 0,
        Top = 1 << 1,
        Right = 1 << 2,
        Bottom = 1 << 3
    }
}
