using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player.Authoring
{
    public class PlayerMobNpcAuthoring : MonoBehaviour
    {
        public class PlayerMobNpcBaker : Baker<PlayerMobNpcAuthoring>
        {
            public override void Bake(PlayerMobNpcAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, typeof(PlayerMobNpcComponent));
                AddComponent(entity, typeof(PlayerMobTag));
            }
        }
    }
}
