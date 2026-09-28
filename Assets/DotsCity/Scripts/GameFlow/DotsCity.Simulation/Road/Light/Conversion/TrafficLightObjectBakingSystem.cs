using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Config;
using Spirit604.DotsCity.Simulation.Level.Props;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    [TemporaryBakingType]
    public struct TrafficLightObjectBakingData : IComponentData
    {
        public Entity RelatedEntityHandler;
        public NativeArray<LightEntityElementTemp> FrameEntities;
    }

    public struct LightEntityElementTemp
    {
        public Entity FrameEntity;
        public float3 IndexPosition;
        public Entity RedEntity;
        public Entity YellowEntity;
        public Entity GreenEntity;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateAfter(typeof(TrafficLightConversionSystem))]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class TrafficLightObjectBakingSystem : SystemBase
    {
        private EntityQuery bakingQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TrafficLightObjectBakingData>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate(bakingQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            var commonGeneralSettingsData = SystemAPI.GetSingleton<CommonGeneralSettingsReference>();

            Entities
            .WithoutBurst()
            .ForEach((
                Entity entity,
                in TrafficLightObjectBakingData trafficLightObjectBakingData) =>
            {
                var frameEntities = trafficLightObjectBakingData.FrameEntities;

                for (int i = 0; i < frameEntities.Length; i++)
                {
                    var frameEntity = frameEntities[i].FrameEntity;

                    if (commonGeneralSettingsData.Config.Value.PropsDamageSupport)
                    {
                        commandBuffer.AddComponent<PropsDamagedTag>(frameEntity);
                    }

                    commandBuffer.AddComponent(frameEntity, new LightFrameHandlerStateComponent());
                    commandBuffer.AddComponent<LightHandlerStateUpdateTag>(frameEntity);
                    commandBuffer.SetComponentEnabled<LightHandlerStateUpdateTag>(frameEntity, false);

                    var relatedEntityHandler = Entity.Null;

                    if (trafficLightObjectBakingData.RelatedEntityHandler != Entity.Null)
                    {
                        relatedEntityHandler = BakerExtension.GetEntity(EntityManager, trafficLightObjectBakingData.RelatedEntityHandler);
                    }

                    commandBuffer.AddComponent(frameEntity, new LightFrameHandlerEntityComponent()
                    {
                        RelatedEntityHandler = relatedEntityHandler
                    });

                    commandBuffer.AddComponent(frameEntity, new LightFrameData()
                    {
                        IndexPosition = frameEntities[i].IndexPosition,
                        RedEntity = frameEntities[i].RedEntity,
                        YellowEntity = frameEntities[i].YellowEntity,
                        GreenEntity = frameEntities[i].GreenEntity
                    });

                    if (frameEntities[i].RedEntity != Entity.Null)
                    {
                        commandBuffer.SetComponentEnabled<MaterialMeshInfo>(frameEntities[i].RedEntity, false);
                    }

                    if (frameEntities[i].YellowEntity != Entity.Null)
                    {
                        commandBuffer.SetComponentEnabled<MaterialMeshInfo>(frameEntities[i].YellowEntity, false);
                    }

                    if (frameEntities[i].GreenEntity != Entity.Null)
                    {
                        commandBuffer.SetComponentEnabled<MaterialMeshInfo>(frameEntities[i].GreenEntity, false);
                    }
                }
            }).Run();

            CompleteDependency();
            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}