using System;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Level.Streaming
{
    [Serializable]
    public struct StreamingLevelConfig : IComponentData
    {
        public float DistanceForStreamingInSQ;
        public float DistanceForStreamingOutSQ;
        public bool StreamingIsEnabled;
    }
}