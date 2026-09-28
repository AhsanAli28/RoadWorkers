#if !FMOD
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Sound;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Sound
{
    [UpdateInGroup(typeof(MonoSyncGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class UnitySoundTrackTargetEntitySystem : SystemBase
    {
        private EntityQuery updateQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            updateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<AudioSourceBehaviour, TrackSoundComponent>()
                .Build(this);

            RequireForUpdate(updateQuery);
        }

        protected override void OnUpdate()
        {
            var worldTransformLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            var pooledEventLookup = SystemAPI.GetComponentLookup<PooledEventTag>(false);

            Entities
            .WithoutBurst()
            .WithReadOnly(worldTransformLookup)
            .WithNativeDisableContainerSafetyRestriction(pooledEventLookup)
            .ForEach((
                Entity entity,
                AudioSourceBehaviour audioSourceBehaviour,
                in TrackSoundComponent trackSoundComponent,
                in LocalTransform transform) =>
            {
                if (worldTransformLookup.HasComponent(trackSoundComponent.TargetEntity))
                {
                    var position = worldTransformLookup[trackSoundComponent.TargetEntity].Position;
                    audioSourceBehaviour.Transform.position = position;
                }
                else
                {
                    pooledEventLookup.SetComponentEnabled(entity, true);
                }
            }).Run();
        }
    }
}
#endif