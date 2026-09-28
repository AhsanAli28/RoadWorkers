using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Npc
{
    public struct NpcTypeComponent : IComponentData
    {
        public NpcType Type;
        //-1 player //0 = Pedestrian static&dynamic //1 - any custom npc
    }

    public enum NpcType { Default, Player, Pedestrian, Npc }
}