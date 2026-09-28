using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace Spirit604.AnimationBaker.Entities.Utils
{
    public static class UnityMathematicsExtension
    {
        public static bool ChanceDropped(float chance, Random randomGenerator)
        {
            if (chance >= 1f)
            {
                return true;
            }
            if (chance <= 0f)
            {
                return false;
            }

            float randValue = randomGenerator.NextFloat(0, 1f);

            return randValue < chance;
        }

        public static float3 Flat(this float3 vector)
        {
            return new float3(vector.x, 0, vector.z);
        }

        public static bool IsEqual(this float3 lhs, float3 rhs)
        {
            return IsEqual(lhs, rhs, 0.0001f);
        }

        public static bool IsEqual(this float3 lhs, float3 rhs, float precision)
        {
            return math.lengthsq(lhs - rhs) < precision;
        }

        public static Random GetRandomGen(float timeStamp, int entityIndex, int entityInQueryIndex = 0)
        {
            uint seed = GetSeed(timeStamp, entityIndex, entityInQueryIndex);

            return Random.CreateFromIndex(seed);
        }

        public static uint GetSeed(float timeStamp, int entityIndex, int entityInQueryIndex = 0)
        {
            unchecked
            {
                return (uint)((int)math.floor(timeStamp) + entityIndex ^ 397 + entityInQueryIndex);
            }
        }
    }
}