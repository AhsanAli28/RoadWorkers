using Spirit604.Gameplay.Npc;

namespace Spirit604.Gameplay.Factory.Npc
{
    public interface INpcInCarFactory
    {
        INpcInCar GetNpc(string key);
    }
}