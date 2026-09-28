using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public static class BlobCurveUtils
    {
        public static float Evaluate(ref BlobArray<float> values, float time)
        {
            int index = (int)math.floor((values.Length - 1) * time);
            index = math.clamp(index, 0, values.Length - 1);

            return values[index];
        }

        public static float[] GenerateCurveArray(AnimationCurve animationCurve, int stepCount)
        {
            float[] returnArray = new float[stepCount];

            for (int j = 0; j < stepCount; j++)
            {
                returnArray[j] = animationCurve.Evaluate(((float)j) / stepCount);
            }

            return returnArray;
        }
    }
}