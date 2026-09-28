using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Level.Props.Authoring
{
    public class PropsAuthoring : MonoBehaviourBase
    {
        [SerializeField] private bool hasCustomPropReset;

        class PropsAuthoringBaker : Baker<PropsAuthoring>
        {
            public override void Bake(PropsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, CullComponentsExtension.GetComponentSet());

                AddComponent(entity,
                   new PropsBakingData()
                   {
                       InitialPosition = authoring.transform.position,
                       InitialForward = authoring.transform.forward,
                       HasCustomPropReset = authoring.hasCustomPropReset,
                   });
            }
        }

        [Button]
        public void SnapToSurface()
        {
            const float castOffset = 3f;
            const float castDistance = 5f;

            var origin = transform.position + new Vector3(0, castOffset, 0);

            var hits = Physics.RaycastAll(origin, Vector3.down, castDistance);

            for (int i = 0; i < hits?.Length; i++)
            {
                if (hits[i].collider != null && !gameObject.GetComponentsInChildren<Collider>().Contains(hits[i].collider))
                {
#if UNITY_EDITOR
                    UnityEditor.Undo.RecordObject(transform, "Undo snap");
#endif
                    transform.position = hits[i].point;
                    break;
                }
            }
        }
    }
}
