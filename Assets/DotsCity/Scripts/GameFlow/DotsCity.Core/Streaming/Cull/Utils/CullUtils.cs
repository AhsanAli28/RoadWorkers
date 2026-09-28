using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public static class CullUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InViewOfCamera(Matrix4x4 vpMatrix, float3 wp, float viewPortOffset = 0)
        {
            // World to clip
            var temp = vpMatrix.MultiplyPoint(wp);

            // Clip to viewport
            temp += Vector3.one;
            temp /= 2;

            return temp.x >= 0 - viewPortOffset && temp.x <= 1 + viewPortOffset && temp.y >= 0 - viewPortOffset && temp.y <= 1 + viewPortOffset && temp.z > 0 && temp.z < 1;
        }
    }
}