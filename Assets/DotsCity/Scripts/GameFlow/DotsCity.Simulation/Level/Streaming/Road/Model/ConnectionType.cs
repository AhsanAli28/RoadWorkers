using System;

namespace Spirit604.DotsCity.Simulation.Level.Streaming
{
    [Flags]
    public enum ConnectionType
    {
        /// <summary> Current traffic node is connected to Streamed traffic node.</summary>
        StreamingConnection = 1 << 0,

        /// <summary> The current traffic node connected with External traffic nodes.</summary>
        ExternalStreamingConnection = 1 << 1,
    }
}
