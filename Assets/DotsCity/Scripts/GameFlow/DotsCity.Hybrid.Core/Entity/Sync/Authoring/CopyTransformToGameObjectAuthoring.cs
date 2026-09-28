using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Hybrid.Core
{
    [RequireComponent(typeof(RuntimeEntityAuthoring))]
    public class CopyTransformToGameObjectAuthoring : MonoBehaviour, IRuntimeEntityComponentSetProvider, IRuntimeInitEntity
    {
        public ComponentType[] GetComponentSet() => new ComponentType[] { typeof(CopyTransformToGameObject) };

        public void Initialize(EntityManager entityManager, GameObject root, Entity entity)
        {
            entityManager.AddComponentObject(entity, root.transform);
        }
    }
}