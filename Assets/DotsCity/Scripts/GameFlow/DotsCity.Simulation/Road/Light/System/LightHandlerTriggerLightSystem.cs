using Spirit604.Gameplay.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Road
{
    [UpdateAfter(typeof(LightHandlerSwitchSystem))]
    [UpdateInGroup(typeof(SimulationGroup))]
    [BurstCompile]
    public partial struct LightHandlerTriggerLightSystem : ISystem
    {
        private EntityQuery lightQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            lightQuery = SystemAPI.QueryBuilder()
                .WithAllRW<LightTrigger, LightTriggerEnabledTag>()
                .Build();

            state.RequireForUpdate(lightQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var switchLightJob = new SwitchLightJob()
            {
                LightHandlerComponentLookup = SystemAPI.GetComponentLookup<LightHandlerComponent>(false),
                LightHandlerStateUpdateTagLookup = SystemAPI.GetComponentLookup<LightHandlerStateUpdateTag>(false),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            };

            switchLightJob.Schedule();
        }

        [BurstCompile]
        public partial struct SwitchLightJob : IJobEntity
        {
            public ComponentLookup<LightHandlerComponent> LightHandlerComponentLookup;
            public ComponentLookup<LightHandlerStateUpdateTag> LightHandlerStateUpdateTagLookup;

            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransformLookup;

            void Execute(
                Entity entity,
                ref LightTriggerBinding trafficNodeLightTriggerBinding,
                ref LightTrigger trafficNodeLightTrigger,
                EnabledRefRW<LightTriggerEnabledTag> trafficNodeLightTriggerEnabledTagRW,
                in DynamicBuffer<LightTriggerRelatedEntity> relatedEntities)
            {
                if (!trafficNodeLightTrigger.Initialized)
                {
                    trafficNodeLightTrigger.Initialized = true;

                    LightHandlerComponentLookup[trafficNodeLightTrigger.SourceLightEntity] = LightHandlerComponentLookup[trafficNodeLightTrigger.SourceLightEntity].SetState(LightState.Green);
                    LightHandlerStateUpdateTagLookup.SetComponentEnabled(trafficNodeLightTrigger.SourceLightEntity, true);

                    for (int i = 0; i < relatedEntities.Length; i++)
                    {
                        LightHandlerStateUpdateTagLookup.SetComponentEnabled(relatedEntities[i].LightEntity, true);
                        LightHandlerComponentLookup[relatedEntities[i].LightEntity] = LightHandlerComponentLookup[relatedEntities[i].LightEntity].SetState(LightState.Red);
                    }
                }

                bool unBind = true;

                if (LocalTransformLookup.HasComponent(trafficNodeLightTriggerBinding.TriggerEntity))
                {
                    var sourcePos = LocalTransformLookup[entity].Position;
                    var targetTransform = LocalTransformLookup[trafficNodeLightTriggerBinding.TriggerEntity];
                    var targetPos = targetTransform.Position;

                    if (!trafficNodeLightTrigger.Passed)
                    {
                        unBind = false;
                        var dir1 = math.normalize(sourcePos - targetPos);

                        trafficNodeLightTrigger.Passed = math.dot(dir1, targetTransform.Forward()) < 0;
                    }
                    else
                    {
                        var distance = math.distancesq(sourcePos, targetPos);

                        unBind = distance >= trafficNodeLightTrigger.TriggerDistanceSQ;
                    }
                }

                if (unBind)
                {
                    trafficNodeLightTriggerEnabledTagRW.ValueRW = false;
                    trafficNodeLightTriggerBinding.TriggerEntity = default;
                    trafficNodeLightTrigger.Initialized = false;
                    trafficNodeLightTrigger.Passed = false;

                    LightHandlerComponentLookup[trafficNodeLightTrigger.SourceLightEntity] = LightHandlerComponentLookup[trafficNodeLightTrigger.SourceLightEntity].SetState(LightState.Red);
                    LightHandlerStateUpdateTagLookup.SetComponentEnabled(trafficNodeLightTrigger.SourceLightEntity, true);

                    for (int i = 0; i < relatedEntities.Length; i++)
                    {
                        LightHandlerComponentLookup[relatedEntities[i].LightEntity] = LightHandlerComponentLookup[relatedEntities[i].LightEntity].SetState(LightState.Green);
                        LightHandlerStateUpdateTagLookup.SetComponentEnabled(relatedEntities[i].LightEntity, true);
                    }
                }
            }
        }
    }
}
