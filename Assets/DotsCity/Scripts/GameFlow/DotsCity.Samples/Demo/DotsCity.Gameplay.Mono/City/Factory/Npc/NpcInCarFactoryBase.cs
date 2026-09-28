using Spirit604.Gameplay.Npc;

namespace Spirit604.Gameplay.Factory.Npc
{
    public class NpcInCarFactoryBase : NpcMonoFactoryBase<NpcInCar>, INpcInCarFactory
    {
        public INpcInCar GetNpc(string key)
        {
            return Get(key);
        }
    }
}