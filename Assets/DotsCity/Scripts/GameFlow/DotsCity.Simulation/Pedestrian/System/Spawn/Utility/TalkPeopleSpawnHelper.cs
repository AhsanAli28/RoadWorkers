using Spirit604.Extensions;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public static class TalkPeopleSpawnHelper
    {
        private const float DISTANCE_FROM_SPAWN_POINT = 0.6f;
        private const float MAX_RANDOM_DEVIATION_ANGLE = 20F;
        private const float MAX_SHIFT_FROM_SPAWN_AXIS = 0.2f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetSpawnPosition(int index, int randomAngle, Vector3 axisPosition, uint baseSeed, ref int counter, out Vector3 spawnPosition, out Quaternion spawnRotation)
        {
            int axisSide = index % 2 == 0 ? 1 : -1;

            int rotator = counter == 2 || counter == 3 ? 1 : 0;

            counter = ++counter % 3;

            var rndGen = new Unity.Mathematics.Random(baseSeed);

            float randomDeviationAngle = rndGen.NextFloat(-MAX_RANDOM_DEVIATION_ANGLE, MAX_RANDOM_DEVIATION_ANGLE);

            var newSeed = MathUtilMethods.ModifySeed(baseSeed, (int)math.floor(randomDeviationAngle));
            rndGen.InitState(newSeed);

            float randomShiftFromAxis = rndGen.NextFloat(-MAX_SHIFT_FROM_SPAWN_AXIS, MAX_SHIFT_FROM_SPAWN_AXIS);

            spawnPosition = axisPosition + Quaternion.Euler(0, randomAngle - rotator * 90 + randomDeviationAngle, 0) * Vector3.forward * (DISTANCE_FROM_SPAWN_POINT + randomShiftFromAxis) * axisSide;
            spawnRotation = Quaternion.LookRotation((axisPosition.Flat() - spawnPosition.Flat()).normalized);
        }
    }
}