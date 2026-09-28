#if !FMOD
using Spirit604.DotsCity.Core.Sound;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Sound
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class UnitySoundSyncPositionSystem : SystemBase
    {
        private EntityQuery updateQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            updateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithNone<TrackSoundComponent>()
                .WithAll<AudioSourceBehaviour>()
                .Build(this);

            RequireForUpdate(updateQuery);
        }

        protected override void OnUpdate()
        {
            Entities
            .WithoutBurst()
            .WithNone<TrackSoundComponent>()
            .ForEach((
                Entity entity,
                AudioSourceBehaviour audioSourceBehaviour,
                in LocalToWorld transform) =>
            {
                audioSourceBehaviour.Transform.position = transform.Position;
            }).Run();
        }
    }
}
#endif