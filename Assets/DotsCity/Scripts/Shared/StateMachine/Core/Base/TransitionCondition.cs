using UnityEngine;
using UnityEngine.Events;

namespace Spirit604.StateMachine
{
    public class TransitionCondition : MonoBehaviour
    {
        public UnityEvent OnEnter;
        public UnityEvent OnLeave;

        public StateResult StateResult { get; set; }

        public virtual void OnDisable()
        {
            Leave();
        }

        public virtual void Enter()
        {
            OnEnter?.Invoke();
        }

        public virtual void Leave()
        {
            OnLeave?.Invoke();
            StateResult = StateResult.None;
        }

        public virtual void Tick()
        {

        }
    }
}