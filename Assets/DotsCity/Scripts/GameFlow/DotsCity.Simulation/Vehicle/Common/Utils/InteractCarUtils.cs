using Spirit604.DotsCity.Simulation.Car.Sound;
using Spirit604.DotsCity.Simulation.Sound;
using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Car
{
    public static class InteractCarUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnterCar(ref EntityCommandBuffer commandBuffer, ref EntityManager entityManager, ref BlobAssetReference<CarSharedConfig> soundConfig, ref SoundEventPlaybackSystem.Singleton soundEventQueue, Entity entity, bool inViewOfCamera, bool ignite, int runtimeIndex, float3 position)
        {
            if (entityManager.HasComponent<CarEngineStartedTag>(entity) && !entityManager.HasComponent<CarStoppingEngineStartedTag>(entity))
            {
                ignite = false;
            }

            var carIgnitionData = entityManager.GetComponentData<CarIgnitionData>(entity);
            carIgnitionData.IgnitionState = IgnitionState.Default;
            commandBuffer.SetComponent(entity, carIgnitionData);

            EnterCar(ref commandBuffer, ref soundConfig, ref soundEventQueue, entity, inViewOfCamera, ignite, runtimeIndex, position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnterCar(
            ref EntityCommandBuffer commandBuffer,
            ref BlobAssetReference<CarSharedConfig> soundConfig,
            ref SoundEventPlaybackSystem.Singleton soundEventQueue,
            Entity entity,
            bool inViewOfCamera,
            bool ignite,
            int runtimeIndex,
            float3 position)
        {
            commandBuffer.AddComponent<HasDriverTag>(entity);

            if (ignite && inViewOfCamera)
            {
                commandBuffer.AddComponent<CarIgnitionStartedTag>(entity);
                commandBuffer.AddComponent<CarCustomEnginePitchTag>(entity);
            }
            else
            {
                commandBuffer.AddComponent<CarEngineStartedTag>(entity);
            }

            if (inViewOfCamera)
            {
                var localId = (int)CarSoundType.EnterCar;
                var soundId = soundConfig.GetSoundID(runtimeIndex, localId);
                soundEventQueue.PlayOneShot(soundId, position);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExitCar(ref EntityCommandBuffer commandBuffer, ref BlobAssetReference<CarSharedConfig> soundConfig, ref SoundEventPlaybackSystem.Singleton soundEventQueue, float3 position, int runtimeIndex)
        {
            var localId = (int)CarSoundType.ExitCar;
            var soundId = soundConfig.GetSoundID(runtimeIndex, localId);
            soundEventQueue.PlayOneShot(soundId, position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StopEngine(ref EntityCommandBuffer commandBuffer, Entity entity, bool removeDriverAfter = false)
        {
            commandBuffer.AddComponent<CarStoppingEngineStartedTag>(entity);
            commandBuffer.AddComponent<CarCustomEnginePitchTag>(entity);

            if (removeDriverAfter)
            {
                commandBuffer.AddComponent<CarRemoveDriverAfterStopEngineTag>(entity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StopEngine(ref EntityManager entityManager, Entity entity, bool removeDriverAfter = false)
        {
            entityManager.AddComponentData(entity, new CarStoppingEngineStartedTag());
            entityManager.AddComponentData(entity, new CarCustomEnginePitchTag());

            if (removeDriverAfter)
            {
                entityManager.AddComponentData(entity, new CarRemoveDriverAfterStopEngineTag());
            }
        }
    }
}
