using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public static class FlagUtils
    {
        public static int CountFlags(int value)
        {
            int count = 0;

            while (value != 0)
            {
                if ((value & 1) != 0) count++;
                value >>= 1;
            }

            return count;
        }

        public static void RotateTileFlags(ConnectDirection[] currentPrefabFlags, float rotation)
        {
            if (rotation < 0)
            {
                rotation += 360f;
            }

            int count = Mathf.RoundToInt(rotation / 90);
            FlagUtils.ShiftFlags(currentPrefabFlags, count);
        }

        public static void ShiftFlags(ConnectDirection[] currentPrefabFlags, int count)
        {
            for (int i = 0; i < count; i++)
            {
                FlagUtils.ShiftFlags(currentPrefabFlags);
            }
        }

        public static void ShiftFlags(ConnectDirection[] currentPrefabFlags)
        {
            for (int i = 0; i < currentPrefabFlags.Length; i++)
            {
                currentPrefabFlags[i] = ShiftFlag(currentPrefabFlags[i]);
            }
        }

        public static ConnectDirection ShiftFlag(ConnectDirection connectDirection)
        {
            switch (connectDirection)
            {
                case ConnectDirection.Left:
                    return ConnectDirection.Top;
                case ConnectDirection.Top:
                    return ConnectDirection.Right;
                case ConnectDirection.Right:
                    return ConnectDirection.Bottom;
                case ConnectDirection.Bottom:
                    return ConnectDirection.Left;
            }

            return connectDirection;
        }

        public static ConnectDirection GetOppositeFlag(ConnectDirection connectDirection)
        {
            switch (connectDirection)
            {
                case ConnectDirection.Left:
                    return ConnectDirection.Right;
                case ConnectDirection.Top:
                    return ConnectDirection.Bottom;
                case ConnectDirection.Right:
                    return ConnectDirection.Left;
                case ConnectDirection.Bottom:
                    return ConnectDirection.Top;
            }

            return connectDirection;
        }
    }
}
