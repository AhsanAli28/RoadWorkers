using UnityEngine;
using UnityEngine.Events;

namespace Spirit604.StateMachine
{
    public abstract class SimpleStateBase : MonoBehaviour
    {
        public UnityEvent OnSuccess;
        public UnityEvent OnFail;
        public UnityEvent OnEnter;
        public UnityEvent OnLeave;

        [SerializeField] SimpleStateBase nextSuccessState;
        [SerializeField] SimpleStateBase nextFailState;

        public SimpleStateBase NextSuccessState => nextSuccessState;
        public SimpleStateBase NextFailState => nextFailState;

        public StateResult StateResult { get; set; }

        public virtual void OnDisable()
        {
            LeaveState();
        }

        public virtual void EnterState()
        {
            OnEnter?.Invoke();
        }

        public virtual void LeaveState()
        {
            OnLeave?.Invoke();
        }

        public virtual void ProcessSuccess()
        {
            OnSuccess?.Invoke();
        }

        public virtual void ProcessFail()
        {
            OnFail?.Invoke();
        }

        public virtual void Tick()
        {

        }
    }
}