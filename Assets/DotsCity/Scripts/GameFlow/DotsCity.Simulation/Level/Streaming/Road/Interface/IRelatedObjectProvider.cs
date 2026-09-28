using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Level.Streaming.Authoring
{
    public interface IRelatedObjectProvider
    {
        GameObject RelatedObject { get; }
    }
}