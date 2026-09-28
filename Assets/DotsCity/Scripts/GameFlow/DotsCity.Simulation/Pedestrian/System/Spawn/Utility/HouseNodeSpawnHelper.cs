using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public static class HouseNodeSpawnHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetSpawnPositionAndSetCustomParams(ref EntityCommandBuffer commandBuffer, ref PedestrianSpawnUtils.SpawnRequestParams spawnRequestParams)
        {
            DefaultNodeSpawnHelper.AddDefaultMovementState(ref commandBuffer, ref spawnRequestParams);
            return spawnRequestParams.WorldTransformLookup[spawnRequestParams.SourceEntity].Position;
        }
    }
}