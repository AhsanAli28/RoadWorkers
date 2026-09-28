using System;

namespace Spirit604.DotsCity.Simulation.Level.Streaming
{
    [Flags]
    public enum ConnectionSettings
    {
        Default = 1 << 0,
        SubNode = 1 << 1,
        DefaultConnection = 1 << 2,
        SubConnection = 1 << 3,
    }
}