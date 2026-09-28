using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Weapon.Authoring
{
    public class BulletEntityAuthoring : MonoBehaviour
    {
        [SerializeField][Range(0, 10f)] private float flySpeed;
        [SerializeField][Range(0, 100f)] private float lifeTime = 6f;
        [SerializeField][Range(0, 100)] private int damageValue = 1;
        [SerializeField] private TrailRenderer trailRenderer;

        class BulletEntityAuthoringBaker : Baker<BulletEntityAuthoring>
        {
            public override void Bake(BulletEntityAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(entity, typeof(BulletComponent));

                PoolEntityUtils.AddPoolComponents(this, entity, EntityWorldType.PureEntity);

                AddComponent(entity, new BulletStatsComponent()
                {
                    FlySpeed = authoring.flySpeed,
                    LifeTime = authoring.lifeTime,
                    Damage = authoring.damageValue
                });
            }
        }
    }
}