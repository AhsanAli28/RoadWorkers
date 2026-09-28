using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadGeneration
{
    public static class RSGeneratorExtension
    {
        private const float HashSize = 1f;

        public static int GetHash(Vector3 position, float hashSize = HashSize)
        {
            return (int)math.hash(GetHashPosition(position, hashSize));
        }

        public static int3 GetHashPosition(float3 position, float hashSize = HashSize)
        {
            return new int3(math.floor(position / hashSize));
        }
    }
}
