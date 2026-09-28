namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    public enum StateType
    {
        /// <summary>
        /// The state proccessed by `PedestrianStateSystem` system (code processing for state should be there).
        /// </summary>
        Default,

        /// <summary>
        /// The state proccessed by external system (code processing for state should be in the separate system).
        /// </summary>
        ExternalSystem,

        /// <summary>
        /// Additive state flag adds to the current state and is processed by the `External system`.
        /// </summary>
        Additive,

        /// <summary>
        /// Additive state flag adds to the current state and is processed by the `External system` & ignores available next state flags.
        /// </summary>
        AdditiveAny
    }
}