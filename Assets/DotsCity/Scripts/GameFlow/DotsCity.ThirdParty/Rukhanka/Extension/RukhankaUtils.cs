using Rukhanka.Toolbox;
using System.Runtime.CompilerServices;
using FixedStringName = Unity.Collections.FixedString512Bytes;

namespace Spirit604.DotsCity.ThirdParty.RukhankaEngine
{
    public static class RukhankaUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetHash(string name) => new FixedStringName(name).CalculateHash32();
    }
}
