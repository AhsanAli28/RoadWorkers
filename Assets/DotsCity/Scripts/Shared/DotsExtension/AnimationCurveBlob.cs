using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.Extensions
{
    public struct AnimationCurveBlob
    {
        private BlobArray<float> sampledValues;
        private float2 timeRange;

        public float2 TimeRange => timeRange;

        public static BlobAssetReference<AnimationCurveBlob> Build(AnimationCurve curve, int intervalCount,
            Allocator allocator)
        {
            var blobBuilder = new BlobBuilder(Allocator.Temp);

            Build(ref blobBuilder, curve, intervalCount);

            var blobRef = blobBuilder.CreateBlobAssetReference<AnimationCurveBlob>(allocator);
            blobBuilder.Dispose();

            return blobRef;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref AnimationCurveBlob Build(ref BlobBuilder blobBuilder, AnimationCurve curve, int intervalCount)
        {
            ref var root = ref blobBuilder.ConstructRoot<AnimationCurveBlob>();

            Build(ref blobBuilder, ref root, curve, intervalCount);

            return ref root;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Build(ref BlobBuilder blobBuilder, ref AnimationCurveBlob root, AnimationCurve curve, int intervalCount)
        {
            var sampledValues = blobBuilder.Allocate(ref root.sampledValues, intervalCount + 1);

            if (curve.keys.Length > 0)
            {
                var timeFrom = curve.keys[0].time;
                var timeTo = curve.keys[curve.keys.Length - 1].time;
                var timeStep = (timeTo - timeFrom) / intervalCount;

                for (int i = 0; i < intervalCount + 1; i++)
                {
                    sampledValues[i] = curve.Evaluate(timeFrom + i * timeStep);
                }

                root.timeRange = new float2(timeFrom, timeTo);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Evaluate(float time)
        {
            var intervalCount = sampledValues.Length - 1;

            var clamp01 = math.unlerp(timeRange.x, timeRange.y, math.clamp(time, timeRange.x, timeRange.y));
            var timeInInterval = clamp01 * intervalCount;
            var segmentIndex = (int)math.floor(timeInInterval);
            if (segmentIndex >= intervalCount) return sampledValues[intervalCount];

            var bottom = sampledValues[segmentIndex];
            var top = sampledValues[segmentIndex + 1];

            return math.lerp(bottom, top, timeInInterval - segmentIndex);
        }
    }
}