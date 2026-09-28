using System;

namespace Spirit604.Gameplay.Road
{
    [Flags]
    public enum TrafficNodeSubNodeType
    {
        None = 0,
        Inner = 1 << 0,
        Outer = 1 << 1,
    }
}