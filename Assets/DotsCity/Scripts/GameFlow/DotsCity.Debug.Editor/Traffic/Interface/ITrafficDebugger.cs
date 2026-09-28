#if UNITY_EDITOR
using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    public interface ITrafficDebugger
    {
        string Tick(Entity entity);
    }
}
#endif