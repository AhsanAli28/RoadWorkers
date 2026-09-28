using Spirit604.Attributes;
using Spirit604.Gameplay.Weapons;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Weapon.Authoring
{
    public struct BulletEntityPrefab : IComponentData
    {
        public Entity PrefabEntity;
        public BulletType BulletType;
    }

    public class BulletEntityPrefabAuthoring : MonoBehaviourBase
    {
        [Expandable]
        [SerializeField] private BulletEntityPrefabContainer bulletEntityPrefabContainer;

        class BulletEntityPrefabBaker : Baker<BulletEntityPrefabAuthoring>
        {
            public override void Bake(BulletEntityPrefabAuthoring authoring)
            {
                DependsOn(authoring.bulletEntityPrefabContainer);

                var bulletEntityDataDictionary = authoring.bulletEntityPrefabContainer.BulletEntityDataDictionary;

                foreach (var data in bulletEntityDataDictionary)
                {
                    var prefabContainerEntity = CreateAdditionalEntity(TransformUsageFlags.None);

                    AddComponent(prefabContainerEntity, new BulletEntityPrefab()
                    {
                        PrefabEntity = GetEntity(data.Value, TransformUsageFlags.Dynamic),
                        BulletType = data.Key
                    });
                }
            }
        }
    }
}