using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    /// <summary>
    /// Disable the built-in Locotomion system if the user has their own solution.
    /// </summary>
    public struct CustomLocomotionTag : IComponentData
    {
    }
}