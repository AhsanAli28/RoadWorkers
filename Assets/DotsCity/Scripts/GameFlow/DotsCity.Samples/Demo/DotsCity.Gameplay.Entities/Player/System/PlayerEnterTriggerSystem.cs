using Spirit604.DotsCity.Gameplay.Level;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Stateful;

namespace Spirit604.DotsCity.Gameplay.Player
{
    [UpdateInGroup(typeof(PhysicsTriggerGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class PlayerEnterTriggerSystem : SystemBase
    {
        private EndFixedStepSimulationEntityCommandBufferSystem m_CommandBufferSystem;
        private TriggerEventConversionSystem m_TriggerSystem;
        private EntityQueryMask m_NonTriggerMask;

        private IPlayerEntityTriggerProccesor entityTriggerProccesor;

        protected override void OnCreate()
        {
            m_CommandBufferSystem = World.GetOrCreateSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
            m_TriggerSystem = World.GetOrCreateSystemManaged<TriggerEventConversionSystem>();

            m_NonTriggerMask = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TriggerComponent>()
                .Build(this)
                .GetEntityQueryMask();

            var updateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PlayerPhysicsShapeComponent>()
                .Build(this);

            RequireForUpdate(updateQuery);

            Enabled = false;
        }

        protected override void OnUpdate()
        {
            Dependency = JobHandle.CombineDependencies(m_TriggerSystem.OutDependency, Dependency);

            var commandBuffer = m_CommandBufferSystem.CreateCommandBuffer();

            // Need this extra variable here so that it can
            // be captured by Entities.ForEach loop below
            var nonTriggerMask = m_NonTriggerMask;

            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((
                Entity e,
                ref DynamicBuffer<StatefulTriggerEvent> triggerEventBuffer,
                ref PlayerPhysicsShapeComponent playerPhysicsShapeComponent) =>
            {
                for (int i = 0; i < triggerEventBuffer.Length; i++)
                {
                    var triggerEvent = triggerEventBuffer[i];
                    var otherEntity = triggerEvent.GetOtherEntity(e);

                    // exclude other triggers and processed events
                    if (!nonTriggerMask.MatchesIgnoreFilter(otherEntity))
                    {
                        continue;
                    }

                    if (triggerEvent.State == EventOverlapState.Enter)
                    {
                        entityTriggerProccesor.ProcessTrigger(otherEntity);
                    }

                    if (triggerEvent.State == EventOverlapState.Exit)
                    {
                        entityTriggerProccesor.ProcessExitTrigger();
                    }
                }
            }).Run();

            m_CommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

        public void Initialize(IPlayerEntityTriggerProccesor entityTriggerProccesor)
        {
            this.entityTriggerProccesor = entityTriggerProccesor;
            Enabled = true;
        }
    }
}