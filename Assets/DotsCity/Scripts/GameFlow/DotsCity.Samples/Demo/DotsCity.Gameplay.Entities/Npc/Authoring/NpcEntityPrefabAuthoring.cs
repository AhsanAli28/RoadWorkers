using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc.Authoring
{
    public class NpcEntityPrefabAuthoring : MonoBehaviour
    {
        [SerializeField] protected GameObject physicsShapePrefab;

        public static void Bake<T>(IBaker baker, NpcEntityPrefabAuthoring authoring) where T : IComponentData
        {
            var entity = baker.CreateAdditionalEntity(TransformUsageFlags.None);

            baker.AddComponent(entity, new NpcPrefabComponent()
            {
                PrefabEntity = baker.GetEntity(authoring.physicsShapePrefab, TransformUsageFlags.Dynamic)
            });

            baker.AddComponent(entity, typeof(T));
        }
    }
}