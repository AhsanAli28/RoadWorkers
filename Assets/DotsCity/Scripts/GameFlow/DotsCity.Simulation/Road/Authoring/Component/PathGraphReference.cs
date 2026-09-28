using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    public struct PathBlobDataGraph
    {
        public BlobArray<PathBlobData> Paths;
    }

    public struct PathGraphReference : IComponentData
    {
        public BlobAssetReference<PathBlobDataGraph> Graph;
    }
}
