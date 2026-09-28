using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public class PlayerTargetAuthoring : MonoBehaviour
    {
        public float ScaleRadius;

        public class PrefabDataComponentBaker : Baker<PlayerTargetAuthoring>
        {
            public override void Bake(PlayerTargetAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new PlayerTargetComponent { ScaleRadius = authoring.ScaleRadius });
            }
        }
    }
}