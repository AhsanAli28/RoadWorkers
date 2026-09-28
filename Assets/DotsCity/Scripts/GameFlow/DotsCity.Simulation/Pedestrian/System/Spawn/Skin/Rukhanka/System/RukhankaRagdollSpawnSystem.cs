#if RUKHANKA
using Rukhanka;
using Spirit604.DotsCity.Simulation.Npc;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{

    [UpdateInGroup(typeof(BeginSimulationGroup), OrderLast = true)]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    [RequireMatchingQueriesForUpdate]
    public partial class RukhankaRagdollSpawnSystem : BeginSimulationSystemBase
    {
        private NpcDeathEventConsumerSystem npcDeathEventConsumerSystem;
        private EntityQuery npcQuery;
        private EntityQuery chunkNpcQuery;
        private NativeStream emptyStream;

        public bool TriggerSupported { get; set; } = false;

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (emptyStream.IsCreated)
            {
                emptyStream.Dispose();
            }
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            if (!TriggerSupported)
            {
                if (!emptyStream.IsCreated)
                {
                    emptyStream = new NativeStream(0, Allocator.Persistent);
                }
            }
        }

        protected override void OnUpdate()
        {
            //NativeStream.Writer writer = default;

            //if (TriggerSupported)
            //{
            //    writer = npcDeathEventConsumerSystem.CreateConsumerWriter(chunkNpcQuery.CalculateChunkCount());
            //}
            //else
            //{
            //    writer = emptyStream.AsWriter();
            //}

            var commandBuffer = GetCommandBuffer();

            Dependency = new RagdollJob()
            {
                CommandBuffer = commandBuffer,
                RukhankaRagdollPrefabContainer = SystemAPI.GetSingletonBuffer<RukhankaRagdollPrefabContainer>()

            }.Schedule(Dependency);

            AddCommandBufferForProducer();

            //if (TriggerSupported)
            //{
            //    npcDeathEventConsumerSystem.RegisterTriggerDependency(Dependency);
            //}
        }

        public void Initialize(
            NpcDeathEventConsumerSystem npcDeathEventConsumerSystem)
        {
            this.npcDeathEventConsumerSystem = npcDeathEventConsumerSystem;
        }

        [WithAll(typeof(RagdollActivateEventTag), typeof(Simulate))]
        [BurstCompile]
        public partial struct RagdollJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public DynamicBuffer<RukhankaRagdollPrefabContainer> RukhankaRagdollPrefabContainer;

            void Execute(
                Entity entity,
                in PedestrianCommonSettings pedestrianCommonSettings,
                in DynamicBuffer<AnimationToProcessComponent> animationToProcessComponent,
                in DynamicBuffer<AnimatorControllerParameterComponent> animatorControllerParameterComponent,
                in RagdollComponent ragdollComponent,
                in LocalTransform transform)
            {
                var ragdollPrefab = RukhankaRagdollPrefabContainer[pedestrianCommonSettings.SkinIndex].PrefabEntity;
                var ragdollEntity = CommandBuffer.Instantiate(ragdollPrefab);

                CommandBuffer.SetComponent(ragdollEntity, transform);
                CommandBuffer.SetComponent(ragdollEntity, ragdollComponent);

                for (int i = 0; i < animationToProcessComponent.Length; i++)
                {
                    CommandBuffer.AppendToBuffer(ragdollEntity, animationToProcessComponent[i]);
                }

                for (int i = 0; i < animatorControllerParameterComponent.Length; i++)
                {
                    CommandBuffer.AppendToBuffer(ragdollEntity, animatorControllerParameterComponent[i]);
                }
            }
        }
    }
}
#endif