using Spirit604.DotsCity.Core;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Sound.Utils
{
    public static class SoundExtension
    {
        #region EntityManager commands

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity CreateSoundEntity(
            ref this EntityManager entityManager,
            int soundId,
            SoundType soundType = SoundType.Default,
            float volume = 1f)
        {
            if (soundId < 0)
            {
                return Entity.Null;
            }

            var q = GetSoundQuery(entityManager, soundType).GetSingletonEntity();
            var soundEntity = entityManager.Instantiate(q);

            entityManager.SetComponentData(soundEntity, new SoundComponent()
            {
                Id = soundId
            });

            if (volume != 1f)
            {
                entityManager.SetComponentData(soundEntity, new SoundVolume()
                {
                    Volume = volume
                });
            }

            return soundEntity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity CreateTrackedSoundEntity(
            ref this EntityManager entityManager,
            int soundId,
            Entity parentEntity,
            float volume = 1f)
        {
            if (soundId < 0)
            {
                return Entity.Null;
            }

            var soundEntity = CreateSoundEntity(ref entityManager, soundId, SoundType.Tracking, volume);

            entityManager.SetComponentData(soundEntity, new TrackSoundComponent()
            {
                TargetEntity = parentEntity
            });

            return soundEntity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity CreateChildSoundEntity(
            ref this EntityManager entityManager,
            int soundId,
            Entity parentEntity,
            float volume = 1f)
        {
            if (soundId < 0)
            {
                return Entity.Null;
            }

            var soundEntity = CreateSoundEntity(ref entityManager, soundId, SoundType.Default, volume);
            entityManager.AssignChild(parentEntity, soundEntity);

            return soundEntity;
        }

        #endregion

        #region CommandBuffer commands

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity CreateTrackedSoundEntity(
            ref this EntityCommandBuffer commandBuffer,
            Entity soundEntityPrefab,
            int soundId,
            Entity parentEntity,
            float volume = 1f)
        {
            if (soundId < 0)
            {
                return Entity.Null;
            }

            var soundEntity = CreateSoundEntity(ref commandBuffer, soundEntityPrefab, soundId, volume);

            commandBuffer.SetComponent(soundEntity, new TrackSoundComponent()
            {
                TargetEntity = parentEntity
            });

            return soundEntity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity CreateSoundEntity(
            ref this EntityCommandBuffer commandBuffer,
            ref DynamicBuffer<LinkedEntityGroup> parentLinkedEntityGroup,
            Entity soundEntityPrefab,
            int soundId,
            Entity parentEntity,
            float volume = 1f)
        {
            if (soundId < 0)
            {
                return Entity.Null;
            }

            var soundEntity = CreateSoundEntity(ref commandBuffer, soundEntityPrefab, soundId, volume);
            EntityExtension.AssignChild(ref commandBuffer, ref parentLinkedEntityGroup, parentEntity, soundEntity);

            return soundEntity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity CreateSoundEntity(
            ref this EntityCommandBuffer commandBuffer,
            Entity soundEntityPrefab,
            int soundId,
            float volume = 1f)
        {
            if (soundId < 0)
            {
                return Entity.Null;
            }

            var soundEntity = commandBuffer.Instantiate(soundEntityPrefab);

            commandBuffer.SetComponent(soundEntity, new SoundComponent()
            {
                Id = soundId
            });

            if (volume != 1f)
            {
                commandBuffer.SetComponent(soundEntity, new SoundVolume()
                {
                    Volume = volume
                });
            }

            return soundEntity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity CreateSoundEntity(
            ref this EntityCommandBuffer commandBuffer,
            Entity soundEntityPrefab,
            int soundId,
            float3 position,
            float volume = 1f)
        {
            if (soundId < 0)
            {
                return Entity.Null;
            }

            var soundEntity = CreateSoundEntity(ref commandBuffer, soundEntityPrefab, soundId, volume);

            if (soundEntity != Entity.Null && !position.Equals(float3.zero))
            {
                commandBuffer.SetComponent(soundEntity, LocalTransform.FromPosition(position));
            }

            return soundEntity;
        }

        #endregion

        #region Query helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EntityQuery GetSoundQuery(EntityManager entityManager) => GetSoundQuery(entityManager, SoundType.OneShot);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EntityQuery GetSoundQuery(EntityManager entityManager, SoundType soundType)
        {
            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SoundComponent, SoundSharedType, Prefab>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(entityManager);

            query.SetSharedComponentFilter(new SoundSharedType() { SoundType = soundType });

            return query;
        }

        #endregion
    }
}