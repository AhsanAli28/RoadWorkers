#if UNITY_EDITOR
using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    public interface ITrafficNodeDebugger
    {
        void Tick(Entity entity);
    }
}
#endif