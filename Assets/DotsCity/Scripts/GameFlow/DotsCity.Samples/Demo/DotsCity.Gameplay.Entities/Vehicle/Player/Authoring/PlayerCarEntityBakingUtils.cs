using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.NavMesh;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.DotsCity.Simulation.Traffic.Authoring;
using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Player.Authoring
{
    public static class PlayerCarEntityBakingUtils
    {
        public static void Bake(
            Entity prefabEntity,
            in RaycastConfigReference raycastConfigReference,
            in TrafficCommonSettingsConfigBlobReference trafficCommonSettingsConfigBlobReference,
            in TrafficSettingsConfigBlobReference trafficSettingsConfigBlobReference,
            in GeneralCoreSettingsDataReference generalCoreSettingsDataReference,
            ref EntityManager entityManager,
            ref EntityCommandBuffer commandBuffer)
        {
            commandBuffer.AddComponent(prefabEntity, typeof(VehicleInputReader));

            if (!trafficSettingsConfigBlobReference.Reference.Value.HasNavMeshObstacle)
            {
                if (entityManager.HasComponent<NavMeshObstacleData>(prefabEntity))
                {
                    commandBuffer.RemoveComponent<NavMeshObstacleData>(prefabEntity);
                    commandBuffer.RemoveComponent<NavMeshObstacleLoadTag>(prefabEntity);
                }
            }

            switch (generalCoreSettingsDataReference.Config.Value.WorldSimulationType)
            {
                case WorldSimulationType.DOTS:
                    commandBuffer.AddComponent(prefabEntity, typeof(InterpolateTransformData));
                    break;
                case WorldSimulationType.HybridMono:
                    commandBuffer.AddComponent(prefabEntity, typeof(CopyTransformFromGameObject));
                    break;
            }

            if (generalCoreSettingsDataReference.Config.Value.DOTSSimulation)
            {
                if (trafficCommonSettingsConfigBlobReference.Reference.Value.DetectObstacleMode != DetectObstacleMode.CalculateOnly)
                {
                    TrafficEntityBakingUtils.CheckForPhysicsLayer(entityManager, prefabEntity, in raycastConfigReference, "PlayerCarEntityBakingSystem", "player car");
                }
            }
        }
    }
}