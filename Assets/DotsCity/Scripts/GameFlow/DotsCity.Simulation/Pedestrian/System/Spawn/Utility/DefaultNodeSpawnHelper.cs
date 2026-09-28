using Spirit604.DotsCity.Simulation.Pedestrian.State;
using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public static class DefaultNodeSpawnHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetSpawnPositionAndSetCustomParams(ref EntityCommandBuffer commandBuffer, ref PedestrianSpawnUtils.SpawnRequestParams spawnRequestParams, float3 sourcePosition, float3 targetPosition, Random randomGen)
        {
            float3 spawnPosition;
            int sourceCrosswalkIndex = spawnRequestParams.NodeLightSettingsLookup[spawnRequestParams.SourceEntity].CrosswalkIndex;
            int targetCrosswalkIndex = spawnRequestParams.NodeLightSettingsLookup[spawnRequestParams.DstEntity].CrosswalkIndex;
            var targetSettings = spawnRequestParams.TargetNodeSettings;

            bool sameCrossWalk = sourceCrosswalkIndex == targetCrosswalkIndex && sourceCrosswalkIndex != -1;
            bool allowRandomize = !sameCrossWalk && targetSettings.ChanceToSpawn > 0;

            if (!allowRandomize)
            {
                spawnPosition = sourcePosition;
                commandBuffer.SetComponentEnabled<HasTargetTag>(spawnRequestParams.PedestrianEntity, false);
                commandBuffer.SetComponentEnabled<IdleTag>(spawnRequestParams.PedestrianEntity, true);
                commandBuffer.SetComponent(spawnRequestParams.PedestrianEntity, new NextStateComponent(ActionState.WaitForGreenLight));
            }
            else
            {
                AddDefaultMovementState(ref commandBuffer, ref spawnRequestParams);
                spawnPosition = PedestrianSpawnUtils.GetRandomSpawnPosition(sourcePosition, targetPosition, randomGen);
            }

            return spawnPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddDefaultMovementState(ref EntityCommandBuffer commandBuffer, ref PedestrianSpawnUtils.SpawnRequestParams spawnRequestParams)
        {
            commandBuffer.SetComponent(spawnRequestParams.PedestrianEntity, new NextStateComponent(ActionState.MovingToNextTargetPoint));
        }
    }
}
