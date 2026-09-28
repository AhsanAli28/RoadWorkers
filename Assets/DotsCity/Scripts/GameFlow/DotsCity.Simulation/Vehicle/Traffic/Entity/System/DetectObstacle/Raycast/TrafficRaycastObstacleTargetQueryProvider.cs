using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public static class TrafficRaycastObstacleTargetQueryProvider
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EntityQuery GetTargetQuery(
            EntityManager entityManager,
            in TrafficGeneralSettingsReference trafficGeneralSettingsReference,
            DetectObstacleMode trafficDetectObstacleMode,
            out bool hasCustomTargets)
        {
            hasCustomTargets = false;
            EntityQueryBuilder targetGroupQueryDesc = default;
            bool created = false;

            switch (trafficDetectObstacleMode)
            {
                case DetectObstacleMode.Hybrid:
                    {
                        if (!trafficGeneralSettingsReference.Config.Value.AvoidanceSupport)
                        {
                            created = true;
                            targetGroupQueryDesc = new EntityQueryBuilder(Allocator.Temp)
                                .WithAny<TrafficCustomRaycastTargetTag, TrafficCollidedTag>()
                                .WithAll<LocalToWorld>();
                        }
                        else
                        {
                            created = true;
                            targetGroupQueryDesc = new EntityQueryBuilder(Allocator.Temp)
                                .WithAny<TrafficCustomRaycastTargetTag>()
                                .WithAll<LocalToWorld>();
                        }

                        break;
                    }
                case DetectObstacleMode.RaycastOnly:
                    {
                        break;
                    }
            }

            if (created)
            {
                hasCustomTargets = true;
                var targetGroupQuery = entityManager.CreateEntityQuery(targetGroupQueryDesc);

                return targetGroupQuery;
            }

            return default;
        }
    }
}