using System.Collections.Generic;

namespace Spirit604.Gameplay.Road
{
    /// <summary>
    /// List<LaneArray> array, each index of the array matches to the lane index of the road.
    /// </summary>
    [System.Serializable]
    public class LaneArray
    {
        public List<Path> paths;
        public int UniqueID;
    }
}
