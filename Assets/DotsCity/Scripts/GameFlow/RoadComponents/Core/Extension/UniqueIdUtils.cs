using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    internal static class UniqueIdUtils
    {
        private const float HashSize = 0.25f;

        public static int GetUniqueID(Component component, Vector3 pos, float hashSize = HashSize)
        {
            var hash = GetHashMapPosition(pos, hashSize);
            var instanceId = component.GetInstanceID();

            unchecked
            {
                return instanceId + (hash << 16);
            }
        }

        public static int GetHashMapPosition(float3 position, float hashSize = HashSize)
        {
            return (int)math.hash(GetHashPosition(position, hashSize));
        }

        public static int3 GetHashPosition(float3 position, float hashSize = HashSize)
        {
            return new int3(math.floor(position / hashSize));
        }

        public static float3 GetCellPosition(float3 position, float hashSize = HashSize)
        {
            return (float3)GetHashPosition(position, hashSize) * hashSize + new float3(hashSize / 2, 0, hashSize / 2);
        }
    }
}
