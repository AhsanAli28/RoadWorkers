using Spirit604.Extensions;
using System.Runtime.CompilerServices;

namespace Spirit604.DotsCity.Simulation.Traffic.Obstacle
{
    public static class CarHashDataExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasState(in this CarHashData carHashData, State state) => DotsEnumExtension.HasFlagUnsafe<State>(carHashData.States, state);
    }
}