using Spirit604.Attributes;
using Spirit604.Collections.Dictionary;
using Spirit604.DotsCity.Core;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    public class PedestrianStateAuthoring : SyncConfigBase
    {
        [Serializable]
        private class StateInfo
        {
            public ActionState NextStates;
            public StateType StateType;
        }

        [Serializable]
        private class StateDictionary : AbstractSerializableDictionary<ActionState, StateInfo> { }

        [Serializable]
        private class MovementStateBindingDictionary : AbstractSerializableDictionary<ActionState, MovementState> { }

        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/pedestrianInit.html#state-authoring")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(Sync))]
        [SerializeField]
        private StateDictionary stateDictionary = new StateDictionary()
        {
            {
                ActionState.AchievedTarget, new StateInfo()
                {
                    StateType = StateType.ExternalSystem
                }
            },
            {
                ActionState.ScaryRunning, new StateInfo()
                {
                    StateType = StateType.Additive,
                }
            },
            {
                ActionState.Reset, new StateInfo()
                {
                    StateType = StateType.ExternalSystem
                }
            }
        };

        [OnValueChanged(nameof(Sync))]
        [SerializeField]
        private MovementStateBindingDictionary movementStateBindingDictionary = new MovementStateBindingDictionary()
        {
            { ActionState.MovingToNextTargetPoint, MovementState.Walking },
            { ActionState.CrossingTheRoad, MovementState.Walking },
            { ActionState.ScaryRunning, MovementState.Running },
            { ActionState.WaitForGreenLight, MovementState.Idle },
            { ActionState.Talking, MovementState.Idle },
            { ActionState.Sitting, MovementState.Idle },
            { ActionState.Idle, MovementState.Idle },
        };

        public class PedestrianStateAuthoringBaker : Baker<PedestrianStateAuthoring>
        {
            public override void Bake(PedestrianStateAuthoring authoring)
            {
                var stateDictionary = authoring.stateDictionary;

                foreach (var stateData in stateDictionary)
                {
                    var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                    AddComponent(entity, new StateData()
                    {
                        SourceState = stateData.Key,
                        NextStates = stateData.Value.NextStates,
                        StateType = stateData.Value.StateType,
                    });
                }

                var movementStateBindingDictionary = authoring.movementStateBindingDictionary;

                ActionState idleStates = ActionState.Default;

                foreach (var stateData in movementStateBindingDictionary)
                {
                    var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                    AddComponent(entity, new MovementStateActionBindingData()
                    {
                        PedestrianActionState = stateData.Key,
                        PedestrianMovementState = stateData.Value,
                    });

                    if (stateData.Value == MovementState.Idle)
                    {
                        idleStates |= stateData.Key;
                    }
                }

                var configEntity = CreateAdditionalEntity(TransformUsageFlags.None);

                AddComponent(configEntity, new CommonStateData()
                {
                    IdleStates = idleStates
                });
            }
        }
    }

    public struct CommonStateData : IComponentData
    {
        public ActionState IdleStates;
    }

    public struct StateData : IComponentData
    {
        public ActionState SourceState;
        public ActionState NextStates;
        public StateType StateType;
    }

    public struct MovementStateActionBindingData : IComponentData
    {
        public ActionState PedestrianActionState;
        public MovementState PedestrianMovementState;
    }
}