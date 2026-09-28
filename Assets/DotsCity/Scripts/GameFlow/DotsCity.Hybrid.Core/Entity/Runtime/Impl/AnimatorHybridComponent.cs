using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Hybrid.Core
{
    [CreateAssetMenu(menuName = HybridComponentBase.BasePath + "AnimatorHybrid")]
    public class AnimatorHybridComponent : HybridComponentBase, IRuntimeEntityComponentSetProvider, IRuntimeInitEntity
    {
        public ComponentType[] GetComponentSet() => new ComponentType[] { typeof(Animator) };

        public void Initialize(EntityManager entityManager, GameObject root, Entity entity)
        {
            entityManager.AddComponentObject(entity, root.GetComponent<Animator>());
        }
    }
}
