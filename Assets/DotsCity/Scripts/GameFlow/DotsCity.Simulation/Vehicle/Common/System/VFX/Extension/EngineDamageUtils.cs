using Spirit604.DotsCity.Core;
using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Car
{
    public static class EngineDamageUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ProcessCarEngineDamage(
            Entity entity,
            ref EntityCommandBuffer commandBuffer,
            ref EngineDamageData carEngineDamageData,
            in LocalTransform transform,
            in HealthComponent healthComponent,
            in BlobAssetReference<EngineStateSettings> carEngineSettingsLocal,
            in DynamicBuffer<EngineStateElement> engineSettingsBuffer)
        {
            ref var smokeEngineStates = ref carEngineSettingsLocal.Value.Settings;

            float damagedPercent = (float)(healthComponent.MaxValue - healthComponent.Value) / (float)healthComponent.MaxValue;

            int state = GetDamagedState(damagedPercent, ref smokeEngineStates);

            if (carEngineDamageData.CurrentState != state)
            {
                carEngineDamageData.CurrentState = state;

                if (carEngineDamageData.RelatedEntity != Entity.Null)
                {
                    PoolEntityUtils.DestroyEntity(ref commandBuffer, carEngineDamageData.RelatedEntity);
                }

                var vfxEntity = commandBuffer.Instantiate(engineSettingsBuffer[state].Prefab);

                var carEngineDamageStateTemp = carEngineDamageData;
                carEngineDamageStateTemp.RelatedEntity = vfxEntity;
                commandBuffer.SetComponent(entity, carEngineDamageStateTemp); // Made for commandbuffer playback issue

                commandBuffer.SetComponent(vfxEntity, new EntityTrackerComponent()
                {
                    LinkedEntity = entity,
                    Offset = carEngineDamageData.SpawnOffset,
                    HasOffset = true
                });

                var initialPosition = transform.Position + math.mul(transform.Rotation, carEngineDamageData.SpawnOffset);
                commandBuffer.SetComponent(vfxEntity, LocalTransform.FromPositionRotation(initialPosition, quaternion.identity));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetDamagedState(float damagedPercent, ref BlobArray<EngineStateData> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].MinHp <= damagedPercent && damagedPercent < buffer[i].MaxHp)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}