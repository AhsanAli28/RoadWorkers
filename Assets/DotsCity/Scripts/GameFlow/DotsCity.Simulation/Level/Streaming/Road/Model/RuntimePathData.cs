using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Level.Streaming
{
    public struct RuntimePathData
    {
        /// <summary> Source TrafficNode entity. </summary>
        public Entity SourceNode;

        /// <summary> Currently connected TrafficNode entity. </summary>
        public Entity ConnectedNode;
    }
}