using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Events;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class NpcDeathEventConsumerSystem : EventConsumerSystemBase
    {
        private EntityQuery configQuery;
        private EntityArchetype pedestrianTriggerArchetype;
        private BeginSimulationEntityCommandBufferSystem commandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            commandBufferSystem = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();

            configQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<ScaryTriggerConfigReference>().Build(this);
            pedestrianTriggerArchetype = EntityManager.CreateArchetype(typeof(TriggerComponent), typeof(LifeTimeComponent));
        }

        protected override void Consume()
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            var config = configQuery.GetSingleton<ScaryTriggerConfigReference>().Config;

            for (int i = 0; i < _triggerStreams.Count; i++)
            {
                Dependency = new NpcDeathEventJob()
                {
                    pedestrianTriggerArchetypeLocal = pedestrianTriggerArchetype,
                    config = config,
                    currentTime = currentTime,
                    npcDeathEventReader = _triggerStreams[i].AsReader(),
                    commandBuffer = commandBufferSystem.CreateCommandBuffer(),
                }.Schedule(Dependency);
            }

            commandBufferSystem.AddJobHandleForProducer(Dependency);
        }

        [BurstCompile]
        private struct NpcDeathEventJob : IJob
        {
            [ReadOnly] public EntityArchetype pedestrianTriggerArchetypeLocal;
            [ReadOnly] public BlobAssetReference<ScaryTriggerConfig> config;
            [ReadOnly] public float currentTime;
            public NativeStream.Reader npcDeathEventReader;
            public EntityCommandBuffer commandBuffer;

            public void Execute()
            {
                for (int i = 0; i < npcDeathEventReader.ForEachCount; i++)
                {
                    npcDeathEventReader.BeginForEachIndex(i);

                    while (npcDeathEventReader.RemainingItemCount > 0)
                    {
                        var npcDeathEventComponent = npcDeathEventReader.Read<NpcDeathEventData>();

                        var triggerEntity = commandBuffer.CreateEntity(pedestrianTriggerArchetypeLocal);

                        commandBuffer.SetComponent(triggerEntity,
                            new TriggerComponent()
                            {
                                Position = npcDeathEventComponent.Position,
                                TriggerDistanceSQ = config.Value.DeathTriggerSqDistance,
                                TriggerAreaType = TriggerAreaType.FearPointTrigger
                            });

                        commandBuffer.SetComponent(triggerEntity,
                            new LifeTimeComponent()
                            {
                                DestroyTimeStamp = currentTime + config.Value.DeathTriggerDuration
                            });
                    }

                    npcDeathEventReader.EndForEachIndex();
                }
            }
        }

        public float GetLifeTime(float lifeTime)
        {
            return (float)SystemAPI.Time.ElapsedTime + lifeTime;
        }
    }
}