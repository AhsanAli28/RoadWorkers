using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Sound;
using Spirit604.DotsCity.Simulation.Sound.Utils;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    public struct ScreamSoundTag : IComponentData { }

    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct ActivateRunningScaryStateSystem : ISystem
    {
        private NativeArray<int> screamCounter;
        private EntityQuery npcQuery;

        private EntityQuery screamSoundQuery;
        private EntityQuery soundPrefabQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            screamCounter = new NativeArray<int>(1, Allocator.Persistent);

            screamSoundQuery = SystemAPI.QueryBuilder()
                .WithAll<ScreamSoundTag>()
                .Build();

            soundPrefabQuery = SoundExtension.GetSoundQuery(state.EntityManager, SoundType.Tracking);

            npcQuery = SystemAPI.QueryBuilder()
                .WithNone<ScaryRunningTag>()
                .WithAll<ProcessScaryRunningTag>()
                .Build();

            state.RequireForUpdate(npcQuery);
        }

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            screamCounter.Dispose();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            screamCounter[0] = screamSoundQuery.CalculateEntityCount();

            var activateScaryJob = new ActivateScaryJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                LightHandlerLookup = SystemAPI.GetComponentLookup<LightHandlerComponent>(),
                InViewOfCameraLookup = SystemAPI.GetComponentLookup<InViewOfCameraTag>(),
                ScaryTriggerConfigReference = SystemAPI.GetSingleton<ScaryTriggerConfigReference>(),
                CurrentTime = (float)SystemAPI.Time.ElapsedTime,
                ScreamCounter = screamCounter,
                SoundPrefabEntity = soundPrefabQuery.GetSingletonEntity(),
            };

            activateScaryJob.Run();
        }

        [WithNone(typeof(ScaryRunningTag))]
        [WithAll(typeof(ProcessScaryRunningTag))]
        [BurstCompile]
        public partial struct ActivateScaryJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public ComponentLookup<LightHandlerComponent> LightHandlerLookup;

            [ReadOnly]
            public ComponentLookup<InViewOfCameraTag> InViewOfCameraLookup;

            [ReadOnly]
            public ScaryTriggerConfigReference ScaryTriggerConfigReference;

            [ReadOnly]
            public float CurrentTime;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<int> ScreamCounter;

            [ReadOnly]
            public Entity SoundPrefabEntity;

            void Execute(
                Entity entity,
                ref DestinationComponent destinationComponent,
                ref NextStateComponent nextStateComponent,
                in StateComponent stateComponent,
                in ProcessScaryRunningTag pedestrianProcessScaryRunningComponent,
                in LocalTransform transform)
            {
                if (!nextStateComponent.TryToSetNextState(ActionState.ScaryRunning))
                {
                    CommandBuffer.RemoveComponent<ProcessScaryRunningTag>(entity);
                    CommandBuffer.RemoveComponent<ScaryRunningTag>(entity);
                    return;
                }

                bool shouldSwitchTarget = stateComponent.MovementState == MovementState.Idle;

                if (!shouldSwitchTarget)
                {
                    float3 directionToPedestrian = math.normalize(transform.Position - pedestrianProcessScaryRunningComponent.TriggerPosition);

                    float3 oldTargetPosition = destinationComponent.PreviousDestination;
                    float3 newTargetPosition = destinationComponent.Value;

                    float3 directionToNextNode = math.normalize(newTargetPosition - oldTargetPosition);

                    float dot = math.dot(directionToPedestrian, directionToNextNode);

                    shouldSwitchTarget = dot < 0;
                }

                if (!shouldSwitchTarget)
                {
                    if (LightHandlerLookup.HasComponent(destinationComponent.DestinationNode) &&
                        LightHandlerLookup[destinationComponent.DestinationNode].State == LightState.Red)
                    {
                        shouldSwitchTarget = true;
                    }
                }

                if (shouldSwitchTarget)
                {
                    destinationComponent = destinationComponent.SwapBack();
                    CommandBuffer.SetComponentEnabled<HasTargetTag>(entity, true);
                }

                var canScream = ScaryTriggerConfigReference.Config.Value.HasScreamSound &&
                    InViewOfCameraLookup.HasComponent(entity) &&
                    InViewOfCameraLookup.IsComponentEnabled(entity) &&
                    ScreamCounter[0] < ScaryTriggerConfigReference.Config.Value.ScreamEntityLimit;

                if (canScream)
                {
                    var shouldScream = true;

                    var seed = UnityMathematicsExtension.GetSeed(CurrentTime, entity.Index, transform.Position);
                    var rnd = new Random(seed);

                    shouldScream = UnityMathematicsExtension.ChanceDropped(ScaryTriggerConfigReference.Config.Value.ChanceToScream, rnd);

                    if (shouldScream)
                    {
                        var soundId = ScaryTriggerConfigReference.Config.Value.ScreamSoundId;

                        if (soundId >= 0)
                        {
                            ScreamCounter[0]++;
                            var soundEntity = SoundExtension.CreateSoundEntity(ref CommandBuffer, SoundPrefabEntity, soundId);

                            CommandBuffer.AddComponent<ScreamSoundTag>(soundEntity);

                            rnd.InitState(MathUtilMethods.ModifySeed(seed, entity.Index));
                            float delay = rnd.NextFloat(ScaryTriggerConfigReference.Config.Value.MinScreamDelay, ScaryTriggerConfigReference.Config.Value.MaxScreamDelay);

                            CommandBuffer.AddComponent(soundEntity, new SoundDelayData()
                            {
                                Duration = delay
                            });

                            CommandBuffer.SetComponent(soundEntity, new TrackSoundComponent()
                            {
                                TargetEntity = entity
                            });
                        }
                        else
                        {
                            UnityEngine.Debug.LogError("Scream sound not found.");
                        }
                    }
                }

                CommandBuffer.SetComponentEnabled<HasTargetTag>(entity, true);
                CommandBuffer.SetComponentEnabled<ScaryRunningTag>(entity, true);
                CommandBuffer.SetComponentEnabled<ProcessScaryRunningTag>(entity, false);
            }
        }
    }
}