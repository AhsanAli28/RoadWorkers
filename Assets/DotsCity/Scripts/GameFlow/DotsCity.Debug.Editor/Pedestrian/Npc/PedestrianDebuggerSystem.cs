#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    [UpdateInGroup(typeof(DebugGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class PedestrianDebuggerSystem : SimpleSystemBase
    {
        public struct EnabledTag : IComponentData { }

        public struct DebugInfo
        {
            public Entity Entity;
            public Vector3 Position;
            public Quaternion Rotation;
            public float Radius;
        }

        public NativeList<DebugInfo> Pedestrians;
        private EntityQuery circles;
        private bool isInitialized;

        protected override void OnCreate()
        {
            base.OnCreate();
            circles = GetEntityQuery(ComponentType.ReadOnly<CircleColliderComponent>());
            RequireForUpdate<EnabledTag>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (Pedestrians.IsCreated)
            {
                Pedestrians.Dispose();
            }

            isInitialized = false;
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();

            if (Pedestrians.IsCreated)
            {
                Pedestrians.Clear();
            }
        }

        protected override void OnUpdate()
        {
            Initialize();

            if (!Pedestrians.IsCreated)
            {
                return;
            }

            var count = circles.CalculateEntityCount();

            if (Pedestrians.Capacity < count)
            {
                Pedestrians.Dispose();
                Pedestrians = new NativeList<DebugInfo>(count, Allocator.Persistent);
            }

            Pedestrians.Clear();
            var pedestriansParallel = Pedestrians.AsParallelWriter();

            Entities
            .WithNativeDisableParallelForRestriction(pedestriansParallel)
            .ForEach((
                ref Entity entity,
                in CircleColliderComponent circleColliderComponent,
                in LocalTransform transform) =>
            {
                var pedestrianDebugInfo = new DebugInfo()
                {
                    Entity = entity,
                    Position = transform.Position,
                    Rotation = transform.Rotation,
                    Radius = circleColliderComponent.Radius,
                };

                pedestriansParallel.AddNoResize(pedestrianDebugInfo);
            }).ScheduleParallel();
        }

        private void Initialize()
        {
            if (!isInitialized && SystemAPI.HasSingleton<NpcCommonConfigReference>())
            {
                isInitialized = true;
                var npcConfig = SystemAPI.GetSingleton<NpcCommonConfigReference>().Config;
                Pedestrians = new NativeList<DebugInfo>(npcConfig.Value.NpcHashMapCapacity, Allocator.Persistent);
            }
        }
    }
}
#endif