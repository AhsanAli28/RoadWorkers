using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    [CreateAssetMenu(menuName = HybridComponentBase.BasePath + "PlayerCarHybridComponent")]
    public class PlayerCarHybridComponent : HybridComponentBase, IRuntimeEntityComponentSetProvider
    {
        ComponentType[] IRuntimeEntityComponentSetProvider.GetComponentSet()
        {
            return new ComponentType[] {
                ComponentType.ReadOnly<AliveTag>(),
                ComponentType.ReadOnly<CarTag>(),
                ComponentType.ReadOnly<PlayerTag>(),
            };
        }
    }
}
