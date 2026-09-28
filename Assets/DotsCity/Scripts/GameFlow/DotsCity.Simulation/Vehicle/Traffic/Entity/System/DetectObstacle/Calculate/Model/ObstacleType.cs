namespace Spirit604.DotsCity.Simulation.Traffic.Obstacle
{
    public enum ObstacleType : byte
    {
        Undefined,

        /// <summary> Default obstacle in the current path or next connected path of the vehicle. </summary>
        DefaultPath,

        /// <summary> Obstacle on neighbouring paths, that starts from the current path. </summary>
        NeighborPath,

        /// <summary> The vehicle stays at the entrance of the crossroad & doesn't enter to avoid traffic jams. </summary>
        JamCase_1,

        /// <summary> Obstacle when the current vehicle and the obstacle vehicle change lanes at the same time. </summary>
        FewChangeLaneCars,

        /// <summary> The obstacle vehicle changes lanes to the lane of the current vehicle. </summary>
        ChangingLane,

        /// <summary> Target obstacle vehicle too close to intersection of two paths (both vehicles are close, but the target vehicle is closer). </summary>
        Intersect_1_TargetCarCloseToIntersectPoint,

        /// <summary> Target obstacle vehicle too close to intersection of two paths (only target vehicle is too close). </summary>
        Intersect_2_TargetCarCloseToIntersectPoint,

        /// <summary> Vehicles meeting at an intersection of two paths have different priorities, with the higher priority vehicle passing first (unless the vehicle is too close to the intersection). </summary>
        Intersect_3_OtherHasPriority,

        /// <summary> Vehicles that meet at an intersection of two paths have the same priority, whichever vehicle is closer to the intersection that passes first. </summary>
        Intersect_4_SamePriority,

        /// <summary> If the cars have the same priority, the car from the right has priority, otherwise, if this option is disabled, the car closer to the intersection has priority. </summary>
        Intersect_5_PriorityToRight,
    }
}
