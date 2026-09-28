using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct UnloadHybridSkinSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<InViewOfCameraTag, DisableUnloadSkinTag>()
                .WithAll<HasSkinTag, HybridLegacySkinTag, PedestrianCommonSettings>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            float timestamp = (float)SystemAPI.Time.ElapsedTime;

            foreach (var (transform, pedestrianCommonSettings, entity) in
                SystemAPI.Query<SystemAPI.ManagedAPI.UnityEngineComponent<Transform>, RefRW<PedestrianCommonSettings>>()
                .WithNone<InViewOfCameraTag, DisableUnloadSkinTag>()
                .WithAll<HasSkinTag, HybridLegacySkinTag>()
                .WithEntityAccess())
            {
                pedestrianCommonSettings.ValueRW.LoadSkinTimestamp = timestamp;
                transform.Value.gameObject.ReturnToPool();

                commandBuffer.RemoveComponent<Transform>(entity);
                commandBuffer.RemoveComponent<Animator>(entity);
                commandBuffer.SetComponentEnabled<HasSkinTag>(entity, false);
                commandBuffer.SetComponentEnabled<CopyTransformToGameObject>(entity, false);

                commandBuffer.SetSharedComponent(entity, new WorldEntitySharedType(EntityWorldType.PureEntity));
            }
        }
    }
}