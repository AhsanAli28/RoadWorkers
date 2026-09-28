using Spirit604.DotsCity.Core;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Common
{
    public struct FactionTypeComponent : IComponentData
    {
        public FactionType Value;
        //0 - all, 1 player 2 police & ped 3 mafia
    }
}