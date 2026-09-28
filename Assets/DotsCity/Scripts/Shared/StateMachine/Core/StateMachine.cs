using System;
using UnityEngine;

namespace Spirit604.StateMachine
{
    public class StateMachine : MonoBehaviour
    {
        [SerializeField] private StateBase initialState;

        private StateBase currentState = null;

        public StateBase InitialState { get => initialState; }

        public bool DisableByDefault { get; set; } = true;

        public StateBase CurrentState { get => currentState; }

        public event Action<StateBase> StateChanged = delegate { };

        protected virtual void Awake()
        {
            if (initialState == null)
            {
                Destroy(gameObject);
            }

            if (DisableByDefault)
                SwitchEnabledState(false);
        }

        protected virtual void OnEnable()
        {
            currentState = initialState;
        }

        protected virtual void OnDisable()
        {
            LeaveState(currentState);
        }

        protected virtual void Update()
        {
            if (currentState == null)
                return;

            currentState.Tick();
            var transitions = currentState.Transitions;

            for (int i = 0; i < transitions.Count; i++)
            {
                var transition = transitions[i].TransitionCondition;

                if (transition.StateResult == StateResult.None)
                {
                    transition.Tick();
                }
                else
                {
                    StateBase newState = null;

                    if (transition.StateResult == StateResult.Success)
                    {
                        newState = transitions[i].OnSuccessState;
                    }
                    else
                    {
                        newState = transitions[i].OnFailState;
                    }

                    if (newState && newState != currentState)
                    {
                        LeaveState(currentState);
                        currentState = newState;
                        EnterState(currentState);
                        StateChanged(currentState);
                        break;
                    }
                    else
                    {
                        transition.Tick();
                    }
                }
            }
        }

        private void LeaveState(StateBase state)
        {
            state?.LeaveState();

            for (int j = 0; j < state?.Transitions?.Count; j++)
            {
                state.Transitions[j]?.TransitionCondition?.Leave();
            }
        }

        private void EnterState(StateBase state)
        {
            state?.EnterState();

            for (int j = 0; j < state.Transitions?.Count; j++)
            {
                state.Transitions[j]?.TransitionCondition?.Enter();
            }
        }

        public void SwitchEnabledState(bool isActive)
        {
            if (!isActive)
            {
                LeaveState(currentState);
            }
            else
            {
                currentState = initialState;
                EnterState(currentState);
            }

            gameObject.SetActive(isActive);
        }

        public bool CheckForInitialStateExist(bool showWarning = true)
        {
            if (initialState != null)
            {
                return true;
            }
            else if (showWarning)
            {
                UnityEngine.Debug.Log("Add initial state!");
            }

            return false;
        }
    }
}