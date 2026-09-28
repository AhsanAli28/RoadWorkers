using Spirit604.Gameplay.Road;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    public struct PathBlobData
    {
        public BlobArray<int> NextConnectedPathsIndexes;
        public BlobArray<IntersectPathInfo> IntersectedPaths;
        public BlobArray<int> NeighbourPathsIndexes;
        public BlobArray<int> ParallelPathsIndexes;
        public BlobArray<RouteNodeData> RouteNodes;

        public float PathLength;
        public int ConnectedPathIndex;
        public int SourceLaneIndex;
        public int Priority;

        public PathOptions Options;
        public PathCurveType PathCurveType;
        public PathRoadType PathRoadType;
        public PathConnectionType PathConnectionType;
        public TrafficGroupType TrafficGroup;
    }
}
