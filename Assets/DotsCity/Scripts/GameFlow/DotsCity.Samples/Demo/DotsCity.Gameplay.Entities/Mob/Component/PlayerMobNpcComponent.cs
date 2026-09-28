using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public struct PlayerMobNpcComponent : IComponentData
    {
        public Entity TargetCarEntity;
        public int SideIndex;
        public float NextUnlockTime;
        public float3 PreviousPlayerPosition;

        public bool ShouldEnterCar => TargetCarEntity != Entity.Null;
    }
}
