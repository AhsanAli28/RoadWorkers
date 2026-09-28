namespace Spirit604.DotsCity.Simulation.Level.Streaming.Authoring
{
    public enum SectionObjectType
    {
        /// <summary> Attach to nearest road section. </summary>        
        AttachToClosest,

        /// <summary> Create a new road section if doesn't exist with the currently computed section hash. </summary>       
        CreateNewIfNessesary,

        /// <summary> Object has a component that implements the `IProviderObject` interface, that provides a reference to the associated object section. </summary>       
        ProviderObject,

        /// <summary> User's own associated object section. </summary>       
        CustomObject
    }
}