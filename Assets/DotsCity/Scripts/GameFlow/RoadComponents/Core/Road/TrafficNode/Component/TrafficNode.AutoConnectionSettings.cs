namespace Spirit604.Gameplay.Road
{
    public partial class TrafficNode
    {
        public struct AutoConnectionSettings
        {
            public AutoConnectionSettings(float waypointOffset = 0, float maxConnectionLength = 0, bool connectCrosswalks = false) : this(waypointOffset, false, 0)
            {
                MaxConnectionLength = maxConnectionLength;
                ConnectCrosswalks = connectCrosswalks;
                MergeDistance = 0;
            }

            public AutoConnectionSettings(float waypointOffset = 0, bool multiAngleRaycast = false, float angle = 0, float maxConnectionLength = 0, bool connectCrosswalks = false, bool externalSubNodes = false)
            {
                WaypointStepOffset = waypointOffset;
                MultiAngleRaycast = multiAngleRaycast;
                Angle = angle;
                MaxConnectionLength = maxConnectionLength;
                ConnectCrosswalks = connectCrosswalks;
                ExternalSubNodes = externalSubNodes;
                CloseRaycast = false;
                AllowMerge = false;
                MergeDistance = 0;
            }

            public float WaypointStepOffset { get; set; }
            public bool MultiAngleRaycast { get; set; }
            public float Angle { get; set; }
            public float MaxConnectionLength { get; set; }
            public bool ConnectCrosswalks { get; set; }
            public bool ExternalSubNodes { get; set; }
            public bool CloseRaycast { get; set; }
            public bool AllowMerge { get; set; }
            public float MergeDistance { get; set; }
        }
    }
}