using Spirit604.Gameplay.Factory;
using Spirit604.Gameplay.Factory.Npc;

namespace Spirit604.DotsCity.Gameplay.Factory.Player
{
    public class PlayerCustomNpcExitCarService : NpcInteractCarServiceBase
    {
        public PlayerCustomNpcExitCarService(INpcInCarFactory npcInCarFactory, INpcFactory npcFactory) : base(npcInCarFactory, npcFactory)
        {
        }
    }
}