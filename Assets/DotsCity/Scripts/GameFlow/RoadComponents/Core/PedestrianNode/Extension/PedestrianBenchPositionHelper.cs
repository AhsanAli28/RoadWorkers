using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Spirit604.Gameplay.Road
{
    public static class PedestrianBenchPositionHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetSeatPosition(int seatNumber, int seatsCount, float seatOffset, float3 InitialPosition, float3 baseOffset, quaternion InitialRotation)
        {
            if (seatsCount == 1)
            {
                return InitialPosition + math.mul(InitialRotation, baseOffset);
            }

            bool isEven = seatsCount % 2 == 0;

            int sideSeatsCount = (int)math.floor((float)seatsCount / 2);

            float sideOffset = 0;

            if (isEven)
            {
                float sideCount = (float)sideSeatsCount - 0.5f;
                int side = seatNumber < sideCount ? -1 : 1;

                int seatCount = (int)(math.floor(math.abs(sideCount - seatNumber)));
                sideOffset = side * (seatOffset * 0.5f + seatOffset * seatCount);
            }
            else
            {
                sideOffset = seatOffset * (seatNumber - sideSeatsCount);
            }

            float3 seatPosition = InitialPosition + math.mul(InitialRotation, baseOffset + new float3(sideOffset, 0, 0));

            return seatPosition;
        }
    }
}