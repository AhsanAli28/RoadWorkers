using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public struct PlayerInteractCarStateComponent : IComponentData
    {
        public PlayerInteractCarState PlayerInteractCarState;
    }

    public class PlayerInteractCarStateAuthoring : MonoBehaviour
    {
        class PlayerInteractCarStateBaker : Baker<PlayerInteractCarStateAuthoring>
        {
            public override void Bake(PlayerInteractCarStateAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, typeof(PlayerInteractCarStateComponent));
            }
        }
    }
}