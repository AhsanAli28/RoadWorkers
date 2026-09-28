#if UNITY_EDITOR
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    [BurstCompile]
    public partial struct TrafficRaycastGizmosSystem : ISystem
    {
        public struct Singleton : IComponentData
        {
            public NativeParallelMultiHashMap<Entity, BoxCastInfo> TrafficRaycastDebugInfo;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(Entity entity, float3 origin, float3 halfExtents, quaternion orientation, float3 direction, float maxDistance, bool hit)
            {
                var boxCastInfo = new BoxCastInfo()
                {
                    origin = origin,
                    halfExtents = halfExtents,
                    orientation = orientation,
                    direction = direction,
                    MaxDistance = maxDistance,
                    HasHit = hit
                };

                Add(entity, boxCastInfo);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(Entity entity, BoxCastInfo boxCastInfo)
            {
                TrafficRaycastDebugInfo.Add(entity, boxCastInfo);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Clear()
            {
                TrafficRaycastDebugInfo.Clear();
            }
        }

        public struct BoxCastInfo
        {
            public Entity Entity;
            public float3 origin;
            public float3 halfExtents;
            public quaternion orientation;
            public float3 direction;
            public float MaxDistance;
            public bool HasHit;
        }

        private NativeParallelMultiHashMap<Entity, BoxCastInfo> trafficRaycastDebugInfo;

        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TrafficSpawnerConfigBlobReference>();
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            if (trafficRaycastDebugInfo.IsCreated)
                trafficRaycastDebugInfo.Dispose();
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            if (!trafficRaycastDebugInfo.IsCreated)
            {
                var trafficSpawnerConfig = SystemAPI.GetSingleton<TrafficSpawnerConfigBlobReference>().Reference;
                trafficRaycastDebugInfo = new NativeParallelMultiHashMap<Entity, BoxCastInfo>(trafficSpawnerConfig.Value.HashMapCapacity, Allocator.Persistent);

                var entity = state.EntityManager.CreateEntity();

                state.EntityManager.AddComponentData(entity, new Singleton()
                {
                    TrafficRaycastDebugInfo = trafficRaycastDebugInfo
                });
            }
        }
    }
}
#endif