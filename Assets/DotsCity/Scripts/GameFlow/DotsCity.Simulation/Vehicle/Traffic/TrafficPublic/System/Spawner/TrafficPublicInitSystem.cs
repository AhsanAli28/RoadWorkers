using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.TrafficPublic
{
    [UpdateInGroup(typeof(StructuralInitGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficPublicInitSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<TrafficPublicInitComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var trafficPublicInitJob = new TrafficPublicInitJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                RouteTempEntitySettingsLookup = SystemAPI.GetComponentLookup<RouteTempEntitySettingsComponent>(true),
                TrafficWagonElementLookup = SystemAPI.GetBufferLookup<TrafficWagonElement>(true),
                TrafficFixedRouteLookup = SystemAPI.GetComponentLookup<TrafficFixedRouteTag>(true),
                TrafficPublicTagLookup = SystemAPI.GetComponentLookup<TrafficPublicTag>(true),
                CarCapacityComponentLookup = SystemAPI.GetComponentLookup<CarCapacityComponent>(false),
                TrafficSettingsComponentLookup = SystemAPI.GetComponentLookup<TrafficSettingsComponent>(false),
                TrafficRailConfigReference = SystemAPI.GetSingleton<TrafficRailConfigReference>(),
                Time = (float)SystemAPI.Time.ElapsedTime,
            };

            trafficPublicInitJob.Run();
        }

        [BurstCompile]
        public partial struct TrafficPublicInitJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public ComponentLookup<RouteTempEntitySettingsComponent> RouteTempEntitySettingsLookup;

            [ReadOnly]
            public BufferLookup<TrafficWagonElement> TrafficWagonElementLookup;

            [ReadOnly]
            public ComponentLookup<TrafficFixedRouteTag> TrafficFixedRouteLookup;

            [ReadOnly]
            public ComponentLookup<TrafficPublicTag> TrafficPublicTagLookup;

            public ComponentLookup<CarCapacityComponent> CarCapacityComponentLookup;
            public ComponentLookup<TrafficSettingsComponent> TrafficSettingsComponentLookup;

            [ReadOnly]
            public TrafficRailConfigReference TrafficRailConfigReference;

            [ReadOnly]
            public float Time;

            void Execute(
                Entity entity,
                ref TrafficPublicInitComponent trafficInitComponent,
                in CarModelComponent carModelComponent)
            {
                CommandBuffer.DestroyEntity(trafficInitComponent.RouteEntitySettings);
                CommandBuffer.RemoveComponent<TrafficPublicInitComponent>(entity);

                if (!TrafficPublicTagLookup.HasComponent(entity))
                {
                    UnityEngine.Debug.Log($"TrafficPublicInitSystem. Trying to init Car model '{carModelComponent.Value}' traffic public. But it doesn't seem to have a traffic public authoring component.");
                    return;
                }

                var routeSettings = RouteTempEntitySettingsLookup[trafficInitComponent.RouteEntitySettings];

                CommandBuffer.AddComponent(entity, new TrafficFixedRouteComponent()
                {
                    RouteEntity = routeSettings.RouteEntity,
                    RouteNodeIndex = routeSettings.RouteIndex,
                    LoopPath = true,
                    RouteLength = routeSettings.RouteLength
                });

                CommandBuffer.AddComponent(entity, new TrafficFixedRouteLinkComponent()
                {
                    RouteEntity = routeSettings.RouteEntity,
                });

                Init(entity, in trafficInitComponent, ref TrafficSettingsComponentLookup);

                if (TrafficWagonElementLookup.HasBuffer(entity))
                {
                    var buffer = TrafficWagonElementLookup[entity];

                    for (int i = 0; i < buffer.Length; i++)
                    {
                        var wagonEntity = buffer[i].Entity;
                        Init(wagonEntity, in trafficInitComponent, ref TrafficSettingsComponentLookup);
                    }
                }
            }

            private void Init(Entity entity, in TrafficPublicInitComponent trafficInitComponent, ref ComponentLookup<TrafficSettingsComponent> trafficLookup)
            {
                var routeSettings = RouteTempEntitySettingsLookup[trafficInitComponent.RouteEntitySettings];

                var trafficSettingsComponent = trafficLookup[entity];

                if (routeSettings.TrafficPublicType == TrafficPublicType.Train)
                {
                    if (TrafficRailConfigReference.Config.Value.LerpTram)
                    {
                        trafficSettingsComponent.AdditionalSettings = DotsEnumExtension.AddFlag<TrafficAdditionalSettings>(trafficSettingsComponent.AdditionalSettings, TrafficAdditionalSettings.HasRailRotationLerp);
                    }
                    else
                    {
                        trafficSettingsComponent.AdditionalSettings = DotsEnumExtension.RemoveFlag<TrafficAdditionalSettings>(trafficSettingsComponent.AdditionalSettings, TrafficAdditionalSettings.HasRailRotationLerp);
                    }
                }

                if (!TrafficFixedRouteLookup.HasComponent(entity))
                {
                    CommandBuffer.AddComponent<TrafficFixedRouteTag>(entity);
                    CommandBuffer.RemoveComponent<TrafficDefaultTag>(entity);
                }

                var rnd = UnityMathematicsExtension.GetRandomGen(Time, entity.Index);

                var carCapacityComponent = CarCapacityComponentLookup[entity];
                var initialCapacity = rnd.NextInt(0, carCapacityComponent.MaxCapacity);
                carCapacityComponent.AvailableCapacity = initialCapacity;
                CarCapacityComponentLookup[entity] = carCapacityComponent;
                trafficLookup[entity] = trafficSettingsComponent;
            }
        }
    }
}