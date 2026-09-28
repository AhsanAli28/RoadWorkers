#if UNITY_EDITOR
using Spirit604.Extensions;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficPhysicsLayerDebugger : MonoBehaviour
    {
        [SerializeField] private bool enableDebug;

        private TrafficDebuggerSystem trafficDebuggerSystem;
        private EntityManager entityManager;

        private void Start()
        {
            trafficDebuggerSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TrafficDebuggerSystem>();
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void OnDrawGizmos()
        {
            if (!enableDebug || !Application.isPlaying)
            {
                return;
            }

            var traffics = trafficDebuggerSystem.Traffics;

            for (int i = 0; i < traffics.Length; i++)
            {
                if (!entityManager.Exists(traffics[i].Entity) || !entityManager.HasComponent<PhysicsCollider>(traffics[i].Entity))
                {
                    continue;
                }

                var physicsCollider = entityManager.GetComponentData<PhysicsCollider>(traffics[i].Entity);

                unsafe
                {
                    Unity.Physics.Collider* ptr = physicsCollider.ColliderPtr;
                    var filter = ptr->GetCollisionFilter();

                    var layerIndex = (filter.BelongsTo);

                    EditorExtension.DrawWorldString($"Layer: {layerIndex}", traffics[i].Position);
                }
            }
        }
    }
}
#endif
