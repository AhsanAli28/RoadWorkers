using Unity.Entities;
using Unity.Jobs;

namespace Spirit604.DotsCity
{
    public abstract partial class SimpleSystemBase : SystemBase
    {
        public JobHandle GetDependency()
        {
            return Dependency;
        }
    }
}