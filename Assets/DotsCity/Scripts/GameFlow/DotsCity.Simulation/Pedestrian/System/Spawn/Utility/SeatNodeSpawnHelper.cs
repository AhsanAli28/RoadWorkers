using Spirit604.Gameplay.Road;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public static class SeatNodeSpawnHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetSpawnSpawnPositionAndSetCustomParams(
            ref PedestrianSpawnUtils.SpawnRequestParams spawnRequestParams,
            ref PedestrianEntitySpawnerSystem.CaptureNodeInfo captureNodeInfo)
        {
            var benchEntity = spawnRequestParams.SourceEntity;

            if (spawnRequestParams.CapacityNodeHashMap.TryGetValue(benchEntity, out int capacity))
            {
                if (capacity > 0)
                {
                    captureNodeInfo = new PedestrianEntitySpawnerSystem.CaptureNodeInfo()
                    {
                        CapturedNodeEntity = benchEntity,
                        PedestrianEntity = spawnRequestParams.PedestrianEntity,
                        PedestrianNodeType = PedestrianNodeType.Sit
                    };
                }
            }

            return spawnRequestParams.SourcePosition;
        }
    }
}