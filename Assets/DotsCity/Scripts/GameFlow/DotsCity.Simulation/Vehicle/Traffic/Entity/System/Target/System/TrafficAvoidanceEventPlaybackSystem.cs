using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(MainThreadEventPlaybackGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficAvoidanceEventPlaybackSystem : ISystem
    {
        #region Helper types

        public struct AvoidanceEventData
        {
            public Entity Entity;
            public float3 Destination;
            public bool CustomProcess;
            public bool BackwardDirection;
            public float AchieveDistance;
            public float CustomDuration;
            public VehicleBoundsPoint VehicleBoundsPoint;
            public bool RemoveComponent;
        }

        public struct Singleton : IComponentData
        {
            internal UnsafeQueue<AvoidanceEventData> EventQueue;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddEvent(AvoidanceEventData avoidanceEventData)
            {
                EventQueue.Enqueue(avoidanceEventData);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RemoveEventTag(Entity entity)
            {
                EventQueue.Enqueue(new AvoidanceEventData()
                {
                    Entity = entity,
                    RemoveComponent = true
                });
            }
        }

        private UnsafeQueue<AvoidanceEventData> eventQueue;

        #endregion

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            eventQueue = new UnsafeQueue<AvoidanceEventData>(Allocator.Persistent);

            state.EntityManager.AddComponentData(state.SystemHandle, new Singleton()
            {
                EventQueue = eventQueue
            });
        }

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            eventQueue.Dispose();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            if (eventQueue.IsEmpty()) return;

            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var trafficLookup = SystemAPI.GetComponentLookup<TrafficTag>(true);

            while (eventQueue.Count > 0)
            {
                var eventData = eventQueue.Dequeue();
                var entity = eventData.Entity;

                // Entity doesn't exist
                if (!trafficLookup.HasComponent(entity)) continue;

                if (!eventData.RemoveComponent)
                {
                    commandBuffer.AddComponent(entity,
                         new TrafficCustomDestinationComponent()
                         {
                             Destination = eventData.Destination,
                             CustomProcess = eventData.CustomProcess,
                             BackwardDirection = eventData.BackwardDirection,
                             AchieveDistance = eventData.AchieveDistance,
                             CustomDuration = eventData.CustomDuration,
                             VehicleBoundsPoint = eventData.VehicleBoundsPoint,
                         });
                }
                else
                {
                    commandBuffer.RemoveComponent<TrafficCustomDestinationComponent>(entity);
                }
            }
        }
    }
}