using Spirit604.DotsCity.Core;
using Unity.Entities;
using Unity.Physics.Stateful;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player.Authoring
{
    public class PlayerNpcAuthoring : MonoBehaviour
    {
        public class PlayerNpcBaker : Baker<PlayerNpcAuthoring>
        {
            public override void Bake(PlayerNpcAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(entity, new PlayerNpcComponent()
                {
                    AvailableCarEntityIndex = -1
                });

                AddComponent(entity, typeof(PlayerTag));
                AddComponent(entity, typeof(PlayerPhysicsShapeComponent));
                AddComponent(entity, typeof(PlayerMobTag));

                AddBuffer<StatefulTriggerEvent>(entity);
            }
        }
    }
}