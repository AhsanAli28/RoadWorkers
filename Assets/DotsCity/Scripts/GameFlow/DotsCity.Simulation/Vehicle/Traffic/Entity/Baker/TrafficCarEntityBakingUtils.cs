using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.NavMesh;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Car.Custom;
using Spirit604.DotsCity.Simulation.Common;
using Spirit604.DotsCity.Simulation.Config;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.Extensions;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.GraphicsIntegration;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public static class TrafficEntityBakingUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Bake(
            ref EntityCommandBuffer commandBuffer,
            ref EntityManager entityManager,
            Entity prefabEntity,
            in TrafficCommonSettingsConfigBlobReference trafficCommonSettingsConfigBlobReference,
            in TrafficSettingsConfigBlobReference trafficSettingsConfigBlobReference,
            in TrafficGeneralSettingsReference trafficGeneralSettingsData,
            in TrafficRailConfigReference trafficRailConfigReference,
            in TrafficCollisionConfigReference trafficCollisionConfigReference,
            in PedestrianGeneralSettingsReference pedestrianGeneralSettingsData,
            in PedestrianSettingsReference pedestrianSettingsReference,
            in GeneralCoreSettingsDataReference coreSettingsData,
            in CommonGeneralSettingsReference commonGeneralSettingsData)
        {
            var entityType = trafficCommonSettingsConfigBlobReference.Reference.Value.EntityType;
            ref var trafficSettings = ref trafficSettingsConfigBlobReference.Reference.Value;
            var isWagon = entityManager.HasComponent<TrafficWagonComponent>(prefabEntity);

            var dotsSimulation = coreSettingsData.Config.Value.DOTSSimulation;

            if (dotsSimulation)
            {
                if (entityType == EntityType.HybridEntityMonoPhysics)
                {
                    UnityEngine.Debug.Log($"TrafficEntityBakingUtils. Selected entity type '{entityType}', but DOTS simulation enabled in the general settings.");
                }
            }
            else
            {
                if (entityType != EntityType.HybridEntityMonoPhysics)
                {
                    UnityEngine.Debug.Log($"TrafficEntityBakingUtils. Selected entity type '{entityType}', but Hybrid Mono simulation enabled in the general settings.");
                }
            }

            if (trafficGeneralSettingsData.Config.Value.HasTraffic && dotsSimulation)
            {
                if (coreSettingsData.Config.Value.SimulationType == SimulationType.NoPhysics && (entityType == EntityType.PureEntityCustomPhysics || entityType == EntityType.PureEntityCustomPhysics))
                {
                    UnityEngine.Debug.Log($"TrafficEntityBakingUtils. Selected vehicle has custom physics, but physics is disabled in the general settings. Make sure physics is enabled.");
                }

                if (trafficCommonSettingsConfigBlobReference.Reference.Value.DetectObstacleMode == DetectObstacleMode.RaycastOnly)
                {
                    if (coreSettingsData.Config.Value.SimulationType == SimulationType.NoPhysics)
                    {
                        UnityEngine.Debug.Log($"TrafficEntityBakingUtils. Selected detect obstacle type is raycast, but physics is disabled in the general settings. Make sure physics is enabled.");
                    }

                    if (trafficCommonSettingsConfigBlobReference.Reference.Value.CullPhysics)
                    {
                        UnityEngine.Debug.Log($"TrafficEntityBakingUtils. Selected detect obstacle type is raycast, but culling physics is enabled. Make sure culling is disabled or select another detect obstacle type in the traffic settings.");
                    }

                    if (entityType == EntityType.PureEntityNoPhysics)
                    {
                        UnityEngine.Debug.Log($"TrafficEntityBakingUtils. Selected detect obstacle type is raycast, but unsupported PureEntityNoPhysics type is selected for traffic.");
                    }
                }

                if (trafficCommonSettingsConfigBlobReference.Reference.Value.DetectNpcMode == DetectNpcMode.Raycast)
                {
                    if (coreSettingsData.Config.Value.SimulationType == SimulationType.NoPhysics)
                    {
                        UnityEngine.Debug.Log($"TrafficEntityBakingUtils. Selected detect npc type is raycast, but physics is disabled in the general settings. Make sure physics is enabled.");
                    }

                    if (pedestrianSettingsReference.Config.Value.EntityType == Pedestrian.EntityType.NoPhysics)
                    {
                        UnityEngine.Debug.Log($"TrafficEntityBakingUtils. Selected detect npc type is raycast, but pedestrian has no physics. Make sure physics is enabled in the pedestrian settings.");
                    }
                }
            }

            if (coreSettingsData.Config.Value.SimulationType == SimulationType.NoPhysics && entityType == EntityType.PureEntitySimplePhysics)
            {
                entityType = EntityType.PureEntityNoPhysics;
            }
            else
            {
                if (entityManager.HasComponent<VehicleOverrideTypeComponent>(prefabEntity))
                {
                    entityType = entityManager.GetComponentData<VehicleOverrideTypeComponent>(prefabEntity).EntityType;
                }
            }

            switch (entityType)
            {
                case EntityType.HybridEntitySimplePhysics:
                    {
                        commandBuffer.SetSharedComponent(prefabEntity, new WorldEntitySharedType(EntityWorldType.HybridEntity));

                        commandBuffer.AddComponent<CopyTransformToGameObject>(prefabEntity);
                        commandBuffer.SetComponentEnabled<CopyTransformToGameObject>(prefabEntity, false);

                        commandBuffer.AddComponent(prefabEntity, new CarLoadHullTag()
                        {
                            TrafficEntityType = entityType
                        });

                        break;
                    }
                case EntityType.HybridEntityMonoPhysics:
                    {
                        if (!entityManager.HasComponent<HybridLinkedEntityTag>(prefabEntity))
                        {
                            commandBuffer.SetSharedComponent(prefabEntity, new WorldEntitySharedType(EntityWorldType.HybridEntity));
                        }
                        else
                        {
                            commandBuffer.SetSharedComponent(prefabEntity, new WorldEntitySharedType(EntityWorldType.LinkedHybridEntity));
                        }

                        commandBuffer.AddComponent(prefabEntity, new CarLoadHullTag()
                        {
                            TrafficEntityType = entityType
                        });

                        if (trafficCollisionConfigReference.Config.Value.AvoidStuckedCollision && !isWagon)
                        {
                            commandBuffer.AddComponent<TrafficMonoStuckInfoComponent>(prefabEntity);
                        }

                        break;
                    }
                case EntityType.HybridEntityCustomPhysics:
                    {
                        commandBuffer.AddComponent<TrafficCustomPhysicsTag>(prefabEntity);

                        commandBuffer.AddComponent<CopyTransformToGameObject>(prefabEntity);
                        commandBuffer.SetComponentEnabled<CopyTransformToGameObject>(prefabEntity, false);

                        commandBuffer.SetSharedComponent(prefabEntity, new WorldEntitySharedType(EntityWorldType.HybridEntity));

                        commandBuffer.AddComponent(prefabEntity, new CarLoadHullTag()
                        {
                            TrafficEntityType = entityType
                        });

                        break;
                    }
                case EntityType.PureEntityCustomPhysics:
                    {
                        commandBuffer.AddComponent<TrafficCustomPhysicsTag>(prefabEntity);

                        break;
                    }
                case EntityType.PureEntitySimplePhysics:
                    {
                        break;
                    }
                case EntityType.PureEntityNoPhysics:
                    {
                        if (entityManager.HasComponent<PhysicsVelocity>(prefabEntity))
                        {
                            commandBuffer.RemoveComponent<PhysicsVelocity>(prefabEntity);
                        }

                        if (entityManager.HasComponent<PhysicsMass>(prefabEntity))
                        {
                            commandBuffer.RemoveComponent<PhysicsMass>(prefabEntity);
                        }

                        if (entityManager.HasComponent<PhysicsCollider>(prefabEntity))
                        {
                            commandBuffer.RemoveComponent<PhysicsCollider>(prefabEntity);
                        }

                        if (entityManager.HasComponent<PhysicsDamping>(prefabEntity))
                        {
                            commandBuffer.RemoveComponent<PhysicsDamping>(prefabEntity);
                        }

                        if (entityManager.HasComponent<PhysicsWorldIndex>(prefabEntity))
                        {
                            commandBuffer.RemoveComponent<PhysicsWorldIndex>(prefabEntity);
                        }

                        if (entityManager.HasComponent<PhysicsGraphicalInterpolationBuffer>(prefabEntity))
                        {
                            commandBuffer.RemoveComponent<PhysicsGraphicalInterpolationBuffer>(prefabEntity);
                            commandBuffer.RemoveComponent<PhysicsGraphicalSmoothing>(prefabEntity);
                        }

                        break;
                    }
            }

            if (entityType != EntityType.PureEntityNoPhysics && trafficCommonSettingsConfigBlobReference.Reference.Value.CullPhysics)
            {
                if (!entityManager.HasComponent<IgnoreCullPhysicsTag>(prefabEntity))
                {
                    commandBuffer.AddComponent<CullPhysicsTag>(prefabEntity);
                    commandBuffer.AddComponent<CustomCullPhysicsTag>(prefabEntity);
                }
            }

            if (!isWagon)
            {
                if (trafficGeneralSettingsData.Config.Value.ChangeLaneSupport)
                {
                    if (!entityManager.HasComponent<TrafficChangeLaneComponent>(prefabEntity))
                    {
                        commandBuffer.AddComponent<TrafficChangeLaneComponent>(prefabEntity);
                    }

                    commandBuffer.AddComponent<TrafficChangingLaneEventTag>(prefabEntity);
                    commandBuffer.SetComponentEnabled<TrafficChangingLaneEventTag>(prefabEntity, false);

#if UNITY_EDITOR
                    if (!entityManager.HasComponent<TrafficChangeLaneDebugInfoComponent>(prefabEntity))
                    {
                        commandBuffer.AddComponent<TrafficChangeLaneDebugInfoComponent>(prefabEntity);
                    }
#endif
                }

                if (trafficCommonSettingsConfigBlobReference.Reference.Value.HasRaycast)
                {
                    if (!entityManager.HasComponent<TrafficRaycastObstacleComponent>(prefabEntity))
                    {
                        commandBuffer.AddComponent<TrafficRaycastObstacleComponent>(prefabEntity);
                        commandBuffer.AddComponent<TrafficRaycastTag>(prefabEntity);
                        commandBuffer.SetComponentEnabled<TrafficRaycastTag>(prefabEntity, false);
                    }
                }

                if (!pedestrianGeneralSettingsData.Config.Value.HasPedestrian)
                {
                    if (entityManager.HasComponent<TrafficNpcObstacleComponent>(prefabEntity))
                    {
                        commandBuffer.RemoveComponent<TrafficNpcObstacleComponent>(prefabEntity);
                    }
                }
                else
                {
                    if (trafficCommonSettingsConfigBlobReference.Reference.Value.DetectNpcMode == DetectNpcMode.Calculate)
                    {
                        if (!entityManager.HasComponent<TrafficNpcObstacleComponent>(prefabEntity))
                        {
                            commandBuffer.AddComponent<TrafficNpcObstacleComponent>(prefabEntity);
                        }
                    }
                }

                if (trafficGeneralSettingsData.Config.Value.AntiStuckSupport)
                {
                    commandBuffer.AddComponent<TrafficStuckInfoComponent>(prefabEntity);
                }

                if (trafficGeneralSettingsData.Config.Value.AvoidanceSupport)
                {
                    commandBuffer.AddComponent<TrafficAvoidanceComponent>(prefabEntity);
                }
            }

            if (!commonGeneralSettingsData.Config.Value.BulletSupport)
            {
                if (entityManager.HasComponent<FactionTypeComponent>(prefabEntity))
                {
                    commandBuffer.RemoveComponent<FactionTypeComponent>(prefabEntity);
                }
            }

            if (!commonGeneralSettingsData.Config.Value.HealthSupport && dotsSimulation)
            {
                if (entityManager.HasComponent<HealthComponent>(prefabEntity))
                {
                    commandBuffer.RemoveComponent<HealthComponent>(prefabEntity);
                }

                if (entityManager.HasComponent<CarStartExplodeComponent>(prefabEntity))
                {
                    commandBuffer.RemoveComponent<CarStartExplodeComponent>(prefabEntity);
                }
            }
            else
            {
                if (entityManager.HasComponent<HealthComponent>(prefabEntity))
                {
                    commandBuffer.SetComponent(prefabEntity, new HealthComponent(trafficSettings.HealthCount));
                }
            }

            if (dotsSimulation)
            {
                if (trafficGeneralSettingsData.Config.Value.CarVisualDamageSystemSupport)
                {
                    commandBuffer.AddComponent<ProcessHitReactionTag>(prefabEntity);

                    float3 offset = default;

                    if (entityManager.HasComponent<CarRelatedHullComponent>(prefabEntity))
                    {
                        var hullEntity = entityManager.GetComponentData<CarRelatedHullComponent>(prefabEntity).HullEntity;

                        if (hullEntity != Entity.Null)
                        {
                            if (entityManager.HasComponent<LocalTransform>(hullEntity))
                            {
                                offset = entityManager.GetComponentData<LocalTransform>(hullEntity).Position;
                            }
                        }
                    }

                    commandBuffer.AddComponent(prefabEntity, new CarHitReactionData()
                    {
                        Offset = offset
                    });

                    commandBuffer.SetComponentEnabled<ProcessHitReactionTag>(prefabEntity, false);
                }

                commandBuffer.AddComponent<TrafficWheelsEnabledTag>(prefabEntity);

                if (trafficGeneralSettingsData.Config.Value.WheelSystemSupport &&
                    (trafficCommonSettingsConfigBlobReference.Reference.Value.CullWheelSupported &&
                    trafficCommonSettingsConfigBlobReference.Reference.Value.CullWheels) || (!trafficCommonSettingsConfigBlobReference.Reference.Value.CullWheelSupported && trafficCommonSettingsConfigBlobReference.Reference.Value.CullPhysics))
                {
                    commandBuffer.AddComponent<CullWheelTag>(prefabEntity);

                    if (entityManager.HasBuffer<VehicleWheel>(prefabEntity))
                    {
                        commandBuffer.SetComponentEnabled<TrafficWheelsEnabledTag>(prefabEntity, false);

                        var wheels = entityManager.GetBuffer<VehicleWheel>(prefabEntity);

                        for (int i = 0; i < wheels.Length; i++)
                        {
                            commandBuffer.SetComponentEnabled<WheelHandlingTag>(wheels[i].WheelEntity, false);
                        }
                    }
                }
                else
                {
                    if (trafficCommonSettingsConfigBlobReference.Reference.Value.CullWheelSupported)
                    {
                        if (entityManager.HasBuffer<VehicleWheel>(prefabEntity))
                        {
                            commandBuffer.RemoveComponent<VehicleWheel>(prefabEntity);
                        }
                    }
                }

                commandBuffer.AddComponent<CarStartExplodeComponent>(prefabEntity);
                commandBuffer.AddComponent<CarCollisionComponent>(prefabEntity);
            }

            if (trafficGeneralSettingsData.Config.Value.VehicleLightSupport)
            {
                commandBuffer.AddComponent<VehicleLightComponent>(prefabEntity);
            }

            if (trafficSettings.HasNavMeshObstacle)
            {
                if (entityManager.HasComponent<NavMeshObstacleBakingData>(prefabEntity))
                {
                    var navMeshObstacleBakingData = entityManager.GetComponentData<NavMeshObstacleBakingData>(prefabEntity);
                    commandBuffer.AddComponent(prefabEntity, new NavMeshObstacleData(navMeshObstacleBakingData));
                    commandBuffer.AddComponent(prefabEntity, new NavMeshObstacleLoadTag());
                }
            }
            else
            {
                if (entityManager.HasComponent<NavMeshObstacleData>(prefabEntity))
                {
                    commandBuffer.RemoveComponent<NavMeshObstacleData>(prefabEntity);
                }

                if (entityManager.HasComponent<NavMeshObstacleLoadTag>(prefabEntity))
                {
                    commandBuffer.RemoveComponent<NavMeshObstacleLoadTag>(prefabEntity);
                }
            }

            var additionalSettings = TrafficAdditionalSettings.None;

            if (trafficSettings.HasRotationLerp)
            {
                additionalSettings = DotsEnumExtension.AddFlag<TrafficAdditionalSettings>(additionalSettings, TrafficAdditionalSettings.HasRotationLerp);
            }

            if (trafficRailConfigReference.Config.Value.LerpTraffic)
            {
                additionalSettings = DotsEnumExtension.AddFlag<TrafficAdditionalSettings>(additionalSettings, TrafficAdditionalSettings.HasRailRotationLerp);
            }

            float baseOffset = 0;

            if (entityManager.HasComponent<VehicleBaseOffsetComponent>(prefabEntity))
            {
                baseOffset = entityManager.GetComponentData<VehicleBaseOffsetComponent>(prefabEntity).Offset;
                commandBuffer.RemoveComponent<VehicleBaseOffsetComponent>(prefabEntity);
            }

            float maxSteerAngle = 0;

            if (entityManager.HasComponent<CustomSteeringData>(prefabEntity))
            {
                var customSteeringData = entityManager.GetComponentData<CustomSteeringData>(prefabEntity);
                maxSteerAngle = customSteeringData.MaxSteeringAngle;
            }
            else
            {
                maxSteerAngle = math.radians(trafficSettings.MaxSteerAngle);
            }

            var trafficSettingsComponent = entityManager.GetComponentData<TrafficSettingsComponent>(prefabEntity);

            if (trafficSettingsComponent.MaxSpeed == 0)
                trafficSettingsComponent.MaxSpeed = trafficSettings.MaxSpeed / ProjectConstants.KmhToMs_RATE;

            if (trafficSettingsComponent.MaxSteerAngle == 0)
                trafficSettingsComponent.MaxSteerAngle = maxSteerAngle;

            trafficSettingsComponent.Acceleration = trafficSettings.Acceleration;
            trafficSettingsComponent.BackwardAcceleration = trafficSettings.BackwardAcceleration;
            trafficSettingsComponent.BrakePower = trafficSettings.BrakePower;
            trafficSettingsComponent.MaxSteerDirectionAngle = math.radians(trafficSettings.MaxSteerDirectionAngle);
            trafficSettingsComponent.SteeringDamping = trafficSettings.SteeringDamping;
            trafficSettingsComponent.AdditionalSettings = additionalSettings;
            trafficSettingsComponent.OffsetY = baseOffset;

            commandBuffer.SetComponent(prefabEntity, trafficSettingsComponent);

            commandBuffer.SetComponentEnabled<TrafficSwitchTargetNodeRequestTag>(prefabEntity, false);
            commandBuffer.SetComponentEnabled<TrafficNextTrafficNodeRequestTag>(prefabEntity, false);
            commandBuffer.SetComponentEnabled<TrafficEnteredTriggerNodeTag>(prefabEntity, false);
            commandBuffer.SetComponentEnabled<TrafficEnteringTriggerNodeTag>(prefabEntity, false);
            commandBuffer.SetComponentEnabled<TrafficIdleTag>(prefabEntity, false);

            if (dotsSimulation)
            {
                if (trafficCommonSettingsConfigBlobReference.Reference.Value.DetectObstacleMode == DetectObstacleMode.RaycastOnly)
                {
                    var raycastConfigReference = entityManager.CreateEntityQuery(typeof(RaycastConfigReference)).GetSingleton<RaycastConfigReference>();

                    CheckForPhysicsLayer(entityManager, prefabEntity, in raycastConfigReference, "TrafficEntityBakingUtils", "traffic car");
                }
            }
        }

        public static void CheckForPhysicsLayer(EntityManager entityManager, Entity entity, in RaycastConfigReference raycastConfigReference, string source, string text)
        {
            if (!entityManager.HasComponent<PhysicsCollider>(entity))
                return;

            var physicsCollider = entityManager.GetComponentData<PhysicsCollider>(entity);

            try
            {
                var filter = physicsCollider.Value.Value.GetCollisionFilter();
                var castFilter = raycastConfigReference.Config.Value.RaycastFilter;

                if ((castFilter & filter.BelongsTo) == 0)
                {
                    var layers = DotsEnumExtension.GetIndexLayers(castFilter);

                    var sb = new StringBuilder();

                    for (int i = 0; i < layers.Length; i++)
                    {
                        sb.Append(layers[i]).Append("|");
                    }

                    var castText = sb.ToString();

                    var carModelComponent = entityManager.GetComponentData<CarModelComponent>(entity);

                    UnityEngine.Debug.Log($"{source}. Raycast config casting {castText} doesn't have {text} Model {carModelComponent.Value} layer {DotsEnumExtension.LayerToIndex(filter.BelongsTo)}");
                }
            }
            catch { }
        }
    }
}
