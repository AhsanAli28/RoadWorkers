using Spirit604.DotsCity.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Spirit604.DotsCity.Events
{
    [UpdateInGroup(typeof(PreEarlyJobGroup), OrderFirst = true)]
    public partial class EntityDamageEventConsumerSystem : EventConsumerSystemBase
    {
        protected override void Consume()
        {
            for (int i = 0; i < _triggerStreams.Count; i++)
            {
                var streamReader = _triggerStreams[i].AsReader();

                Dependency = new DamageReactionJob()
                {
                    Reader = streamReader,
                    HitReactionLookup = GetComponentLookup<ProcessHitReactionTag>(false),
                    HealthLookup = GetComponentLookup<HealthComponent>(false),
                    AliveLookup = GetComponentLookup<AliveTag>(false),

                }.Schedule(Dependency);
            }
        }

        [BurstCompile]
        private struct DamageReactionJob : IJob
        {
            #region Public Fields

            [ReadOnly] public NativeStream.Reader Reader;

            public ComponentLookup<ProcessHitReactionTag> HitReactionLookup;
            public ComponentLookup<HealthComponent> HealthLookup;
            public ComponentLookup<AliveTag> AliveLookup;

            #endregion Public Fields

            #region Public Methods

            public void Execute()
            {
                for (int i = 0; i < Reader.ForEachCount; i++)
                {
                    Reader.BeginForEachIndex(i);

                    while (Reader.RemainingItemCount > 0)
                    {
                        var damageHitData = Reader.Read<DamageHitData>();

                        var entity = damageHitData.DamagedEntity;

                        if (!HealthLookup.HasComponent(entity))
                        {
                            continue;
                        }

                        var healthComponent = HealthLookup[entity];

                        healthComponent = healthComponent.Hit(damageHitData.Damage, damageHitData.HitDirection, damageHitData.HitPosition);
                        healthComponent.ForceMultiplier = damageHitData.ForceMultiplier;
                        healthComponent.DamageDealerID = damageHitData.DamageDealerID;

                        if (!healthComponent.IsAlive)
                        {
                            AliveLookup.SetComponentEnabled(entity, false);
                        }
                        else if (HitReactionLookup.HasComponent(entity))
                        {
                            HitReactionLookup.SetComponentEnabled(entity, true);
                        }

                        HealthLookup[entity] = healthComponent;
                    }

                    Reader.EndForEachIndex();
                }
            }

            #endregion Public Methods
        }
    }
}
