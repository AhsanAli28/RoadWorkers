using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Spirit604.Extensions
{
    public unsafe static class NativeCollectionExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T ElementAt<T>(this NativeArray<T> array, int index)
                where T : unmanaged
        {
            CheckElementWriteAccess(array, index);
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T ElementAtRO<T>(this NativeArray<T> array, int index) where T : unmanaged
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafeReadOnlyPtr(), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T ElementAtRO<T>(this NativeList<T> array, int index) where T : unmanaged
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafeReadOnlyPtr(), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T ElementAtRO<T>(this UnsafeList<T> array, int index) where T : unmanaged
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.Ptr, index);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckElementWriteAccess<T>(NativeArray<T> array, int index) where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (index < 0 || index >= array.Length)
            {
                FailOutOfRangeError(index, array.Length);
            }

            AtomicSafetyHandle.CheckWriteAndThrow(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(array));
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckElementReadAccess<T>(NativeArray<T>.ReadOnly array, int index) where T : unmanaged
        {
            // TODO we need a better approach here
            var a = array[index];
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void FailOutOfRangeError(int index, int length)
        {
            throw new IndexOutOfRangeException($"Index {index} is out of range of '{length}' Length.");
        }
    }
}