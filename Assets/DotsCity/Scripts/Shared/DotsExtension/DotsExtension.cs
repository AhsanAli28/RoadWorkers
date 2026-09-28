using Unity.Collections;
using Unity.Entities;

namespace Spirit604.Extensions
{
    public static class DotsExtension
    {
        public static void EnsureCapacity<T>(ref this NativeList<T> srcList, EntityQuery targetEntityQuery, bool doubleCapacity = true) where T : unmanaged
        {
            var queryEntityCount = targetEntityQuery.CalculateEntityCount();

            if (srcList.Capacity < queryEntityCount)
            {
                var newCapacity = queryEntityCount;

                if (doubleCapacity)
                {
                    newCapacity *= 2;
                }

                srcList.SetCapacity(newCapacity);
            }
        }
    }
}