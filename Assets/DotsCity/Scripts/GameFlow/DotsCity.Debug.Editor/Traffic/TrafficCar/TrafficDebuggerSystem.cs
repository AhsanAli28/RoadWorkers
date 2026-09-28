using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
#if UNITY_EDITOR

    [UpdateInGroup(typeof(DebugGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class TrafficDebuggerSystem : SystemBase
    {
        public struct EnabledTag : IComponentData { }

        public struct TrafficDebugInfo
        {
            public Entity Entity;
            public Vector3 Position;
            public Quaternion Rotation;
            public Bounds Bounds;
            public bool HasObstacle;
        }

        public NativeList<TrafficDebugInfo> Traffics;

        protected override void OnCreate()
        {
            base.OnCreate();

            RequireForUpdate<TrafficSpawnerConfigBlobReference>();
            RequireForUpdate<TrafficDebuggerSystem.EnabledTag>();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            if (!Traffics.IsCreated)
            {
                var trafficSpawnerConfig = SystemAPI.GetSingleton<TrafficSpawnerConfigBlobReference>().Reference.Value;
                Traffics = new NativeList<TrafficDebugInfo>(trafficSpawnerConfig.HashMapCapacity, Allocator.Persistent);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (Traffics.IsCreated)
                Traffics.Dispose();
        }

        protected override void OnUpdate()
        {
            if (!Traffics.IsCreated)
                return;

            var depJob = Dependency;

            Traffics.Clear();
            var trafficsParallel = Traffics.AsParallelWriter();

            var trafficObstacleComponentLookup = SystemAPI.GetComponentLookup<TrafficObstacleComponent>(true);
            var trafficNpcObstacleComponentLookup = SystemAPI.GetComponentLookup<TrafficNpcObstacleComponent>(true);
            var trafficRaycastObstacleComponentLookup = SystemAPI.GetComponentLookup<TrafficRaycastObstacleComponent>(true);

            Dependency = Entities
            .WithNativeDisableParallelForRestriction(trafficsParallel)
            .WithNativeDisableContainerSafetyRestriction(trafficsParallel)
            .WithReadOnly(trafficObstacleComponentLookup)
            .WithReadOnly(trafficNpcObstacleComponentLookup)
            .WithReadOnly(trafficRaycastObstacleComponentLookup)
            .WithAll<TrafficTag>()
            .ForEach((
                ref Entity entity,
                in BoundsComponent boundsComponent,
                in LocalTransform transform) =>
            {
                bool hasObstacle = false;

                if (!hasObstacle && trafficObstacleComponentLookup.HasComponent(entity))
                {
                    hasObstacle = trafficObstacleComponentLookup[entity].HasObstacle;
                }

                if (!hasObstacle && trafficNpcObstacleComponentLookup.HasComponent(entity))
                {
                    hasObstacle = trafficNpcObstacleComponentLookup[entity].HasObstacle;
                }

                if (!hasObstacle && trafficRaycastObstacleComponentLookup.HasComponent(entity))
                {
                    hasObstacle = trafficRaycastObstacleComponentLookup[entity].HasObstacle;
                }

                var trafficDebugInfo = new TrafficDebugInfo()
                {
                    Entity = entity,
                    Position = transform.Position,
                    Rotation = transform.Rotation,
                    HasObstacle = hasObstacle,
                    Bounds = new Bounds()
                    {
                        center = boundsComponent.Center,
                        extents = boundsComponent.Extents
                    }
                };

                trafficsParallel.AddNoResize(trafficDebugInfo);

            }).ScheduleParallel(depJob);
        }

        public bool TryToGetEntity(int entityIndex, out Entity entity)
        {
            entity = Entity.Null;

            for (int i = 0; i < Traffics.Length; i++)
            {
                if (Traffics[i].Entity.Index == entityIndex)
                {
                    entity = Traffics[i].Entity;
                    return true;
                }
            }

            return false;
        }
    }

#endif
}
