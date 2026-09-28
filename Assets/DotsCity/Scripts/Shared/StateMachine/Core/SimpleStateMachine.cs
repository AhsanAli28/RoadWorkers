using System;
using UnityEngine;

namespace Spirit604.StateMachine
{
    public class SimpleStateMachine : MonoBehaviour
    {
        [SerializeField] SimpleStateBase initialState;

        private SimpleStateBase currentState;

        public event Action<SimpleStateBase> StateChanged = delegate { };

        private void Start()
        {
            currentState = initialState;
            initialState.EnterState();
        }

        private void Update()
        {
            if (currentState != null)
            {
                if (currentState.StateResult == StateResult.None)
                {
                    currentState.Tick();
                }
                else if (currentState.StateResult == StateResult.Success)
                {
                    currentState.ProcessSuccess();
                    currentState.LeaveState();
                    currentState = currentState.NextSuccessState;
                    currentState?.EnterState();
                    StateChanged(currentState);
                }
                else if (currentState.StateResult == StateResult.Fail)
                {
                    currentState.ProcessFail();
                    currentState.LeaveState();
                    currentState = currentState.NextFailState;
                    currentState?.EnterState();
                    StateChanged(currentState);
                }
            }
        }

        public void Stop()
        {
            currentState?.LeaveState();
            currentState = null;
        }
    }
}