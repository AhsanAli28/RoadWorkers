using System.Collections.Generic;

namespace Spirit604.DotsCity.Debug
{
    public static class TrafficDebugDescription
    {
        public static Dictionary<TrafficDebugMode, string> Description = new Dictionary<TrafficDebugMode, string>()
        {
            {
                TrafficDebugMode.DebugObstacleDistance,
                $"Change 'maxDistanceToObstacle' parameter for different result \n" +
                "Red box - obstacle \n" +
                "Approach speed = -1 - not approaching \n" +
                "Approach speed = 0 - default approach speed in config \n" +
                "Approach speed > 0 - current approach speed \n"
            },
            {
                TrafficDebugMode.DebugNextPathCalculationDistance,
                "Change 'minDistanceToCheckNextConnectedPath' parameter for different result \n" +
                "Red box - calculating next connected path \n"
            },
            {
                TrafficDebugMode.DebugIntersectedPath,
                "Change 'calculateDistanceToIntesectPoint', 'carTooCloseToIntersectPointDistance', 'closeEnoughDistanceToStopBeforeIntersectPoint' parameter for different result \n" +
                "Red circle - intersected path too far \n" +
                "Green circle - intersected path calculating \n" +
                "White circle - intersected path too closed, path skipped \n" +
                "Number - distance to point \n"
            },
            {
                TrafficDebugMode.DebugChangeLane,
                "Change 'TrafficChangeLaneConfig' parameters for different result \n"
            },
        };
    }
}
