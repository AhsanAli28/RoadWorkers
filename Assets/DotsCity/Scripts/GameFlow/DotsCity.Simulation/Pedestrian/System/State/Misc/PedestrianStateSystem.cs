using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    [UpdateInGroup(typeof(PreEarlyJobGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct PedestrianStateSystem : ISystem, ISystemStartStop
    {
        public struct StateInfo
        {
            public ActionState NextStates;
            public StateType StateType;
        }

        private bool isInitialized;
        private NativeHashMap<int, MovementState> movementStateBindingHashMap;
        private NativeHashMap<int, StateInfo> stateInfoHashMap;
        private EntityQuery npcQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            npcQuery = SystemAPI.QueryBuilder()
                .WithAllRW<StateComponent, NextStateComponent>()
                .WithAllRW<PedestrianMovementSettings>()
                .WithPresentRW<HasTargetTag, MovementStateChangedEventTag>()
                .WithPresentRW<IdleTag>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                .Build();

            state.RequireForUpdate(npcQuery);
        }

        [BurstCompile]
        void ISystemStartStop.OnStartRunning(ref SystemState state)
        {
            Initialize(ref state);
        }

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            if (movementStateBindingHashMap.IsCreated)
            {
                movementStateBindingHashMap.Dispose();
            }

            if (stateInfoHashMap.IsCreated)
            {
                stateInfoHashMap.Dispose();
            }

            isInitialized = false;
        }

        void ISystemStartStop.OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var stateJob = new StateJob()
            {
                MovementStateBindingHashMap = movementStateBindingHashMap,
                StateInfoHashMap = stateInfoHashMap,
            };

            stateJob.ScheduleParallel(npcQuery);
        }

        [BurstCompile]
        public partial struct StateJob : IJobEntity
        {
            [ReadOnly]
            public NativeHashMap<int, MovementState> MovementStateBindingHashMap;

            [ReadOnly]
            public NativeHashMap<int, StateInfo> StateInfoHashMap;

            void Execute(
                ref StateComponent stateComponent,
                ref NextStateComponent nextStateComponent,
                ref PedestrianMovementSettings movementSettings,
                EnabledRefRW<HasTargetTag> hasTargetTagRW,
                EnabledRefRW<IdleTag> idleTagRW,
                EnabledRefRW<MovementStateChangedEventTag> movementStateChangedEventTagRW)
            {
                if (nextStateComponent.HasNextState)
                {
                    var currentState = stateComponent.ActionState;
                    var nextState = nextStateComponent.NextActionState;

                    bool removed = false;

                    if (nextStateComponent.RemoveState != ActionState.Default)
                    {
                        if (stateComponent.HasAnyAdditiveStateFlags() &&
                            DotsEnumExtension.HasFlagUnsafe(stateComponent.AdditiveStateFlags, nextStateComponent.RemoveState))
                        {
                            var additiveFlags = stateComponent.AdditiveStateFlags;
                            additiveFlags = additiveFlags.RemoveFlag(nextStateComponent.RemoveState);
                            stateComponent.AdditiveStateFlags = additiveFlags;
                            removed = true;
                        }

                        nextStateComponent.RemoveState = ActionState.Default;
                    }

                    if (!DotsEnumExtension.HasFlagUnsafe(currentState, nextState) || removed)
                    {
                        var nextStateFlags = nextStateComponent.NextStateFlags;
                        var currentStateType = StateType.Default;

                        GetStateInfo(nextState, out nextStateFlags, out currentStateType);

                        if (!nextStateComponent.CanSwitchState(nextState) && currentStateType != StateType.AdditiveAny && !nextStateComponent.ForceState)
                        {
                            nextState = ActionState.Default;
                            nextStateFlags = nextStateComponent.NextStateFlags;
                        }

                        if (nextStateComponent.ForceState)
                        {
                            nextStateComponent.ForceState = false;
                        }

                        if (!stateComponent.HasAnyAdditiveStateFlags())
                        {
                            if (nextStateComponent.NextStateFlags != nextStateFlags)
                            {
                                nextStateComponent.NextStateFlags = nextStateFlags;
                            }
                        }
                        else
                        {
                            if (nextStateComponent.NextStateFlags != nextStateFlags && nextStateFlags != ActionState.Default)
                            {
                                nextStateComponent.NextStateFlags = nextStateFlags;
                            }
                        }

                        // Enter code here -------------------

                        switch (nextState)
                        {
                            case ActionState.AchievedTarget:
                                {
                                    break;
                                }
                            case ActionState.ScaryRunning:
                                {
                                    break;
                                }
                            case ActionState.Reset:
                                {
                                    nextState = ActionState.MovingToNextTargetPoint;
                                    hasTargetTagRW.ValueRW = true;
                                    break;
                                }
                        }

                        // -----------------------------------

                        switch (currentStateType)
                        {
                            case StateType.Default:
                                break;
                            case StateType.ExternalSystem:
                                {
                                    nextState = ActionState.Default;
                                    break;
                                }
                            case StateType.Additive:
                                {
                                    stateComponent.AdditiveStateFlags = stateComponent.AdditiveStateFlags.AddFlag(nextState);
                                    nextState = stateComponent.ActionState;
                                    break;
                                }
                            case StateType.AdditiveAny:
                                {
                                    stateComponent.AdditiveStateFlags = stateComponent.AdditiveStateFlags.AddFlag(nextState);
                                    nextState = stateComponent.ActionState;
                                    break;
                                }
                        }

                        if (nextState != ActionState.Default)
                        {
                            MovementState movementState = MovementState.Default;

                            if (!stateComponent.HasAnyAdditiveStateFlags())
                            {
                                MovementStateBindingHashMap.TryGetValue((int)nextState, out movementState);
                            }
                            else
                            {
                                MovementStateBindingHashMap.TryGetValue((int)stateComponent.AdditiveStateFlags, out movementState);

                                if (movementState == MovementState.Default)
                                {
                                    MovementStateBindingHashMap.TryGetValue((int)nextState, out movementState);
                                }
                            }

                            stateComponent.ActionState = nextState;
                            stateComponent.MovementState = movementState;
                        }
                    }
                    else
                    {
                        if (currentState == nextState && nextStateComponent.NextStateFlags != ActionState.Default)
                        {
                            var nextStateFlags = ActionState.Default;

                            if (StateInfoHashMap.TryGetValue((int)nextState, out var stateInfo))
                            {
                                nextStateFlags = stateInfo.NextStates;
                            }

                            if (nextStateComponent.NextStateFlags != nextStateFlags)
                            {
                                nextStateComponent.NextStateFlags = nextStateFlags;
                            }
                        }
                    }

                    nextStateComponent.NextActionState = nextStateComponent.NextActionState2;
                    nextStateComponent.NextActionState2 = ActionState.Default;
                }

                if (stateComponent.MovementState != stateComponent.PreviousMovementState)
                {
                    stateComponent.PreviousMovementState = stateComponent.MovementState;

                    float movementSpeed = 0;

                    if (stateComponent.MovementState != MovementState.Idle)
                    {
                        movementSpeed = stateComponent.MovementState ==
                        MovementState.Running ?
                        movementSettings.RunningValue :
                        movementSettings.WalkingValue;
                    }
                    else
                    {
                        idleTagRW.ValueRW = true;
                    }

                    movementSettings.CurrentMovementSpeed = movementSpeed;
                    movementSettings.CurrentMovementSpeedSQ = movementSpeed * movementSpeed;

                    movementStateChangedEventTagRW.ValueRW = true;
                }
            }

            private void GetStateInfo(ActionState state, out ActionState nextStateFlags, out StateType currentStateType)
            {
                nextStateFlags = ActionState.Default;
                currentStateType = StateType.Default;

                if (StateInfoHashMap.TryGetValue((int)state, out var stateInfo))
                {
                    nextStateFlags = stateInfo.NextStates;
                    currentStateType = stateInfo.StateType;
                }
            }
        }

        private void Initialize(ref SystemState systemState)
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;

            var stateInfos = systemState.GetEntityQuery(ComponentType.ReadOnly<StateData>()).ToComponentDataArray<StateData>(Allocator.TempJob);

            stateInfoHashMap = new NativeHashMap<int, StateInfo>(stateInfos.Length, Allocator.Persistent);

            foreach (var state in stateInfos)
            {
                stateInfoHashMap.Add((int)state.SourceState, new StateInfo()
                {
                    StateType = state.StateType,
                    NextStates = state.NextStates
                });
            }

            stateInfos.Dispose();

            var bridgeStates = systemState.GetEntityQuery(ComponentType.ReadOnly<MovementStateActionBindingData>()).ToComponentDataArray<MovementStateActionBindingData>(Allocator.TempJob);

            movementStateBindingHashMap = new NativeHashMap<int, MovementState>(bridgeStates.Length, Allocator.Persistent);

            foreach (var state in bridgeStates)
            {
                var stateIndex = (int)state.PedestrianActionState;

                if (!movementStateBindingHashMap.ContainsKey(stateIndex))
                {
                    movementStateBindingHashMap.Add(stateIndex, state.PedestrianMovementState);
                }
                else
                {
#if UNITY_EDITOR
                    UnityEngine.Debug.LogError($"PedestrianStateSystem. MovementStateBindingHashMap {state.PedestrianActionState} duplicate key error");
#endif
                }
            }

            bridgeStates.Dispose();
        }

    }
}