using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public static class RandomHelper
    {
        public static bool ChanceDropped(int chance)
        {
            if (chance >= 100)
                return true;

            if (chance <= 0)
                return false;

            int randValue = Random.Range(0, 100);

            return randValue < chance;
        }

        public static bool ChanceDropped(float chance)
        {
            if (chance >= 1f)
                return true;

            if (chance <= 0f)
                return false;

            float randValue = Random.Range(0f, 1f);
            return randValue < chance;
        }

        public static Vector3 RandomPointInBox(Vector3 center, Vector3 size)
        {
            return center + new Vector3(
               (Random.value - 0.5f) * size.x,
               (Random.value - 0.5f) * size.y,
               (Random.value - 0.5f) * size.z
            );
        }
    }
}