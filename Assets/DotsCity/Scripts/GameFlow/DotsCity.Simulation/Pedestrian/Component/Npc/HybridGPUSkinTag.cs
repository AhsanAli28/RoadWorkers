using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct HybridGPUSkinTag : IComponentData { }

    /// <summary> The Hybrid skin will only load if the user manually disables this tag, otherwise the GPU will be used (for <b>HybridOnRequestAndGPU</b> type only).</summary>
    public struct PreventHybridSkinTagTag : IComponentData, IEnableableComponent { }
}
