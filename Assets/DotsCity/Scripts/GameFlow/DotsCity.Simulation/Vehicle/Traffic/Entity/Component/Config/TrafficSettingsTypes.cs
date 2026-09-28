namespace Spirit604.DotsCity.Simulation.Traffic
{
    public enum EntityType
    {
        /// <summary> Hybrid entities moved by the simple physical system. </summary>
        HybridEntitySimplePhysics = 0,

        /// <summary> Hybrid entities moved by the custom physical system. </summary>
        HybridEntityCustomPhysics = 1,

        /// <summary> Hybrid entities moved by the custom MonoBehaviour controller. </summary>
        HybridEntityMonoPhysics = 5,

        /// <summary> Pure entities moved by the simple physical system. </summary>
        PureEntityCustomPhysics = 2,

        /// <summary> Pure entities moved by the simple physical system. </summary>
        PureEntitySimplePhysics = 3,

        /// <summary> Pure entities that moved by transform system without physics. </summary>
        PureEntityNoPhysics = 4
    }

    public enum DetectObstacleMode
    {
        /// <summary> Combine types Calculate and Raycast. </summary>
        Hybrid,

        /// <summary> Mathematically calculates the obstacle. </summary>
        CalculateOnly,

        /// <summary> Detect obstacle by raycast only. </summary>
        RaycastOnly
    }

    public enum DetectNpcMode
    {
        Disabled,

        /// <summary> Mathematically calculates the npc. </summary>
        Calculate,

        /// <summary> Detect obstacle by raycast (npc should have PhysicsShape component). </summary>
        Raycast
    }

    public enum PhysicsSimulationType { CustomDots, HybridMono, Simple }

    public enum SimplePhysicsSimulationType
    {
        /// <summary> Simple emulation of real movement based on traffic input. </summary>
        CarInput,

        /// <summary> The vehicle rotation is set based on the destination direction. </summary>
        FollowTarget
    }
}