using Spirit604.Extensions;
using Spirit604.Gameplay.Road;

namespace Spirit604.DotsCity.Simulation.Road
{
    public partial struct PathGraphSystem
    {
        public struct PathData
        {
            public int ConnectedPathDataCount;
            public int ConnectedPathDataByCount;
            public int IntersectedCount;
            public int NeighbourCount;
            public int ParallelCount;
            public int NodeCount;

#if !RUNTIME_ROAD
            public int ConnectedPathDataIndex;
            public int ConnectedPathDataByIndex;
            public int IntersectedIndex;
            public int NeighbourIndex;
            public int ParallelIndex;
            public int NodeIndex;
#else
            public int PathIndex;
#endif

            public float PathLength;
            public int ConnectedPathIndex;
            public int SourceLaneIndex;
            public int Priority;

            public PathOptions Options;
            public PathCurveType PathCurveType;
            public PathRoadType PathRoadType;
            public PathConnectionType PathConnectionType;
            public TrafficGroupType TrafficGroup;

            public bool HasOption(PathOptions option) => DotsEnumExtension.HasFlagUnsafe(Options, option);
        }
    }
}
