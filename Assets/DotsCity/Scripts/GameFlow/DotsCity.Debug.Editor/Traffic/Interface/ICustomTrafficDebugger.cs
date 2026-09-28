#if UNITY_EDITOR
using Unity.Entities;

namespace Spirit604.DotsCity.Debug
{
    public interface ICustomTrafficDebugger
    {
        public void DrawInspector();
        public void DrawSceneView(Entity entity);
    }
}
#endif