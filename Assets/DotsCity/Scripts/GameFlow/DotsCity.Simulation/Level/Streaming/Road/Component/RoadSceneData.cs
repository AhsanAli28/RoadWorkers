using Unity.Entities;
using Unity.Entities.Serialization;

namespace Spirit604.DotsCity.Simulation.Level.Streaming
{
    public struct RoadSceneData : IComponentData
    {
        public EntitySceneReference SceneReference;
        public Hash128 Hash128;
    }
}