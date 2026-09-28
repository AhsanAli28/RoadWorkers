using Spirit604.AnimationBaker.Entities;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.DotsCity.Simulation.Pedestrian.Authoring;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadEventGroup), OrderLast = true)]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class RagdollSystem : BeginSimulationSystemBase
    {
        private NpcDeathEventConsumerSystem npcDeathEventConsumerSystem;
        private PedestrianRagdollSpawner pedestrianRagdollSpawner;
        private EntityQuery npcQuery;
        private EntityQuery chunkNpcQuery;
        private NativeQueue<ActivateRagdollData> activateRagdollQueue = new NativeQueue<ActivateRagdollData>(Allocator.Persistent);
        private NativeStream emptyStream;

        public bool TriggerSupported { get; set; } = false;

        protected override void OnCreate()
        {
            base.OnCreate();

            npcQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithNone<PooledEventTag>()
                .WithAll<RagdollActivateEventTag>()
                .WithAll<StateComponent, HasSkinTag, PedestrianCommonSettings, RagdollComponent>()
                .Build(this);

            chunkNpcQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithNone<PooledEventTag>()
                .WithAll<RagdollActivateEventTag>()
                .WithAll<StateComponent, HasSkinTag, PedestrianCommonSettings, RagdollComponent>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                .Build(this);

            RequireForUpdate(npcQuery);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (activateRagdollQueue.IsCreated)
            {
                activateRagdollQueue.Dispose();
            }

            if (emptyStream.IsCreated)
            {
                emptyStream.Dispose();
            }
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            if (!TriggerSupported)
            {
                if (!emptyStream.IsCreated)
                {
                    emptyStream = new NativeStream(0, Allocator.Persistent);
                }
            }
        }

        protected override void OnUpdate()
        {
            NativeStream.Writer writer = default;

            if (TriggerSupported)
            {
                writer = npcDeathEventConsumerSystem.CreateConsumerWriter(chunkNpcQuery.CalculateChunkCount());
            }
            else
            {
                writer = emptyStream.AsWriter();
            }

            var conversionSettings = SystemAPI.GetSingleton<MiscConversionSettingsReference>();

            SystemAPI.TryGetSingleton<CrowdSkinProviderSystem.Singleton>(out var crowdSkinProviderSystem);

            Dependency = new ActivateRagdollJob()
            {
                Writer = writer,
                PooledEventTagHandle = SystemAPI.GetComponentTypeHandle<PooledEventTag>(false),
                CommonSettingsHandle = SystemAPI.GetComponentTypeHandle<PedestrianCommonSettings>(true),
                RagdollComponentHandle = SystemAPI.GetComponentTypeHandle<RagdollComponent>(true),
                RagdollActivatedComponentHandle = SystemAPI.GetComponentTypeHandle<RagdollActivateEventTag>(true),
                SkinAnimatorDataHandle = SystemAPI.GetComponentTypeHandle<SkinAnimatorData>(true),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                CrowdSkinProvider = crowdSkinProviderSystem,
                AnimatorDataProvider = SystemAPI.GetSingleton<AnimatorDataProviderSystem.Singleton>(),
                ConversionSettings = conversionSettings,
                TriggerSupported = this.TriggerSupported,
                RagdollQueue = this.activateRagdollQueue,
                Timestamp = (float)SystemAPI.Time.ElapsedTime
            }.Schedule(npcQuery, Dependency);

            if (TriggerSupported)
            {
                npcDeathEventConsumerSystem.RegisterTriggerDependency(Dependency);
            }

            if (conversionSettings.Config.Value.DefaultRagdollSystem)
            {
                Dependency.Complete();

                while (activateRagdollQueue.Count > 0)
                {
                    var ragdollData = activateRagdollQueue.Dequeue();
                    pedestrianRagdollSpawner.SpawnRagdoll(ragdollData);
                }
            }
        }

        public void Initialize(PedestrianRagdollSpawner pedestrianRagdollSpawner,
            NpcDeathEventConsumerSystem npcDeathEventConsumerSystem)
        {
            this.pedestrianRagdollSpawner = pedestrianRagdollSpawner;
            this.npcDeathEventConsumerSystem = npcDeathEventConsumerSystem;
        }

        [BurstCompile]
        public struct ActivateRagdollJob : IJobChunk
        {
            public NativeStream.Writer Writer;

            public ComponentTypeHandle<PooledEventTag> PooledEventTagHandle;
            [ReadOnly] public ComponentTypeHandle<PedestrianCommonSettings> CommonSettingsHandle;
            [ReadOnly] public ComponentTypeHandle<RagdollComponent> RagdollComponentHandle;
            [ReadOnly] public ComponentTypeHandle<RagdollActivateEventTag> RagdollActivatedComponentHandle;
            [ReadOnly] public ComponentTypeHandle<SkinAnimatorData> SkinAnimatorDataHandle;
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public CrowdSkinProviderSystem.Singleton CrowdSkinProvider;
            [ReadOnly] public AnimatorDataProviderSystem.Singleton AnimatorDataProvider;

            [ReadOnly] public bool TriggerSupported;

            [NativeDisableParallelForRestriction] public NativeQueue<ActivateRagdollData> RagdollQueue;

            [ReadOnly] public MiscConversionSettingsReference ConversionSettings;

            [ReadOnly] public float Timestamp;

            public void Execute(in ArchetypeChunk chunk, int chunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var pedestrianCommonSettingss = chunk.GetNativeArray(ref CommonSettingsHandle);
                var ragdollComponents = chunk.GetNativeArray(ref RagdollComponentHandle);
                var entities = chunk.GetNativeArray(EntityTypeHandle);

                NativeArray<SkinAnimatorData> skinAnimatorDataHandle = default;
                bool gpuSkin = false;

                if (chunk.Has(ref SkinAnimatorDataHandle))
                {
                    gpuSkin = true;
                    skinAnimatorDataHandle = chunk.GetNativeArray(ref SkinAnimatorDataHandle);
                }

                if (TriggerSupported)
                {
                    Writer.BeginForEachIndex(chunkIndex);
                }

                for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    if (useEnabledMask && !chunk.IsComponentEnabled(ref RagdollActivatedComponentHandle, entityIndex))
                        continue;

                    var pedestrianCommonSettings = pedestrianCommonSettingss[entityIndex];
                    var ragdollComponent = ragdollComponents[entityIndex];
                    var entity = entities[entityIndex];

                    SkinAnimatorData skinAnimatorData = default;

                    if (gpuSkin)
                        skinAnimatorData = skinAnimatorDataHandle[entityIndex];

                    if (ConversionSettings.Config.Value.DefaultRagdollSystem)
                    {
                        PoolEntityUtils.DestroyEntity(in chunk, ref PooledEventTagHandle, entityIndex);
                    }

                    if (TriggerSupported)
                    {
                        Writer.Write(new NpcDeathEventData()
                        {
                            Position = ragdollComponent.Position
                        });
                    }

                    if (ConversionSettings.Config.Value.DefaultRagdollSystem)
                    {
                        if (!math.isnan(ragdollComponent.Position.x))
                        {
                            if (gpuSkin)
                            {
                                if (skinAnimatorData.CurrentAnimationHash != 0)
                                {
                                    var clipData = CrowdSkinProvider.GetClipData(skinAnimatorData.SkinIndex,
                                        skinAnimatorData.CurrentAnimationHash);
                                    var clipLength = clipData.ClipLength;

                                    var animationState =
                                        AnimatorDataProvider.GetAnimationState(skinAnimatorData.CurrentAnimationHash);
                                    var data = AnimatorDataProvider.GetLegacyAnimationData(animationState);

                                    var time = !skinAnimatorData.UniqueAnimation
                                        ? Timestamp
                                        : skinAnimatorData.StartAnimationTime;
                                    var clipTime = (time % clipLength) / clipLength;

                                    RagdollQueue.Enqueue(new ActivateRagdollData()
                                    {
                                        Entity = entity,
                                        SkinIndex = pedestrianCommonSettings.SkinIndex,
                                        Position = ragdollComponent.Position,
                                        Rotation = ragdollComponent.Rotation,
                                        ForceDirection = ragdollComponent.ForceDirection,
                                        ForceMultiplier = ragdollComponent.ForceMultiplier,
                                        DamageDealerID = ragdollComponent.DamageDealerID,
                                        AnimationHash = data.StateNameHash,
                                        StateLayer = data.StateLayer,
                                        NormalizedTime = clipTime,
                                        ParamHash = data.ParamNameHash1,
                                        ParamType = data.ParamType1,
                                        ParamValue = data.Value1
                                    });
                                }
                                else
                                {
                                    RagdollQueue.Enqueue(new ActivateRagdollData()
                                    {
                                        Entity = entity,
                                        SkinIndex = pedestrianCommonSettings.SkinIndex,
                                        Position = ragdollComponent.Position,
                                        Rotation = ragdollComponent.Rotation,
                                        ForceDirection = ragdollComponent.ForceDirection,
                                        ForceMultiplier = ragdollComponent.ForceMultiplier,
                                        DamageDealerID = ragdollComponent.DamageDealerID,
                                    });
                                }
                            }
                            else
                            {
                                RagdollQueue.Enqueue(new ActivateRagdollData()
                                {
                                    Entity = entity,
                                    SkinIndex = pedestrianCommonSettings.SkinIndex,
                                    Position = ragdollComponent.Position,
                                    Rotation = ragdollComponent.Rotation,
                                    ForceDirection = ragdollComponent.ForceDirection,
                                    ForceMultiplier = ragdollComponent.ForceMultiplier,
                                    DamageDealerID = ragdollComponent.DamageDealerID,
                                    HybridSkin = true
                                });
                            }
                        }
                    }
                }

                if (TriggerSupported)
                {
                    Writer.EndForEachIndex();
                }
            }
        }
    }
}