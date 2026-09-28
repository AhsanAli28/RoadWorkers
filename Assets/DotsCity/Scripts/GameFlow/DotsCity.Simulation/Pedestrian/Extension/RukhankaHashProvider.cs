#if RUKHANKA
using Rukhanka.Toolbox;
using FixedStringName = Unity.Collections.FixedString512Bytes;
#endif

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
#if RUKHANKA
    public class RukhankaHashProvider : IAnimationHashProvider
    {
        public int GetHash(string name) => (int)(new FixedStringName(name).CalculateHash32());
    }
#else
    public class RukhankaHashProvider : DefaultHashProvider
    {
    }
#endif
}
