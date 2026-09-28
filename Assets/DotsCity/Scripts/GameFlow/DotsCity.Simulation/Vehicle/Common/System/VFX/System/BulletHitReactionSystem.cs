using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car.Sound;
using Spirit604.DotsCity.Simulation.Sound;
using Spirit604.DotsCity.Simulation.VFX;
using Spirit604.Extensions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Car
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class BulletHitReactionSystem : BeginSimulationSystemBase
    {
        private struct BulletCollisionEventData
        {
            public float3 Position;
            public float3 HitDirection;
        }

        private VFXFactory vfxFactory;
        private EntityQuery updateQuery;
        private NativeList<BulletCollisionEventData> hits;

        protected override void OnCreate()
        {
            base.OnCreate();

            updateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ProcessHitReactionTag, CarRelatedHullComponent>()
                .Build(this);

            RequireForUpdate(updateQuery);
            RequireForUpdate<CarHitReactProviderSystem.FactoryCreatedEventTag>();

            Enabled = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (hits.IsCreated)
            {
                hits.Dispose();
            }
        }

        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            var soundConfig = SystemAPI.GetSingleton<CarSoundCommonConfigReference>().Config;
            var commandBuffer = GetCommandBuffer();

            bool hasEngineDamageSettings = false;
            BlobAssetReference<EngineStateSettings> carEngineSettingsLocal = default;
            Entity engineSettingsEntity = Entity.Null;

            if (SystemAPI.HasSingleton<EngineStateSettingsHolder>())
            {
                hasEngineDamageSettings = true;
                carEngineSettingsLocal = SystemAPI.GetSingleton<EngineStateSettingsHolder>().SettingsReference;
                engineSettingsEntity = SystemAPI.GetSingletonEntity<EngineStateSettingsHolder>();
            }

            hits.EnsureCapacity(updateQuery);

            var hitsLocal = hits;

            var soundEventQueue = SystemAPI.GetSingleton<SoundEventPlaybackSystem.Singleton>();
            var materialMeshInfoLookup = SystemAPI.GetComponentLookup<MaterialMeshInfo>(true);
            var renderBoundsLookup = SystemAPI.GetComponentLookup<RenderBounds>(true);
            var carHitReactionLookup = SystemAPI.GetComponentLookup<CarHitReactionData>(true);

            var settingsBufferLookup = SystemAPI.GetBufferLookup<EngineStateElement>(true);
            var carModelLookup = SystemAPI.GetComponentLookup<CarModelComponent>(true);
            var carEngineDamageDatas = SystemAPI.GetComponentLookup<EngineDamageData>(false);

            var carHitReactionConfigReference = SystemAPI.GetSingleton<CarHitReactionConfigReference>();

            var batchMeshes = CarHitReactProviderSystem.BatchMeshes;
            var batchMaterials = CarHitReactProviderSystem.BatchMaterials;
            var takenIndexes = CarHitReactProviderSystem.TakenIndexes;
            var prefabEntity = CarHitReactProviderSystem.PrefabEntity;

            Entities
            .WithBurst()
            .WithReadOnly(materialMeshInfoLookup)
            .WithReadOnly(renderBoundsLookup)
            .WithReadOnly(carHitReactionLookup)
            .WithReadOnly(settingsBufferLookup)
            .WithReadOnly(carModelLookup)
            .WithReadOnly(batchMeshes)
            .WithReadOnly(batchMaterials)
            .WithNativeDisableContainerSafetyRestriction(takenIndexes)
            .WithAll<ProcessHitReactionTag>()
            .ForEach((
                Entity entity,
                in HealthComponent healthComponent,
                in CarRelatedHullComponent carRelatedHullComponent,
                in LocalTransform transform) =>
            {
                if (carHitReactionLookup.HasComponent(entity) && carModelLookup.HasComponent(entity))
                {
                    var carHitReactionData = carHitReactionLookup[entity];
                    var carModel = carModelLookup[entity];

                    var hitMeshEntity = carHitReactionData.HitMeshEntity;
                    bool newEntity = false;

                    if (hitMeshEntity == Entity.Null)
                    {
                        newEntity = true;
                        hitMeshEntity = commandBuffer.Instantiate(prefabEntity);

                        var position = transform.Position + carHitReactionData.Offset;

                        commandBuffer.SetComponent(hitMeshEntity, LocalTransform.FromPositionRotation(position, transform.Rotation));
                        commandBuffer.SetComponent(hitMeshEntity, renderBoundsLookup[carRelatedHullComponent.HullEntity]);

                        commandBuffer.SetComponent(hitMeshEntity, new HitReactionInitComponent()
                        {
                            VehicleEntity = entity
                        });

                        commandBuffer.SetComponent(hitMeshEntity, new EntityTrackerComponent()
                        {
                            LinkedEntity = entity,
                            Offset = carHitReactionData.Offset,
                            HasOffset = true
                        });

                        commandBuffer.SetComponent(hitMeshEntity, new CarRelatedHullComponent()
                        {
                            HullEntity = carRelatedHullComponent.HullEntity
                        });
                    }

                    int takenIndex = -1;

                    bool meshIsTaken = false;

                    if (newEntity)
                    {
                        var initialIndex = carModel.LocalIndex * carHitReactionConfigReference.Config.Value.PoolSize;
                        var endIndex = initialIndex + carHitReactionConfigReference.Config.Value.PoolSize;

                        for (int index = initialIndex; index < endIndex; index++)
                        {
                            if (!takenIndexes.Contains(index))
                            {
                                takenIndexes.Add(index);

                                takenIndex = index;

                                commandBuffer.AddComponent(hitMeshEntity, new CarHitReactionTakenIndex()
                                {
                                    TakenIndex = index
                                });

                                meshIsTaken = true;
                            }
                        }
                    }
                    else
                    {
                        meshIsTaken = true;
                    }

                    if (meshIsTaken)
                    {
                        if (takenIndex >= 0)
                        {
                            commandBuffer.SetComponent(hitMeshEntity, new MaterialMeshInfo()
                            {
                                MaterialID = batchMaterials[takenIndex],
                                MeshID = batchMeshes[takenIndex],
                            });
                        }

                        var hitReactionState = new HitReactionStateComponent()
                        {
                            ActivateTime = currentTime + carHitReactionConfigReference.Config.Value.EffectDuration
                        };

                        commandBuffer.SetComponent(hitMeshEntity, hitReactionState);

                        if (materialMeshInfoLookup.HasComponent(carRelatedHullComponent.HullEntity))
                        {
                            if (materialMeshInfoLookup.IsComponentEnabled(carRelatedHullComponent.HullEntity))
                            {
                                commandBuffer.SetComponentEnabled<MaterialMeshInfo>(carRelatedHullComponent.HullEntity, false);
                            }
                        }
                    }
                }

                soundEventQueue.PlayOneShot(soundConfig.Value.BulletHitSoundId, transform.Position);

                hitsLocal.Add(new BulletCollisionEventData()
                {
                    Position = healthComponent.HitPosition,
                    HitDirection = healthComponent.HitDirection
                });

                if (hasEngineDamageSettings && carEngineDamageDatas.HasComponent(entity))
                {
                    var carEngineDamageData = carEngineDamageDatas[entity];
                    var settingsBuffer = settingsBufferLookup[engineSettingsEntity];

                    EngineDamageUtils.ProcessCarEngineDamage(
                        entity,
                        ref commandBuffer,
                        ref carEngineDamageData,
                        in transform,
                        in healthComponent,
                        in carEngineSettingsLocal,
                        in settingsBuffer);

                    carEngineDamageDatas[entity] = carEngineDamageData;
                }

                commandBuffer.SetComponentEnabled<ProcessHitReactionTag>(entity, false);

            }).Schedule();

            AddCommandBufferForProducer();

            Dependency.Complete();

            if (hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    BulletHitReactionUtils.CreateBulletVfx(vfxFactory, hits[i].Position, hits[i].HitDirection);
                }

                hits.Clear();
            }
        }

        public void Initialize(VFXFactory vfxFactory)
        {
            this.vfxFactory = vfxFactory;
            hits = new NativeList<BulletCollisionEventData>(10, Allocator.Persistent);
            Enabled = true;
        }
    }
}