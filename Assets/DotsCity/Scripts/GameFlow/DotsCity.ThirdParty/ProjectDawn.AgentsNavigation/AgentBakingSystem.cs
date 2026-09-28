using ProjectDawn.Navigation;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup), OrderLast = true)]
    public partial class AgentBakingSystem : SystemBase
    {
        private EntityQuery agentBakingQuery;
        private EntityQuery configQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            agentBakingQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<AgentBakingTag>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);


            configQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<AgentsNavigationConversionSettingsReference>()
                .Build(this);

            RequireForUpdate(agentBakingQuery);
        }

        protected override void OnUpdate()
        {
            var conversionSettings = SystemAPI.GetSingleton<MiscConversionSettingsReference>();

            AgentsNavigationConversionSettings agentConfig = default;

            if (configQuery.CalculateEntityCount() == 1)
            {
                var agentsNavigationConversionSettingsReference = configQuery.GetSingleton<AgentsNavigationConversionSettingsReference>();
                agentConfig = agentsNavigationConversionSettingsReference.Config.Value;
            }
            else
            {
                agentConfig = AgentsNavigationConversionSettings.GetDefault();
            }

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var entityManager = EntityManager;

            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
            .WithAll<AgentBakingTag>()
            .ForEach((
                Entity entity) =>
            {
                if (conversionSettings.Config.Value.ObstacleAvoidanceType == Npc.Navigation.ObstacleAvoidanceType.AgentsNavigation)
                {
                    if (entityManager.HasComponent<NavMeshPath>(entity))
                    {
                        commandBuffer.SetComponentEnabled<NavMeshPath>(entity, false);
                    }

                    if (conversionSettings.Config.Value.AutoAddAgentComponents)
                    {
                        if (!EntityManager.HasComponent<Agent>(entity))
                        {
                            commandBuffer.AddComponent(entity, new Agent
                            {
                                Layers = agentConfig.m_Layers
                            });
                        }

                        if (!EntityManager.HasComponent<AgentBody>(entity))
                        {
                            commandBuffer.AddComponent(entity, AgentBody.Default);
                        }

                        if (!EntityManager.HasComponent<AgentLocomotion>(entity))
                        {
                            commandBuffer.AddComponent(entity, new AgentLocomotion()
                            {
                                Acceleration = agentConfig.Acceleration,
                                AngularSpeed = agentConfig.AngularSpeed,
                                StoppingDistance = agentConfig.StoppingDistance,
                                AutoBreaking = agentConfig.AutoBreaking,
                            });
                        }

                        if (!EntityManager.HasComponent<AgentShape>(entity))
                        {
                            commandBuffer.AddComponent(entity, new AgentShape()
                            {
                                Radius = agentConfig.Radius,
                                Height = agentConfig.Height,
                                Type = agentConfig.Type
                            });
                        }

                        if (!EntityManager.HasComponent<AgentCollider>(entity))
                        {
                            if (agentConfig.HasCollider)
                            {
                                commandBuffer.AddComponent(entity, new AgentCollider()
                                {
                                    Layers = agentConfig.m_ColliderLayers
                                });
                            }
                        }

                        if (!EntityManager.HasComponent<NavMeshPath>(entity))
                        {
                            commandBuffer.AddComponent(entity, new NavMeshPath()
                            {
                                State = NavMeshPathState.FinishedFullPath,
                                AgentTypeId = agentConfig.AgentTypeId,
                                AreaMask = agentConfig.AreaMask,
                                AutoRepath = agentConfig.AutoRepath,
                                Grounded = agentConfig.m_Grounded,
                                MappingExtent = agentConfig.MappingExtent,
                            });
                        }

                        commandBuffer.SetComponentEnabled<NavMeshPath>(entity, false);

                        if (!EntityManager.HasBuffer<NavMeshNode>(entity))
                        {
                            commandBuffer.AddBuffer<NavMeshNode>(entity);
                        }

                        if (agentConfig.m_LinkTraversalMode != NavMeshLinkTraversalMode.None)
                        {
                            if (!EntityManager.HasComponent<LinkTraversal>(entity))
                            {
                                commandBuffer.AddComponent<LinkTraversal>(entity);
                                commandBuffer.SetComponentEnabled<LinkTraversal>(entity, false);
                            }
                        }

                        if (agentConfig.m_LinkTraversalMode == NavMeshLinkTraversalMode.Seeking)
                        {
                            if (!EntityManager.HasComponent<LinkTraversalSeek>(entity))
                            {
                                commandBuffer.AddComponent<LinkTraversalSeek>(entity);
                            }
                        }

                        if (agentConfig.m_LinkTraversalMode == NavMeshLinkTraversalMode.Custom)
                        {
                            if (!EntityManager.HasComponent<NavMeshLinkTraversal>(entity))
                            {
                                commandBuffer.AddComponent<NavMeshLinkTraversal>(entity);
                            }
                        }

                        switch (agentConfig.AgentAvoidanceType)
                        {
                            case AgentsNavigationSettingsConfig.AvoidanceType.SonarAvoidance:
                                {
                                    if (!EntityManager.HasComponent<AgentSonarAvoid>(entity))
                                    {
                                        commandBuffer.AddComponent(entity, new AgentSonarAvoid()
                                        {
                                            Radius = agentConfig.SonarRadius,
                                            Angle = agentConfig.Angle,
                                            MaxAngle = agentConfig.MaxAngle,
                                            Mode = agentConfig.Mode,
                                            BlockedStop = agentConfig.BlockedStop,
                                            Layers = agentConfig.m_SonarLayers
                                        });
                                    }

                                    if (agentConfig.UseWalls)
                                    {
                                        if (!EntityManager.HasComponent<NavMeshBoundary>(entity))
                                        {
                                            commandBuffer.AddComponent(entity, new NavMeshBoundary { Radius = agentConfig.SonarRadius + 1 });
                                            commandBuffer.AddBuffer<NavMeshWall>(entity);
                                        }
                                    }

                                    break;
                                }
                            case AgentsNavigationSettingsConfig.AvoidanceType.ReciprocalAvoidance:
                                {
                                    if (!EntityManager.HasComponent<AgentReciprocalAvoid>(entity))
                                    {
                                        commandBuffer.AddComponent(entity, new AgentReciprocalAvoid
                                        {
                                            Radius = agentConfig.ReciprocalRadius,
                                            Layers = agentConfig.ReciprocalLayers
                                        });
                                    }

                                    break;
                                }
                        }
                    }
                }

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}