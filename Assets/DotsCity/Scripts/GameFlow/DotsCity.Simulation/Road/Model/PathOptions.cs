using System;

namespace Spirit604.DotsCity.Simulation.Road
{
    [Flags]
    public enum PathOptions
    {
        None = 0,
        EnterOfCrossroad = 1 << 0,
        HasCustomNode = 1 << 1, // Whether node with custom traffic group
        Rail = 1 << 2,
    }
}
