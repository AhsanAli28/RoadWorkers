using Unity.Entities;

namespace Spirit604.DotsCity.Events
{
    public enum TriggerNodeType : byte { Default, Pedestrian, Traffic }

    /// <summary>
    /// Activated when traffic or pedestrian reaches node with trigger type.
    /// </summary>
    public struct TriggerNodeTag : IComponentData, IEnableableComponent { }
}