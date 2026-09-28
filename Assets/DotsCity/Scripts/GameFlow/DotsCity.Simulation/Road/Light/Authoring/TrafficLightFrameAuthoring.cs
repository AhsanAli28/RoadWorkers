using Spirit604.Gameplay.Road;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    [RequireComponent(typeof(TrafficLightFrame))]
    public class TrafficLightFrameAuthoring : MonoBehaviour
    {
        class TrafficLightFrameAuthoringBaker : Baker<TrafficLightFrameAuthoring>
        {
            public override void Bake(TrafficLightFrameAuthoring authoring)
            {
            }
        }
    }
}