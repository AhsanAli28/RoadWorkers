using Spirit604.Gameplay.Inventory;
using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Level
{
    public struct ItemComponent : IComponentData
    {
        public int ItemId;
        public ItemType ItemType;
    }

    public struct ItemTakenTag : IComponentData, IEnableableComponent
    {
    }
}