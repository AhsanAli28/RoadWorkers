using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    [InternalBufferCapacity(1)]
    public struct PathConnectionElement : IBufferElementData
    {
        public int GlobalPathIndex;
        public int ConnectedHash;
        public int ConnectedSubHash;
        public int StartLocalNodeIndex;
        public Entity ConnectedNodeEntity;
        public Entity ConnectedSubNodeEntity;
        public Entity CustomLightEntity;

        public Entity ClosestConnectedNode => HasSubNode ? ConnectedSubNodeEntity : ConnectedNodeEntity;
        public bool HasSubNode => ConnectedSubHash != -1;
        public bool SameHash => ConnectedHash == ConnectedSubHash;
    }
}