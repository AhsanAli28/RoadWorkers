namespace Spirit604.AnimationBaker
{
    public enum AnimationTransitionType
    {
        /// <summary> Animation play sequentially one by one without interpolating.</summary>
        Default,

        /// <summary> The previous animation is interpolated to the beginning of the next animation with the set duration.</summary>
        ToStart,

        /// <summary> The previous animation is interpolated to the global playback time of the next animation with the set duration.</summary>
        ToGlobalTimeSync
    }
}
