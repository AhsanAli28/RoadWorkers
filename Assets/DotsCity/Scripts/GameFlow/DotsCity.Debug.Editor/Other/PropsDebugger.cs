#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Level.Props;
using Spirit604.Extensions;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class PropsDebugger : MonoBehaviour
    {
        [SerializeField] private bool enableDebug;

        private EntityManager entityManager;
        private EntityQuery propsQuery;

        private void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            propsQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PropsComponent>());
        }

        private void OnDrawGizmos()
        {
            if (!enableDebug || !Application.isPlaying)
            {
                return;
            }

            var props = propsQuery.ToComponentDataArray<PropsComponent>(Unity.Collections.Allocator.TempJob);
            var propsEntity = propsQuery.ToEntityArray(Unity.Collections.Allocator.TempJob);

            for (int i = 0; i < props.Length; i++)
            {
                var position = props[i].InitialPosition;
                bool damaged = entityManager.HasComponent<PropsDamagedTag>(propsEntity[i]) && entityManager.IsComponentEnabled<PropsDamagedTag>(propsEntity[i]);
                string text = $"Damaged {damaged}";

                EditorExtension.DrawWorldString(text, position);
            }

            props.Dispose();
            propsEntity.Dispose();
        }
    }
}
#endif
