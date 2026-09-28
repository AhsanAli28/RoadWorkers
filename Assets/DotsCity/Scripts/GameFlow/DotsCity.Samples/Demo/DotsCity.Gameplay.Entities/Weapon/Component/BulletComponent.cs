using Spirit604.DotsCity.Core;
using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Weapon
{
    public struct BulletComponent : IComponentData
    {
        public FactionType FactionType;
    }
}