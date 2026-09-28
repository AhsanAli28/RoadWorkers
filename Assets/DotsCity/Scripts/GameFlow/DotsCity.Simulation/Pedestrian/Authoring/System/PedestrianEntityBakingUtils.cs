using Spirit604.AnimationBaker.Entities;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Common;
using Spirit604.DotsCity.Simulation.Config;
using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Physics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    public static class PedestrianEntityBakingUtils
    {
        public static void Bake(
            ref EntityCommandBuffer commandBuffer,
            ref EntityManager entityManager,
            Entity entity,
            in MiscConversionSettingsReference conversionSettings,
            in GeneralCoreSettingsDataReference coreSettingsData,
            in CommonGeneralSettingsReference commonGeneralSettingsData,
            in PedestrianGeneralSettingsReference pedestrianGeneralSettingsData,
            in PedestrianSettingsReference pedestrianSettingsReference)
        {
            if (entityManager.HasComponent<CircleColliderComponent>(entity))
            {
                CircleColliderComponent circleColliderComponent = new CircleColliderComponent()
                {
                    Radius = conversionSettings.Config.Value.PedestrianColliderRadius
                };

                commandBuffer.SetComponent(entity, circleColliderComponent);
            }

            var dotsSimulation = coreSettingsData.Config.Value.DOTSSimulation;

            bool npcHasPhysics = conversionSettings.Config.Value.EntityType == EntityType.Physics;

            bool hasPhysics = dotsSimulation && (coreSettingsData.Config.Value.SimulationType != SimulationType.NoPhysics &&
                (npcHasPhysics || conversionSettings.Config.Value.CollisionType == CollisionType.Physics));

            if (hasPhysics)
            {
                if (coreSettingsData.Config.Value.CullPhysics)
                {
                    if (entityManager.HasComponent<PhysicsCollider>(entity))
                    {
                        commandBuffer.AddComponent<CullPhysicsTag>(entity);

                        commandBuffer.SetSharedComponent(entity, new PhysicsWorldIndex()
                        {
                            Value = ProjectConstants.NoPhysicsWorldIndex
                        });
                    }
                }
            }
            else
            {
                if (entityManager.HasComponent<PhysicsCollider>(entity))
                {
                    commandBuffer.RemoveComponent<PhysicsCollider>(entity);
                }

                if (entityManager.HasComponent<PhysicsVelocity>(entity))
                {
                    commandBuffer.RemoveComponent<PhysicsVelocity>(entity);
                    commandBuffer.RemoveComponent<PhysicsMass>(entity);
                }
            }

            var navAgentComponent = new NavAgentComponent();

            if (pedestrianGeneralSettingsData.Config.Value.TriggerSupport)
            {
                commandBuffer.AddComponent<TriggerConsumerTag>(entity);
            }

            var avoidanceType = pedestrianGeneralSettingsData.Config.Value.NavigationSupport ? conversionSettings.Config.Value.ObstacleAvoidanceType : ObstacleAvoidanceType.Disabled;

            if (avoidanceType != ObstacleAvoidanceType.Disabled)
            {
                commandBuffer.AddComponent<EnabledNavigationTag>(entity);
                commandBuffer.SetComponentEnabled<EnabledNavigationTag>(entity, false);

                switch (avoidanceType)
                {
                    case ObstacleAvoidanceType.CalcNavPath:
                        {
                            commandBuffer.AddComponent<NavAgentTag>(entity);

#if REESE_PATH
                            Reese.Path.PathAgentAuthoring.AddComponents(ref commandBuffer, entity, "Humanoid", Vector3.zero);
#else
                            Debug.Log("PedestrianEntityBakingUtils. CalcNavPath avoidance type is selected but the Reese navigation package is not installed, install this package, otherwise select a different avoidance method in the Pedestrian settings.");
#endif

                            if (conversionSettings.Config.Value.PedestrianNavigationType == NpcNavigationType.Persist)
                            {
                                commandBuffer.AddComponent<PersistNavigationTag>(entity);
                                commandBuffer.AddComponent<PersistNavigationComponent>(entity);
                            }
                            else
                            {
                                commandBuffer.SetComponentEnabled<UpdateNavTargetTag>(entity, false);
                            }

                            break;
                        }
                    case ObstacleAvoidanceType.LocalAvoidance:
                        {
                            commandBuffer.SetComponentEnabled<UpdateNavTargetTag>(entity, false);
                            commandBuffer.AddComponent<LocalAvoidanceAgentTag>(entity);
                            commandBuffer.AddBuffer<PathPointAvoidanceElement>(entity);
                            commandBuffer.AddComponent<PathLocalAvoidanceEnabledTag>(entity);
                            commandBuffer.SetComponentEnabled<PathLocalAvoidanceEnabledTag>(entity, false);

                            break;
                        }
                    case ObstacleAvoidanceType.AgentsNavigation:
                        {
                            commandBuffer.AddComponent<CustomLocomotionTag>(entity);
                            commandBuffer.AddComponent<PersistNavigationTag>(entity);
                            commandBuffer.AddComponent<PersistNavigationComponent>(entity);
                            commandBuffer.AddComponent<AgentInitTag>(entity);
                            break;
                        }
                }
            }

            commandBuffer.SetComponent(entity, navAgentComponent);

            if (!commonGeneralSettingsData.Config.Value.HealthSupport)
            {
                if (entityManager.HasComponent<HealthComponent>(entity))
                {
                    commandBuffer.RemoveComponent<HealthComponent>(entity);
                }
            }
            else
            {
                var hasRagdoll = conversionSettings.Config.Value.HasRagdoll &&
                    (coreSettingsData.Config.Value.SimulationType != SimulationType.NoPhysics || !dotsSimulation);

                if (hasRagdoll)
                {
                    commandBuffer.AddComponent<RagdollComponent>(entity);
                    commandBuffer.AddComponent<RagdollActivateEventTag>(entity);
                    commandBuffer.SetComponentEnabled<RagdollActivateEventTag>(entity, false);

                    if (conversionSettings.Config.Value.RagdollType == RagdollType.Custom)
                    {
                        commandBuffer.AddComponent<CustomRagdollTag>(entity);
                    }
                }
            }

            bool hasFaction = commonGeneralSettingsData.Config.Value.BulletSupport;

            if (hasFaction)
            {
                commandBuffer.AddComponent(entity, new FactionTypeComponent { Value = FactionType.City });
            }

            var npcRigType = conversionSettings.Config.Value.PedestrianRigType;
            var pedestrianSkinType = conversionSettings.Config.Value.PedestrianSkinType;

            if (npcRigType == NpcRigType.HybridAndGPU)
            {
                pedestrianSkinType = NpcSkinType.RigShowOnlyInView;
            }

            if (pedestrianSkinType == NpcSkinType.RigShowAlways)
            {
                commandBuffer.AddComponent<DisableUnloadSkinTag>(entity);
            }

            bool hasEnityRenderSkin = false;

            if (conversionSettings.Config.Value.HasRig)
            {
                switch (npcRigType)
                {
                    case NpcRigType.HybridLegacy:
                        {
                            AddHybridSkinComponents(ref commandBuffer, entity, npcHasPhysics);
                            break;
                        }
                    case NpcRigType.PureGPU:
                        {
                            AddGPUSkinComponents(ref commandBuffer, entity);
                            hasEnityRenderSkin = true;
                            break;
                        }
                    case NpcRigType.HybridAndGPU:
                        {
                            AddHybridSkinComponents(ref commandBuffer, entity, npcHasPhysics);
                            AddGPUSkinComponents(ref commandBuffer, entity);

                            commandBuffer.SetComponentEnabled<GPUSkinTag>(entity, false);
                            commandBuffer.SetComponentEnabled<HybridLegacySkinTag>(entity, false);
                            commandBuffer.AddComponent<HybridGPUSkinTag>(entity);
                            commandBuffer.AddComponent<HybridGPUSkinData>(entity);
                            commandBuffer.AddComponent<DisableUnloadSkinTag>(entity);
                            hasEnityRenderSkin = true;
                            break;
                        }
                    case NpcRigType.HybridOnRequestAndGPU:
                        {
                            AddHybridSkinComponents(ref commandBuffer, entity, npcHasPhysics);
                            AddGPUSkinComponents(ref commandBuffer, entity);

                            commandBuffer.SetComponentEnabled<GPUSkinTag>(entity, false);
                            commandBuffer.SetComponentEnabled<HybridLegacySkinTag>(entity, false);
                            commandBuffer.AddComponent<HybridGPUSkinTag>(entity);
                            commandBuffer.AddComponent<HybridGPUSkinData>(entity);
                            commandBuffer.AddComponent<PreventHybridSkinTagTag>(entity);
                            commandBuffer.AddComponent<DisableUnloadSkinTag>(entity);
                            hasEnityRenderSkin = true;
                            break;
                        }
                    case NpcRigType.HybridShapeGPU:
                        {
                            AddGPUSkinComponents(ref commandBuffer, entity);
                            AddHybridShapeComponents(ref commandBuffer, entity, npcHasPhysics);
                            hasEnityRenderSkin = true;
                            break;
                        }
                    case NpcRigType.RukhankaHybrid:
                        {
                            AddHybridShapeComponents(ref commandBuffer, entity, npcHasPhysics);
                            break;
                        }
                }
            }

            if (hasEnityRenderSkin)
            {
                AddRenderComponents(ref commandBuffer, entity);
            }

            AddAnimatorCustomStateComponents(ref commandBuffer, entity);
        }

        private static void AddHybridSkinComponents(ref EntityCommandBuffer commandBuffer, Entity entity, bool physicsEnabled)
        {
            commandBuffer.AddComponent<HybridLegacySkinTag>(entity);
            commandBuffer.AddComponent<CopyTransformToGameObject>(entity);
            commandBuffer.SetComponentEnabled<CopyTransformToGameObject>(entity, false);

            if (physicsEnabled)
            {
                commandBuffer.AddComponent<CopyTransformFromGameObject>(entity);
                commandBuffer.AddComponent<HybridPhysicsObjectTag>(entity);
            }
        }

        private static void AddGPUSkinComponents(ref EntityCommandBuffer commandBuffer, Entity entity)
        {
            commandBuffer.AddComponent<GPUSkinTag>(entity);
            commandBuffer.AddComponent<SkinUpdateComponent>(entity);
            commandBuffer.AddComponent<UpdateSkinTag>(entity);
            commandBuffer.AddComponent<SkinAnimatorData>(entity);

            commandBuffer.AddComponent(entity, new AnimationTransitionData()
            {
                LastAnimHash = -1
            });

            commandBuffer.AddComponent<HasAnimTransitionTag>(entity);

            ShaderUtils.AddShaderComponents(ref commandBuffer, entity, true);

            commandBuffer.SetComponentEnabled<UpdateSkinTag>(entity, false);
            commandBuffer.SetComponentEnabled<HasAnimTransitionTag>(entity, false);

            commandBuffer.AddComponent<RenderBounds>(entity);
            commandBuffer.AddComponent<WorldRenderBounds>(entity);
        }

        private static void AddHybridShapeComponents(ref EntityCommandBuffer commandBuffer, Entity entity, bool physicsEnabled)
        {
            commandBuffer.AddComponent<HybridShapeTag>(entity);
            commandBuffer.SetComponentEnabled<HybridShapeTag>(entity, false);
            commandBuffer.AddComponent<CopyTransformToGameObject>(entity);
            commandBuffer.SetComponentEnabled<CopyTransformToGameObject>(entity, false);

            if (physicsEnabled)
            {
                commandBuffer.AddComponent<CopyTransformFromGameObject>(entity);
                commandBuffer.AddComponent<HybridPhysicsObjectTag>(entity);
            }
        }

        private static void AddRenderComponents(ref EntityCommandBuffer commandBuffer, Entity entity)
        {
            commandBuffer.AddComponent<WorldToLocal_Tag>(entity);
            commandBuffer.AddComponent<PerInstanceCullingTag>(entity);
            commandBuffer.AddComponent<BlendProbeTag>(entity);
            commandBuffer.AddComponent<MaterialMeshInfo>(entity);
            commandBuffer.SetComponentEnabled<MaterialMeshInfo>(entity, false);

            var filterSettings = RenderFilterSettings.Default;
            filterSettings.ShadowCastingMode = ShadowCastingMode.On;
            filterSettings.ReceiveShadows = false;

            var renderMeshDescription = new RenderMeshDescription
            {
                FilterSettings = filterSettings,
                LightProbeUsage = LightProbeUsage.Off,
            };

            commandBuffer.AddSharedComponentManaged(entity, filterSettings);
        }

        private static void AddAnimatorCustomStateComponents(ref EntityCommandBuffer commandBuffer, Entity entity)
        {
            commandBuffer.AddComponent<CustomAnimatorStateTag>(entity);
            commandBuffer.AddComponent<HasCustomAnimationTag>(entity);
            commandBuffer.AddComponent<UpdateCustomAnimationTag>(entity);
            commandBuffer.AddComponent<WaitForCustomAnimationTag>(entity);
            commandBuffer.AddComponent<ExitCustomAnimationTag>(entity);

            commandBuffer.SetComponentEnabled<CustomAnimatorStateTag>(entity, false);
            commandBuffer.SetComponentEnabled<HasCustomAnimationTag>(entity, false);
            commandBuffer.SetComponentEnabled<UpdateCustomAnimationTag>(entity, false);
            commandBuffer.SetComponentEnabled<WaitForCustomAnimationTag>(entity, false);
            commandBuffer.SetComponentEnabled<ExitCustomAnimationTag>(entity, false);
        }
    }
}