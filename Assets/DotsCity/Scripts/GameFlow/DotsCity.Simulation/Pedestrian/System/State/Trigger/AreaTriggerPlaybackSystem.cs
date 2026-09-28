using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadEventPlaybackGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct AreaTriggerPlaybackSystem : ISystem
    {
        #region Helper types

        public struct AreaTriggerInfo
        {
            public readonly Entity NpcEntity;
            public readonly float3 TriggerPosition;
            public readonly TriggerAreaType TriggerAreaType;

            public AreaTriggerInfo(Entity npcEntity, float3 triggerPosition, TriggerAreaType triggerAreaType)
            {
                NpcEntity = npcEntity;
                TriggerPosition = triggerPosition;
                TriggerAreaType = triggerAreaType;
            }
        }

        public struct Singleton : IComponentData
        {
            public NativeQueue<AreaTriggerInfo> EventQueue;
        }

        private NativeQueue<AreaTriggerInfo> eventQueue;

        #endregion

        #region Variables

        private EntityQuery triggerQuery;

        #endregion

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            triggerQuery = SystemAPI.QueryBuilder()
                .WithAll<TriggerComponent>()
                .Build();

            var configQuery = SystemAPI.QueryBuilder()
                .WithAll<TriggerConfigReference>()
                .Build();

            state.RequireForUpdate(triggerQuery);
            state.RequireForUpdate(configQuery);

            eventQueue = new NativeQueue<AreaTriggerInfo>(Allocator.Persistent);

            state.EntityManager.AddComponentData(state.SystemHandle, new Singleton()
            {
                EventQueue = eventQueue
            });
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            eventQueue.Dispose();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            if (eventQueue.Count == 0) return;

            var buffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var config = SystemAPI.GetSingleton<TriggerConfigReference>();
            var triggerConsumerTagLookup = SystemAPI.GetComponentLookup<TriggerConsumerTag>();

            while (eventQueue.Count > 0)
            {
                var trigger = eventQueue.Dequeue();
                var entity = trigger.NpcEntity;

                // Entity doesn't exist
                if (!triggerConsumerTagLookup.HasComponent(entity)) continue;

                AreaTriggerUtils.AddTrigger(
                      ref buffer,
                      in config,
                      entity,
                      in trigger);
            }
        }
    }
}