using Spirit604.DotsCity.Core;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Hybrid.Core
{
    [CreateAssetMenu(menuName = HybridComponentBase.BasePath + "CopyTransformFromGameObjectHybrid")]
    public class CopyTransformFromGameObjectHybridComponent : HybridComponentBase, IRuntimeEntityComponentSetProvider, IRuntimeInitEntity
    {
        public ComponentType[] GetComponentSet() => new ComponentType[] { typeof(CopyTransformFromGameObject) };

        public void Initialize(EntityManager entityManager, GameObject root, Entity entity)
        {
            entityManager.AddComponentObject(entity, root.transform);
            entityManager.SetComponentData(entity, LocalTransform.FromPositionRotation(root.transform.localPosition, root.transform.localRotation));
        }
    }
}
