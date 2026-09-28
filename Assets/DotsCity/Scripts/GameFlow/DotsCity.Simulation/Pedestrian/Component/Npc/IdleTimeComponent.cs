using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    /// <summary>
    /// Component for the temporary inactivity of an npc at a given point in time.
    /// </summary>
    public struct IdleTimeComponent : IComponentData
    {
        public bool IsInitialized;
        public float DisableIdleTimestamp;
        public Entity IdleNode;
    }
}
