#if UNITY_EDITOR
using Spirit604.Extensions;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficNodeCommonDebugger : ITrafficNodeDebugger
    {
        private EntityManager entityManager;

        public TrafficNodeCommonDebugger(EntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public void Tick(Entity entity)
        {
            string text = $"(E: {entity.Index})";
            Vector3 position = entityManager.GetComponentData<LocalToWorld>(entity).Position + new Unity.Mathematics.float3(0, 0.5f, 0);
            EditorExtension.DrawWorldString(text, position);
        }
    }
}
#endif