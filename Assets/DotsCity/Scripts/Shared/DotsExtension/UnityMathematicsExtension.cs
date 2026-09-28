using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Spirit604.Extensions
{
    public static class UnityMathematicsExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 RandomPointInBox(Vector3 center, Vector3 size, Random rndGen)
        {
            return center + new Vector3(
               (rndGen.NextFloat(0f, 1f) - 0.5f) * size.x,
               (rndGen.NextFloat(0f, 1f) - 0.5f) * size.y,
               (rndGen.NextFloat(0f, 1f) - 0.5f) * size.z
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Flat(this float3 vector)
        {
            return new float3(vector.x, 0, vector.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBetween3DSpace(float3 a, float3 b, float3 c)
        {
            // Entire line segment
            float3 ab = b - a;
            // The intersection and the first point
            float3 ac = c - a;

            // Need to check 2 things: 
            // 1. If the vectors are pointing in the same direction = if the dot product is positive
            // 2. If the length of the vector between the intersection and the first point is smaller than the entire line

            if (math.dot(ab, ac) > 0f && math.lengthsq(ab) >= math.lengthsq(ac))
            {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEqual(this float3 lhs, float3 rhs)
        {
            return IsEqual(lhs, rhs, 0.0001f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEqual(this float3 lhs, float3 rhs, float precision)
        {
            return math.lengthsq(lhs - rhs) < precision;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 RotatePointRelative(float3 center, float3 point, quaternion rotation)
        {
            var v = point - center; // Point to zero space
            v = math.mul(rotation, v); // Rotate
            v = v + center; // Point to origin space

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetRandomPointInRectangle(Random rndGen, float3 center, quaternion rotation, float width, float height)
        {
            var randWidth = rndGen.NextFloat(-width / 2, width / 2);
            var randheight = rndGen.NextFloat(-height / 2, height / 2);

            var point = center + new float3(randWidth, 0, randheight);

            if (!rotation.Equals(quaternion.identity))
            {
                point = RotatePointRelative(center, point, rotation);
            }

            return point;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Random GetRandomGen(float timeStamp, Entity entity, int entityInQueryIndex = 0)
        {
            return GetRandomGen(timeStamp, entity.Index, entityInQueryIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Random GetRandomGen(float timeStamp, int entityIndex, int entityInQueryIndex = 0)
        {
            uint seed = GetSeed(timeStamp, entityIndex, entityInQueryIndex);

            return Random.CreateFromIndex(seed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetRandomValue(float2 rangeFloat, float timeStamp, int entityIndex, int entityInQueryIndex = 0)
        {
            return GetRandomValue(rangeFloat.x, rangeFloat.y, timeStamp, entityIndex, entityInQueryIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetRandomValue(float min, float max, float timeStamp, int entityIndex, int entityInQueryIndex = 0)
        {
            var random = GetRandomGen(timeStamp, entityIndex, entityInQueryIndex);
            return random.NextFloat(min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Random GetRandomGen(float timeStamp, int entityIndex, float3 position, int entityInQueryIndex = 0)
        {
            uint seed = GetSeed(timeStamp, entityIndex, position, entityInQueryIndex);

            return Random.CreateFromIndex(seed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetSeed(float timeStamp, int entityIndex, int entityInQueryIndex = 0)
        {
            unchecked
            {
                uint t = (uint)math.floor(timeStamp) + 1000;

                t = ((t & 0xff000000) >> 24) +
                 ((t & 0x00ff0000) >> 8) +
                 ((t & 0x0000ff00) << 8) +
                 ((t & 0x000000ff) << 24);

                uint index = (uint)(entityIndex * 148 * (entityInQueryIndex + 1)) ^ 397;
                return (t + index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetSeed(float timeStamp, int entityIndex, float3 position, int entityInQueryIndex = 0)
        {
            unchecked
            {
                return (GetSeed(timeStamp, entityIndex, entityInQueryIndex) * 397) ^ (uint)math.csum(position);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ProjectOnPlane(float3 srcVector, float3 planeNormal) => srcVector - math.dot(srcVector, planeNormal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 NearestPointOnLine(float3 startLinePoint, float3 endLinePoint, float3 sourcePoint)
        {
            var lineDirection = math.normalize(endLinePoint - startLinePoint);
            var v = sourcePoint - startLinePoint;
            var d = math.dot(v, lineDirection);
            return startLinePoint + lineDirection * d;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 To2DSpace(this float3 vector)
        {
            return new float2(vector.x, vector.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 To3DSpace(this float2 vector)
        {
            return new float3(vector.x, 0, vector.y);
        }

#if UNITY_EDITOR
        public static bool DrawGizmosRotatedCube(float3 position, quaternion rotation, Bounds bounds, Color color)
        {
            if (math.isnan(position.x) || math.isnan(position.y) || math.isnan(position.z) || rotation.value.Equals(float4.zero))
            {
                return false;
            }

            rotation = math.normalizesafe(rotation);

            var matrix = Gizmos.matrix;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(position, rotation, Vector3.one);

            var oldColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            Gizmos.matrix = matrix;
            Gizmos.color = oldColor;

            return true;
        }

        public static bool DrawSceneViewRotatedCube(float3 position, quaternion rotation, Bounds bounds, Color color)
        {
            if (math.isnan(position.x) || math.isnan(position.y) || math.isnan(position.z) || rotation.value.Equals(float4.zero))
            {
                return false;
            }

            rotation = math.normalizesafe(rotation);

            var matrix = UnityEditor.Handles.matrix;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(position, rotation, Vector3.one);

            var oldColor = UnityEditor.Handles.color;
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.matrix = rotationMatrix;
            UnityEditor.Handles.DrawWireCube(bounds.center, bounds.size);
            UnityEditor.Handles.matrix = matrix;
            UnityEditor.Handles.color = oldColor;

            return true;
        }
#endif

    }
}