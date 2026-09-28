using System;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [Flags]
    public enum RecalculationType
    {
        /// <summary> A tile can't be changed under any circumstances, except by manual replacement. </summary>
        None,

        /// <summary> A tile can change itself if the connection is not suitable. </summary>
        Self = 1 << 0,

        /// <summary> The current tile can change neighbouring tiles if the neighbour has the 'ByNeighbour' flag & the connection is not suitable. </summary>
        Other = 1 << 1,

        /// <summary> A tile can be changed by a neighbour if the connection is not suitable. </summary>
        ByNeighbor = 1 << 2,
    }
}
