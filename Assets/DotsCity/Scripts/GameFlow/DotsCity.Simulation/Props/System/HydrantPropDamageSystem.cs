using Spirit604.DotsCity.Simulation.VFX;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Level.Props
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class HydrantPropDamageSystem : SystemBase
    {
        private struct ActivateData
        {
            public Entity Entity;
            public VFXType VfxType;
            public float3 Position;
        }

        private VFXFactory vFXFactory;
        private EntityArchetype vfxEntityArchetype;
        private EntityQuery query;

        private NativeList<ActivateData> activatedEntities;

        protected override void OnCreate()
        {
            base.OnCreate();

            vfxEntityArchetype = EntityManager.CreateArchetype(typeof(ParticleSystem));

            activatedEntities = new NativeList<ActivateData>(10, Allocator.Persistent);

            query = new EntityQueryBuilder(Allocator.Temp)
                .WithDisabledRW<PropsDamagedTag>()
                .WithAllRW<PropsProcessDamageTag, PropsVFXData>()
                .WithAll<HydrantTag, LocalToWorld>()
                .Build(this);

            RequireForUpdate(query);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (activatedEntities.IsCreated)
            {
                activatedEntities.Dispose();
            }
        }

        protected override void OnUpdate()
        {
            var vfxEntityArchetypeLocal = vfxEntityArchetype;

            var entityCount = query.CalculateEntityCount();

            if (activatedEntities.Length < entityCount)
            {
                activatedEntities.SetCapacity(entityCount);
            }

            var hydrantDamageJob = new HydrantDamageJob()
            {
                ActivatedEntities = activatedEntities
            };

            Dependency = hydrantDamageJob.Schedule(query, Dependency);

            Dependency.Complete();

            for (int i = 0; i < activatedEntities.Length; i++)
            {
                var entity = activatedEntities[i].Entity;
                var vfxEntity = EntityManager.CreateEntity(vfxEntityArchetypeLocal);

                ParticleSystem particleSystem = vFXFactory.GetVFX(activatedEntities[i].VfxType).GetComponent<ParticleSystem>();
                particleSystem.Play();

                EntityManager.SetComponentData(entity, new PropsVFXData()
                {
                    RelatedEntity = vfxEntity,
                    VFXType = activatedEntities[i].VfxType
                });

                particleSystem.gameObject.transform.position = activatedEntities[i].Position;
                EntityManager.AddComponentObject(vfxEntity, particleSystem);
            }

            activatedEntities.Clear();
        }

        public void Initialize(VFXFactory vFXFactory)
        {
            this.vFXFactory = vFXFactory;
        }

        [BurstCompile]
        private partial struct HydrantDamageJob : IJobEntity
        {
            public NativeList<ActivateData> ActivatedEntities;

            void Execute(
                Entity entity,
                ref PropsVFXData propsVFXData,
                EnabledRefRW<PropsProcessDamageTag> propsProcessDamageTagRW,
                EnabledRefRW<PropsDamagedTag> propsDamagedTagRW,
                in LocalToWorld worldTransform)
            {
                propsProcessDamageTagRW.ValueRW = false;
                propsDamagedTagRW.ValueRW = true;

                ActivatedEntities.Add(new ActivateData()
                {
                    Entity = entity,
                    VfxType = propsVFXData.VFXType,
                    Position = worldTransform.Position,
                });
            }
        }
    }
}
