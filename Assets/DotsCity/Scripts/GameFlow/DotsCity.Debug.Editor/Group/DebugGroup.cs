using Spirit604.DotsCity.Simulation;
using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    [UpdateInGroup(typeof(HashMapGroup), OrderLast = true)]
    public partial class DebugGroup : ComponentSystemGroup
    {
    }
}