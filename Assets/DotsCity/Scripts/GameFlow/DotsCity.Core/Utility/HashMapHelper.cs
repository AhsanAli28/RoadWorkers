using Unity.Collections;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Core
{
    public static class HashMapHelper
    {
        public const float DEFAULT_CELL_RADIUS = 10f;

        public static int GetHashMapPosition(float3 position, float cellRadius = DEFAULT_CELL_RADIUS)
        {
            return (int)math.hash(GetHashPosition(position, cellRadius));
        }

        public static int3 GetHashPosition(float3 position, float cellRadius = DEFAULT_CELL_RADIUS)
        {
            return new int3(math.floor(position / cellRadius));
        }

        public static int GetHashMapRoundPosition(float3 position, float cellRadius = DEFAULT_CELL_RADIUS)
        {
            return (int)math.hash(GetHashPosition(position, cellRadius));
        }

        public static int3 GetHashRoundPosition(float3 position, float cellRadius = DEFAULT_CELL_RADIUS)
        {
            return new int3(math.round(position / cellRadius));
        }

        public static float3 GetCellPosition(float3 position, float cellRadius = DEFAULT_CELL_RADIUS)
        {
            return (float3)GetHashPosition(position, cellRadius) * cellRadius + new float3(cellRadius / 2, 0, cellRadius / 2);
        }

        //  *
        //* * *
        //  *
        public static NativeList<int> GetHashMapPosition5Cells(float3 position, float cellRadius = DEFAULT_CELL_RADIUS, float offset = DEFAULT_CELL_RADIUS, Allocator allocator = Allocator.Temp)
        {
            NativeList<int> list = new NativeList<int>(9, allocator);

            var key = GetHashMapPosition(position, cellRadius);

            list.Add(key);
            var tempPos = position + new float3(offset, 0, 0);

            key = GetHashMapPosition(tempPos, cellRadius);
            TryToAddKey(list, key);

            tempPos = position + new float3(-offset, 0, 0);

            key = GetHashMapPosition(tempPos, cellRadius);
            TryToAddKey(list, key);

            tempPos = position + new float3(0, 0, offset);

            key = GetHashMapPosition(tempPos, cellRadius);
            TryToAddKey(list, key);

            tempPos = position + new float3(0, 0, -offset);

            key = GetHashMapPosition(tempPos, cellRadius);
            TryToAddKey(list, key);

            return list;
        }

        //* * *
        //* * *
        //* * *
        public static NativeList<int> GetHashMapPosition9Cells(float3 position, float cellRadius = DEFAULT_CELL_RADIUS, float offset = DEFAULT_CELL_RADIUS, Allocator allocator = Allocator.Temp)
        {
            var list = GetHashMapPosition5Cells(position, cellRadius, offset, allocator);

            var tempPos = position + new float3(-offset, 0, offset);

            var key = GetHashMapPosition(tempPos, cellRadius);
            TryToAddKey(list, key);

            tempPos = position + new float3(offset, 0, offset);
            key = GetHashMapPosition(tempPos, cellRadius);
            TryToAddKey(list, key);

            tempPos = position + new float3(-offset, 0, -offset);
            key = GetHashMapPosition(tempPos, cellRadius);
            TryToAddKey(list, key);

            tempPos = position + new float3(offset, 0, -offset);
            key = GetHashMapPosition(tempPos, cellRadius);
            TryToAddKey(list, key);

            return list;
        }

        public static void GetHashMapPosition5Cells(ref NativeList<int> keys, float3 position, float cellRadius = DEFAULT_CELL_RADIUS, float offset = DEFAULT_CELL_RADIUS)
        {
            var key = GetHashMapPosition(position, cellRadius);

            keys.Add(key);
            var tempPos = position + new float3(offset, 0, 0);

            key = GetHashMapPosition(tempPos, cellRadius);
            TryToAddKey(keys, key);

            tempPos = position + new float3(-offset, 0, 0);

            key = GetHashMapPosition(tempPos, cellRadius);
            TryToAddKey(keys, key);

            tempPos = position + new float3(0, 0, offset);

            key = GetHashMapPosition(tempPos, cellRadius);
            TryToAddKey(keys, key);

            tempPos = position + new float3(0, 0, -offset);

            key = GetHashMapPosition(tempPos, cellRadius);
            TryToAddKey(keys, key);
        }

        public static void GetHashMapPosition9Cells(ref NativeList<int> keys, float3 position, float cellRadius = DEFAULT_CELL_RADIUS, float offset = DEFAULT_CELL_RADIUS)
        {
            GetHashMapPosition5Cells(ref keys, position, cellRadius, offset);

            var tempPos = position + new float3(-offset, 0, offset);

            var key = GetHashMapPosition(tempPos, cellRadius);
            TryToAddKey(keys, key);

            tempPos = position + new float3(offset, 0, offset);
            key = GetHashMapPosition(tempPos, cellRadius);
            TryToAddKey(keys, key);

            tempPos = position + new float3(-offset, 0, -offset);
            key = GetHashMapPosition(tempPos, cellRadius);
            TryToAddKey(keys, key);

            tempPos = position + new float3(offset, 0, -offset);
            key = GetHashMapPosition(tempPos, cellRadius);
            TryToAddKey(keys, key);
        }

        public static void TryToAddKey(NativeList<int> list, int key)
        {
            if (!list.Contains(key))
            {
                list.Add(key);
            }
        }
    }
}
