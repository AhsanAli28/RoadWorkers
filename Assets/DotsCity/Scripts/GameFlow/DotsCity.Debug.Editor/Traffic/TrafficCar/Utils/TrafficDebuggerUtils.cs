#if UNITY_EDITOR
using Spirit604.Extensions;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficDebuggerUtils
    {
        public static void FocusEntity(ref EntityManager entityManager, Entity entity, float size = 5f, bool instant = false)
        {
            var transform = entityManager.GetComponentData<LocalTransform>(entity);
            EditorExtension.SceneFocus(transform.Position, size, instant);
        }
    }
}
#endif