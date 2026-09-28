using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    public struct CustomAnimatorStateTag : IComponentData, IEnableableComponent { }

    public struct HasCustomAnimationTag : IComponentData, IEnableableComponent { }

    /// <summary> Wait for the skin to load if it has been unloaded. </summary>
    public struct WaitForCustomAnimationTag : IComponentData, IEnableableComponent { }

    public struct UpdateCustomAnimationTag : IComponentData, IEnableableComponent { }

    public struct ExitCustomAnimationTag : IComponentData, IEnableableComponent { }
}