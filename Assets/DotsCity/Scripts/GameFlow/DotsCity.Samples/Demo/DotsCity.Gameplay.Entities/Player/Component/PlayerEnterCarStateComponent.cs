using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public struct PlayerEnterCarStateComponent : IComponentData
    {
        public EnterCarState EnterCarState;
    }

    public enum EnterCarState
    {
        Default = 0,
        Leave = -1,
        Enter = 1
    }
}