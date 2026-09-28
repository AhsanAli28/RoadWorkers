using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Factory;
using Spirit604.Gameplay.Factory.Npc;
using Spirit604.Gameplay.Npc;
using System;

namespace Spirit604.DotsCity.Gameplay.Factory.Player
{
    public abstract class NpcInteractCarServiceBase : NpcSimpleInteractCarServiceBase
    {
        protected readonly INpcInCarFactory npcInCarFactory;

        protected NpcInteractCarServiceBase(INpcInCarFactory npcInCarFactory, INpcFactory npcFactory) : base(npcFactory)
        {
            this.npcInCarFactory = npcInCarFactory;
        }

        protected virtual INpcInCar GetNewNpc(CarSlot sourceSlot, string npcId, Action<CarSlot> onEnter)
        {
            var newNpc = npcInCarFactory.GetNpc(npcId);

            EnterSlot(sourceSlot, newNpc, onEnter);
            return newNpc;
        }
    }
}